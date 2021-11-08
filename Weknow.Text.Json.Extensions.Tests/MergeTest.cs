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
    public class MergeTest
    {
        private static readonly JsonWriterOptions OPT_INDENT =
                        new JsonWriterOptions { Indented = true };

        private readonly ITestOutputHelper _outputHelper;

        #region Ctor

        public MergeTest(ITestOutputHelper outputHelper)
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


        [Fact]
        public void Merge_Object_Test()
        {
            var source = JsonDocument.Parse(JSON1_INDENT).RootElement;
            var element = JsonDocument.Parse(JSON2_INDENT).RootElement;
            var merged= source.Merge(element);

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
        public void Merge_Into_Object_Test()
        {
            var source = JsonDocument.Parse(JSON2_INDENT).RootElement;
            var element = JsonDocument.Parse(JSON1_INDENT).RootElement;
            var merged= source.MergeIntoProp(element, "B.B2");

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
        public void Merge_Array_Test()
        {
            var source = JsonDocument.Parse(JSON_ARR1_INDENT).RootElement;
            var element = JsonDocument.Parse(JSON_ARR2_INDENT).RootElement;
            var merged= source.Merge(element);

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