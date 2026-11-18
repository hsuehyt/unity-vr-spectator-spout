# Unity VR Spectator → OBS (Spout)

Record smooth, steady video of a PCVR game made in Unity, without touching what the player sees in the headset.

A separate "spectator" camera follows the headset with smoothing (or stays fixed in the scene), renders into a 1920×1080 texture, and sends it to OBS over [Spout](https://spout.zeal.co/). OBS records it like any other source.

Originally built for the VR game *誰去取火 Who Will Fetch the Fire*.

- **Follow mode**: first-person view that pans as you pan, but with head shake and tilt smoothed out.
- **Fixed mode**: a static third-person camera you can place anywhere by standing there and pressing a key.
- **Frame guide**: corner brackets and a crosshair in the headset show exactly what's in the shot. Only you see them; they never appear in the recording.
- Zero cost when switched off. The player's headset view is unchanged.

### Why not Unity Recorder?

Unity's built-in [Recorder](https://docs.unity3d.com/Packages/com.unity.recorder@latest) works, but it isn't a great fit for VR:

- **Editor only.** It can't record a built game, and OBS can.
- **It runs inside the game.** Recorder encodes the video in Unity's own process, which can drop frames. In a headset, dropped frames mean stutter and motion sickness for the player. OBS records in a separate program using the graphics card's video encoder (e.g. NVENC), so the game barely notices.
- **Shaky footage by default.** Recorder's usual source is the Game view, which mirrors the raw head movement. This project adds a smoothed, level camera made for viewers.
- **OBS does more.** You get your microphone, overlays, scene switching and live streaming in the same place.

For a quick capture in the Editor, Recorder is still fine.

---

## Requirements

| | |
|---|---|
| Unity | Unity 6 (tested on 6000.3, should work on 2022.3+) |
| Render pipeline | **URP** |
| Graphics API | **Direct3D 11** on Windows (Spout requirement) |
| Package | [KlakSpout](https://github.com/keijiro/KlakSpout) `jp.keijiro.klak.spout` |
| OBS | OBS Studio 32 or 33 with the [OBS Spout2 plugin](https://github.com/Off-World-Live/obs-spout2-plugin/releases) |
| Scene | Your headset camera tagged **MainCamera** (most XR rigs already are) |

Works with either the Input System or the old Input Manager.

---

## 1. Unity setup

### Install KlakSpout
Add this to your project's `Packages/manifest.json`:

```json
"scopedRegistries": [
  {
    "name": "Keijiro",
    "url": "https://registry.npmjs.com",
    "scopes": [ "jp.keijiro" ]
  }
],
"dependencies": {
  "jp.keijiro.klak.spout": "2.0.3",
  ...
}
```

### Import the spectator
Pick one:

- **Unity package (easiest):** download [`VRSpectatorSpout.unitypackage`](VRSpectatorSpout.unitypackage) (click it, then the **Download raw file** button) → in Unity, **Assets → Import Package → Custom Package…**
- **Copy the folder:** copy `Assets/_Recording` **and** `Assets/_Recording.meta` into your project's `Assets` folder. Keep the `.meta` files, or the prefab loses its links.

### Add it to your scene
Drag `Assets/_Recording/FetchFire Spectator.prefab` into the scene that holds your XR rig (or a scene that stays loaded).

That's it. With **Target** left empty it follows the MainCamera. If your headset camera isn't tagged MainCamera, drag it into **Target**.

> Optional: rename the sender in **Spout Sender → Spout Name** (default `FetchFire Spectator`).

---

## 2. OBS setup

1. Install the OBS Spout2 plugin (`OBS_Spout2_Plugin_Install_v*.exe`) with OBS closed.
   If **Spout2 Capture** doesn't show up as a source, use the `.zip` instead and extract it into `C:\ProgramData\obs-studio\plugins`.
2. **Settings → Video**: set **Base (Canvas)** and **Output (Scaled)** resolution to `1920x1080`. Use 60 FPS for smoother motion.
3. Press **Play** in Unity (the sender only exists while the game runs).
4. In OBS, **Sources → + → Spout2 Capture** → in its properties, pick your sender (e.g. `FetchFire Spectator`) from **Spout Senders**.
   Don't leave it on *Use first available sender* if your project has other Spout senders.
5. If the picture looks squashed: right-click the source → **Transform → Reset Transform**.

---

## 3. Recording

1. Start recording in OBS (or set a hotkey for it).
2. Press **Play** in Unity, or launch your build.
3. Put on the headset and play.
4. Stop recording; trim the start and end.

### Keys (on the PC keyboard)

| Key | Action |
|---|---|
| **F9** | Switch Follow ↔ Fixed |
| **F10** | Drop the fixed camera where your head is right now (and switch to Fixed) |
| **F11** | Frame guide: Brackets + Crosshair → Brackets only → Off |

Tip for third-person shots: stand where you want the camera, press **F10**, then walk into frame.

### Frame guide (in the headset only)

In Follow mode you'll see four thin corner brackets about 2 m in front of you. They mark the edges of the recorded shot. A small crosshair with an open centre marks the middle.

- The guide follows the spectator camera's *smoothed* movement, not your head directly. When you turn quickly, it trails slightly behind; that's exactly what the video shows at that moment.
- It's drawn on top of everything, so walls never hide it.
- It only shows for the headset camera. The recording, the Scene view and any other cameras (for example projection cameras) never see it. It does appear in the Unity Game view, because that mirrors the headset.
- It hides in Fixed mode, where a frame in front of your face wouldn't match the camera's position.
- On your monitor the Game view is low resolution, so the thin crosshair may flicker there. In the headset it's 3–4 pixels wide.

---

## Settings

Select the spectator in the Hierarchy:

**Spectator Camera**

| Setting | Default | What it does |
|---|---|---|
| Target | *(empty)* | Headset camera to follow. Empty = MainCamera. |
| Mode | Follow | Starting mode. |
| Position Smoothing | 0.04 | Seconds to catch up half the distance. 0 = exact copy of your head. |
| Rotation Smoothing | 0.12 | Higher = steadier, slower pans. 0 = exact copy. |
| Remove Roll | on | Keeps the horizon level when you tilt your head. |
| Fixed Position / Euler | | Where the fixed camera sits (F10 sets these for you). |
| Look At Player | off | Fixed camera slowly turns to keep you in frame. |

**Frame Guide (headset only)** (child object)

| Setting | Default | What it does |
|---|---|---|
| Style | Brackets And Crosshair | Starting style. F11 cycles while playing. |
| Color | white, 60% | Line colour and opacity. |
| Distance | 2 m | How far ahead the guide floats. Doesn't change how big it looks. |
| Line Width Degrees | 0.15 | Line thickness. Try 0.2–0.25 if it's hard to see. |
| Bracket Length | 0.2 | Length of each corner arm (fraction of half the frame height). |

**Camera**: **Field of View** (default 55) sets how wide the shot is. Unity's FOV is the *vertical* angle, so at 16:9 the horizontal coverage is much wider:

| Field of View | Horizontal coverage | Frame guide in the headset |
|---|---|---|
| 70 | ~102° | Side brackets at or past the edge of your view |
| 60 | ~91° | Side brackets near the edges |
| **55** (default) | ~86° | Clearly visible, comfortable |
| 50 | ~79° | Clearly visible; tighter, more cinematic shot |

**SpectatorRT_1920x1080** (the RenderTexture):
- **Size**: change resolution, e.g. 2560×1440 or 3840×2160. Keep it 16:9, and match OBS's canvas.
- **Anti-aliasing**: 4 samples gives clean edges but uses ~100 MB of video memory at 1080p. Use 2 samples or None if the headset starts dropping frames.

---

## Performance

The spectator renders the scene one extra time per frame. When you're not recording, untick the spectator GameObject. It then costs nothing: no extra rendering, no Spout feed, no hotkeys, no guide. The frame guide itself costs almost nothing.

### At an exhibition

Untick the spectator GameObject so visitors don't see the frame guide and the headset keeps its full frame rate. If you want to film visitors, keep it on but set the guide's **Style** to **Off**, and consider a Fixed third-person shot (F10). If you exhibit a built game and never record from the build, you can also set the object's **Tag** to **EditorOnly** so Unity leaves it out of builds.

## Troubleshooting

| Problem | Fix |
|---|---|
| OBS sender list is empty | Unity must be in Play mode, and the spectator GameObject must be active. |
| OBS shows black | Put on the headset. Unity's XR loop may sit idle until the headset is worn. |
| Picture squashed / black bars | Set OBS canvas to 1920×1080, then **Transform → Reset Transform** on the source. |
| Nothing received at all | Check **Project Settings → Player → Graphics APIs**: Direct3D11 must be first. |
| Camera doesn't move | Headset camera isn't tagged MainCamera. Drag it into **Target**. |
| Compile error about `UniversalAdditionalCameraData` | Project isn't using URP. |
| Frame guide not visible | Must be in Follow mode (F9), Style not Off (F11), and the headset camera must be the spectator's Target (MainCamera). |
| Frame guide too thin / hard to see | Raise **Line Width Degrees** to 0.2–0.25, or raise the Color alpha. |
| Side brackets cut off at the edge of the headset view | Lower the spectator **Field of View** (see the table above). |

## Removing it

Delete the `_Recording` folder and the spectator object from your scene.

## What's included

```
Assets/_Recording/
├─ FetchFire Spectator.prefab           Camera + SpectatorCamera + SpoutSender,
│                                       with a "Frame Guide (headset only)" child
├─ SpectatorCamera.cs                   follow / fixed logic, F9/F10 keys
├─ SpectatorFrameGuide.cs               corner brackets + crosshair, F11 key
├─ SpectatorFrameGuide.shader           always-on-top unlit lines (VR-ready)
├─ SpectatorFrameGuide.mat              material for the guide
└─ SpectatorRT_1920x1080.renderTexture  the video feed sent to OBS
```
