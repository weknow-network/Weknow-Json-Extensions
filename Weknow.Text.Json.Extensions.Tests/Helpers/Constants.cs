using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Weknow.Text.Json.Extensions.Tests
{
    /// <summary>
    /// Json related
    /// </summary>
    public static class Constants
    {
        private static readonly JsonStringEnumConverter EnumConvertor = new JsonStringEnumConverter(JsonNamingPolicy.CamelCase);
        public static readonly JsonImmutableDictionaryConverter ImmutableDictionaryConverter = new JsonImmutableDictionaryConverter();
        public static readonly JsonDictionaryConverter DictionaryConverter = new JsonDictionaryConverter();

        static Constants()
        {
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                // PropertyNameCaseInsensitive = true,
                // IgnoreNullValues = true,
                WriteIndented = true,
                Converters = { EnumConvertor, DictionaryConverter, ImmutableDictionaryConverter }
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

        /// <summary>
        /// Gets the serializer options with indent.
        /// </summary>
        public static JsonSerializerOptions SerializerOptions { get; }
        /// <summary>
        /// Gets the serializer options with indent.
        /// </summary>
        public static JsonSerializerOptions SerializerOptionsWithoutConverters { get; }
    }

}
