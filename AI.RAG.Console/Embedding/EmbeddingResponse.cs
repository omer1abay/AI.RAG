using System.Text.Json.Serialization;

namespace AI.RAG.Console.Embedding;

public class EmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = Array.Empty<float>();
}