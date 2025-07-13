using AI.RAG.Console.Models;
using AI.RAG.Console.Repository;
using System.Text;
using System.Text.Json;

namespace AI.RAG.Console.Services;

public class RAGService(RAGRepository retriever)
{
    private readonly RAGRepository _retriever = retriever;
    private readonly HttpClient _httpClient = new();
    private readonly string _model = "qwen2.5-coder:latest";
    private readonly string _ollamaUrl = "http://127.0.0.1:11434";


    public async Task<object> GetAnswerAsync(string query)
    {
        List<string> relevantContent = await _retriever.GetRelevantContentAsync(query);

        if (relevantContent.Count == 1 && relevantContent[0] == "No relevant data found")
        {
            return new 
            { 
                Context = "No relevant data found",
                Response = "Sorry, I couldn't find any relevant information to answer your question."
            };
        }

        string context = string.Join("\n\n---\n\n", relevantContent);

        var reqBody = new 
        { 
            model = _model,
            prompt = $"""
        You are a strict AI assistant. You MUST answer ONLY using the provided context. 
        If the answer is not in the context, respond with "I don't know. No relevant data found."

        Context:
        {context}

        Question: {query}
        """,
            stream = false
        };

        var response = await _httpClient.PostAsync(new Uri($"{_ollamaUrl}/api/generate"),
                                                         new StringContent(JsonSerializer.Serialize(reqBody), Encoding.UTF8, "application/json"), 
                                                         cancellationToken: default);
        if (!response.IsSuccessStatusCode)
        {
            return new
            {
                Context = context,
                Response = $"Error: {await response.Content.ReadAsStringAsync()}"
            };
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var serializationOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var completionResponse = JsonSerializer.Deserialize<CompletionResponse>(responseContent, serializationOptions);

        return new
        {
            Context = context,
            Response = completionResponse?.Response ?? "I don't know. No relevant data found."
        };

    }


}
