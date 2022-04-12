using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

using Xunit;
using Xunit.Sdk;

using static Weknow.Text.Json.Constants;

namespace Weknow.Text.Json.Extensions.Tests
{
    record RecTest (int id, string name, ConsoleColor color);

    public class SerializeTests
    {
        [Fact]
        public void Serialize_Test()
        {
            var rec = new RecTest(10, "John", ConsoleColor.Cyan);
            string json = rec.Serialize();
#pragma warning disable xUnit2000 // Constants and literals should be the expected argument
            Assert.Equal(json, @"{
  ""id"": 10,
  ""name"": ""John"",
  ""color"": ""cyan""
}");
#pragma warning restore xUnit2000 // Constants and literals should be the expected argument
        }

        [Fact]
        public void DeSerialize_Test()
        {
            var rec = new RecTest(10, "John", ConsoleColor.Cyan);
            var j = JsonExtensions.Empty.MergeObject(rec);
            var result = j.Deserialize<RecTest>(Constants.SerializerOptions);
            Assert.Equal(rec.ToJson().AsString(), result.ToJson().AsString());
        }
    }
}
