using System;
using System.Collections.Generic;
using System.Text;

public class MapNode
{
    public readonly int index;
    public readonly int floor;
    public readonly int x;
    public readonly List<int> nextNodeIndices = new List<int>();
    public readonly List<int> previousNodeIndices = new List<int>();

    public RoomType? roomType;

    public MapNode(int _index, int _floor, int _x)
    {
        index = _index;
        floor = _floor;
        x = _x;
    }

    public bool HasConnections => nextNodeIndices.Count > 0 || previousNodeIndices.Count > 0;
}

public class MapData
{
    private const int ColumnSpacing = 4;
    private const int LabelWidth = 4;

    private readonly MapNode[] _nodes;
    private readonly List<int> _startNodeIndices = new List<int>();

    public IReadOnlyList<MapNode> Nodes => _nodes;
    public IReadOnlyList<int> StartNodeIndices => _startNodeIndices;
    public int Width { get; }
    public int FloorCount { get; }

    public MapData(MapNode[] _mapNodes, int _width, int _floorCount)
    {
        _nodes = _mapNodes;
        Width = _width;
        FloorCount = _floorCount;

        for (int _nodeIndex = 0; _nodeIndex < Width; _nodeIndex++)
        {
            if (_nodes[_nodeIndex].nextNodeIndices.Count > 0)
                _startNodeIndices.Add(_nodeIndex);
        }
    }

    public MapNode GetNode(int _nodeIndex)
    {
        return _nodes[_nodeIndex];
    }

    public RoomType GetRoomType(int _nodeIndex)
    {
        return _nodes[_nodeIndex].roomType.Value;
    }

    public List<DoorDirection> GetNextRoomDirections(int _nodeIndex)
    {
        List<DoorDirection> _directions = new List<DoorDirection>();
        MapNode _node = _nodes[_nodeIndex];

        foreach (int _nextNodeIndex in _node.nextNodeIndices)
            _directions.Add(GetDirection(_node, _nodes[_nextNodeIndex]));

        return _directions;
    }

    public bool TryGetNextRoomIndex(int _nodeIndex, DoorDirection _direction, out int _nextNodeIndex)
    {
        MapNode _node = _nodes[_nodeIndex];

        foreach (int _candidateIndex in _node.nextNodeIndices)
        {
            if (GetDirection(_node, _nodes[_candidateIndex]) != _direction)
                continue;

            _nextNodeIndex = _candidateIndex;
            return true;
        }

        _nextNodeIndex = -1;
        return false;
    }

    public string ToDebugString()
    {
        StringBuilder _builder = new StringBuilder();

        _builder.AppendLine(BuildColumnHeaderRow());

        for (int _floor = FloorCount - 1; _floor >= 0; _floor--)
        {
            _builder.AppendLine(BuildNodeRow(_floor));

            if (_floor > 0)
                _builder.AppendLine(BuildEdgeRow(_floor - 1));
        }

        _builder.AppendLine(BuildLegendRow());

        return _builder.ToString();
    }

    private string BuildColumnHeaderRow()
    {
        char[] _row = CreateEmptyRow();

        for (int _x = 0; _x < Width; _x++)
            _row[GetColumn(_x)] = (char)('0' + _x % 10);

        return new string(_row).TrimEnd();
    }

    private string BuildNodeRow(int _floor)
    {
        char[] _row = CreateEmptyRow();

        WriteLabel(_row, _floor.ToString("00"));

        foreach (MapNode _node in _nodes)
        {
            if (_node.floor != _floor)
                continue;

            _row[GetColumn(_node.x)] = _node.HasConnections ? GetRoomTypeSymbol(_node.roomType) : '.';
        }

        return new string(_row).TrimEnd();
    }

    private string BuildEdgeRow(int _fromFloor)
    {
        char[] _row = CreateEmptyRow();

        foreach (MapNode _node in _nodes)
        {
            if (_node.floor != _fromFloor)
                continue;

            foreach (int _nextNodeIndex in _node.nextNodeIndices)
            {
                int _step = Math.Sign(_nodes[_nextNodeIndex].x - _node.x);
                _row[GetColumn(_node.x) + _step * (ColumnSpacing / 2)] = GetEdgeSymbol(_step);
            }
        }

        return new string(_row).TrimEnd();
    }

    private string BuildLegendRow()
    {
        Dictionary<RoomType, int> _countByRoomType = new Dictionary<RoomType, int>();

        foreach (MapNode _node in _nodes)
        {
            if (!_node.HasConnections)
                continue;

            _countByRoomType.TryGetValue(_node.roomType.Value, out int _count);
            _countByRoomType[_node.roomType.Value] = _count + 1;
        }

        StringBuilder _builder = new StringBuilder();

        foreach (KeyValuePair<RoomType, int> _entry in _countByRoomType)
            _builder.Append($"{GetRoomTypeSymbol(_entry.Key)} {_entry.Key}:{_entry.Value}   ");

        return _builder.ToString().TrimEnd();
    }

    private char[] CreateEmptyRow()
    {
        char[] _row = new char[LabelWidth + (Width - 1) * ColumnSpacing + 1];

        for (int _column = 0; _column < _row.Length; _column++)
            _row[_column] = ' ';

        return _row;
    }

    private void WriteLabel(char[] _row, string _label)
    {
        for (int _index = 0; _index < _label.Length; _index++)
            _row[_index] = _label[_index];
    }

    private int GetColumn(int _x)
    {
        return LabelWidth + _x * ColumnSpacing;
    }

    private static char GetEdgeSymbol(int _step)
    {
        if (_step > 0)
            return '/';

        if (_step < 0)
            return '\\';

        return '|';
    }

    private static char GetRoomTypeSymbol(RoomType? _roomType)
    {
        switch (_roomType)
        {
            case RoomType.Fight: return 'M';
            case RoomType.Elite: return 'E';
            case RoomType.Shop: return '$';
            case RoomType.Chest: return 'T';
            case RoomType.Event: return '?';
            case RoomType.Rest: return 'R';
            case RoomType.Boss: return 'B';
            default: return '_';
        }
    }

    private static DoorDirection GetDirection(MapNode _from, MapNode _to)
    {
        if (_to.x < _from.x)
            return DoorDirection.Left;

        if (_to.x > _from.x)
            return DoorDirection.Right;

        return DoorDirection.Straight;
    }
}
