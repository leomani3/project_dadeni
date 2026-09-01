using UnityEngine;

namespace Utils
{
    public static class CusMath
    {
        public static Vector3 Vector3Clamp(Vector3 value, Vector3 min, Vector3 max)
        {
            float clampedX = Mathf.Clamp(value.x, min.x, max.x);
            float clampedY = Mathf.Clamp(value.y, min.y, max.y);
            float clampedZ = Mathf.Clamp(value.z, min.z, max.z);

            return new Vector3(clampedX, clampedY, clampedZ);
        }

        public static Vector3 Vector3Clamp(Vector3 value, Vector3 max)
        {
            float clampedX = Mathf.Clamp(value.x, 0, max.x);
            float clampedY = Mathf.Clamp(value.y, 0, max.y);
            float clampedZ = Mathf.Clamp(value.z, 0, max.z);

            return new Vector3(clampedX, clampedY, clampedZ);
        }

        public static Vector3 Vector3OffsetClamp(Vector3 value, Vector3 max, Vector3 offset)
        {
            float clampedX = Mathf.Clamp(value.x, offset.x - max.x, max.x + offset.x);
            float clampedY = Mathf.Clamp(value.y, offset.y - max.y, max.y + offset.y);
            float clampedZ = Mathf.Clamp(value.z, offset.z - max.z, max.z + offset.z);

            return new Vector3(clampedX, clampedY, clampedZ);
        }


        public static int RandomBinomial(int _trials, double _probability)
        {
            int successes = 0;
            System.Random rng = new System.Random();

            for (int i = 0; i < _trials; i++)
            {
                if (rng.NextDouble() < _probability)
                    successes++;
            }

            return successes;
        }
        
        public static double RandomBinomial(double _trials, double _probability)
        {
            if (_probability == 1)
                return _trials;

            int successes = 0;
            System.Random rng = new System.Random();

            for (int i = 0; i < _trials; i++)
            {
                if (rng.NextDouble() < _probability)
                    successes++;
            }

            return successes;
        }
    }
}