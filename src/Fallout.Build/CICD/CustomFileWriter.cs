using System;
using System.IO;
using System.Linq;

namespace Fallout.Common.Utilities;

/// <summary>
/// Low-level, indentation-aware line writer backing the CI/CD configuration generators
/// (<c>ConfigurationEntity.Write</c> in <c>Fallout.Common.CI.*</c>).
/// </summary>
/// <remarks>
/// Deliberately hand-rolled, not replaced by a serializer (e.g. YamlDotNet). TeamCity and
/// SpaceAutomation targets emit a <b>Kotlin DSL</b>, which no YAML/JSON serializer can produce; the
/// YAML targets (GitHub Actions, Azure Pipelines, AppVeyor) need exact control over comments, quoting,
/// and indentation that a serializer would silently rewrite. See
/// <see href="https://github.com/ChrisonSimtian/Fallout/blob/main/docs/dependencies-kept.md">docs/dependencies-kept.md</see>.
/// </remarks>
public class CustomFileWriter
{
    private readonly StreamWriter _streamWriter;
    private readonly int _indentationFactor;
    private readonly string _commentPrefix;
    private int _indentation;

    public CustomFileWriter(StreamWriter streamWriter, int indentationFactor, string commentPrefix)
    {
        _streamWriter = streamWriter;
        _indentationFactor = indentationFactor;
        _commentPrefix = commentPrefix;
    }

    public void WriteLine(string text = null)
    {
        _streamWriter.WriteLine(
            text != null
                ? $"{' '.Repeat(_indentation * _indentationFactor)}{text}"
                : string.Empty);
    }

    public void WriteComment(string text = null)
    {
        WriteLine($"{_commentPrefix} {text}".TrimEnd());
    }

    public void Write(Action<CustomFileWriter> writer)
    {
        writer(this);
    }

    public IDisposable Indent()
    {
        return DelegateDisposable.CreateBracket(
            () => _indentation++,
            () => _indentation--);
    }
}
