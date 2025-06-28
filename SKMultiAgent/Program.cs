#pragma warning disable SKEXP0080
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0110
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SKAgent;
using SKProcess;

// ユーザーシークレットから設定を読み込む
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

// DIコンテナのサービス登録
var services = new ServiceCollection();
// 設定をシングルトンで登録
services.AddSingleton<IConfiguration>(configuration);
// Azure OpenAIのチャットモデルを2種類登録
services.AddAzureOpenAIChatCompletion("o3",
    configuration["AzureAIFoundry:Endpoint"]!,
    new AzureCliCredential(),
    serviceId: "azure",
    modelId: "o3");
services.AddAzureOpenAIChatCompletion("gpt-4.1",
    configuration["AzureAIFoundry:Endpoint"]!,
    new AzureCliCredential(),
    serviceId: "azure",
    modelId: "gpt-4.1");
// Semantic Kernelの登録
services.AddKernel();

// エージェントプロバイダーの登録
services.AddSingleton<AgentProvider>();
// 各種エージェントのファクトリをKeyedで登録
services.AddKeyedTransient<CreateAgent>(AgentNames.Planner, 
    (sp, _) =>
    {
        var provider = sp.GetRequiredService<AgentProvider>();
        // 記事プランナーエージェントの生成関数を登録
        return async () => await provider.CreatePlannerAgentAsync(sp.GetRequiredService<Kernel>(), "azure", "o3");
    });
services.AddKeyedTransient<CreateAgent>(AgentNames.SectionWriter,
    (sp, _) =>
    {
        var provider = sp.GetRequiredService<AgentProvider>();
        // セクションライターエージェントの生成関数を登録
        return async () => await provider.CreateSectionWriterAsyncAsync(
            sp.GetRequiredService<Kernel>(), 
            "azure", 
            "gpt-4.1");
    });
services.AddKeyedTransient<CreateAgent>(AgentNames.Researcher,
    (sp, _) =>
    {
        var provider = sp.GetRequiredService<AgentProvider>();
        // リサーチャーエージェントの生成関数を登録
        return async () => await provider.CreateResearcherAgentAsync();
    });
services.AddKeyedTransient<CreateAgent>(AgentNames.ReportFinalizer,
    (sp, _) =>
    {
        var provider = sp.GetRequiredService<AgentProvider>();
        // レポートファイナライザーエージェントの生成関数を登録
        return async () => await provider.CreateReportFinalizerAsync(
            sp.GetRequiredService<Kernel>(), 
            "azure", 
            "gpt-4.1");
    });

// サービスプロバイダーをビルド
var serviceProvider = services.BuildServiceProvider();

// Kernelインスタンスを取得
var kernel = serviceProvider.GetRequiredService<Kernel>();

// プロセスビルダーを作成（記事作成プロセス）
ProcessBuilder processBuilder = new("CreateReport");

// 各ステップをプロセスに追加
var inputThemeStep = processBuilder.AddStepFromType<InputThemeStep>(); // テーマ入力ステップ
var planningStep = processBuilder.AddStepFromType<PlanningStep>();     // 計画作成ステップ
var writeSectionStep = processBuilder.AddMapStepFromType<WriteSectionStep>(); // セクション作成ステップ
var finalizeReportStep = processBuilder.AddStepFromType<FinalizeReportStep>(); // 最終化ステップ

// プロセスのフローを定義
// 最初はテーマの入力イベントから開始
processBuilder.OnInputEvent(CommonEventNames.Start)
    .SendEventTo(new(inputThemeStep));

// テーマ入力が終わったらプラン作成ステップへ
inputThemeStep.OnFunctionResult()
    .SendEventTo(new ProcessFunctionTargetBuilder(planningStep));

// プラン作成後、タイトルは最終化ステップへ送信
planningStep.OnEvent(PlanningStep.PublishTitle)
    .SendEventTo(new ProcessFunctionTargetBuilder(finalizeReportStep, parameterName: "title"));
// セクションプランはセクション作成ステップへ送信
planningStep.OnEvent(PlanningStep.PublishSectionPlan)
    .SendEventTo(new ProcessFunctionTargetBuilder(writeSectionStep));

// セクション作成が完了したら、最終化ステップへ送信
writeSectionStep.OnEvent(WriteSectionStep.PublishSection)
    .SendEventTo(new ProcessFunctionTargetBuilder(finalizeReportStep, parameterName: "sections"));

// 最終化ステップが完了したらプロセスを終了
finalizeReportStep.OnFunctionResult().StopProcess();

// プロセスをビルドし、開始イベントで実行
var process = processBuilder.Build();
await using var context = await process.RunToEndAsync(kernel, new() { Id = CommonEventNames.Start });
