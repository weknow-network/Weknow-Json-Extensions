// credit: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to

namespace System.Text.Json
{
    /// <summary>
    /// Determine the traversing flow for re-write of json
    /// </summary>
    public enum TraverseFlowWrite
    {
        /// <summary>
        /// Write the element and continue to sibling.
        /// </summary>
        Pick,
        /// <summary>
        /// Write element and Drill into children and continue to sibling or ancestor's sibling.
        /// </summary>
        Drill,
        /// <summary>
        /// Skip out, back to parent flow.
        /// </summary>
        SkipToParent,
        /// <summary>
        /// Continue to sibling or ancestor's sibling (not children).
        /// </summary>
        Skip,
    }
}
