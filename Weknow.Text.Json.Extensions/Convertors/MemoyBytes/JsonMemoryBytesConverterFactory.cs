using System.Text.Json.Serialization;

// credit: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to

namespace System.Text.Json
{
    public class JsonMemoryBytesConverterFactory : JsonConverterFactory
    {
        public static readonly JsonConverterFactory Default = new JsonMemoryBytesConverterFactory();
        #region Ctor

        /// <summary>
        /// Prevents a default instance of the <see cref="JsonImmutableDictionaryConverter"/> class from being created.
        /// </summary>
        private JsonMemoryBytesConverterFactory()
        {
        }

        #endregion // Ctor

        #region CanConvert

        /// <summary>
        /// When overridden in a derived class, determines whether the converter instance can convert the specified object type.
        /// </summary>
        /// <param name="typeToConvert">The type of the object to check whether it can be converted by this converter instance.</param>
        /// <returns>
        ///   <see langword="true" /> if the instance can convert the specified object type; otherwise, <see langword="false" />.
        /// </returns>
        public override bool CanConvert(Type typeToConvert)
        {
            if (typeToConvert == typeof(Memory<byte>) )
            {
                return true;
            }
            if (typeToConvert == typeof(ReadOnlyMemory<byte>) )
            {
                return true;
            }

            return false;
        }

        #endregion // CanConvert

        #region CreateConverter

        /// <summary>
        /// Creates the converter.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        /// <exception cref="System.NullReferenceException">Fail to construct a json convertor for [{type.Name}]</exception>
        public override JsonConverter CreateConverter(
            Type type,
            JsonSerializerOptions options)
        {
            if (type == typeof(Memory<byte>))
            {
                return JsonMemoryBytesConverter.Default;
            }
            if (type == typeof(ReadOnlyMemory<byte>))
            {
                return JsonReadOnlyMemoryBytesConverter.Default;
            }
            throw new NotSupportedException($"[{type.Name}] is not supported by {nameof(JsonMemoryBytesConverterFactory)}");
        }

        #endregion // CreateConverter}
    }
}
