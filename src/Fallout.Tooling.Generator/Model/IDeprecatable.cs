namespace Fallout.CodeGeneration.Model;

public interface IDeprecatable
{
    string DeprecationMessage { get; }

    IDeprecatable Parent { get; }
}

public static class DeprecatableExtensions
{
    public static bool IsDeprecated(this IDeprecatable deprecatable)
    {
        return deprecatable.DeprecationMessage != null || (deprecatable.Parent != null && deprecatable.Parent.IsDeprecated());
    }

    public static string GetDeprecationMessage(this IDeprecatable deprecatable)
    {
        var message = deprecatable.DeprecationMessage;
        if (!string.IsNullOrEmpty(message))
        {
            return message;
        }

        return deprecatable.Parent?.GetDeprecationMessage();
    }
}
