using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiOCRFacturas.Models;
using MauiOCRFacturas.Services;
using System.Collections.ObjectModel;

namespace MauiOCRFacturas.ViewModels;

[ObservableObject]
public partial class HistorialViewModel
{
    private readonly IHistorialService _historialService;

    [ObservableProperty]
    private ObservableCollection<ResultadoOCR> _facturas;

    [ObservableProperty]
    private ResultadoOCR? _facturaSeleccionada;

    [ObservableProperty]
    private bool _hayFacturas;

    // Inyectamos el servicio por el constructor
    public HistorialViewModel(IHistorialService historialService)
    {
        _historialService = historialService;
        // Obtenemos la referencia a la colección reactiva
        _facturas = _historialService.ObtenerHistorial();
        ActualizarEstado();
    }

    private void ActualizarEstado()
    {
        HayFacturas = Facturas.Count > 0;
    }

    [RelayCommand]
    private void ActualizarHistorial()
    {
        ActualizarEstado();
    }

    [RelayCommand]
    private async Task SeleccionarFacturaAsync(ResultadoOCR factura)
    {
        if (factura is null) return;
        FacturaSeleccionada = factura;

        await Shell.Current.DisplayAlert(
            $"Factura #{factura.Id}",
            factura.TextoCompleto,
            "Cerrar");
    }

    [RelayCommand]
    private async Task LimpiarHistorialAsync()
    {
        bool confirmar = await Shell.Current.DisplayAlert(
            "Confirmar",
            "¿Se eliminarán todos los registros del historial. Continuar?",
            "Sí, borrar", "Cancelar");

        if (!confirmar) return;

        // Usamos el servicio para limpiar
        _historialService.LimpiarHistorial();
        ActualizarEstado();
    }

    [RelayCommand]
    private async Task VolverAsync()
    {
        await Shell.Current.GoToAsync("//main");
    }
}