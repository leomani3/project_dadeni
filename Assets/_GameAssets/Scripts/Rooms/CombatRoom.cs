using System.Collections.Generic;

public class CombatRoom : Room
{
    public void Init(IReadOnlyList<Entity> _enemyPrefabs)
    {
        RunManager.Instance.StartCombat(_enemyPrefabs);
    }

    public override void OnEnter()
    {
        //tp camera
    }

    public override void Exit()
    {
    }
}