using System.Collections.Generic;

public class MapData
{
    private readonly List<RoomType> _rooms;

    public IReadOnlyList<RoomType> Rooms => _rooms;

    public MapData(List<RoomType> _roomTypes)
    {
        _rooms = _roomTypes;
    }

    public List<DoorDirection> GetNextRoomDirections(int _roomIndex)
    {
        List<DoorDirection> _directions = new List<DoorDirection>();

        if (_roomIndex + 1 < _rooms.Count)
            _directions.Add(DoorDirection.Straight);

        return _directions;
    }

    public bool TryGetNextRoomIndex(int _roomIndex, DoorDirection _direction, out int _nextRoomIndex)
    {
        _nextRoomIndex = _roomIndex + 1;

        return _direction == DoorDirection.Straight && _nextRoomIndex < _rooms.Count;
    }
}
