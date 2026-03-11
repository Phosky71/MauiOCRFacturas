using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiOCRFacturas.Services;
using System.Collections.ObjectModel;

namespace MauiOCRFacturas.ViewModels
{
    public partial class TraductorViewModel : ObservableObject
    {
        private readonly SpeechTranslatorService speechService;
        private readonly ServiceBusService serviceBusService;

        [ObservableProperty]
        private string textoReconocido = string.Empty;

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private bool mensajeEnviado = false;

        // Colección de resultados por idioma
        [ObservableProperty]
        private ObservableCollection<TraduccionResultado> traducciones = new();

        // Idiomas disponibles con checkbox
        [ObservableProperty]
        private ObservableCollection<IdiomaOpcion> idiomasDisponibles = new()
        {
            new IdiomaOpcion("Inglés",    "en",    true),
            new IdiomaOpcion("Francés",   "fr",    false),
            new IdiomaOpcion("Alemán",    "de",    false),
            new IdiomaOpcion("Italiano",  "it",    false),
            new IdiomaOpcion("Portugués", "pt",    false),
            new IdiomaOpcion("Japonés",   "ja",    false),
            new IdiomaOpcion("Chino",     "zh-Hans", false),
            new IdiomaOpcion("Árabe",     "ar",    false),
            new IdiomaOpcion("Ruso",      "ru",    false),
        };

        public TraductorViewModel(SpeechTranslatorService speechService,
                                  ServiceBusService serviceBusService)
        {
            this.speechService = speechService;
            this.serviceBusService = serviceBusService;
        }

        [RelayCommand]
        private async Task GrabarYTraducirAsync()
        {
            try
            {
                IsLoading = true;
                MensajeEnviado = false;
                Traducciones.Clear();
                TextoReconocido = "Escuchando... habla ahora";

                // 1. Reconocer voz
                var textoOriginal = await speechService.ReconocerVozAsync("es-ES");

                if (string.IsNullOrEmpty(textoOriginal))
                {
                    TextoReconocido = "No se detectó voz. Inténtalo de nuevo.";
                    return;
                }

                TextoReconocido = textoOriginal;

                // 2. Traducir a todos los idiomas seleccionados simultáneamente
                var idiomasSeleccionados = IdiomasDisponibles
                    .Where(i => i.Seleccionado)
                    .ToList();

                if (!idiomasSeleccionados.Any())
                {
                    TextoReconocido += "\n(Selecciona al menos un idioma)";
                    return;
                }

                // Lanzar todas las traducciones en paralelo
                var tareas = idiomasSeleccionados.Select(async idioma =>
                {
                    var traduccion = await speechService.TraducirTextoAsync(
                        textoOriginal, idioma.Codigo);
                    return new TraduccionResultado(idioma.Nombre, traduccion);
                });

                var resultados = await Task.WhenAll(tareas);

                foreach (var r in resultados)
                    Traducciones.Add(r);

                // 3. Enviar todas las traducciones al Service Bus
                var mensajeCompleto = string.Join("\n", resultados
                    .Select(r => $"[{r.Idioma}]: {r.Texto}"));

                await serviceBusService.EnviarMensajeAsync(mensajeCompleto);
                MensajeEnviado = true;
            }
            catch (Exception ex)
            {
                TextoReconocido = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    // Idioma con checkbox para selección múltiple
    public partial class IdiomaOpcion : ObservableObject
    {
        public string Nombre { get; }
        public string Codigo { get; }

        [ObservableProperty]
        private bool seleccionado;

        public IdiomaOpcion(string nombre, string codigo, bool seleccionado = false)
        {
            Nombre = nombre;
            Codigo = codigo;
            Seleccionado = seleccionado;
        }
    }

    // Resultado de traducción por idioma
    public record TraduccionResultado(string Idioma, string Texto);
}
