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
    public class AsJsonTests
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


        [Fact]
        public void AsString_Default_Test()
        {
            var json = ENTITY.ToJson();
            string result = json.GetRawText();
            Assert.Equal(JSON_INDENT, result);
        }

        [Fact]
        public void AsString_Array_Test()
        {
            var arr = new []{ 1, 2, 3 };
            var json = arr.ToJson();
            string result = json.AsString();
            Assert.Equal("[1,2,3]", result);
        }

        [Fact]
        public void AsString_Enumerable_Test()
        {
            var arr = new []{ 1, 2, 3 }.Select(m => m);
            var json = arr.ToJson();
            string result = json.AsString();
            Assert.Equal("[1,2,3]", result);
        }
        [Fact]
        public void AsAsIndentStringTest()
        {
            var json = ENTITY.ToJson();
            string result = json.AsIndentString();
            Assert.Equal(JSON_INDENT, result);
        }

        [Fact]
        public void AsString_Default_To_Indent_Test()
        {
            var json = ENTITY.ToJson(SerializerOptions);
            string result = json.GetRawText();
            Assert.Equal(JSON_INDENT, result);
        }
    }
}
