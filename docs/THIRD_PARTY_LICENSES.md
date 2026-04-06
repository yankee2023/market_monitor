# Third-Party Licenses

このファイルは、Tokyo Market Technical で直接参照している主要な外部ライブラリと、そのライセンス情報をまとめたものです。
依存関係を追加・更新した場合は、README.mdの案内とあわせて本ファイルを更新してください。

## 1. アプリケーション本体

| Package | Version | License |
| --- | --- | --- |
| ExcelDataReader | 3.8.0 | MIT |
| Microsoft.Extensions.DependencyInjection | 10.0.0 | MIT |
| Microsoft.Data.Sqlite | 9.0.0 | MIT |
| Serilog | 4.2.0 | Apache-2.0 |
| Serilog.Sinks.File | 6.0.0 | Apache-2.0 |

## 2. Console PoC

| Package | Version | License |
| --- | --- | --- |
| Serilog | 4.3.1 | Apache-2.0 |
| Serilog.Sinks.File | 7.0.0 | Apache-2.0 |

## 3. テストプロジェクト

| Package | Version | License |
| --- | --- | --- |
| coverlet.collector | 6.0.4 | MIT |
| coverlet.msbuild | 6.0.4 | MIT |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT |
| xunit | 2.9.3 | Apache-2.0 |
| xunit.runner.visualstudio | 3.1.4 | Apache-2.0 |

## 4. 運用ルール
- 新しい外部ライブラリを追加した場合は、本ファイルにパッケージ名、バージョン、ライセンスを追記する。
- 既存ライブラリのバージョンを更新した場合は、本ファイルの対応行も更新する。
- ライセンス条件の厳しいライブラリを導入する場合は、利用可否を事前に確認する。
- 本ファイルは、プロジェクトで直接参照している主要パッケージを対象とする。

## 5. ライセンス種別ごとの取り扱いルール

### MIT
- MITライセンスのライブラリを再配布する場合は、著作権表示とライセンス文を保持する。
- ソース配布・バイナリ配布のいずれでも、配布物にライセンス情報を含めることを確認する。
- 改変自体は可能だが、元ライセンスの表示を削除しない。
- 本プロジェクトでMITライセンスの依存を追加した場合は、本ファイルに対象パッケージを追記する。

### Apache-2.0
- Apache-2.0ライセンスのライブラリを再配布する場合は、著作権表示、ライセンス文、必要に応じてNOTICEファイルを保持する。
- 改変したファイルを再配布する場合は、変更を加えたことが分かる形を維持する。
- 商用利用自体は可能だが、特許条項を含むため、導入時にライセンス種別を確認する。
- 本プロジェクトでApache-2.0ライセンスの依存を追加した場合は、NOTICE要否も含めて確認する。

## 6. 注意事項
- ライセンス種別は各パッケージの公開情報に基づいて整理している。
- 継続的な依存更新時には、最新版のライセンス条件を必ず再確認すること。
