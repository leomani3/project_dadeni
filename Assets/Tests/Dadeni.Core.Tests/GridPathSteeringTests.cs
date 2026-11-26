using System.Collections.Generic;
using Deckbuilder.Grid;
using NUnit.Framework;
using UnityEngine;
using Utils;

namespace Dadeni.Core.Tests
{
    [TestFixture]
    public class GridPathSteeringTests
    {
        private static readonly Vector2Int Origin = new Vector2Int(0, 0);

        [Test]
        public void HoveringNeighborsOneByOneFollowsTheHoveredCells()
        {
            GridTopology _topology = GridTopology.Rectangle(5, 5);
            GridPathSteering _steering = new GridPathSteering();

            Steer(_steering, _topology, new Vector2Int(1, 0));
            Steer(_steering, _topology, new Vector2Int(2, 0));
            Steer(_steering, _topology, new Vector2Int(2, 1));
            Steer(_steering, _topology, new Vector2Int(2, 2));

            CollectionAssert.AreEqual(
                new[]
                {
                    Origin,
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0),
                    new Vector2Int(2, 1),
                    new Vector2Int(2, 2)
                },
                _steering.Path);
        }

        [Test]
        public void TheOppositeSweepReachesTheSameCellByTheOtherSide()
        {
            GridTopology _topology = GridTopology.Rectangle(5, 5);
            GridPathSteering _steering = new GridPathSteering();

            Steer(_steering, _topology, new Vector2Int(0, 1));
            Steer(_steering, _topology, new Vector2Int(0, 2));
            Steer(_steering, _topology, new Vector2Int(1, 2));
            Steer(_steering, _topology, new Vector2Int(2, 2));

            CollectionAssert.AreEqual(
                new[]
                {
                    Origin,
                    new Vector2Int(0, 1),
                    new Vector2Int(0, 2),
                    new Vector2Int(1, 2),
                    new Vector2Int(2, 2)
                },
                _steering.Path);
        }

        [Test]
        public void SteeredPathAlwaysKeepsTheShortestLength()
        {
            GridTopology _topology = GridTopology.Rectangle(5, 5);
            GridPathSteering _steering = new GridPathSteering();

            Vector2Int[] _hovered =
            {
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new Vector2Int(2, 1),
                new Vector2Int(3, 1),
                new Vector2Int(3, 2)
            };

            foreach (Vector2Int _cell in _hovered)
            {
                Steer(_steering, _topology, _cell);
                Assert.AreEqual(GridShapeUtility.ManhattanDistance(Origin, _cell), _steering.StepCount, $"Steering to {_cell} stopped being a shortest path.");
            }
        }

        [Test]
        public void HoveringAwayFromTheSteeredPathFallsBackToTheShortestPath()
        {
            GridTopology _topology = GridTopology.Rectangle(5, 5);
            GridPathSteering _steering = new GridPathSteering();

            Steer(_steering, _topology, new Vector2Int(1, 0));
            Steer(_steering, _topology, new Vector2Int(2, 0));
            Steer(_steering, _topology, new Vector2Int(2, 1));
            Steer(_steering, _topology, new Vector2Int(2, 2));

            Steer(_steering, _topology, new Vector2Int(0, 2));

            Assert.AreEqual(2, _steering.StepCount);
            Assert.AreEqual(Origin, _steering.Path[0]);
            Assert.AreEqual(new Vector2Int(0, 2), _steering.Path[^1]);
        }

        [Test]
        public void HoveringBackOntoTheSteeredPathTrimsIt()
        {
            GridTopology _topology = GridTopology.Rectangle(5, 5);
            GridPathSteering _steering = new GridPathSteering();

            Steer(_steering, _topology, new Vector2Int(1, 0));
            Steer(_steering, _topology, new Vector2Int(2, 0));
            Steer(_steering, _topology, new Vector2Int(2, 1));
            Steer(_steering, _topology, new Vector2Int(2, 2));

            Steer(_steering, _topology, new Vector2Int(2, 0));

            CollectionAssert.AreEqual(
                new[]
                {
                    Origin,
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0)
                },
                _steering.Path);
        }

        [Test]
        public void SteeringAroundAnObstacleKeepsTheDetourTheHoverTook()
        {
            GridTopology _topology = GridTopology.Parse(@"
.....
.###.
.....");

            GridPathSteering _steering = new GridPathSteering();
            Vector2Int _origin = new Vector2Int(2, 0);

            _steering.TrySteerTo(_topology, _origin, new Vector2Int(3, 0), 10);
            _steering.TrySteerTo(_topology, _origin, new Vector2Int(4, 0), 10);
            _steering.TrySteerTo(_topology, _origin, new Vector2Int(4, 1), 10);
            _steering.TrySteerTo(_topology, _origin, new Vector2Int(4, 2), 10);
            _steering.TrySteerTo(_topology, _origin, new Vector2Int(3, 2), 10);
            _steering.TrySteerTo(_topology, _origin, new Vector2Int(2, 2), 10);

            CollectionAssert.AreEqual(
                new[]
                {
                    new Vector2Int(2, 0),
                    new Vector2Int(3, 0),
                    new Vector2Int(4, 0),
                    new Vector2Int(4, 1),
                    new Vector2Int(4, 2),
                    new Vector2Int(3, 2),
                    new Vector2Int(2, 2)
                },
                _steering.Path);
        }

        [Test]
        public void ADestinationBeyondTheStepBudgetIsRefused()
        {
            GridTopology _topology = GridTopology.Rectangle(5, 5);
            GridPathSteering _steering = new GridPathSteering();

            Assert.IsFalse(_steering.TrySteerTo(_topology, Origin, new Vector2Int(4, 0), 3));
            CollectionAssert.AreEqual(new[] { Origin }, _steering.Path);
        }

        [Test]
        public void AnUnreachableDestinationIsRefused()
        {
            GridTopology _topology = GridTopology.Parse(@"
.#.
.#.
.#.");

            GridPathSteering _steering = new GridPathSteering();

            Assert.IsFalse(_steering.TrySteerTo(_topology, Origin, new Vector2Int(2, 0), 10));
            CollectionAssert.AreEqual(new[] { Origin }, _steering.Path);
        }

        [Test]
        public void MovingTheOriginRestartsTheSteering()
        {
            GridTopology _topology = GridTopology.Rectangle(5, 5);
            GridPathSteering _steering = new GridPathSteering();

            Steer(_steering, _topology, new Vector2Int(1, 0));
            Steer(_steering, _topology, new Vector2Int(2, 0));

            _steering.TrySteerTo(_topology, new Vector2Int(4, 4), new Vector2Int(4, 3), 10);

            CollectionAssert.AreEqual(
                new[]
                {
                    new Vector2Int(4, 4),
                    new Vector2Int(4, 3)
                },
                _steering.Path);
        }

        [Test]
        public void EverySteeredStepIsOrthogonalAndAdjacent()
        {
            GridTopology _topology = GridTopology.Rectangle(6, 6);
            GridPathSteering _steering = new GridPathSteering();

            Vector2Int[] _hovered =
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 1),
                new Vector2Int(1, 2),
                new Vector2Int(5, 5),
                new Vector2Int(4, 5)
            };

            foreach (Vector2Int _cell in _hovered)
            {
                _steering.TrySteerTo(_topology, Origin, _cell, 20);

                IReadOnlyList<Vector2Int> _path = _steering.Path;

                for (int _i = 1; _i < _path.Count; _i++)
                    Assert.AreEqual(1, GridShapeUtility.ManhattanDistance(_path[_i - 1], _path[_i]), $"Step {_i} jumped from {_path[_i - 1]} to {_path[_i]}.");
            }
        }

        private static void Steer(GridPathSteering _steering, IGridTopology _topology, Vector2Int _destination)
        {
            Assert.IsTrue(_steering.TrySteerTo(_topology, Origin, _destination, 20), $"Steering to {_destination} was refused.");
        }
    }
}
