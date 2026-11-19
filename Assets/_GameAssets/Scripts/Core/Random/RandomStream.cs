using System.Collections.Generic;

namespace Utils
{
    public class RandomStream
    {
        private const ulong GoldenGamma = 0x9E3779B97F4A7C15UL;
        private const ulong MixA = 0xBF58476D1CE4E5B9UL;
        private const ulong MixB = 0x94D049BB133111EBUL;

        private ulong _state;

        public long Seed { get; }
        public int CallCount { get; private set; }

        public RandomStream(long _seed)
        {
            Seed = _seed;
            _state = (ulong)_seed;
        }

        public static long Mix(long _seed, long _salt)
        {
            ulong _value = (ulong)_seed + GoldenGamma * (ulong)(_salt + 1);
            _value = (_value ^ (_value >> 30)) * MixA;
            _value = (_value ^ (_value >> 27)) * MixB;
            return (long)(_value ^ (_value >> 31));
        }

        public float Value()
        {
            return (float)((NextRaw() >> 11) * (1.0 / 9007199254740992.0));
        }

        public int Range(int _minInclusive, int _maxExclusive)
        {
            if (_maxExclusive <= _minInclusive)
                return _minInclusive;

            ulong _span = (ulong)(_maxExclusive - _minInclusive);

            return _minInclusive + (int)(NextRaw() % _span);
        }

        public float Range(float _minInclusive, float _maxInclusive)
        {
            return _minInclusive + Value() * (_maxInclusive - _minInclusive);
        }

        public bool Chance(float _probability)
        {
            return Value() < _probability;
        }

        public T Pick<T>(IReadOnlyList<T> _items)
        {
            return _items[Range(0, _items.Count)];
        }

        public void Shuffle<T>(IList<T> _items)
        {
            for (int _index = _items.Count - 1; _index > 0; _index--)
            {
                int _swapIndex = Range(0, _index + 1);
                (_items[_index], _items[_swapIndex]) = (_items[_swapIndex], _items[_index]);
            }
        }

        private ulong NextRaw()
        {
            CallCount++;

            _state += GoldenGamma;

            ulong _value = _state;
            _value = (_value ^ (_value >> 30)) * MixA;
            _value = (_value ^ (_value >> 27)) * MixB;

            return _value ^ (_value >> 31);
        }
    }
}
