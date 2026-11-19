using NUnit.Framework;
using Utils;

namespace Dadeni.Core.Tests
{
    [TestFixture]
    public class RunRandomTests
    {
        [Test]
        public void GetStreamThrowsBeforeInitialize()
        {
            RunRandom.Reset();

            Assert.Throws<System.InvalidOperationException>(() => RunRandom.GetStream(RandomChannel.Map));
        }

        [Test]
        public void ChannelsAreIndependentOfEachOther()
        {
            RunRandom.Initialize(9001L);

            RandomStream _shop = RunRandom.GetStream(RandomChannel.Shop);
            for (int _i = 0; _i < 50; _i++)
                _shop.Value();

            float _mapAfterShopDraws = RunRandom.GetStream(RandomChannel.Map).Value();

            RunRandom.Initialize(9001L);
            float _mapWithNoShopDraws = RunRandom.GetStream(RandomChannel.Map).Value();

            Assert.AreEqual(_mapWithNoShopDraws, _mapAfterShopDraws, "Draws on one channel shifted another channel's sequence.");
        }

        [Test]
        public void TwoChannelsDoNotShareASequence()
        {
            RunRandom.Initialize(5L);

            Assert.AreNotEqual(RunRandom.GetStream(RandomChannel.Map).Value(), RunRandom.GetStream(RandomChannel.Combat).Value());
        }

        [Test]
        public void SeedStringsRoundTrip()
        {
            long[] _seeds = { 0L, 1L, 34L, 1234567L, 987654321098L };

            foreach (long _seed in _seeds)
                Assert.AreEqual(_seed, RunRandom.ParseSeed(RunRandom.ToSeedString(_seed)), $"Seed {_seed} did not survive the round trip.");
        }

        [Test]
        public void SeedStringsNeverUseLettersThatLookLikeDigits()
        {
            for (long _seed = 0; _seed < 5000; _seed += 7)
            {
                string _seedString = RunRandom.ToSeedString(_seed);

                Assert.IsFalse(_seedString.Contains("I"), $"Seed string {_seedString} contains I, which reads as 1.");
                Assert.IsFalse(_seedString.Contains("O"), $"Seed string {_seedString} contains O, which reads as 0.");
            }
        }

        [Test]
        public void SeedAlphabetIsBase34()
        {
            Assert.AreEqual("10", RunRandom.ToSeedString(34), "The seed alphabet is expected to hold 34 symbols (0-9 and A-Z without I and O).");
        }

        [Test]
        public void AnyTypedWordIsAUsableSeed()
        {
            long _first = RunRandom.ParseSeed("dofus like roguelike");
            long _second = RunRandom.ParseSeed("dofus like roguelike");

            Assert.AreEqual(_first, _second);
            Assert.GreaterOrEqual(_first, 0L);
        }

        [Test]
        public void SeedParsingIgnoresCaseAndSpacing()
        {
            Assert.AreEqual(RunRandom.ParseSeed("AB12"), RunRandom.ParseSeed(" ab 12 "));
        }

        [Test]
        public void InitializeFromAnEmptyStringStillProducesARun()
        {
            RunRandom.Initialize(string.Empty);

            Assert.IsTrue(RunRandom.IsInitialized);
            Assert.IsNotNull(RunRandom.GetStream(RandomChannel.Map));
        }
    }
}
