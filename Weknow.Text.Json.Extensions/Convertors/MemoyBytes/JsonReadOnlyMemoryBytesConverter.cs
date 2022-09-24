using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

// credit: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to

namespace System.Text.Json
{

    /// <summary>
    /// <![CDATA[Json ReadOnlyMemory<byte>  converter]]>
    /// </summary>
    /// <seealso cref="System.Text.Json.Serialization.JsonConverter" />
    public class JsonReadOnlyMemoryBytesConverter : JsonConverter<ReadOnlyMemory<byte>>
    {
        public static readonly JsonConverter<ReadOnlyMemory<byte>> Default = new JsonReadOnlyMemoryBytesConverter();

        #region Read

        /// <summary>
        /// Reads and converts the JSON to type.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns>
        /// The converted value.
        /// </returns>
        /// <exception cref="JsonException">
        /// </exception>
        public override ReadOnlyMemory<byte> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            byte[] bytes = reader.GetBytesFromBase64();
            ReadOnlyMemory<byte> result = bytes.AsMemory();
            return result;
        }

        #endregion // Read

        #region Write

        /// <summary>
        /// Writes the specified writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="bytes">The dictionary.</param>
        /// <param name="options">The options.</param>
        public override void Write(
            Utf8JsonWriter writer,
            ReadOnlyMemory<byte> bytes,
            JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, bytes.ToArray(), options);
        }

        #endregion // Write

    }
}
