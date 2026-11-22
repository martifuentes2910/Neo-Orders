using NeoOrders.Application.Contracts.Orders;
using NeoOrders.Application.Contracts.Notifications;
using NeoOrders.Application.Contracts.Import;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NeoOrders.Application.DTOs.Import;
using NeoOrders.Infrastructure.Data;
using NeoOrders.Infrastructure.Models;
using NeoOrders.Domain.Orders;

namespace NeoOrders.Infrastructure.Services;

public sealed class CsvImportService : ICsvImportService
{
    private const int MaxBytes = 2 * 1024 * 1024;
    private const int MaxDataRows = 500;
    private static readonly string[] ClientHeaders = ["Name", "Email", "Phone", "Address"];
    private static readonly string[] ProductHeaders = ["Sku", "Name", "Category", "BasePrice", "Cost", "Stock", "MinStockAlert", "Active"];
    private static readonly string[] OrderHeaders = ["OrderNumber", "ClientId", "SellerId", "ProductId", "Cantidad"];

    private readonly AppDbContext _db;

    public CsvImportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<CsvImportResultDto> ImportClientsAsync(Stream csvStream, CancellationToken ct = default)
    {
        var (rows, errors) = await ReadRowsAsync(csvStream, ClientHeaders, ct);
        var result = new CsvImportResultDto { TotalRows = rows.Count, Errors = errors };
        if (errors.Count > 0) return result;

        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<Client>(rows.Count);

        for (var i = 0; i < rows.Count; i++)
        {
            var rowNum = i + 2;
            var row = rows[i];
            var name = Get(row, "Name");
            var email = Get(row, "Email");
            var phone = Get(row, "Phone");
            var address = Get(row, "Address");

            if (string.IsNullOrWhiteSpace(name))
                errors.Add(Err(rowNum, "Name", name, "El nombre es obligatorio."));
            else if (name.Length > 100)
                errors.Add(Err(rowNum, "Name", name, "El nombre no puede superar 100 caracteres."));

            if (string.IsNullOrWhiteSpace(email))
                errors.Add(Err(rowNum, "Email", email, "El email es obligatorio."));
            else if (email.Length > 200 || !IsValidEmail(email))
                errors.Add(Err(rowNum, "Email", email, "Email inválido."));
            else if (!seenEmails.Add(email))
                errors.Add(Err(rowNum, "Email", email, "Email duplicado en el archivo."));

            if (phone.Length > 30)
                errors.Add(Err(rowNum, "Phone", phone, "El teléfono no puede superar 30 caracteres."));
            if (address.Length > 200)
                errors.Add(Err(rowNum, "Address", address, "La dirección no puede superar 200 caracteres."));

            if (errors.Any(e => e.Row == rowNum)) continue;

            candidates.Add(new Client
            {
                Nombre = name,
                Email = email,
                Telefono = NullIfEmpty(phone),
                Direccion = NullIfEmpty(address),
                FechaAlta = DateTime.UtcNow
            });
        }

        if (errors.Count > 0) return result;

        var emails = candidates.Select(c => c.Email).ToList();
        var existingEmails = await _db.Clients.AsNoTracking()
            .Where(c => emails.Contains(c.Email))
            .Select(c => c.Email)
            .ToListAsync(ct);
        var existingSet = new HashSet<string>(existingEmails, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < candidates.Count; i++)
        {
            if (existingSet.Contains(candidates[i].Email))
                errors.Add(Err(i + 2, "Email", candidates[i].Email, "Ya existe un cliente con ese email en la base de datos."));
        }

        result.ValidRows = candidates.Count - errors.Count(e => e.Field == "Email" && e.Message.Contains("base de datos"));
        if (errors.Count > 0) { result.ValidRows = 0; return result; }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        _db.Clients.AddRange(candidates);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        result.ValidRows = candidates.Count;
        result.Imported = candidates.Count;
        return result;
    }

    public async Task<CsvImportResultDto> ImportProductsAsync(Stream csvStream, CancellationToken ct = default)
    {
        var (rows, errors) = await ReadRowsAsync(csvStream, ProductHeaders, ct);
        var result = new CsvImportResultDto { TotalRows = rows.Count, Errors = errors };
        if (errors.Count > 0) return result;

        var seenSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<Product>(rows.Count);

        for (var i = 0; i < rows.Count; i++)
        {
            var rowNum = i + 2;
            var row = rows[i];
            var sku = Get(row, "Sku");
            var name = Get(row, "Name");
            var category = Get(row, "Category");
            var basePriceRaw = Get(row, "BasePrice");
            var costRaw = Get(row, "Cost");
            var stockRaw = Get(row, "Stock");
            var minStockRaw = Get(row, "MinStockAlert");
            var activeRaw = Get(row, "Active");

            if (string.IsNullOrWhiteSpace(sku)) errors.Add(Err(rowNum, "Sku", sku, "El SKU es obligatorio."));
            else if (sku.Length > 50) errors.Add(Err(rowNum, "Sku", sku, "El SKU no puede superar 50 caracteres."));
            else if (!seenSkus.Add(sku)) errors.Add(Err(rowNum, "Sku", sku, "SKU duplicado en el archivo."));

            if (string.IsNullOrWhiteSpace(name)) errors.Add(Err(rowNum, "Name", name, "El nombre es obligatorio."));
            else if (name.Length > 120) errors.Add(Err(rowNum, "Name", name, "El nombre no puede superar 120 caracteres."));

            if (string.IsNullOrWhiteSpace(category)) errors.Add(Err(rowNum, "Category", category, "La categoría es obligatoria."));
            else if (category.Length > 60) errors.Add(Err(rowNum, "Category", category, "La categoría no puede superar 60 caracteres."));

            var basePriceOk = TryParseDecimal(basePriceRaw, out var basePrice);
            if (!basePriceOk) errors.Add(Err(rowNum, "BasePrice", basePriceRaw, "Precio base inválido."));
            else if (basePrice < 0.01m || basePrice > 10000000m) errors.Add(Err(rowNum, "BasePrice", basePriceRaw, "El precio base debe estar entre 0,01 y 10.000.000."));

            var costOk = TryParseDecimal(costRaw, out var cost);
            if (!costOk) errors.Add(Err(rowNum, "Cost", costRaw, "Costo inválido."));
            else if (cost < 0) errors.Add(Err(rowNum, "Cost", costRaw, "El costo no puede ser negativo."));

            var stockOk = int.TryParse(stockRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock);
            if (!stockOk) errors.Add(Err(rowNum, "Stock", stockRaw, "Stock inválido."));
            else if (stock < 0 || stock > 100000) errors.Add(Err(rowNum, "Stock", stockRaw, "El stock debe estar entre 0 y 100000."));

            var minOk = int.TryParse(minStockRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minStock);
            if (!minOk) errors.Add(Err(rowNum, "MinStockAlert", minStockRaw, "Stock mínimo inválido."));
            else if (minStock < 0 || minStock > 1000) errors.Add(Err(rowNum, "MinStockAlert", minStockRaw, "El stock mínimo debe estar entre 0 y 1000."));

            var activeOk = TryParseBool(activeRaw, out var active);
            if (!activeOk) errors.Add(Err(rowNum, "Active", activeRaw, "Active inválido. Use true/false/1/0/si/no."));

            if (errors.Any(e => e.Row == rowNum)) continue;

            candidates.Add(new Product
            {
                Sku = sku,
                Name = name,
                Category = category,
                BasePrice = decimal.Round(basePrice, 2, MidpointRounding.AwayFromZero),
                Cost = decimal.Round(cost, 2, MidpointRounding.AwayFromZero),
                Stock = stock,
                MinStockAlert = minStock,
                Active = active,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (errors.Count > 0) return result;

        var skus = candidates.Select(c => c.Sku).ToList();
        var existingSkus = await _db.Products.AsNoTracking()
            .Where(p => skus.Contains(p.Sku))
            .Select(p => p.Sku)
            .ToListAsync(ct);
        var existingSet = new HashSet<string>(existingSkus, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < candidates.Count; i++)
        {
            if (existingSet.Contains(candidates[i].Sku))
                errors.Add(Err(i + 2, "Sku", candidates[i].Sku, "Ya existe un producto con ese SKU en la base de datos."));
        }

        if (errors.Count > 0) { result.ValidRows = 0; return result; }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        _db.Products.AddRange(candidates);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        result.ValidRows = candidates.Count;
        result.Imported = candidates.Count;
        return result;
    }

    public async Task<CsvImportResultDto> ImportOrdersAsync(Stream csvStream, CancellationToken ct = default)
    {
        var (rows, errors) = await ReadRowsAsync(csvStream, OrderHeaders, ct);
        var result = new CsvImportResultDto { TotalRows = rows.Count, Errors = errors };
        if (errors.Count > 0) return result;

        var seenOrderNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parsed = new List<(int Row, string OrderNumber, int ClientId, int? SellerId, int ProductId, int Cantidad)>(rows.Count);

        for (var i = 0; i < rows.Count; i++)
        {
            var rowNum = i + 2;
            var row = rows[i];
            var orderNumber = Get(row, "OrderNumber");
            var clientRaw = Get(row, "ClientId");
            var sellerRaw = Get(row, "SellerId");
            var productRaw = Get(row, "ProductId");
            var cantidadRaw = Get(row, "Cantidad");

            if (string.IsNullOrWhiteSpace(orderNumber))
                errors.Add(Err(rowNum, "OrderNumber", orderNumber, "OrderNumber es obligatorio."));
            else if (orderNumber.Length > 50)
                errors.Add(Err(rowNum, "OrderNumber", orderNumber, "OrderNumber no puede superar 50 caracteres."));
            else if (!seenOrderNumbers.Add(orderNumber))
                errors.Add(Err(rowNum, "OrderNumber", orderNumber, "OrderNumber duplicado en el archivo."));

            var clientOk = int.TryParse(clientRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var clientId) && clientId > 0;
            if (!clientOk) errors.Add(Err(rowNum, "ClientId", clientRaw, "ClientId inválido. Debe ser un entero mayor a 0."));

            int? sellerId = null;
            if (!string.IsNullOrWhiteSpace(sellerRaw))
            {
                if (int.TryParse(sellerRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sid) && sid > 0)
                    sellerId = sid;
                else
                    errors.Add(Err(rowNum, "SellerId", sellerRaw, "SellerId inválido. Debe ser vacío o un entero mayor a 0."));
            }

            var productOk = int.TryParse(productRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var productId) && productId > 0;
            if (!productOk) errors.Add(Err(rowNum, "ProductId", productRaw, "ProductId inválido. Debe ser un entero mayor a 0."));

            var cantidadOk = int.TryParse(cantidadRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cantidad) && cantidad > 0;
            if (!cantidadOk) errors.Add(Err(rowNum, "Cantidad", cantidadRaw, "Cantidad inválida. Debe ser un entero mayor a 0."));

            if (errors.Any(e => e.Row == rowNum)) continue;
            parsed.Add((rowNum, orderNumber, clientId, sellerId, productId, cantidad));
        }

        if (errors.Count > 0) return result;

        var orderNumbers = parsed.Select(p => p.OrderNumber).ToList();
        var clientIds = parsed.Select(p => p.ClientId).Distinct().ToList();
        var sellerIds = parsed.Where(p => p.SellerId.HasValue).Select(p => p.SellerId!.Value).Distinct().ToList();
        var productIds = parsed.Select(p => p.ProductId).Distinct().ToList();

        var existingOrderNumbers = await _db.Orders.AsNoTracking()
            .Where(o => o.OrderNumber != null && orderNumbers.Contains(o.OrderNumber))
            .Select(o => o.OrderNumber!)
            .ToListAsync(ct);
        var existingOrderNumberSet = new HashSet<string>(existingOrderNumbers, StringComparer.OrdinalIgnoreCase);

        var clients = await _db.Clients.AsNoTracking().Where(c => clientIds.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct);
        var clientSet = clients.ToHashSet();

        var sellers = await _db.Sellers.AsNoTracking().Where(s => sellerIds.Contains(s.Id)).Select(s => new { s.Id, s.Active }).ToListAsync(ct);
        var sellerMap = sellers.ToDictionary(s => s.Id, s => s.Active);

        var products = await _db.Products.AsNoTracking().Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.BasePrice, p.Active }).ToListAsync(ct);
        var productMap = products.ToDictionary(p => p.Id);

        foreach (var p in parsed)
        {
            if (existingOrderNumberSet.Contains(p.OrderNumber))
                errors.Add(Err(p.Row, "OrderNumber", p.OrderNumber, "Ya existe un pedido con ese OrderNumber en la base de datos."));
            if (!clientSet.Contains(p.ClientId))
                errors.Add(Err(p.Row, "ClientId", p.ClientId.ToString(CultureInfo.InvariantCulture), "El cliente no existe."));
            if (p.SellerId.HasValue)
            {
                if (!sellerMap.TryGetValue(p.SellerId.Value, out var sellerActive))
                    errors.Add(Err(p.Row, "SellerId", p.SellerId.Value.ToString(CultureInfo.InvariantCulture), "El vendedor no existe."));
                else if (!sellerActive)
                    errors.Add(Err(p.Row, "SellerId", p.SellerId.Value.ToString(CultureInfo.InvariantCulture), "El vendedor está inactivo."));
            }
            if (!productMap.TryGetValue(p.ProductId, out var prod))
                errors.Add(Err(p.Row, "ProductId", p.ProductId.ToString(CultureInfo.InvariantCulture), "El producto no existe."));
            else if (!prod.Active)
                errors.Add(Err(p.Row, "ProductId", p.ProductId.ToString(CultureInfo.InvariantCulture), "El producto está inactivo."));
        }

        if (errors.Count > 0) { result.ValidRows = 0; return result; }

        var now = DateTime.UtcNow;
        var entities = new List<Order>(parsed.Count);
        foreach (var p in parsed)
        {
            var prod = productMap[p.ProductId];
            entities.Add(new Order
            {
                OrderNumber = p.OrderNumber,
                ClientId = p.ClientId,
                SellerId = p.SellerId,
                ProductId = p.ProductId,
                ProductName = prod.Name,
                Cantidad = p.Cantidad,
                PrecioUnitario = prod.BasePrice,
                Total = p.Cantidad * prod.BasePrice,
                Fecha = now,
                Estado = OrderStatuses.Pendiente
            });
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        _db.Orders.AddRange(entities);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        result.ValidRows = entities.Count;
        result.Imported = entities.Count;
        return result;
    }

    private static async Task<(List<Dictionary<string, string>> Rows, List<CsvImportErrorDto> Errors)> ReadRowsAsync(
        Stream csvStream, string[] expectedHeaders, CancellationToken ct)
    {
        var errors = new List<CsvImportErrorDto>();
        if (csvStream == null)
        {
            errors.Add(Err(0, "file", "", "Debe seleccionar un archivo CSV."));
            return ([], errors);
        }
        if (csvStream.CanSeek && csvStream.Length == 0)
        {
            errors.Add(Err(0, "file", "", "Debe seleccionar un archivo CSV."));
            return ([], errors);
        }
        if (csvStream.CanSeek && csvStream.Length > MaxBytes)
        {
            errors.Add(Err(0, "file", "", "El archivo supera el límite de 2 MB."));
            return ([], errors);
        }


        List<List<string>> parsed;
        try
        {
            using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
            var content = await reader.ReadToEndAsync(ct);
            if (Encoding.UTF8.GetByteCount(content) > MaxBytes)
            {
                errors.Add(new CsvImportErrorDto { Row = 0, Field = "archivo", Value = string.Empty, Message = "El archivo no puede superar 2 MB." });
                return ([], errors);
            }
            parsed = ParseCsv(content, ';');
        }
        catch (Exception)
        {
            errors.Add(Err(0, "file", "", "No se pudo leer el archivo CSV. Verifique el formato, delimitador ';' y codificación UTF-8."));
            return ([], errors);
        }

        var nonEmpty = parsed.Where(r => r.Any(c => !string.IsNullOrWhiteSpace(c))).ToList();
        if (nonEmpty.Count == 0)
        {
            errors.Add(Err(0, "file", "", "El archivo CSV está vacío."));
            return ([], errors);
        }

        var headers = nonEmpty[0].Select(h => h.Trim()).ToArray();
        var headerSet = new HashSet<string>(headers.Where(h => !string.IsNullOrWhiteSpace(h)), StringComparer.OrdinalIgnoreCase);
        var expectedSet = new HashSet<string>(expectedHeaders, StringComparer.OrdinalIgnoreCase);
        var missing = expectedHeaders.Where(h => !headerSet.Contains(h)).ToList();
        var extra = headers.Where(h => !string.IsNullOrWhiteSpace(h) && !expectedSet.Contains(h)).ToList();
        if (missing.Count > 0)
            errors.Add(Err(1, "header", string.Join(";", headers), $"Faltan columnas requeridas: {string.Join(", ", missing)}."));
        if (extra.Count > 0)
            errors.Add(Err(1, "header", string.Join(";", headers), $"Columnas no reconocidas: {string.Join(", ", extra)}."));
        if (errors.Count > 0) return ([], errors);

        var headerIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(headers[i])) continue;
            if (!headerIndex.TryAdd(headers[i], i))
            {
                errors.Add(Err(1, "header", headers[i], $"Columna duplicada: {headers[i]}."));
            }
        }
        if (errors.Count > 0) return ([], errors);

        var rows = new List<Dictionary<string, string>>();
        for (var i = 1; i < nonEmpty.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var raw = nonEmpty[i];
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in expectedHeaders)
            {
                var idx = headerIndex[h];
                var val = idx < raw.Count ? raw[idx]?.Trim() ?? string.Empty : string.Empty;
                dict[h] = val;
            }
            rows.Add(dict);
            if (rows.Count > MaxDataRows)
            {
                errors.Add(Err(0, "file", "", $"El archivo supera el límite de {MaxDataRows} filas de datos."));
                return ([], errors);
            }
        }

        if (rows.Count == 0)
            errors.Add(Err(0, "file", "", "El archivo no contiene filas de datos."));

        return (rows, errors);
    }

    private static List<List<string>> ParseCsv(string content, char delimiter)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < content.Length; i++)
        {
            var ch = content[i];
            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"') { field.Append('"'); i++; }
                    else inQuotes = false;
                }
                else
                {
                    field.Append(ch);
                }
                continue;
            }

            if (ch == '"') { inQuotes = true; continue; }
            if (ch == delimiter) { row.Add(field.ToString()); field.Clear(); continue; }
            if (ch == '\r' || ch == '\n')
            {
                if (ch == '\r' && i + 1 < content.Length && content[i + 1] == '\n') i++;
                row.Add(field.ToString());
                field.Clear();
                rows.Add(row);
                row = new List<string>();
                continue;
            }
            field.Append(ch);
        }

        if (inQuotes) throw new FormatException("CSV con comillas sin cerrar.");
        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }
        return rows;
    }

    private static string Get(Dictionary<string, string> row, string key) =>
        row.TryGetValue(key, out var value) ? value : string.Empty;

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static CsvImportErrorDto Err(int row, string field, string value, string message) => new()
    {
        Row = row,
        Field = field,
        Value = value?.Length > 200 ? value[..200] : value ?? string.Empty,
        Message = message
    };

    private static bool IsValidEmail(string email)
    {
        try { _ = new System.Net.Mail.MailAddress(email); return email.Contains('@'); }
        catch { return false; }
    }

    private static bool TryParseDecimal(string raw, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var normalized = raw.Trim().Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseBool(string raw, out bool value)
    {
        value = false;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var v = raw.Trim().ToLowerInvariant();
        if (v is "true" or "1" or "si" or "sí" or "yes") { value = true; return true; }
        if (v is "false" or "0" or "no") { value = false; return true; }
        return false;
    }
}
