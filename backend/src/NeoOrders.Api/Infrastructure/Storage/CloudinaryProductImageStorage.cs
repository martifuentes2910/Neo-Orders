using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using OrdersApi.Application.Contracts.Productos;

namespace OrdersApi.Infrastructure.Storage;

/// <summary>Sube y borra imágenes en Cloudinary. Las credenciales se inyectan desde configuración/secretos.</summary>
public sealed class CloudinaryProductImageStorage : IProductImageStorage
{
    private readonly Cloudinary _cloudinary;
    private readonly string _folder;
    private readonly ILogger<CloudinaryProductImageStorage> _logger;

    public CloudinaryProductImageStorage(
        string cloudName,
        string apiKey,
        string apiSecret,
        string folder,
        ILogger<CloudinaryProductImageStorage> logger)
    {
        _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        _folder = folder.Trim('/');
        _logger = logger;
    }

    public bool IsConfigured => true;

    public async Task<StoredImage> UploadAsync(byte[] content, string fileName, CancellationToken ct = default)
    {
        await using var stream = new MemoryStream(content, writable: false);
        var publicId = string.IsNullOrEmpty(_folder)
            ? Guid.NewGuid().ToString("N")
            : $"{_folder}/{Guid.NewGuid():N}";

        var result = await _cloudinary.UploadAsync(new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            PublicId = publicId,
            Overwrite = true,
            Transformation = new Transformation()
                .Width(1200).Height(1200).Crop("limit")
                .FetchFormat("auto").Quality("auto"),
        }, ct);

        if (result.Error is not null || result.SecureUrl is null)
            throw new InvalidOperationException(result.Error?.Message ?? "Cloudinary no devolvió una URL segura.");

        return new StoredImage(result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task DeleteAsync(string? publicId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(publicId)) return;
        try
        {
            var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
            if (result.Error is not null)
                _logger.LogWarning("No se pudo borrar la imagen {PublicId} de Cloudinary: {Error}", publicId, result.Error.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo borrar la imagen {PublicId} de Cloudinary.", publicId);
        }
    }
}
