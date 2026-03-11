namespace MauiOCRFacturas.Models;

public class ObjetoDetectado
{
    public string Nombre { get; set; } = string.Empty;
    public double Confianza { get; set; }
    // Coordenadas del bounding box que devuelve Azure
    public float X { get; set; }
    public float Y { get; set; }
    public float Ancho { get; set; }
    public float Alto { get; set; }
}

public class ResultadoVision
{
    public List<ObjetoDetectado> Objetos { get; set; } = new();
    public List<string> Etiquetas { get; set; } = new();
    public string Descripcion { get; set; } = string.Empty;
    public string TextoCompleto { get; set; } = string.Empty;
    public DateTime FechaAnalisis { get; set; } = DateTime.Now;
    public float ImagenAncho { get; set; }
    public float ImagenAlto { get; set; }
}