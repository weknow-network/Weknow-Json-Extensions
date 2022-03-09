// credit: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to

namespace System.Text.Json
{
    // Determine the traversing flow
    public enum TraverseFlow
    {
        /// <summary>
        /// Skip out, back to parent flow.
        /// </summary>
        SkipToParent,
        /// <summary>
        /// Continue to sibling or ancestor's sibling (not children).
        /// </summary>
        Skip,
        /// <summary>
        /// When match: Continue to sibling or ancestor's sibling (not children).
        /// Otherwise: Drill
        /// </summary>
        SkipWhenMatch,
        ///// <summary>
        ///// Stop traversing
        ///// </summary>
        //Break,
        /// <summary>
        /// Drill into children and continue to sibling or ancestor's sibling.
        /// </summary>
        Drill,
        ///// <summary>
        ///// Drill into children and then break (don't continue to sibling or ancestor's sibling).
        ///// </summary>
        //DrillAndBreak
    }
}
