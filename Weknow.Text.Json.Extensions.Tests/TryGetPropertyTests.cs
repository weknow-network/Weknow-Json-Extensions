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
    public class TryGetPropertyTests
    {
        private readonly ITestOutputHelper _outputHelper;

        #region Ctor

        public TryGetPropertyTests(ITestOutputHelper outputHelper)
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

        [Fact]
        public void TryGetProperty_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            Assert.True(source.TryGetProperty(out var property, "B", "B2", "B22"));
            Assert.Equal(22, property.GetInt32());
        }

        [Fact]
        public void TryGetProperty_Missing_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            Assert.False(source.TryGetProperty(out var property, "B", "X", "B22"));
        }

        [Fact]
        public void TryGetProperty_Empty_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            Assert.False(source.TryGetProperty(out var property));
        }
    }
}
