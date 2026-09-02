using System.Collections.Generic;
using UnityEngine;

namespace Deckbuilder.Grid
{
    public enum GridDirection
    {
        North,
        South,
        East,
        West,
        NorthEast,
        NorthWest,
        SouthEast,
        SouthWest
    }

    public static class GridDirectionUtility
    {
        private static readonly Dictionary<GridDirection, Vector2Int> m_offsets = new()
        {
            { GridDirection.North, new Vector2Int(0, 1) },
            { GridDirection.South, new Vector2Int(0, -1) },
            { GridDirection.East, new Vector2Int(1, 0) },
            { GridDirection.West, new Vector2Int(-1, 0) },
            { GridDirection.NorthEast, new Vector2Int(1, 1) },
            { GridDirection.NorthWest, new Vector2Int(-1, 1) },
            { GridDirection.SouthEast, new Vector2Int(1, -1) },
            { GridDirection.SouthWest, new Vector2Int(-1, -1) },
        };

        public static readonly GridDirection[] Orthogonal =
        {
            GridDirection.North, GridDirection.South, GridDirection.East, GridDirection.West
        };

        public static readonly GridDirection[] Diagonal =
        {
            GridDirection.NorthEast, GridDirection.NorthWest, GridDirection.SouthEast, GridDirection.SouthWest
        };

        public static readonly GridDirection[] All =
        {
            GridDirection.North, GridDirection.South, GridDirection.East, GridDirection.West,
            GridDirection.NorthEast, GridDirection.NorthWest, GridDirection.SouthEast, GridDirection.SouthWest
        };

        public static Vector2Int GetOffset(GridDirection _direction)
        {
            return m_offsets[_direction];
        }
    }
}
