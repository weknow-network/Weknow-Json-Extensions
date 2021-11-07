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

    /// <summary>
    /// Json extensions
    /// </summary>
    public static class JsonIExtensions
    {
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
                return new JsonElement();
            var json = JsonDocument.Parse(j);
            return json.RootElement;
        }

        #endregion // ToJson

        #region WhereProp

        /// <summary>
        /// Where operation, exclude root level properties according to a filter.
        /// </summary>
        /// <param name="doc">The element.</param>
        /// <param name="filter">The filter which determine whether to keep the property.</param>
        /// <param name="deep">The recursive deep (0 = only root elements).</param>
        /// <param name="onRemove">On remove property notification.</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
        public static JsonDocument WhereProp(
            this JsonDocument doc,
            Func<JsonProperty, bool> filter,
            byte deep = 0,
            Action<JsonProperty>? onRemove = null)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(bufferWriter))
            {
                doc.RootElement.WhereImp(writer, filter, null, onRemove, deep);
            }
            var reader = new Utf8JsonReader(bufferWriter.WrittenSpan);
            var result = JsonDocument.ParseValue(ref reader);
            return result;
        }

        /// <summary>
        /// Where operation, exclude root level properties according to a filter.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="filter">The filter which determine whether to keep the property.</param>
        /// <param name="deep">The recursive deep (0 = only root elements).</param>
        /// <param name="onRemove">On remove property notification.</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
        public static JsonElement WhereProp(
            this JsonElement element,
            Func<JsonProperty, bool> filter,
            byte deep = 0,
            Action<JsonProperty>? onRemove = null)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(bufferWriter))
            {
                element.WhereImp(writer, filter, null, onRemove, deep);
            }
            var reader = new Utf8JsonReader(bufferWriter.WrittenSpan);
            var result = JsonDocument.ParseValue(ref reader);
            return result.RootElement;
        }

        #endregion // WhereProp

        #region Where

        /// <summary>
        /// Where operation, exclude json parts according to a filter.
        /// </summary>
        /// <param name="doc">The element.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="deep">The recursive deep (0 = only root elements).</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
        public static JsonDocument Where(this JsonDocument doc, Func<JsonElement, bool> filter, byte deep = 0)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(bufferWriter))
            {
                doc.RootElement.WhereImp(writer, null, filter, null, deep);
            }
            var reader = new Utf8JsonReader(bufferWriter.WrittenSpan);
            var result = JsonDocument.ParseValue(ref reader);
            return result;
        }

        /// <summary>
        /// Where operation, exclude json parts according to a filter.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="deep">The recursive deep (0 = only root elements).</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
        public static JsonElement Where(this JsonElement element, Func<JsonElement, bool> filter, byte deep = 0)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(bufferWriter))
            {
                element.WhereImp(writer, null, filter, null, deep);
            }
            var reader = new Utf8JsonReader(bufferWriter.WrittenSpan);
            var result = JsonDocument.ParseValue(ref reader);
            return result.RootElement;
        }

        #endregion // Where

        #region SplitProp

        /// <summary>
        /// Split operation, split root level properties according to a filter.
        /// </summary>
        /// <param name="doc">The element.</param>
        /// <param name="propName">The property name.</param>
        /// <param name="propParentName">The name of the parent property.</param>
        /// <param name="deep">The recursive deep (0 = only root elements).</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
        public static SplitResult SplitProp(
            this JsonDocument doc,
            string propName,
            string? propParentName = null,
            byte deep = 0)
        {
            return SplitProp(doc.RootElement, propName, propParentName, deep);
        }

        /// <summary>
        /// Split operation, split root level properties according to a filter.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="propName">The property name.</param>
        /// <param name="propParentName">The name of the parent property.</param>
        /// <param name="deep">The recursive deep (0 = only root elements).</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
        public static SplitResult SplitProp(
            this JsonElement element,
            string propName,
            string? propParentName = null,
            byte deep = 0)
        {
            var bufferWriterPositive = new ArrayBufferWriter<byte>();
            var bufferWriterNegative = new ArrayBufferWriter<byte>();
            using (var writerPositive = new Utf8JsonWriter(bufferWriterPositive))
            using (var writerNegative = new Utf8JsonWriter(bufferWriterNegative))
            {
                element.SplitPropImp(writerPositive, writerNegative, propName, propParentName, deep);
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
            return new SplitResult(positive?.RootElement ?? new JsonElement(), negative?.RootElement ?? new JsonElement());
        }

        #endregion // SplitProp

        #region WhereImp

        /// <summary>
        /// Where operation, exclude root level properties according to a filter.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="propFilter">The property filter.</param>
        /// <param name="elementFilter">The element filter.</param>
        /// <param name="onRemove">On remove property notification.</param>
        /// <param name="deep">The recursive deep (0 = only root elements).</param>
        /// <param name="curDeep">The current deep.</param>
        /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
        private static void WhereImp(
            this JsonElement element,
            Utf8JsonWriter writer,
            Func<JsonProperty, bool>? propFilter,
            Func<JsonElement, bool>? elementFilter,
            Action<JsonProperty>? onRemove,
            byte deep = 0,
            byte curDeep = 0)
        {
            if (curDeep > deep)
            {
                element.WriteTo(writer);
                return;
            }
            if (element.ValueKind == JsonValueKind.Object)
            {
                writer.WriteStartObject();
                foreach (JsonProperty e in element.EnumerateObject())
                {
                    if (!(propFilter?.Invoke(e) ?? true))
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
                        v.WhereImp(writer, propFilter, elementFilter, onRemove, deep, (byte)(curDeep + 1));
                    }
                    else if (v.ValueKind == JsonValueKind.Array)
                    {
                        writer.WritePropertyName(e.Name);
                        v.WhereImp(writer, propFilter, elementFilter, onRemove, deep, (byte)(curDeep + 1));
                    }
                    else
                    {
                        if (elementFilter?.Invoke(v) ?? true)
                            e.WriteTo(writer);
                        else if (propFilter == null)
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
                    if (elementFilter?.Invoke(e) ?? true)
                        e.WhereImp(writer, propFilter, elementFilter, onRemove, deep, (byte)(curDeep + 1));
                }
                writer.WriteEndArray();
            }
            else
            {
                if (elementFilter?.Invoke(element) ?? false)
                    element.WriteTo(writer);
            }
        }

        #endregion // WhereImp


        #region SplitPropImp

        /// <summary>
        /// Split operation, exclude root level properties according to a filter.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="positiveWriter">The positive writer.</param>
        /// <param name="negativeWriter">The negative writer.</param>
        /// <param name="propName">The property name.</param>
        /// <param name="propParentName">The name of the parent property.</param>
        /// <param name="onRemove">On remove property notification.</param>
        /// <param name="deep">The recursive deep (0 = only root elements).</param>
        /// <param name="curDeep">The current deep.</param>
        /// <param name="isParentEquals"></param>
        /// <exception cref="System.NotSupportedException">Only 'Object' element are supported</exception>
        private static void SplitPropImp(
            this JsonElement element,
            Utf8JsonWriter? positiveWriter,
            Utf8JsonWriter? negativeWriter,
            string propName,
            string? propParentName = null,
            byte deep = 0,
            byte curDeep = 0,
            bool isParentEquals = false)
        {
            if (positiveWriter == null && negativeWriter == null) return;

            if (curDeep > deep)
            {
                if (negativeWriter != null)
                    element.WriteTo(negativeWriter);
                return;
            }

            if (element.ValueKind == JsonValueKind.Object)
            {

                negativeWriter?.WriteStartObject();

                foreach (JsonProperty e in element.EnumerateObject())
                {
                    if (curDeep > deep)
                    {
                        if (negativeWriter != null)
                            e.WriteTo(negativeWriter);
                        continue;
                    }
                    JsonElement v = e.Value;
                    bool isEquals = e.Name == propName;
                    if (propParentName == null)
                    {
                        if (isEquals)
                        {
                            if (positiveWriter != null)
                            {

                                positiveWriter.WriteStartObject();
                                positiveWriter.WritePropertyName(e.Name);
                                v.WriteTo(positiveWriter);
                                positiveWriter.WriteEndObject();
                            }
                            continue;
                        }
                    }
                    else if (isParentEquals)
                    {
                        if (isEquals)
                        {
                            if (positiveWriter != null)
                            {

                                positiveWriter.WriteStartObject();
                                positiveWriter.WritePropertyName(e.Name);
                                v.WriteTo(positiveWriter);
                                positiveWriter.WriteEndObject();
                            }
                        }
                    }

                    if (v.ValueKind == JsonValueKind.Object)
                    {
                        negativeWriter?.WritePropertyName(e.Name);
                        v.SplitPropImp(positiveWriter, negativeWriter,
                                       propName, propParentName,
                                       deep, (byte)(curDeep + 1),
                                       e.Name == propParentName);
                    }
                    else if (v.ValueKind == JsonValueKind.Array)
                    {
                        negativeWriter?.WritePropertyName(e.Name);
                        v.SplitPropImp(positiveWriter, negativeWriter,
                                       propName, propParentName,
                                       deep, (byte)(curDeep + 1),
                                       e.Name == propParentName);
                    }
                    else
                    {
                        if (negativeWriter != null)
                        {
                            negativeWriter.WritePropertyName(e.Name);
                            v.WriteTo(negativeWriter);
                            continue;
                        }
                    }
                }
                negativeWriter?.WriteEndObject();
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                negativeWriter?.WriteStartArray();
                foreach (JsonElement e in element.EnumerateArray())
                {
                    e.SplitPropImp(positiveWriter, negativeWriter,
                                   propName, propParentName,
                                   deep, (byte)(curDeep + 1),
                                   isParentEquals);
                }
                negativeWriter?.WriteEndArray();
            }
            else
            {
                if (negativeWriter != null)
                    element.WriteTo(negativeWriter);
            }
        }

        #endregion // SplitPropImp
    }
}
