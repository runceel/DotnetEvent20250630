#pragma warning disable SKEXP0110
using Azure.AI.Agents.Persistent;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;

namespace SKAgent;
// 各種エージェントを生成するためのプロバイダクラス
public class AgentProvider(IConfiguration configuration)
{
    // Azure AI Foundry Agent Service のエージェント（情報収集用）を作成する
    public async Task<Agent> CreateResearcherAgentAsync()
    {
        // Azure AI Foundry のクライアントを初期化
        var aiProjectClient = new AIProjectClient(
            new(configuration["AzureAIFoundry:AgentEndpoint"]!),
            new AzureCliCredential());
        var agentClient = aiProjectClient.GetPersistentAgentsClient();
        // エージェント定義を取得
        var agentDefinition = await agentClient.Administration.GetAgentAsync(configuration["AzureAIFoundry:AgentId"]);
        // AzureAIAgent を生成し返す
        return new AzureAIAgent(
            agentDefinition,
            agentClient)
        {
            Description = "記事のセクションを作成するために必要な情報を収集するエージェントです。",
        };
    }

    // ChatCompletionAgent を使用して、記事を作成するための計画を立てるエージェントを作成する
    public Task<Agent> CreatePlannerAgentAsync(Kernel baseKernel, string serviceId, string modelId)
    {
        // カーネルを複製または新規作成
        var kernel = baseKernel?.Clone() ?? new();
        var templateFactory = new KernelPromptTemplateFactory();
        // 記事プランナーエージェントを生成し返す
        return Task.FromResult<Agent>(new ChatCompletionAgent
        {
            Name = AgentNames.Planner,
            Instructions = """
                <BasicInstructions>
                    あなたはユーザーから与えられたテーマの記事を作成するための計画を立てるエージェントです。
                </BasicInstructions>
                <Details>
                    記事全体のタイトルとセクションのタイトルと、各セクションを書くためにインターネットで調べるキーワードを含む計画を立てます。
                </Details>
                """,
            Description = "記事を作成するための計画を立てます。",
            Kernel = kernel,
            Arguments = new(new OpenAIPromptExecutionSettings
            {
                ServiceId = serviceId,
                ModelId = modelId,
                ResponseFormat = typeof(ReportPlan),
            }),
        });
    }

    // ChatCompletionAgent を使用して、記事のセクションを作成するエージェントを作成する
    public Task<Agent> CreateSectionWriterAsyncAsync(Kernel baseKernel, string serviceId, string modelId)
    {
        // カーネルを複製または新規作成
        var kernel = baseKernel?.Clone() ?? new();
        var templateFactory = new KernelPromptTemplateFactory();
        // セクションライターエージェントを生成し返す
        return Task.FromResult<Agent>(new ChatCompletionAgent
        {
            Name = AgentNames.SectionWriter,
            Instructions = """
                <BasicInstructions>
                    あなたは記事の1セクションを作成するエージェントです。
                </BasicInstructions>
                <Details>
                    セクションを記載する上での参考情報はユーザーから与えられます。
                    参考情報の内容のみを使って、記事のセクションを作成してください。
                </Details>
                """,
            Kernel = kernel,
            Description = "記事のタイトルと、記事のセクションタイトルと、参考情報を元に記事のセクションを作成します。",
            Arguments = new(new OpenAIPromptExecutionSettings
            {
                ServiceId = serviceId,
                ModelId = modelId,
                ResponseFormat = typeof(Section),
            }),
        });
    }

    // ChatCompletionAgent を使用して、記事の最終化を行いファイルに保存するエージェントを作成する
    public Task<Agent> CreateReportFinalizerAsync(Kernel baseKernel, string serviceId, string modelId)
    {
        // カーネルを複製または新規作成し、ファイル保存用プラグインを追加
        var kernel = baseKernel?.Clone() ?? new();
        kernel.Plugins.AddFromType<FileSystemPlugin>();

        var templateFactory = new KernelPromptTemplateFactory();
        // レポートファイナライザーエージェントを生成し返す
        return Task.FromResult<Agent>(new ChatCompletionAgent
        {
            Name = AgentNames.ReportFinalizer,
            Instructions = """
                <BasicInstructions>
                    あなたは記事の仕上げを行いファイルに保存を行うエージェントです。
                </BasicInstructions>
                <Details>
                    与えられたタイトルとセクションをまとめて、最終的な記事を作成します。
                    与えられたセクションに「はじめに」と「まとめ」のセクションが含まれていない場合は、追加してください。
                    作成した記事は、Markdown 形式で記述し、ファイルシステムに保存します。
                    ファイルシステムに保存を行うとファイル名が返ってくるので、ユーザーにそのファイル名を伝えてください。
                </Details>
                <Examples>
                    report-20250601-123456.md に保存しました。
                </Examples>
                """,
            Kernel = kernel,
            Description = "記事のタイトルと、セクションをまとめて、最終的な記事を作成します。",
            Arguments = new(new OpenAIPromptExecutionSettings
            {
                ServiceId = serviceId,
                ModelId = modelId,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            }),
        });
    }
}

// ファイルに保存するためのプラグイン
class FileSystemPlugin
{
    [KernelFunction, Description("ファイルシステムに記事を保存します。ファイル名は自動で作成されます。")]
    [return: Description("保存したファイルの名前。")] 
    public async Task<string> SaveReportAsync(
        [Description("保存する記事の内容。")] string text)
    {
        // ファイル名を自動生成し、記事を保存
        var fileName = $"report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.md";
        await File.WriteAllTextAsync(fileName, text);
        return fileName;
    }
}

// レポートのプラン（記事全体の構成情報）
public record ReportPlan(
    [property: Description("The title of the report.")]
    string Title,
    [property: Description("The sections of the report.")]
    SectionPlan[] Sections);

// セクションのプラン（各セクションの構成情報）
public record SectionPlan(
    [property: Description("The title of the section.")]
    string Title, 
    [property: Description("The search keywords to write this content.")]
    string SearchKeywords);

// セクションの内容
public record Section(
    [property: Description("The title of the section.")]
    string Title,
    [property: Description("The content of the section.")]
    string Content);
