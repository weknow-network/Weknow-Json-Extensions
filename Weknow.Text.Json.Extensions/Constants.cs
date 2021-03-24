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

        static Constants()
        {
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                // PropertyNameCaseInsensitive = true,
                // IgnoreNullValues = true,
                WriteIndented = true,
                Converters = { EnumConvertor, /* JsonDictionaryConverter.Default, */ JsonImmutableDictionaryConverter.Default  }
            };
            SerializerOptionsWithStandardDictionary = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                // PropertyNameCaseInsensitive = true,
                // IgnoreNullValues = true,
                WriteIndented = true,
                Converters = { EnumConvertor,  JsonDictionaryConverter.Default,  JsonImmutableDictionaryConverter.Default  }
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
        /// Gets the serializer options with indent & with standard dictionary convertor .
        /// </summary>
        public static JsonSerializerOptions SerializerOptionsWithStandardDictionary { get; }
        /// <summary>
        /// Gets the serializer options with indent.
        /// </summary>
        public static JsonSerializerOptions SerializerOptionsWithoutConverters { get; }
    }

}
