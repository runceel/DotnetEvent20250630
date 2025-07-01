# DotnetEvent20250630

このリポジトリには、.NET と Microsoft Extensions AI、Azure OpenAI、Semantic Kernel を使用したサンプルプロジェクトが含まれています。

## プロジェクト一覧

### 1. ChatClientHelloWorld
Azure OpenAI と Azure AI Inference を使用したチャットクライアントのサンプル

### 2. EmbeddingHelloWorld  
Azure OpenAI の埋め込み（Embedding）機能を使用したベクトル生成のサンプル

### 3. SKAgent
Semantic Kernel を使用したAIエージェントのサンプル

### 4. SKMultiAgent
複数のAIエージェントを連携させるマルチエージェントシステムのサンプル

### 5. VectorDataApp
.NET Aspire を使用したベクトルデータベース（Qdrant）とAIチャットアプリケーション

### 6. EvaluationTest
AI応答の品質評価を行うテストプロジェクト

## 必要な設定

各プロジェクトを実行するには、Azure のサービスと適切な設定が必要です。

### Azure サービスの準備

1. **Azure OpenAI Service**
   - Azure Portal で Azure OpenAI Service リソースを作成
   - 必要なモデルをデプロイ：
     - `gpt-4o-mini`
     - `gpt-4.1` 
     - `o3`（プレビュー）
     - `text-embedding-3-large`
     - `text-embedding-3-small`

2. **Azure AI Foundry**
   - Azure AI Foundry でプロジェクトを作成
   - DeepSeek-V3-0324 モデルへのアクセスを設定

3. **Docker Desktop**（VectorDataApp用）
   - [Docker Desktop](https://www.docker.com/) をインストール
   - Qdrant コンテナが自動で起動されます

### プロジェクト別設定

各プロジェクトは User Secrets を使用して設定を管理します。Visual Studio で各プロジェクトを右クリックし、「ユーザー シークレットの管理」を選択して設定してください。

#### ChatClientHelloWorld

**必要な User Secrets:**
```json
{
  "AzureAIFoundry:Endpoint": "https://YOUR-AI-FOUNDRY-ENDPOINT",
  "AzureAIFoundry:DeepSeekEndpoint": "https://YOUR-DEEPSEEK-ENDPOINT", 
  "AzureAIFoundry:DeepSeekKey": "YOUR-DEEPSEEK-API-KEY"
}
```

#### EmbeddingHelloWorld

**必要な User Secrets:**
```json
{
  "AzureAIFoundry:Endpoint": "https://YOUR-AI-FOUNDRY-ENDPOINT"
}
```

#### SKAgent

**必要な User Secrets:**
```json
{
  "AzureAIFoundry:Endpoint": "https://YOUR-AI-FOUNDRY-ENDPOINT"
}
```

#### SKMultiAgent

**必要な User Secrets:**
```json
{
  "AzureAIFoundry:Endpoint": "https://YOUR-AI-FOUNDRY-ENDPOINT"
}
```

#### VectorDataApp.AppHost

**必要な User Secrets:**
```json
{
  "ConnectionStrings:openai": "Endpoint=https://YOUR-DEPLOYMENT-NAME.openai.azure.com;Key=YOUR-API-KEY"
}
```

#### EvaluationTest

**必要な User Secrets:**
```json
{
  "AZURE_OPENAI_ENDPOINT": "https://YOUR-DEPLOYMENT-NAME.openai.azure.com",
  "AZURE_INFERENCE_ENDPOINT": "https://YOUR-INFERENCE-ENDPOINT",
  "AZURE_INFERENCE_KEY": "YOUR-INFERENCE-API-KEY"
}
```

### 認証について

多くのプロジェクトで Azure CLI 認証（`AzureCliCredential`）を使用しています。事前に以下を実行してください：

```bash
az login
```

## 実行方法

### Visual Studio
1. 該当するプロジェクトをスタートアップ プロジェクトに設定
2. F5 キーで実行

### Visual Studio Code / コマンドライン
```bash
cd [プロジェクトフォルダ]
dotnet run
```

### VectorDataApp の場合
```bash
cd VectorDataApp/VectorDataApp.AppHost
dotnet run
```

## 注意事項

- Azure OpenAI Service のリソースには適切な課金設定が必要です
- プレビューモデル（o3）を使用する場合は、適切なAPIバージョンの設定が必要です
- VectorDataApp を実行する前に Docker Desktop が起動していることを確認してください
- User Secrets の設定値は実際のエンドポイントとキーに置き換えてください

## トラブルシューティング

### HTTPS証明書の問題
初回実行時にHTTPS証明書のエラーが発生する場合：
```bash
dotnet dev-certs https --trust
```

### Docker の問題
VectorDataApp でエラーが発生する場合は、Docker Desktop が起動していることを確認してください。

## ライセンス

MIT License - 詳細は [LICENSE.txt](LICENSE.txt) を参照してください。