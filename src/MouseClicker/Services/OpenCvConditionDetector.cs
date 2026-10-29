using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenCvSharp;
using Rectangle = System.Drawing.Rectangle;

namespace MouseClicker;

internal readonly record struct DetectionResult(double Score, Rectangle MatchBounds, double Scale = 1);

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
        var result = condition.UseMultiScale ? MatchMultiScale(source, template) : Match(source, template);
        var bounds = result.MatchBounds;
        bounds.Offset(condition.DetectionArea.Location);
        return result with { MatchBounds = bounds };
    }

    internal static DetectionResult Match(Bitmap source, Bitmap template)
        => Match(source, template, false);

    internal static DetectionResult Match(Bitmap source, Bitmap template, bool useMultiScale)
    {
        using var sourceMat = ToMat(source);
        using var templateMat = ToMat(template);
        ValidateTemplate(templateMat);
        return useMultiScale ? MatchMultiScale(sourceMat, templateMat) : Match(sourceMat, templateMat);
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

    // Coarse search is bounded in image size and candidate count. Only a few promising
    // locations are checked against original pixels; scaled Mats never survive this call.
    private static DetectionResult MatchMultiScale(Mat source, Mat template)
    {
        if (source.Empty()) throw new InvalidDataException(Localization.Get("ConditionErrorTemplateFit"));
        double upper = Math.Min(3, Math.Min((double)source.Width / template.Width, (double)source.Height / template.Height));
        const double lower = .01;
        var none = new DetectionResult(0, Rectangle.Empty, 1);
        if (upper < lower) return none;
        double q = Math.Min(1, 640d / Math.Max(source.Width, source.Height));
        // Small originals can disappear below the coarse-image detail floor. Preserve
        // their exact-scale search over the whole source, then merge with scaled hits.
        var exactSmall = template.Width <= source.Width && template.Height <= source.Height &&
            Math.Min(template.Width, template.Height) * q < 6 ? Match(source, template) : none;
        using var smallSource = new Mat();
        Cv2.Resize(source, smallSource, new OpenCvSharp.Size(Math.Max(2, (int)Math.Round(source.Width * q)), Math.Max(2, (int)Math.Round(source.Height * q))), interpolation: InterpolationFlags.Area);
        using var baseTemplate = new Mat();
        Cv2.Resize(template, baseTemplate, new OpenCvSharp.Size(Math.Max(2, (int)Math.Floor(template.Width * upper * q)), Math.Max(2, (int)Math.Floor(template.Height * upper * q))), interpolation: upper * q < 1 ? InterpolationFlags.Area : InterpolationFlags.Linear);
        var samples = new List<DetectionResult>();
        var visited = new HashSet<int>();
        void Evaluate(double scale)
        {
            if (scale < lower || scale > upper) return;
            int width = Math.Max(2, (int)Math.Round(template.Width * scale * q));
            int height = Math.Max(2, (int)Math.Round(template.Height * scale * q));
            if (Math.Min(width, height) < 6 || width > smallSource.Width || height > smallSource.Height || !visited.Add(width)) return;
            using var candidate = new Mat();
            Cv2.Resize(baseTemplate, candidate, new OpenCvSharp.Size(width, height), interpolation: InterpolationFlags.Area);
            Cv2.MeanStdDev(candidate, out _, out Scalar deviation);
            if (Math.Max(deviation.Val0, Math.Max(deviation.Val1, deviation.Val2)) < 5) return;
            samples.Add(Match(smallSource, candidate) with { Scale = scale });
        }
        // Logarithmic candidates cover one hundredth through triple size, not just fit-to-area.
        double logStep = Math.Log(upper / lower) / 47;
        for (int i = 0; i < 48; i++) Evaluate(lower * Math.Exp(logStep * i));
        Evaluate(upper);
        Evaluate(1);
        foreach (var seed in samples.OrderByDescending(s => s.Score).Take(4).ToArray())
            for (int i = -4; i <= 4; i++) Evaluate(seed.Scale * Math.Exp(logStep * i / 8));
        if (samples.Count == 0) return exactSmall;
        DetectionResult best = exactSmall;
        var verified = new HashSet<(int Width, int X, int Y)>();
        foreach (var seed in samples.OrderByDescending(s => s.Score).Take(4))
        {
            int center = (int)Math.Round(template.Width * seed.Scale);
            int radius = Math.Min(16, Math.Max(2, (int)Math.Ceiling(2 / q)));
            int stride = Math.Max(1, radius / 3);
            for (int offset = -radius; offset <= radius; offset += stride)
                Verify(center + offset, seed);
            Verify(center, seed);
        }
        if (best.Score > 0)
        {
            // Resolve integer-pixel scale uncertainty left by the coarse image.
            var seed = new DetectionResult(best.Score, new Rectangle((int)Math.Round(best.MatchBounds.X * q), (int)Math.Round(best.MatchBounds.Y * q), 0, 0), best.Scale);
            int width = best.MatchBounds.Width;
            for (int i = -2; i <= 2; i++) Verify(width + i, seed);
        }
        return best;

        void Verify(int width, DetectionResult seed)
        {
            double scale = (double)width / template.Width;
            int height = (int)Math.Round(template.Height * scale);
            if (scale < lower || scale > upper || width < 6 || height < 6 || width > source.Width || height > source.Height) return;
            int x = (int)Math.Round(seed.MatchBounds.X / q), y = (int)Math.Round(seed.MatchBounds.Y / q);
            if (!verified.Add((width, x, y))) return;
            int margin = Math.Max(8, (int)Math.Ceiling(6 / q));
            int left = Math.Max(0, x - margin), top = Math.Max(0, y - margin);
            int right = Math.Min(source.Width, x + width + margin), bottom = Math.Min(source.Height, y + height + margin);
            if (right - left < width || bottom - top < height) return;
            using var candidate = new Mat();
            Cv2.Resize(template, candidate, new OpenCvSharp.Size(width, height), interpolation: scale < 1 ? InterpolationFlags.Area : InterpolationFlags.Linear);
            Cv2.MeanStdDev(candidate, out _, out Scalar deviation);
            if (Math.Max(deviation.Val0, Math.Max(deviation.Val1, deviation.Val2)) < 5) return;
            using var region = new Mat(source, new Rect(left, top, right - left, bottom - top));
            var result = Match(region, candidate);
            var bounds = result.MatchBounds;
            bounds.Offset(left, top);
            if (result.Score > best.Score) best = result with { MatchBounds = bounds, Scale = scale };
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var template in _templates.Values) template.Dispose();
        _templates.Clear();
    }
}
