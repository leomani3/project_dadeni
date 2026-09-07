using System;

namespace Utils
{
    [Flags]
    public enum RotationAxis
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 4,
        All = X | Y | Z
    }
}
