using System;
using System.Collections.Generic;

[Serializable]
public class CharacterProgress
{
    public bool unlocked;
    public int level = 1;
    public double experience;
    public List<string> ownedItemIds = new List<string>();
    public int runsStarted;
    public int runsWon;

    public CharacterProgress()
    {
    }

    public CharacterProgress(bool _unlocked)
    {
        unlocked = _unlocked;
    }
}
