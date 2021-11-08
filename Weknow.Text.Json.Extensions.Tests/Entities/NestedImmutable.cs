using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Weknow.Text.Json.Extensions.Tests
{
    public class NestedImmutable : IEquatable<NestedImmutable>
    {
        public int Id { get; set; }
        public ImmutableDictionary<string, ConsoleColor> Map { get; set; } = ImmutableDictionary<string, ConsoleColor>.Empty;

        public override bool Equals(object obj)
        {
            return Equals(obj as NestedImmutable);
        }

        public bool Equals(NestedImmutable other)
        {
            return other != null &&
                   Id == other.Id &&
                   Map.Count == other.Map.Count && other.Map.All(p => Map[p.Key] == p.Value);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Map.Aggregate(0, (a, b) => a ^ b.Key.GetHashCode() ^ b.Value.GetHashCode()));
        }

        public static bool operator ==(NestedImmutable left, NestedImmutable right)
        {
            return EqualityComparer<NestedImmutable>.Default.Equals(left, right);
        }

        public static bool operator !=(NestedImmutable left, NestedImmutable right)
        {
            return !(left == right);
        }
    }
}
