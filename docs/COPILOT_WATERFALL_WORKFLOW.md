# Copilot Waterfall Workflow

## 1. 目的
本書は、GitHub Copilot を使って以下の 2 段階を再現可能にするための運用手順を定義する。

- 仕様書のみを入力として設計書を生成する
- 設計書のみを入力として実装を生成する

本書は [SPECIFICATION.md](../SPECIFICATION.md) と [DESIGN.md](../DESIGN.md) の生成規約を、実際の入力テンプレートへ落とし込んだものである。

---

## 2. 運用原則
- 入力元は常に単一の正本とする。
- 仕様→設計のときは仕様書だけを渡し、既存実装の詳細を混ぜない。
- 設計→実装のときは設計書だけを渡し、既存コードの都合を先に混ぜない。
- 生成結果は要求 ID と責務境界で検収する。
- 仕様変更時は、仕様書、設計書、実装、テストを同一変更で更新する。

---

## 3. 仕様書から設計書を生成する手順
1. [templates/DESIGN_FROM_SPEC_TEMPLATE.md](../templates/DESIGN_FROM_SPEC_TEMPLATE.md) を開く。
2. SPECIFICATION.md の内容をテンプレートの入力欄へ貼る。
3. Copilot へ、仕様に存在しない機能を追加しないこと、要求 ID ごとに設計要素を起こすことを明示する。
4. 出力された設計書を DESIGN.md の章構成と比較する。
5. 要求トレーサビリティ表が FR-01 から FR-10 まで欠落なく埋まっていることを確認する。

### 3.1 検収観点
- 各 FR に対応する Feature 境界があるか
- Shared 配下へ責務を入れ過ぎていないか
- Composition が組み立て責務だけになっているか
- DTO、インターフェース、例外契約、テスト観点があるか

---

## 4. 設計書から実装を生成する手順
1. [templates/IMPLEMENTATION_FROM_DESIGN_TEMPLATE.md](../templates/IMPLEMENTATION_FROM_DESIGN_TEMPLATE.md) を開く。
2. DESIGN.md の内容をテンプレートの入力欄へ貼る。
3. Copilot へ、既存の責務境界を壊さず、1 クラス 1 ファイル規約と XML コメント規約を守ることを明示する。
4. 出力された実装をフォルダ構成、public API、テスト対象で検収する。
5. 実装後に dotnet test を実行し、トレーサビリティに対応するテストが通ることを確認する。

### 4.1 検収観点
- Design の公開インターフェースがそのままコードに落ちているか
- ViewModel が Feature 入口インターフェースだけに依存しているか
- Shared の実装が Feature を参照していないか
- テストが要求 ID ごとの観点を満たしているか

---

## 5. 差分レビュー手順
生成後は次の順序で差分を見る。

1. 仕様書と設計書の責務差分
2. 設計書と実装の public API 差分
3. 実装とテストの観点差分

差分が見つかった場合は、先に上流文書を修正し、その後に下流を再生成または修正する。

---

## 6. 禁止事項
- 仕様書にない機能を設計へ勝手に追加しない
- 設計書にない public API を実装へ勝手に追加しない
- 旧構成と新構成を併存させたまま正本扱いしない
- テストが通っていても仕様・設計との不整合を放置しない

---

## 7. このリポジトリでの正本
- 要件の正本: [SPECIFICATION.md](../SPECIFICATION.md)
- 設計の正本: [DESIGN.md](../DESIGN.md)
- 実装の正本: [MarketMonitor](../MarketMonitor)
- 生成手順の正本: [docs/COPILOT_WATERFALL_WORKFLOW.md](COPILOT_WATERFALL_WORKFLOW.md)