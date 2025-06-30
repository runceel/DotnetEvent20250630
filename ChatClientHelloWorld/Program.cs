using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// ログ出力用のファクトリを作成（コンソールに詳細レベルで出力）
var loggerFactory = LoggerFactory.Create(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace));
// ユーザーシークレットから設定を読み込む
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

// Azure OpenAI のクライアントを初期化
var aoai = new AzureOpenAIClient(
    new(configuration["AzureAIFoundry:Endpoint"]!),
    new AzureCliCredential());
// Azure OpenAI のチャットクライアントを構築（gpt-4.1）
IChatClient aoaiChatClient = aoai.GetChatClient("gpt-4.1")
    .AsIChatClient()
    .AsBuilder()
    .UseLogging(loggerFactory) // ログ出力を有効化
    .UseFunctionInvocation()  // ツール呼び出しを有効化
    .Build();

// Azure AI Inference サービスのクライアントを初期化
var aiInference = new ChatCompletionsClient(
    new(configuration["AzureAIFoundry:DeepSeekEndpoint"]!),
    new AzureKeyCredential(configuration["AzureAIFoundry:DeepSeekKey"]!));
// Inference サービスのチャットクライアントを構築（DeepSeek-V3-0324）
IChatClient aiInferenceChatClient = aiInference.AsIChatClient("DeepSeek-V3-0324")
    .AsBuilder()
    .UseLogging(loggerFactory) // ログ出力を有効化
    .UseFunctionInvocation()  // ツール呼び出しを有効化
    .Build();

// 2つのチャットクライアントでAIに問い合わせ
await InvokeAIAsync(aoaiChatClient);
Console.WriteLine("=====================================");
Console.WriteLine("");
await InvokeAIAsync(aiInferenceChatClient);

// チャットクライアントにツールを渡して問い合わせを行う関数
async Task InvokeAIAsync(IChatClient chatClient)
{
    // 現在日時を返すツールを作成
    var getTodayTool = AIFunctionFactory.Create(
        () => DateTimeOffset.Now,
        name: "getToday",
        description: "Returns today's date and time.");

    // チャットオプションを設定（ツール自動選択・ツールリスト指定）
    var chatOptions = new ChatOptions
    {
        ToolMode = ChatToolMode.Auto,
        Tools = [getTodayTool],
    };

    // AIに「今日は何日ですか？」と問い合わせ、応答を取得
    var chatResponse = await chatClient.GetResponseAsync(
        "今日は何日ですか？",
        chatOptions);
    // 応答を出力
    Console.WriteLine(chatResponse.Text);
}