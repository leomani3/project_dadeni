using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using CW.Common;
using Pinpin;

namespace Lean.Pool
{
	/// <summary>This component allows you to pool GameObjects, giving you a very fast alternative to Instantiate and Destroy.
	/// Pools also have settings to preload, recycle, and set the spawn capacity, giving you lots of control over your spawning.</summary>
	[ExecuteInEditMode]
	public class LeanComponentPool<T> : ACommonPool, ISerializationCallbackReceiver where T:Component
	{
		[System.Serializable]
		public class Delay
		{
			public T Clone;
			public float Life;
		}

		public enum NotificationType
		{
			None,
			SendMessage,
			BroadcastMessage,
			IPoolable,
			BroadcastIPoolable
		}

		public enum StrategyType
		{
			ActivateAndDeactivate,
			DeactivateViaHierarchy
		}

		/// <summary>All active and enabled pools in the scene.</summary>
		public static LinkedList<LeanComponentPool<T>> Instances = new LinkedList<LeanComponentPool<T>>(); private LinkedListNode<LeanComponentPool<T>> instancesNode;

		/// <summary>The prefab this pool controls.</summary>
		public T Prefab { set { if (value != prefab) { UnregisterPrefab(); prefab = value; RegisterPrefab(); } } get { return prefab; } } [SerializeField] private T prefab;
		public NotificationType Notification { set { notification = value; } get { return notification; } } [SerializeField] private NotificationType notification = NotificationType.IPoolable;
		public StrategyType Strategy { set { strategy = value; } get { return strategy; } } [SerializeField] private StrategyType strategy = StrategyType.ActivateAndDeactivate;
		public int Preload { set { preload = value; } get { return preload; } } [SerializeField] private int preload;
		public int Capacity { set { capacity = value; } get { return capacity; } } [SerializeField] private int capacity;
		public bool Persist { set { persist = value; } get { return persist; } } [SerializeField] private bool persist;
		public bool Stamp { set { stamp = value; } get { return stamp; } } [SerializeField] private bool stamp;
		public bool Warnings { set { warnings = value; } get { return warnings; } } [SerializeField] private bool warnings = true;
		[SerializeField]
		private List<T> spawnedClonesList = new List<T>();
		private HashSet<T> spawnedClonesHashSet = new HashSet<T>();
		[SerializeField]
		private List<T> despawnedClones = new List<T>();
		[SerializeField]
		private List<Delay> delays = new List<Delay>();

		private Dictionary<GameObject, T> spawnedMap = new Dictionary<GameObject, T>();
		
		[SerializeField]
		private Transform deactivatedChild;

		private static List<IPoolable> tempPoolables = new List<IPoolable>();

		/// <summary>If you're using the <b>Strategy = DeactivateViaHierarchy</b> mode, then all despawned clones will be placed under this.</summary>
		public Transform DeactivatedChild
		{
			get
			{
				if (deactivatedChild == null)
				{
					var child = new GameObject("Despawned Clones");

					child.SetActive(false);

					deactivatedChild = child.transform;

					deactivatedChild.SetParent(transform, false);
				}

				return deactivatedChild;
			}
		}

#if UNITY_EDITOR
		/// <summary>This will return false if you have preloaded prefabs do not match the <b>Prefab</b>.
		/// NOTE: This is only available in the editor.</summary>
		public bool DespawnedClonesMatch
		{
			get
			{
				for (var i = despawnedClones.Count - 1; i >= 0; i--)
				{
					var clone = despawnedClones[i];

					if (clone != null && UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(clone) != prefab)
					{
						return false;
					}
				}

				return true;
			}
		}
#endif

		public override void SetPrefab(GameObject _prefab)
		{
			if (_prefab.TryGetComponent(out T _prefabComponent))
			{
				Prefab = _prefabComponent;
			}
			else
			{
				Debug.LogError("Failed to set prefab " + _prefab.name + " to pool " + this.name);
			}
		}

		public override bool IsInPool(GameObject clone)
		{
			return spawnedMap.ContainsKey(clone);
		}

		/// <summary>Returns the amount of spawned clones.</summary>
		public int Spawned
		{
			get
			{
				return spawnedClonesList.Count + spawnedClonesHashSet.Count;
			}
		}

		/// <summary>Returns the amount of despawned clones.</summary>
		public int Despawned
		{
			get
			{
				return despawnedClones.Count;
			}
		}

		/// <summary>Returns the total amount of spawned and despawned clones.</summary>
		public int Total
		{
			get
			{
				return Spawned + Despawned;
			}
		}

		/// <summary>This will either spawn a previously despawned/preloaded clone, recycle one, create a new one, or return null.
		/// NOTE: This method is designed to work with Unity's event system, so it has no return value.</summary>
		public void Spawn()
		{
			var clone = default(T); TrySpawn(ref clone);
		}

		/// <summary>This will either spawn a previously despawned/preloaded clone, recycle one, create a new one, or return null.
		/// NOTE: This method is designed to work with Unity's event system, so it has no return value.</summary>
		public void Spawn(Vector3 position)
		{
			var clone = default(T); TrySpawn(ref clone, position, transform.localRotation);
		}

		/// <summary>This will either spawn a previously despawned/preloaded clone, recycle one, create a new one, or return null.</summary>
		public T Spawn(Transform parent, bool worldPositionStays = false)
		{
			var clone = default(T); TrySpawn(ref clone, parent, worldPositionStays); return clone;
		}

		/// <summary>This will either spawn a previously despawned/preloaded clone, recycle one, create a new one, or return null.</summary>
		public T Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
		{
			var clone = default(T); TrySpawn(ref clone, position, rotation, parent); return clone;
		}

		/// <summary>This will either spawn a previously despawned/preloaded clone, recycle one, create a new one, or return null.</summary>
		public bool TrySpawn(ref T clone, Transform parent, bool worldPositionStays = false)
		{
			if (prefab == null) { if (warnings == true) Debug.LogWarning("You're attempting to spawn from a pool with a null prefab", this); return false; }
			if (parent != null && worldPositionStays == true)
			{
				return TrySpawn(ref clone, prefab.transform.position, Quaternion.identity, Vector3.one, parent, worldPositionStays);
			}
			return TrySpawn(ref clone, transform.localPosition, transform.localRotation, transform.localScale, parent, worldPositionStays);
		}

		/// <summary>This will either spawn a previously despawned/preloaded clone, recycle one, create a new one, or return null.</summary>
		public bool TrySpawn(ref T clone, Vector3 position, Quaternion rotation, Transform parent = null)
		{
			if (prefab == null) { if (warnings == true) Debug.LogWarning("You're attempting to spawn from a pool with a null prefab", this); return false; }
			if (parent != null)
			{
				position = parent.InverseTransformPoint(position);
				rotation = Quaternion.Inverse(parent.rotation) * rotation;
			}
			return TrySpawn(ref clone, position, rotation, prefab.transform.localScale, parent, false);
		}

		/// <summary>This will either spawn a previously despawned/preloaded clone, recycle one, create a new one, or return null.</summary>
		public bool TrySpawn(ref T clone)
		{
			if (prefab == null) { if (warnings == true) Debug.LogWarning("You're attempting to spawn from a pool with a null prefab", this); return false; }
			var transform = prefab.transform;
			return TrySpawn(ref clone, transform.localPosition, transform.localRotation, transform.localScale, null, false);
		}

		public override bool TrySpawn(ref GameObject clone, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Transform parent, bool worldPositionStays)
		{
			T objClone = null;
			if (TrySpawn(ref objClone, localPosition, localRotation, localScale, parent, worldPositionStays))
			{
				if (objClone != null)
					clone = objClone.gameObject;
				return true;
			}
			return false;
		}

		/// <summary>This will either spawn a previously despawned/preloaded clone, recycle one, create a new one, or return null.</summary>
		public bool TrySpawn(ref T clone, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Transform parent, bool worldPositionStays)
		{
			if (prefab != null)
			{
				// Spawn a previously despawned/preloaded clone?
				for (var i = despawnedClones.Count - 1; i >= 0; i--)
				{
					clone = despawnedClones[i];

					despawnedClones.RemoveAt(i);

					if (clone != null)
					{
						SpawnClone(clone, localPosition, localRotation, localScale, parent, worldPositionStays);

						return true;
					}

					if (warnings == true) Debug.LogWarning("This pool contained a null despawned clone, did you accidentally destroy it?", this);
				}

				// Make a new clone?
				if (capacity <= 0 || Total < capacity)
				{
					clone = CreateClone(localPosition, localRotation, localScale, parent, worldPositionStays);

					// Add clone to spawned list
					if (recycle == true)
					{
						spawnedClonesList.Add(clone);
					}
					else
					{
						spawnedClonesHashSet.Add(clone);
					}
					spawnedMap.Add(clone.gameObject, clone);

					// Activate?
					if (strategy == StrategyType.ActivateAndDeactivate)
					{
						clone.gameObject.SetActive(true);
					}

					// Notifications
					InvokeOnSpawn(clone);

					return true;
				}

				// Recycle?
				if (recycle == true && TryDespawnOldest(ref clone, false) == true)
				{
					SpawnClone(clone, localPosition, localRotation, localScale, parent, worldPositionStays);

					return true;
				}
			}
			else
			{
				if (warnings == true) Debug.LogWarning("You're attempting to spawn from a pool with a null prefab", this);
			}

			return false;
		}

		/// <summary>This will despawn the oldest prefab clone that is still spawned.</summary>
		[ContextMenu("Despawn Oldest")]
		public void DespawnOldest()
		{
			var clone = default(T);

			TryDespawnOldest(ref clone, true);
		}

		private bool TryDespawnOldest(ref T clone, bool registerDespawned)
		{
			MergeSpawnedClonesToList();

			// Loop through all spawnedClones from the front (oldest) until one is found
			while (spawnedClonesList.Count > 0)
			{
				clone = spawnedClonesList[0];

				spawnedClonesList.RemoveAt(0);
				spawnedMap.Remove(clone.gameObject);

				if (clone != null)
				{
					DespawnNow(clone, registerDespawned);

					return true;
				}

				if (warnings == true) Debug.LogWarning("This pool contained a null spawned clone, did you accidentally destroy it?", this);
			}

			return false;
		}

		[ContextMenu("Despawn All")]
		public void DespawnAll()
		{
			DespawnAll(true);
		}

		/// <summary>This method will despawn all currently spawned prefabs managed by this pool.
		/// NOTE: If this pool's prefabs were spawned using <b>LeanPool.Spawn</b>, then <b>cleanLinks</b> should be set to true.</summary>
		public override void DespawnAll(bool cleanLinks)
		{
			// Merge
			MergeSpawnedClonesToList();

			// Despawn
			for (var i = spawnedClonesList.Count - 1; i >= 0; i--)
			{
				var clone = spawnedClonesList[i];

				if (clone != null)
				{
					if (cleanLinks == true)
					{
						LeanPool.Links.Remove(clone.gameObject);
					}

					DespawnNow(clone);
				}
			}

			spawnedClonesList.Clear();
			spawnedMap.Clear();

			// Clear all delays
			for (var i = delays.Count - 1; i >= 0; i--)
			{
				LeanClassPool<Delay>.Despawn(delays[i]);
			}

			delays.Clear();
		}

		/// <summary>This will either instantly despawn the specified gameObject, or delay despawn it after t seconds.</summary>
		public void Despawn(T clone, float t = 0.0f)
		{
			if (clone != null)
			{
				// Delay the despawn?
				if (t > 0.0f)
				{
					DespawnWithDelay(clone, t);
				}
				// Despawn now?
				else
				{
					TryDespawn(clone);

					// If this clone was marked for delayed despawn, remove it
					for (var i = delays.Count - 1; i >= 0; i--)
					{
						var delay = delays[i];

						if (delay.Clone == clone)
						{
							delays.RemoveAt(i);
						}
					}
				}
			}
			else
			{
				if (warnings == true) Debug.LogWarning("You're attempting to despawn a null gameObject", this);
			}
		}

		public override void Despawn(GameObject gameObjectClone, float delay)
		{
			if (gameObjectClone != null)
			{
				if (spawnedMap.TryGetValue(gameObjectClone, out var clone) == true)
				{
					Despawn(clone, delay);
				}
				else
				{
					if (warnings == true) Debug.LogWarning("You're attempting to despawn a GameObject that wasn't spawned from this pool. (CUSTOM)", gameObject);
				}
			}
			else
			{
				if (warnings == true) Debug.LogWarning("You're attempting to despawn a null GameObject (CUSTOM)", this);
			}
		}

		/// <summary>This allows you to remove all references to the specified clone from this pool.
		/// A detached clone will act as a normal GameObject, requiring you to manually destroy or otherwise manage it.
		/// NOTE: If this clone has been despawned then it will still be parented to the pool.
		/// NOTE: If this pool's prefabs were spawned using <b>LeanPool.Spawn</b>, then <b>cleanLinks</b> should be set to true.</summary>
		public void Detach(T clone, bool cleanLinks = true)
		{
			if (clone != null)
			{
				if (spawnedClonesHashSet.Remove(clone) == true || spawnedClonesList.Remove(clone) == true || despawnedClones.Remove(clone) == true)
				{
					spawnedMap.Remove(clone.gameObject);
					if (cleanLinks == true)
					{
						// Remove the link between this clone and this pool if it hasn't already been
						LeanPool.Links.Remove(clone.gameObject);
					}

					// If this clone was marked for delayed despawn, remove it
					for (var i = delays.Count - 1; i >= 0; i--)
					{
						var delay = delays[i];

						if (delay.Clone == clone)
						{
							delays.RemoveAt(i);
						}
					}
				}
				else
				{
					if (warnings == true) Debug.LogWarning("You're attempting to detach a GameObject that wasn't spawned from this pool.", clone);
				}
			}
			else
			{
				if (warnings == true) Debug.LogWarning("You're attempting to detach a null GameObject", this);
			}
		}

		public override void Detach(GameObject clone, bool cleanLinks = true)
		{
			if (spawnedMap.TryGetValue(clone, out var componentClone) == true)
			{
				Detach(componentClone, cleanLinks);
			}
			else
			{
				if (warnings == true) Debug.LogWarning("You're attempting to detach a GameObject that wasn't spawned from this pool. (CUSTOM)", clone);
			}
		}

		/// <summary>This method will create an additional prefab clone and add it to the despawned list.</summary>
		[ContextMenu("Preload One More")]
		public void PreloadOneMore()
		{
			if (prefab != null)
			{
				// Create clone
				var clone = CreateClone(Vector3.zero, Quaternion.identity, Vector3.one, null, false);

				// Add clone to despawned list
				despawnedClones.Add(clone);

				// Deactivate it
				if (strategy == StrategyType.ActivateAndDeactivate)
				{
					clone.gameObject.SetActive(false);

					clone.transform.SetParent(transform, false);
				}
				else
				{
					clone.transform.SetParent(DeactivatedChild, false);
				}

				if (warnings == true && capacity > 0 && Total > capacity) Debug.LogWarning("You've preloaded more than the pool capacity, please verify you're preloading the intended amount.", this);
			}
			else
			{
				if (warnings == true) Debug.LogWarning("Attempting to preload a null prefab.", this);
			}
		}

		/// <summary>This will preload the pool based on the <b>Preload</b> setting.</summary>
		[ContextMenu("Preload All")]
		public void PreloadAll()
		{
			if (preload > 0)
			{
				if (prefab != null)
				{
					for (var i = Total; i < preload; i++)
					{
						PreloadOneMore();
					}
				}
				else if (warnings == true)
				{
					if (warnings == true) Debug.LogWarning("Attempting to preload a null prefab", this);
				}
			}
		}

		/// <summary>This will destroy all preloaded or despawned clones. This is useful if you've despawned more prefabs than you likely need, and want to free up some memory.</summary>
		[ContextMenu("Clean")]
		public void Clean()
		{
			for (var i = despawnedClones.Count - 1; i >= 0; i--)
			{
				DestroyImmediate(despawnedClones[i]);
			}

			despawnedClones.Clear();
		}

		/// <summary>This method will clear and fill the specified list with the specified clones from this pool.</summary>
		public void GetClones(List<T> cloneList, bool addSpawnedClones = true, bool addDespawnedClones = true)
		{
			if (cloneList != null)
			{
				cloneList.Clear();

				if (addSpawnedClones == true)
				{
					cloneList.AddRange(spawnedClonesList);
					cloneList.AddRange(spawnedClonesHashSet);
				}

				if (addDespawnedClones == true)
				{
					cloneList.AddRange(despawnedClones);
				}
			}
		}

		protected virtual void Awake()
		{
			if (Application.isPlaying == true)
			{
				PreloadAll();

				if (persist == true)
				{
					DontDestroyOnLoad(this);
				}
			}
		}

		protected virtual void OnEnable()
		{
			instancesNode = Instances.AddLast(this);
			commonInstanceNode = CommonInstances.AddLast(this);

			RegisterPrefab();
		}

		protected virtual void OnDisable()
		{
			UnregisterPrefab();

			CommonInstances.Remove(commonInstanceNode); commonInstanceNode = null;
			Instances.Remove(instancesNode); instancesNode = null;
		}

		protected virtual void OnDestroy()
		{
			// If OnDestroy is called then the scene is likely changing, so we detach the spawned prefabs from the global links dictionary to prevent issues.
			foreach (var clone in spawnedClonesList)
			{
				if (clone != null)
				{
					LeanPool.Detach(clone, false);
				}
			}

			foreach (var clone in spawnedClonesHashSet)
			{
				if (clone != null)
				{
					LeanPool.Detach(clone, false);
				}
			}
		}

		protected virtual void Update()
		{
			// Decay the life of all delayed destruction calls
			for (var i = delays.Count - 1; i >= 0; i--)
			{
				var delay = delays[i];

				delay.Life -= Time.deltaTime;

				// Skip to next one?
				if (delay.Life > 0.0f)
				{
					continue;
				}

				// Remove and pool delay
				delays.RemoveAt(i); LeanClassPool<Delay>.Despawn(delay);

				// Finally despawn it after delay
				if (delay.Clone != null)
				{
					Despawn(delay.Clone);
				}
				else
				{
					if (warnings == true) Debug.LogWarning("Attempting to update the delayed destruction of a prefab clone that no longer exists, did you accidentally destroy it?", this);
				}
			}
		}

		private void RegisterPrefab()
		{
			if (prefab != null)
			{
				var existingPool = default(ACommonPool);

				if (prefabMap.TryGetValue(prefab.gameObject, out existingPool) == true)
				{
					// if (existingPool != this)
					// 	Debug.LogWarning("You have multiple pools managing the same prefab (" + prefab.name + ").", existingPool);
				}
				else
				{
					prefabMap.Add(prefab.gameObject, this);
				}
			}
		}

		private void UnregisterPrefab()
		{
			// Skip actually null prefabs, but allow destroyed prefabs
			if (Equals(prefab, null) == true)
			{
				return;
			}

			var existingPool = default(ACommonPool);

			if (prefabMap.TryGetValue(prefab.gameObject, out existingPool) == true && existingPool == this)
			{
				prefabMap.Remove(prefab.gameObject);
			}
		}

		private void DespawnWithDelay(T clone, float t)
		{
			// If this object is already marked for delayed despawn, update the time and return
			for (var i = delays.Count - 1; i >= 0; i--)
			{
				var delay = delays[i];

				if (delay.Clone == clone)
				{
					if (t < delay.Life)
					{
						delay.Life = t;
					}

					return;
				}
			}

			// Create delay
			var newDelay = LeanClassPool<Delay>.Spawn() ?? new Delay();

			newDelay.Clone = clone;
			newDelay.Life  = t;

			delays.Add(newDelay);
		}

		private void TryDespawn(T clone)
		{
			if (spawnedClonesHashSet.Remove(clone) == true || spawnedClonesList.Remove(clone) == true)
			{
				spawnedMap.Remove(clone.gameObject);
				DespawnNow(clone);
			}
			else
			{
				if (warnings == true) Debug.LogWarning("You're attempting to despawn a GameObject that wasn't spawned from this pool, make sure your Spawn and Despawn calls match.", clone);
			}
		}

		private void DespawnNow(T clone, bool register = true)
		{
			// Add clone to despawned list
			if (register == true)
			{
				despawnedClones.Add(clone);
			}

			// Messages?
			InvokeOnDespawn(clone);

			// Deactivate it
			if (strategy == StrategyType.ActivateAndDeactivate)
			{
				clone.gameObject.SetActive(false);

				clone.transform.SetParent(transform, false);
			}
			else
			{
				clone.transform.SetParent(DeactivatedChild, false);
			}
		}

		private T CreateClone(Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Transform parent, bool worldPositionStays)
		{
			var clone = DoInstantiate(prefab, localPosition, localRotation, localScale, parent, worldPositionStays);

			if (stamp == true)
			{
				clone.name = prefab.name + " " + Total;
			}
			else
			{
				clone.name = prefab.name;
			}

			return clone;
		}

		private T DoInstantiate(T prefab, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Transform parent, bool worldPositionStays)
		{
#if UNITY_EDITOR
			if (Application.isPlaying == false && UnityEditor.PrefabUtility.IsPartOfRegularPrefab(prefab) == true)
			{
				if (worldPositionStays == true)
				{
					return (T)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
				}
				else
				{
					var clone = (T)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);

					clone.transform.localPosition = localPosition;
					clone.transform.localRotation = localRotation;
					clone.transform.localScale    = localScale;

					return clone;
				}
			}
#endif

			if (worldPositionStays == true)
			{
				return Instantiate(prefab, parent, true);
			}
			else
			{
				var clone = Instantiate(prefab, localPosition, localRotation, parent);

				clone.transform.localPosition = localPosition;
				clone.transform.localRotation = localRotation;
				clone.transform.localScale    = localScale;

				return clone;
			}
		}

		private void SpawnClone(T clone, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Transform parent, bool worldPositionStays)
		{
			// Register
			if (recycle == true)
			{
				spawnedClonesList.Add(clone);
			}
			else
			{
				spawnedClonesHashSet.Add(clone);
			}
			spawnedMap.Add(clone.gameObject, clone);

			// Update transform
			var cloneTransform = clone.transform;

			cloneTransform.SetParent(null, false);

			cloneTransform.localPosition = localPosition;
			cloneTransform.localRotation = localRotation;
			cloneTransform.localScale    = localScale;

			cloneTransform.SetParent(parent, worldPositionStays);

			// Make sure it's in the current scene
			if (parent == null)
			{
				SceneManager.MoveGameObjectToScene(clone.gameObject, SceneManager.GetActiveScene());
			}

			// Activate
			if (strategy == StrategyType.ActivateAndDeactivate)
			{
				clone.gameObject.SetActive(true);
			}

			// Notifications
			InvokeOnSpawn(clone);
		}

		private void InvokeOnSpawn(T clone)
		{
			switch (notification)
			{
				case NotificationType.SendMessage: clone.SendMessage("OnSpawn", SendMessageOptions.DontRequireReceiver); break;
				case NotificationType.BroadcastMessage: clone.BroadcastMessage("OnSpawn", SendMessageOptions.DontRequireReceiver); break;
				case NotificationType.IPoolable: clone.GetComponents(tempPoolables); for (var i = tempPoolables.Count - 1; i >= 0; i--) tempPoolables[i].OnSpawn(); break;
				case NotificationType.BroadcastIPoolable: clone.GetComponentsInChildren(tempPoolables); for (var i = tempPoolables.Count - 1; i >= 0; i--) tempPoolables[i].OnSpawn(); break;
			}
		}

		private void InvokeOnDespawn(T clone)
		{
			switch (notification)
			{
				case NotificationType.SendMessage: clone.SendMessage("OnDespawn", SendMessageOptions.DontRequireReceiver); break;
				case NotificationType.BroadcastMessage: clone.BroadcastMessage("OnDespawn", SendMessageOptions.DontRequireReceiver); break;
				case NotificationType.IPoolable: clone.GetComponents(tempPoolables); for (var i = tempPoolables.Count - 1; i >= 0; i--) tempPoolables[i].OnDespawn(); break;
				case NotificationType.BroadcastIPoolable: clone.GetComponentsInChildren(tempPoolables); for (var i = tempPoolables.Count - 1; i >= 0; i--) tempPoolables[i].OnDespawn(); break;
			}
		}

		private void MergeSpawnedClonesToList()
		{
			if (spawnedClonesHashSet.Count > 0)
			{
				spawnedClonesList.AddRange(spawnedClonesHashSet);
				foreach (T item in spawnedClonesHashSet)
				{
					spawnedMap.Add(item.gameObject, item);
				}

				spawnedClonesHashSet.Clear();
			}
		}

		public void OnBeforeSerialize()
		{
			MergeSpawnedClonesToList();
		}

		public void OnAfterDeserialize()
		{
			if (recycle == false)
			{
				for (var i = spawnedClonesList.Count - 1; i >= 0; i--)
				{
					var clone = spawnedClonesList[i];

					spawnedClonesHashSet.Add(clone);
				}

				spawnedClonesList.Clear();
				spawnedMap.Clear();
			}
		}
	}
}