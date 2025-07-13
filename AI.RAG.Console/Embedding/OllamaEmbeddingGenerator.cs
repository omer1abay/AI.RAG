
using System.Text;
using System.Text.Json;

namespace AI.RAG.Console.Embedding;

public class OllamaEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly string _model = "qwen2.5-coder:latest";
    private readonly string _ollamaUrl = "http://127.0.0.1:11434";
    private readonly HttpClient _httpClient = new();

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var reqBody = new { model = _model, prompt = text };

        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentNullException("text cannot be null");
        }

        var response = await _httpClient.PostAsync(new Uri($"{_ollamaUrl}/api/embeddings"), 
                                                         new StringContent(JsonSerializer.Serialize(reqBody), Encoding.UTF8, "application/json"),
                                                         cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Ollama API error: {await response.Content.ReadAsStringAsync()}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var serializationOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseContent, serializationOptions);

        if (embeddingResponse?.Embedding == null || embeddingResponse.Embedding.Length == 0)
        {
            throw new Exception("Failed to generate embedding.");
        }

        return embeddingResponse.Embedding;

    }
}
