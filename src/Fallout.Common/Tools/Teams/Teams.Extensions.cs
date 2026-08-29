using System.Text.Json.Serialization;

namespace Fallout.Common.Tools.Teams;

public partial class TeamsMessage
{
    [JsonPropertyName("@type")]
    internal string Type => "MessageCard";

    [JsonPropertyName("@context")]
    internal string Context => "http://schema.org/extensions";
}
