using Camera.MAUI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiOCRFacturas.Helpers;
using MauiOCRFacturas.Models;
using MauiOCRFacturas.Services;
using Microsoft.Maui.Graphics;

namespace MauiOCRFacturas.ViewModels;

public partial class VisionViewModel : ObservableObject
{
    private readonly ComputerVisionService _visionService;
    private CancellationTokenSource? _cancelacionToken;
    private bool _analizando = false;
    private bool _pausado = false;

    [ObservableProperty] private ObjetosDrawable canvasObjetos = new();
    [ObservableProperty] private string resultadoTexto = "Apunta la cámara a un objeto para detectarlo.";
    [ObservableProperty] private bool isLoading = false;
    [ObservableProperty] private string textoBoton = "Pausar";

    public VisionViewModel(ComputerVisionService visionService)
    {
        _visionService = visionService;
    }

    public async Task IniciarAnalisisContinuoAsync(CameraView camara)
    {
        _cancelacionToken = new CancellationTokenSource();
        _pausado = false;
        TextoBoton = "Pausar";

        while (!_cancelacionToken.Token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(2000, _cancelacionToken.Token);

                if (_pausado || _analizando) continue;

                _analizando = true;
                IsLoading = true;

                var stream = await camara.TakePhotoAsync();
                if (stream == null)
                {
                    _analizando = false;
                    IsLoading = false;
                    continue;
                }

                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var resultado = await _visionService.AnalizarImagenAsync(memoryStream);

                CanvasObjetos = new ObjetosDrawable
                {
                    Objetos = resultado.Objetos,
                    ImagenOriginalAncho = resultado.ImagenAncho,
                    ImagenOriginalAlto = resultado.ImagenAlto
                };

                ResultadoTexto = resultado.TextoCompleto;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ResultadoTexto = $"Error: {ex.Message}";
            }
            finally
            {
                _analizando = false;
                IsLoading = false;
            }
        }
    }

    public void DetenerAnalisis()
    {
        _cancelacionToken?.Cancel();
    }

    [RelayCommand]
    private void ToggleAnalisis()
    {
        _pausado = !_pausado;
        TextoBoton = _pausado ? "Reanudar" : "Pausar";
        ResultadoTexto = _pausado
            ? "Análisis pausado."
            : "Apunta la cámara a un objeto para detectarlo.";

        if (_pausado)
            CanvasObjetos = new ObjetosDrawable();
    }
}
