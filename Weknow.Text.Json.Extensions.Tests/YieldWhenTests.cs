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

        #region YieldWhen_Path_Test

        [Theory]
        [InlineData("friends.[].name", "Yaron,Aviad,Eyal")]
        [InlineData("friends.*.name", "Yaron,Aviad,Eyal")]
        [InlineData("*.[].name", "Yaron,Aviad,Eyal")]
        [InlineData("friends.[1].name", "Aviad")]
        [InlineData("skills.*.Role.[]", "architect,cto")]
        [InlineData("skills.*.level", "3")]
        [InlineData("skills.[3].role.[]", "architect,cto")]
        [InlineData("skills.[3]", @"{""role"":[""architect"",""cto""],""level"":3}")]
        public async Task YieldWhen_Path_Test(string path, string expectedJoined)
        {
            using var srm = File.OpenRead("deep-filter-data.json");
            var source = await JsonDocument.ParseAsync(srm);

            var items = source.YieldWhen(path);

            var results = items.Select(m => 
                m.ValueKind switch 
                {
                    JsonValueKind.Number => $"{m.GetInt32()}",
                    JsonValueKind.Array => string.Join(",", m.EnumerateArray().Select(a => a.GetString())),
                    JsonValueKind.Object => m.AsString(),
                    _ => m.GetString()
                }).ToArray();
            string[] expected = expectedJoined.StartsWith("{") ? new[] { expectedJoined } : expectedJoined.Split(",");
            Assert.True(expected.SequenceEqual(results));
        }

        [Theory]
        [InlineData("skills.[3].role.[]", JsonValueKind.String, "architect")]
        [InlineData("skills.[3].role", JsonValueKind.Array, "architect,cto")]
        public async Task YieldWhen_Path_Array_Test(string path, JsonValueKind expectedKind, string expectedJoined)
        {
            using var srm = File.OpenRead("deep-filter-data.json");
            var source = await JsonDocument.ParseAsync(srm);

            var item = source.YieldWhen(path).First();
            Assert.Equal(expectedKind, item.ValueKind);
            var res = item.ValueKind switch
            {
                JsonValueKind.Array => string.Join(",", item.EnumerateArray().Select(a => a.GetString())),
                _ => item.GetString()
            };
            Assert.Equal(expectedJoined, res);
        }

        #endregion // YieldWhen_Path_Test

        #region YieldWhen_Skill_Test

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

        #endregion // YieldWhen_Skill_Test

        #region YieldWhen_Friends_Test

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

        #endregion // YieldWhen_Friends_Test
    }
}
