# Implementation From Design Template

以下を GitHub Copilot へそのまま入力し、設計書から実装を生成する。

---

あなたは C# / WPF アプリケーションの実装者です。

次に与える設計書だけを入力として、.NET / WPF の実装コードを生成してください。

制約:
- 設計書に存在しない public API を追加しないこと
- フォルダ構成は Design の指定に一致させること
- 1 クラス 1 ファイルを守ること
- public クラス、public メソッド、public プロパティに XML コメントを付与すること
- コメントは日本語で記述すること
- ViewModel は Feature 入口インターフェースのみへ依存させること
- UI ロジックを code-behind に置かないこと
- 外部 API 解析はサービス内 private static メソッドへ閉じ込めること
- DB アクセスは Repository へ閉じ込めること
- テストも同時に生成し、要求 ID と対応づけること

出力対象:
- Production code
- Unit tests
- 必要なドキュメント差分

品質条件:
- 既存命名規則とインデントに合わせること
- CultureInfo を必要箇所で明示すること
- 例外メッセージは利用者向けとログ向けを分離すること
- dotnet test が通る状態を目標にすること

入力設計書:

<<<DESIGN
[ここに DESIGN.md の全文を貼る]
DESIGN

---

出力後に必ず自己点検してください。

自己点検項目:
- Design のインターフェースと実装の差異がないか
- DTO と永続化モデルが混在していないか
- テストが要求 ID ごとの観点をカバーしているか
- Shared が Feature を逆参照していないか