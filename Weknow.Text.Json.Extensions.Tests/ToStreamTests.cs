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
    public class ToStreamTests
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
        public void ToStream_Default_Test()
        {
            var json = JsonDocument.Parse(JSON);
            var srm = json.ToStream() as MemoryStream;
            string result = Encoding.UTF8.GetString(srm.ToArray()) ;
            Assert.Equal(JSON, result);
        }

        [Fact]
        public void ToStream_Default_To_Indent_Test()
        {
            var json = JsonDocument.Parse(JSON);
            var srm = json.ToStream(OPT_INDENT) as MemoryStream;
            string result = Encoding.UTF8.GetString(srm.ToArray());
            Assert.Equal(JSON_INDENT, result);
        }

        [Fact]
        public void ToStream_Indent_To_Default_Test()
        {
            var json = JsonDocument.Parse(JSON_INDENT);
            var srm = json.ToStream() as MemoryStream;
            string result = Encoding.UTF8.GetString(srm.ToArray());
            Assert.Equal(JSON, result);
        }

        [Fact]
        public void ToStream_Indent_To_Indent_Test()
        {
            var json = JsonDocument.Parse(JSON_INDENT);
            var srm = json.ToStream(OPT_INDENT) as MemoryStream;
            string result = Encoding.UTF8.GetString(srm.ToArray());
            Assert.Equal(JSON_INDENT, result);
        }
    }
}
