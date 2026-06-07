# Simple Replay

Press a hotkey. Get the last few minutes of your screen saved as a video. That's it.

Simple Replay runs silently in your system tray and keeps a rolling buffer of your screen. Whenever something happens worth keeping — a bug, a clutch moment, an unexpected error — press your hotkey and the replay is saved automatically.

---

## Install

**Requirements:** Windows 10 (1903 or later)

1. Go to the [latest release](../../releases/latest) and download both files:
   - `SimpleReplay.exe`
   - `install.ps1`

2. Put them in the same folder, then right-click `install.ps1` and choose **Run with PowerShell**.

   The installer will:
   - Install the .NET 8 Desktop Runtime (if you don't have it)
   - Download ffmpeg (~80 MB, one-time)
   - Create shortcuts on your Desktop and in the Start Menu

3. Launch Simple Replay from the shortcut. A small icon will appear in your system tray.

> **Tip:** To run on startup, right-click the Start Menu shortcut → **More** → **Open file location**, then copy it to `shell:startup` in the Run dialog (`Win+R`).

---

## Usage

### Saving a replay

Press **Ctrl+Shift+F9** (default) at any time — no need to focus the app.

A notification will appear while saving, then again when the file is ready:

```
Simple Replay — Saved!
47s · 124.3 MB → replay_20260606_143201.mp4
```

Replays are saved to **Videos\SimpleReplay** by default.

### Tray icon

Right-click the tray icon for quick actions:

| Menu item | What it does |
|---|---|
| Save Replay Now | Same as pressing the hotkey |
| Settings… | Open the settings window |
| Exit | Stop the app |

---

## Settings

Open Settings by right-clicking the tray icon.

| Setting | Default | Description |
|---|---|---|
| **Hotkey** | `Ctrl+Shift+F9` | The key combination that saves a replay |
| **Buffer (minutes)** | `5` | How many minutes are kept ready at all times |
| **Capture FPS** | `30` | Frames captured per second |
| **Output width / height** | `1280 × 720` | Resolution of the saved video |
| **Output folder** | `Videos\SimpleReplay` | Where replay files are saved |
| **Codec** | `h264` | Video codec — `h264` is fastest, `h265` gives smaller files |
| **Encode preset** | `ultrafast` | Speed vs compression — `ultrafast` saves quickly, `slow` gives smaller files |
| **CRF (quality)** | `23` | 0 = best quality / largest file · 51 = worst quality / smallest file |
| **Hardware accel** | `none` | `nvenc` (NVIDIA) · `qsv` (Intel) · `amf` (AMD) — speeds up saving significantly |
| **Buffer quality** | `55` | JPEG quality used for the in-memory buffer — lower uses less RAM |

### RAM usage

At default settings (30 fps, 5 min, buffer quality 55) the buffer uses around **400–600 MB** of RAM. If that's too much:
- Lower **Buffer quality** (e.g. 40) to reduce RAM at a slight quality cost
- Lower **Capture FPS** (e.g. 15)
- Reduce **Buffer (minutes)**

---

## Troubleshooting

**The hotkey doesn't work**
Some apps (games, remote desktop, certain system tools) block global hotkeys. Try using **Save Replay Now** from the tray icon instead. You can also try a different key combination in Settings.

**"ffmpeg.exe was not found"**
Re-run `install.ps1` — the ffmpeg download may have failed the first time.

**The saved video is blank or shows a lock screen**
Captures happen at the moment of capture, not retroactively. If the screen was locked or a DRM-protected window was fullscreen, those frames will be black. This is a Windows limitation.

**Hardware acceleration isn't working**
Leave **Hardware accel** set to `none` — if the selected accelerator isn't found, saving will fail rather than fall back automatically. Software encoding with `ultrafast` is fast enough for most cases.

**The file is too large**
Increase the **CRF** value (e.g. 28–32) or switch **Codec** to `h265`. You can also lower the output resolution.

---

## Uninstall

1. Exit Simple Replay from the tray icon
2. Delete `SimpleReplay.exe` and `install.ps1`
3. Delete `%APPDATA%\SimpleReplay` (contains ffmpeg and your settings)
4. Delete the shortcuts from your Desktop and Start Menu
