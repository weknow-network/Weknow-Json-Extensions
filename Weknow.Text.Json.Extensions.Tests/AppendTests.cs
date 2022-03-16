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
    public class AppendTests
    {
        private static readonly JsonWriterOptions OPT_INDENT =
                        new JsonWriterOptions { Indented = true };

        private readonly ITestOutputHelper _outputHelper;

        #region Ctor

        public AppendTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        #endregion Ctor


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

        private const string JSON_ARR1_INDENT = @"[""A"", ""B"",""C"" ]";
        private const string JSON_ARR2_INDENT = @"[1, 2, 3]";

        private void Write(JsonElement source, JsonElement positive, JsonElement negative)
        {
            _outputHelper.WriteLine("Source:-----------------");
            _outputHelper.WriteLine(source.AsString());
            _outputHelper.WriteLine("Element:-----------------");
            _outputHelper.WriteLine(positive.AsString());
            _outputHelper.WriteLine("Mereged:-----------------");
            _outputHelper.WriteLine(negative.AsString());
        }


        [Theory]
        [InlineData("{'A':1}", "{'B':2}", "{'A':1, 'B':2}")]
        public void Append_Theory_Test(string a, string b, string expected)
        {
            var source = JsonDocument.Parse(a.Replace('\'', '"')).RootElement;
            var element = JsonDocument.Parse(b.Replace('\'', '"')).RootElement;
            var expectedResult = JsonDocument.Parse(expected.Replace('\'', '"')).RootElement;
            var merged = source.Append(element);

            Write(source, element, merged);

            Assert.Equal(expectedResult.AsString(), merged.AsString());
        }

        [Fact]
        public void Append_Object_Test()
        {
            var source = JsonDocument.Parse(JSON1_INDENT).RootElement;
            var element = JsonDocument.Parse(JSON2_INDENT).RootElement;
            var merged= source.Append(element);

            Write(source, element, merged);

            Assert.Equal(JsonValueKind.Object, merged.ValueKind);
            Assert.True(merged.TryGetProperty("A", out _));
            Assert.True(merged.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.True(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
        }


        [Fact]
        public void Append_MultiObject_Test()
        {
            var source = JsonDocument.Parse(JSON1_INDENT).RootElement;
            var element0 = JsonDocument.Parse(JSON2_INDENT).RootElement;
            var element1 = JsonDocument.Parse(JSON3_INDENT).RootElement;
            var merged= source.Append(element0, element1);

            Write(source, element0, merged);

            Assert.Equal(JsonValueKind.Object, merged.ValueKind);
            Assert.True(merged.TryGetProperty("A", out _));
            Assert.True(merged.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.True(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
            Assert.True(merged.TryGetProperty("C", out var c));
            Assert.Equal(1, c[0].GetInt16());
            Assert.Equal(2, c[1].GetInt16());
            Assert.Equal(3, c[2].GetInt16());
        }

        [Fact]
        public void Append_Duplicate_MultiObject_Test()
        {
            var element0 = JsonDocument.Parse(JSON2_INDENT).RootElement;
            var source = JsonDocument.Parse(JSON1_INDENT).RootElement;
            var element2 = JsonDocument.Parse(JSON3_INDENT).RootElement;
            var merged= source.Append(element0, source, element2);

            Write(source, element0, merged);

            Assert.Equal(JsonValueKind.Object, merged.ValueKind);
            Assert.True(merged.TryGetProperty("A", out _));
            Assert.True(merged.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.True(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
            Assert.True(merged.TryGetProperty("C", out var c));
            Assert.Equal(1, c[0].GetInt16());
            Assert.Equal(2, c[1].GetInt16());
            Assert.Equal(3, c[2].GetInt16());
        }

        [Fact]
        public void Append_Into_Object_Test()
        {
            var source = JsonDocument.Parse(JSON2_INDENT).RootElement;
            var element = JsonDocument.Parse(JSON1_INDENT).RootElement;
            var merged= source.AppendIntoProp("B.B2", element);

            Write(source, element, merged);

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
        public void Append_Into_MultiObject_Test()
        {
            var source = JsonDocument.Parse(JSON2_INDENT).RootElement;
            var element1 = JsonDocument.Parse(JSON1_INDENT).RootElement;
            var element2 = JsonDocument.Parse(JSON3_INDENT).RootElement;
            var merged = source.AppendIntoProp("B.B2", element1, element2);

            Write(source, element1, merged);

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

        [Fact]
        public void Append_Array_Test()
        {
            var source = JsonDocument.Parse(JSON_ARR1_INDENT).RootElement;
            var element = JsonDocument.Parse(JSON_ARR2_INDENT).RootElement;
            var merged= source.Append(element);

            Write(source, element, merged);

            Assert.Equal(JsonValueKind.Array, merged.ValueKind);
            Assert.Equal("A", merged[0].GetString());
            Assert.Equal("B", merged[1].GetString());
            Assert.Equal("C", merged[2].GetString());
            Assert.Equal(1, merged[3].GetInt32());
            Assert.Equal(2, merged[4].GetInt32());
            Assert.Equal(3, merged[5].GetInt32());
        }
    }
}
