#pragma warning disable SKEXP0110
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using SKAgent;

// ユーザーシークレットから設定を読み込む
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

// Semantic Kernel のビルダーを作成
var builder = Kernel.CreateBuilder();
// Azure OpenAI のチャットモデルを2種類登録
builder.AddAzureOpenAIChatCompletion("o3",
    configuration["AzureAIFoundry:Endpoint"]!,
    new AzureCliCredential(),
    serviceId: "azure",
    modelId: "o3");
builder.AddAzureOpenAIChatCompletion("gpt-4.1",
    configuration["AzureAIFoundry:Endpoint"]!,
    new AzureCliCredential(),
    serviceId: "azure",
    modelId: "gpt-4.1");

// Kernel インスタンスを生成
var kernel = builder.Build();

// エージェントプロバイダーを生成
var agentProvider = new AgentProvider(configuration);

// 各種エージェントを生成
var researcherAgent = await agentProvider.CreateResearcherAgentAsync(); // 情報収集エージェント
var plannerAgent = await agentProvider.CreatePlannerAgentAsync(kernel, "azure", "o3"); // 記事プランナーエージェント
var sectionWriter = await agentProvider.CreateSectionWriterAsyncAsync(kernel, "azure", "gpt-4.1"); // セクション作成エージェント
var reportFinalizer = await agentProvider.CreateReportFinalizerAsync(kernel, "azure", "gpt-4.1"); // 記事最終化エージェント

// プランナーエージェントで記事の計画を作成
await RunAgentAsync(plannerAgent, "C# .NET Blazor の入門記事");

// リサーチャーエージェントで情報を収集
//await RunAgentAsync(researcherAgent, "C# .NET Blazor");

// セクションライターエージェントでセクションを作成
//await RunAgentAsync(sectionWriter, """
//    Blazor 入門という記事のBlazorの概要セクションを作成してください。
//    以下の情報を参考にしてください。
//    Blazor は、C# と .NET を使用して、クライアント側の Web アプリケーションを構築するためのフレームワークです。
//    Blazor は、WebAssembly を使用してブラウザで実行されるため、JavaScript を使用せずに、C# のコードを直接ブラウザで実行できます。
//    Blazor は、サーバー側とクライアント側の両方で実行できるため、柔軟なアプリケーション開発が可能です。
//    Blazor は、コンポーネントベースの開発モデルを採用しており、再利用可能な UI コンポーネントを作成できます。
//    """);

// 最終稿を作成
//await RunAgentAsync(reportFinalizer, """
//    タイトル：犬の可愛さ
//    本文：小型犬は吠えている様子も可愛い。
//    """);

// 指定したエージェントにメッセージを送り、結果を出力する関数
static async Task RunAgentAsync(Agent agent, string userMessage)
{
    // エージェント名を出力
    Console.WriteLine($"================================ {agent.Name} ======================================");
    AgentThread? agentThread = null;
    // エージェントにメッセージを送信し、最初の応答を取得
    var result = await agent.InvokeAsync(userMessage, agentThread).FirstAsync();
    agentThread = result.Thread;

    // スレッドが存在すれば削除
    if (agentThread != null)
    {
        await agentThread.DeleteAsync();
    }

    // 応答メッセージの各アイテムを出力
    foreach (var item in result.Message.Items)
    {
        var output = item switch
        {
            TextContent text => text.Text,
            AnnotationContent annotation => $"[{annotation.Label} {annotation.Title}]({annotation.ReferenceId})",
            _ => item.ToString()
        };

        Console.WriteLine(output);
    }

    Console.WriteLine();
}
