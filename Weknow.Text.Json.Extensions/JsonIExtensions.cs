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

// credit: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to

namespace System.Text.Json
{

    // Determine the traversing flow
    public enum TraverseFlow
    {
        /// <summary>
        /// Skip and continue the parent flow.
        /// </summary>
        BackToParent,
        /// <summary>
        /// Continue to sibling or ancestor's sibling (not children).
        /// </summary>
        Continue,
        /// <summary>
        /// Stop traversing
        /// </summary>
        Break,
        /// <summary>
        /// Drill into children and continue to sibling or ancestor's sibling.
        /// </summary>
        Drill,
        /// <summary>
        /// Drill into children and then break (don't continue to sibling or ancestor's sibling).
        /// </summary>
        DrillAndBreak
    }

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

        #region DeepFilter

        /// <summary>
        /// Filters descendant element by predicate.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="predicate">
        /// The predicate: (current, deep, breadcrumbs spine) =&gt; (should yield, flow strategy).
        /// deep: start at 0.
        /// breadcrumbs spine: spine of ancestor's properties and arrays index.
        /// </param>
        /// <returns></returns>
        public static IEnumerable<JsonElement> DeepFilter(this JsonDocument source, Func<JsonElement, int, IImmutableList<string>, (bool shpuldYield, TraverseFlow flowStrategy)> predicate)
        {
            return source.RootElement.DeepFilter(0, ImmutableList<string>.Empty, predicate);
        }

        /// <summary>
        /// Filters descendant element by predicate.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="predicate">
        /// The predicate: (current, deep, breadcrumbs spine) =&gt; (should yield, flow strategy).
        /// deep: start at 0.
        /// breadcrumbs spine: spine of ancestor's properties and arrays index.
        /// </param>
        /// <returns></returns>
        public static IEnumerable<JsonElement> DeepFilter(this JsonElement source, Func<JsonElement, int, IImmutableList<string>, (bool shpuldYield, TraverseFlow flowStrategy)> predicate)
        {
            return source.DeepFilter(0, ImmutableList<string>.Empty, predicate);
        }

        /// <summary>
        /// Filters descendant element by predicate.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="deep">The deep.</param>
        /// <param name="spine">The breadcrumbs spine.</param>
        /// <param name="predicate">The predicate: (current, deep, breadcrumbs spine) =&gt; (should yield, flow strategy).
        /// deep: start at 0.
        /// breadcrumbs spine: spine of ancestor's properties and arrays index.</param>
        /// <returns></returns>
        private static IEnumerable<JsonElement> DeepFilter(
                                this JsonElement source,
                                int deep,
                                IImmutableList<string> spine,
                                Func<JsonElement, int, IImmutableList<string>, (bool shpuldYield, TraverseFlow flowStrategy)> predicate)
        {
            if (source.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty p in source.EnumerateObject())
                {
                    var spn = spine.Add(p.Name);
                    var val = p.Value;
                    var (shouldYield, flowStrategy) = predicate(val, deep, spn);
                    if (shouldYield) yield return val;
                    if (flowStrategy == TraverseFlow.Break)
                        yield break;
                    if (flowStrategy == TraverseFlow.Drill ||
                        flowStrategy == TraverseFlow.DrillAndBreak)
                    {
                        foreach (var result in val.DeepFilter(deep + 1, spn, predicate))
                        {
                            yield return result;
                        }
                        if (flowStrategy == TraverseFlow.BackToParent)
                            break;
                        if (flowStrategy == TraverseFlow.DrillAndBreak)
                            yield break;
                    }
                }
            }
            else if (source.ValueKind == JsonValueKind.Array)
            {
                int i = 0;
                foreach (JsonElement val in source.EnumerateArray())
                {
                    var spn = spine.Add((i++).ToString());
                    var (shouldYield, flowStrategy) = predicate(val, deep, spn);
                    if (shouldYield) yield return val;
                    if (flowStrategy == TraverseFlow.Break)
                        yield break;
                    if (flowStrategy == TraverseFlow.Drill ||
                        flowStrategy == TraverseFlow.DrillAndBreak)
                    {
                        foreach (var result in val.DeepFilter(deep + 1, spn, predicate))
                        {
                            yield return result;
                        }
                        if (flowStrategy == TraverseFlow.BackToParent)
                            break;
                        if (flowStrategy == TraverseFlow.DrillAndBreak)
                            yield break;
                    }
                }
            }

        }

        #endregion // DeepFilter

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
            if (json.ValueKind == JsonValueKind.Undefined)
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
            if (json.ValueKind == JsonValueKind.Undefined)
                return String.Empty;
            using var ms = new MemoryStream();
            using (var w = new Utf8JsonWriter(ms, INDENTED_JSON_OPTIONS))
            {
                json.WriteTo(w);
            }
            var result = Encoding.UTF8.GetString(ms.ToArray());
            return result;
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
        /// example: (element, propertyPath) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
        /// </param>
        /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
        /// <param name="onRemove">On remove property notification.</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
        public static JsonDocument WhereProp(
            this JsonDocument doc,
            Func<JsonProperty, string, bool> filter,
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
        /// example: (element, propertyPath) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
        /// </param>
        /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
        /// <param name="onRemove">On remove property notification.</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
        public static JsonElement WhereProp(
            this JsonElement element,
            Func<JsonProperty, string, bool> filter,
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
        /// example: (element, propertyPath) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
        /// </param>
        /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
        public static JsonDocument Where(this JsonDocument doc, Func<JsonElement, string, bool> filter, byte deep = 0)
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
        /// example: (element, propertyPath) => element.ValueKind != JsonValueKind.Number && propertyPath == "root.child";]]>
        /// </param>
        /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
        public static JsonElement Where(this JsonElement element, Func<JsonElement, string, bool> filter, byte deep = 0)
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
        /// <returns></returns>
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
        /// <param name="propNames">The properties name or properties path separate with '.', for example: root.child.grandchild</param>
        /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
        /// <returns></returns>
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
        /// <param name="propNames">The properties name or properties path separate with '.', for example: root.child.grandchild</param>
        /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// Split operation, split properties according to a filter.
        /// </summary>
        /// <param name="doc">The json document.</param>
        /// <param name="propNames">The properties name.</param>
        /// <param name="propParentName">The name of the parent property.</param>
        /// <returns></returns>
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
        /// Split operation, split properties according to a filter.
        /// </summary>
        /// <param name="doc">The json document.</param>
        /// <param name="propNames">The properties name.</param>
        /// <param name="propParentName">The name of the parent property.</param>
        /// <param name="deep">The recursive deep (0 (0 = ignores, 1 = only root elements)= only root elements).</param>
        /// <returns></returns>
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
        /// Split operation, split properties according to a filter.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="propNames">The properties name.</param>
        /// <param name="propParentName">The name of the parent property.</param>
        /// <returns></returns>
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
        /// Split operation, split properties according to a filter.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="propParentName">The name of the parent property.</param>
        /// <param name="propNames">The properties name.</param>
        /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
        /// <returns></returns>
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
        /// Split operation, split properties according to a filter.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="propParentName">The name of the parent property.</param>
        /// <param name="propNames">The properties name.</param>
        /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
        /// <returns></returns>
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
        /// <param name="propFilter">The property filter.</param>
        /// <param name="propNames">The property filter.</param>
        /// <param name="elementFilter">The element filter.</param>
        /// <param name="onRemove">On remove property notification.</param>
        /// <param name="deep">The recursive deep (0 = ignores, 1 = only root elements).</param>
        /// <param name="curDeep">The current deep.</param>
        /// <param name="spine"></param>
        /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
        private static void WhereImp(
            this JsonElement element,
            Utf8JsonWriter writer,
            Func<JsonProperty, string, bool>? propFilter = null,
            IImmutableSet<string>? propNames = null,
            Func<JsonElement, string, bool>? elementFilter = null,
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
                    bool isEquals = propFilter?.Invoke(e, curSpine) ?? true;
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
                        if (elementFilter?.Invoke(v, curSpine) ?? true)
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
                foreach (JsonElement e in element.EnumerateArray())
                {
                    if (elementFilter?.Invoke(e, spine) ?? true)
                        e.WhereImp(writer, propFilter, propNames, elementFilter, onRemove, deep, (byte)(curDeep + 1), spine);
                }
                writer.WriteEndArray();
            }
            else
            {
                if (elementFilter?.Invoke(element, spine) ?? true)
                    element.WriteTo(writer);
            }
        }

        #endregion // WhereImp

        #region Merge

        /// <summary>
        /// Merge Json elements
        /// </summary>
        /// <param name="source"></param>
        /// <param name="elements"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static JsonElement Merge(
            this JsonDocument source,
            params JsonElement[] elements)
        {
            return source.RootElement.Merge(elements);
        }

        /// <summary>
        /// Merge Json elements
        /// </summary>
        /// <param name="source"></param>
        /// <param name="elements"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static JsonElement Merge(
            this JsonElement source,
            params JsonElement[] elements)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.Merge(source, elements);
            }

            var reader = new Utf8JsonReader(buffer.WrittenSpan);
            JsonDocument result = JsonDocument.ParseValue(ref reader);
            return result.RootElement;

        }

        /// <summary>
        /// Merge Json elements
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="source"></param>
        /// <param name="elements"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static void Merge(
            this Utf8JsonWriter writer,
            JsonElement source,
            params JsonElement[] elements)
        {
            if (source.ValueKind != JsonValueKind.Object && source.ValueKind != JsonValueKind.Array)
                throw new NotSupportedException("Only json object or array are supported, both source and element should be json object");

            var buffer = new ArrayBufferWriter<byte>();
            if (source.ValueKind == JsonValueKind.Object)
            {
                writer.WriteStartObject();

                foreach (JsonProperty e in source.EnumerateObject())
                {
                    JsonElement v = e.Value;
                    writer.WritePropertyName(e.Name);
                    v.WriteTo(writer);

                }
                foreach (JsonElement element in elements)
                {
                    #region Validation

                    if (source.ValueKind != element.ValueKind)
                        throw new NotSupportedException("Both json must be of same kind");

                    #endregion // Validation

                    foreach (JsonProperty e in element.EnumerateObject())
                    {
                        JsonElement v = e.Value;
                        writer.WritePropertyName(e.Name);
                        v.WriteTo(writer);

                    }
                }
                writer.WriteEndObject();
            }
            else
            {
                writer.WriteStartArray();

                foreach (JsonElement e in source.EnumerateArray())
                {
                    e.WriteTo(writer);

                }
                foreach (JsonElement element in elements)
                {
                    #region Validation

                    if (source.ValueKind != element.ValueKind)
                        throw new NotSupportedException("Both json must be of same kind");

                    #endregion // Validation

                    foreach (JsonElement e in element.EnumerateArray())
                    {
                        e.WriteTo(writer);
                    }
                }
                writer.WriteEndArray();
            }
        }


        #endregion // Merge

        #region MergeIntoProp

        /// <summary>
        /// Merge Json elements
        /// </summary>
        /// <param name="source"></param>
        /// <param name="elements"></param>
        /// <param name="sourceTargetProp">
        /// The property on the source which the element sould be merged into.
        /// It can be either property name of property path with '.' separator.
        /// </param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static JsonElement MergeIntoProp(
            this JsonElement source,
            string sourceTargetProp,
            params JsonElement[] elements)
        {
            var set = ImmutableHashSet.Create(sourceTargetProp);
            return source.TraverseProps(set, MergeInto);

            void MergeInto(Utf8JsonWriter w, JsonProperty target, string path)
            {
                w.WritePropertyName(target.Name);

                w.Merge(target.Value, elements);
            }
        }

        #endregion // MergeIntoProp

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
    }
}
