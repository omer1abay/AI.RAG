namespace AI.RAG.Console.Repository;

public class TextContent
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = [];
}
