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

namespace Weknow.Text.Json.Extensions.Tests
{
    public class DeepFilterTests
    {
        private readonly ITestOutputHelper _outputHelper;

        #region Ctor

        public DeepFilterTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        #endregion Ctor


        [Fact]
        public async Task DeepFilter_Skill_Test()
        {
            using var srm = File.OpenRead("deep-filter-data.json");
            var source = await JsonDocument.ParseAsync(srm);

            var items = source.DeepFilter((json, deep, breadcrumbs) =>
            {
                string last = breadcrumbs[^1];
                if(deep == 0 && last == "skills")
                    return (false, TraverseFlow.DrillAndBreak);

                if (int.TryParse(last, out _))
                {
                    if(json.ValueKind == JsonValueKind.String)
                        return (true, TraverseFlow.Continue);
                    return (false, TraverseFlow.Continue);
                }

                return deep switch
                {
                    < 2 => (false, TraverseFlow.Drill),
                    _ => (false, TraverseFlow.BackToParent),
                };
            });

            var results = items.Select(m => m.GetString()).ToArray();
            Assert.Equal(4, results.Length);
            string[] expected = { "c#", "Typescript", "neo4J", "elasticsearch" };
            Assert.True(expected.SequenceEqual(results));
        }

        [Fact]
        public async Task DeepFilter_Friends_Test()
        {
            using var srm = File.OpenRead("deep-filter-data.json");
            var source = await JsonDocument.ParseAsync(srm);

            var items = source.DeepFilter((json, deep, breadcrumbs) =>
            {
                string last = breadcrumbs[^1];
                if(last == "role") throw new NotSupportedException("shouldn't get so deep");

                if (deep == 0)
                {
                    if (last == "friends")
                        return (false, TraverseFlow.DrillAndBreak);
                    return (false, TraverseFlow.Continue);
                }

                if (int.TryParse(last, out _))
                {
                    if(json.TryGetProperty("IsSkipper", out var p) && p.GetBoolean())
                        return (true, TraverseFlow.Continue);
                    return (false, TraverseFlow.Continue);
                }

                return deep switch
                {
                    < 2 => (false, TraverseFlow.Drill),
                    _ => (false, TraverseFlow.BackToParent),
                };
            });

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
