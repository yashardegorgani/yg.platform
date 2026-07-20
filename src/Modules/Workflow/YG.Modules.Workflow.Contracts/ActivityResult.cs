using System.Text.Json;

namespace YG.Modules.Workflow.Contracts;

public sealed class ActivityResult
{
    public bool Succeeded { get; private init; }
    public JsonElement? Output { get; private init; }   // lands under the step's ResultKey
    public string? Error { get; private init; }

    public static ActivityResult Success(JsonElement? output = null)
        => new() { Succeeded = true, Output = output };

    public static ActivityResult Failure(string error)
        => new() { Succeeded = false, Error = error };
}