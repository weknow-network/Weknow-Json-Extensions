using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;

namespace Weknow.Text.Json.Extensions.Tests
{
    /// <summary>
    /// Response structure
    /// </summary>
    public record KeyValueStyle
    {
        /// <summary>
        /// Gets he chapter selection.
        /// </summary>
        public ImmutableDictionary<string, string[]> KV { get; init; } = ImmutableDictionary<string, string[]>.Empty;
    }
}
