using NHotkey;
using NHotkey.WindowsForms;
using System.Windows.Forms;

namespace SimpleReplay.Services;

public sealed class HotkeyService : IDisposable
{
    private const string HotkeyName = "SimpleReplaySave";
    private Action? _callback;

    public void Register(string hotkeyString, Action callback)
    {
        _callback = callback;
        Unregister();

        var combined = ParseHotkey(hotkeyString);
        if ((combined & ~(Keys.Control | Keys.Shift | Keys.Alt)) == Keys.None)
            throw new ArgumentException($"Could not parse hotkey: '{hotkeyString}'");

        HotkeyManager.Current.AddOrReplace(HotkeyName, combined, OnHotkey);
    }

    public void Unregister()
    {
        try { HotkeyManager.Current.Remove(HotkeyName); }
        catch { }
    }

    private void OnHotkey(object? sender, HotkeyEventArgs e)
    {
        e.Handled = true;
        _callback?.Invoke();
    }

    internal static Keys ParseHotkey(string hotkey)
    {
        var result = Keys.None;

        foreach (var part in hotkey.Split('+', StringSplitOptions.TrimEntries))
        {
            switch (part.ToLower())
            {
                case "ctrl":  result |= Keys.Control; break;
                case "shift": result |= Keys.Shift;   break;
                case "alt":   result |= Keys.Alt;     break;
                default:
                    if (Enum.TryParse<Keys>(part, ignoreCase: true, out var k))
                        result |= k;
                    break;
            }
        }

        return result;
    }

    public void Dispose() => Unregister();
}
