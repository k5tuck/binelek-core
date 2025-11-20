using System.Text.Json.Serialization;
namespace Binah.Ontology.Models.Chat;

public class ChatRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("conversationId")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("includeOntology")]
    public bool IncludeOntology { get; set; } = true;

    [JsonPropertyName("userId")]
    public string? UserId { get; set; }
}