using System.Text;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace IncidentTriageAgent.Llm;

public sealed class OllamaChatClient
{
    private readonly OllamaApiClient _client;
    private readonly string _model;

    public OllamaChatClient(Uri baseUri, string model)
    {
        _model = model;
        _client = new OllamaApiClient(baseUri)
        {
            SelectedModel = model
        };
    }

    public async Task<string> ChatAsync(IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var sb = new StringBuilder();

        await foreach (var chunk in _client.ChatAsync(BuildRequest(messages), ct))
        {
            if (!string.IsNullOrWhiteSpace(chunk?.Message?.Content))
            {
                sb.Append(chunk.Message.Content);
            }
        }

        return sb.ToString().Trim();
    }

    public async IAsyncEnumerable<string> ChatStreamingAsync(
        IEnumerable<ChatMessage> messages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var chunk in _client.ChatAsync(BuildRequest(messages), ct))
        {
            if (!string.IsNullOrWhiteSpace(chunk?.Message?.Content))
            {
                yield return chunk.Message.Content;
            }
        }
    }

    private ChatRequest BuildRequest(IEnumerable<ChatMessage> messages)
    {
        var chatMessages = messages.Select(m => new Message(MapRole(m.Role), m.Content)).ToList();

        return new ChatRequest
        {
            Model = _model,
            Messages = chatMessages
        };
    }

    private static ChatRole MapRole(string role) => role.ToLowerInvariant() switch
    {
        "system" => ChatRole.System,
        "assistant" => ChatRole.Assistant,
        _ => ChatRole.User
    };
}

public sealed record ChatMessage(string Role, string Content);
