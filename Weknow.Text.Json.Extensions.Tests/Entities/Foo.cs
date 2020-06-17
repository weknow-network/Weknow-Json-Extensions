using System;
using System.Collections.Generic;
using System.Text;

namespace Weknow.Text.Json.Extensions.Tests
{
    public class Foo : IEquatable<Foo?>
    {
        private Foo()
        {

        }

        public Foo(int id, string name, DateTime date)
        {
            Id = id;
            Name = name;
            Date = date;
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Foo);
        }

        public bool Equals(Foo? other)
        {
            return other != null &&
                   Id == other.Id &&
                   Name == other.Name &&
                   Date == other.Date;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Name, Date);
        }

        public static bool operator ==(Foo? left, Foo? right)
        {
            return EqualityComparer<Foo>.Default.Equals(left, right);
        }

        public static bool operator !=(Foo? left, Foo? right)
        {
            return !(left == right);
        }
    }
}
