using System;
using UnityEngine;

[Flags]
public enum Team
{
    Ally = 1 << 0,
    Enemy = 1 << 1
}

public class EntityTeamModule : EntityModule
{
    [SerializeField] private Team _team = Team.Ally;

    public Team Team => _team;

    public bool BelongsTo(Team _teamMask)
    {
        return (_teamMask & _team) != 0;
    }
}
