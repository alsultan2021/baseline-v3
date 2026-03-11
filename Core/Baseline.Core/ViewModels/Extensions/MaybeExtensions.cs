using CSharpFunctionalExtensions;

namespace Baseline.Core.Extensions;

/// <summary>
/// Extensions for Maybe type.
/// </summary>
public static class MaybeExtensions
{
    /// <summary>
    /// TryGetValue extension for Maybe
    /// </summary>
    public static bool TryGetValue<T>(this Maybe<T> maybe, out T value)
    {
        if (maybe.HasValue)
        {
            value = maybe.Value;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>
    /// AsNullable for Maybe
    /// </summary>
    public static T? AsNullable<T>(this Maybe<T> maybe) where T : class
        => maybe.HasValue ? maybe.Value : null;

    /// <summary>
    /// GetValueOrDefault for Maybe
    /// </summary>
    public static T GetValueOrDefault<T>(this Maybe<T> maybe, T defaultValue = default!)
        => maybe.HasValue ? maybe.Value : defaultValue;
}
