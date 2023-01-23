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
using static Weknow.Text.Json.Extensions.Tests.TryAddPropertyTests;

namespace Weknow.Text.Json.Extensions.Tests
{
    public class TryAddPropertyTests
    {
        private static readonly JsonWriterOptions OPT_INDENT =
                        new JsonWriterOptions { Indented = true };

        private readonly ITestOutputHelper _outputHelper;

        #region Ctor

        public TryAddPropertyTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        #endregion Ctor

        private void Write(JsonDocument source, JsonElement positive, string? desc = null)
        {
            if(desc != null) 
            {
                _outputHelper.WriteLine($"======= {desc} =========");
            }
            _outputHelper.WriteLine("Source:-----------------");
            _outputHelper.WriteLine(source.RootElement.AsString());
            _outputHelper.WriteLine("Result:-----------------");
            _outputHelper.WriteLine(positive.AsString());
        }

        public enum IgnoreWhenNull
        {
            True,
            False
        }
        public enum CaseSensitive
        {
            True,
            False
        }

        [Theory]
        [InlineData(""" { "A1": 0, "B": 0 } """,
            """ { "A1": 0, "B": 0, "C": 1 } """,
            "",
            "C", 1,
            IgnoreWhenNull.True, CaseSensitive.True)]
        [InlineData(""" { "Start": { "A2": 0, "B": 0 } } """,
            """ { "Start": { "A2": 0, "B": 0, "C": 1 } } """,
            "Start",
            "C", 1,
            IgnoreWhenNull.True, CaseSensitive.True)]
        [InlineData(""" { "Start": { "A2": 0, "B": 0, "C": null  } } """,
            """ { "Start": { "A2": 0, "B": 0, "C": 1 } } """,
            "Start",
            "c", 1,
            IgnoreWhenNull.True, CaseSensitive.False,
            "Ignore case: 'c' will modify property 'C'")]
        [InlineData(""" { "Start": { "A3": 0, "B": 0 } } """,
            """ { "Start": { "A3": 0, "B": 0} } """,
            "start",
            "C", 1,
            IgnoreWhenNull.True, CaseSensitive.True,
            "Not found: case sensitive filter")]
        [InlineData(""" { "Start": { "A4": 0, "B": 0 } } """,
            """ { "Start": { "A4": 0, "B": 0 } } """,
            "start",
            "C", 1,
            IgnoreWhenNull.False, CaseSensitive.True)]
        [InlineData(""" { "Start": { "A5": 0, "B": 0 } } """,
            """ { "Start": { "A5": 0, "B": 0, "b": 1 } } """,
            "Start",
            "b", 1,
            IgnoreWhenNull.False, CaseSensitive.True,
            "Case sensitive: create new property of lower case 'b'")]
        public void TryAdd_WithPath_Test(string origin,
                                         string expected,
                                         string path,
                                         string name,
                                         object value,
                                         IgnoreWhenNull ignoreNull,
                                         CaseSensitive caseSensitive,
                                         string? desc = null)
        {
            var serializationOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = caseSensitive == CaseSensitive.False };
            var options = new JsonPropertyModificatonOpions
            {
                Options = serializationOptions,
                IgnoreNull = ignoreNull == IgnoreWhenNull.True
            };
            var source = JsonDocument.Parse(origin);
            var result = source.RootElement.TryAddProperty(path, name, value, options);

            Write(source, result, desc);

            var expectedResult = JsonDocument.Parse(expected.Replace('\'', '"')).RootElement;
            Assert.Equal(expectedResult.AsString(), result.AsString());
        }

        [Theory]
        [InlineData( """ { "A": 0, "B": 0 } """,
            """ { "A": 0, "B": 0, "C": 1 } """,
            "C", 1,
            IgnoreWhenNull.True, CaseSensitive.True)]
        [InlineData( """ { "A": 0, "B": 0 } """,
            """ { "A": 0, "B": 0, "C": 1 } """,
            "C", 1,
            IgnoreWhenNull.True, CaseSensitive.False)]
        [InlineData( """ { "A": 0, "B": 0, "C": null } """,
            """ { "A": 0, "B": 0, "C": 1 } """,
            "C", 1,
            IgnoreWhenNull.True, CaseSensitive.True)]
        [InlineData( """ { "A": 0, "B": 0, "C": null } """,
            """ { "A": 0, "B": 0, "C": null, "c": 1 } """,
            "c", 1,
            IgnoreWhenNull.True, CaseSensitive.True)]
        [InlineData( """ { "A": 0, "B": 0, "C": null } """,
            """ { "A": 0, "B": 0, "C": null } """,
            "c", 1,
            IgnoreWhenNull.False, CaseSensitive.False)]
        public void TryAdd_Test(string origin,
                                string expected,
                                string name,
                                object value,
                                IgnoreWhenNull ignoreNull,
                                CaseSensitive caseSensitive,
                                string? desc = null)
        {
            var serializationOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = caseSensitive == CaseSensitive.False };
            var options = new JsonPropertyModificatonOpions
            {
                Options = serializationOptions,
                IgnoreNull = ignoreNull == IgnoreWhenNull.True
            };
            var source = JsonDocument.Parse(origin);
            var result = source.RootElement.TryAddProperty(name, value, options);

            Write(source, result);

            var expectedResult = JsonDocument.Parse(expected.Replace('\'', '"')).RootElement;
            Assert.Equal(expectedResult.AsString(), result.AsString());
        }
    }
}
 