using System.Collections.Generic;

namespace APCGS.Utils
{
    public class SafeDictionary<K,V> : Dictionary<K, V>
    {
        public V @default;

        public SafeDictionary(V @default) : base() { this.@default = @default; }
        public SafeDictionary(V @default, int capacity) : base(capacity) { this.@default = @default; }
        public SafeDictionary(V @default, IEqualityComparer<K> comparer) : base(comparer) { this.@default = @default; }
        public SafeDictionary(V @default, IDictionary<K,V> dictionary) : base(dictionary) { this.@default = @default; }
        public SafeDictionary(V @default, int capacity, IEqualityComparer<K> comparer) : base(capacity,comparer) { this.@default = @default; }
        public SafeDictionary(V @default, IDictionary<K, V> dictionary, IEqualityComparer<K> comparer) : base(dictionary,comparer) { this.@default = @default; }

        new public V this[K index]
        {
            get => ContainsKey(index) ? base[index] : @default;
            set { if (ContainsKey(index)) { if (Equals(value, @default)) Remove(index); else base[index] = value; } else if(!Equals(value, @default)) Add(index, value); }
        }
    }
}
