using System.Windows.Forms;
using Xunit;

namespace MouseClicker.Tests
{
    public class HotkeyCaptureTests
    {
        [Fact]
        public void TryGetCapturedKey_AcceptsEverySupportedKey()
        {
            Assert.NotEmpty(GlobalHotkeyManager.SupportedKeys);
            foreach (Keys supported in GlobalHotkeyManager.SupportedKeys)
            {
                Assert.True(HotkeySettingsForm.TryGetCapturedKey(supported, out Keys captured), $"{supported} should be accepted.");
                Assert.Equal(supported, captured);
            }
        }

        [Theory]
        [InlineData(Keys.Control)]
        [InlineData(Keys.Alt)]
        [InlineData(Keys.Shift)]
        [InlineData(Keys.Control | Keys.Alt)]
        [InlineData(Keys.Control | Keys.Shift)]
        [InlineData(Keys.Control | Keys.Alt | Keys.Shift)]
        public void TryGetCapturedKey_RejectsModifiedKeys(Keys modifiers)
        {
            foreach (Keys supported in GlobalHotkeyManager.SupportedKeys)
                Assert.False(HotkeySettingsForm.TryGetCapturedKey(supported | modifiers, out _), $"{supported | modifiers} should be rejected.");
        }

        [Theory]
        [InlineData(Keys.None)]
        [InlineData(Keys.A)]
        [InlineData(Keys.Space)]
        [InlineData(Keys.Escape)]
        [InlineData(Keys.Enter)]
        [InlineData(Keys.Tab)]
        [InlineData(Keys.F1)]
        [InlineData(Keys.F5)]
        [InlineData(Keys.LWin)]
        [InlineData(Keys.RWin)]
        [InlineData(Keys.D0)]
        public void TryGetCapturedKey_RejectsUnsupportedKeys(Keys keyData)
        {
            Assert.False(HotkeySettingsForm.TryGetCapturedKey(keyData, out _));
        }
    }
}
