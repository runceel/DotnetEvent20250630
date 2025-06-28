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

// 埋め込み生成器（Embedding Generator）を構築
IEmbeddingGenerator<string, Embedding<float>> aoaiEmbeddingGenerator = aoai
    .GetEmbeddingClient("text-embedding-3-large") // モデル名を指定
    .AsIEmbeddingGenerator()
    .AsBuilder()
    .UseLogging(loggerFactory) // ログ出力を有効化
    .Build();

// それぞれのテキストから埋め込みベクトルを生成
var ringo = await aoaiEmbeddingGenerator.GenerateVectorAsync("リンゴ");
var apple = await aoaiEmbeddingGenerator.GenerateVectorAsync("赤くて丸い果物");
var neko = await aoaiEmbeddingGenerator.GenerateVectorAsync("猫");
var cat = await aoaiEmbeddingGenerator.GenerateVectorAsync("我儘で自分勝手だけどとても可愛い動物");

// コサイン類似度を計算して出力
Console.WriteLine($"リンゴ - 赤くて丸い果物: {CosineSimilarity(ringo, apple)}");
Console.WriteLine($"我儘で自分勝手だけどとても可愛い動物 - 赤くて丸い果物: {CosineSimilarity(cat, apple)}");
Console.WriteLine($"猫 - 我儘で自分勝手だけどとても可愛い動物: {CosineSimilarity(neko, cat)}");

// ReadOnlyMemory<float> を 2 つ受け取ってコサイン類似度を返す関数
static float CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
{
    // ベクトルの長さが一致しない場合は例外
    if (a.Length != b.Length)
        throw new ArgumentException("ベクトルの長さが一致しません。");
    float dotProduct = 0;
    float normA = 0;
    float normB = 0;
    // 各要素ごとに内積とノルムを計算
    for (int i = 0; i < a.Length; i++)
    {
        dotProduct += a.Span[i] * b.Span[i];
        normA += a.Span[i] * a.Span[i];
        normB += b.Span[i] * b.Span[i];
    }
    // コサイン類似度を返す
    return dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));
}