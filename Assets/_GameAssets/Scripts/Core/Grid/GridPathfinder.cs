using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace Deckbuilder.Grid
{
    public static class GridPathfinder
    {
        public static bool TryFindPath(IGridTopology _topology, Vector2Int _start, Vector2Int _goal, List<Vector2Int> _path, bool _ignoreOccupants = false, RandomStream _random = null)
        {
            _path.Clear();

            if (!_topology.Contains(_start) || !_topology.Contains(_goal))
                return false;

            if (_start == _goal)
            {
                _path.Add(_start);
                return true;
            }

            List<Vector2Int> _openSet = new List<Vector2Int> { _start };
            Dictionary<Vector2Int, Vector2Int> _cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            Dictionary<Vector2Int, int> _gScore = new Dictionary<Vector2Int, int> { [_start] = 0 };
            Dictionary<Vector2Int, int> _fScore = new Dictionary<Vector2Int, int> { [_start] = GridShapeUtility.ManhattanDistance(_start, _goal) };

            GridDirection[] _explorationOrder = GetExplorationOrder(_random);

            while (_openSet.Count > 0)
            {
                Vector2Int _current = GetLowestFScore(_openSet, _fScore, _random);

                if (_current == _goal)
                {
                    ReconstructPath(_cameFrom, _current, _path);
                    return true;
                }

                _openSet.Remove(_current);

                foreach (GridDirection _direction in _explorationOrder)
                {
                    Vector2Int _neighbor = _current + GridDirectionUtility.GetOffset(_direction);

                    if (!IsWalkable(_topology, _neighbor, _ignoreOccupants))
                        continue;

                    int _tentativeGScore = _gScore[_current] + 1;
                    if (_gScore.TryGetValue(_neighbor, out int _existingGScore) && _tentativeGScore >= _existingGScore)
                        continue;

                    _cameFrom[_neighbor] = _current;
                    _gScore[_neighbor] = _tentativeGScore;
                    _fScore[_neighbor] = _tentativeGScore + GridShapeUtility.ManhattanDistance(_neighbor, _goal);

                    if (!_openSet.Contains(_neighbor))
                        _openSet.Add(_neighbor);
                }
            }

            return false;
        }

        public static void GetReachableCoordinates(IGridTopology _topology, Vector2Int _start, int _maxSteps, List<Vector2Int> _results, bool _ignoreOccupants = false)
        {
            _results.Clear();

            if (_maxSteps <= 0 || !_topology.Contains(_start))
                return;

            Dictionary<Vector2Int, int> _visitedCost = new Dictionary<Vector2Int, int> { [_start] = 0 };
            Queue<Vector2Int> _frontier = new Queue<Vector2Int>();
            _frontier.Enqueue(_start);

            while (_frontier.Count > 0)
            {
                Vector2Int _current = _frontier.Dequeue();
                int _currentCost = _visitedCost[_current];

                if (_currentCost >= _maxSteps)
                    continue;

                foreach (GridDirection _direction in GridDirectionUtility.Orthogonal)
                {
                    Vector2Int _neighbor = _current + GridDirectionUtility.GetOffset(_direction);

                    if (!IsWalkable(_topology, _neighbor, _ignoreOccupants))
                        continue;

                    int _newCost = _currentCost + 1;
                    if (_visitedCost.TryGetValue(_neighbor, out int _existingCost) && _existingCost <= _newCost)
                        continue;

                    _visitedCost[_neighbor] = _newCost;
                    _frontier.Enqueue(_neighbor);
                }
            }

            foreach (Vector2Int _coordinate in _visitedCost.Keys)
            {
                if (_coordinate != _start)
                    _results.Add(_coordinate);
            }
        }

        public static bool IsWalkable(IGridTopology _topology, Vector2Int _coordinate, bool _ignoreOccupants)
        {
            if (!_topology.Contains(_coordinate))
                return false;

            CellFlags _flags = _topology.GetFlags(_coordinate);

            if ((_flags & CellFlags.Obstacle) != 0)
                return false;

            return _ignoreOccupants || (_flags & CellFlags.Occupied) == 0;
        }

        private static GridDirection[] GetExplorationOrder(RandomStream _random)
        {
            if (_random == null)
                return GridDirectionUtility.Orthogonal;

            GridDirection[] _shuffled = (GridDirection[])GridDirectionUtility.Orthogonal.Clone();
            _random.Shuffle(_shuffled);

            return _shuffled;
        }

        private static Vector2Int GetLowestFScore(List<Vector2Int> _openSet, Dictionary<Vector2Int, int> _fScore, RandomStream _random)
        {
            int _lowestScore = int.MaxValue;

            foreach (Vector2Int _coordinate in _openSet)
            {
                if (_fScore.TryGetValue(_coordinate, out int _score) && _score < _lowestScore)
                    _lowestScore = _score;
            }

            if (_random == null)
            {
                foreach (Vector2Int _coordinate in _openSet)
                {
                    if (_fScore.TryGetValue(_coordinate, out int _score) && _score == _lowestScore)
                        return _coordinate;
                }

                return _openSet[0];
            }

            List<Vector2Int> _candidates = new List<Vector2Int>();

            foreach (Vector2Int _coordinate in _openSet)
            {
                if (_fScore.TryGetValue(_coordinate, out int _score) && _score == _lowestScore)
                    _candidates.Add(_coordinate);
            }

            return _candidates.Count > 0 ? _random.Pick(_candidates) : _openSet[0];
        }

        private static void ReconstructPath(Dictionary<Vector2Int, Vector2Int> _cameFrom, Vector2Int _current, List<Vector2Int> _path)
        {
            _path.Add(_current);

            while (_cameFrom.TryGetValue(_current, out Vector2Int _previous))
            {
                _current = _previous;
                _path.Add(_current);
            }

            _path.Reverse();
        }
    }
}
