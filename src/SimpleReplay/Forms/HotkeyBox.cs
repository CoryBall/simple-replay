using System.Windows.Forms;

namespace SimpleReplay.Forms;

internal sealed class HotkeyBox : TextBox
{
    private string _hotkey = string.Empty;
    private bool _keyPressed;

    public string Hotkey
    {
        get => _hotkey;
        set
        {
            _hotkey = value;
            Text = value;
            ForeColor = SystemColors.WindowText;
        }
    }

    public HotkeyBox()
    {
        ReadOnly = true;
        BackColor = SystemColors.Window;
        Cursor = Cursors.IBeam;
    }

    protected override void OnEnter(EventArgs e)
    {
        base.OnEnter(e);
        _keyPressed = false;
        Text = "Press a key combo…";
        ForeColor = SystemColors.GrayText;
    }

    protected override void OnLeave(EventArgs e)
    {
        base.OnLeave(e);
        if (!_keyPressed)
        {
            Text = _hotkey;
            ForeColor = SystemColors.WindowText;
        }
    }

    protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
    {
        // Mark everything except Tab as input so KeyDown fires for it
        if (e.KeyCode != Keys.Tab)
            e.IsInputKey = true;
        base.OnPreviewKeyDown(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.SuppressKeyPress = true;
        e.Handled = true;

        var key = e.KeyCode;

        // Escape cancels — restore previous value
        if (key == Keys.Escape)
        {
            _keyPressed = false;
            Text = _hotkey;
            ForeColor = SystemColors.WindowText;
            return;
        }

        // Ignore bare modifier key presses
        if (key is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
                or Keys.ShiftKey  or Keys.LShiftKey  or Keys.RShiftKey
                or Keys.Menu      or Keys.LMenu       or Keys.RMenu)
            return;

        var parts = new List<string>();
        if (e.Control) parts.Add("Ctrl");
        if (e.Shift)   parts.Add("Shift");
        if (e.Alt)     parts.Add("Alt");
        parts.Add(key.ToString());

        _hotkey = string.Join("+", parts);
        _keyPressed = true;
        Text = _hotkey;
        ForeColor = SystemColors.WindowText;
    }
}
