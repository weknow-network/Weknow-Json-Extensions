using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

// credit: https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-converters-how-to

namespace Weknow.Text.Json.Extensions
{
    /// <summary>
    /// Json immutable dictionary converter
    /// </summary>
    /// <seealso cref="System.Text.Json.Serialization.JsonConverterFactory" />
    public class JsonImmutableDictionaryConverter : JsonConverterFactory
    {
        public static readonly JsonImmutableDictionaryConverter Default = new JsonImmutableDictionaryConverter();

        #region Ctor

        /// <summary>
        /// Prevents a default instance of the <see cref="JsonImmutableDictionaryConverter"/> class from being created.
        /// </summary>
        private JsonImmutableDictionaryConverter()
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
            if (!typeToConvert.IsGenericType)
            {
                return false;
            }

            if (typeToConvert.GetGenericTypeDefinition() != typeof(ImmutableDictionary<,>))
            {
                return false;
            }

            return true;
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
            Type keyType = type.GetGenericArguments()[0];
            Type valueType = type.GetGenericArguments()[1];

            Type genType= typeof(ConvertStrategy<,>).MakeGenericType(
                    new Type[] { keyType, valueType });
            JsonConverter? converter = (JsonConverter?)Activator.CreateInstance(
                genType,
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] { options },
                culture: null);

            return converter ?? throw new NullReferenceException($"Fail to construct a json convertor for [{type.Name}]");
        }

        #endregion // CreateConverter

        /// <summary>
        /// Convert Strategy
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        private class ConvertStrategy<TKey, TValue> :
                JsonConverter<ImmutableDictionary<TKey, TValue>>
                where TKey : notnull
        {
            private readonly JsonConverter<TKey> _keyConverter;
            private readonly JsonConverter<TValue> _valueConverter;
            private Type _keyType;
            private Type _valueType;

            #region Ctor

            /// <summary>
            /// Initializes a new instance of the <see cref="ConvertStrategyX{TKey, TValue}"/> class.
            /// </summary>
            /// <param name="options">The options.</param>
            public ConvertStrategy(JsonSerializerOptions options)
            {
                // For performance, use the existing converter if available.
                _valueConverter = (JsonConverter<TValue>)options
                    .GetConverter(typeof(TValue));
                _keyConverter = (JsonConverter<TKey>)options
                    .GetConverter(typeof(TKey));

                // Cache the key and value types.
                _keyType = typeof(TKey);
                _valueType = typeof(TValue);
            }

            #endregion // Ctor

            #region Read

            /// <summary>
            /// Reads and converts the JSON to type <typeparamref name="T" />.
            /// </summary>
            /// <param name="reader">The reader.</param>
            /// <param name="typeToConvert">The type to convert.</param>
            /// <param name="options">An object that specifies serialization options to use.</param>
            /// <returns>
            /// The converted value.
            /// </returns>
            /// <exception cref="JsonException">
            /// </exception>
            public override ImmutableDictionary<TKey, TValue> Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException();
                }

                var dictionary = ImmutableDictionary.CreateBuilder<TKey, TValue>();

                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (reader.TokenType != JsonTokenType.StartArray)
                    {
                        throw new JsonException();
                    }

                    // Get the key.
                    reader.Read();

                    // Get the key.
                    TKey k;
                    if (_keyConverter != null)
                    {
                        k = _keyConverter.Read(ref reader, _keyType, options);
                    }
                    else
                    {
                        k = JsonSerializer.Deserialize<TKey>(ref reader, options);
                    }

                    // Get the value.
                    reader.Read();
                    TValue v;
                    if (_valueConverter != null)
                    {
                        v = _valueConverter.Read(ref reader, _valueType, options);
                    }
                    else
                    {
                        v = JsonSerializer.Deserialize<TValue>(ref reader, options);
                    }

                    // Add to dictionary.
                    dictionary.Add(k, v);

                    reader.Read(); // end array
                }

                return dictionary.ToImmutable();
            }

            #endregion // Read

            #region Write

            /// <summary>
            /// Writes the specified writer.
            /// </summary>
            /// <param name="writer">The writer.</param>
            /// <param name="dictionary">The dictionary.</param>
            /// <param name="options">The options.</param>
            public override void Write(
                Utf8JsonWriter writer,
                ImmutableDictionary<TKey, TValue> dictionary,
                JsonSerializerOptions options)
            {
                writer.WriteStartArray();

                foreach (KeyValuePair<TKey, TValue> kvp in dictionary)
                {
                    writer.WriteStartArray();
                    if (_keyConverter != null)
                    {
                        _keyConverter.Write(writer, kvp.Key, options);
                    }
                    else
                    {
                        JsonSerializer.Serialize(writer, kvp.Key, options);
                    }

                    if (_valueConverter != null)
                    {
                        _valueConverter.Write(writer, kvp.Value, options);
                    }
                    else
                    {
                        JsonSerializer.Serialize(writer, kvp.Value, options);
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndArray();
            }

            #endregion // Write
        }
    }
}
