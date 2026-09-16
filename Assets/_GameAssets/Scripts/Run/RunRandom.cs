using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Utils;

public static class RunRandom
{
    private const string SeedAlphabet = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    private static readonly Dictionary<RandomChannel, RandomStream> _streams = new Dictionary<RandomChannel, RandomStream>();

    public static long Seed { get; private set; }
    public static bool IsInitialized { get; private set; }
    public static string SeedString => ToSeedString(Seed);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _streams.Clear();
        Seed = 0;
        IsInitialized = false;
    }

    public static void Initialize(string _seedInput)
    {
        Initialize(string.IsNullOrWhiteSpace(_seedInput) ? GenerateSeed() : ParseSeed(_seedInput));
    }

    public static void Initialize(long _seed)
    {
        _streams.Clear();

        Seed = _seed;
        IsInitialized = true;

        foreach (RandomChannel _channel in Enum.GetValues(typeof(RandomChannel)))
            _streams.Add(_channel, new RandomStream(RandomStream.Mix(_seed, (long)_channel)));
    }

    public static RandomStream GetStream(RandomChannel _channel)
    {
        if (!IsInitialized)
            throw new InvalidOperationException($"RunRandom.GetStream({_channel}) called before Initialize.");

        return _streams[_channel];
    }

    public static long GenerateSeed()
    {
        return (DateTime.UtcNow.Ticks ^ ((long)Guid.NewGuid().GetHashCode() << 32)) & long.MaxValue;
    }

    public static string ToSeedString(long _seed)
    {
        ulong _remaining = (ulong)_seed;

        if (_remaining == 0)
            return SeedAlphabet[0].ToString();

        StringBuilder _builder = new StringBuilder();

        while (_remaining > 0)
        {
            _builder.Insert(0, SeedAlphabet[(int)(_remaining % (ulong)SeedAlphabet.Length)]);
            _remaining /= (ulong)SeedAlphabet.Length;
        }

        return _builder.ToString();
    }

    public static long ParseSeed(string _seedString)
    {
        string _normalized = _seedString.Trim().ToUpperInvariant().Replace(" ", string.Empty);

        ulong _seed = 0;

        foreach (char _character in _normalized)
        {
            int _digit = SeedAlphabet.IndexOf(_character);

            if (_digit < 0)
                return HashSeedString(_normalized);

            _seed = _seed * (ulong)SeedAlphabet.Length + (ulong)_digit;
        }

        return (long)_seed;
    }

    private static long HashSeedString(string _seedString)
    {
        long _hash = 0;

        foreach (char _character in _seedString)
            _hash = RandomStream.Mix(_hash, _character);

        return _hash & long.MaxValue;
    }
}
