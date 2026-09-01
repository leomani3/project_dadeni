using Lean.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pinpin
{
	public abstract class ACommonPool : MonoBehaviour
	{
		/// <summary>All active and enabled pools in the scene.</summary>
		public static LinkedList<ACommonPool> CommonInstances = new LinkedList<ACommonPool>(); protected LinkedListNode<ACommonPool> commonInstanceNode;
		protected static Dictionary<GameObject, ACommonPool> prefabMap = new Dictionary<GameObject, ACommonPool>();
		public bool Recycle { set { recycle = value; } get { return recycle; } }
		[SerializeField] protected bool recycle;


		/// <summary>Find the pool responsible for handling the specified prefab.</summary>
		public static bool TryFindPoolByPrefab(GameObject prefab, ref ACommonPool foundPool)
		{
			return prefabMap.TryGetValue(prefab, out foundPool);
		}

		public abstract void SetPrefab(GameObject _prefab);
		public abstract void DespawnAll(bool cleanLinks);
		public abstract void Despawn(GameObject gameObject, float delay);
		public abstract bool IsInPool(GameObject clone);
		public abstract void Detach(GameObject clone, bool cleanLinks = true);
		public abstract bool TrySpawn(ref GameObject clone, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Transform parent, bool worldPositionStays);

		/// <summary>Find the pool responsible for handling the specified prefab clone.
		/// NOTE: This can be an expensive operation if you have many large pools.</summary>
		public static bool TryFindPoolByClone(GameObject clone, ref ACommonPool pool)
		{
			foreach (var instance in CommonInstances)
			{
				if (instance.IsInPool(clone))
				{
					pool = instance;
					return true;
				}
			}

			return false;
		}
	}
}