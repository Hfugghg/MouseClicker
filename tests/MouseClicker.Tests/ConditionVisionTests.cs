using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using Xunit;

namespace MouseClicker.Tests;

public class ConditionVisionTests
{
    private static Bitmap Pattern(int seed = 1)
    {
        var bitmap = new Bitmap(16, 16);
        var random = new Random(seed);
        for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
                bitmap.SetPixel(x, y, Color.FromArgb(random.Next(256), random.Next(256), random.Next(256)));
        return bitmap;
    }

    private static ClickCondition Condition(Bitmap image)
    {
        using var stream = new MemoryStream();
        image.Save(stream, ImageFormat.Png);
        return new ClickCondition { DetectionArea = new Rectangle(-100, 20, 64, 64), TemplateImageBase64 = Convert.ToBase64String(stream.ToArray()) };
    }

    [Fact]
    public void NativeMatching_FindsTemplateAtIndependentLocation()
    {
        using var template = Pattern();
        using var source = new Bitmap(64, 64);
        using (var graphics = Graphics.FromImage(source)) { graphics.Clear(Color.Black); graphics.DrawImageUnscaled(template, 31, 19); }
        var result = OpenCvConditionDetector.Match(source, template);
        Assert.True(result.Score > .99);
        Assert.Equal(new Rectangle(31, 19, 16, 16), result.MatchBounds);
    }

    [Fact]
    public void NativeMatching_ChoosesGenuineTargetOverHigherCorrelationColourDistractor()
    {
        using var template = new Bitmap(16, 16);
        using var actual = new Bitmap(16, 16);
        using var source = new Bitmap(40, 16);
        var random = new Random(123);
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
        {
            var pixel = Color.FromArgb(random.Next(20, 100), random.Next(20, 100), random.Next(20, 100));
            template.SetPixel(x, y, pixel);
            actual.SetPixel(x, y, pixel);
            source.SetPixel(x, y, Color.FromArgb(pixel.R + 130, pixel.G + 130, pixel.B + 130));
        }
        actual.SetPixel(4, 4, Color.FromArgb(80, 80, 80));
        using (var graphics = Graphics.FromImage(source)) graphics.DrawImageUnscaled(actual, 24, 0);
        var result = OpenCvConditionDetector.Match(source, template);
        Assert.True(result.Score > .99);
        Assert.Equal(new Rectangle(24, 0, 16, 16), result.MatchBounds);
    }

    [Fact]
    public void NativeMatching_RejectsDifferentPicture()
    {
        using var template = Pattern();
        using var other = Pattern(87);
        Assert.True(OpenCvConditionDetector.Match(other, template).Score < .9);
    }

    [Fact]
    public void NativeMatching_DoesNotIgnoreColours()
    {
        using var template = Pattern();
        using var other = new Bitmap(16, 16);
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
        {
            var pixel = template.GetPixel(x, y);
            other.SetPixel(x, y, Color.FromArgb(pixel.B, pixel.R, pixel.G));
        }
        Assert.True(OpenCvConditionDetector.Match(other, template).Score < .9);
    }

    [Fact]
    public void NativeMatching_RejectsPureColourAndOversizedTemplate()
    {
        using var flat = new Bitmap(16, 16);
        using var template = Pattern();
        using var small = new Bitmap(8, 8);
        Assert.Throws<InvalidDataException>(() => OpenCvConditionDetector.Match(template, flat));
        Assert.Throws<InvalidDataException>(() => OpenCvConditionDetector.Match(small, template));
    }

    [Fact]
    public void Condition_RoundTripsAndOldConfigurationRemainsUnconditional()
    {
        using var template = Pattern();
        var area = new ClickArea { Area = new Rectangle(500, 300, 10, 10), Condition = Condition(template) };
        var restored = JsonSerializer.Deserialize<ClickArea>(JsonSerializer.Serialize(area))!;
        restored.Condition!.Validate();
        Assert.Equal(area.Area, restored.Area);
        Assert.Equal(area.Condition!.DetectionArea, restored.Condition.DetectionArea);
        Assert.Equal(area.Condition.TemplateImageBase64, restored.Condition.TemplateImageBase64);
        Assert.Null(JsonSerializer.Deserialize<ClickArea>("{\"Area\":{\"X\":1,\"Y\":2,\"Width\":3,\"Height\":4}}")!.Condition);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public void Condition_RejectsInvalidSettings(int kind)
    {
        using var template = Pattern();
        var condition = Condition(template);
        switch (kind)
        {
            case 0: condition.Threshold = double.NaN; break;
            case 1: condition.PollIntervalMs = 0; break;
            case 2: condition.TimeoutMs = -1; break;
            case 3: condition.TemplateImageBase64 = "not a png"; break;
            case 4: condition.TriggerMode = (ConditionTriggerMode)99; break;
            case 5: condition.DetectionArea = new Rectangle(0, 0, 8, 8); break;
        }
        Assert.Throws<InvalidDataException>(condition.Validate);
    }

    [Fact]
    public void Condition_RejectsTruncatedPng()
    {
        using var template = Pattern();
        var condition = Condition(template);
        condition.TemplateImageBase64 = Convert.ToBase64String(Convert.FromBase64String(condition.TemplateImageBase64)[..33]);
        Assert.Throws<InvalidDataException>(condition.Validate);
    }

    [Fact]
    public void Capture_RejectsMonitorGapsAndAllowsNegativeCoordinates()
    {
        Rectangle[] screens = [new(-100, 0, 100, 100), new(20, 0, 100, 100)];
        ScreenCapture.ValidateArea(new Rectangle(-80, 10, 40, 40), screens);
        Assert.Throws<InvalidDataException>(() => ScreenCapture.ValidateArea(new Rectangle(-10, 0, 40, 40), screens));
        Assert.Throws<InvalidDataException>(() => ScreenCapture.ValidateArea(new Rectangle(int.MaxValue, 0, 10, 10), screens));
    }
}
