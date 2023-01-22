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

        private void Write(JsonDocument source, JsonElement positive)
        {
            _outputHelper.WriteLine("Source:-----------------");
            _outputHelper.WriteLine(source.RootElement.AsString());
            _outputHelper.WriteLine("Result:-----------------");
            _outputHelper.WriteLine(positive.AsString());
        }


        [Theory]
        [InlineData(""" { "A": 0, "B": 0 } """,
            """ { "A": 0, "B": 0, "C": 1 } """,
            "",
            "C", 1,
            true, true)]
        [InlineData(""" { "Start": { "A": 0, "B": 0 } } """,
            """ { "Start": { "A": 0, "B": 0, "C": 1 } } """,
            "Start",
            "C", 1,
            true, true)]
        [InlineData(""" { "Start": { "A": 0, "B": 0 } } """,
            """ { "Start": { "A": 0, "B": 0} } """,
            "start",
            "C", 1, // todo throw?
            true, true)]
        [InlineData(""" { "Start": { "A": 0, "B": 0 } } """,
            """ { "Start": { "A": 0, "B": 0 } } """,
            "start",
            "C", 1,
            false, true)]
        [InlineData(""" { "Start": { "A": 0, "B": 0 } } """,
            """ { "Start": { "A": 0, "B": 0, "b": 1 } } """,
            "Start",
            "b", 1,
            false, true)]
        public void TryAdd_WithPath_Test(string origin,
                                         string expected,
                                         string path,
                                         string name,
                                         object value,
                                         bool addIfEmpty,
                                         bool caseInsensitive)
        {
            var option = new JsonSerializerOptions { PropertyNameCaseInsensitive = !caseInsensitive };
            var source = JsonDocument.Parse(origin);
            var result = source.RootElement.TryAddProperty(path, name, value, option,  addIfEmpty);

            Write(source, result);

            var expectedResult = JsonDocument.Parse(expected.Replace('\'', '"')).RootElement;
            Assert.Equal(expectedResult.AsString(), result.AsString());
        }

        [Theory]
        [InlineData( """ { "A": 0, "B": 0 } """,
            """ { "A": 0, "B": 0, "C": 1 } """,
            "C", 1,
            true, false)]
        [InlineData( """ { "A": 0, "B": 0 } """,
            """ { "A": 0, "B": 0, "C": 1 } """,
            "C", 1, true, null)]
        [InlineData( """ { "A": 0, "B": 0, "C": null } """,
            """ { "A": 0, "B": 0, "C": 1 } """,
            "C", 1,
            true, false)]
        [InlineData( """ { "A": 0, "B": 0, "C": null } """,
            """ { "A": 0, "B": 0, "C": null, "c": 1 } """,
            "c", 1,
            true, false)]
        [InlineData( """ { "A": 0, "B": 0, "C": null } """,
            """ { "A": 0, "B": 0, "C": null } """,
            "c", 1,
            false, true)]
        public void TryAdd_Test(string origin,
                                string expected,
                                string name,
                                object value,
                                bool addIfEmpty,
                                bool? caseInsensitive)
        {
            var option = caseInsensitive != null 
                            ? new JsonSerializerOptions { PropertyNameCaseInsensitive = caseInsensitive ?? true } 
                            : null as JsonSerializerOptions;
            var source = JsonDocument.Parse(origin);
            var result = source.RootElement.TryAddProperty(name, value, option, addIfEmpty);

            Write(source, result);

            var expectedResult = JsonDocument.Parse(expected.Replace('\'', '"')).RootElement;
            Assert.Equal(expectedResult.AsString(), result.AsString());
        }
    }
}
 