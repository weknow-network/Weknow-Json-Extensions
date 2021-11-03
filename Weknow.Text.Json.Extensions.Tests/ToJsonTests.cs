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
    public class ToJsonTests
    {
        private record BEntity(string C);
        private record Entity(int A, BEntity B);

        private readonly Entity ENTITY = new Entity(12, new BEntity("Z"));

        private const string JSON_INDENT =
@"{
  ""a"": 12,
  ""b"": {
    ""c"": ""Z""
  }
}";
        private static readonly JsonWriterOptions OPT_INDENT =
            new JsonWriterOptions { Indented = true };



        [Fact]
        public void ToStream_Default_Test()
        {
            var json = ENTITY.ToJson();
            string result = json.GetRawText();
            Assert.Equal(JSON_INDENT, result);
        }

        [Fact]
        public void ToStream_Default_To_Indent_Test()
        {
            var json = ENTITY.ToJson(SerializerOptions);
            string result = json.GetRawText();
            Assert.Equal(JSON_INDENT, result);
        }
    }
}
