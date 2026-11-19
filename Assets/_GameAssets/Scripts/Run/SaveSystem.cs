using UnityEngine;
using Utils;

public static class SaveSystem
{
    public const string MetaKey = "GameData";
    public const string RunKey = "RunState";

    public static bool HasRunSave => ES3.KeyExists(RunKey);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearSaveOnPlay()
    {
#if UNITY_EDITOR
        if (!GameConfig.Instance.cheatSettings.startResetData)
            return;

        ClearAllSaveFiles();
        GameConfig.Instance.Log("startResetData is on : every save has been cleared.");
#endif
    }

    public static RunState LoadRun()
    {
        return ES3.Load<RunState>(RunKey);
    }

    public static void SaveRun(RunState _runState)
    {
        if (GameConfig.Instance.cheatSettings.preventSave)
            return;

        ES3.Save(RunKey, _runState);
    }

    public static void DeleteRun()
    {
        if (ES3.KeyExists(RunKey))
            ES3.DeleteKey(RunKey);
    }

    public static void SaveMeta()
    {
        GameData.Instance.Save();
    }

    public static void DeleteEverything()
    {
        ClearAllSaveFiles();
        GameData.ResetGameData();
    }

    private static void ClearAllSaveFiles()
    {
        ES3.DeleteFile();
        ES3.DeleteFile(new ES3Settings(ES3.Location.Cache));
    }
}
