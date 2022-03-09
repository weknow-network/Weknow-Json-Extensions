using FakeItEasy;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

using static Weknow.Text.Json.Constants;

using static System.Text.Json.TraverseFlowInstruction;

namespace Weknow.Text.Json.Extensions.Tests
{
    public class YieldWhenTests
    {
        private readonly ITestOutputHelper _outputHelper;

        #region Ctor

        public YieldWhenTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        #endregion Ctor


        [Fact]
        public async Task YieldWhen_Skill_Test()
        {
            using var srm = File.OpenRead("deep-filter-data.json");
            var source = await JsonDocument.ParseAsync(srm);

            var items = source.YieldWhen((json, deep, breadcrumbs) =>
            {
                string last = breadcrumbs[^1];
                if(deep == 0 && last == "skills")
                    return Drill;

                if (last[0] == '[' && last[^1] == ']')
                {
                    if (json.ValueKind == JsonValueKind.String)
                        return Yield;
                    return Skip;
                }

                return deep switch
                {
                    < 3 => Drill,
                    _ => Do(TraverseFlow.SkipToParent),
                };
            });

            var results = items.Select(m => m.GetString()).ToArray();
            Assert.Equal(4, results.Length);
            string[] expected = { "c#", "Typescript", "neo4J", "elasticsearch" };
            Assert.True(expected.SequenceEqual(results));
        }

        [Fact]
        public async Task YieldWhen_Friends_Test()
        {
            using var srm = File.OpenRead("deep-filter-data.json");
            var source = await JsonDocument.ParseAsync(srm);

            var items = source.YieldWhen((json, deep, breadcrumbs) =>
            {
                string last = breadcrumbs[^1];
                if(last == "role") throw new NotSupportedException("shouldn't get so deep");

                if (deep == 0)
                {
                    if (last == "friends")
                        return Drill;
                    return Skip;
                }

                if (last[0] == '[' && last[^1] == ']')
                {
                    if(json.TryGetProperty("IsSkipper", out var p) && p.GetBoolean())
                        return Yield;
                    return Skip;
                }

                return deep switch
                {
                    < 3 => Drill,
                    _ => SkipToParent,
                };
            });

            _outputHelper.WriteLine("Results:");
            _outputHelper.WriteLine(items.ToJson().AsIndentString());
            var results = items
                .Select(m =>
            {
                if (!m.TryGetProperty("name", out var p)) throw new NotSupportedException("Should filtered out");
                return p.GetString();

            }).ToArray();
            string[] expected = { "Yaron", "Aviad" };
            Assert.True(expected.SequenceEqual(results));
        }
    }
}
