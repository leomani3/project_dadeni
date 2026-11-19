using System;

namespace Deckbuilder.Grid
{
    [Flags]
    public enum CellFlags
    {
        None = 0,
        Obstacle = 1 << 0,
        Occupied = 1 << 1
    }
}
