using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Xunit;
using Xunit.Sdk;

using static Weknow.Text.Json.Extensions.Tests.Constants;

namespace Weknow.Text.Json.Extensions.Tests
{
    public class DictionarySerializationTest
    {
        private readonly static Func<Dictionary<ConsoleColor, string>, Dictionary<ConsoleColor, string>, bool> COMPARE_DIC =
            (a, b) => a.Count == b.Count && a.All(p => b[p.Key] == p.Value);
        private readonly static Func<Dictionary<string, object>, Dictionary<string, object>, bool> COMPARE_STR_OBJ_DIC =
            (a, b) => a.Count == b.Count && a.All(p => b[p.Key].Equals(p.Value));
        private readonly static Func<ImmutableDictionary<ConsoleColor, string>, ImmutableDictionary<ConsoleColor, string>, bool> COMPARE_IMM_DIC =
            (a, b) => a.Count == b.Count && a.All(p => b[p.Key] == p.Value);

        #region Dictionary_Test

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

        #endregion // Dictionary_Test

        #region Dictionary_WithoutConvertor_Test

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

        #endregion // Dictionary_WithoutConvertor_Test

        #region Dictionary_String_Object_Test

        [Fact]
        public void Dictionary_String_Object_Test()
        {
            var source = new Dictionary<string, object> 
            {
                ["A"] = nameof(ConsoleColor.Blue),
                ["B"] = new Foo(10, "Bamby", DateTime.Now)
            };

            source.AssertSerialization(COMPARE_STR_OBJ_DIC);
        }

        #endregion // Dictionary_String_Object_Test

        #region Foo_Test

        [Fact]
        public void Foo_Test()
        {
            var source = new Foo(2, "B", DateTime.Now);

            source.AssertSerialization();
        }

        #endregion // Foo_Test

        #region Dictionary_Complex_Key_Test

        [Fact]
        public void Dictionary_Complex_Key_Test()
        {
            var source = new Dictionary<Foo, string>
            {
                [new Foo(2, "B", DateTime.Now)] = "B",
                [new Foo(3, "Q", DateTime.Now.AddDays(1))] = "Q"
            };

            source.AssertSerialization((a, b) => a.Count == b.Count && a.All(p => b[p.Key] == p.Value));
        }

        #endregion // Dictionary_Complex_Key_Test

        #region Dictionary_Complex_Value_Test

        [Fact]
        public void Dictionary_Complex_Value_Test()
        {
            var source = new Dictionary<int, Foo>
            { 
                [2] = new Foo(2, "B", DateTime.Now),
                [3] = new Foo(3, "Q", DateTime.Now.AddDays(1))
            };

            source.AssertSerialization((a, b) => a.Count == b.Count && a.All(p => b[p.Key] == p.Value));
        }

        #endregion // Dictionary_Complex_Value_Test

        #region Dictionary_Nested_Test

        [Fact]
        public void Dictionary_Nested_Test()
        {
            var source = new Dictionary<ConsoleColor, string>
            {
                [ConsoleColor.Blue] = nameof(ConsoleColor.Blue),
                [ConsoleColor.White] = nameof(ConsoleColor.White)
            };

            var nested = new Nested { Id = 2, Map = source};

            nested.AssertSerialization();
        }

        #endregion // Dictionary_Nested_Test
    }
}
