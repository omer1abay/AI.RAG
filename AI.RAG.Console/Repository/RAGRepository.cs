using AI.RAG.Console.Embedding;
using Npgsql;
using System.Globalization;

namespace AI.RAG.Console.Repository;

public class RAGRepository(string connString, IEmbeddingGenerator embeddingGenerator)
{
    private readonly string _connString = connString;
    private readonly IEmbeddingGenerator _embeddingGenerator = embeddingGenerator;

    public async Task<TextContent> AddTextContentAsync(string content, CancellationToken cancellationToken = default)
    {

        if (string.IsNullOrEmpty(content))
        {
            throw new ArgumentNullException(nameof(content), "Content cannot be null or empty.");
        }
        var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(content, cancellationToken);
        var textContent = new TextContent
        {
            Content = content,
            Embedding = embedding
        };

        using var connection = new NpgsqlConnection(_connString);
        await connection.OpenAsync(cancellationToken);

        string insertQuery = "INSERT INTO text_contents (content, embedding) VALUES (@Content, @Embedding) RETURNING id;";
        using var command = new NpgsqlCommand(insertQuery, connection);
        command.Parameters.AddWithValue("Content", textContent.Content);
        command.Parameters.AddWithValue("Embedding", textContent.Embedding);
        
        var id = await command.ExecuteScalarAsync(cancellationToken);
        textContent.Id = Convert.ToInt32(id);

        return textContent;
    }

    public async Task<List<string>> GetRelevantContentAsync(string text, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(text, cancellationToken);
        var conn = new NpgsqlConnection(_connString);
        await conn.OpenAsync(cancellationToken);

        string query = @"
            SELECT content 
            FROM text_contents 
            WHERE embedding <-> CAST(@Embedding as vector) > 0.7
            ORDER BY embedding <-> CAST(@Embedding as vector) 
            LIMIT 5;";

        using var command = new NpgsqlCommand(query, conn);
        string embeddingString = $"[{string.Join(",", embedding.Select(v => v.ToString("G", CultureInfo.InvariantCulture)))}]";
        command.Parameters.AddWithValue("Embedding", embeddingString);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var results = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetString(0));
        }


        return results.Count != 0 ? results : ["No relevant data found"];
    }

}
