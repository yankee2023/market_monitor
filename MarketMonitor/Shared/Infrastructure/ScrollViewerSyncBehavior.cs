using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MarketMonitor.Shared.Infrastructure;

/// <summary>
/// 同一グループ内の ScrollViewer の横スクロール位置を同期する。
/// </summary>
public static class ScrollViewerSyncBehavior
{
    private static readonly Dictionary<string, List<ScrollViewer>> Groups = new(StringComparer.Ordinal);

    /// <summary>
    /// 同期グループ名を取得する。
    /// </summary>
    public static string GetGroupName(DependencyObject obj)
    {
        return (string)obj.GetValue(GroupNameProperty);
    }

    /// <summary>
    /// 同期グループ名を設定する。
    /// </summary>
    public static void SetGroupName(DependencyObject obj, string value)
    {
        obj.SetValue(GroupNameProperty, value);
    }

    /// <summary>
    /// 内部更新中フラグを取得する。
    /// </summary>
    public static bool GetIsSyncing(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsSyncingProperty);
    }

    /// <summary>
    /// 内部更新中フラグを設定する。
    /// </summary>
    public static void SetIsSyncing(DependencyObject obj, bool value)
    {
        obj.SetValue(IsSyncingProperty, value);
    }

    /// <summary>
    /// 同期グループ名。
    /// </summary>
    public static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.RegisterAttached(
            "GroupName",
            typeof(string),
            typeof(ScrollViewerSyncBehavior),
            new PropertyMetadata(string.Empty, OnGroupNameChanged));

    private static readonly DependencyProperty IsSyncingProperty =
        DependencyProperty.RegisterAttached(
            "IsSyncing",
            typeof(bool),
            typeof(ScrollViewerSyncBehavior),
            new PropertyMetadata(false));

    private static void OnGroupNameChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not ScrollViewer scrollViewer)
        {
            return;
        }

        scrollViewer.Loaded -= OnScrollViewerLoaded;
        scrollViewer.Unloaded -= OnScrollViewerUnloaded;
        scrollViewer.ScrollChanged -= OnScrollViewerScrollChanged;

        Unregister(scrollViewer, e.OldValue as string);

        if (e.NewValue is not string groupName || string.IsNullOrWhiteSpace(groupName))
        {
            return;
        }

        scrollViewer.Loaded += OnScrollViewerLoaded;
        scrollViewer.Unloaded += OnScrollViewerUnloaded;
        scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;

        if (scrollViewer.IsLoaded)
        {
            Register(scrollViewer, groupName);
        }
    }

    private static void OnScrollViewerLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            Register(scrollViewer, GetGroupName(scrollViewer));
        }
    }

    private static void OnScrollViewerUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer scrollViewer)
        {
            Unregister(scrollViewer, GetGroupName(scrollViewer));
        }
    }

    private static void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer source
            || e.HorizontalChange == 0d
            || GetIsSyncing(source))
        {
            return;
        }

        var groupName = GetGroupName(source);
        if (string.IsNullOrWhiteSpace(groupName) || !Groups.TryGetValue(groupName, out var viewers))
        {
            return;
        }

        foreach (var target in viewers.Where(viewer => !ReferenceEquals(viewer, source)).ToList())
        {
            if (!target.IsLoaded)
            {
                continue;
            }

            SetIsSyncing(target, true);
            target.ScrollToHorizontalOffset(source.HorizontalOffset);
            SetIsSyncing(target, false);
        }
    }

    private static void Register(ScrollViewer scrollViewer, string? groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return;
        }

        if (!Groups.TryGetValue(groupName, out var viewers))
        {
            viewers = [];
            Groups[groupName] = viewers;
        }

        if (viewers.Contains(scrollViewer))
        {
            return;
        }

        var existingViewer = viewers.FirstOrDefault(viewer => viewer.IsLoaded);
        viewers.Add(scrollViewer);

        if (existingViewer is not null)
        {
            SetIsSyncing(scrollViewer, true);
            scrollViewer.ScrollToHorizontalOffset(existingViewer.HorizontalOffset);
            SetIsSyncing(scrollViewer, false);
        }
    }

    private static void Unregister(ScrollViewer scrollViewer, string? groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName) || !Groups.TryGetValue(groupName, out var viewers))
        {
            return;
        }

        viewers.Remove(scrollViewer);
        if (viewers.Count == 0)
        {
            Groups.Remove(groupName);
        }
    }
}