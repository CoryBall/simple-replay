using System.Windows.Forms;
using SimpleReplay.Services;

namespace SimpleReplay.Tests;

public sealed class HotkeyServiceTests
{
    [Fact]
    public void ParseHotkey_CtrlShiftF9_ReturnsCorrectKeys()
    {
        var result = HotkeyService.ParseHotkey("Ctrl+Shift+F9");

        result.Should().HaveFlag(Keys.Control);
        result.Should().HaveFlag(Keys.Shift);
        result.Should().HaveFlag(Keys.F9);
    }

    [Fact]
    public void ParseHotkey_AltF10_ReturnsCorrectKeys()
    {
        var result = HotkeyService.ParseHotkey("Alt+F10");

        result.Should().HaveFlag(Keys.Alt);
        result.Should().HaveFlag(Keys.F10);
        result.Should().NotHaveFlag(Keys.Control);
        result.Should().NotHaveFlag(Keys.Shift);
    }

    [Fact]
    public void ParseHotkey_IsCaseInsensitive()
    {
        var upper = HotkeyService.ParseHotkey("Ctrl+Shift+F9");
        var lower = HotkeyService.ParseHotkey("ctrl+shift+f9");

        lower.Should().Be(upper);
    }

    [Fact]
    public void ParseHotkey_SingleKey_ReturnsJustThatKey()
    {
        var result = HotkeyService.ParseHotkey("F5");

        result.Should().HaveFlag(Keys.F5);
        result.Should().NotHaveFlag(Keys.Control);
        result.Should().NotHaveFlag(Keys.Shift);
        result.Should().NotHaveFlag(Keys.Alt);
    }

    [Fact]
    public void ParseHotkey_AllModifiers_ParsesAll()
    {
        var result = HotkeyService.ParseHotkey("Ctrl+Shift+Alt+F1");

        result.Should().HaveFlag(Keys.Control);
        result.Should().HaveFlag(Keys.Shift);
        result.Should().HaveFlag(Keys.Alt);
        result.Should().HaveFlag(Keys.F1);
    }
}
