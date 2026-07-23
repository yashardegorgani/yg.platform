using System.Text.Json;

namespace YG.Modules.Workflow.Contracts;

/// <summary>
/// Shared context-reading helpers for activities: dotted-path resolution
/// ("request.item" = context["request"].item) and tolerant int reading
/// (form fields arrive as strings, designers may record real numbers).
/// </summary>
public static class ContextPaths
{
    public static bool TryResolvePath(
        this IReadOnlyDictionary<string, JsonElement> ctx, string path, out JsonElement element)
    {
        element = default;
        var segments = path.Split('.');
        if (!ctx.TryGetValue(segments[0], out element))
            return false;

        foreach (var segment in segments.Skip(1))
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(segment, out element))
                return false;
        }
        return true;
    }

    public static bool TryReadInt(this JsonElement element, out int value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out value),
            JsonValueKind.String => int.TryParse(element.GetString(), out value),
            _ => false,
        };
    }
}