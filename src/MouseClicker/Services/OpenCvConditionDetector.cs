using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenCvSharp;
using Rectangle = System.Drawing.Rectangle;

namespace MouseClicker;

internal readonly record struct DetectionResult(double Score, Rectangle MatchBounds);

internal interface IConditionDetector : IDisposable
{
    DetectionResult Detect(ClickCondition condition);
}

internal sealed class OpenCvConditionDetector : IConditionDetector
{
    private readonly Dictionary<ClickCondition, Mat> _templates = new();
    private bool _disposed;

    public DetectionResult Detect(ClickCondition condition)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_templates.TryGetValue(condition, out var template))
        {
            condition.Validate();
            template = Cv2.ImDecode(condition.GetTemplateBytes(), ImreadModes.Color);
            try { ValidateTemplate(template); }
            catch { template.Dispose(); throw; }
            _templates.Add(condition, template);
        }
        using var screenshot = ScreenCapture.Capture(condition.DetectionArea);
        using var source = ToMat(screenshot);
        var result = Match(source, template);
        var bounds = result.MatchBounds;
        bounds.Offset(condition.DetectionArea.Location);
        return result with { MatchBounds = bounds };
    }

    internal static DetectionResult Match(Bitmap source, Bitmap template)
    {
        using var sourceMat = ToMat(source);
        using var templateMat = ToMat(template);
        ValidateTemplate(templateMat);
        return Match(sourceMat, templateMat);
    }

    private static Mat ToMat(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Bmp);
        return Cv2.ImDecode(stream.ToArray(), ImreadModes.Color);
    }

    private static void ValidateTemplate(Mat template)
    {
        if (template.Empty() || template.Width < 2 || template.Height < 2 ||
            (long)template.Width * template.Height > ClickCondition.MaximumTemplatePixels)
            throw new InvalidDataException(Localization.Get("ConditionErrorDimensions"));
        Cv2.MeanStdDev(template, out _, out Scalar deviation);
        if (Math.Max(deviation.Val0, Math.Max(deviation.Val1, deviation.Val2)) < 5)
            throw new InvalidDataException(Localization.Get("ConditionErrorLowDetail"));
    }

    private static DetectionResult Match(Mat source, Mat template)
    {
        if (source.Empty() || template.Width > source.Width || template.Height > source.Height)
            throw new InvalidDataException(Localization.Get("ConditionErrorTemplateFit"));
        using var correlations = new Mat();
        Cv2.MatchTemplate(source, template, correlations, TemplateMatchModes.CCoeffNormed);
        // Score every candidate using shape and colour together, before choosing a location.
        // A colour-shifted distractor may have higher correlation than the genuine target.
        using var difference = new Mat();
        using var colourAgreement = new Mat();
        using var combined = new Mat();
        Cv2.MatchTemplate(source, template, difference, TemplateMatchModes.SqDiffNormed);
        Cv2.Subtract(Scalar.All(1), difference, colourAgreement);
        Cv2.Min(correlations, colourAgreement, combined);
        if (!Cv2.CheckRange(combined)) throw new InvalidDataException(Localization.Get("ConditionErrorScore"));
        Cv2.MinMaxLoc(combined, out _, out double score, out _, out OpenCvSharp.Point location);
        if (!double.IsFinite(score)) throw new InvalidDataException(Localization.Get("ConditionErrorScore"));
        return new DetectionResult(Math.Clamp(score, 0, 1), new Rectangle(location.X, location.Y, template.Width, template.Height));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var template in _templates.Values) template.Dispose();
        _templates.Clear();
    }
}
