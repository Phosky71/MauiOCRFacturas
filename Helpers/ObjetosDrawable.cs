using MauiOCRFacturas.Models;
using Microsoft.Maui.Graphics;

namespace MauiOCRFacturas.Helpers;

public class ObjetosDrawable : IDrawable
{
    public List<ObjetoDetectado> Objetos { get; set; } = new();
    public float ImagenOriginalAncho { get; set; } = 1;
    public float ImagenOriginalAlto { get; set; } = 1;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (Objetos == null || Objetos.Count == 0) return;

        float escalaX = dirtyRect.Width / ImagenOriginalAncho;
        float escalaY = dirtyRect.Height / ImagenOriginalAlto;

        foreach (var obj in Objetos)
        {
            float x = obj.X * escalaX;
            float y = obj.Y * escalaY;
            float w = obj.Ancho * escalaX;
            float h = obj.Alto * escalaY;

            // Rectángulo del objeto
            canvas.StrokeColor = Colors.Red;
            canvas.StrokeSize = 2;
            canvas.DrawRectangle(x, y, w, h);

            // Fondo de la etiqueta
            canvas.FillColor = Color.FromArgb("#CC0078D4");
            canvas.FillRectangle(x, y - 20, w, 20);

            // Texto de la etiqueta
            canvas.FontColor = Colors.White;
            canvas.FontSize = 11;
            canvas.DrawString(
                $"{obj.Nombre} {obj.Confianza:P0}",
                x + 3, y - 19, w - 3, 19,
                HorizontalAlignment.Left,
                VerticalAlignment.Top);
        }
    }
}