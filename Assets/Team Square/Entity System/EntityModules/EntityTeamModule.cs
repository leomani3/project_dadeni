using UnityEngine;

public enum Team
{
    Player = 0,
    Enemy = 1
}

public class EntityTeamModule : EntityModule
{
    [SerializeField] private Team _team;

    public Team Team => _team;
}
