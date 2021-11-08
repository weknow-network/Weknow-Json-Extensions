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
    public class SplitTests
    {
        private static readonly JsonWriterOptions OPT_INDENT =
                        new JsonWriterOptions { Indented = true };

        private readonly ITestOutputHelper _outputHelper;

        #region Ctor

        public SplitTests(ITestOutputHelper outputHelper)
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

        private void Write(JsonDocument source, JsonElement positive, JsonElement negative)
        {
            _outputHelper.WriteLine("Source:-----------------");
            _outputHelper.WriteLine(source.RootElement.AsString());
            _outputHelper.WriteLine("Positive:-----------------");
            _outputHelper.WriteLine(positive.AsString());
            _outputHelper.WriteLine("Negative:-----------------");
            _outputHelper.WriteLine(negative.AsString());
        }


        [Fact]
        public void SplitProp_Root_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var (positive, negative) = source.RootElement.SplitProp("C");

            Write(source, positive, negative);

            Assert.True(negative.TryGetProperty("A", out _));
            Assert.True(negative.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.True(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
            Assert.False(negative.TryGetProperty("C", out _));
            Assert.True(negative.TryGetProperty("D", out var d));
            Assert.True(d[0].TryGetProperty("D1", out var d1));
            Assert.Equal(1, d1.GetInt32());

            Assert.False(positive.TryGetProperty("A", out _));
            Assert.False(positive.TryGetProperty("B", out _));
            Assert.True(positive.TryGetProperty("C", out var c));
            Assert.Equal("C1", c[0].GetString());
            Assert.Equal("C2", c[1].GetString());
            Assert.False(positive.TryGetProperty("D", out _));
        }

        [Fact]
        public void SplitProp_Root_Multi_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var (positive, negative) = source.RootElement.SplitProp("C", "A");

            Write(source, positive, negative);

            Assert.False(negative.TryGetProperty("A", out _));
            Assert.True(negative.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.True(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
            Assert.False(negative.TryGetProperty("C", out _));
            Assert.True(negative.TryGetProperty("D", out var d));
            Assert.True(d[0].TryGetProperty("D1", out var d1));
            Assert.Equal(1, d1.GetInt32());

            Assert.True(positive.TryGetProperty("A", out var a));
            Assert.Equal(12, a.GetInt32());
            Assert.False(positive.TryGetProperty("B", out _));
            Assert.True(positive.TryGetProperty("C", out var c));
            Assert.Equal("C1", c[0].GetString());
            Assert.Equal("C2", c[1].GetString());
            Assert.False(positive.TryGetProperty("D", out _));
        }


        [Fact]
        public void SplitProp_Root_Branch_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var (positive, negative) = source.RootElement.SplitProp("B");

            Write(source, positive, negative);

            Assert.True(negative.TryGetProperty("A", out _));
            Assert.False(negative.TryGetProperty("B", out _));
            Assert.True(negative.TryGetProperty("C", out var c));
            Assert.Equal("C1", c[0].GetString());
            Assert.Equal("C2", c[1].GetString());

            Assert.False(positive.TryGetProperty("A", out _));
            Assert.True(positive.TryGetProperty("B", out _));
            Assert.False(positive.TryGetProperty("C", out _));
        }

        [Fact]
        public void SplitProp_Root_Branch_Multi_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var (positive, negative) = source.RootElement.SplitProp("B21", "B22");

            Write(source, positive, negative);

            Assert.True(negative.TryGetProperty("A", out _));
            Assert.True(negative.TryGetProperty("B", out var nb));
            Assert.True(nb.TryGetProperty("B1", out var nb1));
            Assert.True(nb.TryGetProperty("B2", out var nb2));
            Assert.True(nb2.ValueKind == JsonValueKind.Null);
            Assert.True(negative.TryGetProperty("C", out var c));

            Assert.False(positive.TryGetProperty("A", out _));
            Assert.False(positive.TryGetProperty("B", out _));
            Assert.True(positive.TryGetProperty("B21", out var pb21));
            Assert.True(pb21.TryGetProperty("B211", out var pb211));
            Assert.Equal(211, pb211.GetInt32());
            Assert.True(positive.TryGetProperty("B22", out var pb22));
            Assert.Equal(22, pb22.GetInt32());
            Assert.False(positive.TryGetProperty("C", out _));
        }

        [Fact]
        public void SplitProp_Recurcive_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            IImmutableSet<string> set = ImmutableHashSet.Create("B21");
            var (positive, negative) = source.SplitProp(set, deep: 35);

            Write(source, positive, negative);

            Assert.True(negative.TryGetProperty("A", out _));
            Assert.True(negative.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.False(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
            Assert.True(negative.TryGetProperty("C", out _));

            Assert.True(positive.TryGetProperty("B21", out var b21));
            Assert.True(b21.TryGetProperty("B211", out var b211));
            Assert.True(b211.TryGetInt32(out var v211));
            Assert.Equal(211, v211);

        }

        [Fact]
        public void SplitProp_Recurcive_Shallow_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var set = ImmutableHashSet.Create("B21");
            var (positive, negative) = source.SplitProp(set, deep: 1);

            Write(source, positive, negative);

            Assert.True(negative.TryGetProperty("A", out _));
            Assert.True(negative.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.True(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
            Assert.True(negative.TryGetProperty("C", out _));

            Assert.Equal(JsonValueKind.Object, positive.ValueKind);
            Assert.Empty( positive.EnumerateObject());
        }
    }
}
