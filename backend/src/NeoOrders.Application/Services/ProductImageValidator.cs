namespace NeoOrders.Application.Services;

/// <summary>Validación de imágenes de producto: extensión, content-type, tamaño y firma del archivo.</summary>
public static class ProductImageValidator
{
    public const long MaxBytes = 2L * 1024 * 1024;

    private static readonly Dictionary<string, string> FormatosPorExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "jpg",
        [".jpeg"] = "jpg",
        [".png"] = "png",
        [".webp"] = "webp",
    };

    private static readonly Dictionary<string, string> FormatosPorContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = "jpg",
        ["image/jpg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

    /// <summary>Devuelve null cuando el archivo es válido; si no, el mensaje para el cliente.</summary>
    public static string? Validate(string fileName, string? contentType, byte[] content)
    {
        if (content.Length == 0) return "Debe seleccionar una imagen.";
        if (content.Length > MaxBytes) return "La imagen no puede superar los 2 MB.";

        var extension = Path.GetExtension(fileName);
        if (!FormatosPorExtension.TryGetValue(extension, out var formatoExtension))
            return "Formato no admitido. Use JPG, PNG o WebP.";

        var normalizedContentType = (contentType ?? string.Empty).Trim();
        if (!FormatosPorContentType.TryGetValue(normalizedContentType, out var formatoContentType))
            return "Tipo de archivo no admitido. Use JPG, PNG o WebP.";

        var formatoReal = DetectarFormato(content);
        if (formatoReal is null)
            return "El archivo no es una imagen JPG, PNG ni WebP válida.";

        if (formatoReal != formatoExtension || formatoReal != formatoContentType)
            return "La extensión, el tipo y el contenido de la imagen no coinciden.";

        return null;
    }

    private static string? DetectarFormato(byte[] b)
    {
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "jpg";

        if (b.Length >= 8 &&
            b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 &&
            b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A) return "png";

        if (b.Length >= 12 &&
            b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 &&
            b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return "webp";

        return null;
    }
}
