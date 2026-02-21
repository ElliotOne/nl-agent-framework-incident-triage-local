using Microsoft.Extensions.Configuration;

namespace IncidentTriageAgent.App;

public sealed class AgentAppConfig
{
    public string Provider { get; init; } = "ollama";
    public string BaseUrl { get; init; } = "http://localhost:11434/v1";
    public string ApiKey { get; init; } = "ollama";
    public string ModelId { get; init; } = "mistral:7b";
    public float Temperature { get; init; } = 0.1f;
    public int MaxOutputTokens { get; init; } = 800;

    public static AgentAppConfig Load(IConfiguration configuration)
    {
        var section = configuration.GetSection("Agent");

        return new AgentAppConfig
        {
            Provider = section["Provider"] ?? "ollama",
            BaseUrl = section["BaseUrl"] ?? "http://localhost:11434/v1",
            ApiKey = section["ApiKey"] ?? "ollama",
            ModelId = section["ModelId"] ?? "mistral:7b",
            Temperature = ParseFloat(section["Temperature"], 0.1f),
            MaxOutputTokens = ParseInt(section["MaxOutputTokens"], 800)
        };
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(BaseUrl))
        {
            throw new InvalidOperationException("Agent:BaseUrl is required.");
        }

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Agent:BaseUrl must be an absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(ModelId))
        {
            throw new InvalidOperationException("Agent:ModelId is required.");
        }

        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("Agent:ApiKey is required.");
        }

        if (MaxOutputTokens <= 0)
        {
            throw new InvalidOperationException("Agent:MaxOutputTokens must be greater than zero.");
        }
    }

    private static int ParseInt(string? value, int fallback) =>
        int.TryParse(value, out var parsed) ? parsed : fallback;

    private static float ParseFloat(string? value, float fallback) =>
        float.TryParse(value, out var parsed) ? parsed : fallback;
}
