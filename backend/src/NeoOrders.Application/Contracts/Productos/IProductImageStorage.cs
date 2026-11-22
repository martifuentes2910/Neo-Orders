namespace NeoOrders.Application.Contracts.Productos;

/// <summary>Imagen ya almacenada: URL pública e identificador interno del proveedor.</summary>
public sealed record StoredImage(string Url, string PublicId);

/// <summary>Almacenamiento de imágenes de producto. La infraestructura decide dónde se guardan los archivos.</summary>
public interface IProductImageStorage
{
    /// <summary>False cuando el proveedor no está configurado; la subida debe rechazarse con un mensaje claro.</summary>
    bool IsConfigured { get; }

    Task<StoredImage> UploadAsync(byte[] content, string fileName, CancellationToken ct = default);

    /// <summary>Borrado best-effort: los huérfanos se registran en logs, nunca rompen la operación principal.</summary>
    Task DeleteAsync(string? publicId, CancellationToken ct = default);
}
