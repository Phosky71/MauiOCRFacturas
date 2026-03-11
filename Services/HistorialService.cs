using MauiOCRFacturas.Models;
using System.Collections.ObjectModel;

namespace MauiOCRFacturas.Services;

/// <summary>
/// Interfaz para abstraer el manejo del historial.
/// </summary>
public interface IHistorialService
{
    ObservableCollection<ResultadoOCR> ObtenerHistorial();
    void AgregarFactura(ResultadoOCR factura);
    void LimpiarHistorial();
}

/// <summary>
/// Implementación del servicio que mantendrá la lista de facturas en memoria.
/// Al registrarse como Singleton, los datos persistirán mientras la app esté abierta.
/// </summary>
public class HistorialService : IHistorialService
{
    private readonly ObservableCollection<ResultadoOCR> _historial = new();

    public ObservableCollection<ResultadoOCR> ObtenerHistorial()
    {
        return _historial;
    }

    public void AgregarFactura(ResultadoOCR factura)
    {
        factura.Id = _historial.Count + 1;
        _historial.Insert(0, factura); // Lo inserta al principio para ver el más reciente primero
    }

    public void LimpiarHistorial()
    {
        _historial.Clear();
    }
}