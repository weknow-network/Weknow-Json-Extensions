using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    }
}
