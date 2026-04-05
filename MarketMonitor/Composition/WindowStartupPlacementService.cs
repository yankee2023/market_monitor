using System.Windows;

namespace MarketMonitor.Composition;

/// <summary>
/// ウィンドウの初期表示位置とサイズを作業領域内へ収める責務を表す。
/// </summary>
internal static class WindowStartupPlacementService
{
    /// <summary>
    /// 作業領域内に収まる初期表示矩形を計算する。
    /// </summary>
    internal static Rect CalculateStartupBounds(Rect workArea, double desiredWidth, double desiredHeight)
    {
        var width = Math.Min(desiredWidth, workArea.Width);
        var height = Math.Min(desiredHeight, workArea.Height);
        var left = workArea.Left + Math.Max(0d, (workArea.Width - width) / 2d);
        var top = workArea.Top + Math.Max(0d, (workArea.Height - height) / 2d);

        return new Rect(left, top, width, height);
    }

    /// <summary>
    /// ウィンドウへ初期表示位置を適用する。
    /// </summary>
    internal static void Apply(Window window, Rect workArea)
    {
        ArgumentNullException.ThrowIfNull(window);

        var bounds = CalculateStartupBounds(workArea, window.Width, window.Height);
        window.MaxWidth = workArea.Width;
        window.MaxHeight = workArea.Height;
        window.Width = bounds.Width;
        window.Height = bounds.Height;
        window.Left = bounds.Left;
        window.Top = bounds.Top;
    }
}