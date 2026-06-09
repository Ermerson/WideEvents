namespace WideEvents.Core.Builder;

/// <summary>
/// Converts a flat key-value dictionary with dotted keys into a nested dictionary hierarchy.
/// Keys like <c>user.id</c> become <c>{ "user": { "id": ... } }</c>.
/// </summary>
internal static class WideEventStructureBuilder
{
    internal static Dictionary<string, object?> Build(IReadOnlyDictionary<string, object?> flat)
    {
        var root = new Dictionary<string, object?>();
        foreach (var (key, value) in flat)
            SetNested(root, key, value);
        return root;
    }

    internal static void SetNested(Dictionary<string, object?> root, string key, object? value)
    {
        var segments = key.Split('.');
        var current = root;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            if (current.TryGetValue(segments[i], out var existing)
                && existing is Dictionary<string, object?> child)
            {
                current = child;
            }
            else
            {
                var created = new Dictionary<string, object?>();
                current[segments[i]] = created;
                current = created;
            }
        }

        current[segments[^1]] = value;
    }
}
