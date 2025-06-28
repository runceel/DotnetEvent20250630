using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Configuration;

namespace EvaluationTest;

// AOAIEvaluationTest クラスは、Azure OpenAI のチャットクライアントを使用して AI の応答を評価するテストクラスです。
[TestClass]
public sealed class AOAIEvaluationTest
{
    // 評価のための設定を保持する ReportingConfiguration
    private static ReportingConfiguration _reportingConfiguration;

    // 各種チャットクライアントのインスタンスを保持
    private static IChatClient _gpt4omini;
    private static IChatClient _gpt41;
    private static IChatClient _deepSeek;
    private static IChatClient _o3;

    // 静的コンストラクタで Azure OpenAI クライアントとチャットクライアントを初期化
    static AOAIEvaluationTest()
    {
        // ユーザーシークレットから Azure OpenAI のエンドポイントとキーを取得
        var config = new ConfigurationBuilder().AddUserSecrets<AOAIEvaluationTest>().Build();

        // Azure OpenAI クライアントを作成
        // gpt-4o-mini と gpt-4.1 モデルのクライアントを作成
        var openAIClient = new AzureOpenAIClient(new(config["AZURE_OPENAI_ENDPOINT"]!), new AzureCliCredential());
        _gpt4omini = openAIClient.GetChatClient("gpt-4o-mini").AsIChatClient();
        _gpt41 = openAIClient.GetChatClient("gpt-4.1").AsIChatClient();

        // o3 モデルのクライアントを作成
        var openAIClientForO3 = new AzureOpenAIClient(
            new(config["AZURE_OPENAI_ENDPOINT"]!), 
            new AzureCliCredential(), 
            options:new(AzureOpenAIClientOptions.ServiceVersion.V2024_12_01_Preview));
        _o3 = openAIClientForO3.GetChatClient("o3").AsIChatClient();

        // Azure AI Inference サービスのクライアントを作成
        var aiInference = new Azure.AI.Inference.ChatCompletionsClient(
            new(config["AZURE_INFERENCE_ENDPOINT"]!),
            new AzureKeyCredential(config["AZURE_INFERENCE_KEY"]!));
        _deepSeek = aiInference.AsIChatClient("DeepSeek-V3-0324")
            .AsBuilder()
            .Build();

        // 評価のための ReportingConfiguration を作成
        _reportingConfiguration = DiskBasedReportingConfiguration.Create(
            storageRootPath: "TestReports",
            evaluators: [
                // クエリに対する応答の関連性を評価
                new RelevanceEvaluator(), 
                // 期待する応答との一致度合いを評価
                new EquivalenceEvaluator()],
            chatConfiguration: new ChatConfiguration(_gpt41),
            enableResponseCaching: true,
            executionName: "AIEvaluationTest");
    }

    // gpt-4o-mini、gpt-4.1、o3、DeepSeek-V3-0324 の各モデルに対するテストメソッドを定義
    [TestMethod]
    public async Task TestGpt4oMini()
    {
        // gpt-4o-mini
        await using var scenarioRun = await _reportingConfiguration.CreateScenarioRunAsync(
            "AIEvaluationTest.TestGpt4oMini");
        await RunBasicScenarioAsync(_gpt4omini, scenarioRun);
    }

    [TestMethod]
    public async Task TestGpt41()
    {
        // gpt-4.1
        await using var scenarioRun = await _reportingConfiguration.CreateScenarioRunAsync(
            "AIEvaluationTest.TestGpt41");
        await RunBasicScenarioAsync(_gpt41, scenarioRun);
    }

    [TestMethod]
    public async Task TestO3()
    {
        // o3
        await using var scenarioRun = await _reportingConfiguration.CreateScenarioRunAsync(
            "AIEvaluationTest.TestO3");
        await RunBasicScenarioAsync(_o3, scenarioRun);
    }

    [TestMethod]
    public async Task TestDeepSeek()
    {
        // DeepSeek-V3-0324
        await using var scenarioRun = await _reportingConfiguration.CreateScenarioRunAsync(
            "AIEvaluationTest.TestDeepSeek");
        await RunBasicScenarioAsync(_deepSeek, scenarioRun);
    }

    // 基本的なシナリオを実行し、AIの応答を評価するメソッド
    private static async Task RunBasicScenarioAsync(IChatClient chatClient, ScenarioRun scenarioRun)
    {
        // 評価したい入力
        IList<ChatMessage> messages = [
            new(ChatRole.System, """
                 あなたは猫型AIアシスタントです。
                 猫っぽく振舞うためには語尾は必ず「にゃん」をつけてください。
                 ユーザーからの質問にシンプルに答えだけを回答してください。
                 """),
            new(ChatRole.User, """
                 押すと開くドアがあります。
                 そのドアから部屋に入り、体を真後ろに回転させてドアの方を向き、そのままドアを通って部屋から出ます。
                 その後、その姿勢のまま後ろ向きに部屋に入ります。
                 この時ドアを閉めるためには押せばいいでしょうか、引けばいいでしょうか。
                 """)];

        // AI を使って応答を取得
        var response = await chatClient.GetResponseAsync(messages);

        // 応答の内容を評価
        EvaluationResult result = await scenarioRun.EvaluateAsync(
            messages, 
            response, 
            // EquivalenceEvaluator のための期待する応答を設定
            [new EquivalenceEvaluatorContext("押せばいいにゃん。")]);

        // 評価結果を確認
        NumericMetric relevance = result.Get<NumericMetric>(RelevanceEvaluator.RelevanceMetricName);
        Assert.IsFalse(relevance.Interpretation!.Failed, relevance.Reason);
        Assert.IsTrue(relevance.Interpretation.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);
        NumericMetric equivalence = result.Get<NumericMetric>(EquivalenceEvaluator.EquivalenceMetricName);
        Assert.IsFalse(equivalence.Interpretation!.Failed, equivalence.Reason);
        Assert.IsTrue(equivalence.Interpretation.Rating is EvaluationRating.Good or EvaluationRating.Exceptional);
    }
}
