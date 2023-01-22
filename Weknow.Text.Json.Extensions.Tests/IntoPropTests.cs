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
            """
            {
              "A": [1, 2, 3]
            }
            """;

        private const string JSON_ARR_INDENT = @"[1, 2, 3]";

        private void Write(JsonElement source, JsonElement result)
        {
            _outputHelper.WriteLine("Source:-----------------");
            _outputHelper.WriteLine(source.AsString());
            _outputHelper.WriteLine("Result:-----------------");
            _outputHelper.WriteLine(result.AsString());
        }


        [Theory]
        [InlineData(
            JSON_INDENT,
            """
            {
                "root": {
                    "A": [1, 2, 3]
                }
            }
            """, 
            "root")]
        [InlineData(
            JSON_ARR_INDENT,
            """
            {
                "root": [1, 2, 3]
            }
            """, 
            "root")]
        public void IntoProp_Object_Test(string origin, string expected, string prop)
        {
            var source = JsonDocument.Parse(origin).RootElement;
            var result = source.IntoProp(prop);

            Write(source, result);

            var expectedResult = JsonDocument.Parse(expected.Replace('\'', '"')).RootElement;
            Assert.Equal(expectedResult.AsString(), result.AsString());
        }
    }
}
