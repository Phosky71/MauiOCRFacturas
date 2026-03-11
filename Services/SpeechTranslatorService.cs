using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;

namespace MauiOCRFacturas.Services
{
    public class SpeechTranslatorService
    {
        private readonly string speechApiKey;
        private readonly string speechRegion;
        private readonly string translatorApiKey;
        private readonly string translatorRegion;
        private readonly HttpClient httpClient = new();

        public SpeechTranslatorService(IConfiguration configuration)
        {
            speechApiKey     = configuration["AzureSpeech:ApiKey"]!;
            speechRegion     = configuration["AzureSpeech:Region"]!;
            translatorApiKey = configuration["AzureTranslator:ApiKey"]!;
            translatorRegion = configuration["AzureTranslator:Region"]!;
        }

        public async Task<string> ReconocerVozAsync(string idiomaOrigen = "es-ES")
        {
            var config = SpeechConfig.FromSubscription(speechApiKey, speechRegion);
            config.SpeechRecognitionLanguage = idiomaOrigen;

            // Fix: dar más tiempo para detectar voz
            config.SetProperty(
                PropertyId.SpeechServiceConnection_InitialSilenceTimeoutMs, "5000");
            config.SetProperty(
                PropertyId.SpeechServiceConnection_EndSilenceTimeoutMs, "2000");

            using var recognizer = new SpeechRecognizer(config);
            var result = await recognizer.RecognizeOnceAsync();

            return result.Reason == ResultReason.RecognizedSpeech
                ? result.Text
                : string.Empty;
        }

        public async Task<string> TraducirTextoAsync(string texto, string idiomaDestino)
        {
            if (string.IsNullOrEmpty(texto)) return string.Empty;

            var url = $"https://api.cognitive.microsofttranslator.com/translate" +
                      $"?api-version=3.0&to={idiomaDestino}";

            var body = new[] { new { Text = texto } };
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(body),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            request.Headers.Add("Ocp-Apim-Subscription-Key", translatorApiKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", translatorRegion);

            var response = await httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement[0]
                .GetProperty("translations")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;
        }
    }
}
