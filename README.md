# market_monitor
**C# & Visual Studio 2026 で構築する、個人向け日本株モニタリングツール**

---

## 📌 プロジェクト概要
本プロジェクトは、個人投資家としての視点を活かし、リアルタイム（または定期的）な東証プライム銘柄の動向をデスクトップ上で効率的に監視することを目的とした Windows アプリケーションです。
単なる数値表示にとどまらず、将来的にはテクニカル指標の可視化やアラート機能を搭載し、投資判断をサポートする「自分専用のダッシュボード」を目指します。

---

## 📚 ドキュメント構成
- [SPECIFICATION.md](SPECIFICATION.md): 仕様書。要件、画面仕様、データ仕様、異常系、設計生成規約、実装生成規約を定義します。
- [DESIGN.md](DESIGN.md): 設計書。仕様書を受けて、責務分割、コンポーネント構成、フェーズ別の設計内容を定義します。
- [docs/COPILOT_WATERFALL_WORKFLOW.md](docs/COPILOT_WATERFALL_WORKFLOW.md): Copilot による仕様→設計→実装の生成手順です。
- [templates/DESIGN_FROM_SPEC_TEMPLATE.md](templates/DESIGN_FROM_SPEC_TEMPLATE.md): 仕様書から設計書を生成するための入力テンプレートです。
- [templates/IMPLEMENTATION_FROM_DESIGN_TEMPLATE.md](templates/IMPLEMENTATION_FROM_DESIGN_TEMPLATE.md): 設計書から実装を生成するための入力テンプレートです。

---

## 🛠 技術スタック
| カテゴリ | 選定技術 / キーワード | 備考 |
| :--- | :--- | :--- |
| **開発環境** | **Visual Studio 2026** | 最新のIDE機能を活用 |
| **言語** | **C# (.NET 9/10)** | 強力な型付けと非同期処理 |
| **UIフレームワーク** | **WPF** | 豊富なグラフライブラリと安定性 |
| **データ取得** | **HttpClient / JSON** | Yahoo Finance、Stooqフォールバック、JPX一覧との連携 |
| **データ保存** | **SQLite** | 過去の価格推移をローカルに記録 |
| **グラフ描画** | **WPF ItemsControl (Candlestick Chart)** | 日本株チャートと軸ラベル、ツールチップを表示 |

---

## 🚀 開発ロードマップ

> [!NOTE]
> 現在は日本株専用アプリとして、仕様書、設計書、実装を縦割り構成へ再整備しています。

### Phase 1: データ取得の基盤構築 (PoC)
- [x] コンソールアプリによるAPI接続テスト
- [x] 日本株現在値の取得
- [x] JSONデータのパース処理の実装

### Phase 2: GUIプロトタイプ (デスクトップ化)
- [x] WPFを用いたメインウィンドウの作成
- [x] ボタン押下による手動更新・タイマーによる自動更新の実装
- [x] グリッドやカード形式での現在値表示

### Phase 3: データの蓄積と視覚化
- [x] SQLiteを用いたローカルDBの構築（履歴データの保存）
- [x] 日本株の日足/週足ローソク足の取得・表示
- [x] 日本株チャートへの縦軸株価ラベル、見やすく間引いた横軸日付ラベルの表示
- [x] チャート上のマウスオーバーで日付、始値、終値、高値、安値を表示
- [x] ローソク足の表示期間切替（1か月/3か月/6か月/1年）
- [x] 汎用チャート指標UIによる MA5/MA25/MA75 の個別表示切替

### Phase 4: 高度な分析・通知機能
- [ ] **推論:** MACD等のテクニカル指標の自動計算
- [ ] 特定価格到達時のWindowsデスクトップ通知
- [ ] 特定銘柄のセクター別比較機能

---

## 💡 特徴（予定）
* **軽量な常駐設計:** デスクトップの隅に配置しても作業を邪魔しないUI。
* **日本市場への特化:** 東京証券取引所プライム市場の銘柄コードと銘柄名に最適化した検索・表示。
* **カスタマイズ性:** 自分の投資手法（エミン・ユルマズ氏の手法など）に合わせた分析ロジックの統合。

---

## 📝 開発メモ
* **命名規則:** クラス名やメソッド名はアッパーキャメルケース（PascalCase）を徹底。
* **非同期処理:** API通信中にUIがフリーズしないよう `async/await` を適切に使用する。
* **ログ出力:** WPF本線はSerilogで `logs/app-.log` に日次ローテーション出力する。
* **日本株データ取得:** 現在値とローソク足は Yahoo Finance を優先し、取得不可時のみ Stooq へフォールバックする。
* **仕様整合:** 仕様書、設計書、実装は要求 ID 単位で同時更新し、差分を残さない。
* **静的解析:** Roslyn Analyzer を有効化し、通常のビルドで保守性・品質ルールを検出する。
* **初期表示:** アプリ起動時は東証プライム銘柄を既定表示とし、日本株チャートがそのまま確認できる状態で開始する。
* **東証プライム入力:** 4桁コード入力に加えて、JPX上場銘柄一覧を参照した東証プライム銘柄名の入力解決に対応する。
* **テスト:** `MarketMonitorTest` で `MainViewModel` の単体テストを実装済み。
* **参考:** 事実に基づく情報（株価等）と、ロジックによる推論（売買サイン等）を明確に区別して表示するUI設計を心がける。

---

## 外部ライブラリライセンス
- 本プロジェクトで利用している主要な外部ライブラリとライセンスは [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md) に記載しています。
- [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md) には、ライセンス一覧だけでなく、MITやApache-2.0などの取り扱いルールも記載しています。
- 外部ライブラリを追加または更新した場合は、README.mdの記述とあわせて [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md) も更新してください。

---

### はじめに（セットアップ）
1.  Visual Studio 2026 で「WPF アプリケーション」プロジェクトを新規作成。
2.  ソリューション名を `MarketMonitor` （またはお好みの名前）に設定。
3.  `.NET デスクトップ開発` ワークロードがインストールされていることを確認。

---

## Copilot Instructions の配置方針
- どの開き方でも有効化されるよう、instructionsを以下の2箇所に配置します。
	- リポジトリルート: `.github/`
	- ソリューション配下: `MarketMonitor/.github/`
- 正本はリポジトリルートの `.github/` とします。
- 同期は `tools/sync-copilot-instructions.ps1` を実行します。
- 差分検知は `tools/check-copilot-instructions-sync.ps1` で実行できます（GitHub Actionsでも自動実行）。

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\sync-copilot-instructions.ps1
```

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\check-copilot-instructions-sync.ps1
```

---
