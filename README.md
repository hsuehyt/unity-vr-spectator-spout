# Unity VR Spectator → OBS (Spout)

Record smooth, steady video of a PCVR game made in Unity, without touching what the player sees in the headset.

A separate "spectator" camera follows the headset with smoothing (or stays fixed in the scene), renders into a 1920×1080 texture, and sends it to OBS over [Spout](https://spout.zeal.co/). OBS records it like any other source.

Originally built for the VR game *誰去取火 Who Will Fetch the Fire*.

- **Follow mode**: first-person view that pans as you pan, but with head shake and tilt smoothed out.
- **Fixed mode**: a static third-person camera you can place anywhere by standing there and pressing a key.
- Zero cost when switched off. The player's headset view is unchanged.

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

- **Unity package (easiest):** download `VRSpectatorSpout.unitypackage` from [Releases](../../releases) → in Unity, **Assets → Import Package → Custom Package…**
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

Tip for third-person shots: stand where you want the camera, press **F10**, then walk into frame.

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

**Camera**: change **Field of View** (default 70) for a wider or tighter shot.

**SpectatorRT_1920x1080** (the RenderTexture):
- **Size**: change resolution, e.g. 2560×1440 or 3840×2160. Keep it 16:9, and match OBS's canvas.
- **Anti-aliasing**: 4 samples gives clean edges but uses ~100 MB of video memory at 1080p. Use 2 samples or None if the headset starts dropping frames.

---

## Performance

The spectator renders the scene one extra time per frame. When you're not recording, untick the spectator GameObject. It then costs nothing.

## Troubleshooting

| Problem | Fix |
|---|---|
| OBS sender list is empty | Unity must be in Play mode, and the spectator GameObject must be active. |
| OBS shows black | Put on the headset. Unity's XR loop may sit idle until the headset is worn. |
| Picture squashed / black bars | Set OBS canvas to 1920×1080, then **Transform → Reset Transform** on the source. |
| Nothing received at all | Check **Project Settings → Player → Graphics APIs**: Direct3D11 must be first. |
| Camera doesn't move | Headset camera isn't tagged MainCamera. Drag it into **Target**. |
| Compile error about `UniversalAdditionalCameraData` | Project isn't using URP. |

## Removing it

Delete the `_Recording` folder and the spectator object from your scene.

## What's included

```
Assets/_Recording/
├─ FetchFire Spectator.prefab           Camera + SpectatorCamera + SpoutSender
├─ SpectatorCamera.cs                   follow / fixed logic, F9/F10 keys
└─ SpectatorRT_1920x1080.renderTexture  the video feed sent to OBS
```
