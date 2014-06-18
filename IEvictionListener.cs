using System;

namespace ConcurrentLinkedDictionary
{
    public interface IEvictionListener<K,V>
    {
        void onEviction(K key, V value);
    }
}

