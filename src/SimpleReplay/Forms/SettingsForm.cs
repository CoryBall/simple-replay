using SimpleReplay.Models;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleReplay.Forms;

public sealed class SettingsForm : Form
{
    public AppSettings Settings { get; private set; }

    private TextBox _hotkeyBox = null!;
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

    public SettingsForm(AppSettings current)
    {
        // Clone so Cancel truly discards
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
    }

    private void BuildUI()
    {
        Text = "Simple Replay — Settings";
        Size = new Size(460, 580);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

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
            _hotkeyBox = new TextBox { Dock = DockStyle.Fill });

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

        // Button row
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

        Controls.Add(outer);
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

    private void LoadValues()
    {
        _hotkeyBox.Text = Settings.Hotkey;
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
        Settings.Hotkey = _hotkeyBox.Text.Trim();
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
