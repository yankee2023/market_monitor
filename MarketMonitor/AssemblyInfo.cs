using System.Windows;
using System.Runtime.CompilerServices;

// WPF テーマ辞書の解決先とテスト公開範囲を定義するアセンブリ設定。
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    // テーマ固有のリソースディクショナリの配置先。
    // ページまたはアプリケーションのリソースで見つからない場合に使用される。
    ResourceDictionaryLocation.SourceAssembly
    // 共通リソースディクショナリの配置先。
    // ページ、アプリケーション、テーマ固有辞書のいずれにも存在しない場合に使用される。
)]
[assembly: InternalsVisibleTo("MarketMonitorTest")]
