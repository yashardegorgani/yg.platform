using System.Text.Json;
using YG.Modules.Workflow.Domain.Definition;

namespace YG.Modules.Workflow.Engine;

/// <summary>
/// Answers one question: is this condition satisfied by the instance context?
/// It reads state; it never computes state. If a workflow needs a calculation,
/// a STEP performs it and records the result — then a condition reads THAT.
/// </summary>
internal static class ConditionEvaluator
{
    public static bool Evaluate(ConditionDefinition condition,
        Dictionary<string, JsonElement> context)
    {
        var actual = ResolvePath(condition.Field, context);

        // Missing data never satisfies a condition. No exceptions thrown:
        // an unanswered question is "no", not a crash.
        if (actual is null || condition.Value is null) 
            return false;

        return condition.Op switch
        {
            "eq" => JsonEquals(actual.Value, condition.Value.Value),
            "ne" => !JsonEquals(actual.Value, condition.Value.Value),
            "gt" => Compare(actual.Value, condition.Value.Value) is > 0,
            "lt" => Compare(actual.Value, condition.Value.Value) is < 0,
            "gte" => Compare(actual.Value, condition.Value.Value) is >= 0,
            "lte" => Compare(actual.Value, condition.Value.Value) is <= 0,
            _ => false,
        };
    }

    /// <summary>"approve.decision" -> context["approve"], then property "decision".</summary>
    private static JsonElement? ResolvePath(string field,
        Dictionary<string, JsonElement> context)
    {
        var segments = field.Split('.');
        if (!context.TryGetValue(segments[0], out var current)) return null;

        foreach (var segment in segments.Skip(1))
        {
            if (current.ValueKind != JsonValueKind.Object ||
                !current.TryGetProperty(segment, out current))
                return null;
        }
        return current;
    }

    private static bool JsonEquals(JsonElement a, JsonElement b)
    {
        // Numbers compare numerically (2 == 2.0), everything else by kind + value.
        if (a.ValueKind == JsonValueKind.Number && b.ValueKind == JsonValueKind.Number)
            return a.GetDecimal() == b.GetDecimal();

        if (a.ValueKind != b.ValueKind) return false;

        return a.ValueKind switch
        {
            JsonValueKind.String => a.GetString() == b.GetString(),
            JsonValueKind.True or JsonValueKind.False or JsonValueKind.Null => true,
            _ => a.GetRawText() == b.GetRawText(),
        };
    }

    /// <summary>Ordering comparison; null when the kinds aren't comparable.</summary>
    private static int? Compare(JsonElement a, JsonElement b)
    {
        if (a.ValueKind == JsonValueKind.Number && b.ValueKind == JsonValueKind.Number)
            return a.GetDecimal().CompareTo(b.GetDecimal());

        if (a.ValueKind == JsonValueKind.String && b.ValueKind == JsonValueKind.String)
            return string.CompareOrdinal(a.GetString(), b.GetString());

        return null;   // "gt" between a string and a number is not a question with an answer
    }
}