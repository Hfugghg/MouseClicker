using System.Windows.Forms;
using Xunit;

namespace MouseClicker.Tests
{
    public class GlobalHotkeyManagerTests
    {
        private static GlobalHotkeyManager.HookKeyDecision Decide(
            Keys key,
            bool controlDown = false,
            bool altDown = false,
            bool shiftDown = false,
            bool windowsDown = false,
            bool isClicking = false)
        {
            return GlobalHotkeyManager.DecideKeyDown(
                key, Keys.F6, controlDown, altDown, shiftDown, windowsDown, isClicking);
        }

        [Fact]
        public void BareHotkey_Invokes()
        {
            Assert.Equal(GlobalHotkeyManager.HookKeyDecision.Invoke, Decide(Keys.F6));
        }

        [Theory]
        [InlineData(true, false, false, false)]
        [InlineData(false, true, false, false)]
        [InlineData(false, false, true, false)]
        [InlineData(false, false, false, true)]
        public void HotkeyWithAnyModifier_PassesThroughWithoutInvoking(bool controlDown, bool altDown, bool shiftDown, bool windowsDown)
        {
            Assert.Equal(GlobalHotkeyManager.HookKeyDecision.PassThrough,
                Decide(Keys.F6, controlDown, altDown, shiftDown, windowsDown));
            Assert.Equal(GlobalHotkeyManager.HookKeyDecision.PassThrough,
                Decide(Keys.F6, controlDown, altDown, shiftDown, windowsDown, isClicking: true));
        }

        [Fact]
        public void BareEscape_WhileClicking_InvokesAndSuppresses()
        {
            Assert.Equal(GlobalHotkeyManager.HookKeyDecision.InvokeAndSuppress,
                Decide(Keys.Escape, isClicking: true));
        }

        [Fact]
        public void BareEscape_WhileIdle_InvokesWithoutSuppressing()
        {
            Assert.Equal(GlobalHotkeyManager.HookKeyDecision.Invoke, Decide(Keys.Escape));
        }

        [Theory]
        [InlineData(true, false, false, false)]
        [InlineData(false, true, false, false)]
        [InlineData(false, false, true, false)]
        [InlineData(false, false, false, true)]
        public void EscapeWithAnyModifier_WhileClicking_PassesThroughAndIsNotSuppressed(bool controlDown, bool altDown, bool shiftDown, bool windowsDown)
        {
            Assert.Equal(GlobalHotkeyManager.HookKeyDecision.PassThrough,
                Decide(Keys.Escape, controlDown, altDown, shiftDown, windowsDown, isClicking: true));
        }

        [Fact]
        public void UnrelatedKey_PassesThrough()
        {
            Assert.Equal(GlobalHotkeyManager.HookKeyDecision.PassThrough, Decide(Keys.A));
            Assert.Equal(GlobalHotkeyManager.HookKeyDecision.PassThrough, Decide(Keys.A, isClicking: true));
        }
    }
}
