using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Xunit;
using Xunit.Sdk;

using static Weknow.Text.Json.Extensions.Tests.Constants;

namespace Weknow.Text.Json.Extensions.Tests
{
    public class ImmutableSerializationTest
    {
        private readonly static Func<Dictionary<ConsoleColor, string>, Dictionary<ConsoleColor, string>, bool> COMPARE_DIC =
            (a, b) => a.Count == b.Count && a.All(p => b[p.Key] == p.Value);
        private readonly static Func<ImmutableDictionary<ConsoleColor, string>, ImmutableDictionary<ConsoleColor, string>, bool> COMPARE_IMM_DIC =
            (a, b) => a.Count == b.Count && a.All(p => b[p.Key] == p.Value);

        [Fact]
        public void Dictionary_Test()
        {
            var source = new Dictionary<ConsoleColor, string> 
            {
                [ConsoleColor.Blue] = nameof(ConsoleColor.Blue),
                [ConsoleColor.White] = nameof(ConsoleColor.White)
            };

            source.AssertSerialization(COMPARE_DIC);
        }

        [Fact]
        public void Dictionary_WithoutConvertor_Test()
        {
            var source = new Dictionary<ConsoleColor, string> 
            {
                [ConsoleColor.Blue] = nameof(ConsoleColor.Blue),
                [ConsoleColor.White] = nameof(ConsoleColor.White)
            };

            try
            {
                source.AssertSerialization(
                    COMPARE_DIC,
                    options: SerializerOptionsWithoutConverters);
                throw new Exception("Unexpected");
            }
            catch (NotSupportedException)
            {
                // expected
            }
        }

        [Fact]
        public void ImmutableDictionary_Test()
        {
            var source = ImmutableDictionary<ConsoleColor, string>.Empty
                .Add(ConsoleColor.Blue, nameof(ConsoleColor.Blue))
                .Add(ConsoleColor.White, nameof(ConsoleColor.White));

            source.AssertSerialization(COMPARE_IMM_DIC);
        }

        [Fact]
        public void Foo_Test()
        {
            var source = new Foo(2, "B", DateTime.Now);

            source.AssertSerialization();
        }

        [Fact]
        public void ImmutableDictionary_Complex_Key_Test()
        {
            var source = ImmutableDictionary<Foo, string>.Empty
                .Add(new Foo(2, "B", DateTime.Now),"B")
                .Add(new Foo(3, "Q", DateTime.Now.AddDays(1)), "Q");

            source.AssertSerialization((a, b) => a.Count == b.Count && a.All(p => b[p.Key] == p.Value));
        }

        [Fact]
        public void ImmutableDictionary_Complex_Value_Test()
        {
            var source = ImmutableDictionary<int, Foo>.Empty
                .Add(2, new Foo(2, "B", DateTime.Now))
                .Add(3, new Foo(3, "Q", DateTime.Now.AddDays(1)));

            source.AssertSerialization((a, b) => a.Count == b.Count && a.All(p => b[p.Key] == p.Value));
        }

        [Fact]
        public void ImmutableDictionary_WithoutConvertor_Test()
        {
            var source = ImmutableDictionary<ConsoleColor, string>.Empty
                .Add(ConsoleColor.Blue, nameof(ConsoleColor.Blue))
                .Add(ConsoleColor.White, nameof(ConsoleColor.White));

            try
            {
                source.AssertSerialization(
                    COMPARE_IMM_DIC,
                    options: SerializerOptionsWithoutConverters);
                throw new Exception("Unexpected");
            }
            catch (NotSupportedException)
            {
                // expected
            }
        }

        [Fact]
        public void ImmutableDictionary_Nested_Test()
        {
            var source = ImmutableDictionary<string, ConsoleColor>.Empty
                .Add(nameof(ConsoleColor.Blue), ConsoleColor.Blue)
                .Add(nameof(ConsoleColor.White), ConsoleColor.White);

            var nested = new Nested { Id = 2, Map = source};

            nested.AssertSerialization();
        }
    }
}
