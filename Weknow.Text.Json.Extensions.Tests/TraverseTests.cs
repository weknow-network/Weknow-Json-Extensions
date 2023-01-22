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
    public class TraverseTests
    {
        private static readonly JsonWriterOptions OPT_INDENT =
                        new JsonWriterOptions { Indented = true };

        private readonly ITestOutputHelper _outputHelper;

        #region Ctor

        public TraverseTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        #endregion Ctor


        private const string JSON_INDENT =
                """
                {
                  "A": 12,
                  "B": {
                    "B1": "Cool",
                    "B2": {
                        "B21": {
                          "B211": 211
                        },
                        "B22": 22    
                    }
                  },
                  "C": ["C1", "C2"],
                  "D": [{ "D1": 1}, "D2", 3]
                }
                """;

        private void Write(JsonDocument source, JsonElement positive)
        {
            _outputHelper.WriteLine("Source:-----------------");
            _outputHelper.WriteLine(source.RootElement.AsString());
            _outputHelper.WriteLine("Result:-----------------");
            _outputHelper.WriteLine(positive.AsString());
        }


        [Fact]
        public void Traverse_Remove_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var result = source.RootElement.TraverseProps((writer, prop, path) => { /* remove */}, "C");

            Write(source, result);

            Assert.True(result.TryGetProperty("A", out _));
            Assert.True(result.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.True(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
            Assert.False(result.TryGetProperty("C", out _));
            Assert.True(result.TryGetProperty("D", out var d));
            Assert.True(d[0].TryGetProperty("D1", out var d1));
            Assert.Equal(1, d1.GetInt32());
        }

        [Fact]
        public void Traverse_Increment_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var result = source.RootElement.TraverseProps((writer, prop, path) =>
            {
                JsonElement pval = prop.Value;
                if (pval.ValueKind == JsonValueKind.Number)
                {
                    var value = pval.GetDouble();;
                    writer.WriteNumber(prop.Name, value + 1);
                }
            }, "A", "B22");

            Write(source, result);

            Assert.True(result.TryGetProperty("A", out var a));
            Assert.Equal(13, a.GetInt32());
            Assert.True(result.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.True(b2.TryGetProperty("B21", out var b21));
            Assert.True(b21.TryGetProperty("B211", out var b211));
            Assert.Equal(211, b211.GetInt32());
            Assert.True(b2.TryGetProperty("B22", out var b22));
            Assert.Equal(23, b22.GetInt32());
            Assert.True(result.TryGetProperty("C", out _));
            Assert.True(result.TryGetProperty("D", out var d));
            Assert.True(d[0].TryGetProperty("D1", out var d1));
            Assert.Equal(1, d1.GetInt32());
        }

        
    }
}
