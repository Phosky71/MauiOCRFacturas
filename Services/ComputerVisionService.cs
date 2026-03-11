using Azure;
using Azure.AI.Vision.ImageAnalysis;
using MauiOCRFacturas.Models;
using Microsoft.Extensions.Configuration;

namespace MauiOCRFacturas.Services;

public class ComputerVisionService
{
    private readonly ImageAnalysisClient _client;

    public ComputerVisionService(IConfiguration configuration)
    {
        var endpoint = configuration["AzureComputerVision:Endpoint"]!;
        var apiKey = configuration["AzureComputerVision:ApiKey"]!;
        _client = new ImageAnalysisClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey));
    }

    public async Task<ResultadoVision> AnalizarImagenAsync(Stream imagenStream)
    {
        var resultado = new ResultadoVision
        {
            FechaAnalisis = DateTime.Now
        };

        var imageData = BinaryData.FromStream(imagenStream);

        var result = await _client.AnalyzeAsync(
            imageData,
            VisualFeatures.Objects | VisualFeatures.Tags | VisualFeatures.Caption);

        // Descripcion automatica
        if (result.Value.Caption != null)
            resultado.Descripcion = result.Value.Caption.Text;

        // Objetos detectados
        if (result.Value.Objects?.Values != null)
        {
            foreach (var obj in result.Value.Objects.Values)
            {
                if (obj.Tags.Count > 0)
                {
                    resultado.Objetos.Add(new ObjetoDetectado
                    {
                        Nombre = obj.Tags[0].Name,
                        Confianza = obj.Tags[0].Confidence
                    });
                }
            }
        }

        // Etiquetas generales
        if (result.Value.Tags?.Values != null)
        {
            foreach (var tag in result.Value.Tags.Values)
            {
                resultado.Etiquetas.Add($"{tag.Name} ({tag.Confidence:P0})");
            }
        }

        // Construir texto completo para mostrar en pantalla
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== DESCRIPCION ===");
        sb.AppendLine(resultado.Descripcion);
        sb.AppendLine();
        sb.AppendLine("=== OBJETOS DETECTADOS ===");
        foreach (var obj in resultado.Objetos)
            sb.AppendLine($"- {obj.Nombre} ({obj.Confianza:P0})");
        sb.AppendLine();
        sb.AppendLine("=== ETIQUETAS ===");
        foreach (var etiqueta in resultado.Etiquetas)
            sb.AppendLine($"- {etiqueta}");

        resultado.TextoCompleto = sb.ToString();
        return resultado;
    }
}
