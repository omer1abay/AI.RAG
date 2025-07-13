namespace AI.RAG.Console.Embedding;

public interface IEmbeddingGenerator
{
    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken);
}
