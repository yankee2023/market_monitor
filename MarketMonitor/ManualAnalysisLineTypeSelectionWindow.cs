using System.Windows;
using System.Windows.Media;
using MarketMonitor.Features.Dashboard.Models;
using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor;

/// <summary>
/// 手動描画で追加する分析ライン種別を選択するダイアログ。
/// </summary>
internal sealed class ManualAnalysisLineTypeSelectionWindow : Window
{
    private readonly List<SelectionItem> _items;

    public ManualAnalysisLineTypeSelectionWindow(IReadOnlyList<ChartAnalysisLineTypeOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Title = "描画する線種を選択";
        Width = 460;
        Height = 380;
        MinWidth = 420;
        MinHeight = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        _items = options
            .Select((option, index) => new SelectionItem(option, index == 0))
            .ToList();

        Content = BuildContent();
    }

    public ChartAnalysisLineType? GetSelectedLineType()
    {
        return _items.FirstOrDefault(item => item.IsSelected)?.Option.LineType;
    }

    private System.Windows.Controls.Grid BuildContent()
    {
        var root = new System.Windows.Controls.Grid { Margin = new Thickness(12) };
        root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });
        root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

        var caption = new System.Windows.Controls.TextBlock
        {
            Text = "追加する分析ラインの種類を選択してください。",
            Margin = new Thickness(0, 0, 0, 8),
            TextWrapping = TextWrapping.Wrap
        };
        System.Windows.Controls.Grid.SetRow(caption, 0);
        root.Children.Add(caption);

        var stack = new System.Windows.Controls.StackPanel();
        foreach (var item in _items)
        {
            var border = new System.Windows.Controls.Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 8),
                Background = System.Windows.Media.Brushes.White
            };

            var itemStack = new System.Windows.Controls.StackPanel();
            var radioButton = new System.Windows.Controls.RadioButton
            {
                Content = item.Option.DisplayName,
                IsChecked = item.IsSelected,
                FontWeight = FontWeights.SemiBold,
                GroupName = "ManualAnalysisLineType"
            };
            radioButton.Checked += (_, _) => SelectItem(item);
            itemStack.Children.Add(radioButton);

            var preview = new System.Windows.Controls.Canvas
            {
                Width = 40,
                Height = 10,
                Margin = new Thickness(22, 6, 0, 4)
            };
            var line = new System.Windows.Shapes.Polyline
            {
                Points = new PointCollection([new System.Windows.Point(0, 5), new System.Windows.Point(40, 5)]),
                Stroke = ParseBrush(item.Option.StrokeColor),
                StrokeThickness = 2.2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeDashArray = CreateDashArray(item.Option.StrokeDashArray)
            };
            preview.Children.Add(line);
            itemStack.Children.Add(preview);

            itemStack.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = item.Option.Description,
                Margin = new Thickness(22, 0, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = System.Windows.Media.Brushes.DimGray,
                FontSize = 12
            });

            border.Child = itemStack;
            stack.Children.Add(border);
        }

        var scrollViewer = new System.Windows.Controls.ScrollViewer
        {
            Content = stack,
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Disabled
        };
        System.Windows.Controls.Grid.SetRow(scrollViewer, 1);
        root.Children.Add(scrollViewer);

        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var cancelButton = new System.Windows.Controls.Button { Content = "キャンセル", Width = 96, Margin = new Thickness(0, 0, 8, 0) };
        cancelButton.Click += (_, _) => DialogResult = false;
        buttonPanel.Children.Add(cancelButton);

        var okButton = new System.Windows.Controls.Button { Content = "描画を開始", Width = 96 };
        okButton.Click += (_, _) => DialogResult = true;
        buttonPanel.Children.Add(okButton);

        System.Windows.Controls.Grid.SetRow(buttonPanel, 2);
        root.Children.Add(buttonPanel);

        return root;
    }

    private void SelectItem(SelectionItem selectedItem)
    {
        foreach (var item in _items)
        {
            item.IsSelected = ReferenceEquals(item, selectedItem);
        }
    }

    private static System.Windows.Media.Brush ParseBrush(string color)
    {
        return (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromInvariantString(color)!;
    }

    private static DoubleCollection CreateDashArray(string dashArray)
    {
        var result = new DoubleCollection();
        if (string.IsNullOrWhiteSpace(dashArray))
        {
            return result;
        }

        foreach (var value in dashArray.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (double.TryParse(value, out var parsed))
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    private sealed class SelectionItem
    {
        public SelectionItem(ChartAnalysisLineTypeOption option, bool isSelected)
        {
            Option = option;
            IsSelected = isSelected;
        }

        public ChartAnalysisLineTypeOption Option { get; }

        public bool IsSelected { get; set; }
    }
}