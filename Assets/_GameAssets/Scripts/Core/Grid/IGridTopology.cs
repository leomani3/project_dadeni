using UnityEngine;

namespace Deckbuilder.Grid
{
    public interface IGridTopology
    {
        bool Contains(Vector2Int _coordinate);

        CellFlags GetFlags(Vector2Int _coordinate);
    }
}
