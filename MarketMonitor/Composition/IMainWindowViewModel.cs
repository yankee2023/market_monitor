using MarketMonitor.Features.Dashboard.Models;
using MarketMonitor.Features.JapaneseStockChart.Models;

namespace MarketMonitor.Composition;

/// <summary>
/// メインウィンドウ初期化時に必要な ViewModel 契約を表す。
/// </summary>
internal interface IMainWindowViewModel
{
    /// <summary>
    /// 手動描画モード有効状態。
    /// </summary>
    bool IsAnalysisLineDrawingEnabled { get; }

    /// <summary>
    /// 初期表示に必要な非同期処理を実行する。
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 手動描画に利用できる分析ライン種別一覧を取得する。
    /// </summary>
    IReadOnlyList<ChartAnalysisLineTypeOption> GetManualAnalysisLineTypeOptions();

    /// <summary>
    /// 指定した線種で手動描画モードを開始する。
    /// </summary>
    void StartManualAnalysisLineDrawing(ChartAnalysisLineType lineType);

    /// <summary>
    /// 手動描画モードを終了する。
    /// </summary>
    void CancelManualAnalysisLineDrawing();

    /// <summary>
    /// ローソク足チャート上のクリック座標を受け取る。
    /// </summary>
    /// <param name="chartX">チャート左端基準の X 座標。</param>
    /// <param name="chartY">チャート上端基準の Y 座標。</param>
    void RegisterJapaneseChartClick(double chartX, double chartY);

    /// <summary>
    /// ローソク足チャート上のポインター押下を処理する。
    /// </summary>
    bool BeginJapaneseChartPointerInteraction(double chartX, double chartY);

    /// <summary>
    /// ローソク足チャート上のポインター移動を処理する。
    /// </summary>
    bool UpdateJapaneseChartPointerInteraction(double chartX, double chartY);

    /// <summary>
    /// ローソク足チャート上のポインター解放を処理する。
    /// </summary>
    bool CompleteJapaneseChartPointerInteraction(double chartX, double chartY);

    /// <summary>
    /// 自動追加候補の分析ライン一覧を取得する。
    /// </summary>
    IReadOnlyList<AutoAnalysisLineCandidate> GetAutoAnalysisLineCandidates();

    /// <summary>
    /// 選択した自動分析ラインを現在の表示へ追加する。
    /// </summary>
    void AppendSelectedAutoAnalysisLines(IReadOnlyList<Guid> selectedLineIds);
}