using AI.RAG.Console.Embedding;
using AI.RAG.Console.Models;
using AI.RAG.Console.Repository;
using AI.RAG.Console.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IEmbeddingGenerator, OllamaEmbeddingGenerator>();
builder.Services.AddSingleton<RAGRepository>(sp => 
{
    string connString = builder.Configuration.GetConnectionString("NeonRAG");
    return new RAGRepository(connString, sp.GetRequiredService<IEmbeddingGenerator>());
});

builder.Services.AddSingleton<RAGService>(sp => 
{
    var repository = sp.GetRequiredService<RAGRepository>();
    return new RAGService(repository);
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("add-text", async (RAGRepository repository, HttpContext context) =>
{
    var request = await context.Request.ReadFromJsonAsync<AddTextRequest>();

    if (string.IsNullOrEmpty(request?.Content))
    {
        return Results.BadRequest("Content cannot be null or empty.");
    }

    try
    {
        var textContent = await repository.AddTextContentAsync(request.Content);
        return Results.Ok(textContent);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.MapGet("get-content", async (RAGService service, string query) =>
{
    if (string.IsNullOrEmpty(query))
    {
        return Results.BadRequest("Query parameter is required");
    }

    var response = await service.GetAnswerAsync(query);

    return Results.Ok(new {query , response });
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
