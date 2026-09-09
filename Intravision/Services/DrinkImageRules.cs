namespace Intravision.Services;

public static class DrinkImageRules
{
    public const int MaxBytes = 2 * 1024 * 1024;
    public static string Validate(byte[] bytes)
    {
        if (bytes.Length is <= 0 or > MaxBytes)
            throw new ArgumentException("Изображение должно быть размером от 1 байта до 2 МБ.");
        return bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ? ".png"
            : bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255 ? ".jpg"
            : bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8) ? ".webp"
            : throw new ArgumentException("Поддерживаются изображения PNG, JPEG и WebP.");
    }
}
