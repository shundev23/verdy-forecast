# アーキテクチャとファイルの置き場所

## 全体像

```
[ブラウザ（React）]
      │ 同じドメイン。Cookie＋CSRF用ヘッダー
      ▼
[App Service F1：VerdyForecast.Api] ── ログイン（OpenID Connect）──> [Entra ID]
      │ マネージドID
      ├──> [Azure SQL（無料枠）]   試合・スタッツ・採点・予想などの本体
      ├──> [Blob Storage]          取得データの原本、学習用スナップショット
      ├──> [Key Vault]             API-Football の APIキー
      └──> [Azure AI Language F0] 戦術タグの候補づくり

[Azure Functions（Flex Consumption）：VerdyForecast.Functions]
      タイマー → 「試合が終わった」メッセージ → Storageのキュー
      → 結果の取り込み → 原本の保存 → レーティングと予想の再計算 → 答え合わせ
      │ マネージドID
      └──> SQL / Blob / Queue / Key Vault / API-Football（許可リストのホストだけ）
```

- 本体（API）はクリーンアーキテクチャ、試合後の処理の流れはイベント駆動にする。
- API と Functions は、同じ Domain と Application を使う。入口が2つあっても、ルールは1か所にまとまる。

## フォルダ構成

```
verdy-forecast/
├─ .claude/skills/verdy-forecast-dev/   開発用スキル（このフォルダ）
├─ .github/
│  └─ dependabot.yml                    NuGet と npm の更新
├─ pipelines/                           Azure DevOps のパイプライン定義
│  ├─ ci.yml                            PRごと：ビルド、テスト、チェック
│  └─ cd.yml                            main：インフラとアプリのデプロイ
├─ docs/
│  ├─ work-instructions.md              作業指示書
│  ├─ evaluation-procedure.md           評価手順書
│  ├─ threat-model.md                   脅威モデル（Phase 1 で作る）
│  └─ evaluation-records/               評価の記録
├─ infra/                               Bicep
│  ├─ main.bicep
│  ├─ modules/
│  └─ ps-rule.yaml                      PSRule for Azure の設定
├─ src/
│  ├─ VerdyForecast.Domain/             ルールと計算。外部への依存なし
│  ├─ VerdyForecast.Application/        ユースケースとポート（インターフェース）
│  ├─ VerdyForecast.Infrastructure/     SQL、Blob、キュー、外部API、Key Vault、AI Language
│  ├─ VerdyForecast.Api/                ASP.NET Core（BFF、APIのエンドポイント、Reactの配信）
│  ├─ VerdyForecast.Functions/          Azure Functions（試合後の処理）
│  └─ web/                              React＋TypeScript（Vite）
├─ tests/
│  ├─ VerdyForecast.Domain.Tests/
│  ├─ VerdyForecast.Application.Tests/
│  ├─ VerdyForecast.Infrastructure.IntegrationTests/
│  ├─ VerdyForecast.Api.IntegrationTests/
│  ├─ VerdyForecast.Functions.IntegrationTests/
│  ├─ VerdyForecast.ArchitectureTests/  依存ルールの自動チェック
│  └─ e2e/                              Playwright
├─ analysis/                            自分のPCでの分析・実験（Jupyter）
│  └─ data/                             ※ .gitignore に入れる。コミットしない
├─ Directory.Build.props
├─ Directory.Packages.props
├─ global.json
├─ .editorconfig
├─ .gitignore
└─ VerdyForecast.slnx
```

## 層と依存のルール

| 層（プロジェクト） | 参照してよいもの | 置くもの | 置かないもの |
|---|---|---|---|
| Domain | なし（.NET の標準ライブラリだけ） | エンティティ、値オブジェクト、ルール（予想のロック、MVPの決定など）、計算（Elo、ポアソン、評価指標） | DB、HTTP、ファイル、設定、ログ、外部ライブラリ |
| Application | Domain | ユースケース、ポート（リポジトリや外部データ取得のインターフェース）、入力の検証、DTO | EF Core、Azure SDK、HttpClient の具体的な実装 |
| Infrastructure | Application、Domain | ポートの実装（EF Core、Blob、Queue、API-Football、AI Language、Key Vault） | 画面やエンドポイントの処理、ビジネスルール |
| Api | Application、Infrastructure（DI の登録だけ） | エンドポイント、認証（BFF）、セキュリティヘッダー、Reactの配信 | ビジネスルール、SQL |
| Functions | Application、Infrastructure（DI の登録だけ） | トリガー（タイマー、キュー）と、ユースケースの呼び出し | ビジネスルール、SQL |

- Api と Functions が Infrastructure を参照してよいのは、`Program.cs` での DI の登録だけ。エンドポイントや関数の中から Infrastructure の型を直接使わない。
- 依存ルールは `tests/VerdyForecast.ArchitectureTests`（ArchUnitNET）で自動チェックする。ルールを破るコードはテストで落ちる。

## ポート（Application に置くインターフェース）の例

| ポート | 役割 | 実装（Infrastructure） |
|---|---|---|
| `IMatchRepository` など各リポジトリ | 試合、予想、レビューなどの保存と取得 | EF Core |
| `IMatchResultSource` | 全チームの試合結果の取得 | API-Football（だめなら Wikipedia や CSV に差し替える） |
| `IRawDataStore` | 取得データの原本の保存とハッシュの記録 | Blob Storage |
| `ITagSuggester` | メモとスタッツから戦術タグの候補を出す | Azure AI Language |
| `IEventPublisher` | 「試合が終わった」などのメッセージを送る | Storage のキュー |

- 時刻は `TimeProvider`（.NET 標準）を使う。テストでは `FakeTimeProvider` に差し替える。
- 取得元を差し替えるときは、Infrastructure に新しい実装を足して、DI の登録を変えるだけで済むようにする。

## 試合後の処理の流れ（イベント駆動）

1. タイマーで、終わった試合がないかを確認する（間隔は作業指示書の要決定）。
2. 終わった試合があれば、試合ごとにメッセージをキューに送る。
3. キューのメッセージを受けて、順番に処理する。
   1. 結果の取り込み（原本を Blob に保存 → 形式を検証 → SQL に反映）
   2. Elo レーティングと、次の試合の予想を計算し直す
   3. 答え合わせ（モデルと自分の予想の評価指標を記録）
4. 各処理が終わったら、次の処理のメッセージを送る。

メッセージの形（個人のデータは入れない）:

```json
{
  "messageId": "0f8fad5b-d9cb-469f-a165-70867728950e",
  "type": "MatchFinished",
  "matchId": 12345,
  "occurredAt": "2026-11-28T08:00:00Z"
}
```

- 同じメッセージを二重に処理しないように、処理済みの `messageId` を SQL に記録して、記録済みなら何もしない。
- 何回か失敗したメッセージはポイズンキューに移る。ポイズンキューにメッセージが入ったら、アラートを出す。
- 処理の途中で失敗しても中途半端な状態が残らないように、SQLへの書き込みはトランザクションでまとめる。

## データの置き場所

| 置き場所 | 中身 | ルール |
|---|---|---|
| Azure SQL | チーム、選手、試合、レーティング、モデルの予想、自分の予想、スタッツ、採点、MVP、タグ、メモ、評価、処理済みメッセージ、監査ログ | 時刻は UTC で保存して、画面で日本時間に変換する |
| Blob `raw` コンテナ | API から取得したデータの原本（JSON） | `raw/{取得元}/{yyyy}/{MM}/{dd}/{ID}.json`。上書きしない。SHA-256 を SQL に記録する |
| Blob `snapshots` コンテナ | 学習・分析に使ったデータのスナップショット（CSV） | どのデータでどのモデルを作ったか、後から辿れるようにする |
| 自分のPC `analysis/data/` | 分析用に落としたデータ | コミットしない（.gitignore に入れる） |

## 名前の付け方

- C# の名前空間はフォルダ構成に合わせる（例: `VerdyForecast.Domain.Matches`）。
- クラスやメソッドの名前は英語。コメントとドキュメントは日本語でよい。
- TypeScript のコンポーネントは PascalCase、それ以外のファイルは kebab-case。
- Azure のリソース名は `種類の略-verdy-forecast-環境` を基本にする（例: `rg-verdy-forecast`、`app-verdy-forecast-prod`）。ストレージアカウントのようにハイフンを使えないものは、ハイフンを抜いて短くする。
