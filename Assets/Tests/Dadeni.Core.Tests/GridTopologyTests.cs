using Deckbuilder.Grid;
using NUnit.Framework;
using UnityEngine;

namespace Dadeni.Core.Tests
{
    [TestFixture]
    public class GridTopologyTests
    {
        [Test]
        public void ParseReadsTheFirstLineAsTheHighestRow()
        {
            GridTopology _topology = GridTopology.Parse(@"
#..
...
..o");

            Assert.AreEqual(CellFlags.Obstacle, _topology.GetFlags(new Vector2Int(0, 2)));
            Assert.AreEqual(CellFlags.Occupied, _topology.GetFlags(new Vector2Int(2, 0)));
            Assert.AreEqual(CellFlags.None, _topology.GetFlags(new Vector2Int(1, 1)));
        }

        [Test]
        public void ParseTreatsSpacesAsHolesInTheGrid()
        {
            GridTopology _topology = GridTopology.Parse(@"
..
. ");

            Assert.IsTrue(_topology.Contains(new Vector2Int(0, 0)));
            Assert.IsFalse(_topology.Contains(new Vector2Int(1, 0)));
            Assert.AreEqual(3, _topology.Count);
        }

        [Test]
        public void ParseAndToDebugStringRoundTrip()
        {
            string _map = "#.o\n.O.\no.#";

            Assert.AreEqual(_map, GridTopology.Parse(_map).ToDebugString());
        }

        [Test]
        public void RectangleCoversEveryCoordinateOnce()
        {
            GridTopology _topology = GridTopology.Rectangle(4, 3);

            Assert.AreEqual(12, _topology.Count);
            Assert.IsTrue(_topology.Contains(new Vector2Int(3, 2)));
            Assert.IsFalse(_topology.Contains(new Vector2Int(4, 0)));
            Assert.IsFalse(_topology.Contains(new Vector2Int(0, 3)));
        }

        [Test]
        public void RemoveCellMakesACoordinateMissingRatherThanEmpty()
        {
            GridTopology _topology = GridTopology.Rectangle(2, 2);
            _topology.RemoveCell(new Vector2Int(0, 0));

            Assert.IsFalse(_topology.Contains(new Vector2Int(0, 0)));
            Assert.AreEqual(3, _topology.Count);
        }

        [Test]
        public void EmptyTopologyRendersAsEmptyString()
        {
            Assert.AreEqual(string.Empty, new GridTopology().ToDebugString());
        }
    }
}
