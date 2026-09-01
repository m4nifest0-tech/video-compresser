using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VideoCompressor.Converters;

public class ProgressBarFillConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        double value = System.Convert.ToDouble(values[0], culture);
        double min = System.Convert.ToDouble(values[1], culture);
        double max = System.Convert.ToDouble(values[2], culture);

        double range = max - min;
        double fraction = range > 0 ? (value - min) / range : 0;
        fraction = Math.Clamp(fraction, 0, 1);

        bool isRemainder = "Remainder".Equals(parameter as string, StringComparison.OrdinalIgnoreCase);
        double weight = isRemainder ? 1 - fraction : fraction;

        return new GridLength(weight, GridUnitType.Star);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
