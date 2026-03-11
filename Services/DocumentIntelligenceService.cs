using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using MauiOCRFacturas.Models;
using Microsoft.Extensions.Configuration;

namespace MauiOCRFacturas.Services;

public class DocumentIntelligenceService
{
    private readonly string _endpoint;
    private readonly string _apiKey;

    private readonly DocumentAnalysisClient _client;

    public DocumentIntelligenceService(IConfiguration configuration)
    {
        _endpoint = configuration["AzureDocumentIntelligence:Endpoint"]!;
        _apiKey = configuration["AzureDocumentIntelligence:ApiKey"]!;

        _client = new DocumentAnalysisClient(new Uri(_endpoint), new AzureKeyCredential(_apiKey));
    }

    
    /// <summary>
    /// Analiza un documento (factura) a partir de un stream de imagen
    /// y devuelve el resultado como un objeto ResultadoOCR.
    /// </summary>
    public async Task<ResultadoOCR> AnalizarDocumentoAsync(Stream imagenStream)
    {
        // Usamos el modelo prebuilt-invoice para facturas
        var operation = await _client.AnalyzeDocumentAsync(
            WaitUntil.Completed,
            "prebuilt-invoice",
            imagenStream);

        var result = operation.Value;
        var resultadoOCR = new ResultadoOCR
        {
            FechaAnalisis = DateTime.Now,
            TextoCompleto = string.Empty
        };

        var sb = new System.Text.StringBuilder();

        foreach (var document in result.Documents)
        {
            sb.AppendLine("=== DATOS DE LA FACTURA ===");
            sb.AppendLine();

            // Proveedor
            if (document.Fields.TryGetValue("VendorName", out var vendorName))
            {
                resultadoOCR.Proveedor = vendorName.Content;
                sb.AppendLine($"Proveedor: {vendorName.Content}");
            }

            // Cliente
            if (document.Fields.TryGetValue("CustomerName", out var customerName))
            {
                resultadoOCR.Cliente = customerName.Content;
                sb.AppendLine($"Cliente: {customerName.Content}");
            }

            // Numero de factura
            if (document.Fields.TryGetValue("InvoiceId", out var invoiceId))
            {
                resultadoOCR.NumeroFactura = invoiceId.Content;
                sb.AppendLine($"Numero de Factura: {invoiceId.Content}");
            }

            // Fecha de factura
            if (document.Fields.TryGetValue("InvoiceDate", out var invoiceDate))
            {
                sb.AppendLine($"Fecha: {invoiceDate.Content}");
            }

            // Total
            if (document.Fields.TryGetValue("InvoiceTotal", out var total))
            {
                resultadoOCR.Total = total.Content;
                sb.AppendLine($"Total: {total.Content}");
            }

            // Subtotal
            if (document.Fields.TryGetValue("SubTotal", out var subtotal))
            {
                sb.AppendLine($"Subtotal: {subtotal.Content}");
            }

            // Impuestos
            if (document.Fields.TryGetValue("TotalTax", out var tax))
            {
                sb.AppendLine($"Impuestos: {tax.Content}");
            }

            sb.AppendLine();
            sb.AppendLine("--- Items ---");

            // Items de la factura
            if (document.Fields.TryGetValue("Items", out var items)
                && items.FieldType == DocumentFieldType.List)
            {
                foreach (var item in items.Value.AsList())
                {
                    if (item.FieldType == DocumentFieldType.Dictionary)
                    {
                        var itemFields = item.Value.AsDictionary();
                        if (itemFields.TryGetValue("Description", out var desc))
                            sb.AppendLine($"  - {desc.Content}");
                        if (itemFields.TryGetValue("Amount", out var amount))
                            sb.AppendLine($"    Importe: {amount.Content}");
                    }
                }
            }
        }

        // Si no hay documentos estructurados, usamos el texto raw
        if (result.Documents.Count == 0)
        {
            sb.AppendLine("=== TEXTO EXTRAIDO ===");
            foreach (var page in result.Pages)
            {
                foreach (var line in page.Lines)
                {
                    sb.AppendLine(line.Content);
                }
            }
        }

        resultadoOCR.TextoCompleto = sb.ToString();
        return resultadoOCR;
    }
}
