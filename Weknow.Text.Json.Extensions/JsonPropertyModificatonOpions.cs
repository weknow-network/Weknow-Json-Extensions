// credit: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to

namespace System.Text.Json;

/// <summary>
/// Options for property modification
/// </summary>
/// <seealso cref="System.IEquatable&lt;System.Text.Json.JsonPropertyModificatonOpions&gt;" />
public record JsonPropertyModificatonOpions
{
    /// <summary>
    /// Gets the options for serialization of the property value.
    /// Including case-sensitivity of the path & property name filter.
    /// </summary>
    public JsonSerializerOptions? Options { get; init; }

    /// <summary>
    /// When true, property with null value considered as not exists
    /// </summary>
    /// <value>
    ///   <c>true</c> if [ignore null]; otherwise, <c>false</c>.
    /// </value>
    public bool IgnoreNull { get; init; } = true;
}
