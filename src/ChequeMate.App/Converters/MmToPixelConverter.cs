using System.Globalization;
using System.Windows.Data;

namespace ChequeMate.App.Converters;

/// <summary>
/// Converts millimeters to screen pixels for the template editor canvas.
/// Uses a fixed scale of ~3.78 pixels per mm (96 DPI screen).
/// </summary>
public class MmToPixelConverter : IValueConverter
{
    // 96 DPI / 25.4 mm per inch = 3.78 px/mm
    private const double PixelsPerMm = 96.0 / 25.4;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double mm)
            return mm * PixelsPerMm;
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double px)
            return px / PixelsPerMm;
        return 0.0;
    }
}
