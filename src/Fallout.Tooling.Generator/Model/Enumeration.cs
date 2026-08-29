using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Fallout.CodeGeneration.Model;

public class Enumeration : IDeprecatable
{
    [JsonIgnore]
    public Tool Tool { get; set; }

    [JsonIgnore]
    public IDeprecatable Parent => Tool;

    [JsonRequired]
    [RegularExpression(RegexPatterns.Name)]
    [Description("Name of the enumeration.")]
    public string Name { get; set; }

    [JsonRequired]
    [Description("The enumeration values.")]
    public List<string> Values { get; set; }

    [Description("Obsolete message. Enumeration is marked as obsolete when specified.")]
    public string DeprecationMessage { get; set; }
}
