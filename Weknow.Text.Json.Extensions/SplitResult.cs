// credit: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to

namespace System.Text.Json
{
    /// <summary>
    /// Split result
    /// </summary>
    /// <param name="Positive">Json part of positive filtering result</param>
    /// <param name="Negative">Json part of negative filtering redult</param>
    public record SplitResult(JsonElement Positive, JsonElement Negative);
}
