using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;

namespace MauiOCRFacturas.Services;

public class SpeechService
{
    private readonly string _subscriptionKey;
    private readonly string _region;

    public SpeechService(IConfiguration configuration)
    {
        _subscriptionKey = configuration["AzureSpeech:Key"] 
                           ?? throw new ArgumentNullException("Falta AzureSpeech:Key en appsettings.json");
            
        _region = configuration["AzureSpeech:Region"] 
                  ?? throw new ArgumentNullException("Falta AzureSpeech:Region en appsettings.json");
    }

    public async Task<string> TranscribeFromMicrophoneAsync()
    {
        var config = SpeechConfig.FromSubscription(_subscriptionKey, _region);
        config.SpeechRecognitionLanguage = "es-ES";

        using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
        using var recognizer = new SpeechRecognizer(config, audioConfig);

        var result = await recognizer.RecognizeOnceAsync();

        return result.Reason switch
        {
            ResultReason.RecognizedSpeech => result.Text,
            ResultReason.NoMatch => throw new Exception("No se reconoció ningún audio."),
            ResultReason.Canceled => throw new Exception(
                $"Reconocimiento cancelado: {CancellationDetails.FromResult(result).ErrorDetails}"),
            _ => throw new Exception("Error desconocido en Speech.")
        };
    }
}