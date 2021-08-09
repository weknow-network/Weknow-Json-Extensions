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
    public class WhereTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private Action<JsonProperty> _fakeOnRemove = A.Fake<Action<JsonProperty>>();

        #region Ctor

        public WhereTests(ITestOutputHelper outputHelper)
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
        public void Where_Root_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var target = source.RootElement.Where(m =>
                                    // remove property with value or raw value elements if > 12
                                    m.ValueKind != JsonValueKind.Number || m.GetInt32() > 12);

            _outputHelper.WriteLine(target.GetRawText());

            Assert.True(target.TryGetProperty("A", out var a));
            Assert.True(a.ValueKind == JsonValueKind.Null);
            Assert.True(target.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.True(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
            Assert.True(target.TryGetProperty("C", out _));
            Assert.True(target.TryGetProperty("D", out var d));
            Assert.True(d[0].TryGetProperty("D1", out var d1));
            Assert.Equal(1, d1.GetInt32());
            Assert.Equal("D2", d[1].GetString());
            Assert.Equal(3, d[2].GetInt32());
        }

        [Fact]
        public void Where_Deep_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var target = source.RootElement.Where(m =>
                                    // remove property with value or raw value elements if > 12
                                    m.ValueKind != JsonValueKind.Number || m.GetInt32() > 12, 5);

            _outputHelper.WriteLine(target.GetRawText());

            Assert.True(target.TryGetProperty("A", out var a));
            Assert.True(a.ValueKind == JsonValueKind.Null);
            Assert.True(target.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.True(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
            Assert.True(target.TryGetProperty("C", out _));
            Assert.True(target.TryGetProperty("D", out var d));
            Assert.True(d[0].TryGetProperty("D1", out var d1));
            Assert.True(d1.ValueKind == JsonValueKind.Null);
            Assert.Equal("D2", d[1].GetString());
            Assert.Equal(2, d.EnumerateArray().Count());
        }

        [Fact]
        public void WhereProp_Root_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var target = source.RootElement.WhereProp(m => m.Name != "C", onRemove: _fakeOnRemove);

            _outputHelper.WriteLine(target.GetRawText());

            Assert.True(target.TryGetProperty("A", out _));
            Assert.True(target.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.True(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
            Assert.False(target.TryGetProperty("C", out _));
            Assert.True(target.TryGetProperty("D", out var d));
            Assert.True(d[0].TryGetProperty("D1", out var d1));
            Assert.Equal(1, d1.GetInt32());
            A.CallTo(() => _fakeOnRemove.Invoke(A<JsonProperty>.That.Matches(p => p.Name == "C")))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeOnRemove.Invoke(A<JsonProperty>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void WhereProp_Root_Branch_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var target = source.RootElement.WhereProp(m => m.Name != "B", onRemove: _fakeOnRemove);

            _outputHelper.WriteLine(target.GetRawText());

            Assert.True(target.TryGetProperty("A", out _));
            Assert.False(target.TryGetProperty("B", out _));
            Assert.True(target.TryGetProperty("C", out var c));
            Assert.Equal("C1", c[0].GetString());
            Assert.Equal("C2", c[1].GetString());
            A.CallTo(() => _fakeOnRemove.Invoke(A<JsonProperty>.That.Matches(p => p.Name == "B")))
                                                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeOnRemove.Invoke(A<JsonProperty>.Ignored))
                                                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void WhereProp_Recurcive_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var target = source.RootElement.WhereProp(m => m.Name != "B21", 35, onRemove: _fakeOnRemove);

            _outputHelper.WriteLine(target.GetRawText());

            Assert.True(target.TryGetProperty("A", out _));
            Assert.True(target.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.False(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
            Assert.True(target.TryGetProperty("C", out _));
            A.CallTo(() => _fakeOnRemove.Invoke(A<JsonProperty>.That.Matches(p => p.Name == "B21")))
                                                .MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeOnRemove.Invoke(A<JsonProperty>.Ignored))
                                                .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public void WhereProp_Recurcive_Shallow_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var target = source.RootElement.WhereProp(m => m.Name != "B21", 1, onRemove: _fakeOnRemove);

            _outputHelper.WriteLine(target.GetRawText());

            Assert.True(target.TryGetProperty("A", out _));
            Assert.True(target.TryGetProperty("B", out var b));
            Assert.True(b.TryGetProperty("B1", out _));
            Assert.True(b.TryGetProperty("B2", out var b2));
            Assert.True(b2.TryGetProperty("B21", out _));
            Assert.True(b2.TryGetProperty("B22", out _));
            Assert.True(target.TryGetProperty("C", out _));
            A.CallTo(() => _fakeOnRemove.Invoke(A<JsonProperty>.Ignored))
                                                .MustNotHaveHappened();
        }
    }
}
