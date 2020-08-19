using System.Collections;
using System.Collections.Generic;

namespace SiS.Communication.Common
{
    internal class BiMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly Dictionary<TKey, TValue> _forward = new Dictionary<TKey, TValue>();
        private readonly Dictionary<TValue, TKey> _reverse = new Dictionary<TValue, TKey>();

        public void Add(TKey key, TValue val)
        {
            _forward.Add(key, val);
            _reverse.Add(val, key);
        }

        public void RemoveKey(TKey key)
        {
            TValue val = _forward[key];
            _forward.Remove(key);
            _reverse.Remove(val);
        }

        public void RemoveValue(TValue val)
        {
            TKey key = _reverse[val];
            _reverse.Remove(val);
            _forward.Remove(key);
        }

        public bool ContainsKey(TKey key)
        {
            return _forward.ContainsKey(key);
        }

        public bool ContainsValue(TValue val)
        {
            return _reverse.ContainsKey(val);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _forward.GetEnumerator();
        }

        public TValue this[TKey key]
        {
            get => _forward[key];
            set
            {
                if (_forward.ContainsKey(key))
                {
                    if (_reverse.ContainsKey(value))
                    {
                        return;
                    }
                    RemoveKey(key);
                }
                Add(key, value);
            }
        }

        public TKey GetKey(TValue val)
        {
            return _reverse[val];
        }

        public void SetKey(TValue val, TKey key)
        {
            if (_reverse.ContainsKey(val))
            {
                if (_forward.ContainsKey(key))
                {
                    return;
                }
                RemoveValue(val);
            }
            Add(key, val);
        }
    }
}
