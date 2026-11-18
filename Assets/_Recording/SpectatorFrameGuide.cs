using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Shows the edges (corner brackets) and centre (crosshair) of the recorded shot,
/// in the headset only. Never appears in the recording or on any other camera.
///   F11 : Brackets + Crosshair -> Brackets only -> Off
/// Only visible in Follow mode. Put this on a child of the SpectatorCamera object.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SpectatorFrameGuide : MonoBehaviour
{
    public enum Style { BracketsAndCrosshair, BracketsOnly, Off }

    [Tooltip("Starting style. F11 cycles through them while playing.")]
    public Style style = Style.BracketsAndCrosshair;
    public Color color = new Color(1f, 1f, 1f, 0.6f);
    [Tooltip("How far in front of you the guide floats (metres). Doesn't change how big it looks.")]
    [Range(0.5f, 5f)] public float distance = 2f;
    [Tooltip("Line thickness in degrees of your view.")]
    [Range(0.05f, 0.5f)] public float lineWidthDegrees = 0.15f;
    [Tooltip("Length of each corner bracket arm, as a fraction of half the frame height.")]
    [Range(0.05f, 0.5f)] public float bracketLength = 0.2f;
    [Tooltip("Uses the VRSpectator/FrameGuide shader (always drawn on top).")]
    public Material material;

#if ENABLE_INPUT_SYSTEM
    public Key cycleKey = Key.F11;
#else
    public KeyCode cycleKey = KeyCode.F11;
#endif

    SpectatorCamera _spec;
    Camera _specCam;
    MeshRenderer _renderer;
    Mesh _mesh;
    Material _mat;
    float _bFov, _bAspect, _bDist, _bWidth, _bArm;
    Style _bStyle = (Style)(-1);

    void Awake()
    {
        _spec = GetComponentInParent<SpectatorCamera>();
        _specCam = _spec ? _spec.GetComponent<Camera>() : null;
        _renderer = GetComponent<MeshRenderer>();
        _renderer.shadowCastingMode = ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
        _renderer.lightProbeUsage = LightProbeUsage.Off;
        _renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _renderer.enabled = false;

        _mesh = new Mesh { name = "SpectatorFrameGuide" };
        _mesh.MarkDynamic();
        GetComponent<MeshFilter>().sharedMesh = _mesh;
        if (material) { _mat = new Material(material); _renderer.sharedMaterial = _mat; }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    void OnEnable()  { RenderPipelineManager.beginCameraRendering += OnBeginCamera; }
    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        if (_renderer) _renderer.enabled = false;
    }

    void OnDestroy()
    {
        if (_mesh) Destroy(_mesh);
        if (_mat) Destroy(_mat);
    }

    void Update()
    {
        if (Pressed(cycleKey))
        {
            style = (Style)(((int)style + 1) % 3);
            Debug.Log("[SpectatorFrameGuide] " + style);
        }
    }

    void LateUpdate()
    {
        if (!_specCam) return;
        if (_mat) _mat.SetColor("_Color", color);

        float aspect = _specCam.targetTexture
            ? (float)_specCam.targetTexture.width / _specCam.targetTexture.height
            : _specCam.aspect;
        if (style != _bStyle || _specCam.fieldOfView != _bFov || aspect != _bAspect ||
            distance != _bDist || lineWidthDegrees != _bWidth || bracketLength != _bArm)
            Rebuild(_specCam.fieldOfView, aspect);
    }

    // Draw only for the headset camera; hide for the spectator, projection cameras, Scene view, etc.
    void OnBeginCamera(ScriptableRenderContext ctx, Camera cam)
    {
        if (!_renderer) return;
        bool show = style != Style.Off
                    && _spec && _spec.mode == SpectatorCamera.Mode.Follow
                    && _spec.target && cam.transform == _spec.target
                    && _mat;
        if (show && (cam.cullingMask & (1 << gameObject.layer)) == 0)
            gameObject.layer = FirstLayerIn(cam.cullingMask); // headset camera may skip Default
        _renderer.enabled = show;
    }

    static int FirstLayerIn(int mask)
    {
        for (int i = 0; i < 32; i++) if ((mask & (1 << i)) != 0) return i;
        return 0;
    }

    void Rebuild(float fov, float aspect)
    {
        _bStyle = style; _bFov = fov; _bAspect = aspect;
        _bDist = distance; _bWidth = lineWidthDegrees; _bArm = bracketLength;

        var v = new List<Vector3>();
        var t = new List<int>();
        float halfH = distance * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
        float halfW = halfH * aspect;
        float w = distance * Mathf.Tan(lineWidthDegrees * Mathf.Deg2Rad);

        if (style != Style.Off)
        {
            // Corner brackets
            float arm = bracketLength * halfH;
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            {
                var c = new Vector2(sx * halfW, sy * halfH);
                AddLine(v, t, c, c + new Vector2(-sx * arm, 0f), w);
                AddLine(v, t, c, c + new Vector2(0f, -sy * arm), w);
            }
        }
        if (style == Style.BracketsAndCrosshair)
        {
            // Open-centre crosshair: four ticks around a gap
            float gap = 0.025f * halfH, len = 0.07f * halfH;
            AddLine(v, t, new Vector2(gap, 0f), new Vector2(gap + len, 0f), w);
            AddLine(v, t, new Vector2(-gap, 0f), new Vector2(-gap - len, 0f), w);
            AddLine(v, t, new Vector2(0f, gap), new Vector2(0f, gap + len), w);
            AddLine(v, t, new Vector2(0f, -gap), new Vector2(0f, -gap - len), w);
        }

        _mesh.Clear();
        _mesh.SetVertices(v);
        _mesh.SetTriangles(t, 0);
        _mesh.bounds = new Bounds(new Vector3(0f, 0f, distance), new Vector3(halfW * 2f + 1f, halfH * 2f + 1f, 1f));
    }

    // A flat strip from a to b, w thick, ends extended by w/2 so bracket corners meet cleanly.
    void AddLine(List<Vector3> v, List<int> t, Vector2 a, Vector2 b, float w)
    {
        var d = (b - a).normalized;
        var n = new Vector2(-d.y, d.x) * (w * 0.5f);
        a -= d * (w * 0.5f);
        b += d * (w * 0.5f);
        int i = v.Count;
        v.Add(new Vector3(a.x + n.x, a.y + n.y, distance));
        v.Add(new Vector3(a.x - n.x, a.y - n.y, distance));
        v.Add(new Vector3(b.x - n.x, b.y - n.y, distance));
        v.Add(new Vector3(b.x + n.x, b.y + n.y, distance));
        t.Add(i); t.Add(i + 1); t.Add(i + 2);
        t.Add(i); t.Add(i + 2); t.Add(i + 3);
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
