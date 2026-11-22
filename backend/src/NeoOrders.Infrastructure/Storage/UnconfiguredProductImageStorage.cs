using NeoOrders.Application.Contracts.Productos;

namespace NeoOrders.Infrastructure.Storage;

/// <summary>
/// Se registra cuando faltan credenciales de Cloudinary. La aplicación arranca igual y el CRUD de productos
/// sigue funcionando; sólo la carga de imágenes devuelve un error claro.
/// </summary>
public sealed class UnconfiguredProductImageStorage : IProductImageStorage
{
    public bool IsConfigured => false;

    public Task<StoredImage> UploadAsync(byte[] content, string fileName, CancellationToken ct = default) =>
        throw new InvalidOperationException(
            "Almacenamiento de imágenes no configurado. Definí Cloudinary__CloudName, Cloudinary__ApiKey y Cloudinary__ApiSecret.");

    public Task DeleteAsync(string? publicId, CancellationToken ct = default) => Task.CompletedTask;
}
