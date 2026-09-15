using System.Collections.Generic;
using UnityEngine;

public class MapData
{
    private List<RoomType> _rooms;

    public List<RoomType> Rooms => _rooms;

    public MapData(List<RoomType> rooms)
    {
        _rooms = rooms;
    }
}