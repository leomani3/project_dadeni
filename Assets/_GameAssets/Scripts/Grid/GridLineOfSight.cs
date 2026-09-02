using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deckbuilder.Grid
{
    public static class GridLineOfSight
    {
        public static bool HasLineOfSight(GridCell _from, GridCell _to, Func<Vector2Int, GridCell> _cellResolver, out GridCell _blockingCell, Entity _ignoreOccupant = null)
        {
            _blockingCell = null;

            if (_from == null || _to == null)
                return false;

            foreach (Vector2Int _coordinate in GetLineCoordinates(_from.Coordinate, _to.Coordinate))
            {
                if (_coordinate == _from.Coordinate || _coordinate == _to.Coordinate)
                    continue;

                GridCell _cell = _cellResolver(_coordinate);
                if (_cell == null)
                    continue;

                bool _blockedByOccupant = _cell.IsOccupied && _cell.Occupant != _ignoreOccupant;
                if (_cell.IsObstacle || _blockedByOccupant)
                {
                    _blockingCell = _cell;
                    return false;
                }
            }

            return true;
        }

        public static IEnumerable<Vector2Int> GetLineCoordinates(Vector2Int _from, Vector2Int _to)
        {
            int _x0 = _from.x, _y0 = _from.y;
            int _x1 = _to.x, _y1 = _to.y;

            int _dx = Mathf.Abs(_x1 - _x0);
            int _dy = Mathf.Abs(_y1 - _y0);
            int _signX = _x1 > _x0 ? 1 : -1;
            int _signY = _y1 > _y0 ? 1 : -1;

            int _x = _x0, _y = _y0;
            int _error = _dx - _dy;

            while (true)
            {
                yield return new Vector2Int(_x, _y);

                if (_x == _x1 && _y == _y1)
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
