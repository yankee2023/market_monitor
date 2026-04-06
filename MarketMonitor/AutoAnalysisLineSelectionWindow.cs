using System.Windows;
using MarketMonitor.Composition;

namespace MarketMonitor;

/// <summary>
/// 自動分析ライン追加時の候補選択ダイアログ。
/// </summary>
internal sealed class AutoAnalysisLineSelectionWindow : Window
{
    private readonly List<SelectionItem> _items;

    public AutoAnalysisLineSelectionWindow(IReadOnlyList<AutoAnalysisLineCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        Title = "追加する分析ラインを選択";
        Width = 520;
        Height = 460;
        MinWidth = 460;
        MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResizeWithGrip;

        _items = candidates
            .Select(item => new SelectionItem(item.LineId, item.DisplayName, item.Description))
            .ToList();

        Content = BuildContent();
    }

    public IReadOnlyList<Guid> GetSelectedLineIds()
    {
        return _items.Where(item => item.IsSelected).Select(item => item.LineId).ToArray();
    }

    private System.Windows.Controls.Grid BuildContent()
    {
        var root = new System.Windows.Controls.Grid { Margin = new Thickness(12) };
        root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new GridLength(1d, GridUnitType.Star) });
        root.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

        var caption = new System.Windows.Controls.TextBlock
        {
            Text = "追加したい線にチェックを入れてください。",
            Margin = new Thickness(0, 0, 0, 8)
        };
        System.Windows.Controls.Grid.SetRow(caption, 0);
        root.Children.Add(caption);

        var listContainer = new System.Windows.Controls.ScrollViewer
        {
            VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Disabled
        };
        System.Windows.Controls.Grid.SetRow(listContainer, 1);

        var stack = new System.Windows.Controls.StackPanel();
        foreach (var item in _items)
        {
            var border = new System.Windows.Controls.Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = System.Windows.Media.Brushes.LightGray,
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var itemStack = new System.Windows.Controls.StackPanel();
            var checkbox = new System.Windows.Controls.CheckBox
            {
                Content = item.DisplayName,
                IsChecked = true,
                FontWeight = FontWeights.SemiBold
            };
            checkbox.Checked += (_, _) => item.IsSelected = true;
            checkbox.Unchecked += (_, _) => item.IsSelected = false;
            itemStack.Children.Add(checkbox);
            itemStack.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = item.Description,
                Margin = new Thickness(22, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Foreground = System.Windows.Media.Brushes.DimGray,
                FontSize = 12
            });

            border.Child = itemStack;
            stack.Children.Add(border);
        }

        listContainer.Content = stack;
        root.Children.Add(listContainer);

        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0)
        };

        var cancelButton = new System.Windows.Controls.Button { Content = "キャンセル", Width = 96, Margin = new Thickness(0, 0, 8, 0) };
        cancelButton.Click += (_, _) => DialogResult = false;
        buttonPanel.Children.Add(cancelButton);

        var okButton = new System.Windows.Controls.Button { Content = "追加", Width = 96 };
        okButton.Click += (_, _) => DialogResult = true;
        buttonPanel.Children.Add(okButton);

        System.Windows.Controls.Grid.SetRow(buttonPanel, 2);
        root.Children.Add(buttonPanel);

        return root;
    }

    private sealed class SelectionItem
    {
        public SelectionItem(Guid lineId, string displayName, string description)
        {
            LineId = lineId;
            DisplayName = displayName;
            Description = description;
            IsSelected = true;
        }

        public Guid LineId { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public bool IsSelected { get; set; }
    }
}
