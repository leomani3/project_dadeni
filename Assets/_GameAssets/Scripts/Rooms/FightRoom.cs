using System.Collections.Generic;
using Deckbuilder.Combat;
using UnityEngine;

public class FightRoom : Room
{
    [SerializeField] private Arena _arena;

    public void Init(List<Entity> enemies)
    {
        _arena.SpawnEnemies(enemies, true);
    }

    public override void Enter()
    {
    }

    public override void Exit()
    {
    }
}