using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Xunit;
using Xunit.Sdk;

using static Weknow.Text.Json.Constants;

namespace Weknow.Text.Json.Extensions.Tests
{
    public class ReadOnlyMemotyArrayTests
    {
        private static readonly Func<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>, bool> _comparer =
            (a, b) => a.ToArray().SequenceEqual(b.ToArray());

        [Fact]
        public void ReadOnlyArray_Should_Fail_Test()
        {
            byte[] bytes = { 1, 2, 3, 4, 3, 2, 1 };
            ReadOnlyMemory<byte> source = bytes.AsMemory(); 

            Assert.Throws<InvalidOperationException>(() =>
            source.AssertSerialization(_comparer, options: SerializerOptionsWithoutConverters));
        }

        [Fact]
        public void ReadOnlyArray_Test()
        {
            byte[] bytes = { 1, 2, 3, 4, 3, 2, 1 };
            ReadOnlyMemory<byte> source = bytes.AsMemory();

            source.AssertSerialization(_comparer);
        }

    }
}
