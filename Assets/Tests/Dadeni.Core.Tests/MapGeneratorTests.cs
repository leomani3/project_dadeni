using System.Collections.Generic;
using NUnit.Framework;
using Utils;

namespace Dadeni.Core.Tests
{
    [TestFixture]
    public class MapGeneratorTests
    {
        private static MapGenerationSettings DefaultSettings()
        {
            return new MapGenerationSettings
            {
                width = 7,
                floorCount = 15,
                pathCount = 6,
                treasureFloor = 8,
                firstEliteAndRestFloor = 5
            };
        }

        private static MapData Generate(int _seed, MapGenerationSettings _settings = null)
        {
            return MapGenerator.Generate(_settings ?? DefaultSettings(), new RandomStream(_seed));
        }

        [Test]
        public void SameSeedProducesAnIdenticalMap()
        {
            Assert.AreEqual(Generate(777).ToDebugString(), Generate(777).ToDebugString());
        }

        [Test]
        public void DifferentSeedsProduceDifferentMaps()
        {
            Assert.AreNotEqual(Generate(1).ToDebugString(), Generate(2).ToDebugString());
        }

        [Test]
        public void GridHasOneNodePerColumnPerFloorPlusTheBoss()
        {
            MapGenerationSettings _settings = DefaultSettings();
            MapData _map = Generate(5, _settings);

            Assert.AreEqual(_settings.width * _settings.floorCount + 1, _map.Nodes.Count);
            Assert.AreEqual(_settings.floorCount + 1, _map.FloorCount);
        }

        [Test]
        public void AtLeastOneStartNodeExists()
        {
            for (int _seed = 0; _seed < 20; _seed++)
                Assert.Greater(Generate(_seed).StartNodeIndices.Count, 0, $"Seed {_seed} produced no entry point.");
        }

        [Test]
        public void EveryStartNodeSitsOnTheFirstFloor()
        {
            MapData _map = Generate(42);

            foreach (int _startIndex in _map.StartNodeIndices)
                Assert.AreEqual(0, _map.GetNode(_startIndex).floor);
        }

        [Test]
        public void EveryConnectedNodeIsReachableFromAStartNode()
        {
            for (int _seed = 0; _seed < 20; _seed++)
            {
                MapData _map = Generate(_seed);
                HashSet<int> _reached = new HashSet<int>();
                Queue<int> _frontier = new Queue<int>();

                foreach (int _startIndex in _map.StartNodeIndices)
                {
                    if (_reached.Add(_startIndex))
                        _frontier.Enqueue(_startIndex);
                }

                while (_frontier.Count > 0)
                {
                    MapNode _node = _map.GetNode(_frontier.Dequeue());

                    foreach (int _nextIndex in _node.nextNodeIndices)
                    {
                        if (_reached.Add(_nextIndex))
                            _frontier.Enqueue(_nextIndex);
                    }
                }

                foreach (MapNode _node in _map.Nodes)
                {
                    if (_node != null && _node.HasConnections)
                        Assert.IsTrue(_reached.Contains(_node.index), $"Seed {_seed}: node {_node.index} is connected but unreachable.");
                }
            }
        }

        [Test]
        public void EveryEdgeClimbsExactlyOneFloor()
        {
            MapData _map = Generate(11);

            foreach (MapNode _node in _map.Nodes)
            {
                if (_node == null)
                    continue;

                foreach (int _nextIndex in _node.nextNodeIndices)
                    Assert.AreEqual(_node.floor + 1, _map.GetNode(_nextIndex).floor, $"Edge {_node.index} -> {_nextIndex} skips a floor.");
            }
        }

        [Test]
        public void EveryEdgeSpansAtMostOneColumn()
        {
            MapData _map = Generate(13);

            foreach (MapNode _node in _map.Nodes)
            {
                if (_node == null)
                    continue;

                foreach (int _nextIndex in _node.nextNodeIndices)
                {
                    MapNode _next = _map.GetNode(_nextIndex);

                    if (_next.roomType == RoomType.Boss)
                        continue;

                    Assert.LessOrEqual(System.Math.Abs(_next.x - _node.x), 1, $"Edge {_node.index} -> {_nextIndex} spans more than one column.");
                }
            }
        }

        [Test]
        public void ConnectionsAreSymmetric()
        {
            MapData _map = Generate(17);

            foreach (MapNode _node in _map.Nodes)
            {
                if (_node == null)
                    continue;

                foreach (int _nextIndex in _node.nextNodeIndices)
                    Assert.Contains(_node.index, _map.GetNode(_nextIndex).previousNodeIndices, $"Edge {_node.index} -> {_nextIndex} has no matching back edge.");
            }
        }

        [Test]
        public void ExactlyOneBossNodeExistsAndItIsLast()
        {
            MapData _map = Generate(23);

            int _bossCount = 0;

            foreach (MapNode _node in _map.Nodes)
            {
                if (_node != null && _node.roomType == RoomType.Boss)
                {
                    _bossCount++;
                    Assert.AreEqual(0, _node.nextNodeIndices.Count, "The boss node should lead nowhere.");
                    Assert.Greater(_node.previousNodeIndices.Count, 0, "The boss node should be reachable.");
                }
            }

            Assert.AreEqual(1, _bossCount);
        }

        [Test]
        public void EveryConnectedNodeHasARoomType()
        {
            for (int _seed = 0; _seed < 20; _seed++)
            {
                MapData _map = Generate(_seed);

                foreach (MapNode _node in _map.Nodes)
                {
                    if (_node != null && _node.HasConnections)
                        Assert.IsTrue(_node.roomType.HasValue, $"Seed {_seed}: node {_node.index} has no room type.");
                }
            }
        }

        [Test]
        public void FirstFloorIsAlwaysAFight()
        {
            for (int _seed = 0; _seed < 20; _seed++)
            {
                MapData _map = Generate(_seed);

                foreach (int _startIndex in _map.StartNodeIndices)
                    Assert.AreEqual(RoomType.Fight, _map.GetRoomType(_startIndex), $"Seed {_seed}: entry node {_startIndex} is not a fight.");
            }
        }

        [Test]
        public void NextRoomDirectionsMatchTheGraphEdges()
        {
            MapData _map = Generate(31);

            foreach (MapNode _node in _map.Nodes)
            {
                if (_node == null || _node.nextNodeIndices.Count == 0)
                    continue;

                List<DoorDirection> _directions = _map.GetNextRoomDirections(_node.index);

                Assert.AreEqual(_node.nextNodeIndices.Count, _directions.Count, $"Node {_node.index} exposes the wrong number of doors.");

                foreach (DoorDirection _direction in _directions)
                {
                    Assert.IsTrue(_map.TryGetNextRoomIndex(_node.index, _direction, out int _nextIndex), $"Node {_node.index} door {_direction} leads nowhere.");
                    Assert.Contains(_nextIndex, _node.nextNodeIndices);
                }
            }
        }

        [Test]
        public void RestAndEliteRoomsStayOffTheEarlyFloors()
        {
            MapGenerationSettings _settings = DefaultSettings();

            for (int _seed = 0; _seed < 20; _seed++)
            {
                MapData _map = Generate(_seed, _settings);

                foreach (MapNode _node in _map.Nodes)
                {
                    if (_node == null || !_node.roomType.HasValue)
                        continue;

                    bool _isGated = _node.roomType == RoomType.Rest || _node.roomType == RoomType.Elite;

                    if (_isGated)
                        Assert.GreaterOrEqual(_node.floor, _settings.firstEliteAndRestFloor, $"Seed {_seed}: {_node.roomType} on floor {_node.floor}.");
                }
            }
        }

        [Test]
        public void DebugStringKeepsEveryFloorOnTheSameColumnGrid()
        {
            MapGenerationSettings _settings = DefaultSettings();
            string[] _lines = Generate(99, _settings).ToDebugString().Replace("\r\n", "\n").Split('\n');

            List<int> _columns = new List<int>();
            for (int _i = 0; _i < _lines[0].Length; _i++)
            {
                if (_lines[0][_i] != ' ')
                    _columns.Add(_i);
            }

            Assert.AreEqual(_settings.width, _columns.Count, "The header row does not expose one column per map column.");

            for (int _i = 1; _i < _columns.Count; _i++)
                Assert.AreEqual(_columns[1] - _columns[0], _columns[_i] - _columns[_i - 1], "Columns are not evenly spaced.");

            foreach (int _column in _columns)
            {
                for (int _row = 1; _row < _lines.Length - 1; _row++)
                {
                    string _line = _lines[_row];

                    if (_column >= _line.Length)
                        continue;

                    Assert.AreNotEqual('\t', _line[_column], "Column grid was built with tabs, which will not align.");
                }
            }

            Assert.IsTrue(_lines[_lines.Length - 2].Contains("Boss"), "The legend row is missing.");
        }
    }
}
