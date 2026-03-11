using MauiOCRFacturas.Services;

namespace MauiOCRFacturas.Views;

public partial class VoiceSummaryPage : ContentPage
{
    private readonly SpeechService _speechService;
    private readonly OpenAIService _openAIService;

    // Estado visual
    public string TranscriptionText { get; set; } = "";
    public string SummaryText { get; set; } = "";
    public bool HasTranscription { get; set; } = false;
    public bool HasSummary { get; set; } = false;
    public bool IsLoading { get; set; } = false;

    public VoiceSummaryPage(SpeechService speechService, OpenAIService openAIService)
    {
        InitializeComponent();
        BindingContext = this;

        _speechService = speechService;
        _openAIService = openAIService;
    }

    private async void OnRecordClicked(object sender, EventArgs e)
    {
        // Verificar permiso de micrófono
        var status = await Permissions.RequestAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
        {
            await DisplayAlert("Permiso denegado",
                "Necesitas conceder acceso al micrófono para usar esta función.", "OK");
            return;
        }

        RecordButton.IsEnabled = false;
        RecordButton.Text = "🔴 Escuchando... (habla ahora)";
        StatusLabel.Text = "Escuchando... habla claramente al micrófono";

        try
        {
            var transcription = await _speechService.TranscribeFromMicrophoneAsync();

            TranscriptionText = transcription;
            HasTranscription = true;
            StatusLabel.Text = "Transcripción completada";
            OnPropertyChanged(nameof(TranscriptionText));
            OnPropertyChanged(nameof(HasTranscription));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo transcribir: {ex.Message}", "OK");
            StatusLabel.Text = "Error en la grabación";
        }
        finally
        {
            RecordButton.IsEnabled = true;
            RecordButton.Text = "🎙️ Iniciar Grabación";
        }
    }

    private async void OnSummarizeClicked(object sender, EventArgs e)
    {
        IsLoading = true;
        SummarizeButton.IsEnabled = false;
        OnPropertyChanged(nameof(IsLoading));

        try
        {
            var summary = await _openAIService.GenerateSummaryAsync(TranscriptionText);

            SummaryText = summary;
            HasSummary = true;
            OnPropertyChanged(nameof(SummaryText));
            OnPropertyChanged(nameof(HasSummary));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo generar el resumen: {ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
            SummarizeButton.IsEnabled = true;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    private void OnClearClicked(object sender, EventArgs e)
    {
        TranscriptionText = "";
        SummaryText = "";
        HasTranscription = false;
        HasSummary = false;
        StatusLabel.Text = "Presiona el botón para empezar a grabar";
        OnPropertyChanged(nameof(TranscriptionText));
        OnPropertyChanged(nameof(SummaryText));
        OnPropertyChanged(nameof(HasTranscription));
        OnPropertyChanged(nameof(HasSummary));
    }
}
