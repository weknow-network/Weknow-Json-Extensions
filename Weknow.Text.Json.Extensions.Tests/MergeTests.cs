using FakeItEasy;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

using static Weknow.Text.Json.Constants;

namespace Weknow.Text.Json.Extensions.Tests
{
    public class MergeTests
    {
        private static readonly JsonWriterOptions OPT_INDENT =
                        new JsonWriterOptions { Indented = true };

        private readonly ITestOutputHelper _outputHelper;

        #region Ctor

        public MergeTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        #endregion Ctor


        private const string JSON_ARR1_INDENT = @"[""A"", ""B"",""C"" ]";
        private const string JSON_ARR2_INDENT = @"[1, 2, 3]";

        #region Write

        private void Write(JsonElement result, JsonElement expected, JsonElement source, IEnumerable<JsonElement> joined)
        {
            _outputHelper.WriteLine("Result:-----------------");
            _outputHelper.WriteLine(result.AsString());
            _outputHelper.WriteLine("Expectes:-----------------");
            _outputHelper.WriteLine(expected.AsString());
            _outputHelper.WriteLine("");
            _outputHelper.WriteLine("Source:-----------------");
            _outputHelper.WriteLine(source.AsString());
            int i = 0;
            foreach (var item in joined)
            {
                _outputHelper.WriteLine($"Element [{i++}]:-----------------");
                _outputHelper.WriteLine(item.AsString());
            }
        }

        #endregion // Write


        [Theory]
        [InlineData("{'A':1, 'B':2}", "{'A':1}", "{'B':2}")]
        [InlineData("{'A':1, 'B':2, 'C':3}", "{'A':1}", "{'B':2}", "{'C':3}")]
        [InlineData("{'A':2, 'B':3, 'C':3}", "{'A':1}", "{'A':2, 'B':2}", "{'B':3, 'C':3}")]
        [InlineData("{'A':2, 'B':3, 'C':[1,2,3]}", "{'A':1}", "{'A':2, 'B':[2,4,6],'C':'Z'}", "{'B':3, 'C':[1,2,3]}")]
        [InlineData("[1,2,3,4,5,6]", "[1,2,3]", "[4,5,6]")]
        [InlineData("[1,2,3,3,4,5,6]", "[1,2,3]", "[3,4,5,6]")]
        [InlineData("[1,2,3,'3','4','5','6']", "[1,2,3]", "['3','4','5','6']")]
        [InlineData("{'A':1}", "[1,2,3]", "{'A':1}")]
        [InlineData("[1,2,3]", "{'A':1}", "[1,2,3]")]
        [InlineData("{'A':2}", "{'A':{'A1':1}}", "{'A':2}")]
        [InlineData("{'A':{'A1':1,'A2':2}}", "{'A':{'A1':1}}", "{'A':{'A2':2}}")]
        public void Merge_Theory_Test(string expected, string source, params string[] joined)
        {
            var sourceElement = JsonDocument.Parse(source.Replace('\'', '"')).RootElement;
            var joinedElement = joined.Select(b =>  JsonDocument.Parse(b.Replace('\'', '"')).RootElement);
            var expectedResult = JsonDocument.Parse(expected.Replace('\'', '"')).RootElement;
            var merged = sourceElement.Merge(joinedElement);

            Write(expectedResult, merged, sourceElement, joinedElement);

            Assert.Equal(expectedResult.AsString(), merged.AsString());
        }

        [Fact]
        public void Merge_Object_Test()
        {
            var sourceElement = JsonDocument.Parse("{'A':1}".Replace('\'', '"')).RootElement;
            var joinedElement = new { B = 2};
            var expectedResult = JsonDocument.Parse("{'A':1, 'b':2}".Replace('\'', '"')).RootElement;
            var merged = sourceElement.Merge(joinedElement);

            Write(expectedResult, merged, sourceElement, new [] { joinedElement.ToJson() });

            Assert.Equal(expectedResult.AsString(), merged.AsString());
        }

        [Fact]
        public void MergeInto_Object_Test()
        {
            var sourceElement = JsonDocument.Parse("{'A':1,'B':{'B1':[1,2,3]}}".Replace('\'', '"')).RootElement;
            var joinedElement = new { X = "Y"};
            var expectedResult = JsonDocument.Parse("{'A':1, 'B':{'B1':[1,{'x':'Y'},3]}}".Replace('\'', '"')).RootElement;
            var merged = sourceElement.MergeInto("B.B1.[1]", joinedElement);

            Write(expectedResult, merged, sourceElement, new [] { joinedElement.ToJson() });

            Assert.Equal(expectedResult.AsString(), merged.AsString());
        }

        [Theory]

        [InlineData("B", "{'A':1, 'B':{'B1':3, 'C':[1,2,3]}}", "{'A':1,'B': {'B1':1}}", "{'B1':3, 'C':[1,2,3]}")]
        [InlineData("B.B1.[1]", "{'A':1, 'B':{'B1':[1,5,3]}}", "{'A':1,'B':{'B1':[1,2,3]}}", "5")]
        [InlineData("B.B1.[1]", "{'A':1, 'B':{'B1':[1,{'X':'Y'},3]}}", "{'A':1,'B':{'B1':[1,2,3]}}", "{'X':'Y'}")]
        [InlineData("B.B1.[]", "{'A':1, 'B':{'B1':[{'X':'Y'},{'X':'Y'},{'X':'Y'}]}}", "{'A':1,'B':{'B1':[1,2,3]}}", "{'X':'Y'}")]
        public void MergeInto_Theory_Test(string path, string expected, string source, params string[] joined)
        {
            var sourceElement = JsonDocument.Parse(source.Replace('\'', '"')).RootElement;
            var joinedElement = joined.Select(b =>  JsonDocument.Parse(b.Replace('\'', '"')).RootElement);
            var expectedResult = JsonDocument.Parse(expected.Replace('\'', '"')).RootElement;
            var merged = sourceElement.MergeInto(path, joinedElement);

            Write(expectedResult, merged, sourceElement, joinedElement);

            Assert.Equal(expectedResult.AsString(), merged.AsString());
        }


        private const string JSON_INDENT =
@"{
  ""A"": 12,
  ""B"": {
    ""B1"": ""Cool"",
    ""B2"": {
        ""B21"": {
          ""B211"": 211
        },
        ""B22"": 22    
    }
  },
  ""C"": [""C1"", ""C2""],
  ""D"": [{ ""D1"": 1}, ""D2"", 3]
}
";
        private const string JSON1_INDENT =
@"{
  ""A"": 12
}
";
        private const string JSON2_INDENT =
@"{
  ""B"": {
    ""B1"": ""Cool"",
    ""B2"": {
        ""B21"": {
          ""B211"": 211
        },
        ""B22"": 22    
    }
  }
}
";

        private const string JSON3_INDENT =
@"{
  ""C"": [1, 2, 3]
}
";


        [Fact]
        public void Merge_Into_Object_Test()
        {
            var source = JsonDocument.Parse(JSON2_INDENT).RootElement;
            var element = JsonDocument.Parse(JSON1_INDENT).RootElement;
            var merged = source.MergeInto("B.B2", element);

            Write(merged, JsonExtensions.Empty, source, new[] { element });

            Assert.Equal(JsonValueKind.Object, merged.ValueKind);
            Assert.False(merged.TryGetProperty("A", out _));
            Assert.True(merged.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.True(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
            Assert.True(b2.TryGetProperty("A", out _));
        }

        [Fact]
        public void Merge_Into_MultiObject_Test()
        {

            var source = JsonDocument.Parse(JSON2_INDENT).RootElement;
            var element1 = JsonDocument.Parse(JSON1_INDENT).RootElement;
            var element2 = JsonDocument.Parse(JSON3_INDENT).RootElement;
            var merged = source.MergeInto("B.B2", element1, element2);

            Write(merged, JsonExtensions.Empty, source, new[] { element1, element2 });

            Assert.Equal(JsonValueKind.Object, merged.ValueKind);
            Assert.False(merged.TryGetProperty("A", out _));
            Assert.True(merged.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.True(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
            Assert.True(b2.TryGetProperty("A", out _));
            Assert.True(b2.TryGetProperty("C", out var c));
            Assert.Equal(1, c[0].GetInt16());
            Assert.Equal(2, c[1].GetInt16());
            Assert.Equal(3, c[2].GetInt16());
        }

    }
}
