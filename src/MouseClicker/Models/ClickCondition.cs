using System.Drawing;
using System.IO;
using System.Buffers.Binary;

namespace MouseClicker;

public enum ConditionTriggerMode { OncePerAppearance, WhileVisible }

public sealed class ClickCondition
{
    public Rectangle DetectionArea { get; set; }
    public string TemplateImageBase64 { get; set; } = string.Empty;
    public double Threshold { get; set; } = .90;
    public int PollIntervalMs { get; set; } = 200;
    public int TimeoutMs { get; set; } = 60000;
    public ConditionTriggerMode TriggerMode { get; set; }
    public bool UseMultiScale { get; set; }
    public const int MaximumTemplateBytes = 32 * 1024 * 1024;
    public const int MaximumTemplatePixels = 32 * 1024 * 1024;

    public void Validate()
    {
        if (DetectionArea.Width <= 0 || DetectionArea.Height <= 0 ||
            (long)DetectionArea.X + DetectionArea.Width > int.MaxValue ||
            (long)DetectionArea.Y + DetectionArea.Height > int.MaxValue ||
            (long)DetectionArea.Width * DetectionArea.Height > 32L * 1024 * 1024)
            throw new InvalidDataException(Localization.Get("ConditionErrorDetectionArea"));
        if (!double.IsFinite(Threshold) || Threshold < .1 || Threshold > 1 ||
            PollIntervalMs < 50 || PollIntervalMs > 60000 || TimeoutMs < 0 || TimeoutMs > 86400000 ||
            !Enum.IsDefined(TriggerMode))
            throw new InvalidDataException(Localization.Get("ConditionErrorSettings"));
        var bytes = GetTemplateBytes();
        try
        {
            using var stream = new MemoryStream(bytes, false);
            using var image = Image.FromStream(stream, false, true);
            using var decoded = new Bitmap(image);
        }
        catch (Exception ex) when (ex is ArgumentException or System.Runtime.InteropServices.ExternalException or OutOfMemoryException)
        { throw new InvalidDataException(Localization.Get("ConditionErrorDecode"), ex); }
    }

    internal byte[] GetTemplateBytes()
    {
        if (string.IsNullOrWhiteSpace(TemplateImageBase64))
            throw new InvalidDataException(Localization.Get("ConditionErrorTemplateEmpty"));
        if (TemplateImageBase64.Length > (MaximumTemplateBytes + 2L) / 3 * 4)
            throw new InvalidDataException(Localization.Get("ConditionErrorTemplateSize"));
        byte[] bytes;
        try { bytes = Convert.FromBase64String(TemplateImageBase64); }
        catch (FormatException ex) { throw new InvalidDataException(Localization.Get("ConditionErrorEncoding"), ex); }
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        if (bytes.Length > MaximumTemplateBytes)
            throw new InvalidDataException(Localization.Get("ConditionErrorTemplateSize"));
        if (bytes.Length < 33 || !bytes.AsSpan(0, 8).SequenceEqual(signature) ||
            !bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8))
            throw new InvalidDataException(Localization.Get("ConditionErrorPng"));
        uint width = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4));
        uint height = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4));
        if (width < 2 || height < 2 ||
            (ulong)width * height > MaximumTemplatePixels)
            throw new InvalidDataException(Localization.Get("ConditionErrorDimensions"));
        if (!UseMultiScale && (width > DetectionArea.Width || height > DetectionArea.Height))
            throw new InvalidDataException(string.Format(Localization.Get("ConditionTemplateFit"), width, height, DetectionArea.Width, DetectionArea.Height));
        return bytes;
    }
}
