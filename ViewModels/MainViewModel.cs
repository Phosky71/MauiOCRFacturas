using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiOCRFacturas.Models;
using MauiOCRFacturas.Services;

namespace MauiOCRFacturas.ViewModels;

[ObservableObject]
public partial class MainViewModel
{
    private readonly DocumentIntelligenceService _ocrService;
    private readonly IHistorialService _historialService; // Nuevo servicio inyectado

    [ObservableProperty]
    private ImageSource? _imagenCapturada;

    [ObservableProperty]
    private string _resultadoTexto = "Capture o seleccione una imagen de factura para analizarla.";

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private bool _hayResultado = false;

    [ObservableProperty]
    private ResultadoOCR? _ultimoResultado;

    // Inyectamos AMBOS servicios por el constructor
    public MainViewModel(DocumentIntelligenceService ocrService, IHistorialService historialService)
    {
        _ocrService = ocrService;
        _historialService = historialService;
    }

    [RelayCommand]
    private async Task TomarFotoAsync()
    {
        try
        {
            // 1. Verificar primero si ya tenemos el permiso
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                // Si no lo tenemos, lo solicitamos
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status != PermissionStatus.Granted)
            {
                await Shell.Current.DisplayAlert("Permiso denegado",
                    "Se necesita acceso a la cámara para tomar fotos de facturas.", "OK");
                return;
            }

            // 2. Comprobar si el dispositivo soporta capturar fotos
            if (MediaPicker.Default.IsCaptureSupported)
            {
                var foto = await MediaPicker.Default.CapturePhotoAsync();
                if (foto is null) return;

                await ProcesarFotoAsync(foto);
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "Tu dispositivo no soporta la captura de fotos.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al acceder a la cámara: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task SeleccionarImagenAsync()
    {
        try
        {
            var imagen = await MediaPicker.Default.PickPhotoAsync();
            if (imagen is null) return;

            await ProcesarFotoAsync(imagen);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Error al seleccionar imagen: {ex.Message}", "OK");
        }
    }

    private async Task ProcesarFotoAsync(FileResult foto)
    {
        try
        {
            IsLoading = true;
            HayResultado = false;
            ResultadoTexto = "Analizando documento con Azure Document Intelligence...";

            // Leer la imagen en un MemoryStream para evitar problemas de bloqueos o permisos de archivo en Android/iOS
            using var stream = await foto.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            
            // Mostrar preview de la imagen de forma segura copiando los bytes
            ImagenCapturada = ImageSource.FromStream(() => new MemoryStream(memoryStream.ToArray()));

            // Resetear la posición del stream de memoria al principio para enviarlo a Azure
            memoryStream.Position = 0;

            // Analizar con Azure
            var resultado = await _ocrService.AnalizarDocumentoAsync(memoryStream);
            resultado.RutaImagen = foto.FullPath;

            // Guardar en historial usando el servicio
            _historialService.AgregarFactura(resultado);

            UltimoResultado = resultado;
            ResultadoTexto = resultado.TextoCompleto;
            HayResultado = true;
        }
        catch (Exception ex)
        {
            ResultadoTexto = $"Error durante el análisis:\n{ex.Message}";
            await Shell.Current.DisplayAlert("Error de OCR",
                $"No se pudo analizar el documento:\n{ex.Message}", "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task VerHistorialAsync()
    {
        await Shell.Current.GoToAsync("//historial");
    }
}