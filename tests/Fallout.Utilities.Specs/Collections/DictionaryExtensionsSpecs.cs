using System.Collections;
using System.Collections.Generic;
using Fallout.Common.Utilities.Collections;
using FluentAssertions;
using Xunit;

namespace Fallout.Common.Specs;

public class DictionaryExtensionsSpecs
{
    [Fact]
    public static void ToGeneric()
    {
        var sourceDictionary = new Dictionary<string, string>
        {
            { "key", "value" },
            { "key2", "value2" }
        };

        IDictionary dict = sourceDictionary;
        dict.ToGeneric<string, string>().Should().Equal(sourceDictionary);
    }
}
