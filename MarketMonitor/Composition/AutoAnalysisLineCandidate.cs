namespace MarketMonitor.Composition;

/// <summary>
/// 自動分析ライン追加ダイアログに表示する候補情報。
/// </summary>
public sealed record AutoAnalysisLineCandidate(Guid LineId, string DisplayName, string Description);
