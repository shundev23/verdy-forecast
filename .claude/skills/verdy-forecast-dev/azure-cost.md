# 予算のルール（使ってよいリソースと、使わないリソース）

- 予算は月1,000円。できるだけ0円にする。
- Cost Management の予算アラート（50%、80%、100%）を設定してある。ただし、アラートは通知が来るだけで、課金は止まらない。
- 新しいリソースを作ったら、翌日に Cost Management で、そのリソースの費用を確認する。
- リージョンは東日本（Japan East）。

## 使ってよいリソース（許可リスト）

| サービス | プランや設定 | 注意 |
|---|---|---|
| App Service | Free（F1）、Linux | 1日のCPU時間は60分まで。使わないとスリープする。上限を超えると、その日は止まる |
| Azure Functions | Flex Consumption（オンデマンドのみ） | 常時起動（always ready）のインスタンスは0にする。Linux の従来の従量課金プランは .NET 10 に対応していないので使わない |
| Storage アカウント | Standard、LRS、ホット | Functions 用とデータ用の2つ。Blob とキューを使う |
| Azure SQL Database | 無料オファー（サーバーレス、月10万 vCore 秒、32GB） | 無料枠を使い切ったときの動きは「翌月まで自動で一時停止」にする。「課金して使い続ける」にしない |
| Key Vault | Standard | 操作の回数ぶんだけ課金（数円程度）。RBAC で権限を管理する |
| Log Analytics と Application Insights | 従量課金（Analytics Logs） | 月5GBまで無料。1日の取り込み上限を設定する |
| Azure AI Language | Free（F0） | 月5,000件まで。超えると止まるだけで課金はされない |
| Microsoft Entra ID | Free | 自分専用のユーザー1人と、アプリの登録 |
| Defender for Cloud | 無料の部分（Foundational CSPM）だけ | 有料のプランは有効にしない |
| Cost Management | 予算とアラート | 無料 |

## 使わないリソース

| リソース | 理由 |
|---|---|
| Azure Container Registry | 何もしなくても毎日課金される |
| プライベートエンドポイント、NAT Gateway、Application Gateway、Front Door、Azure Firewall、VPN Gateway | 時間単位で課金される |
| 有料の固定IPアドレス（Standard のパブリック IP） | 時間単位で課金される |
| App Service の Basic（B1）以上のプラン、Functions の Premium・専用プラン | 予算を超える |
| Static Web Apps の Standard プラン | 予算を超える。画面は App Service から配信する |
| Azure SQL の無料オファー以外のデータベース | 予算を超える |
| Defender for Cloud の有料プラン、Microsoft Sentinel | 予算を超える |
| Service Bus の Standard・Premium、Cosmos DB（無料枠以外）、Azure Cache for Redis | 予算を超える。キューは Storage のキューを使う |
| Azure OpenAI など、生成AIのモデルのデプロイ | v1 では使わない |
| Azure Machine Learning | v3 でお試しするまで使わない（下を参照） |

## Azure Machine Learning を使うとき（v3 のお試し）

- ユーザーが明示的に許可したときだけ使う。期間を決めて試し、終わったら削除する。
- 計算用のマシン（コンピューティングインスタンス）は、停止していてもディスク代と1日約0.33ドルのロードバランサー代がかかる。使い終わったら停止ではなく削除する。
- 独自の実行環境（カスタム環境）を作ると、Container Registry が自動で作られて、毎日課金される。用意されている環境（キュレーション環境）を使う。
- ワークスペースを削除しても、一緒に作られた Storage、Key Vault、Application Insights、Container Registry は残って課金が続く。これらも削除する。

## 無料枠を超えたときの動き

| サービス | 超えたときの動き | アプリの動き |
|---|---|---|
| App Service F1 | その日は止まる | 翌日まで使えない（自分1人なので許容する） |
| Azure SQL 無料オファー | 翌月まで一時停止 | 503 と「今月の無料枠を使い切った」というメッセージを返す |
| Azure AI Language F0 | 止まる | タグの候補を出さず、手でタグを選ぶ |
| API-Football 無料プラン | その日は呼べない | 前回のデータで表示して、「データが古い」と知らせる |
| Log Analytics | 1日の上限で取り込みが止まる | ログが一部欠ける（アラートも届かなくなることがあるので注意） |
