using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Weknow.Text.Json
{
    /// <summary>
    /// Json related
    /// </summary>
    public static class Constants
    {
        private static readonly JsonStringEnumConverter EnumConvertor = new JsonStringEnumConverter(JsonNamingPolicy.CamelCase);

        #region Ctor

        /// <summary>
        /// Initializes the <see cref="Constants"/> class.
        /// </summary>
        static Constants()
        {
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                // PropertyNameCaseInsensitive = true,
                // IgnoreNullValues = true,
                WriteIndented = true,
                Converters = { EnumConvertor, /* JsonDictionaryConverter.Default, */ JsonImmutableDictionaryConverter.Default, JsonMemoryBytesConverterFactory.Default }
            };
            SerializerOptionsWithStandardDictionary = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                // PropertyNameCaseInsensitive = true,
                // IgnoreNullValues = true,
                WriteIndented = true,
                Converters = { EnumConvertor,  JsonDictionaryConverter.Default,  JsonImmutableDictionaryConverter.Default, JsonMemoryBytesConverterFactory.Default }
            };
            SerializerOptionsWithoutConverters = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                // PropertyNameCaseInsensitive = true,
                // IgnoreNullValues = true,
                WriteIndented = true,
                Converters = { EnumConvertor }
            };
        }

        #endregion // Ctor

        /// <summary>
        /// Gets the serializer options with indent.
        /// </summary>
        public static JsonSerializerOptions SerializerOptions { get; }

        /// <summary>
        /// Gets the serializer options with indent and with standard dictionary convertor .
        /// </summary>
        public static JsonSerializerOptions SerializerOptionsWithStandardDictionary { get; }
        /// <summary>
        /// Gets the serializer options with indent.
        /// </summary>
        public static JsonSerializerOptions SerializerOptionsWithoutConverters { get; }
    }

}
