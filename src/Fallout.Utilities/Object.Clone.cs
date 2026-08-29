using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;

namespace Fallout.Common.Utilities;

[DebuggerNonUserCode]
[DebuggerStepThrough]
public static partial class ObjectExtensions
{
    /// <summary>
    /// Clones an object via <see cref="DataContractSerializer"/>.
    /// </summary>
    public static T Clone<T>(this T obj)
    {
        var serializer = new DataContractSerializer(typeof(T));
        using var memoryStream = new MemoryStream();
        serializer.WriteObject(memoryStream, obj);
        memoryStream.Seek(offset: 0, loc: SeekOrigin.Begin);
        return (T)serializer.ReadObject(memoryStream);
    }
}
