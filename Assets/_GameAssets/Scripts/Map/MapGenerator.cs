using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

[Serializable]
public class MapGenerationSettings
{
    [Min(1)] public int width = 7;
    [Min(2)] public int floorCount = 15;
    [Min(1)] public int pathCount = 4;
    [Min(1)] public int treasureFloor = 8;
    [Min(0)] public int firstEliteAndRestFloor = 5;

    [Range(0f, 1f)] public float shopRatio = 0.05f;
    [Range(0f, 1f)] public float restRatio = 0.12f;
    [Range(0f, 1f)] public float eventRatio = 0.22f;
    [Range(0f, 1f)] public float eliteRatio = 0.08f;
}

public static class MapGenerator
{
    public static MapData Generate(MapGenerationSettings _settings, RandomStream _random)
    {
        MapNode[] _nodes = CreateNodes(_settings);

        CreatePaths(_nodes, _settings, _random);
        TrimConvergingStartEdges(_nodes, _settings);
        AssignRoomTypes(_nodes, _settings, _random);
        ConnectBossNode(_nodes, _settings);

        return new MapData(_nodes, _settings.width, _settings.floorCount + 1);
    }

    private static MapNode[] CreateNodes(MapGenerationSettings _settings)
    {
        int _gridNodeCount = _settings.width * _settings.floorCount;
        MapNode[] _nodes = new MapNode[_gridNodeCount + 1];

        for (int _floor = 0; _floor < _settings.floorCount; _floor++)
        {
            for (int _x = 0; _x < _settings.width; _x++)
            {
                int _index = _floor * _settings.width + _x;
                _nodes[_index] = new MapNode(_index, _floor, _x);
            }
        }

        _nodes[_gridNodeCount] = new MapNode(_gridNodeCount, _settings.floorCount, _settings.width / 2);

        return _nodes;
    }

    private static void ConnectBossNode(MapNode[] _nodes, MapGenerationSettings _settings)
    {
        MapNode _bossNode = _nodes[_nodes.Length - 1];
        _bossNode.roomType = RoomType.Boss;

        int _lastFloorStartIndex = (_settings.floorCount - 1) * _settings.width;

        for (int _x = 0; _x < _settings.width; _x++)
        {
            MapNode _node = _nodes[_lastFloorStartIndex + _x];

            if (!_node.HasConnections)
                continue;

            _node.nextNodeIndices.Add(_bossNode.index);
            _bossNode.previousNodeIndices.Add(_node.index);
        }
    }

    private static void CreatePaths(MapNode[] _nodes, MapGenerationSettings _settings, RandomStream _random)
    {
        int _firstPathStartX = -1;

        for (int _path = 0; _path < _settings.pathCount; _path++)
        {
            int _x = _random.Range(0, _settings.width);

            if (_path == 0)
            {
                _firstPathStartX = _x;
            }
            else if (_path == 1 && _settings.width > 1)
            {
                while (_x == _firstPathStartX)
                    _x = _random.Range(0, _settings.width);
            }

            for (int _floor = 0; _floor < _settings.floorCount - 1; _floor++)
                _x = ConnectToNextFloor(_nodes, _settings, _random, _floor, _x);
        }
    }

    private static int ConnectToNextFloor(MapNode[] _nodes, MapGenerationSettings _settings, RandomStream _random, int _floor, int _x)
    {
        List<int> _candidateXs = new List<int>();

        for (int _offset = -1; _offset <= 1; _offset++)
        {
            int _candidateX = _x + _offset;

            if (_candidateX < 0 || _candidateX >= _settings.width)
                continue;

            if (CrossesExistingEdge(_nodes, _settings, _floor, _x, _candidateX))
                continue;

            _candidateXs.Add(_candidateX);
        }

        int _nextX = _random.Pick(_candidateXs);

        Connect(_nodes, _settings, _floor, _x, _nextX);

        return _nextX;
    }

    private static bool CrossesExistingEdge(MapNode[] _nodes, MapGenerationSettings _settings, int _floor, int _x, int _nextX)
    {
        if (_nextX == _x)
            return false;

        MapNode _neighbour = _nodes[_floor * _settings.width + _nextX];

        return _neighbour.nextNodeIndices.Contains((_floor + 1) * _settings.width + _x);
    }

    private static void Connect(MapNode[] _nodes, MapGenerationSettings _settings, int _floor, int _x, int _nextX)
    {
        MapNode _node = _nodes[_floor * _settings.width + _x];
        MapNode _nextNode = _nodes[(_floor + 1) * _settings.width + _nextX];

        if (_node.nextNodeIndices.Contains(_nextNode.index))
            return;

        _node.nextNodeIndices.Add(_nextNode.index);
        _nextNode.previousNodeIndices.Add(_node.index);
    }

    private static void TrimConvergingStartEdges(MapNode[] _nodes, MapGenerationSettings _settings)
    {
        HashSet<int> _reachedNodeIndices = new HashSet<int>();

        for (int _x = 0; _x < _settings.width; _x++)
        {
            MapNode _startNode = _nodes[_x];

            if (_startNode.nextNodeIndices.Count == 0)
                continue;

            for (int _edgeIndex = _startNode.nextNodeIndices.Count - 1; _edgeIndex >= 0; _edgeIndex--)
            {
                int _nextNodeIndex = _startNode.nextNodeIndices[_edgeIndex];

                if (_reachedNodeIndices.Add(_nextNodeIndex))
                    continue;

                _nodes[_nextNodeIndex].previousNodeIndices.Remove(_startNode.index);
                _startNode.nextNodeIndices.RemoveAt(_edgeIndex);
            }
        }
    }

    private static void AssignRoomTypes(MapNode[] _nodes, MapGenerationSettings _settings, RandomStream _random)
    {
        List<MapNode> _connectedNodes = new List<MapNode>();

        foreach (MapNode _node in _nodes)
        {
            if (_node.HasConnections)
                _connectedNodes.Add(_node);
        }

        List<RoomType> _bucket = CreateRoomTypeBucket(_connectedNodes.Count, _settings);

        AssignFixedFloors(_connectedNodes, _settings);

        int _typelessNodeCount = 0;

        foreach (MapNode _node in _connectedNodes)
        {
            if (_node.roomType == null)
                _typelessNodeCount++;
        }

        while (_bucket.Count < _typelessNodeCount)
            _bucket.Add(RoomType.Fight);

        _random.Shuffle(_bucket);

        foreach (MapNode _node in _connectedNodes)
        {
            if (_node.roomType != null)
                continue;

            for (int _bucketIndex = 0; _bucketIndex < _bucket.Count; _bucketIndex++)
            {
                if (BreaksRules(_bucket[_bucketIndex], _node, _nodes, _settings))
                    continue;

                _node.roomType = _bucket[_bucketIndex];
                _bucket.RemoveAt(_bucketIndex);
                break;
            }
        }

        foreach (MapNode _node in _connectedNodes)
        {
            if (_node.roomType == null)
                _node.roomType = RoomType.Fight;
        }
    }

    private static List<RoomType> CreateRoomTypeBucket(int _connectedNodeCount, MapGenerationSettings _settings)
    {
        List<RoomType> _bucket = new List<RoomType>();

        AddToBucket(_bucket, RoomType.Shop, Mathf.RoundToInt(_connectedNodeCount * _settings.shopRatio));
        AddToBucket(_bucket, RoomType.Rest, Mathf.RoundToInt(_connectedNodeCount * _settings.restRatio));
        AddToBucket(_bucket, RoomType.Event, Mathf.RoundToInt(_connectedNodeCount * _settings.eventRatio));
        AddToBucket(_bucket, RoomType.Elite, Mathf.RoundToInt(_connectedNodeCount * _settings.eliteRatio));

        return _bucket;
    }

    private static void AddToBucket(List<RoomType> _bucket, RoomType _roomType, int _count)
    {
        for (int _index = 0; _index < _count; _index++)
            _bucket.Add(_roomType);
    }

    private static void AssignFixedFloors(List<MapNode> _connectedNodes, MapGenerationSettings _settings)
    {
        foreach (MapNode _node in _connectedNodes)
        {
            if (_node.floor == 0)
                _node.roomType = RoomType.Fight;
            else if (_node.floor == _settings.treasureFloor)
                _node.roomType = RoomType.Chest;
            else if (_node.floor == _settings.floorCount - 1)
                _node.roomType = RoomType.Rest;
        }
    }

    private static bool BreaksRules(RoomType _roomType, MapNode _node, MapNode[] _nodes, MapGenerationSettings _settings)
    {
        if ((_roomType == RoomType.Elite || _roomType == RoomType.Rest) && _node.floor < _settings.firstEliteAndRestFloor)
            return true;

        if (_roomType == RoomType.Rest && _node.floor == _settings.floorCount - 2)
            return true;

        foreach (int _parentIndex in _node.previousNodeIndices)
        {
            MapNode _parent = _nodes[_parentIndex];

            if (IsUniqueAlongPath(_roomType) && _parent.roomType == _roomType)
                return true;

            foreach (int _siblingIndex in _parent.nextNodeIndices)
            {
                if (_siblingIndex == _node.index)
                    continue;

                if (_nodes[_siblingIndex].roomType == _roomType)
                    return true;
            }
        }

        return false;
    }

    private static bool IsUniqueAlongPath(RoomType _roomType)
    {
        return _roomType == RoomType.Elite || _roomType == RoomType.Shop || _roomType == RoomType.Rest || _roomType == RoomType.Chest;
    }
}
