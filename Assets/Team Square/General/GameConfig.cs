using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Utils;

[CreateAssetMenu(menuName = "Config/GameConfig")]
public class GameConfig : ScriptableObject
{
    private static GameConfig _instance;
    public static GameConfig Instance => _instance ?? Load();

    private static GameConfig Load()
    {
        _instance = Resources.Load<GameConfig>("GameConfig");
#if UNITY_EDITOR
        if (_instance == null)
            UnityEngine.Debug.LogError("GameConfig asset not found in Resources folder!");
#endif
        return _instance;
    }


    //-------------------------------------

    public DebuggingSettings debuggingSettings = new DebuggingSettings();
    public CheatSettings cheatSettings = new CheatSettings();
    public GameSettings gameSettings = new GameSettings();
    public UISettings uiSettings = new UISettings();

    //-------------------------------------

    [System.Serializable]
    public class CheatSettings
    {
        public bool preventSave = false;
        public bool startResetData = false;
        public bool noFTUE = false;
        public bool noCurrencyRequired = false;
        public bool noMenu = false;
        public bool disableBootStrapper = false;
    }

    [System.Serializable]
    public class DebuggingSettings
    {
        public bool developmentBuild;
    }

    public class UISettings
    {
        [Space(10)]
        public bool bounceOnClick = true;
        public float clickScaleDuration = 0.07f;
        public Vector3 clickScale = Vector3.one * 0.95f;
        public Vector3 clickBounceScale = Vector3.one * 1.1f;

        [Space(10)]
        public float hoverScaleDuration = 0.15f;
        public Vector3 hoverScale = Vector3.one * 1.05f;

        [Space(10)]
        public float lockedShakeDuration = 0.3f;
        public float lockedShakeStrenght = 30f;
        public int lockedShakeVibrato = 20;
    }

    [System.Serializable]
    public class GameSettings
    {
        public bool isDemo = false;

        [Space]
        public CurrencyAsset[] resetedCurrency;
        public double[] resetCurrenciesNeeded;

        [Space, Header("Tutorial")]
        public float delayBeforeCanValidateOnClick = 0.2f;
        

        public double GetResetCurrencyNeeded(int index)
        {
            if (resetCurrenciesNeeded == null || resetCurrenciesNeeded.Length == 0)
            {
                Debug.LogError("resetCurrenciesNeeded is not initialized.");
                return 0;
            }

            if (index < 0 || index >= resetCurrenciesNeeded.Length)
            {
                Debug.LogWarning($"Index {index} is out of bounds for resetCurrenciesNeeded array.");
                return resetCurrenciesNeeded[resetCurrenciesNeeded.Length - 1];
            }

            return resetCurrenciesNeeded[index];
        }
    }
}