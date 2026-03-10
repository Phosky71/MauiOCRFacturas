using Microsoft.Extensions.Logging;
using MauiOCRFacturas.Services;
using MauiOCRFacturas.ViewModels;
using MauiOCRFacturas.Views;

namespace MauiOCRFacturas;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Registro de servicios
        builder.Services.AddSingleton<DocumentIntelligenceService>();

        // Registro de ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<HistorialViewModel>();

        // Registro de Vistas
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<HistorialPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
