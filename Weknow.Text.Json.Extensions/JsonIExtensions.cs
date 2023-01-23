using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using static Weknow.Text.Json.Constants;

using static System.Text.Json.TraverseFlowInstruction;
using System.Threading;


// credit: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to

namespace System.Text.Json;


/// <summary>
/// Json extensions
/// </summary>
public static class JsonExtensions
{
    /// <summary>
    /// Empty json object
    /// </summary>
    public static readonly JsonElement Empty = CreateEmptyJsonElement();
    private static JsonWriterOptions INDENTED_JSON_OPTIONS = new JsonWriterOptions { Indented = true };

    #region CreatePathFilter

    /// <summary>
    /// Creates a path filter
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="caseSensitive">if set to <c>true</c> [case sensitive].</param>
    /// <returns></returns>
    private static Func<JsonElement, int, IImmutableList<string>, TraverseFlowInstruction> CreatePathFilter(string path, bool caseSensitive = false)
    {
        var filter = path.Split('.');

        return (current, deep, breadcrumbs) =>
        {
            var cur = breadcrumbs[deep];
            var validationPath = filter.Length > deep ? filter[deep] : "";
            if (validationPath == "*" || string.Compare(validationPath, cur, !caseSensitive) == 0)
            {
                if (deep == filter.Length - 1)
                    return Yield;
                return Drill;
            }
            if (validationPath == "[]" && cur[0] == '[' && cur[^1] == ']')
            {
                if (deep == filter.Length - 1)
                    return Yield;
                return Drill;
            }

            return Skip;
        };
    }

    #endregion // CreatePathFilter

    #region CreatePathWriteFilter

    /// <summary>
    /// Creates a path filter
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="caseSensitive">if set to <c>true</c> [case sensitive].</param>
    /// <returns></returns>
    private static Func<JsonElement, int, IImmutableList<string>, TraverseFlowWrite> CreatePathWriteFilter(
                    string path,
                    bool caseSensitive = false)
    {
        var filter = path.Split('.');

        return (current, deep, breadcrumbs) =>
        {
            var cur = breadcrumbs[deep];
            var validationPath = filter.Length > deep ? filter[deep] : "";
            if (validationPath == "*" || string.Compare(validationPath, cur, !caseSensitive) == 0)
            {
                if (deep == filter.Length - 1)
                    return TraverseFlowWrite.Pick;
                return TraverseFlowWrite.Drill;
            }
            if (validationPath == "[]" && cur[0] == '[' && cur[^1] == ']')
            {
                if (deep == filter.Length - 1)
                    return TraverseFlowWrite.Pick;
                return TraverseFlowWrite.Drill;
            }

            return TraverseFlowWrite.Skip;
        };
    }

    #endregion // CreatePathWriteFilter

    #region CreateExcludePathWriteFilter

    /// <summary>
    /// Creates a path filter
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="caseSensitive">if set to <c>true</c> [case sensitive].</param>
    /// <returns></returns>
    private static Func<JsonElement, int, IImmutableList<string>, TraverseFlowWrite> CreateExcludePathWriteFilter(
                                                                                        string path,
                                                                                        bool caseSensitive = false)
    {
        var filter = path.Split('.');

        return (current, deep, breadcrumbs) =>
        {
            var cur = breadcrumbs[deep];
            var validationPath = filter.Length > deep ? filter[deep] : "";
            if (validationPath == "*" || string.Compare(validationPath, cur, !caseSensitive) == 0)
            {
                if (deep == filter.Length - 1)
                    return TraverseFlowWrite.Skip;
                return TraverseFlowWrite.Drill;
            }
            if (validationPath == "[]" && cur[0] == '[' && cur[^1] == ']')
            {
                if (deep == filter.Length - 1)
                    return TraverseFlowWrite.Skip;
                return TraverseFlowWrite.Drill;
            }
            if (validationPath.Length > 2 && validationPath[0] == '[' && validationPath[^1] == ']')
            {
                if (cur == validationPath)
                    return TraverseFlowWrite.Skip;
                return TraverseFlowWrite.Pick;
            }

            return TraverseFlowWrite.Drill;
        };
    }

    #endregion // CreateExcludePathWriteFilter

    #region TryGetProperty

    /// <summary>
    /// Looks for a property named propertyName in the current object, returning a value
    /// that indicates whether or not such a property exists. When the property exists,
    /// its value is assigned to the value argument.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="value">When this method returns, contains the value of the specified property.</param>
    /// <param name="path">The name's path of the property to find.</param>
    /// <returns></returns>
    public static bool TryGetProperty(this JsonDocument source, out JsonElement value, params string[] path)
    {
        return source.RootElement.TryGetProperty(out value, path);
    }


    /// <summary>
    /// Looks for a property named propertyName in the current object, returning a value
    /// that indicates whether or not such a property exists. When the property exists,
    /// its value is assigned to the value argument.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="value">When this method returns, contains the value of the specified property.</param>
    /// <param name="path">The name's path of the property to find.</param>
    /// <returns></returns>
    public static bool TryGetProperty(this JsonElement source, out JsonElement value, params string[] path)
    {
        #region Validation

        if (path == null || path.Length == 0)
        {
            value = default;
            return false;
        }

        #endregion // Validation

        value = source;
        Span<string> cur = path.AsSpan();
        while (cur.Length != 0)
        {
            var head = cur[0];
            if (!value.TryGetProperty(head, out value)) return false;
            cur = cur[1..];
        }
        return true;
    }

    #endregion // TryGetProperty

    #region YieldWhen

    /// <summary>
    /// Filters descendant element by path.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    public static IEnumerable<JsonElement> YieldWhen(
        this JsonDocument source,
        string path)
    {
        return source.RootElement.YieldWhen(path);
    }

    /// <summary>
    /// Filters descendant element by path.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    public static IEnumerable<JsonElement> YieldWhen(
        this JsonElement source,
        string path)
    {
        return source.YieldWhen(false, path);
    }

    /// <summary>
    /// Filters descendant element by path.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="caseSensitive">indicate whether path should be a case sensitive</param>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    public static IEnumerable<JsonElement> YieldWhen(
        this JsonElement source,
        bool caseSensitive,
        string path)
    {
        Func<JsonElement, int, IImmutableList<string>, TraverseFlowInstruction> predicate =
            CreatePathFilter(path, caseSensitive);
        return source.YieldWhen(0, ImmutableList<string>.Empty, predicate);
    }

    /// <summary>
    /// Filters descendant element by predicate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="predicate">
    /// <![CDATA[The predicate: (current, deep, breadcrumbs spine) => ...;
    /// current: the current JsonElement.
    /// deep: start at 0.
    /// breadcrumbs spine: spine of ancestor's properties and arrays index.
    /// TIP: using static System.Text.Json.TraverseFlowInstruction;]]>
    /// </param>
    /// <returns></returns>
    public static IEnumerable<JsonElement> YieldWhen(
        this JsonDocument source,
        Func<JsonElement, int, IImmutableList<string>, TraverseFlowInstruction> predicate)
    {
        return source.RootElement.YieldWhen(0, ImmutableList<string>.Empty, predicate);
    }

    /// <summary>
    /// Filters descendant element by predicate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="predicate">
    /// <![CDATA[The predicate: (current, deep, breadcrumbs spine) => ...;
    /// current: the current JsonElement.
    /// deep: start at 0.
    /// breadcrumbs spine: spine of ancestor's properties and arrays index.
    /// TIP: using static System.Text.Json.TraverseFlowInstruction;]]>
    /// </param>
    /// <returns></returns>
    public static IEnumerable<JsonElement> YieldWhen(this JsonElement source, Func<JsonElement, int, IImmutableList<string>, TraverseFlowInstruction> predicate)
    {
        return source.YieldWhen(0, ImmutableList<string>.Empty, predicate);
    }

    /// <summary>
    /// Filters descendant element by predicate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="deep">The deep.</param>
    /// <param name="spine">The breadcrumbs spine.</param>
    /// <param name="predicate">
    /// <![CDATA[The predicate: (current, deep, breadcrumbs spine) => ...;
    /// current: the current JsonElement.
    /// deep: start at 0.
    /// breadcrumbs spine: spine of ancestor's properties and arrays index.
    /// TIP: using static System.Text.Json.TraverseFlowInstruction;]]>
    /// </param>
    /// <returns></returns>
    private static IEnumerable<JsonElement> YieldWhen(
                            this JsonElement source,
                            int deep,
                            IImmutableList<string> spine,
                            Func<JsonElement, int, IImmutableList<string>, TraverseFlowInstruction> predicate)
    {
        if (source.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty p in source.EnumerateObject())
            {
                var spn = spine.Add(p.Name);
                var val = p.Value;
                var (pick, flow) = predicate(val, deep, spn);
                if (pick)
                {
                    yield return val;
                    if (flow == TraverseFlow.SkipWhenMatch)
                        continue;
                }
                else if (flow == TraverseFlow.SkipWhenMatch)
                    flow = TraverseFlow.Drill;
                if (flow == TraverseFlow.Skip)
                    continue;
                if (flow == TraverseFlow.SkipToParent)
                    break;
                if (flow == TraverseFlow.Drill)
                {
                    foreach (var result in val.YieldWhen(deep + 1, spn, predicate))
                    {
                        yield return result;
                    }
                }
            }
        }
        else if (source.ValueKind == JsonValueKind.Array)
        {
            int i = 0;
            foreach (JsonElement val in source.EnumerateArray())
            {
                var spn = spine.Add($"[{i++}]");
                var (shouldYield, flowStrategy) = predicate(val, deep, spn);
                if (shouldYield)
                {
                    yield return val;
                    if (flowStrategy == TraverseFlow.SkipWhenMatch)
                        continue;
                }
                else if (flowStrategy == TraverseFlow.SkipWhenMatch)
                    flowStrategy = TraverseFlow.Drill;

                if (flowStrategy == TraverseFlow.Skip)
                    continue;
                if (flowStrategy == TraverseFlow.SkipToParent)
                    break;
                if (flowStrategy == TraverseFlow.Drill)
                {
                    foreach (var result in val.YieldWhen(deep + 1, spn, predicate))
                    {
                        yield return result;
                    }
                }
            }
        }

    }

    #endregion // YieldWhen

    #region Filter

    /// <summary>
    /// Rewrite json while excluding elements according to a filter
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The path.</param>
    /// <param name="caseSensitive">if set to <c>true</c> [case sensitive].</param>
    /// <param name="onReplace"><![CDATA[
    /// Optional remove element notification.
    /// Action's signature : (current, deep, breadcrumbs spine) => ...;
    /// current: the current JsonElement.
    /// deep: start at 0.
    /// breadcrumbs spine: spine of ancestor's properties and arrays index.
    /// Returns: if return an element it will replace the existing value, otherwise it will remove the current value]]></param>
    /// <returns></returns>
    public static JsonElement Filter(
        this JsonDocument source,
        string path,
        bool caseSensitive = false,
        Func<JsonElement, int, IImmutableList<string>, TraverseFlowWrite, JsonElement?>? onReplace = null)
    {
        return source.RootElement.Filter(path, caseSensitive, onReplace);
    }


    /// <summary>
    /// Rewrite json while excluding elements according to a path
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The path.</param>
    /// <param name="caseSensitive">indicate whether path should be a case sensitive</param>
    /// <param name="onReplace"><![CDATA[
    /// Optional remove element notification.
    /// Action's signature : (current, deep, breadcrumbs spine) => ...;
    /// current: the current JsonElement.
    /// deep: start at 0.
    /// breadcrumbs spine: spine of ancestor's properties and arrays index.
    /// Returns: if return an element it will replace the existing value, otherwise it will remove the current value]]></param>
    /// <returns></returns>
    public static JsonElement Filter(
        this JsonElement source,
        string path,
        bool caseSensitive = false,
        Func<JsonElement, int, IImmutableList<string>, TraverseFlowWrite, JsonElement?>? onReplace = null)
    {
        Func<JsonElement, int, IImmutableList<string>, TraverseFlowWrite> predicate =
            CreatePathWriteFilter(path, caseSensitive);
        return source.Filter(predicate, onReplace);
    }

    /// <summary>
    /// Filters descendant element by predicate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="predicate"><![CDATA[The predicate: (current, deep, breadcrumbs spine) => ...;
    /// current: the current JsonElement.
    /// deep: start at 0.
    /// breadcrumbs spine: spine of ancestor's properties and arrays index.
    /// TIP: using static System.Text.Json.TraverseFlowWrite;]]></param>
    /// <param name="onReplace"><![CDATA[
    /// Optional remove element notification.
    /// Action's signature : (current, deep, breadcrumbs spine) => ...;
    /// current: the current JsonElement.
    /// deep: start at 0.
    /// breadcrumbs spine: spine of ancestor's properties and arrays index.
    /// Returns: if return an element it will replace the existing value, otherwise it will remove the current value]]></param>
    /// <returns></returns>
    public static JsonElement Filter(
        this JsonDocument source,
        Func<JsonElement, int, IImmutableList<string>, TraverseFlowWrite> predicate,
        Func<JsonElement, int, IImmutableList<string>, TraverseFlowWrite, JsonElement?>? onReplace = null)
    {
        return source.RootElement.Filter(predicate, onReplace);
    }

    /// <summary>
    /// Filters descendant element by predicate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="predicate">
    /// <![CDATA[The predicate: (current, deep, breadcrumbs spine) => ...;
    /// current: the current JsonElement.
    /// deep: start at 0.
    /// breadcrumbs spine: spine of ancestor's properties and arrays index.
    /// TIP: using static System.Text.Json.TraverseFlowWrite;]]>
    /// </param>
    /// <param name="onReplace">         
    /// <![CDATA[
    /// Optional remove element notification.
    /// Action's signature : (current, deep, breadcrumbs spine) => ...;
    /// current: the current JsonElement.
    /// deep: start at 0.
    /// breadcrumbs spine: spine of ancestor's properties and arrays index.
    /// Returns: if return an element it will replace the existing value, otherwise it will remove the current value]]>
    /// </param>
    /// <returns></returns>
    public static JsonElement Filter(
        this JsonElement source,
        Func<JsonElement, int, IImmutableList<string>, TraverseFlowWrite> predicate,
        Func<JsonElement, int, IImmutableList<string>, TraverseFlowWrite, JsonElement?>? onReplace = null)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(bufferWriter))
        {
            source.FilterImp(writer, predicate, onReplace);
        }
        var reader = new Utf8JsonReader(bufferWriter.WrittenSpan);
        var result = JsonDocument.ParseValue(ref reader);
        return result.RootElement;
    }


    #endregion // Filter

    #region Exclude

    /// <summary>
    /// Rewrite json while excluding elements according to a filter
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    public static JsonElement Exclude(
        this JsonDocument source,
        string path)
    {
        return source.RootElement.Exclude(path);
    }

    /// <summary>
    /// Rewrite json while excluding elements according to a path
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    public static JsonElement Exclude(
        this JsonElement source,
        string path)
    {
        return source.Exclude(false, path);
    }

    /// <summary>
    /// Rewrite json while excluding elements according to a case-sensitive path
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    public static JsonElement ExcludeSensitive(
        this JsonElement source,
        string path)
    {
        return source.Exclude(true, path);
    }

    /// <summary>
    /// Rewrite json while excluding elements according to a path
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="caseSensitive">indicate whether path should be a case sensitive</param>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    private static JsonElement Exclude(
        this JsonElement source,
        bool caseSensitive,
        string path)
    {
        Func<JsonElement, int, IImmutableList<string>, TraverseFlowWrite> predicate =
            CreateExcludePathWriteFilter(path, caseSensitive);
        return source.Filter(predicate);
    }

    #endregion // Exclude

    #region FilterImp

    /// <summary>
    /// Where operation, clean up the element according to the filter
    /// (exclude whatever don't match the filter).
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="writer">The writer.</param>
    /// <param name="predicate"><![CDATA[The predicate: (current, deep, breadcrumbs spine) => ...;
    /// current: the current JsonElement.
    /// deep: start at 0.
    /// breadcrumbs spine: spine of ancestor's properties and arrays index.
    /// TIP: using static System.Text.Json.TraverseFlowWrite;]]></param>
    /// <param name="onReplace"><![CDATA[
    /// Optional replace / remove element notification.
    /// Action's signature : (current, deep, breadcrumbs spine) => ...;
    /// current: the current JsonElement.
    /// deep: start at 0.
    /// breadcrumbs spine: spine of ancestor's properties and arrays index.
    /// Returns: if return an element it will replace the existing value, otherwise it will remove the current value]]></param>
    /// <param name="deep">The current deep.</param>
    /// <param name="breadcrumbs">the path into the json</param>
    /// <param name="propParent">if set to <c>true</c> [parent is property].</param>
    private static void FilterImp(
        this JsonElement element,
        Utf8JsonWriter writer,
        Func<JsonElement, int, IImmutableList<string>, TraverseFlowWrite> predicate,
        Func<JsonElement, int, IImmutableList<string>, TraverseFlowWrite, JsonElement?>? onReplace = null,
        int deep = 0,
        IImmutableList<string>? breadcrumbs = null,
        bool propParent = false)
    {
        IImmutableList<string> spine = breadcrumbs ?? ImmutableList<string>.Empty;
        if (element.ValueKind == JsonValueKind.Object)
        {
            int count = 0;
            void TryStartObject()
            {
                if (Interlocked.Increment(ref count) == 1)
                    writer.WriteStartObject();
            }
            void TryEndObject()
            {
                if (count > 0)
                    writer.WriteEndObject();
            }
            if (propParent) TryStartObject();
            foreach (JsonProperty p in element.EnumerateObject())
            {
                var spn = spine.Add(p.Name);
                var val = p.Value;
                var flow = predicate(val, deep, spn);
                switch (flow)
                {
                    case TraverseFlowWrite.Pick:
                        if (!TryReplace())
                        {
                            TryStartObject();
                            writer.WritePropertyName(p.Name);
                            val.WriteTo(writer);
                        }
                        break;
                    case TraverseFlowWrite.SkipToParent:
                    case TraverseFlowWrite.Skip:
                        TryReplace();
                        break;
                    case TraverseFlowWrite.Drill:
                        TryStartObject();
                        writer.WritePropertyName(p.Name);
                        if (val.ValueKind == JsonValueKind.Object || val.ValueKind == JsonValueKind.Array)
                            val.FilterImp(writer, predicate, onReplace, deep + 1, spn, true);
                        else
                            val.WriteTo(writer);
                        break;
                }

                if (flow == TraverseFlowWrite.SkipToParent)
                    break;

                bool TryReplace()
                {
                    var replacement = onReplace?.Invoke(val, deep, spn, flow);
                    bool shouldReplace = replacement != null;
                    if (shouldReplace)
                    {
                        TryStartObject();
                        writer.WritePropertyName(p.Name);
                        replacement?.WriteTo(writer);
                    }
                    return shouldReplace;
                }
            }
            TryEndObject();
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            int i = 0;
            foreach (JsonElement val in element.EnumerateArray())
            {
                var spn = spine.Add($"[{i++}]");
                var flow = predicate(val, deep, spn);
                switch (flow)
                {
                    case TraverseFlowWrite.Pick:
                        val.WriteTo(writer);
                        break;
                    case TraverseFlowWrite.SkipToParent:
                    case TraverseFlowWrite.Skip:
                        TryReplace();
                        break;
                    case TraverseFlowWrite.Drill:
                        if (val.ValueKind == JsonValueKind.Object || val.ValueKind == JsonValueKind.Array)
                            val.FilterImp(writer, predicate, onReplace, deep + 1, spn);
                        else
                            val.WriteTo(writer);
                        break;
                }
                if (flow == TraverseFlowWrite.SkipToParent)
                    break;

                bool TryReplace()
                {
                    var replacement = onReplace?.Invoke(val, deep, spn, flow);
                    bool shouldReplace = replacement != null;
                    if (shouldReplace)
                    {
                        replacement?.WriteTo(writer);
                    }
                    return shouldReplace;
                }
            }
            writer.WriteEndArray();
        }
        else
        {
            element.WriteTo(writer);
        }
    }

    #endregion // FilterImp

    #region Merge

    /// <summary>
    /// Merge source json with other json (which will override the source on conflicts)
    /// Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <returns></returns>
    public static JsonElement Merge(
        this JsonDocument source,
        params JsonElement[] joined)
    {
        return source.RootElement.Merge(joined);
    }

    /// <summary>
    /// Merge source json with other json (which will override the source on conflicts)
    /// Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <returns></returns>
    public static JsonElement Merge(
        this JsonElement source,
        params JsonElement[] joined)
    {
        return source.Merge((IEnumerable<JsonElement>)joined);
    }

    /// <summary>
    /// Merge source json with other json (which will override the source on conflicts)
    /// Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <returns></returns>
    public static JsonElement Merge(
        this JsonDocument source,
        IEnumerable<JsonElement> joined)
    {
        return source.RootElement.Merge(joined);
    }

    /// <summary>
    /// Merge source json with other json (which will override the source on conflicts)
    /// Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <returns></returns>
    public static JsonElement Merge(
        this JsonElement source,
        IEnumerable<JsonElement> joined)
    {
        return joined.Aggregate(source, (acc, cur) => acc.MergeImp(cur));
    }


    /// <summary>
    /// Merge source json with other json (which will override the source on conflicts)
    /// Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <returns></returns>
    private static JsonElement MergeImp(
        this JsonElement source,
        JsonElement joined)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            source.MergeImp(joined, writer);
        }

        var reader = new Utf8JsonReader(buffer.WrittenSpan);
        JsonDocument result = JsonDocument.ParseValue(ref reader);
        return result.RootElement;
    }

    /// <summary>
    /// Merge source json with other json (which will override the source on conflicts)
    /// Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <param name="writer">The writer.</param>
    private static void MergeImp(
        this JsonElement source,
        JsonElement joined,
        Utf8JsonWriter writer)
    {
        #region Validation

        if (source.ValueKind == JsonValueKind.Array && joined.ValueKind != JsonValueKind.Array)
        {
            joined.WriteTo(writer); // override
            return;
        }
        if (source.ValueKind == JsonValueKind.Object && joined.ValueKind != JsonValueKind.Object)
        {
            joined.WriteTo(writer); // override
            return;
        }
        if (joined.ValueKind != JsonValueKind.Object && joined.ValueKind != JsonValueKind.Array)
        {
            joined.WriteTo(writer); // override
            return;
        }

        #endregion // Validation

        if (source.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            var map = joined.EnumerateObject().ToDictionary(m => m.Name, m => m.Value);
            foreach (JsonProperty p in source.EnumerateObject())
            {

                var name = p.Name;
                var val = p.Value;

                writer.WritePropertyName(p.Name);
                if (map.ContainsKey(name))
                {
                    var j = map[name];
                    val.MergeImp(j, writer);
                    map.Remove(name);
                    continue;
                }
                val.WriteTo(writer);
            }
            foreach (var p in map)
            {
                var name = p.Key;
                writer.WritePropertyName(name);
                var val = p.Value;
                val.WriteTo(writer);
            }
            writer.WriteEndObject();
        }
        else if (source.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (JsonElement val in source.EnumerateArray())
            {
                val.WriteTo(writer);
            }
            foreach (JsonElement val in joined.EnumerateArray())
            {
                val.WriteTo(writer);
            }
            writer.WriteEndArray();
        }
        else
        {
            joined.WriteTo(writer);
        }
    }

    #endregion // Merge

    #region MergeObject

    /// <summary>
    /// Merge source json with other json (which will override the source on conflicts)
    /// Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <returns></returns>
    public static JsonElement MergeObject<T>(
        this JsonElement source,
        T joined)
    {
        var j = joined.ToJson();
        return source.Merge(j);
    }

    #endregion // MergeObject

    #region MergeInto

    /// <summary>
    /// Merge source json with other json at specific location within the source
    /// Note: 
    /// - On conflicts, will override the source
    /// - Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The target path for merging.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <remarks>Conflicts may happens when trying to merge object into array or other versa.</remarks>
    /// <returns></returns>
    public static JsonElement MergeInto(
        this JsonDocument source,
        string path,
        params JsonElement[] joined)
    {
        return source.MergeInto(path, false, (IEnumerable<JsonElement>)joined);
    }

    /// <summary>
    /// Merge source json with other json at specific location within the source
    /// Note: 
    /// - On conflicts, will override the source
    /// - Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The target path for merging.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <remarks>Conflicts may happens when trying to merge object into array or other versa.</remarks>
    /// <returns></returns>
    public static JsonElement MergeInto(
        this JsonElement source,
        string path,
        params JsonElement[] joined)
    {
        return source.MergeInto(path, false, (IEnumerable<JsonElement>)joined);
    }

    /// <summary>
    /// Merge source json with other json at specific location within the source
    /// Note: 
    /// - On conflicts, will override the source
    /// - Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The target path for merging.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <remarks>Conflicts may happens when trying to merge object into array or other versa.</remarks>
    /// <returns></returns>
    public static JsonElement MergeInto(
        this JsonDocument source,
        string path,
        IEnumerable<JsonElement> joined)
    {
        return source.MergeInto(path, false, joined);
    }

    /// <summary>
    /// Merge source json with other json at specific location within the source
    /// Note: 
    /// - On conflicts, will override the source
    /// - Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The target path for merging.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <remarks>Conflicts may happens when trying to merge object into array or other versa.</remarks>
    /// <returns></returns>
    public static JsonElement MergeInto(
        this JsonElement source,
        string path,
        IEnumerable<JsonElement> joined)
    {
        return source.MergeInto(path, false, joined);
    }

    /// <summary>
    /// Merge source json with other json at specific location within the source
    /// Note: 
    /// - On conflicts, will override the source
    /// - Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The target path for merging.</param>
    /// <param name="caseSensitive">if set to <c>true</c> [case sensitive].</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <remarks>Conflicts may happens when trying to merge object into array or other versa.</remarks>
    /// <returns></returns>
    public static JsonElement MergeInto(
        this JsonDocument source,
        string path,
        bool caseSensitive,
        params JsonElement[] joined)
    {
        return source.RootElement.MergeInto(path, caseSensitive, (IEnumerable<JsonElement>)joined);
    }

    /// <summary>
    /// Merge source json with other json at specific location within the source
    /// Note: 
    /// - On conflicts, will override the source
    /// - Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The target path for merging.</param>
    /// <param name="caseSensitive">if set to <c>true</c> [case sensitive].</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <remarks>Conflicts may happens when trying to merge object into array or other versa.</remarks>
    /// <returns></returns>
    public static JsonElement MergeInto(
        this JsonElement source,
        string path,
        bool caseSensitive,
        params JsonElement[] joined)
    {
        return source.MergeInto(path, caseSensitive, (IEnumerable<JsonElement>)joined);
    }

    /// <summary>
    /// Merge source json with other json at specific location within the source
    /// Note: 
    /// - On conflicts, will override the source
    /// - Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The target path for merging.</param>
    /// <param name="caseSensitive">if set to <c>true</c> [case sensitive].</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <remarks>Conflicts may happens when trying to merge object into array or other versa.</remarks>
    /// <returns></returns>
    public static JsonElement MergeInto(
        this JsonDocument source,
        string path,
        bool caseSensitive,
        IEnumerable<JsonElement> joined)
    {
        return source.RootElement.MergeInto(path, caseSensitive, joined);
    }

    /// <summary>
    /// Merge source json with other json at specific location within the source
    /// Note: 
    /// - On conflicts, will override the source
    /// - Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The target path for merging.</param>
    /// <param name="caseSensitive">if set to <c>true</c> [case sensitive].</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <remarks>Conflicts may happens when trying to merge object into array or other versa.</remarks>
    /// <returns></returns>
    public static JsonElement MergeInto(
        this JsonElement source,
        string path,
        bool caseSensitive,
        IEnumerable<JsonElement> joined)
    {
        Func<JsonElement, int, IImmutableList<string>, TraverseFlowWrite> predicate =
                  CreateExcludePathWriteFilter(path, caseSensitive);
        return source.Filter(predicate, OnMerge);

        JsonElement? OnMerge(JsonElement target, int deep, IImmutableList<string> breadcrumbs, TraverseFlowWrite flow)
        {
            return target.Merge(joined);
        }

    }

    /// <summary>
    /// Merge source json with other json at specific location within the source
    /// Note: 
    /// - On conflicts, will override the source
    /// - Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="predicate">Identify the merge with element.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <remarks>Conflicts may happens when trying to merge object into array or other versa.</remarks>
    /// <returns></returns>
    public static JsonElement MergeInto(
        this JsonDocument source,
        Func<JsonElement, int, IImmutableList<string>, bool> predicate,
        params JsonElement[] joined)
    {
        return source.RootElement.MergeInto(predicate, (IEnumerable<JsonElement>)joined);
    }

    /// <summary>
    /// Merge source json with other json at specific location within the source
    /// Note: 
    /// - On conflicts, will override the source
    /// - Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="predicate">Identify the merge with element.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <remarks>Conflicts may happens when trying to merge object into array or other versa.</remarks>
    /// <returns></returns>
    public static JsonElement MergeInto(
        this JsonElement source,
        Func<JsonElement, int, IImmutableList<string>, bool> predicate,
        params JsonElement[] joined)
    {
        return source.MergeInto(predicate, (IEnumerable<JsonElement>)joined);
    }

    /// <summary>
    /// Merge source json with other json at specific location within the source
    /// Note: 
    /// - On conflicts, will override the source
    /// - Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="predicate">Identify the merge with element.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <remarks>Conflicts may happens when trying to merge object into array or other versa.</remarks>
    /// <returns></returns>
    public static JsonElement MergeInto(
        this JsonDocument source,
        Func<JsonElement, int, IImmutableList<string>, bool> predicate,
        IEnumerable<JsonElement> joined)
    {
        return source.RootElement.MergeInto(predicate, joined);
    }

    /// <summary>
    /// Merge source json with other json at specific location within the source
    /// Note: 
    /// - On conflicts, will override the source
    /// - Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="predicate">Identify the merge with element.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <remarks>Conflicts may happens when trying to merge object into array or other versa.</remarks>
    /// <returns></returns>
    public static JsonElement MergeInto(
        this JsonElement source,
        Func<JsonElement, int, IImmutableList<string>, bool> predicate,
        IEnumerable<JsonElement> joined)
    {
        return source.Filter(Predicate, OnMerge);

        TraverseFlowWrite Predicate(JsonElement target, int deep, IImmutableList<string> breadcrumbs)
        {
            return predicate(target, deep, breadcrumbs) switch
            {
                true => TraverseFlowWrite.Skip,
                _ => TraverseFlowWrite.Pick
            };
        }

        JsonElement? OnMerge(JsonElement target, int deep, IImmutableList<string> breadcrumbs, TraverseFlowWrite flow)
        {
            return target.Merge(joined);
        }
    }

    #endregion // MergeInto

    #region MergeObjectInto

    /// <summary>
    /// Merge source json with other json at specific location within the source
    /// Note: which will override the source on conflicts, Array will be concatenate.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="path">The target path for merging.</param>
    /// <param name="joined">The joined element (will override on conflicts).</param>
    /// <returns></returns>
    public static JsonElement MergeObjectInto<T>(
        this JsonElement source,
        string path,
        T joined)
    {
        var j = joined.ToJson();
        return source.MergeInto(path, false, j);
    }

    #endregion // MergeObjectInto

    #region AsString

    /// <summary>
    /// Gets the json representation as string.
    /// </summary>
    /// <param name="json">The j.</param>
    /// <param name="options">The options.</param>
    /// <returns></returns>
    public static string AsString(
        this JsonDocument json,
        JsonWriterOptions options = default)
    {
        return json.RootElement.AsString(options);
    }

    /// <summary>
    /// Gets the json representation as string.
    /// </summary>
    /// <param name="json">The j.</param>
    /// <param name="options">The options.</param>
    /// <returns></returns>
    public static string AsString(
        this JsonElement json,
        JsonWriterOptions options = default)
    {
        if (json.ValueKind == JsonValueKind.String) return json.GetString() ?? String.Empty;
        if (json.ValueKind == JsonValueKind.Number) return $"{json.GetDouble()}";
        if (json.ValueKind == JsonValueKind.True) return "False";
        if (json.ValueKind == JsonValueKind.False) return "False";
        if (json.ValueKind == JsonValueKind.Null || json.ValueKind == JsonValueKind.Undefined)
            return String.Empty;
        using var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms, options))
        {
            json.WriteTo(w);
        }
        var result = Encoding.UTF8.GetString(ms.ToArray());
        return result;
    }

    /// <summary>
    /// Gets the json representation as indented string.
    /// </summary>
    /// <param name="json">The j.</param>
    /// <returns></returns>
    public static string AsIndentString(
        this JsonDocument json)
    {
        return json.RootElement.AsString(INDENTED_JSON_OPTIONS);
    }

    /// <summary>
    /// Gets the json representation as indented string.
    /// </summary>
    /// <param name="json">The j.</param>
    /// <returns></returns>
    public static string AsIndentString(
        this JsonElement json)
    {
        return json.AsString(INDENTED_JSON_OPTIONS);
    }

    #endregion // AsString

    #region ToStream

    /// <summary>
    /// Gets the json representation as Stream.
    /// </summary>
    /// <param name="json">The j.</param>
    /// <param name="options">The options.</param>
    /// <returns></returns>
    public static Stream ToStream(
        this JsonDocument json,
        JsonWriterOptions options = default)
    {
        return json.RootElement.ToStream(options);
    }

    /// <summary>
    /// Gets the json representation as string.
    /// </summary>
    /// <param name="json">The j.</param>
    /// <param name="options">The options.</param>
    /// <returns></returns>
    public static Stream ToStream(
        this JsonElement json,
        JsonWriterOptions options = default)
    {
        var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms, options))
        {
            json.WriteTo(w);
        }
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    #endregion // ToStream

    #region Serialize

    /// <summary>
    /// Serializes the specified instance.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="instance">The instance.</param>
    /// <param name="options">The options.</param>
    /// <returns></returns>
    public static string Serialize<T>(
        this T instance,
        JsonSerializerOptions? options = null)
    {
        options = options ?? SerializerOptions;
        string json = JsonSerializer.Serialize(instance, options);
        return json;
    }

    #endregion // Serialize

    #region ToJson

    /// <summary>
    /// Convert instance to JsonElement.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="instance">The instance.</param>
    /// <param name="options">The options (used for the serialization).</param>
    /// <returns></returns>
    public static JsonElement ToJson<T>(
        this T instance,
        JsonSerializerOptions? options = null)
    {
        if (instance is JsonElement element) return element;
        if (instance is JsonDocument doc) return doc.RootElement;

        options = options ?? SerializerOptions;
        byte[]? j = JsonSerializer.SerializeToUtf8Bytes(instance, options);
        if (j == null)
            return Empty;
        var json = JsonDocument.Parse(j);
        return json.RootElement;
    }

    #endregion // ToJson

    #region WhereProp

    /// <summary>
    /// Where operation, clean up the element's properties according to the filter 
    /// (exclude properties which don't match the filter).
    /// </summary>
    /// <param name="doc">The element.</param>
    /// <param name="filter"><![CDATA[
    /// The filter which determine whether to keep the property. 
    /// CDATA[Func<element, path, deep, bool result>
    /// example: (element, path, deep) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
    /// </param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <param name="onRemove">On remove property notification.</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static JsonDocument WhereProp(
        this JsonDocument doc,
        Func<JsonProperty, string, int, bool> filter,
        byte deep = 0,
        Action<JsonProperty>? onRemove = null)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(bufferWriter))
        {
            doc.RootElement.WhereImp(writer, filter, null, null, onRemove, deep);
        }
        var reader = new Utf8JsonReader(bufferWriter.WrittenSpan);
        var result = JsonDocument.ParseValue(ref reader);
        return result;
    }

    /// <summary>
    /// Where operation, clean up the element's properties according to the filter 
    /// (exclude properties which don't match the filter).
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="filter"><![CDATA[
    /// The filter which determine whether to keep the property. 
    /// Func<element, path, deep, bool result>
    /// example: (element, path, deep) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
    /// </param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <param name="onRemove">On remove property notification.</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static JsonElement WhereProp(
        this JsonElement element,
        Func<JsonProperty, string, int, bool> filter,
        byte deep = 0,
        Action<JsonProperty>? onRemove = null)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(bufferWriter))
        {
            element.WhereImp(writer, filter, null, null, onRemove, deep);
        }
        var reader = new Utf8JsonReader(bufferWriter.WrittenSpan);
        var result = JsonDocument.ParseValue(ref reader);
        return result.RootElement;
    }

    /// <summary>
    /// Where operation, clean up the element's properties according to the filter 
    /// (exclude properties which don't match the filter).
    /// </summary>
    /// <param name="doc">The element.</param>
    /// <param name="filter"><![CDATA[
    /// The filter which determine whether to keep the property. 
    /// example: (element, propertyPath) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
    /// </param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <param name="onRemove">On remove property notification.</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static JsonElement WhereProp(
        this JsonDocument doc,
        IImmutableSet<string> filter,
        byte deep = 0,
        Action<JsonProperty>? onRemove = null)
    {
        JsonElement result = WherePropSetInternal(doc.RootElement, filter);
        return result;
    }

    /// <summary>
    /// Where operation, clean up the element's properties according to the filter 
    /// (exclude properties which don't match the filter).
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="filter"><![CDATA[
    /// The filter which determine whether to keep the property. 
    /// example: (element, propertyPath) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
    /// </param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <param name="onRemove">On remove property notification.</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static JsonElement WhereProp(
        this JsonElement element,
        IImmutableSet<string> filter,
        byte deep = 0,
        Action<JsonProperty>? onRemove = null)
    {
        JsonElement result = WherePropSetInternal(element, filter);
        return result;
    }

    /// <summary>
    /// Where operation, clean up the element's properties according to the filter 
    /// (exclude properties which don't match the filter).
    /// </summary>
    /// <param name="doc">The element.</param>
    /// <param name="filter"><![CDATA[
    /// The filter which determine whether to keep the property. 
    /// example: (element, propertyPath) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
    /// </param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static JsonElement WhereProp(
        this JsonDocument doc,
        params string[] filter)
    {
        IImmutableSet<string> set = ImmutableHashSet.CreateRange(filter);
        JsonElement result = WherePropSetInternal(doc.RootElement, set);
        return result;
    }

    /// <summary>
    /// Where operation, clean up the element's properties according to the filter 
    /// (exclude properties which don't match the filter).
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="filter"><![CDATA[
    /// The filter which determine whether to keep the property. 
    /// example: (element, propertyPath) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
    /// </param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static JsonElement WhereProp(
        this JsonElement element,
        params string[] filter)
    {
        IImmutableSet<string> set = ImmutableHashSet.CreateRange(filter);
        JsonElement result = WherePropSetInternal(element, set);
        return result;
    }

    /// <summary>
    /// Where operation, clean up the element's properties according to the filter 
    /// (exclude properties which don't match the filter).
    /// </summary>
    /// <param name="doc">The element.</param>
    /// <param name="filter"><![CDATA[
    /// The filter which determine whether to keep the property. 
    /// example: (element, propertyPath) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
    /// </param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static JsonElement WhereProp(
        this JsonDocument doc,
        IEnumerable<string> filter)
    {
        IImmutableSet<string> set = ImmutableHashSet.CreateRange(filter);
        JsonElement result = WherePropSetInternal(doc.RootElement, set);
        return result;
    }

    /// <summary>
    /// Where operation, clean up the element's properties according to the filter 
    /// (exclude properties which don't match the filter).
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="filter"><![CDATA[
    /// The filter which determine whether to keep the property. 
    /// example: (element, propertyPath) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
    /// </param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static JsonElement WhereProp(
        this JsonElement element,
        IEnumerable<string> filter)
    {
        IImmutableSet<string> set = ImmutableHashSet.CreateRange(filter);
        JsonElement result = WherePropSetInternal(element, set);
        return result;
    }

    #endregion // WhereProp

    #region Where

    /// <summary>
    /// Where operation, clean up the element according to the filter 
    /// (exclude whatever don't match the filter).
    /// </summary>
    /// <param name="doc">The element.</param>
    /// <param name="filter"><![CDATA[
    /// The filter which determine whether to keep the property. 
    /// Func<element, path, deep, bool result>
    /// example: (element, path, deep) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
    /// </param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    [Obsolete("Use Filter", false)]
    public static JsonDocument Where(this JsonDocument doc, Func<JsonElement, string, int, bool> filter, byte deep = 0)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(bufferWriter))
        {
            doc.RootElement.WhereImp(writer, null, null, filter, null, deep);
        }
        var reader = new Utf8JsonReader(bufferWriter.WrittenSpan);
        var result = JsonDocument.ParseValue(ref reader);
        return result;
    }

    /// <summary>
    /// Where operation, clean up the element according to the filter 
    /// (exclude whatever don't match the filter).
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="filter"><![CDATA[
    /// The filter which determine whether to keep the property. 
    /// Func<element, path, deep, bool result>
    /// example: (element, path, deep) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
    /// </param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    [Obsolete("Use Filter", false)]
    public static JsonElement Where(this JsonElement element, Func<JsonElement, string, int, bool> filter, byte deep = 0)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(bufferWriter))
        {
            element.WhereImp(writer, null, null, filter, null, deep);
        }
        var reader = new Utf8JsonReader(bufferWriter.WrittenSpan);
        var result = JsonDocument.ParseValue(ref reader);
        return result.RootElement;
    }

    #endregion // Where

    #region SplitProp

    /// <summary>
    /// Split operation, split properties according to a filter.
    /// </summary>
    /// <param name="doc">The json document.</param>
    /// <param name="propNames">The properties name or properties path separate with '.', for example: root.child.grandchild</param>
    /// <returns>
    /// positive: properties which match the filter.
    /// negative: properties which didn't match the filter.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static SplitResult SplitProp(
        this JsonDocument doc,
        params string[] propNames)
    {
        var set = ImmutableHashSet.CreateRange(propNames);
        return SplitPropInternal(doc.RootElement, set, null);
    }

    /// <summary>
    /// Split operation, split properties according to a filter.
    /// </summary>
    /// <param name="doc">The json document.</param>
    /// <param name="deep">The deep.</param>
    /// <param name="propNames">The properties name or properties path separate with '.', for example: root.child.grandchild</param>
    /// <returns>
    /// positive: properties which match the filter.
    /// negative: properties which didn't match the filter.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static SplitResult SplitProp(
        this JsonDocument doc,
        byte deep,
        params string[] propNames)
    {
        var set = ImmutableHashSet.CreateRange(propNames);
        return SplitPropInternal(doc.RootElement, set, null, deep);
    }

    /// <summary>
    /// Split operation, split properties according to a filter.
    /// </summary>
    /// <param name="doc">The json document.</param>
    /// <param name="propNames">The properties name or properties path separate with '.', for example: root.child.grandchild</param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <returns>
    /// positive: properties which match the filter.
    /// negative: properties which didn't match the filter.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static SplitResult SplitProp(
        this JsonDocument doc,
        IImmutableSet<string> propNames,
        byte deep = 0)
    {
        return SplitPropInternal(doc.RootElement, propNames, null, deep);
    }

    /// <summary>
    /// Split operation, split properties according to a filter.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="propNames">The properties name or properties path separate with '.', for example: root.child.grandchild</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static SplitResult SplitProp(
        this JsonElement element,
        params string[] propNames)
    {
        var set = ImmutableHashSet.CreateRange(propNames);
        return SplitPropInternal(element, set, null);
    }

    /// <summary>
    /// Split operation, split properties according to a filter.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="deep">The deep.</param>
    /// <param name="propNames">The properties name or properties path separate with '.', for example: root.child.grandchild</param>
    /// <returns>
    /// positive: properties which match the filter.
    /// negative: properties which didn't match the filter.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static SplitResult SplitProp(
        this JsonElement element,
        byte deep,
        params string[] propNames)
    {
        var set = ImmutableHashSet.CreateRange(propNames);
        return SplitPropInternal(element, set, null, deep);
    }

    /// <summary>
    /// Split operation, split properties according to a filter.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="propNames">The properties name or properties path separate with '.', for example: root.child.grandchild</param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <returns>
    /// positive: properties which match the filter.
    /// negative: properties which didn't match the filter.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static SplitResult SplitProp(
        this JsonElement element,
        IImmutableSet<string> propNames,
        byte deep = 0)
    {
        return SplitPropInternal(element, propNames, null, deep);
    }

    /// <summary>
    /// Split operation, split properties according to a filter.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="propNames">The properties name or properties path separate with '.', for example: root.child.grandchild</param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <returns>
    /// positive: properties which match the filter.
    /// negative: properties which didn't match the filter.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static SplitResult SplitProp(
        this JsonElement element,
        IEnumerable<string> propNames,
        byte deep = 0)
    {
        var set = ImmutableHashSet.CreateRange(propNames);
        return SplitPropInternal(element, set, null, deep);
    }

    #endregion // SplitProp

    #region SplitChidProp


    /// <summary>
    /// Split properties according to a filter from a parent property.
    /// </summary>
    /// <param name="doc">The json document.</param>
    /// <param name="propNames">The properties name.</param>
    /// <param name="propParentName">The name of the parent property.</param>
    /// <returns>
    /// positive: properties under the parent which match the filter.
    /// negative: properties under the parent which didn't match the filter.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static SplitResult SplitChidProp(
        this JsonDocument doc,
        string propParentName,
        params string[] propNames)
    {
        var set = ImmutableHashSet.CreateRange(propNames);
        return SplitPropInternal(doc.RootElement, set, propParentName);
    }

    /// <summary>
    /// Split properties according to a filter from a parent property.
    /// </summary>
    /// <param name="doc">The json document.</param>
    /// <param name="propNames">The properties name.</param>
    /// <param name="propParentName">The name of the parent property.</param>
    /// <param name="deep">The recursive deep (0 (0 = ignores, 1 = only root elements)= only root elements).</param>
    /// <returns>
    /// positive: properties under the parent which match the filter.
    /// negative: properties under the parent which didn't match the filter.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static SplitResult SplitChidProp(
        this JsonDocument doc,
        string propParentName,
        IImmutableSet<string> propNames,
        byte deep = 0)
    {
        return SplitPropInternal(doc.RootElement, propNames, propParentName, deep);
    }

    /// <summary>
    /// Split properties according to a filter from a parent property.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="propNames">The properties name.</param>
    /// <param name="propParentName">The name of the parent property.</param>
    /// <returns>
    /// positive: properties under the parent which match the filter.
    /// negative: properties under the parent which didn't match the filter.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static SplitResult SplitChidProp(
        this JsonElement element,
        string propParentName,
        params string[] propNames)
    {
        var set = ImmutableHashSet.CreateRange(propNames);
        return SplitPropInternal(element, set, propParentName);
    }

    /// <summary>
    /// Split properties according to a filter from a parent property.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="propParentName">The name of the parent property.</param>
    /// <param name="propNames">The properties name.</param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <returns>
    /// positive: properties under the parent which match the filter.
    /// negative: properties under the parent which didn't match the filter.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static SplitResult SplitChidProp(
        this JsonElement element,
        string propParentName,
        IImmutableSet<string> propNames,
        byte deep = 0)
    {
        return SplitPropInternal(element, propNames, propParentName, deep);
    }

    /// <summary>
    /// Split properties according to a filter from a parent property.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="propParentName">The name of the parent property.</param>
    /// <param name="propNames">The properties name.</param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <returns>
    /// positive: properties under the parent which match the filter.
    /// negative: properties under the parent which didn't match the filter.
    /// </returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    public static SplitResult SplitChidProp(
        this JsonElement element,
        string propParentName,
        IEnumerable<string> propNames,
        byte deep = 0)
    {
        var set = ImmutableHashSet.CreateRange(propNames);
        return SplitPropInternal(element, set, propParentName, deep);
    }

    #endregion // SplitChidProp

    #region WhereImp

    /// <summary>
    /// Where operation, clean up the element according to the filter 
    /// (exclude whatever don't match the filter).
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="writer">The writer.</param>
    /// <param name="propFilter">
    /// </param>
    /// <param name="propNames">The property filter.</param>
    /// <param name="elementFilter"><![CDATA[
    /// The filter which determine whether to keep the element. 
    /// Func<element, path, deep, bool result>
    /// example: (element, path, deep) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
    /// </param>
    /// <param name="onRemove">On remove property notification.</param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <param name="curDeep">The current deep.</param>
    /// <param name="spine"></param>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    private static void WhereImp(
        this JsonElement element,
        Utf8JsonWriter writer,
        Func<JsonProperty, string, int, bool>? propFilter = null,
        IImmutableSet<string>? propNames = null,
        Func<JsonElement, string, int, bool>? elementFilter = null,
        Action<JsonProperty>? onRemove = null,
        byte deep = 0,
        byte curDeep = 0,
        string spine = "")
    {
        if (deep != 0 && curDeep >= deep)
        {
            element.WriteTo(writer);
            return;
        }
        if (element.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            foreach (JsonProperty e in element.EnumerateObject())
            {
                string curSpine = string.IsNullOrEmpty(spine) ? e.Name : $"{spine}.{e.Name}";
                bool isEquals = propFilter?.Invoke(e, curSpine, deep) ?? true;
                if (propNames != null)
                    isEquals = propNames.Contains(e.Name) || propNames.Contains(curSpine);
                if (!(isEquals))
                {
                    onRemove?.Invoke(e);
                    continue;
                }
                if (curDeep > deep)
                {
                    e.WriteTo(writer);
                    continue;
                }
                JsonElement v = e.Value;
                if (v.ValueKind == JsonValueKind.Object)
                {
                    writer.WritePropertyName(e.Name);
                    v.WhereImp(writer, propFilter, propNames, elementFilter, onRemove, deep, (byte)(curDeep + 1), curSpine);
                }
                else if (v.ValueKind == JsonValueKind.Array)
                {
                    writer.WritePropertyName(e.Name);
                    v.WhereImp(writer, propFilter, propNames, elementFilter, onRemove, deep, (byte)(curDeep + 1), curSpine);
                }
                else
                {
                    if (elementFilter?.Invoke(v, curSpine, deep) ?? true)
                        e.WriteTo(writer);
                    else if (propFilter == null && propNames == null)
                        writer.WriteNull(e.Name);
                }
            }
            writer.WriteEndObject();
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            int i = 0;
            foreach (JsonElement e in element.EnumerateArray())
            {
                string curSpine = string.IsNullOrEmpty(spine) ? $"[{i++}]" : $"{spine}.[{i++}]";
                if (elementFilter?.Invoke(e, curSpine, deep) ?? true)
                    e.WhereImp(writer, propFilter, propNames, elementFilter, onRemove, deep, (byte)(curDeep + 1), curSpine);
            }
            writer.WriteEndArray();
        }
        else
        {
            if (elementFilter?.Invoke(element, spine, deep) ?? true)
                element.WriteTo(writer);
        }
    }

    #endregion // WhereImp

    #region WherePropSetInternal

    /// <summary>
    /// Where operation, find properties according to a filter.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="propNames">The properties name.</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    internal static JsonElement WherePropSetInternal(
        JsonElement element,
        IImmutableSet<string> propNames)
    {
        var bufferWriterPositive = new ArrayBufferWriter<byte>();
        using (var writerPositive = new Utf8JsonWriter(bufferWriterPositive))
        {
            element.SplitPropImp(writerPositive, null, propNames);
        }
        JsonDocument? positive = null;
        if (bufferWriterPositive.WrittenSpan.Length > 0)
        {
            var readerPositive = new Utf8JsonReader(bufferWriterPositive.WrittenSpan);
            positive = JsonDocument.ParseValue(ref readerPositive);
        }
        return positive?.RootElement ?? Empty;
    }

    #endregion // WherePropSetInternal

    #region SplitPropInternal

    /// <summary>
    /// Split operation, split properties according to a filter.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="propNames">The properties name.</param>
    /// <param name="propParentName">The name of the parent property.</param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    internal static SplitResult SplitPropInternal(
        JsonElement element,
        IImmutableSet<string> propNames,
        string? propParentName = null,
        byte deep = 0)
    {
        var bufferWriterPositive = new ArrayBufferWriter<byte>();
        var bufferWriterNegative = new ArrayBufferWriter<byte>();
        using (var writerPositive = new Utf8JsonWriter(bufferWriterPositive))
        using (var writerNegative = new Utf8JsonWriter(bufferWriterNegative))
        {
            element.SplitPropImp(writerPositive, writerNegative, propNames, propParentName, deep);
        }
        JsonDocument? positive = null, negative = null;
        if (bufferWriterPositive.WrittenSpan.Length > 0)
        {
            var readerPositive = new Utf8JsonReader(bufferWriterPositive.WrittenSpan);
            positive = JsonDocument.ParseValue(ref readerPositive);
        }
        if (bufferWriterNegative.WrittenSpan.Length > 0)
        {
            var readerNegative = new Utf8JsonReader(bufferWriterNegative.WrittenSpan);
            negative = JsonDocument.ParseValue(ref readerNegative);
        }
        return new SplitResult(positive?.RootElement ?? Empty, negative?.RootElement ?? Empty);
    }

    #endregion // SplitPropInternal

    #region SplitPropImp

    /// <summary>
    /// Split operation, exclude properties according to a filter.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="positiveWriter">The positive writer.</param>
    /// <param name="negativeWriter">The negative writer.</param>
    /// <param name="propNames">The property name.</param>
    /// <param name="propParentName">The names of the parent property.</param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <param name="curDeep">The current deep.</param>
    /// <param name="isParentEquals"></param>
    /// <param name="spine"></param>
    /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
    private static void SplitPropImp(
        this JsonElement element,
        Utf8JsonWriter? positiveWriter,
        Utf8JsonWriter? negativeWriter,
        IImmutableSet<string> propNames,
        string? propParentName = null,
        byte deep = 0,
        byte curDeep = 0,
        bool isParentEquals = false,
        string spine = "")
    {
        bool hasParent = !string.IsNullOrEmpty(propParentName);
        if (positiveWriter == null && negativeWriter == null) return;

        if (deep != 0 && curDeep >= deep)
        {
            if (negativeWriter != null && !hasParent)
                element.WriteTo(negativeWriter);
            return;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            bool positiveStarted = false;
            bool negativeStarted = false;

            foreach (JsonProperty e in element.EnumerateObject())
            {
                JsonElement v = e.Value;
                string curSpine = string.IsNullOrEmpty(spine) ? e.Name : $"{spine}.{e.Name}";
                bool isEquals = propNames.Contains(e.Name) || propNames.Contains(curSpine);
                if (!hasParent)
                {
                    if (isEquals)
                    {
                        WritePositive();
                        continue;
                    }
                }
                else if (isParentEquals && isEquals)
                {
                    WritePositive();
                    if (hasParent)
                        continue;
                }

                if (hasParent && isParentEquals)
                {
                    WriteNegative();
                }
                else if (v.ValueKind == JsonValueKind.Object)
                {
                    WriteNegativeRec();
                }
                else if (v.ValueKind == JsonValueKind.Array)
                {
                    WriteNegativeRec();
                }
                else
                {
                    WriteNegative();
                }

                #region Local Methods: WritePositive(), WriteNegative(), WriteNegativeRec()

                void WritePositive()
                {
                    if (positiveWriter != null)
                    {
                        if (!positiveStarted)
                        {
                            positiveWriter.WriteStartObject();
                            positiveStarted = true;
                        }
                        positiveWriter.WritePropertyName(e.Name);
                        v.WriteTo(positiveWriter);
                    }
                }
                void WriteNegative()
                {
                    if (!hasParent || isParentEquals)
                    {
                        if (negativeWriter != null)
                        {
                            if (!negativeStarted)
                            {
                                negativeWriter.WriteStartObject();
                                negativeStarted = true;
                            }
                            negativeWriter.WritePropertyName(e.Name);
                            v.WriteTo(negativeWriter);
                        }
                    }
                }

                void WriteNegativeRec()
                {
                    if (!hasParent || isParentEquals)
                    {
                        if (!negativeStarted)
                        {
                            negativeWriter?.WriteStartObject();
                            negativeStarted = true;
                        }
                        negativeWriter?.WritePropertyName(e.Name);
                    }
                    v.SplitPropImp(positiveWriter, negativeWriter,
                                   propNames, propParentName,
                                   deep, (byte)(curDeep + 1),
                                   e.Name == propParentName,
                                   curSpine);
                }

                #endregion // Local Methods: WritePositive(), WriteNegative(), WriteNegativeRec()
            }
            if (positiveStarted)
                positiveWriter?.WriteEndObject();
            if (negativeStarted)
                negativeWriter?.WriteEndObject();
            else if (!hasParent || isParentEquals)
                negativeWriter?.WriteNullValue();
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            if (!hasParent || isParentEquals)
                negativeWriter?.WriteStartArray();
            foreach (JsonElement e in element.EnumerateArray())
            {
                e.SplitPropImp(positiveWriter, negativeWriter,
                               propNames, propParentName,
                               deep, (byte)(curDeep + 1),
                               isParentEquals,
                               spine);
            }
            if (!hasParent || isParentEquals)
                negativeWriter?.WriteEndArray();
        }
        else
        {
            if (negativeWriter != null && (!hasParent || isParentEquals))
                element.WriteTo(negativeWriter);
        }
    }

    #endregion // SplitPropImp

    #region TraverseProps

    /// <summary>
    /// TraversePropsProps over a json, will callback when find a property.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="propNames">The property name.</param>
    /// <param name="action">Action: (writer, current-element, current-path)</param>
    /// <exception cref="System.NotSupportedException">Only 'Object' or 'Array' element are supported</exception>
    public static JsonElement TraverseProps(
        this JsonElement element,
        Action<Utf8JsonWriter, JsonProperty, string> action,
        params string[] propNames)
    {
        var set = ImmutableHashSet.CreateRange(propNames);
        return TraverseProps(element, set, action);
    }

    /// <summary>
    /// TraversePropsProps over a json, will callback when find a property.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="propNames">The property name.</param>
    /// <param name="action">Action: (writer, current-element, current-path)</param>
    /// <param name="options"></param>
    /// <exception cref="System.NotSupportedException">Only 'Object' or 'Array' element are supported</exception>
    public static JsonElement TraverseProps(
        this JsonElement element,
        IImmutableSet<string> propNames,
        Action<Utf8JsonWriter, JsonProperty, string> action,
        TraversePropsOptions? options = default)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            element.TraverseProps(writer, propNames, action, options ?? new TraversePropsOptions(), 0);
        }

        var reader = new Utf8JsonReader(buffer.WrittenSpan);
        JsonDocument result = JsonDocument.ParseValue(ref reader);
        return result.RootElement;
    }

    /// <summary>
    /// TraversePropsProps over a json, will callback when find a property.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="writer">The positive writer.</param>
    /// <param name="propNames">The property name.</param>
    /// <param name="action">Action: (writer, current-element, current-path)</param>
    /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
    /// <param name="curDeep">The current deep.</param>
    /// <param name="spine"></param>
    /// <param name="options"></param>
    /// <exception cref="System.NotSupportedException">Only 'Object' or 'Array' element are supported</exception>
    private static void TraverseProps(
        this JsonElement element,
        Utf8JsonWriter writer,
        IImmutableSet<string> propNames,
        Action<Utf8JsonWriter, JsonProperty, string> action,
        TraversePropsOptions options,
        byte deep = 0,
        byte curDeep = 0,
        string spine = "")
    {
        if (deep != 0 && curDeep >= deep)
        {
            element.WriteTo(writer); ;
            return;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();

            foreach (JsonProperty e in element.EnumerateObject())
            {
                string curSpine = string.IsNullOrEmpty(spine) ? e.Name : $"{spine}.{e.Name}";
                bool isEquals = propNames.Contains(e.Name) || propNames.Contains(curSpine);
                JsonElement v = e.Value;
                if (isEquals)
                {
                    action(writer, e, curSpine);
                    continue;
                }


                else if (v.ValueKind == JsonValueKind.Object)
                {
                    WriteRec();
                }
                else if (v.ValueKind == JsonValueKind.Array)
                {
                    WriteRec();
                }
                else
                {
                    Write();
                }

                #region Local Methods: Write(), WriteRec()


                void WriteRec()
                {

                    writer.WritePropertyName(e.Name);

                    v.TraverseProps(writer, propNames, action, options, deep, (byte)(curDeep + 1), curSpine);
                }

                void Write()
                {
                    writer.WritePropertyName(e.Name);
                    v.WriteTo(writer);
                }

                #endregion // Local Methods: Write(), WriteRec()
            }
            writer.WriteEndObject();
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (JsonElement e in element.EnumerateArray())
            {
                e.TraverseProps(writer, propNames, action, options,
                               deep, (byte)(curDeep + 1),
                               spine);
            }
            writer.WriteEndArray();
        }
        else
        {
            element.WriteTo(writer);
        }
    }

    #endregion // TraverseProps

    #region CreateEmptyJsonElement

    /// <summary>
    /// Create Empty Json Element
    /// </summary>
    /// <returns></returns>
    private static JsonElement CreateEmptyJsonElement()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }

        var reader = new Utf8JsonReader(buffer.WrittenSpan);
        JsonDocument result = JsonDocument.ParseValue(ref reader);
        return result.RootElement;
    }

    #endregion // CreateEmptyJsonElement

    #region IntoProp

    /// <summary>
    /// Wrap Into a property of empty element.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="propName">Name of the property.</param>
    /// <returns></returns>
    public static JsonElement IntoProp(this JsonElement source, string propName)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WritePropertyName(propName);
            source.WriteTo(writer);
            writer.WriteEndObject();
        }

        var reader = new Utf8JsonReader(buffer.WrittenSpan);
        JsonDocument result = JsonDocument.ParseValue(ref reader);
        return result.RootElement;
    }

    #endregion // IntoProp

    #region AddIntoArray

    /// <summary>
    /// Adds the addition into existing json array.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="addition">The addition into the source.</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException"></exception>
    public static JsonElement AddIntoArray<T>(this JsonDocument source, params T[] addition) => AddIntoArray(source.RootElement, addition);

    /// <summary>
    /// Adds the addition into existing json array.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="addition">The addition into the source.</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException"></exception>
    public static JsonElement AddIntoArray<T>(this JsonElement source, params T[] addition)
    {
        if (addition.Length == 1)
            return source.AddIntoArray(addition[0].ToJson());
        return source.AddIntoArray(addition.Select(m => m.ToJson()).ToJson());
    }

    /// <summary>
    /// Adds the addition into existing json array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="options">The options.</param>
    /// <param name="addition">The addition into the source.</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException"></exception>
    public static JsonElement AddIntoArray<T>(this JsonElement source,
        JsonSerializerOptions options, params T[] addition)
    {
        if (addition.Length == 1)
            return source.AddIntoArray(addition[0].ToJson(options));
        return source.AddIntoArray(addition.Select(m => m.ToJson()).ToJson());
    }

    /// <summary>
    /// Adds the addition into existing json array.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="addition">The addition into the source.</param>
    /// <param name="deconstruct">if set to <c>true</c> will be merged into the source (deconstruct).</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException"></exception>
    public static JsonElement AddIntoArray(this JsonDocument source, JsonElement addition, bool deconstruct = true) => AddIntoArray(source.RootElement, addition, deconstruct);

    /// <summary>
    /// Adds the addition into existing json array.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="addition">The addition into the source.</param>
    /// <param name="deconstruct">if set to <c>true</c> will be merged into the source (deconstruct).</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException"></exception>
    public static JsonElement AddIntoArray(this JsonElement source, JsonElement addition, bool deconstruct = true)
    {
        if (source.ValueKind != JsonValueKind.Array)
            throw new NotSupportedException("Only array are eligible as source");
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartArray();
            foreach (var item in source.EnumerateArray())
            {
                item.WriteTo(writer);
            }
            if (addition.ValueKind == JsonValueKind.Array && deconstruct)
            {
                foreach (var item in addition.EnumerateArray())
                {
                    item.WriteTo(writer);
                }
            }
            else
                addition.WriteTo(writer);
            writer.WriteEndArray();
        }

        var reader = new Utf8JsonReader(buffer.WrittenSpan);
        JsonDocument result = JsonDocument.ParseValue(ref reader);
        return result.RootElement;
    }

    #endregion // AddIntoArray

    #region AddIntoObject

    /// <summary>
    /// Adds the addition into existing json object or json array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="addition">The addition into the source.</param>
    /// <param name="options">The options.</param>
    /// <returns></returns>
    public static JsonElement AddIntoObject<T>(this JsonDocument source, T addition,
        JsonSerializerOptions? options = null) => AddIntoObject(source.RootElement, addition, options);

    /// <summary>
    /// Adds the addition into existing json object.
    /// When source is an object, addition expected to be object as well.
    /// When source is an array, if the addition is an array it will be merged into the source (deconstruct)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source.</param>
    /// <param name="addition">The addition into the source.</param>
    /// <param name="options">The options.</param>
    /// <returns></returns>
    public static JsonElement AddIntoObject<T>(this JsonElement source, T addition,
        JsonSerializerOptions? options = null) => source.AddIntoObject(addition.ToJson(options));


    /// <summary>
    /// Adds the addition into existing json object or json array.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="addition">The addition into the source.</param>
    /// <returns></returns>
    public static JsonElement AddIntoObject(this JsonDocument source, JsonElement addition) => AddIntoObject(source.RootElement, addition);

    /// <summary>
    /// Adds the addition into existing json object.
    /// When source is an object, addition expected to be object as well.
    /// When source is an array, if the addition is an array it will be merged into the source (deconstruct)
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="addition">The addition into the source.</param>
    /// <returns></returns>
    public static JsonElement AddIntoObject(this JsonElement source, JsonElement addition)
    {
        if (source.ValueKind == JsonValueKind.Array)
            return AddIntoArray(source, addition);

        if (source.ValueKind != JsonValueKind.Object)
            throw new NotSupportedException("Only object are eligible as source");
        if (source.ValueKind == JsonValueKind.Object && addition.ValueKind != JsonValueKind.Object)
            throw new NotSupportedException("When source is an object addition expect to be object as well");
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            foreach (var item in source.EnumerateObject())
            {
                item.WriteTo(writer);
            }
            foreach (var item in addition.EnumerateObject())
            {
                item.WriteTo(writer);
            }
            writer.WriteEndObject();
        }

        var reader = new Utf8JsonReader(buffer.WrittenSpan);
        JsonDocument result = JsonDocument.ParseValue(ref reader);
        return result.RootElement;
    }

    #endregion // AddIntoObject

    #region TryAddProperty

    /// <summary>
    /// Try add property into a specific path in the json
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source json.</param>
    /// <param name="path">The path where the property should go into.</param>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The value of the property.</param>
    /// <param name="options">The options.</param>
    /// <returns></returns>
    public static JsonElement TryAddProperty<T>(
                                this JsonElement source,
                                string path,
                                string name,
                                T value,
                                JsonPropertyModificatonOpions? options = null)
    {
        if (string.IsNullOrEmpty(path))
        {
            return source.TryAddProperty(name, value, options);
        }
        bool caseSensitive = !(options?.Options?.PropertyNameCaseInsensitive ?? true);
        Func<JsonElement, int, IImmutableList<string>, TraverseFlowWrite> predicate =
                    CreatePathWriteFilter(path, caseSensitive);
        return source.Filter(predicate, OnTryAdd);

        JsonElement? OnTryAdd(JsonElement target, int deep, IImmutableList<string> breadcrumbs, TraverseFlowWrite flow)
        {
            if (flow == TraverseFlowWrite.Skip)
                return target;
            var result = target.TryAddProperty(name, value, options);
            return result;
        }

    }

    /// <summary>
    /// Try add property into a json object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="source">The source json.</param>
    /// <param name="name">The name of the property.</param>
    /// <param name="value">The value of the property.</param>
    /// <param name="options">The options.</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Only object are eligible as target for TryAddProperty</exception>
    public static JsonElement TryAddProperty<T>(
                                this JsonElement source,
                                string name,
                                T value,
                                JsonPropertyModificatonOpions? options = null)
    {
        if (source.ValueKind != JsonValueKind.Object)
            throw new NotSupportedException("Only object are eligible as target for TryAddProperty");

        options = options ?? new JsonPropertyModificatonOpions();

        bool ignoreCase = options.Options?.PropertyNameCaseInsensitive ?? true;
        bool ignoreNull = options.IgnoreNull;
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            bool exists = false;
            foreach (JsonProperty item in source.EnumerateObject())
            {
                bool shouldOverride = false;
                if (string.Compare(item.Name, name, ignoreCase) == 0)
                {
                    exists = true;
                    shouldOverride = item.Value.ValueKind switch
                    {
                        JsonValueKind.Null => ignoreNull,
                        JsonValueKind.Undefined => ignoreNull,
                        _ => false
                    };
                }
                if (shouldOverride)
                    WriteProp(item.Name);
                else
                    item.WriteTo(writer);
            }

            if (!exists)
                WriteProp(name);

            writer.WriteEndObject();

            void WriteProp(string propName)
            {
                writer.WritePropertyName(propName);
                var val = value.ToJson(options.Options);
                val.WriteTo(writer);
            }
        }

        var reader = new Utf8JsonReader(buffer.WrittenSpan);
        JsonDocument result = JsonDocument.ParseValue(ref reader);
        return result.RootElement;
    }

    #endregion // TryAddProperty
}
