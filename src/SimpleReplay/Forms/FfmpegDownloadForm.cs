using SimpleReplay.Services;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleReplay.Forms;

public sealed class FfmpegDownloadForm : Form
{
    private readonly Label _statusLabel;
    private readonly ProgressBar _progressBar;
    private readonly Button _cancelButton;
    private readonly CancellationTokenSource _cts = new();

    public FfmpegDownloadForm()
    {
        Text = "Simple Replay — First-time Setup";
        Size = new Size(420, 160);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 3,
            ColumnCount = 1,
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _statusLabel = new Label
        {
            Text = "Preparing download…",
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
        };

        _progressBar = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Minimum = 0,
            Maximum = 100,
            Margin = new Padding(0, 0, 0, 8),
        };

        _cancelButton = new Button { Text = "Cancel", Width = 80, Anchor = AnchorStyles.Right };
        _cancelButton.Click += (_, _) => _cts.Cancel();

        panel.Controls.Add(_statusLabel, 0, 0);
        panel.Controls.Add(_progressBar, 0, 1);
        panel.Controls.Add(_cancelButton, 0, 2);

        Controls.Add(panel);
        CancelButton = _cancelButton;
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);

        var bootstrapper = new FfmpegBootstrapper();
        var progress = new Progress<(string Status, int Percent)>(report =>
        {
            _statusLabel.Text = report.Status;
            _progressBar.Value = report.Percent;
        });

        try
        {
            await bootstrapper.DownloadAsync(progress, _cts.Token);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (OperationCanceledException)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to download ffmpeg:\n\n{ex.Message}\n\nCheck your internet connection and try again.",
                "Simple Replay", MessageBoxButtons.OK, MessageBoxIcon.Error);
            DialogResult = DialogResult.Abort;
            Close();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _cts.Dispose();
        base.Dispose(disposing);
    }
}
