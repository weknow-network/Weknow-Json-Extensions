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
    public class SplitParentTests
    {
        private static readonly JsonWriterOptions OPT_INDENT =
                        new JsonWriterOptions { Indented = true };

        private readonly ITestOutputHelper _outputHelper;

        #region Ctor

        public SplitParentTests(ITestOutputHelper outputHelper)
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
        ""B22"": 22   , 
        ""B23"": 23    
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
        public void SplitChidProp_Root_Branch_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var (positive, negative) = source.RootElement.SplitChidProp("B2", "B21");

            Write(source, positive, negative);

            Assert.True(positive.TryGetProperty("B21", out var pb21));
            Assert.True(pb21.TryGetProperty("B211", out var pb211));
            Assert.Equal(211, pb211.GetInt32());
            Assert.False(positive.TryGetProperty("B22", out _));
            Assert.False(positive.TryGetProperty("B23", out _));

            Assert.True(negative.TryGetProperty("B22", out var nb22));
            Assert.Equal(22, nb22.GetInt32());
            Assert.True(negative.TryGetProperty("B23", out var nb23));
            Assert.Equal(23, nb23.GetInt32());
        }

        [Fact]
        public void SplitChidProp_Root_Branch_Multi_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var (positive, negative) = source.RootElement.SplitChidProp("B2","B21", "B22");

            Write(source, positive, negative);

            Assert.True(positive.TryGetProperty("B21", out var pb21));
            Assert.True(pb21.TryGetProperty("B211", out var pb211));
            Assert.Equal(211, pb211.GetInt32());
            Assert.True(positive.TryGetProperty("B22", out var pb22));
            Assert.Equal(22, pb22.GetInt32());
            Assert.False(positive.TryGetProperty("B23", out _));

            Assert.False(negative.TryGetProperty("B21", out _));
            Assert.False(negative.TryGetProperty("B22", out _));
            Assert.True(negative.TryGetProperty("B23", out var Nb23));
            Assert.Equal(23, Nb23.GetInt32());
        }

        [Fact]
        public void SplitChidProp_Root_Branch_MultiSet_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var set = ImmutableHashSet.CreateRange(new[] { "B21", "B22" });
            var (positive, negative) = source.RootElement.SplitChidProp("B2", set);

            Write(source, positive, negative);

            Assert.True(positive.TryGetProperty("B21", out var pb21));
            Assert.True(pb21.TryGetProperty("B211", out var pb211));
            Assert.Equal(211, pb211.GetInt32());
            Assert.True(positive.TryGetProperty("B22", out var pb22));
            Assert.Equal(22, pb22.GetInt32());
            Assert.False(positive.TryGetProperty("B23", out _));

            Assert.False(negative.TryGetProperty("B21", out _));
            Assert.False(negative.TryGetProperty("B22", out _));
            Assert.True(negative.TryGetProperty("B23", out var Nb23));
            Assert.Equal(23, Nb23.GetInt32());
        }

        [Fact]
        public void SplitChidProp_Root_Branch_MultiPathSet_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var set = ImmutableHashSet.CreateRange(new[] { "B.B2.B21", "B.B2.B22" });
            var (positive, negative) = source.RootElement.SplitChidProp("B2", set);

            Write(source, positive, negative);

            Assert.True(positive.TryGetProperty("B21", out var pb21));
            Assert.True(pb21.TryGetProperty("B211", out var pb211));
            Assert.Equal(211, pb211.GetInt32());
            Assert.True(positive.TryGetProperty("B22", out var pb22));
            Assert.Equal(22, pb22.GetInt32());
            Assert.False(positive.TryGetProperty("B23", out _));

            Assert.False(negative.TryGetProperty("B21", out _));
            Assert.False(negative.TryGetProperty("B22", out _));
            Assert.True(negative.TryGetProperty("B23", out var Nb23));
            Assert.Equal(23, Nb23.GetInt32());
        }

        [Fact]
        public void SplitChidProp_Root_Branch_WrongParent_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var (positive, negative) = source.RootElement.SplitChidProp("B","B21");

            Write(source, positive, negative);

            Assert.Equal(JsonValueKind.Undefined, positive.ValueKind);

            Assert.True(negative.TryGetProperty("B1", out _));
            Assert.True(negative.TryGetProperty("B2", out _));
        }
    }
}
