using Sirenix.OdinInspector;
using UnityEngine;
using Utils;
using Stats;

[CreateAssetMenu(menuName = "Config/GameAssets")]
public class GameAssets : ScriptableObject
{
    private static GameAssets _instance;
    public static GameAssets Instance => _instance ?? Load();

    private static GameAssets Load()
    {
        _instance = Resources.Load<GameAssets>("GameAssets");
        return _instance;
    }


    // ----------------------------------------------------------

    [AssetList(Path = "_GameAssets/Objects/Currencies/")]
    public CurrencyAsset[] currencyAssets;

    // [AssetList(Path = "_GameAssets/Stats/")]
    // public StatData[] statData;
}