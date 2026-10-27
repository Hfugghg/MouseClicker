using System.Drawing;
using MouseClicker;
using MouseClicker.Tests.TestDoubles;
using Xunit;

namespace MouseClicker.Tests
{
    public class MouseControllerRightClickTests
    {
        [Fact]
        public void RightButton_IsForwardedInSoftwareMode()
        {
            var input = new FakeMouseInput();
            new MouseController(input).PerformRandomClickInArea(new Rectangle(10, 20, 30, 40), false, true);
            Assert.Single(input.SoftwareClickButtons);
            Assert.True(input.SoftwareClickButtons[0]);
            Assert.Empty(input.HardwareClickButtons);
        }

        [Fact]
        public void RightButton_IsForwardedInSendInputMode()
        {
            var input = new FakeMouseInput();
            new MouseController(input).PerformRandomClickInArea(new Rectangle(10, 20, 30, 40), true, true);
            Assert.Single(input.HardwareClickButtons);
            Assert.True(input.HardwareClickButtons[0]);
            Assert.Empty(input.SoftwareClickButtons);
        }

        [Fact]
        public void DefaultButton_RemainsLeftClick()
        {
            var input = new FakeMouseInput();
            new MouseController(input).PerformRandomClickInArea(new Rectangle(10, 20, 30, 40));
            Assert.Single(input.SoftwareClickButtons);
            Assert.False(input.SoftwareClickButtons[0]);
        }
    }
}
