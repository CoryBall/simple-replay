# Simple Replay

A Windows system tray app that keeps a rolling buffer of your screen and saves the last N minutes to a video file when you press a hotkey.

---

## Requirements

- Windows 10 1903 or later
- .NET 8 Desktop Runtime (installed by `install.ps1`)
- ffmpeg (downloaded by `install.ps1`)

---

## Installation

1. Download `SimpleReplay-win-x64-Setup.exe` from the [latest release](../../releases/latest).
2. Run it. The installer handles the .NET 8 Desktop Runtime if it's not already present and creates Desktop and Start Menu shortcuts.
3. On first launch, the app will download ffmpeg (~80 MB, one-time) before starting.

To run on startup, copy the Start Menu shortcut to `shell:startup` (open with `Win+R`).

Updates are downloaded automatically in the background and applied the next time the app is launched.

---

## Usage

Simple Replay starts minimized to the system tray. There is no main window.

**Save a replay:** Press `Ctrl+Shift+F9` (default) from any application. A notification confirms when the file is saved.

**Tray menu:** Right-click the tray icon to save manually, open Settings, or exit.

Replays are saved as MP4 files to `Videos\SimpleReplay` by default, named `replay_YYYYMMDD_HHMMSS.mp4`.

---

## Configuration

Open Settings by right-clicking the tray icon and choosing **Settings…**. Changes take effect immediately after saving.

### Capture

| Setting | Default | Description |
|---|---|---|
| Hotkey | `Ctrl+Shift+F9` | Global key combination that triggers a save. Format: `Ctrl+Shift+F9`, `Alt+F10`, etc. |
| Buffer (minutes) | `5` | How many minutes of footage are kept in memory at all times. |
| Capture FPS | `30` | Frames captured per second. Lower values reduce CPU and RAM usage. |
| Output width | `1280` | Width of the saved video in pixels. |
| Output height | `720` | Height of the saved video in pixels. |

### Output

| Setting | Default | Description |
|---|---|---|
| Output folder | `Videos\SimpleReplay` | Directory where replay files are saved. |
| Codec | `h264` | `h264` encodes faster; `h265` produces smaller files at the same quality. |
| Encode preset | `ultrafast` | Controls the speed/compression tradeoff. `ultrafast` saves quickest; `slow` produces smaller files. |
| CRF (quality) | `23` | Constant rate factor. Lower = better quality, larger file. Range: 0–51. Typical range: 18–28. |
| Hardware accel | `none` | Uses GPU encoding if available. Options: `nvenc` (NVIDIA), `qsv` (Intel), `amf` (AMD). If the selected accelerator is not found, saving will fail — leave as `none` if unsure. |

### Memory

| Setting | Default | Description |
|---|---|---|
| Buffer quality | `55` | JPEG quality used for frames stored in memory. Lower values reduce RAM usage at some quality cost. |

At default settings (30 fps, 5 min, buffer quality 55) the buffer uses approximately 400–600 MB of RAM.

---

## Uninstall

1. Exit via the tray icon.
2. Delete `SimpleReplay.exe` and `install.ps1`.
3. Delete `%APPDATA%\SimpleReplay` (contains ffmpeg and settings).
4. Delete the Desktop and Start Menu shortcuts.
