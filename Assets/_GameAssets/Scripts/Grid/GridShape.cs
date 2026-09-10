using System.Collections.Generic;
using UnityEngine;

namespace Deckbuilder.Grid
{
    public enum GridShape
    {
        Single,
        Cross,
        Diamond,
        Square
    }

    public static class GridShapeUtility
    {
        public static IEnumerable<Vector2Int> GetCoordinates(Vector2Int _origin, GridShape _shape, int _size, int _minSize = 0)
        {
            int _radius = Mathf.Max(_size - 1, 0);

            foreach (Vector2Int _offset in GetOffsets(_shape, _radius))
            {
                if (_minSize > 0 && ManhattanDistance(Vector2Int.zero, _offset) < _minSize)
                    continue;

                yield return _origin + _offset;
            }
        }

        public static IEnumerable<Vector2Int> GetOffsets(GridShape _shape, int _size)
        {
            switch (_shape)
            {
                case GridShape.Single:
                    yield return Vector2Int.zero;
                    break;

                case GridShape.Cross:
                    foreach (Vector2Int _offset in GetCross(_size))
                        yield return _offset;
                    break;

                case GridShape.Diamond:
                    foreach (Vector2Int _offset in GetDiamond(_size))
                        yield return _offset;
                    break;

                case GridShape.Square:
                    foreach (Vector2Int _offset in GetSquare(_size))
                        yield return _offset;
                    break;
            }
        }

        public static int ManhattanDistance(Vector2Int _a, Vector2Int _b)
        {
            return Mathf.Abs(_a.x - _b.x) + Mathf.Abs(_a.y - _b.y);
        }

        private static IEnumerable<Vector2Int> GetCross(int _size)
        {
            for (int _offset = -_size; _offset <= _size; _offset++)
            {
                yield return new Vector2Int(_offset, 0);

                if (_offset != 0)
                    yield return new Vector2Int(0, _offset);
            }
        }

        private static IEnumerable<Vector2Int> GetDiamond(int _size)
        {
            for (int _x = -_size; _x <= _size; _x++)
            {
                int _remaining = _size - Mathf.Abs(_x);
                for (int _y = -_remaining; _y <= _remaining; _y++)
                    yield return new Vector2Int(_x, _y);
            }
        }

        private static IEnumerable<Vector2Int> GetSquare(int _size)
        {
            for (int _x = -_size; _x <= _size; _x++)
                for (int _y = -_size; _y <= _size; _y++)
                    yield return new Vector2Int(_x, _y);
        }
    }
}
