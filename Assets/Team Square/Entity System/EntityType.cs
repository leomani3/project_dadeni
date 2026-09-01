using System;
using System.Collections.Generic;

[Flags]
public enum EntityType
{
    Player   = 1 << 0,
    Skeleton = 1 << 1,
    Boss     = 1 << 2,
}

public static class EntityTypeExtensions
{
    public static List<EntityType> GetFlags(this EntityType entityType)
    {
        List<EntityType> flags = new List<EntityType>();
        foreach (EntityType value in Enum.GetValues(typeof(EntityType)))
        {
            if (value != 0 && (entityType & value) == value)
                flags.Add(value);
        }
        return flags;
    }
}
