using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MarketMonitor.Shared.Infrastructure;

/// <summary>
/// プロパティ変更通知の共通基盤を提供する。
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// プロパティ変更通知を発火する。
    /// </summary>
    /// <param name="propertyName">変更されたプロパティ名。</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// 値が変わった場合だけプロパティを更新する。
    /// </summary>
    /// <typeparam name="T">更新対象の型。</typeparam>
    /// <param name="storage">更新対象の格納先。</param>
    /// <param name="value">新しい値。</param>
    /// <param name="propertyName">変更されたプロパティ名。</param>
    /// <returns>値を更新した場合は true。</returns>
    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}