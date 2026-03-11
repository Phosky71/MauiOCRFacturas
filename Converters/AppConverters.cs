using System.Globalization;

namespace MauiOCRFacturas.Converters;

/// <summary>
/// Invierte un valor booleano (True pasa a False y viceversa).
/// Muy útil para ocultar elementos cuando algo es verdadero.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool booleanValue)
            return !booleanValue;
        
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool booleanValue)
            return !booleanValue;
            
        return false;
    }
}

/// <summary>
/// Devuelve True si el objeto NO es nulo, y False si es nulo.
/// Útil para mostrar elementos de la UI solo cuando hay datos.
/// </summary>
public class NotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}