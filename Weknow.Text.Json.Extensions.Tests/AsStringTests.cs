using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;

using Xunit;
using Xunit.Sdk;

using static Weknow.Text.Json.Extensions.Tests.Constants;

namespace Weknow.Text.Json.Extensions.Tests
{
    public class AsStringTests
    {
        private const string JSON = "{\"A\":12,\"B\":{\"C\":\"Z\"}}";

        private const string JSON_INDENT =
@"{
  ""A"": 12,
  ""B"": {
    ""C"": ""Z""
  }
}";
        private static readonly JsonWriterOptions OPT_INDENT =
            new JsonWriterOptions { Indented = true }; 



        [Fact]
        public void AsString_Default_Test()
        {
            var json = JsonDocument.Parse(JSON);
            string result = json.AsString();
            Assert.Equal(JSON, result);
        }

        [Fact]
        public void AsString_Default_To_Indent_Test()
        {
            var json = JsonDocument.Parse(JSON);
            string result = json.AsString(OPT_INDENT);
            Assert.Equal(JSON_INDENT, result);
        }

        [Fact]
        public void AsString_Indent_To_Default_Test()
        {
            var json = JsonDocument.Parse(JSON_INDENT);
            string result = json.AsString();
            Assert.Equal(JSON, result);
        }

        [Fact]
        public void AsString_Indent_To_Indent_Test()
        {
            var json = JsonDocument.Parse(JSON_INDENT);
            string result = json.AsString(OPT_INDENT);
            Assert.Equal(JSON_INDENT, result);
        }
    }
}
