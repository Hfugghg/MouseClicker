using Xunit;

namespace MouseClicker.Tests
{
    public class ClickTimingTests
    {
        [Theory]
        [InlineData(100, -500, 400)]
        [InlineData(100, 500, 600)]
        [InlineData(100, -100, 10)]
        [InlineData(100, -101, 10)]
        [InlineData(100, -109, 10)]
        [InlineData(100, -99, 10)]
        [InlineData(100, -91, 10)]
        [InlineData(100, -110, 10)]
        [InlineData(100, -90, 10)]
        [InlineData(100, -111, 11)]
        [InlineData(100, -89, 11)]
        [InlineData(10, -10000, 9990)]
        [InlineData(10, 10000, 10010)]
        [InlineData(600000, -10000, 590000)]
        [InlineData(600000, 10000, 610000)]
        [InlineData(100, 0, 100)]
        [InlineData(10, 0, 10)]
        public void CalculateClickInterval_ReflectsNegativeTotalsAndKeepsMinimum(int baseInterval, int randomJitter, int expected)
        {
            Assert.Equal(expected, Form1.CalculateClickInterval(baseInterval, randomJitter));
        }
    }
}
