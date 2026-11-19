using System.Collections.Generic;
using UnityEngine;

namespace Deckbuilder.Grid
{
    public static class GridLineOfSight
    {
        public static bool HasLineOfSight(IGridTopology _topology, Vector2Int _from, Vector2Int _to, out Vector2Int _blockingCoordinate, Vector2Int? _ignoreOccupantAt = null)
        {
            _blockingCoordinate = default;

            if (!_topology.Contains(_from) || !_topology.Contains(_to))
                return false;

            foreach (Vector2Int _coordinate in GetLineCoordinates(_from, _to))
            {
                if (_coordinate == _from || _coordinate == _to)
                    continue;

                if (!_topology.Contains(_coordinate))
                    continue;

                CellFlags _flags = _topology.GetFlags(_coordinate);

                bool _isObstacle = (_flags & CellFlags.Obstacle) != 0;
                bool _isBlockingOccupant = (_flags & CellFlags.Occupied) != 0 && _coordinate != _ignoreOccupantAt;

                if (_isObstacle || _isBlockingOccupant)
                {
                    _blockingCoordinate = _coordinate;
                    return false;
                }
            }

            return true;
        }

        public static IEnumerable<Vector2Int> GetLineCoordinates(Vector2Int _from, Vector2Int _to)
        {
            int _x = _from.x;
            int _y = _from.y;

            int _dx = Mathf.Abs(_to.x - _x);
            int _dy = Mathf.Abs(_to.y - _y);
            int _signX = _to.x > _x ? 1 : -1;
            int _signY = _to.y > _y ? 1 : -1;

            int _error = _dx - _dy;

            while (true)
            {
                yield return new Vector2Int(_x, _y);

                if (_x == _to.x && _y == _to.y)
                    break;

                int _doubledError = _error * 2;

                if (_doubledError > -_dy)
                {
                    _error -= _dy;
                    _x += _signX;
                }

                if (_doubledError < _dx)
                {
                    _error += _dx;
                    _y += _signY;
                }
            }
        }
    }
}
