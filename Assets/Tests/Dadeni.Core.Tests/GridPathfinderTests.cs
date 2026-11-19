using System.Collections.Generic;
using Deckbuilder.Grid;
using NUnit.Framework;
using UnityEngine;
using Utils;

namespace Dadeni.Core.Tests
{
    [TestFixture]
    public class GridPathfinderTests
    {
        private readonly List<Vector2Int> m_path = new List<Vector2Int>();

        [Test]
        public void StraightLineOnOpenGridCostsManhattanDistance()
        {
            GridTopology _topology = GridTopology.Rectangle(5, 5);

            Assert.IsTrue(GridPathfinder.TryFindPath(_topology, new Vector2Int(0, 0), new Vector2Int(4, 0), m_path));
            Assert.AreEqual(5, m_path.Count);
            Assert.AreEqual(new Vector2Int(0, 0), m_path[0]);
            Assert.AreEqual(new Vector2Int(4, 0), m_path[m_path.Count - 1]);
        }

        [Test]
        public void PathStepsAreAlwaysOrthogonalAndAdjacent()
        {
            GridTopology _topology = GridTopology.Parse(@"
.....
.###.
.....
.###.
.....");

            Assert.IsTrue(GridPathfinder.TryFindPath(_topology, new Vector2Int(0, 4), new Vector2Int(4, 0), m_path));

            for (int _i = 1; _i < m_path.Count; _i++)
            {
                int _distance = GridShapeUtility.ManhattanDistance(m_path[_i - 1], m_path[_i]);
                Assert.AreEqual(1, _distance, $"Step {_i} jumped from {m_path[_i - 1]} to {m_path[_i]}.");
            }
        }

        [Test]
        public void PathNeverCrossesAnObstacle()
        {
            GridTopology _topology = GridTopology.Parse(@"
.....
.###.
.....");

            Assert.IsTrue(GridPathfinder.TryFindPath(_topology, new Vector2Int(2, 0), new Vector2Int(2, 2), m_path));

            foreach (Vector2Int _coordinate in m_path)
                Assert.AreNotEqual(CellFlags.Obstacle, _topology.GetFlags(_coordinate), $"Path walked through obstacle at {_coordinate}.");
        }

        [Test]
        public void FullyWalledGoalIsUnreachable()
        {
            GridTopology _topology = GridTopology.Parse(@"
.....
.###.
.#.#.
.###.
.....");

            Assert.IsFalse(GridPathfinder.TryFindPath(_topology, new Vector2Int(0, 0), new Vector2Int(2, 2), m_path));
            Assert.AreEqual(0, m_path.Count);
        }

        [Test]
        public void OccupiedCellsBlockUnlessIgnored()
        {
            GridTopology _topology = GridTopology.Parse(@"
.o.
.o.
.o.");

            Vector2Int _start = new Vector2Int(0, 1);
            Vector2Int _goal = new Vector2Int(2, 1);

            Assert.IsFalse(GridPathfinder.TryFindPath(_topology, _start, _goal, m_path));
            Assert.IsTrue(GridPathfinder.TryFindPath(_topology, _start, _goal, m_path, true));
        }

        [Test]
        public void SameStartAndGoalReturnsSingleStepPath()
        {
            GridTopology _topology = GridTopology.Rectangle(3, 3);

            Assert.IsTrue(GridPathfinder.TryFindPath(_topology, new Vector2Int(1, 1), new Vector2Int(1, 1), m_path));
            Assert.AreEqual(1, m_path.Count);
        }

        [Test]
        public void CoordinatesOutsideTheGridAreRejected()
        {
            GridTopology _topology = GridTopology.Rectangle(3, 3);

            Assert.IsFalse(GridPathfinder.TryFindPath(_topology, new Vector2Int(0, 0), new Vector2Int(9, 9), m_path));
            Assert.IsFalse(GridPathfinder.TryFindPath(_topology, new Vector2Int(-1, 0), new Vector2Int(1, 1), m_path));
        }

        [Test]
        public void ReachableCellsFormADiamondAndExcludeTheOrigin()
        {
            GridTopology _topology = GridTopology.Rectangle(9, 9);
            List<Vector2Int> _results = new List<Vector2Int>();
            Vector2Int _start = new Vector2Int(4, 4);

            GridPathfinder.GetReachableCoordinates(_topology, _start, 2, _results);

            Assert.AreEqual(12, _results.Count);
            Assert.IsFalse(_results.Contains(_start));

            foreach (Vector2Int _coordinate in _results)
                Assert.LessOrEqual(GridShapeUtility.ManhattanDistance(_start, _coordinate), 2);
        }

        [Test]
        public void ReachableCellsRespectWallsRatherThanRawDistance()
        {
            GridTopology _topology = GridTopology.Parse(@"
..#..
..#..
..#..");

            List<Vector2Int> _results = new List<Vector2Int>();
            GridPathfinder.GetReachableCoordinates(_topology, new Vector2Int(0, 1), 10, _results);

            foreach (Vector2Int _coordinate in _results)
                Assert.Less(_coordinate.x, 2, $"Reached {_coordinate} through the wall at x = 2.");
        }

        [Test]
        public void ZeroStepsReachesNothing()
        {
            GridTopology _topology = GridTopology.Rectangle(5, 5);
            List<Vector2Int> _results = new List<Vector2Int>();

            GridPathfinder.GetReachableCoordinates(_topology, new Vector2Int(2, 2), 0, _results);

            Assert.AreEqual(0, _results.Count);
        }

        [Test]
        public void SameSeedProducesTheSameRandomizedPath()
        {
            GridTopology _topology = GridTopology.Rectangle(6, 6);
            Vector2Int _start = new Vector2Int(0, 0);
            Vector2Int _goal = new Vector2Int(5, 5);

            List<Vector2Int> _first = new List<Vector2Int>();
            List<Vector2Int> _second = new List<Vector2Int>();

            GridPathfinder.TryFindPath(_topology, _start, _goal, _first, false, new RandomStream(1234));
            GridPathfinder.TryFindPath(_topology, _start, _goal, _second, false, new RandomStream(1234));

            Assert.AreEqual(_first, _second);
        }

        [Test]
        public void RandomizedPathIsStillAValidShortestPath()
        {
            GridTopology _topology = GridTopology.Rectangle(6, 6);
            Vector2Int _start = new Vector2Int(0, 0);
            Vector2Int _goal = new Vector2Int(5, 5);

            for (int _seed = 0; _seed < 25; _seed++)
            {
                Assert.IsTrue(GridPathfinder.TryFindPath(_topology, _start, _goal, m_path, false, new RandomStream(_seed)));
                Assert.AreEqual(GridShapeUtility.ManhattanDistance(_start, _goal) + 1, m_path.Count, $"Seed {_seed} produced a detour.");
            }
        }
    }
}
