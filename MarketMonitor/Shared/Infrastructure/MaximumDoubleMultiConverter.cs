using System.Globalization;
using System.Windows.Data;

namespace MarketMonitor.Shared.Infrastructure;

/// <summary>
/// 複数の数値から最大値を返す。
/// </summary>
public sealed class MaximumDoubleMultiConverter : IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var maximum = 0d;
        foreach (var value in values)
        {
            if (value is double numericValue && !double.IsNaN(numericValue) && !double.IsInfinity(numericValue))
            {
                maximum = Math.Max(maximum, numericValue);
            }
        }

        return maximum;
    }

    /// <inheritdoc />
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        return targetTypes.Select(_ => System.Windows.Data.Binding.DoNothing).ToArray();
    }
}