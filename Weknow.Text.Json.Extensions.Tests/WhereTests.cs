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
        private static readonly JsonWriterOptions OPT_INDENT =
                        new JsonWriterOptions { Indented = true };

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

        private void Write(JsonDocument source, JsonElement target)
        {
            _outputHelper.WriteLine("Source:-----------------");
            _outputHelper.WriteLine(source.RootElement.AsString());
            _outputHelper.WriteLine("Target:-----------------");
            _outputHelper.WriteLine(target.AsString());
        }

        [Fact]
        public void Exclude_Root_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var target = source.RootElement.Where((e, _, _) =>
                                    // remove property with value or raw value elements if > 12
                                    e.ValueKind != JsonValueKind.Number || e.GetInt32() > 12);

            Write(source, target);

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
            Assert.Equal(2, d.EnumerateArray().Count());
        }

        [Fact]
        public void Exclude_Deep_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var target = source.RootElement.Where((e, _, _) =>
                                    // remove property with value or raw value elements if > 12
                                    e.ValueKind != JsonValueKind.Number || e.GetInt32() > 12, 1);

            Write(source, target);

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
            Assert.Equal("D2", d[1].GetString());
            Assert.Equal(3, d[2].GetInt32());
        }

        [Fact]
        public void WhereProp_Root_Simple_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var target = source.RootElement.WhereProp("A", "B", "D");

            Write(source, target);

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
        }

        [Fact]
        public void WhereProp_Root_Branch_Simple_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var target = source.RootElement.WhereProp("A", "C", "D");

            Write(source, target);

            Assert.True(target.TryGetProperty("A", out _));
            Assert.False(target.TryGetProperty("B", out _));
            Assert.True(target.TryGetProperty("C", out var c));
            Assert.Equal("C1", c[0].GetString());
            Assert.Equal("C2", c[1].GetString());
        }

        [Fact]
        public void WhereProp_Root_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var target = source.RootElement.WhereProp((e, _, _) => e.Name != "C", onRemove: _fakeOnRemove);

            Write(source, target);

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
            var target = source.RootElement.WhereProp((e,_, _) => e.Name != "B", onRemove: _fakeOnRemove);

            Write(source, target);

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
            var target = source.RootElement.WhereProp((e, _, _) => e.Name != "B21", 35, onRemove: _fakeOnRemove);

            Write(source, target);

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
        public void WhereProp_Path_Test()
        {
            var source = JsonDocument.Parse(JSON_INDENT);
            var target = source.RootElement.WhereProp((_, path, _) => path != "B.B2.B21", 35, onRemove: _fakeOnRemove);

            Write(source, target);

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
            var target = source.RootElement.WhereProp((e, _, _) => e.Name != "B21", 1, onRemove: _fakeOnRemove);

            Write(source, target);

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

        [Fact]
        public void WhereProp_Path_Array_Test()
        {
            string JSON =
@"{
	""personalizations"": [
        {
            ""to"": [
    
                {
                    ""name"": ""Bnaya"",
					""email"": ""bnaya@somewhere.com""
    
                }
			],
			""bcc"": [

                {
                    ""name"": ""Ruth"",
					""email"": ""Ruth@somewhere.com""

                },
				{
                    ""name"": ""Bnaya Eshet"",
					""email"": ""bnaya@gmail.com""

                }
			]
        },
        {
            ""to"": [
    
                {
                    ""name"": ""Mike"",
					""email"": ""mike@somewhere.com""
    
                }
			]
        }
	]
}
";
            var source = JsonDocument.Parse(JSON);
            var target = source.RootElement.WhereProp("personalizations.[].to.[0].name");
            //var target = source.RootElement.WhereProp((_, path, _) => path == "personalizations.[].to.[0].name");

            Write(source, target);

            //Assert.True(target.TryGetProperty("", out _));
            //Assert.True(target.TryGetProperty("B", out var b));
            //Assert.True(b.TryGetProperty("B1", out _));
            //Assert.True(b.TryGetProperty("B2", out var b2));
            //Assert.False(b2.TryGetProperty("B21", out _));
            //Assert.True(b2.TryGetProperty("B22", out _));
            //Assert.True(target.TryGetProperty("C", out _));
            //A.CallTo(() => _fakeOnRemove.Invoke(A<JsonProperty>.That.Matches(p => p.Name == "B21")))
            //                                    .MustHaveHappenedOnceExactly();
            //A.CallTo(() => _fakeOnRemove.Invoke(A<JsonProperty>.Ignored))
            //                                    .MustHaveHappenedOnceExactly();
        }
    }
}
