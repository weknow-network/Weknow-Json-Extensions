using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace Weknow.Text.Json.Extensions.Tests
{
    /// <summary>
    /// Response structure
    /// </summary>
    public record FromFile
    {
        public string Vertical { get; init; }

        /// <summary>
        /// Gets he product
        /// </summary>
        public string Product { get; init; } = string.Empty;

        /// <summary>
        /// The choices
        /// </summary>
        public ImmutableDictionary<string, string[]> Choices;

        /// <summary>
        /// Gets he geographic.
        /// </summary>
        public GeoInfo Geo { get; init; } = new GeoInfo();

        /// <summary>
        /// Gets he chapter selection.
        /// </summary>
        public ImmutableDictionary<string, string[]> ChaptersChoices { get; init; } = ImmutableDictionary<string, string[]>.Empty;

        /// <summary>
        /// Gets he entityCount.
        /// </summary>
        public int EntityCount { get; init; } = 1;

        /// <summary>
        /// Gets he entitiesDetails.
        /// </summary>
        public JsonElement[] EntitiesDetails { get; init; } = Array.Empty<JsonElement>();
    }

    /// <summary>
    /// Geographic information
    /// </summary>
    public record GeoInfo
    {

        /// <summary>
        /// Gets or regions.
        /// </summary>
        public ImmutableDictionary<string, string[]> Regions { get; init; } =
                ImmutableDictionary<string, string[]>.Empty;

        /// <summary>
        /// Gets or countries.
        /// </summary>
        public string[] Countries { get; init; } = Array.Empty<string>();
    }
}
