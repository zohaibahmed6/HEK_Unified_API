using System.Reflection;

namespace HekCoreApi.Api.Features.Canonical;

/// <summary>
/// Applies FR-4/FR-5 (HEK_UNIFIED_API_SPEC.md): a consumer states which fields it wants, and only
/// gets fields inside its own scope - anything else is silently dropped, never returned. Reflects
/// over the canonical record's public properties rather than hand-writing a projection per model
/// (NFR-8: minimal code, one filter usable for every future canonical resource).
/// </summary>
public static class FieldSelector
{
    /// <summary>
    /// Projects <paramref name="source"/> onto a field-name -> value map, restricted to the
    /// intersection of <paramref name="requestedFields"/> (caller-supplied, case-insensitive; null/empty
    /// means "all") and <paramref name="allowedFields"/> (server-enforced scope for this consumer).
    /// </summary>
    public static IReadOnlyDictionary<string, object?> Project(
        object source,
        IReadOnlyCollection<string>? requestedFields,
        IReadOnlyCollection<string> allowedFields)
    {
        var allowed = new HashSet<string>(allowedFields, StringComparer.OrdinalIgnoreCase);

        var wanted = requestedFields is { Count: > 0 }
            ? new HashSet<string>(requestedFields, StringComparer.OrdinalIgnoreCase)
            : null;

        var result = new Dictionary<string, object?>();
        foreach (var property in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!allowed.Contains(property.Name))
            {
                continue;
            }

            if (wanted is not null && !wanted.Contains(property.Name))
            {
                continue;
            }

            var camelCaseName = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            result[camelCaseName] = property.GetValue(source);
        }

        return result;
    }
}
