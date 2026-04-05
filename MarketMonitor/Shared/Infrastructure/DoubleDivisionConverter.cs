using System.Globalization;
using System.Windows.Data;

namespace MarketMonitor.Shared.Infrastructure;

/// <summary>
/// 数値を指定係数で除算する。
/// </summary>
public sealed class DoubleDivisionConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double numericValue)
        {
            return 1d;
        }

        if (parameter is not string parameterText
            || !double.TryParse(parameterText, NumberStyles.Float, CultureInfo.InvariantCulture, out var divisor)
            || divisor == 0d)
        {
            return 1d;
        }

        var result = numericValue / divisor;
        return result <= 0d ? 1d : result;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return System.Windows.Data.Binding.DoNothing;
    }
}