using System;
using System.Drawing;
using Xunit;
using MouseClicker;
using MouseClicker.Tests.TestDoubles;

namespace MouseClicker.Tests
{
    public class MouseControllerTests
    {
        // =========================================================================
        // 测试1：软件模式 — 有效矩形 — 光标移动到区域内并执行软件点击
        // =========================================================================
        [Fact]
        public void PerformRandomClickInArea_SoftwareMode_ValidRectangle_MovesCursorAndClicks()
        {
            var fake = new FakeMouseInput();
            var controller = new MouseController(fake);
            var area = new Rectangle(100, 100, 50, 50);

            controller.PerformRandomClickInArea(area, hardwareMode: false);

            // 验证光标被设置了一次
            Assert.Single(fake.CursorPositions);
            var pos = fake.CursorPositions[0];
            Assert.InRange(pos.X, area.Left, area.Right - 1);  // Random.Next(Left, Right) 不含 Right
            Assert.InRange(pos.Y, area.Top, area.Bottom - 1);

            // 验证软件点击被调用，硬件点击未调用
            Assert.Equal(1, fake.SoftwareClickCount);
            Assert.Equal(0, fake.HardwareClickCount);
        }

        // =========================================================================
        // 测试2：硬件模式 — 有效矩形 — 执行硬件点击
        // =========================================================================
        [Fact]
        public void PerformRandomClickInArea_HardwareMode_ValidRectangle_PerformsHardwareClick()
        {
            var fake = new FakeMouseInput();
            var controller = new MouseController(fake);
            var area = new Rectangle(200, 200, 60, 60);

            controller.PerformRandomClickInArea(area, hardwareMode: true);

            Assert.Single(fake.CursorPositions);
            var pos = fake.CursorPositions[0];
            Assert.InRange(pos.X, area.Left, area.Right - 1);
            Assert.InRange(pos.Y, area.Top, area.Bottom - 1);

            // 验证硬件点击被调用，软件点击未调用
            Assert.Equal(1, fake.HardwareClickCount);
            Assert.Equal(0, fake.SoftwareClickCount);
        }

        // =========================================================================
        // 测试3：宽度为零或负 — 应立即返回，不抛异常，不发起任何调用
        // =========================================================================
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void PerformRandomClickInArea_ZeroOrNegativeWidth_ReturnsEarly(int width)
        {
            var fake = new FakeMouseInput();
            var controller = new MouseController(fake);
            var area = new Rectangle(50, 50, width, 40);

            controller.PerformRandomClickInArea(area);

            // 没有任何调用
            Assert.Empty(fake.CursorPositions);
            Assert.Equal(0, fake.TotalClickCount);
        }

        // =========================================================================
        // 测试4：高度为零或负 — 应立即返回
        // =========================================================================
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void PerformRandomClickInArea_ZeroOrNegativeHeight_ReturnsEarly(int height)
        {
            var fake = new FakeMouseInput();
            var controller = new MouseController(fake);
            var area = new Rectangle(50, 50, 40, height);

            controller.PerformRandomClickInArea(area);

            Assert.Empty(fake.CursorPositions);
            Assert.Equal(0, fake.TotalClickCount);
        }

        // =========================================================================
        // 测试5：软件模式多次点击 — 验证随机性和点击模式
        // =========================================================================
        [Fact]
        public void PerformRandomClickInArea_SoftwareMode_MultipleClicks_ProduceRandomPositions()
        {
            var fake = new FakeMouseInput();
            var controller = new MouseController(fake);
            var area = new Rectangle(150, 150, 80, 80);

            for (int i = 0; i < 10; i++)
                controller.PerformRandomClickInArea(area, hardwareMode: false);

            // 10 次光标移动 + 10 次点击
            Assert.Equal(10, fake.CursorPositions.Count);
            Assert.Equal(10, fake.SoftwareClickCount);
            Assert.Equal(0, fake.HardwareClickCount);

            // 所有坐标都在区域内
            foreach (var (x, y) in fake.CursorPositions)
            {
                Assert.InRange(x, area.Left, area.Right - 1);
                Assert.InRange(y, area.Top, area.Bottom - 1);
            }

            // 至少有一次位置不同（随机性检查）
            bool anyDiff = false;
            for (int i = 1; i < fake.CursorPositions.Count; i++)
            {
                if (fake.CursorPositions[i] != fake.CursorPositions[0])
                {
                    anyDiff = true;
                    break;
                }
            }
            Assert.True(anyDiff, "10 次点击位置应不完全相同");
        }

        // =========================================================================
        // 测试6：硬件模式多次点击 — 验证随机性和点击模式
        // =========================================================================
        [Fact]
        public void PerformRandomClickInArea_HardwareMode_MultipleClicks_ProduceRandomPositions()
        {
            var fake = new FakeMouseInput();
            var controller = new MouseController(fake);
            var area = new Rectangle(250, 250, 70, 70);

            for (int i = 0; i < 10; i++)
                controller.PerformRandomClickInArea(area, hardwareMode: true);

            Assert.Equal(10, fake.CursorPositions.Count);
            Assert.Equal(10, fake.HardwareClickCount);
            Assert.Equal(0, fake.SoftwareClickCount);

            foreach (var (x, y) in fake.CursorPositions)
            {
                Assert.InRange(x, area.Left, area.Right - 1);
                Assert.InRange(y, area.Top, area.Bottom - 1);
            }

            bool anyDiff = false;
            for (int i = 1; i < fake.CursorPositions.Count; i++)
            {
                if (fake.CursorPositions[i] != fake.CursorPositions[0])
                {
                    anyDiff = true;
                    break;
                }
            }
            Assert.True(anyDiff, "10 次硬件点击位置应不完全相同");
        }

        // =========================================================================
        // 测试7：边界 — 宽/高为负值组合
        // =========================================================================
        [Theory]
        [InlineData(-10, -10)]
        [InlineData(-50, 20)]
        [InlineData(30, -70)]
        [InlineData(0, 0)]
        public void PerformRandomClickInArea_NegativeOrZeroDimensions_ReturnsEarly(int width, int height)
        {
            var fake = new FakeMouseInput();
            var controller = new MouseController(fake);
            var area = new Rectangle(50, 50, width, height);

            controller.PerformRandomClickInArea(area);

            Assert.Empty(fake.CursorPositions);
            Assert.Equal(0, fake.TotalClickCount);
        }

        // =========================================================================
        // 测试8：软件模式 — 屏幕原点坐标不抛异常
        // =========================================================================
        [Fact]
        public void PerformRandomClickInArea_SoftwareMode_NearScreenOrigin_DoesNotThrow()
        {
            var fake = new FakeMouseInput();
            var controller = new MouseController(fake);
            var area = new Rectangle(0, 0, 100, 100);

            var ex = Record.Exception(() =>
                controller.PerformRandomClickInArea(area, hardwareMode: false));

            Assert.Null(ex);
            Assert.Single(fake.CursorPositions);
            Assert.Equal(1, fake.SoftwareClickCount);
        }

        // =========================================================================
        // 测试9：硬件模式 — 屏幕原点坐标不抛异常
        // =========================================================================
        [Fact]
        public void PerformRandomClickInArea_HardwareMode_NearScreenOrigin_DoesNotThrow()
        {
            var fake = new FakeMouseInput();
            var controller = new MouseController(fake);
            var area = new Rectangle(0, 0, 100, 100);

            var ex = Record.Exception(() =>
                controller.PerformRandomClickInArea(area, hardwareMode: true));

            Assert.Null(ex);
            Assert.Single(fake.CursorPositions);
            Assert.Equal(1, fake.HardwareClickCount);
        }

        // =========================================================================
        // 测试10：单像素区域 — 两种模式都应到达该唯一坐标
        // =========================================================================
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PerformRandomClickInArea_SinglePixelArea_CursorLandsOnExactPosition(bool hardwareMode)
        {
            var fake = new FakeMouseInput();
            var controller = new MouseController(fake);
            var area = new Rectangle(300, 300, 1, 1);

            controller.PerformRandomClickInArea(area, hardwareMode);

            Assert.Single(fake.CursorPositions);
            Assert.Equal(300, fake.CursorPositions[0].X);
            Assert.Equal(300, fake.CursorPositions[0].Y);
            Assert.Equal(1, fake.TotalClickCount);
        }

        // =========================================================================
        // 测试11：构造函数传入 null 应抛 ArgumentNullException
        // =========================================================================
        [Fact]
        public void Constructor_NullMouseInput_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new MouseController(null!));
        }
    }
}
