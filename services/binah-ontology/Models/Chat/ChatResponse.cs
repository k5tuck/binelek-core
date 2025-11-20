using System.Text.Json.Serialization;
using Binah.Ontology.Models.Base;
using Binah.Ontology.Models.Lineage;
namespace Binah.Ontology.Models.Chat;

public class ChatResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    [JsonPropertyName("references")]
    public List<EntityReference> References { get; set; } = new();

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}