# Tokyo Market Technical 向け Copilot 指示

このファイルは Tokyo Market Technical 固有のルールだけを扱います。

他プロジェクトにも再利用しやすい C# / WPF 共通ルールは [.github/instructions/common-csharp-wpf.instructions.md](.github/instructions/common-csharp-wpf.instructions.md) に分離しました。

## 1. スコープ
- 本アプリケーションは日本株専用とすること。
- 仕様変更が明示されない限り、為替機能と海外市場機能は追加しないこと。
- ユーザー入力は東証 `.T` シンボル、または東証プライム、スタンダード、グロース銘柄名として扱うこと。

## 2. 正本管理
- [SPECIFICATION.md](SPECIFICATION.md)、[DESIGN.md](DESIGN.md)、実装を同一変更で整合させること。
- 要件の正本は [SPECIFICATION.md](SPECIFICATION.md) とすること。
- 設計の正本は [DESIGN.md](DESIGN.md) とすること。
- 振る舞い変更時はコードだけでなくドキュメントも同時更新すること。
- Copilot のウォーターフォール運用は [docs/COPILOT_WATERFALL_WORKFLOW.md](docs/COPILOT_WATERFALL_WORKFLOW.md) と templates 配下のテンプレートに合わせること。

## 3. アーキテクチャ
- `Composition`、`Shared`、`Features` を中心とした feature-sliced 構成を維持すること。
- 複数機能で共有が確定した責務のみ `Shared` に置くこと。
- メイン画面の統合制御は `Features/Dashboard/ViewModels/MainViewModel` に集約すること。
- `Composition` には依存関係の組み立てと起動設定だけを置くこと。
- 削除済みの旧 root-level 構成を再導入しないこと。

## 4. データ取得と永続化
- 日本株の現在値とローソク足は Yahoo Finance を主取得元、Stooq をフォールバックとすること。
- 東証プライム、スタンダード、グロース銘柄名解決は JPX 上場銘柄一覧を使用すること。
- SQLite 履歴スキーマは `symbol`、`stock_price`、`recorded_at` を前提とすること。
- 依存追加や更新でライセンスに影響がある場合は [THIRD_PARTY_LICENSES.md](THIRD_PARTY_LICENSES.md) と README の参照も更新すること。

## 5. ログとエラー
- Serilog を使い、ログ出力先は `logs/app-.log` を維持すること。
- 利用者向けエラーメッセージは日本語で統一すること。
- マーケットデータ系の共通エラーメッセージは shared 側へ集約すること。

## 6. テストと品質ゲート
- 本リポジトリのテストは xUnit を優先すること。
- feature orchestration、fallback、repository、symbol resolution を変更する場合はテストを追加または更新すること。
- `MarketMonitorTest` に設定したカバレッジしきい値を維持または改善すること。
- 本番コード変更時は、可能な限り反射ではなく interface や注入可能な依存を使ってテスト容易性を確保すること。

## 7. プロジェクト固有の記述ルール
- コメントは日本語で記載すること。
- public XML コメントは責務変更に合わせて更新すること。
- 現在の命名規則とフォルダ構成を、仕様変更なしに崩さないこと。
