# Simple Replay

## Problem

**Component purpose:** A background system tray utility that continuously buffers screen captures in a rolling window and, when a global hotkey is pressed, saves the last N minutes to a video file with a Windows toast notification confirming success.

**Inputs and outputs:**
- *Inputs:* Screen pixels (continuous), global hotkey event (on-demand), config file (at startup)
- *Outputs:* MP4 video file written to a user-configured directory, Windows toast notification

## Success

**Acceptance criteria:**
- Hotkey triggers within 500ms of being pressed, regardless of which window has focus
- Saved video covers at least the configured buffer window (e.g., if set to 5 min, at least 5 min is present in the file)
- Toast notification appears within 2 seconds of hotkey press
- No dropped frames or app crashes during extended idle runs (8+ hours)
- Config file changes take effect on restart without code changes
- User can configure output codec (e.g., h264, h265), compression preset, and CRF/quality factor

**Expected usage patterns:**
- Single user, single machine — no concurrency concerns
- Hotkey pressed infrequently (a few times per session), but capture runs continuously
- Buffer window: 1–10 minutes typical; up to 30 minutes for power users

**SLOs:**
- Capture loop CPU usage: < 10% on a modern machine at default settings (30fps, 720p)
- RAM for buffer: < 1GB at default settings (30fps, 5min, 720p)
- Save time: < 10 seconds for a 5-minute replay at default settings

## Approach

### Decisions

| # | Decision | Choice |
|---|---|---|
| 1 | Language | **C# / .NET 8** |
| 2 | Screen capture | **Windows.Graphics.Capture (WinRT)** |
| 3 | Rolling buffer | **In-memory ring buffer of JPEG-compressed frames** |
| 4 | Encoding | **FFmpeg.AutoGen** (C# bindings to libav) |
| 5 | Hotkey | **NHotkey** (NuGet wrapper around `RegisterHotKey`) |
| 6 | Notifications | **Microsoft.Toolkit.Uwp.Notifications** (what BurntToast wraps, used directly) |
| 7 | Configuration | **GUI settings window** (WinForms, accessible from system tray icon) |
| 8 | Installation | **Single self-contained `.exe`** (`dotnet publish --self-contained`) |
| 9 | Platform | **Windows only (v1)** |

### NuGet packages

| Package | Purpose |
|---|---|
| `NHotkey.WinForms` | Global hotkey registration |
| `FFmpeg.AutoGen` | C# bindings to libavcodec/libavformat/libswscale |
| `Microsoft.Toolkit.Uwp.Notifications` | Native Windows toast notifications |
| `System.Drawing.Common` | WinForms tray icon and settings window |

> ffmpeg native libs (`.dll`) bundled alongside the exe at publish time via a NuGet content package.

### UI framework

WinForms — lightweight, well-suited for a system tray app with a single settings dialog. No WPF or MAUI overhead needed.

### Settings window fields

- Hotkey (keyboard shortcut picker)
- Buffer duration (minutes)
- FPS (frames per second)
- Output directory (folder picker)
- Output codec (`h264` / `h265`)
- Encoding preset (`ultrafast` / `fast` / `medium` / `slow`)
- CRF / quality factor (slider, 0–51)
- Hardware acceleration (`none` / `nvenc` / `qsv` / `amf`)
- Buffer JPEG quality (controls RAM usage, separate from output quality)

Settings persisted to `%APPDATA%\SimpleReplay\settings.json` (JSON is fine here since users never hand-edit it — the GUI is the interface).

### Runtime sequence

```
startup
  └─ load settings from %APPDATA%\SimpleReplay\settings.json
  └─ register NHotkey hotkey
  └─ show system tray icon (right-click → Settings / Exit)
  └─ start CaptureThread

CaptureThread (continuous, background thread)
  └─ WinRT GraphicsCaptureSession → Direct3D11CaptureFrame
  └─ copy to CPU bitmap
  └─ resize to configured resolution
  └─ JPEG-compress at buffer_jpeg_quality → byte[]
  └─ ConcurrentQueue<(timestamp, byte[])>.Enqueue()
  └─ evict frames older than buffer_minutes

hotkey pressed
  └─ SaveThread spawned (does not block capture)
      └─ snapshot queue → List<(timestamp, byte[])>
      └─ toast: "Saving X-second replay..."
      └─ FFmpeg.AutoGen pipeline:
           open output file → avformat_write_header
           foreach frame: av_packet (mjpeg decode → h264/h265 encode → mux)
           av_write_trailer → close
      └─ toast: "Saved — filename.mp4 (X.X MB, Xs)"

tray icon → Settings
  └─ open WinForms settings dialog
  └─ on Save: write settings.json, restart capture thread with new config
```

## Risks

### Open questions

- **FFmpeg.AutoGen mjpeg→h264 pipeline complexity** — FFmpeg.AutoGen requires manual management of `AVCodecContext`, `AVFrame`, and `SwsContext`. The JPEG decode → rescale → h264 encode pipeline is non-trivial in C#. May need a spike to validate the < 10s SLO before committing to this approach.
- **WinRT capture requires a message loop** — `Windows.Graphics.Capture` must be initialized on a thread with a dispatcher/message loop. Needs validation that this works cleanly on a background thread in a WinForms app without a visible window.
- **Hardware acceleration detection** — Auto-detecting NVENC/QSV/AMF availability at runtime adds complexity. Failing gracefully to software encode when the selected hwaccel isn't available needs explicit handling.
- **Single-exe size with ffmpeg dlls** — Bundling libavcodec, libavformat, libswscale, etc. will add ~30–50MB to the exe. Acceptable, but worth confirming.

### Assumptions

- Target machine runs Windows 10 1903+ (required for `Windows.Graphics.Capture`)
- .NET 8 runtime is either self-contained in the exe or already present
- User has sufficient disk space for output directory (a 5-min h264 replay at CRF 23 is ~150–400MB depending on content)
- GPU-accelerated encode is optional — software h264 `ultrafast` must meet the < 10s SLO on its own on a modern CPU

### Security considerations

- Output directory is user-controlled — no path traversal risk as long as we sanitize the configured path before writing
- No network access, no elevation required
- `RegisterHotKey` does not require admin — no UAC prompt needed

## Breakdown

Six sub-components, each independently implementable in order:

### 1. Project scaffold & settings persistence
- WinForms app with no visible window at startup
- System tray icon with right-click menu (Settings, Exit)
- `SettingsService` — load/save `%APPDATA%\SimpleReplay\settings.json` with defaults
- Default settings: 30fps, 5min buffer, 720p, h264, preset ultrafast, CRF 23, hwaccel none, buffer JPEG quality 55

### 2. Settings GUI
- WinForms dialog opened from tray icon
- Fields for all configurable values (hotkey picker, fps, buffer duration, output dir, codec, preset, CRF slider, hwaccel dropdown, buffer JPEG quality)
- Save writes `settings.json`; cancel discards changes

### 3. Screen capture + rolling buffer
- `CaptureService` — GDI `Graphics.CopyFromScreen` on a dedicated thread
- Frame pipeline: screen grab → resize → JPEG compress → enqueue
- `FrameBuffer` — `ConcurrentQueue` with timestamp-based eviction to maintain the configured window

### 4. Encoding pipeline
- `EncoderService` — accepts `List<byte[]>` of JPEG frames, pipes them to bundled `ffmpeg.exe` as mjpeg input, writes MP4
- Supports codec (h264/h265), preset, CRF, and hwaccel flags
- Falls back gracefully if hwaccel fails

### 5. Hotkey + save orchestration
- Register global hotkey via `NHotkey.WinForms` on startup (re-register when settings change)
- On trigger: snapshot buffer → spawn save task → send "Saving..." toast → await encode → send result toast
- Guard against double-trigger while save is in progress

### 6. Notifications
- `NotificationService` wrapping `Microsoft.Toolkit.Uwp.Notifications`
- Two toast types: in-progress ("Saving Xs replay...") and completion ("Saved — file.mp4, X.X MB")
- Graceful no-op if notification permission is denied
