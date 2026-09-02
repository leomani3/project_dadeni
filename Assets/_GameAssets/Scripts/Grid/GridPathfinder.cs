using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deckbuilder.Grid
{
    public static class GridPathfinder
    {
        public static List<GridCell> FindPath(GridCell _start, GridCell _goal, Func<Vector2Int, GridCell> _cellResolver, bool _ignoreOccupants = false, bool _randomize = false)
        {
            if (_start == null || _goal == null)
                return null;

            Vector2Int _startCoordinate = _start.Coordinate;
            Vector2Int _goalCoordinate = _goal.Coordinate;

            List<Vector2Int> _openSet = new List<Vector2Int> { _startCoordinate };
            Dictionary<Vector2Int, Vector2Int> _cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            Dictionary<Vector2Int, int> _gScore = new Dictionary<Vector2Int, int> { [_startCoordinate] = 0 };
            Dictionary<Vector2Int, int> _fScore = new Dictionary<Vector2Int, int> { [_startCoordinate] = ManhattanDistance(_startCoordinate, _goalCoordinate) };

            while (_openSet.Count > 0)
            {
                Vector2Int _current = GetLowestFScore(_openSet, _fScore, _randomize);

                if (_current == _goalCoordinate)
                    return ReconstructPath(_cameFrom, _current, _cellResolver);

                _openSet.Remove(_current);

                foreach (GridDirection _direction in GetExplorationOrder(_randomize))
                {
                    Vector2Int _neighborCoordinate = _current + GridDirectionUtility.GetOffset(_direction);
                    GridCell _neighborCell = _cellResolver(_neighborCoordinate);

                    if (_neighborCell == null)
                        continue;

                    if (!IsWalkable(_neighborCell, _ignoreOccupants))
                        continue;

                    int _tentativeGScore = _gScore[_current] + 1;
                    if (_gScore.TryGetValue(_neighborCoordinate, out int _existingGScore) && _tentativeGScore >= _existingGScore)
                        continue;

                    _cameFrom[_neighborCoordinate] = _current;
                    _gScore[_neighborCoordinate] = _tentativeGScore;
                    _fScore[_neighborCoordinate] = _tentativeGScore + ManhattanDistance(_neighborCoordinate, _goalCoordinate);

                    if (!_openSet.Contains(_neighborCoordinate))
                        _openSet.Add(_neighborCoordinate);
                }
            }

            return null;
        }

        public static List<GridCell> GetReachableCells(GridCell _start, int _maxSteps, Func<Vector2Int, GridCell> _cellResolver, bool _ignoreOccupants = false)
        {
            List<GridCell> _result = new List<GridCell>();
            if (_start == null || _maxSteps <= 0)
                return _result;

            Dictionary<Vector2Int, int> _visitedCost = new Dictionary<Vector2Int, int> { [_start.Coordinate] = 0 };
            Queue<Vector2Int> _frontier = new Queue<Vector2Int>();
            _frontier.Enqueue(_start.Coordinate);

            while (_frontier.Count > 0)
            {
                Vector2Int _current = _frontier.Dequeue();
                int _currentCost = _visitedCost[_current];

                if (_currentCost >= _maxSteps)
                    continue;

                foreach (GridDirection _direction in GridDirectionUtility.Orthogonal)
                {
                    Vector2Int _neighborCoordinate = _current + GridDirectionUtility.GetOffset(_direction);
                    GridCell _neighborCell = _cellResolver(_neighborCoordinate);

                    if (_neighborCell == null || !IsWalkable(_neighborCell, _ignoreOccupants))
                        continue;

                    int _newCost = _currentCost + 1;
                    if (_visitedCost.TryGetValue(_neighborCoordinate, out int _existingCost) && _existingCost <= _newCost)
                        continue;

                    _visitedCost[_neighborCoordinate] = _newCost;
                    _frontier.Enqueue(_neighborCoordinate);
                }
            }

            foreach (Vector2Int _coordinate in _visitedCost.Keys)
            {
                if (_coordinate == _start.Coordinate)
                    continue;

                GridCell _cell = _cellResolver(_coordinate);
                if (_cell != null)
                    _result.Add(_cell);
            }

            return _result;
        }

        private static bool IsWalkable(GridCell _cell, bool _ignoreOccupants)
        {
            if (_cell.IsObstacle)
                return false;

            return _ignoreOccupants || !_cell.IsOccupied;
        }

        private static GridDirection[] GetExplorationOrder(bool _randomize)
        {
            if (!_randomize)
                return GridDirectionUtility.Orthogonal;

            GridDirection[] _shuffled = (GridDirection[])GridDirectionUtility.Orthogonal.Clone();
            for (int _i = _shuffled.Length - 1; _i > 0; _i--)
            {
                int _swapIndex = UnityEngine.Random.Range(0, _i + 1);
                (_shuffled[_i], _shuffled[_swapIndex]) = (_shuffled[_swapIndex], _shuffled[_i]);
            }

            return _shuffled;
        }

        private static Vector2Int GetLowestFScore(List<Vector2Int> _openSet, Dictionary<Vector2Int, int> _fScore, bool _randomize)
        {
            int _lowestScore = _fScore[_openSet[0]];
            for (int _i = 1; _i < _openSet.Count; _i++)
            {
                if (_fScore.TryGetValue(_openSet[_i], out int _score) && _score < _lowestScore)
                    _lowestScore = _score;
            }

            if (!_randomize)
            {
                foreach (Vector2Int _coordinate in _openSet)
                {
                    if (_fScore.TryGetValue(_coordinate, out int _score) && _score == _lowestScore)
                        return _coordinate;
                }
            }

            List<Vector2Int> _candidates = new List<Vector2Int>();
            foreach (Vector2Int _coordinate in _openSet)
            {
                if (_fScore.TryGetValue(_coordinate, out int _score) && _score == _lowestScore)
                    _candidates.Add(_coordinate);
            }

            return _candidates[UnityEngine.Random.Range(0, _candidates.Count)];
        }

        private static List<GridCell> ReconstructPath(Dictionary<Vector2Int, Vector2Int> _cameFrom, Vector2Int _current, Func<Vector2Int, GridCell> _cellResolver)
        {
            List<GridCell> _path = new List<GridCell> { _cellResolver(_current) };
            while (_cameFrom.TryGetValue(_current, out Vector2Int _previous))
            {
                _current = _previous;
                _path.Add(_cellResolver(_current));
            }

            _path.Reverse();
            return _path;
        }

        private static int ManhattanDistance(Vector2Int _a, Vector2Int _b)
        {
            return Mathf.Abs(_a.x - _b.x) + Mathf.Abs(_a.y - _b.y);
        }
    }
}
