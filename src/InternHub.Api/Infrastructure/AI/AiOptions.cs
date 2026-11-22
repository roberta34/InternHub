namespace InternHub.Api.Infrastructure.AI;

public sealed class AiOptions
{
    public string Provider { get; set; } = "Baseline";
    public AiFallbackOptions Fallback { get; set; } = new();
    public AiOllamaOptions Ollama { get; set; } = new();
}

public sealed class AiFallbackOptions
{
    public bool Enabled { get; set; }
}

public sealed class AiOllamaOptions
{
    public string? Endpoint { get; set; }
    public string? Model { get; set; }
}
