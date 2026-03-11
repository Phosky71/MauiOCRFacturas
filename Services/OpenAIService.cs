using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace MauiOCRFacturas.Services;

public class OpenAIService
{
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deploymentName;
    private readonly HttpClient _httpClient;

    public OpenAIService(IConfiguration configuration)
    {
        _endpoint = configuration["AzureOpenAI:Endpoint"]?.TrimEnd('/') 
            ?? throw new ArgumentNullException("Falta AzureOpenAI:Endpoint en appsettings.json");
            
        _apiKey = configuration["AzureOpenAI:Key"] 
            ?? throw new ArgumentNullException("Falta AzureOpenAI:Key en appsettings.json");
            
        _deploymentName = configuration["AzureOpenAI:DeploymentName"] 
            ?? throw new ArgumentNullException("Falta AzureOpenAI:DeploymentName en appsettings.json");

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
    }

    // CORRECCIÓN: Cambiado de Task a Task<string>
    public async Task<string> GenerateSummaryAsync(string transcribedText)
    {
        var url = $"{_endpoint}/openai/deployments/{_deploymentName}/chat/completions?api-version=2024-02-01";

        var requestBody = new
        {
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "Eres un asistente que genera resúmenes claros y concisos en español. " +
                              "Cuando recibas una transcripción de audio, extrae los puntos clave y devuelve " +
                              "un resumen estructurado con bullet points."
                },
                new
                {
                    role = "user",
                    content = $"Por favor, genera un resumen de la siguiente transcripción de voz:\n\n{transcribedText}"
                }
            },
            max_tokens = 500,
            temperature = 0.5
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error OpenAI ({response.StatusCode}): {error}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "Sin respuesta";
    }
}
