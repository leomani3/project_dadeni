using System.Collections.Generic;
using Deckbuilder.Grid;
using NUnit.Framework;
using UnityEngine;

namespace Dadeni.Core.Tests
{
    [TestFixture]
    public class GridLineOfSightTests
    {
        [Test]
        public void OpenGridHasSightEverywhere()
        {
            GridTopology _topology = GridTopology.Rectangle(5, 5);

            Assert.IsTrue(GridLineOfSight.HasLineOfSight(_topology, new Vector2Int(0, 0), new Vector2Int(4, 4), out Vector2Int _blocking));
            Assert.AreEqual(default(Vector2Int), _blocking);
        }

        [Test]
        public void ObstacleBetweenTwoCellsBlocksAndIsReported()
        {
            GridTopology _topology = GridTopology.Parse(@"
...
.#.
...");

            Assert.IsFalse(GridLineOfSight.HasLineOfSight(_topology, new Vector2Int(0, 1), new Vector2Int(2, 1), out Vector2Int _blocking));
            Assert.AreEqual(new Vector2Int(1, 1), _blocking);
        }

        [Test]
        public void OccupantsOnTheOriginAndTargetNeverBlock()
        {
            GridTopology _topology = GridTopology.Parse(@"
ooo");

            Assert.IsTrue(GridLineOfSight.HasLineOfSight(_topology, new Vector2Int(0, 0), new Vector2Int(1, 0), out Vector2Int _blocking));
        }

        [Test]
        public void OccupantBlocksUnlessExplicitlyIgnored()
        {
            GridTopology _topology = GridTopology.Parse(@"
.o..");

            Vector2Int _from = new Vector2Int(0, 0);
            Vector2Int _to = new Vector2Int(3, 0);
            Vector2Int _occupant = new Vector2Int(1, 0);

            Assert.IsFalse(GridLineOfSight.HasLineOfSight(_topology, _from, _to, out Vector2Int _blocking));
            Assert.AreEqual(_occupant, _blocking);

            Assert.IsTrue(GridLineOfSight.HasLineOfSight(_topology, _from, _to, out _blocking, _occupant));
        }

        [Test]
        public void SightIsSymmetric()
        {
            GridTopology _topology = GridTopology.Parse(@"
.....
..#..
.....");

            Vector2Int _a = new Vector2Int(0, 1);
            Vector2Int _b = new Vector2Int(4, 1);

            bool _forward = GridLineOfSight.HasLineOfSight(_topology, _a, _b, out Vector2Int _unused);
            bool _backward = GridLineOfSight.HasLineOfSight(_topology, _b, _a, out _unused);

            Assert.AreEqual(_forward, _backward);
        }

        [Test]
        public void LineCoordinatesStartAtFromAndEndAtTo()
        {
            List<Vector2Int> _line = new List<Vector2Int>(GridLineOfSight.GetLineCoordinates(new Vector2Int(1, 1), new Vector2Int(4, 3)));

            Assert.AreEqual(new Vector2Int(1, 1), _line[0]);
            Assert.AreEqual(new Vector2Int(4, 3), _line[_line.Count - 1]);
        }

        [Test]
        public void LineToSelfIsASingleCoordinate()
        {
            List<Vector2Int> _line = new List<Vector2Int>(GridLineOfSight.GetLineCoordinates(new Vector2Int(2, 2), new Vector2Int(2, 2)));

            Assert.AreEqual(1, _line.Count);
        }

        [Test]
        public void CoordinatesOutsideTheGridHaveNoSight()
        {
            GridTopology _topology = GridTopology.Rectangle(3, 3);

            Assert.IsFalse(GridLineOfSight.HasLineOfSight(_topology, new Vector2Int(0, 0), new Vector2Int(7, 7), out Vector2Int _blocking));
        }
    }
}
