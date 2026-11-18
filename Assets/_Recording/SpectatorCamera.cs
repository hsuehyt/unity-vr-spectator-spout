using UnityEngine;
using UnityEngine.Rendering.Universal;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Recording-only camera. Renders into a RenderTexture (sent to OBS by a SpoutSender),
/// never to the headset or the desktop window.
///   F9  : switch Follow <-> Fixed
///   F10 : drop the Fixed camera where your head is right now (and switch to Fixed)
/// Disable this GameObject when you're not recording and it costs nothing.
/// </summary>
[RequireComponent(typeof(Camera))]
public class SpectatorCamera : MonoBehaviour
{
    public enum Mode { Follow, Fixed }

    [Tooltip("Headset camera to follow. Left empty, the MainCamera is used.")]
    public Transform target;
    public Mode mode = Mode.Follow;

    [Header("Follow mode")]
    [Tooltip("Seconds to close half the distance to the head. 0 = exact copy.")]
    [Range(0f, 0.5f)] public float positionSmoothing = 0.04f;
    [Tooltip("Seconds to close half the angle to the head. Higher = steadier pans.")]
    [Range(0f, 0.5f)] public float rotationSmoothing = 0.12f;
    [Tooltip("Keep the horizon level (removes head tilt).")]
    public bool removeRoll = true;

    [Header("Fixed mode")]
    public Vector3 fixedPosition = new Vector3(0f, 1.6f, -2f);
    public Vector3 fixedEuler = Vector3.zero;
    [Tooltip("In Fixed mode, slowly turn to keep the player in frame.")]
    public bool lookAtPlayer = false;

    [Header("Keys")]
#if ENABLE_INPUT_SYSTEM
    public Key toggleModeKey = Key.F9;
    public Key dropFixedHereKey = Key.F10;
#else
    public KeyCode toggleModeKey = KeyCode.F9;
    public KeyCode dropFixedHereKey = KeyCode.F10;
#endif

    Camera _cam;
    bool _snapNext = true;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        // URP: render this camera flat (mono), never into the headset.
        var urp = _cam.GetUniversalAdditionalCameraData();
        if (urp != null) urp.allowXRRendering = false;
        if (_cam.targetTexture == null)
            Debug.LogWarning("[SpectatorCamera] No Target Texture set; it would draw over the Game view.", this);
    }

    void OnEnable()
    {
        _snapNext = true;
        Application.onBeforeRender += UpdatePose; // latest head pose, same frame as the headset
    }

    void OnDisable()
    {
        Application.onBeforeRender -= UpdatePose;
    }

    void Update()
    {
        if (Pressed(toggleModeKey))
        {
            mode = mode == Mode.Follow ? Mode.Fixed : Mode.Follow;
            _snapNext = true;
            Debug.Log("[SpectatorCamera] Mode: " + mode);
        }
        if (Pressed(dropFixedHereKey) && ResolveTarget())
        {
            fixedPosition = target.position;
            fixedEuler = Level(target.rotation).eulerAngles;
            mode = Mode.Fixed;
            _snapNext = true;
            Debug.Log("[SpectatorCamera] Fixed camera dropped at " + fixedPosition);
        }
    }

    void LateUpdate() { UpdatePose(); }

    void UpdatePose()
    {
        if (mode == Mode.Fixed)
        {
            transform.position = fixedPosition;
            var rot = Quaternion.Euler(fixedEuler);
            if (lookAtPlayer && ResolveTarget())
            {
                var dir = target.position - fixedPosition;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    var want = Quaternion.LookRotation(dir, Vector3.up);
                    rot = _snapNext ? want : Quaternion.Slerp(transform.rotation, want, Blend(0.3f));
                }
            }
            transform.rotation = rot;
            _snapNext = false;
            return;
        }

        if (!ResolveTarget()) return;
        var goalPos = target.position;
        var goalRot = removeRoll ? Level(target.rotation) : target.rotation;

        if (_snapNext)
        {
            transform.SetPositionAndRotation(goalPos, goalRot);
            _snapNext = false;
            return;
        }
        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, goalPos, Blend(positionSmoothing)),
            Quaternion.Slerp(transform.rotation, goalRot, Blend(rotationSmoothing)));
    }

    // Frame-rate independent smoothing: halfLife seconds to close half the gap.
    static float Blend(float halfLife)
    {
        if (halfLife <= 0f) return 1f;
        return 1f - Mathf.Pow(0.5f, Time.unscaledDeltaTime / halfLife);
    }

    static Quaternion Level(Quaternion q)
    {
        var fwd = q * Vector3.forward;
        if (Mathf.Abs(Vector3.Dot(fwd, Vector3.up)) > 0.98f) return q; // looking straight up/down
        return Quaternion.LookRotation(fwd, Vector3.up);
    }

    bool ResolveTarget()
    {
        if (target) return true;
        var main = Camera.main;
        if (main && main != _cam) target = main.transform;
        return target;
    }

#if ENABLE_INPUT_SYSTEM
    static bool Pressed(Key k)
    {
        var kb = Keyboard.current;
        return kb != null && k != Key.None && kb[k].wasPressedThisFrame;
    }
#else
    static bool Pressed(KeyCode k) { return Input.GetKeyDown(k); }
#endif
}
