using Camera.MAUI;
using MauiOCRFacturas.Services;
using MauiOCRFacturas.ViewModels;
using MauiOCRFacturas.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MauiOCRFacturas;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCameraView()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Cargar appsettings.json embebido
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.EndsWith("appsettings.json"));

        if (resourceName != null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();
                builder.Configuration.AddConfiguration(config);
            }
        }

        // ─── Servicios ────────────────────────────────
        builder.Services.AddSingleton<DocumentIntelligenceService>();
        builder.Services.AddSingleton<IHistorialService, HistorialService>();
        builder.Services.AddSingleton<ComputerVisionService>();
        builder.Services.AddSingleton<SpeechTranslatorService>();
        builder.Services.AddSingleton<ServiceBusService>();
        builder.Services.AddSingleton<SpeechService>();
        builder.Services.AddSingleton<OpenAIService>();

        // ─── ViewModels ───────────────────────────────
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<HistorialViewModel>();
        builder.Services.AddTransient<VisionViewModel>();
        builder.Services.AddTransient<TraductorViewModel>();

        // ─── Páginas ──────────────────────────────────
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<HistorialPage>();
        builder.Services.AddTransient<VisionPage>();
        builder.Services.AddTransient<TraductorPage>();
        builder.Services.AddTransient<VoiceSummaryPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
