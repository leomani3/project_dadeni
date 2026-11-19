using System.Collections.Generic;
using NUnit.Framework;
using Utils;

namespace Dadeni.Core.Tests
{
    [TestFixture]
    public class RandomStreamTests
    {
        [Test]
        public void SameSeedProducesTheSameSequence()
        {
            RandomStream _first = new RandomStream(4242);
            RandomStream _second = new RandomStream(4242);

            for (int _i = 0; _i < 100; _i++)
                Assert.AreEqual(_first.Value(), _second.Value());
        }

        [Test]
        public void DifferentSeedsDivergeImmediately()
        {
            Assert.AreNotEqual(new RandomStream(1).Value(), new RandomStream(2).Value());
        }

        [Test]
        public void ValueStaysInsideTheUnitInterval()
        {
            RandomStream _random = new RandomStream(7);

            for (int _i = 0; _i < 10000; _i++)
            {
                float _value = _random.Value();

                Assert.GreaterOrEqual(_value, 0f);
                Assert.Less(_value, 1f);
            }
        }

        [Test]
        public void IntRangeIsLowerInclusiveAndUpperExclusive()
        {
            RandomStream _random = new RandomStream(9);
            bool _sawLowerBound = false;
            bool _sawTopValue = false;

            for (int _i = 0; _i < 5000; _i++)
            {
                int _value = _random.Range(5, 8);

                Assert.GreaterOrEqual(_value, 5);
                Assert.Less(_value, 8);

                _sawLowerBound |= _value == 5;
                _sawTopValue |= _value == 7;
            }

            Assert.IsTrue(_sawLowerBound, "Never produced the lower bound.");
            Assert.IsTrue(_sawTopValue, "Never produced the highest allowed value.");
        }

        [Test]
        public void EmptyOrInvertedRangeReturnsTheMinimum()
        {
            RandomStream _random = new RandomStream(3);

            Assert.AreEqual(4, _random.Range(4, 4));
            Assert.AreEqual(4, _random.Range(4, 1));
        }

        [Test]
        public void ChanceIsNeverTrueAtZeroAndAlwaysTrueAtOne()
        {
            RandomStream _random = new RandomStream(11);

            for (int _i = 0; _i < 1000; _i++)
            {
                Assert.IsFalse(_random.Chance(0f));
                Assert.IsTrue(_random.Chance(1f));
            }
        }

        [Test]
        public void ShuffleKeepsEveryElement()
        {
            List<int> _items = new List<int>();
            for (int _i = 0; _i < 50; _i++)
                _items.Add(_i);

            new RandomStream(13).Shuffle(_items);

            Assert.AreEqual(50, _items.Count);

            for (int _i = 0; _i < 50; _i++)
                Assert.Contains(_i, _items);
        }

        [Test]
        public void ShuffleIsReproducibleForASeed()
        {
            List<int> _first = new List<int>();
            List<int> _second = new List<int>();

            for (int _i = 0; _i < 20; _i++)
            {
                _first.Add(_i);
                _second.Add(_i);
            }

            new RandomStream(17).Shuffle(_first);
            new RandomStream(17).Shuffle(_second);

            Assert.AreEqual(_first, _second);
        }

        [Test]
        public void MixIsStableAndChannelSpecific()
        {
            Assert.AreEqual(RandomStream.Mix(100, 3), RandomStream.Mix(100, 3));
            Assert.AreNotEqual(RandomStream.Mix(100, 3), RandomStream.Mix(100, 4));
        }

        [Test]
        public void CallCountTracksConsumedDraws()
        {
            RandomStream _random = new RandomStream(19);

            Assert.AreEqual(0, _random.CallCount);

            _random.Value();
            _random.Range(0, 10);

            Assert.AreEqual(2, _random.CallCount);
        }
    }
}
