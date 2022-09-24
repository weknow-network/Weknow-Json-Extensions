using System.Text.Json.Serialization;

// credit: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to

namespace System.Text.Json
{
    /// <summary>
    /// <![CDATA[Json Memory<byte>  converter]]>
    /// </summary>
    /// <seealso cref="System.Text.Json.Serialization.JsonConverter" />
    public class JsonMemoryBytesConverter : JsonConverter<Memory<byte>>
    {
        public static readonly JsonConverter<Memory<byte>> Default = new JsonMemoryBytesConverter();

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
        public override Memory<byte> Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var bytes = reader.GetBytesFromBase64();
            return bytes.AsMemory();
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
            Memory<byte> bytes,
            JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, bytes.ToArray(), options);
        }

        #endregion // Write

    }
}
