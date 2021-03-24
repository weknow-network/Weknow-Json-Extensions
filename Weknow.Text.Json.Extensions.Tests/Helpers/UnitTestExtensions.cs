using System;
using System.Text.Json;

using static Weknow.Text.Json.Constants;

namespace Xunit
{
    public static class UnitTestExtensions
    {
        /// <summary>
        /// Asserts the serialization.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="testData">The test data.</param>
        /// <param name="equalityCheck">The equality check.</param>
        /// <param name="userMessage"></param>
        public static void AssertSerialization<T>(
            this T testData,
            Func<T, T, bool> equalityCheck = null,
            string userMessage = null,
            JsonSerializerOptions options = null)
        {
            options = options ?? SerializerOptions;
            string json = JsonSerializer.Serialize(testData, options);
            T deserialized = JsonSerializer.Deserialize<T>(json, options);

            bool equals = equalityCheck?.Invoke(testData, deserialized) ??
                          object.Equals(testData, deserialized);

            Assert.True(equals, userMessage);
        }
    }
}
