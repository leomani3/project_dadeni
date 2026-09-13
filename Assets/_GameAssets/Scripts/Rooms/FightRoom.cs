using System.Collections.Generic;

public class FightRoom : Room
{
    public void Init(IReadOnlyList<Entity> _enemyPrefabs)
    {
        RunManager.Instance.StartCombat(_enemyPrefabs);
    }

    public override void Enter()
    {
    }

    public override void Exit()
    {
    }
}