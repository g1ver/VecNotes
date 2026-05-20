using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Pgvector;

namespace VecNotes.Services;

public interface IEmbeddingService
{
    Task<Vector> EmbedAsync(string text);
}

public class OllamaEmbeddingService(HttpClient http, IConfiguration config) : IEmbeddingService
{
    private readonly string _model = config["Ollama:Model"] ?? "nomic-embed-text";

    public async Task<Vector> EmbedAsync(string text)
    {
        var response = await http.PostAsJsonAsync("/api/embed", new { model = _model, input = text });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EmbedResponse>();
        if (result?.Embeddings is not { Length: > 0 })
            throw new InvalidOperationException("Ollama returned no embeddings");
        return new Vector(result.Embeddings[0]);
    }

    record EmbedResponse([property: JsonPropertyName("embeddings")] float[][] Embeddings);
}
