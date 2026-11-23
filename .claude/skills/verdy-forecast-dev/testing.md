# テストの書き方とコマンド

## テストの種類と置き場所

| 種類 | 置き場所 | ツール | 何を確かめるか |
|---|---|---|---|
| ドメインの単体テスト | `tests/VerdyForecast.Domain.Tests` | xUnit v3、Shouldly | ルールと計算（予想のロック、MVP、Elo、ポアソン、評価指標、キックオフ時点のデータだけを使う仕組み） |
| ユースケースの単体テスト | `tests/VerdyForecast.Application.Tests` | xUnit v3、Shouldly、NSubstitute | ユースケースの流れ、入力の検証、ポートの呼び出し |
| 依存ルールのテスト | `tests/VerdyForecast.ArchitectureTests` | ArchUnitNET | 層の依存の向き（例: Domain が ほかの層を参照していない） |
| インフラの結合テスト | `tests/VerdyForecast.Infrastructure.IntegrationTests` | Testcontainers（SQL Server、Azurite） | EF Core のマッピングとマイグレーション、Blob、キュー |
| API の結合テスト | `tests/VerdyForecast.Api.IntegrationTests` | WebApplicationFactory | 認証（401）、CSRF 用ヘッダー、ロック後の拒否、セキュリティヘッダー、ProblemDetails、レート制限 |
| Functions の結合テスト | `tests/VerdyForecast.Functions.IntegrationTests` | Testcontainers（Azurite） | 同じメッセージの二重処理がないこと、リトライ、ポイズンキュー |
| 画面の部品のテスト | `src/web` の中（`*.test.tsx`） | Vitest、React Testing Library、MSW | 表示、入力、API エラー時の表示 |
| E2E テスト | `tests/e2e` | Playwright（TypeScript） | 主な画面の流れ（予想の入力、試合後のレビューの入力、答え合わせ） |
| ミューテーションテスト | Domain が対象 | Stryker.NET | テストがバグを見つけられるか（評価手順書で使う） |

ツールのバージョンは、導入するときに最新の安定版を確認して、`Directory.Packages.props` と `package.json` で固定する。アサーションには FluentAssertions を使わない（v8 から商用ライセンスになったため）。

## .NET 10 のテスト実行方式

- .NET 10 SDK の `dotnet test` は、Microsoft.Testing.Platform（MTP）のモードで動かす。リポジトリ直下の `global.json` に次を書く。

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  },
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

- `sdk.version` は、インストールした .NET 10 SDK に合わせて調整する。
- xUnit v3 は 4.0 から、既定で MTP v2 を使う。
- カバレッジは MTP 用の拡張（`Microsoft.Testing.Extensions.CodeCoverage`）で取る。Coverlet の従来の使い方は MTP では使えない。

## よく使うコマンド

```powershell
# ビルド（警告はエラー扱い）
dotnet build

# .NET のテストを全部
dotnet test

# プロジェクトを指定する
dotnet test --project tests/VerdyForecast.Domain.Tests

# カバレッジを取る
dotnet test --coverage --coverage-output-format cobertura

# ミューテーションテスト（Domain。MTP 用のランナーは Stryker.NET 4.13 からのプレビュー機能）
cd tests/VerdyForecast.Domain.Tests
dotnet stryker --test-runner mtp

# 画面のテスト
cd src/web
npm run test

# E2E（Testing 環境のアプリを起動してから）
cd tests/e2e
npx playwright test
```

コマンドのオプションは、ツールのバージョンで変わることがある。エラーになったら `--help` で確認する。

## テストを書くときのルール

- 時刻は `TimeProvider` を受け取り、テストでは `FakeTimeProvider` を使う。キックオフの1秒前、ちょうど、1秒後を必ず試す。
- テストデータは、作り物だけにする。API から取った本物のデータや、自分のメモをテストに入れない（公開リポジトリのため）。
- テストから Azure のサービスや外部の API に接続しない。外部 API の応答は、作り物の JSON で再現する。
- ランダムな値を使う計算は、シードを固定する。
- 1つのテストでは1つのことを確かめる。名前は「何を・どんな条件で・どうなるか」が分かるようにする（例: `Submit_AfterKickoff_IsRejected`）。
- Testcontainers を使うテストは、Docker Desktop が起動している必要がある。CI では Microsoft がホストする Linux のエージェントで動かす。

## データリーケージのテスト

予測に使うデータが「キックオフ時点で分かっていたもの」だけかを、テストで確かめる。

1. 過去のある試合について、予測を作る。
2. その試合のキックオフより後のデータ（その試合の結果や、試合後の採点）を足す。
3. もう一度、同じ試合の予測を作る。
4. 1 と 3 の予測が完全に同じであることを確かめる。

## セキュリティのテスト（API の結合テストに入れる）

- 未ログインで `/api/...` を呼ぶと 401 になる（ログイン画面へのリダイレクトにならない）。
- `X-CSRF` ヘッダーがない POST は拒否される。
- キックオフ後に予想を変えようとすると拒否され、DB の値が変わらない。
- 応答にセキュリティヘッダー（CSP など）が付いている。
- エラーの応答にスタックトレースや例外のメッセージが入っていない。
- セッションの Cookie に `Secure`、`HttpOnly`、`SameSite` が付いていて、名前が `__Host-` で始まる。
- 書き込み系のエンドポイントを短い時間に大量に呼ぶと、429 になる。
- `Testing` 以外の環境でテスト用の偽ログインを有効にすると、起動時に失敗する。

## E2E テストのログイン

- 本物のログインには MFA があるので、E2E では自動化できない。
- E2E は `Testing` 環境で起動したアプリに対して行い、テスト用の偽ログインを使う。
- 本番に対しては、ログインなしでできる確認だけを行う（未ログインで 401 になる、ヘッダーが付いている、など）。
