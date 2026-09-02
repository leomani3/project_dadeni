using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Deckbuilder.Actions
{
    public class EntityActionQueueModule : EntityModule
    {
        private readonly Queue<IEntityAction> m_queue = new();
        private Coroutine m_processRoutine;

        public bool IsProcessing => m_processRoutine != null;

        public void Enqueue(IEntityAction _action)
        {
            m_queue.Enqueue(_action);

            if (m_processRoutine == null)
                m_processRoutine = StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            while (m_queue.Count > 0)
            {
                IEntityAction _action = m_queue.Dequeue();
                yield return _action.Execute();
            }

            m_processRoutine = null;
        }

        public override void Cleanup()
        {
            if (m_processRoutine != null)
            {
                StopCoroutine(m_processRoutine);
                m_processRoutine = null;
            }

            m_queue.Clear();
        }
    }
}
