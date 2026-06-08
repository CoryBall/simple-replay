using SimpleReplay.Models;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleReplay.Forms;

public sealed class SettingsForm : Form
{
    public AppSettings Settings { get; private set; }

    private HotkeyBox _hotkeyBox = null!;
    private ComboBox _monitor = null!;
    private List<Screen> _screens = null!;
    private NumericUpDown _bufferMinutes = null!;
    private NumericUpDown _fps = null!;
    private NumericUpDown _width = null!;
    private NumericUpDown _height = null!;
    private TextBox _outputDir = null!;
    private ComboBox _codec = null!;
    private ComboBox _preset = null!;
    private TrackBar _crf = null!;
    private Label _crfLabel = null!;
    private ComboBox _hwAccel = null!;
    private TrackBar _bufferJpegQuality = null!;
    private Label _jpegQualityLabel = null!;
    private Label _descLabel = null!;

    public SettingsForm(AppSettings current)
    {
        Settings = new AppSettings
        {
            Hotkey = current.Hotkey,
            BufferMinutes = current.BufferMinutes,
            Fps = current.Fps,
            Width = current.Width,
            Height = current.Height,
            OutputDirectory = current.OutputDirectory,
            Codec = current.Codec,
            Preset = current.Preset,
            Crf = current.Crf,
            HwAccel = current.HwAccel,
            BufferJpegQuality = current.BufferJpegQuality,
        };
        BuildUI();
        LoadValues();
        WireDescriptions();
    }

    private void BuildUI()
    {
        Text = "Simple Replay — Settings";
        Size = new Size(460, 660);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        // Root splits settings area from description panel
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = Padding.Empty,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));

        // ── Settings rows ───────────────────────────────────────────────────────
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            AutoSize = false,
        };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));

        int r = 0;

        AddRow(outer, r++, "Hotkey",
            _hotkeyBox = new HotkeyBox { Dock = DockStyle.Fill });

        _screens = Screen.AllScreens.OrderBy(s => s.Bounds.X).ToList();
        _monitor = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        for (int i = 0; i < _screens.Count; i++)
        {
            var s = _screens[i];
            var label = $"Display {i + 1}{(s.Primary ? " (Primary)" : "")} — {s.Bounds.Width}×{s.Bounds.Height}";
            _monitor.Items.Add(label);
        }
        _monitor.SelectedIndexChanged += OnMonitorChanged;
        AddRow(outer, r++, "Monitor", _monitor);

        AddRow(outer, r++, "Buffer (minutes)",
            _bufferMinutes = Spinner(1, 60));

        AddRow(outer, r++, "Capture FPS",
            _fps = Spinner(1, 60));

        AddRow(outer, r++, "Output width (px)",
            _width = Spinner(320, 3840, 160));

        AddRow(outer, r++, "Output height (px)",
            _height = Spinner(240, 2160, 90));

        AddRow(outer, r++, "Output folder",
            BuildFolderRow());

        AddRow(outer, r++, "Codec",
            _codec = Combo("h264", "h265"));

        AddRow(outer, r++, "Encode preset",
            _preset = Combo("ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow"));

        _crfLabel = MakeLabel("");
        _crf = new TrackBar { Minimum = 0, Maximum = 51, TickFrequency = 5, Dock = DockStyle.Fill };
        _crf.ValueChanged += (_, _) => _crfLabel.Text = $"CRF (quality): {_crf.Value}";
        AddRow(outer, r++, _crfLabel, _crf);

        AddRow(outer, r++, "Hardware accel",
            _hwAccel = Combo("none", "nvenc", "qsv", "amf"));

        _jpegQualityLabel = MakeLabel("");
        _bufferJpegQuality = new TrackBar { Minimum = 10, Maximum = 95, TickFrequency = 5, Dock = DockStyle.Fill };
        _bufferJpegQuality.ValueChanged += (_, _) => _jpegQualityLabel.Text = $"Buffer quality: {_bufferJpegQuality.Value}";
        AddRow(outer, r++, _jpegQualityLabel, _bufferJpegQuality);

        var btnRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0),
        };
        var btnSave   = new Button { Text = "Save",   DialogResult = DialogResult.OK,     Width = 80 };
        var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 80 };
        btnSave.Click += OnSave;
        btnRow.Controls.AddRange(new Control[] { btnCancel, btnSave });
        outer.Controls.Add(btnRow);
        outer.SetColumnSpan(btnRow, 2);

        // ── Description panel ───────────────────────────────────────────────────
        var descPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(255, 255, 225),
            Padding = new Padding(10, 6, 10, 6),
        };
        var topBorder = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = SystemColors.ControlDark };
        _descLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.TopLeft,
            ForeColor = SystemColors.InfoText,
            Text = "Hover over a setting to see its description.",
        };
        descPanel.Controls.Add(_descLabel);
        descPanel.Controls.Add(topBorder);

        root.Controls.Add(outer,      0, 0);
        root.Controls.Add(descPanel,  0, 1);
        Controls.Add(root);
        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private Control BuildFolderRow()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Height = 26 };
        var btn = new Button { Text = "…", Width = 28, Dock = DockStyle.Right };
        _outputDir = new TextBox { Dock = DockStyle.Fill };
        btn.Click += (_, _) =>
        {
            using var dlg = new FolderBrowserDialog { SelectedPath = _outputDir.Text };
            if (dlg.ShowDialog() == DialogResult.OK)
                _outputDir.Text = dlg.SelectedPath;
        };
        panel.Controls.Add(_outputDir);
        panel.Controls.Add(btn);
        return panel;
    }

    private void OnMonitorChanged(object? sender, EventArgs e)
    {
        if (_monitor.SelectedIndex < 0 || _monitor.SelectedIndex >= _screens.Count) return;
        var s = _screens[_monitor.SelectedIndex];
        _width.Value  = Clamp(s.Bounds.Width,  320, 3840);
        _height.Value = Clamp(s.Bounds.Height, 240, 2160);
    }

    private void WireDescriptions()
    {
        Describe(_hotkeyBox,
            "The key combination that triggers saving the replay. Click the box and press your desired combo (e.g. Ctrl+Shift+F9). Press Escape to cancel.");

        Describe(_monitor,
            "Which monitor to capture. Selecting a monitor automatically fills in the width and height to match its native resolution, but you can adjust them freely.");

        Describe(_bufferMinutes,
            "How many minutes of footage to keep in memory at all times. Longer buffers let you save more of the past but use more RAM (~300 MB per 5 min at default settings).");

        Describe(_fps,
            "Frames captured per second. Higher values produce smoother video but increase CPU usage and RAM. 30 fps is a good balance; 60 fps is noticeably smoother for fast action.");

        Describe(_width,
            "Width of the saved video in pixels. 1280 (720p) is a good default. Use 1920 for full HD — this increases encoding time and file size.");

        Describe(_height,
            "Height of the saved video in pixels. 720 gives 1280×720 (HD). Use 1080 for 1920×1080 (Full HD). Should match your width choice.");

        Describe(_outputDir,
            "Folder where saved replay videos are written. Each file is named replay_YYYYMMDD_HHMMSS.mp4. Click … to browse.");

        Describe(_codec,
            "Video compression format. h264 is universally compatible with all players and fast to encode. h265 (HEVC) produces ~40% smaller files but encodes slower and requires a capable player.");

        Describe(_preset,
            "Encoding speed vs. file size tradeoff. 'ultrafast' saves in seconds with larger files; 'slow' takes longer but produces smaller files. For a replay buffer, 'ultrafast' or 'superfast' is recommended.");

        Describe(_crf,
            "Constant Rate Factor — controls quality vs. file size. Lower = better quality, larger file. 18–28 is a typical range; 23 is the default. Has no effect when using hardware acceleration (nvenc/qsv/amf).");

        Describe(_hwAccel,
            "Use your GPU to encode video, which is much faster than CPU. 'nvenc' = NVIDIA, 'qsv' = Intel, 'amf' = AMD. If your GPU doesn't support it, encoding automatically falls back to CPU.");

        Describe(_bufferJpegQuality,
            "JPEG compression quality for frames held in RAM. Higher = better image fidelity but more memory used. Lower = smaller RAM footprint but slight quality loss before encoding. 55 is a good balance.");
    }

    private void Describe(Control ctrl, string text)
    {
        ctrl.MouseEnter += (_, _) => _descLabel.Text = text;
        ctrl.Enter      += (_, _) => _descLabel.Text = text;
        ctrl.MouseLeave += (_, _) => { if (!ctrl.Focused) _descLabel.Text = "Hover over a setting to see its description."; };
        ctrl.Leave      += (_, _) => _descLabel.Text = "Hover over a setting to see its description.";
    }

    private void LoadValues()
    {
        _hotkeyBox.Hotkey = Settings.Hotkey;

        // Select the saved monitor, fall back to primary
        var monitorIdx = _screens.FindIndex(s => s.DeviceName == Settings.MonitorDeviceName);
        if (monitorIdx < 0) monitorIdx = _screens.FindIndex(s => s.Primary);
        _monitor.SelectedIndex = Math.Max(0, monitorIdx);

        _bufferMinutes.Value = Clamp(Settings.BufferMinutes, 1, 60);
        _fps.Value = Clamp(Settings.Fps, 1, 60);
        _width.Value = Clamp(Settings.Width, 320, 3840);
        _height.Value = Clamp(Settings.Height, 240, 2160);
        _outputDir.Text = Settings.OutputDirectory;
        _codec.SelectedItem = Settings.Codec;
        if (_codec.SelectedIndex < 0) _codec.SelectedIndex = 0;
        _preset.SelectedItem = Settings.Preset;
        if (_preset.SelectedIndex < 0) _preset.SelectedIndex = 0;
        _crf.Value = Clamp(Settings.Crf, 0, 51);
        _hwAccel.SelectedItem = Settings.HwAccel;
        if (_hwAccel.SelectedIndex < 0) _hwAccel.SelectedIndex = 0;
        _bufferJpegQuality.Value = Clamp(Settings.BufferJpegQuality, 10, 95);
    }

    private void OnSave(object? sender, EventArgs e)
    {
        Settings.Hotkey = _hotkeyBox.Hotkey;
        Settings.MonitorDeviceName = _monitor.SelectedIndex >= 0 && _monitor.SelectedIndex < _screens.Count
            ? _screens[_monitor.SelectedIndex].DeviceName
            : "";
        Settings.BufferMinutes = (int)_bufferMinutes.Value;
        Settings.Fps = (int)_fps.Value;
        Settings.Width = (int)_width.Value;
        Settings.Height = (int)_height.Value;
        Settings.OutputDirectory = _outputDir.Text.Trim();
        Settings.Codec = _codec.SelectedItem?.ToString() ?? "h264";
        Settings.Preset = _preset.SelectedItem?.ToString() ?? "ultrafast";
        Settings.Crf = _crf.Value;
        Settings.HwAccel = _hwAccel.SelectedItem?.ToString() ?? "none";
        Settings.BufferJpegQuality = _bufferJpegQuality.Value;
    }

    // Helpers
    private static void AddRow(TableLayoutPanel layout, int row, string label, Control ctrl)
        => AddRow(layout, row, MakeLabel(label), ctrl);

    private static void AddRow(TableLayoutPanel layout, int row, Control label, Control ctrl)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        label.Margin = new Padding(0, 6, 8, 0);
        ctrl.Margin = new Padding(0, 4, 0, 0);
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(ctrl, 1, row);
    }

    private static Label MakeLabel(string text) =>
        new() { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

    private static NumericUpDown Spinner(int min, int max, int increment = 1) =>
        new() { Minimum = min, Maximum = max, Increment = increment, Dock = DockStyle.Fill };

    private static ComboBox Combo(params string[] items)
    {
        var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
        cb.Items.AddRange(items);
        return cb;
    }

    private static int Clamp(int value, int min, int max) =>
        Math.Max(min, Math.Min(max, value));
}
