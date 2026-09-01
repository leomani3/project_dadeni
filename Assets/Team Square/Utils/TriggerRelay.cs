using System;
using UnityEngine;

namespace Utils
{
	public class TriggerRelay : MonoBehaviour
	{
		public Action<Collider> onTriggerEnter;
		public Action<Collider> onTriggerExit;

		private void OnTriggerEnter ( Collider other )
		{
			// this.Log($"TriggerRelay OnTriggerEnter with {other.name}" );
			onTriggerEnter?.Invoke( other );
		}

		private void OnTriggerExit ( Collider other )
		{
			onTriggerExit?.Invoke( other );
		}
	}
}