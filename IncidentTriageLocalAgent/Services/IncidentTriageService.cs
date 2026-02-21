using System.Text.Json;
using IncidentTriageAgent.Domain;
using IncidentTriageAgent.Llm;

namespace IncidentTriageAgent.Services;

public sealed class IncidentTriageService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly OllamaChatClient _chatClient;

    public IncidentTriageService(OllamaChatClient chatClient)
    {
        _chatClient = chatClient;
    }

    public async Task<IncidentTriageReport> RunStructuredAsync(string prompt, CancellationToken ct = default)
    {
        var response = await _chatClient.ChatAsync(
        [
            new ChatMessage("system", BuildStructuredInstructions()),
            new ChatMessage("user", prompt)
        ],
        ct);

        return ParseStructuredReport(response);
    }

    public Task<string> RunTextAsync(string prompt, CancellationToken ct = default) =>
        _chatClient.ChatAsync(
        [
            new ChatMessage("system", BuildTextInstructions()),
            new ChatMessage("user", prompt)
        ],
        ct);

    public IAsyncEnumerable<string> RunStreamingTextAsync(string prompt, CancellationToken ct = default) =>
        _chatClient.ChatStreamingAsync(
        [
            new ChatMessage("system", BuildTextInstructions()),
            new ChatMessage("user", prompt)
        ],
        ct);

    private static IncidentTriageReport ParseStructuredReport(string response)
    {
        var payload = ExtractJsonObject(response);

        var report = JsonSerializer.Deserialize<IncidentTriageReport>(payload, SerializerOptions);
        if (report is null)
        {
            throw new InvalidOperationException("Model returned an empty structured response.");
        }

        return report;
    }

    private static string ExtractJsonObject(string input)
    {
        var start = input.IndexOf('{');
        var end = input.LastIndexOf('}');

        if (start < 0 || end <= start)
        {
            throw new InvalidOperationException("Structured response did not contain a valid JSON object.");
        }

        return input[start..(end + 1)];
    }

    private static string BuildStructuredInstructions() =>
        """
        You are an Incident Triage Agent for real production operations.

        Your job is to produce practical, safe, and concise incident triage guidance.

        Non-negotiable rules:
        1. Never invent observability data, logs, or metrics that were not provided.
        2. If key information is missing, list it explicitly under missing data.
        3. Prioritize immediate risk reduction and customer impact containment.
        4. Keep actions executable by an on-call engineer.
        5. Use severity levels P1, P2, P3, or P4 only.

        Domain choices:
        - API
        - Database
        - Queue
        - Networking
        - Compute
        - Storage
        - Identity
        - ThirdPartyDependency
        - Unknown

        Return ONLY valid JSON with this exact shape:
        {
          "incidentSummary": "string",
          "severity": "P1|P2|P3|P4",
          "primaryDomain": "API|Database|Queue|Networking|Compute|Storage|Identity|ThirdPartyDependency|Unknown",
          "customerImpact": "string",
          "topLikelyCauses": ["string", "string", "string"],
          "immediateActions15Minutes": ["string", "string", "string"],
          "stabilizationActions60Minutes": ["string", "string", "string"],
          "escalationTargets": ["string", "string"],
          "stakeholderUpdateDraft": "string",
          "missingCriticalData": ["string"]
        }
        """;

    private static string BuildTextInstructions() =>
        """
        You are an Incident Triage Agent for real production operations.

        Your job is to produce practical, safe, and concise incident triage guidance.

        Non-negotiable rules:
        1. Never invent observability data, logs, or metrics that were not provided.
        2. If key information is missing, list it explicitly under missing data.
        3. Prioritize immediate risk reduction and customer impact containment.
        4. Keep actions executable by an on-call engineer.
        5. Use severity levels P1, P2, P3, or P4 only.

        Domain choices:
        - API
        - Database
        - Queue
        - Networking
        - Compute
        - Storage
        - Identity
        - ThirdPartyDependency
        - Unknown

        Return concise Markdown.
        """;
}
