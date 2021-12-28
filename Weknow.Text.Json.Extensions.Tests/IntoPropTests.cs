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
    public class IntoPropTests
    {
        private static readonly JsonWriterOptions OPT_INDENT =
                        new JsonWriterOptions { Indented = true };

        private readonly ITestOutputHelper _outputHelper;

        #region Ctor

        public IntoPropTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        #endregion Ctor


        private const string JSON_INDENT =
@"{
  ""A"": [1, 2, 3]
}
";

        private const string JSON_ARR_INDENT = @"[1, 2, 3]";

        private void Write(JsonElement source, JsonElement result)
        {
            _outputHelper.WriteLine("Source:-----------------");
            _outputHelper.WriteLine(source.AsString());
            _outputHelper.WriteLine("Result:-----------------");
            _outputHelper.WriteLine(result.AsString());
        }


        [Fact]
        public void IntoProp_Object_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT).RootElement;
            var result = source.IntoProp("root");

            Write(source, result);

            Assert.Equal(JsonValueKind.Object, result.ValueKind);
            Assert.False(result.TryGetProperty("A", out _));
            Assert.True(result.TryGetProperty("root", out var root));
            Assert.True(root.TryGetProperty("A", out var a));
            Assert.Equal(1, a[0].GetInt16());
            Assert.Equal(2, a[1].GetInt16());
            Assert.Equal(3, a[2].GetInt16());
        }

        [Fact]
        public void IntoProp_array_Test()
        {
            var source = JsonDocument.Parse(JSON_ARR_INDENT).RootElement;
            var result = source.IntoProp("root");

            Write(source, result);

            Assert.Equal(JsonValueKind.Object, result.ValueKind);
            Assert.True(result.TryGetProperty("root", out var root));
            Assert.Equal(1, root[0].GetInt16());
            Assert.Equal(2, root[1].GetInt16());
            Assert.Equal(3, root[2].GetInt16());
        }
    }
}
