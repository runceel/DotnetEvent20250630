#pragma warning disable SKEXP0080
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using SKAgent;
using System.Text.Json;

namespace SKProcess;

// 共通イベント名を定義する静的クラス
public static class CommonEventNames
{
    // プロセス開始イベント名
    public const string Start = nameof(Start);
}

// 記事のテーマを入力するステップを表すクラス
public class InputThemeStep : KernelProcessStep
{
    [KernelFunction]
    public string InputTheme(KernelProcessStepContext context)
    {
        // ユーザーに記事のテーマを入力させる
        Console.Write("記事のテーマを入力してください: ");
        var theme = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(theme))
        {
            // テーマが空の場合は例外を投げる
            throw new InvalidOperationException("テーマは空にできません。");
        }

        // 入力されたテーマを返す
        return theme;
    }
}

// 記事の計画を立てるステップを表すクラス
public class PlanningStep : KernelProcessStep
{
    // 記事タイトル公開イベント名
    public const string PublishTitle = nameof(PublishTitle);
    // セクションプラン公開イベント名
    public const string PublishSectionPlan = nameof(PublishSectionPlan);

    [KernelFunction]
    public async Task CreatePlanAsync(
        KernelProcessStepContext context, 
        string theme,
        [FromKernelServices(AgentNames.Planner)]CreateAgent createPlannerAgent)
    {
        // 記事のテーマに基づきプランを作成する
        Console.WriteLine($"{theme} についてのプランを考えています…");
        var planner = await createPlannerAgent();
        var plannerResult = await planner.InvokeAsync(theme).FirstAsync();
        // プランのJSONをReportPlan型にデシリアライズ
        var plan = JsonSerializer.Deserialize<ReportPlan>(
            plannerResult.Message.Content ?? "", 
            AIJsonUtilities.DefaultOptions);
        if (plan == null) throw new InvalidOperationException("Plan deserialization failed.");

        // スレッドを削除
        await plannerResult.Thread.DeleteAsync();

        // 記事タイトルとセクションを出力
        Console.WriteLine($"記事タイトル: {plan.Title}, セクション: {string.Join(", ", plan.Sections.Select(x => x.Title))}");
        // タイトルとセクションプランをイベントとして発行
        await context.EmitEventAsync(PublishTitle, plan.Title);
        await context.EmitEventAsync(PublishSectionPlan, plan.Sections);
    }
}

// セクションを作成するステップを表すクラス
public class WriteSectionStep : KernelProcessStep
{
    // セクション公開イベント名
    public const string PublishSection = nameof(PublishSection);

    [KernelFunction]
    public async Task WriteSectionAsync(
        KernelProcessStepContext context,
        SectionPlan sectionPlan,
        [FromKernelServices(AgentNames.Researcher)] CreateAgent createResearcher,
        [FromKernelServices(AgentNames.SectionWriter)] CreateAgent createSectionWriter)
    {
        // セクション作成のための情報を検索
        Console.WriteLine($"{sectionPlan.SearchKeywords} について検索しています…");
        var researcher = await createResearcher();
        var researcherResult = await researcher.InvokeAsync(sectionPlan.SearchKeywords).FirstAsync();
        await researcherResult.Thread.DeleteAsync();

        // セクション本文を作成
        Console.WriteLine($"{sectionPlan.Title} セクションを書いています…");
        var sectionWriter = await createSectionWriter();
        var sectionWriterResult = await sectionWriter.InvokeAsync(
            $"""
            以下の情報を参考に、{sectionPlan.Title} セクションを作成してください。
            {researcherResult.Message.Content}

            セクションの内容は、Markdown 形式で記述してください。
            """).FirstAsync();
        await sectionWriterResult.Thread.DeleteAsync();

        // セクションのJSONをSection型にデシリアライズ
        var section = JsonSerializer.Deserialize<Section>(
            sectionWriterResult.Message.Content ?? "", 
            AIJsonUtilities.DefaultOptions);
        if (section == null) throw new InvalidOperationException("Section deserialization failed.");

        // セクションをイベントとして発行
        await context.EmitEventAsync(PublishSection, section);
    }
}

// 記事の最終稿を作成するステップを表すクラス
public class FinalizeReportStep : KernelProcessStep
{
    [KernelFunction]
    public async Task FinalizeReportAsync(
        KernelProcessStepContext context,
        Kernel kernel,
        string title,
        Section[] sections,
        [FromKernelServices(AgentNames.ReportFinalizer)] CreateAgent createReportFinalizer)
    {
        // 記事の最終稿を作成
        Console.WriteLine($"最終稿を書いています…");
        var reportFinalizer = await createReportFinalizer();
        var finalResult = await reportFinalizer.InvokeAsync($"""
            以下の内容を元に、最終的な記事を作成してください。

            <タイトル>{title}</タイトル>
            <セクション>{JsonSerializer.Serialize(sections, AIJsonUtilities.DefaultOptions)}</セクション>
            """).FirstAsync();
        await finalResult.Thread.DeleteAsync();
        // 最終稿の内容を出力
        Console.WriteLine(finalResult.Message.Content);
    }
}

