using System;

namespace JetHerald
{
    public struct NamespacedId 
    {
        public string Namespace { get; init; }
        public string Id { get; init; }
        
        public NamespacedId(string str)
        {
            var ind = str.IndexOf("://");
            if (ind < 0) throw new ArgumentException("Could not parse namespaced id");
            Namespace = str[..ind].ToLowerInvariant();
            Id = str[(ind + 3)..];
        }

        public NamespacedId(string ns, string id)
        {
            Namespace = ns;
            Id = id;
        }

        public static NamespacedId Telegram(long id)
            => new NamespacedId("telegram", $"{id}");

        public static NamespacedId Discord(ulong id)
            => new NamespacedId("discord", $"{id}");   

        public override string ToString() => $"{Namespace}://{Id}";

        public override int GetHashCode() => HashCode.Combine(Namespace, Id);

        public override bool Equals(object obj)
            => obj is NamespacedId nsid && this == nsid;

        public static bool operator == (NamespacedId a, NamespacedId b)
            => a.Namespace == b.Namespace && a.Id == b.Id;

        public static bool operator !=(NamespacedId a, NamespacedId b)
            => !(a == b);

    }



}
