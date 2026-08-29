using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace Fallout.Common;

/// <summary>
/// Provides a collection of common assertion methods.
/// </summary>
[DebuggerNonUserCode]
[DebuggerStepThrough]
public static class Assert
{
    /// <summary>
    /// Throws an exception with the specified message and inner-exception.
    /// </summary>
    public static void Fail(string message, Exception exception = null)
    {
        throw new Exception(message, exception);
    }

    /// <summary>
    /// Asserts that the condition is true with an optional exception message. If no message is provided, the argument expression is used.
    /// </summary>
    public static void True(
        bool condition,
        string message = null,
        [CallerArgumentExpression("condition")]
        string argumentExpression = null)
    {
        if (!condition)
        {
            throw new ArgumentException(message ?? "Expected condition to be true", message == null ? argumentExpression : null);
        }
    }

    /// <summary>
    /// Asserts that the condition is false with an optional exception message. If no message is provided, the argument expression is used.
    /// </summary>
    public static void False(
        bool condition,
        string message = null,
        [CallerArgumentExpression("condition")]
        string argumentExpression = null)
    {
        if (condition)
        {
            throw new ArgumentException(message ?? "Expected condition to be false", message == null ? argumentExpression : null);
        }
    }

    /// <summary>
    /// Asserts that the object is not <c>null</c> with an optional exception message. If no message is provided, the argument expression is used.
    /// </summary>
    public static T NotNull<T>(
        this T obj,
        string message = null,
        [CallerArgumentExpression("obj")] string argumentExpression = null)
        where T : class
    {
        if (obj == null)
        {
            throw new ArgumentException(
                message ?? $"Expected object of type '{typeof(T).FullName}' to be not null",
                message == null ? argumentExpression : null);
        }

        return obj;
    }

    /// <summary>
    /// Asserts that the object is not <c>null</c> with an optional exception message. If no message is provided, the argument expression is used.
    /// </summary>
    public static T? NotNull<T>(
        this T? obj,
        string message = null,
        [CallerArgumentExpression("obj")] string argumentExpression = null)
        where T : struct
    {
        if (obj == null)
        {
            throw new ArgumentException(
                message ?? $"Expected object of type '{typeof(T).FullName}' to be not null",
                message == null ? argumentExpression : null);
        }

        return obj;
    }

    /// <summary>
    /// Asserts that the string is not <c>null</c> or empty with an optional exception message. If no message is provided, the argument expression is used.
    /// </summary>
    public static string NotNullOrEmpty(
        this string str,
        string message = null,
        [CallerArgumentExpression("str")] string argumentExpression = null)
    {
        if (string.IsNullOrEmpty(str))
        {
            throw new ArgumentException(message ?? "Expected string to be not null or empty",
                message == null ? argumentExpression : null);
        }

        return str;
    }

    /// <summary>
    /// Asserts that the string is not <c>null</c> or has only whitespace characters with an optional exception message. If no message is provided, the argument expression is used.
    /// </summary>
    public static string NotNullOrWhiteSpace(
        this string str,
        string message = null,
        [CallerArgumentExpression("str")] string argumentExpression = null)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            throw new ArgumentException(message ?? "Expected string to be not null or whitespace",
                message == null ? argumentExpression : null);
        }

        return str;
    }

    /// <summary>
    /// Asserts that the collection is not empty with an optional exception message. If no message is provided, the argument expression is used.
    /// </summary>
    public static void NotEmpty<T>(
        IReadOnlyCollection<T> collection,
        string message = null,
        [CallerArgumentExpression("collection")]
        string argumentExpression = null)
    {
        if (collection.NotNull(argumentExpression: argumentExpression).Count == 0)
        {
            throw new ArgumentException(message ?? "Expected collection to be not empty",
                message == null ? argumentExpression : null);
        }
    }

    /// <summary>
    /// Asserts that the collection is empty with an optional exception message. If no message is provided, the argument expression is used.
    /// </summary>
    public static void Empty<T>(
        IReadOnlyCollection<T> collection,
        string message = null,
        [CallerArgumentExpression("collection")]
        string argumentExpression = null)
    {
        if (collection.NotNull(argumentExpression: argumentExpression).Count > 0)
        {
            throw new ArgumentException(message ?? "Expected collection to be empty",
                message == null ? argumentExpression : null);
        }
    }

    /// <summary>
    /// Asserts that the collection has specified number of elements an optional exception message. If no message is provided, the argument expression is used.
    /// </summary>
    public static void Count<T>(
        IReadOnlyCollection<T> collection,
        int length,
        string message = null,
        [CallerArgumentExpression("collection")]
        string argumentExpression = null)
    {
        if (collection.NotNull(argumentExpression: argumentExpression).Count != length)
        {
            throw new ArgumentException(message ?? $"Expected collection to have length of {length}",
                message == null ? argumentExpression : null);
        }
    }

    /// <summary>
    /// Asserts that the collection has only a single element with an optional exception message. If no message is provided, the argument expression is used.
    /// </summary>
    public static void HasSingleItem<T>(
        IReadOnlyCollection<T> collection,
        string message = null,
        [CallerArgumentExpression("collection")]
        string argumentExpression = null)
    {
        Count(collection, length: 1, message, argumentExpression);
    }

    /// <summary>
    /// Asserts that the file exists with an optional exception message. If no message is provided, the argument expression is used.
    /// </summary>
    public static void FileExists(string path, string message = null,
        [CallerArgumentExpression("path")] string argumentExpression = null)
    {
        if (!File.Exists(path.NotNull(argumentExpression)))
        {
            throw new ArgumentException(message ?? $"Expected file to exist: {path}",
                message == null ? argumentExpression : null);
        }
    }

    /// <summary>
    /// Asserts that the directory exists with an optional exception message. If no message is provided, the argument expression is used.
    /// </summary>
    public static void DirectoryExists(string path, string message = null,
        [CallerArgumentExpression("path")] string argumentExpression = null)
    {
        if (!Directory.Exists(path.NotNull(argumentExpression)))
        {
            throw new ArgumentException(message ?? $"Expected directory to exist: {path}",
                message == null ? argumentExpression : null);
        }
    }
}
