using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>(); 
        [SerializeField]
        private List<TValue> values = new List<TValue>();

        public SerializableDictionary() { }
        public SerializableDictionary(SerializableDictionary<TKey, TValue> copy) : base(copy) { }
        
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();

            foreach (var pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }
        
        public void OnAfterDeserialize()
        {
            this.Clear();

            int count = keys.Count;
            for (int i = 0; i < count; i++)
            {
                if (i < values.Count)
                {
                    this[keys[i]] = values[i];
                }
            }
        }
    }
}