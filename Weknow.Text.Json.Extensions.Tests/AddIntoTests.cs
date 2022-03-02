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
    public class AddIntoTests
    {
        private static readonly JsonWriterOptions OPT_INDENT =
                        new JsonWriterOptions { Indented = true };

        private readonly ITestOutputHelper _outputHelper;

        #region Ctor

        public AddIntoTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        #endregion Ctor


        private const string JSON_OBJ =
@"{
  ""A"": [1, 2, 3],
  ""B"": 10
}
";
        private const string JSON_ARR =
@"[{""A"": 1}, 2]";

        private const string JSON_ARR_INDENT = @"[1, 2, 3]";

        private void Write(JsonElement source, JsonElement addition, JsonElement result)
        {
            _outputHelper.WriteLine("Source:-----------------");
            _outputHelper.WriteLine(source.AsString());
            _outputHelper.WriteLine("Addition:-----------------");
            _outputHelper.WriteLine(addition.AsString());
            _outputHelper.WriteLine("Result:-----------------");
            _outputHelper.WriteLine(result.AsString());
        }


        [Fact]
        public void AddInto_Arr_Arr_Test()
        {
            var source = JsonDocument.Parse(JSON_ARR).RootElement;
            var addition = JsonDocument.Parse("[4, 5, 6]").RootElement;
            var result = source.AddIntoArray(addition);

            Write(source, addition, result);

            Assert.Equal(5, result.EnumerateArray().Count());
        }

        [Fact]
        public void AddInto_Arr_Obj_Test()
        {
            var source = JsonDocument.Parse(JSON_ARR).RootElement;
            var addition = JsonDocument.Parse(@"{ ""C"":4 }").RootElement;
            var result = source.AddIntoArray(addition);

            Write(source, addition, result);

            Assert.Equal(3, result.EnumerateArray().Count());
        }

        [Fact]
        public void AddInto_Arr_Double_Test()
        {
            var source = JsonDocument.Parse(JSON_ARR).RootElement;
            var result = source.AddIntoArray(4.2);

            Write(source, JsonExtensions.Empty, result);

            Assert.Equal(3, result.EnumerateArray().Count());
        }

        [Fact]
        public void AddInto_Arr_DoubleInt_Test()
        {
            var source = JsonDocument.Parse(JSON_ARR).RootElement;
            var result = source.AddIntoArray(4.2, 5);

            Write(source, JsonExtensions.Empty, result);

            Assert.Equal(4, result.EnumerateArray().Count());
        }

        [Fact]
        public void AddInto_Arr_Error_Test()
        {
            var source = JsonDocument.Parse(@"{ ""C"":4 }").RootElement;
            var addition = JsonDocument.Parse(JSON_ARR).RootElement;
            Assert.Throws<NotSupportedException>(() => source.AddIntoArray(addition));
        }

        [Fact]
        public void AddInto_Obj_Error_Test()
        {
            var source = JsonDocument.Parse(JSON_OBJ).RootElement;
            var addition = JsonDocument.Parse(JSON_ARR).RootElement;
            Assert.Throws<NotSupportedException>(() => source.AddIntoObject(addition));
        }

        [Fact]
        public void AddInto_Obj_OfT_Error_Test()
        {
            var source = JsonDocument.Parse(JSON_OBJ).RootElement;
            var addition = new[] { 1, 2 };
            Assert.Throws<NotSupportedException>(() => source.AddIntoObject(addition));
        }

        [Fact]
        public void AddInto_Obj_Test()
        {
            var source = JsonDocument.Parse(JSON_OBJ).RootElement;
            var addition = JsonDocument.Parse(@"{ ""C"": 3 }").RootElement;
            var result = source.AddIntoObject(addition);

            Write(source, addition, result);

            Assert.True(result.TryGetProperty("C", out var c));
            Assert.Equal(3, c.GetInt32());
        }

        [Fact]
        public void AddInto_Obj_OfT_Test()
        {
            var source = JsonDocument.Parse(JSON_OBJ).RootElement;
            var addition = new { C = 3 };
            var result = source.AddIntoObject(addition);

            Write(source, addition.ToJson(), result);

            Assert.True(result.TryGetProperty("c", out var c));
            Assert.Equal(3, c.GetInt32());
        }

        [Fact]
        public void AddInto_Obj_OfT_Options_Test()
        {
            var source = JsonDocument.Parse(JSON_OBJ).RootElement;
            var addition = new { C = 3 };
            var result = source.AddIntoObject(addition, new JsonSerializerOptions ());

            Write(source, addition.ToJson(), result);

            Assert.True(result.TryGetProperty("C", out var c));
            Assert.Equal(3, c.GetInt32());
        }
    }
}
