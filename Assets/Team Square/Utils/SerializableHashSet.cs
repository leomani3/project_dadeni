using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    [Serializable]
    public class SerializableHashSet<T> : HashSet<T>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<T> items = new List<T>(); // Liste pour la sérialisation

        public SerializableHashSet() { }

        public SerializableHashSet(SerializableHashSet<T> copy) : base(copy) { }

        // Sauvegarde des éléments du HashSet dans la liste avant la sérialisation
        public void OnBeforeSerialize()
        {
            items.Clear();
            items.AddRange(this);
        }

        // Chargement des éléments dans le HashSet après la désérialisation
        public void OnAfterDeserialize()
        {
            this.Clear();
            foreach (var item in items)
            {
                this.Add(item);
            }
        }
    }
}

