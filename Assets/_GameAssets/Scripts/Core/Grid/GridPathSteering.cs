using System.Collections.Generic;
using UnityEngine;

namespace Deckbuilder.Grid
{
    public class GridPathSteering
    {
        private readonly List<Vector2Int> m_path = new List<Vector2Int>();
        private readonly List<Vector2Int> m_shortestPath = new List<Vector2Int>();
        private readonly List<Vector2Int> m_segment = new List<Vector2Int>();

        public IReadOnlyList<Vector2Int> Path => m_path;
        public int StepCount => m_path.Count > 0 ? m_path.Count - 1 : 0;

        public void Clear()
        {
            m_path.Clear();
        }

        public bool TrySteerTo(IGridTopology _topology, Vector2Int _origin, Vector2Int _destination, int _maxSteps, bool _ignoreOccupants = false)
        {
            if (m_path.Count == 0 || m_path[0] != _origin)
            {
                m_path.Clear();
                m_path.Add(_origin);
            }

            if (!GridPathfinder.TryFindPath(_topology, _origin, _destination, m_shortestPath, _ignoreOccupants))
            {
                ResetToOrigin(_origin);
                return false;
            }

            int _shortestStepCount = m_shortestPath.Count - 1;

            if (_shortestStepCount > _maxSteps)
            {
                ResetToOrigin(_origin);
                return false;
            }

            if (m_path[^1] == _destination)
                return true;

            int _indexOnPath = m_path.IndexOf(_destination);

            if (_indexOnPath >= 0)
            {
                m_path.RemoveRange(_indexOnPath + 1, m_path.Count - _indexOnPath - 1);
                return true;
            }

            if (GridPathfinder.TryFindPath(_topology, m_path[^1], _destination, m_segment, _ignoreOccupants)
                && m_path.Count - 1 + m_segment.Count - 1 == _shortestStepCount)
            {
                for (int _i = 1; _i < m_segment.Count; _i++)
                    m_path.Add(m_segment[_i]);

                return true;
            }

            m_path.Clear();
            m_path.AddRange(m_shortestPath);

            return true;
        }

        private void ResetToOrigin(Vector2Int _origin)
        {
            m_path.Clear();
            m_path.Add(_origin);
        }
    }
}
