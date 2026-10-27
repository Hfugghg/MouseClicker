using Xunit;

namespace MouseClicker.Tests
{
    public class Form1HotkeyGuardTests
    {
        [Fact]
        public void IdleFormWithoutModalUi_StartsClicking()
        {
            Assert.Equal(Form1.HotkeyAction.Start,
                Form1.ResolveHotkeyAction(isClicking: false, isSelectingArea: false, modalDepth: 0, hasOpenModalForm: false));
        }

        [Fact]
        public void ActiveSession_StopsEvenWhenEveryModalSourceIsPresent()
        {
            Assert.Equal(Form1.HotkeyAction.Stop,
                Form1.ResolveHotkeyAction(isClicking: true, isSelectingArea: true, modalDepth: 3, hasOpenModalForm: true));
        }

        [Theory]
        [InlineData(true, 0, false)]
        [InlineData(false, 1, false)]
        [InlineData(false, 0, true)]
        [InlineData(false, 2, true)]
        public void IdleFormWithModalUi_DoesNotStartClicking(bool isSelectingArea, int modalDepth, bool hasOpenModalForm)
        {
            Assert.Equal(Form1.HotkeyAction.None,
                Form1.ResolveHotkeyAction(isClicking: false, isSelectingArea, modalDepth, hasOpenModalForm));
        }
    }
}
