using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MarketMonitor.Shared.Infrastructure;

/// <summary>
/// 真偽値を GridLength へ変換する。
/// </summary>
public sealed class BooleanToGridLengthConverter : IValueConverter
{
    /// <summary>
    /// true 時の幅。
    /// </summary>
    public double TrueWidth { get; set; }

    /// <summary>
    /// false 時の幅。
    /// </summary>
    public double FalseWidth { get; set; }

    /// <summary>
    /// true 時の単位種別。
    /// </summary>
    public GridUnitType TrueUnitType { get; set; } = GridUnitType.Pixel;

    /// <summary>
    /// false 時の単位種別。
    /// </summary>
    public GridUnitType FalseUnitType { get; set; } = GridUnitType.Pixel;

    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool result && result;
        return boolValue
            ? new GridLength(TrueWidth, TrueUnitType)
            : new GridLength(FalseWidth, FalseUnitType);
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is GridLength gridLength)
        {
            return Math.Abs(gridLength.Value - TrueWidth) < 0.1d;
        }

        return System.Windows.Data.Binding.DoNothing;
    }
}