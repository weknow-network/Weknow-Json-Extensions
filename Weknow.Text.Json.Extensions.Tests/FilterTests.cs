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
	public class FilterTests
	{
		private static readonly JsonWriterOptions OPT_INDENT =
						new JsonWriterOptions { Indented = true };

		private readonly ITestOutputHelper _outputHelper;
		private Action<JsonProperty> _fakeOnRemove = A.Fake<Action<JsonProperty>>();

		#region Ctor

		public FilterTests(ITestOutputHelper outputHelper)
		{
			_outputHelper = outputHelper;
		}

		#endregion Ctor


		private const string JSON_INDENT =
@"{
  ""A"": 10,
  ""B"": [
	{ ""Val"": 40 },
	{ ""Val"": 20 },
	{ ""Factor"": 20 } 
  ],
  ""C"": [0, 25, 50, 100],
  ""Note"": ""Re-shape json""
}
";

		#region Write

		private void Write(JsonDocument source, JsonElement target)
		{
			_outputHelper.WriteLine("Source:-----------------");
			_outputHelper.WriteLine(source.RootElement.AsString());
			_outputHelper.WriteLine("Target:-----------------");
			_outputHelper.WriteLine(target.AsString());
		}

		#endregion // Write

		#region Filter_Gt30_Test

		[Fact]
		public void Filter_Gt30_Test()
		{
			var source = JsonDocument.Parse(JSON_INDENT);
			var target = source.RootElement.Filter((e, _, _) =>
									e.ValueKind != JsonValueKind.Number || e.GetInt32() > 30 ? TraverseFlowWrite.Drill : TraverseFlowWrite.Skip);

			Write(source, target);
			Assert.Equal(
				@"{""B"":[{""Val"":40}],""C"":[50,100],""Note"":""Re-shape json""}",
				target.AsString());
		}

		#endregion // Filter_Gt30_Test

		#region FilterPath_Test

		[Theory]
		[InlineData("B.[1]", @"{""B"":[{""Val"":20}]}")]
		[InlineData("B.[1].val", @"{""B"":[{""Val"":20}]}")]
		[InlineData("B.*.val", @"{""B"":[{""Val"":40},{""Val"":20}]}")]
		[InlineData("B.[]", @"{""B"":[{""Val"":40},{""Val"":20},{""Factor"":20}]}")]
		[InlineData("B.[].val", @"{""B"":[{""Val"":40},{""Val"":20}]}")]
		[InlineData("B.[].val", @"{""B"":[]}", true)]
		[InlineData("B.[].Factor", @"{""B"":[{""Factor"":20}]}", true)]
		public void FilterPath_Test(string path, string expected, bool caseSensitive = false)
		{
			_outputHelper.WriteLine(path);
			var source = JsonDocument.Parse(JSON_INDENT);
			var target = caseSensitive 
				? source.RootElement.FilterSensitive(path)
				: source.RootElement.Filter(path);

			Write(source, target);
			Assert.Equal(
				expected,
				target.AsString());
		}

		#endregion // FilterPath_Test

		#region ExcludePath_Test

		[Theory]
        [InlineData("B.[]", @"{""A"":10,""B"":[],""C"":[],""Note"":""Re-shape json""}")]
        [InlineData("B.*.val", @"{""A"":10,""B"":[{""Factor"":20}],""C"":[0,25,50,100],""Note"":""Re-shape json""}")]
        [InlineData("B.[1]", @"{""A"":10,""B"":[{""Val"":40},{""Factor"":20}],""C"":[0,50,100],""Note"":""Re-shape json""}")]
        [InlineData("B", @"{""A"":10,""C"":[0,25,50,100],""Note"":""Re-shape json""}")]
        [InlineData("B.[1].val", @"{""A"":10,""B"":[{""Val"":40},{""Factor"":20}],""C"":[0,25,50,100],""Note"":""Re-shape json""}")]	   

		public void ExcludePath_Test(string path, string expected, bool caseSensitive = false)
		{
			_outputHelper.WriteLine(path);
			var source = JsonDocument.Parse(JSON_INDENT);
			var target = caseSensitive 
				? source.RootElement.ExcludeSensitive(path)
				: source.RootElement.Exclude(path);

			Write(source, target);
			Assert.Equal(
				expected,
				target.AsString());
		}

		#endregion // ExcludePath_Test
	}
}
