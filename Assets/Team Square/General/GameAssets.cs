using Deckbuilder.Cards;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;

[CreateAssetMenu(menuName = "Config/GameAssets")]
public class GameAssets : ScriptableObject
{
    private static GameAssets _instance;
    public static GameAssets Instance => _instance ?? Load();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticInstance()
    {
        _instance = null;
    }

    private static GameAssets Load()
    {
        _instance = Resources.Load<GameAssets>("GameAssets");
        return _instance;
    }


    // ----------------------------------------------------------

    [AssetList(Path = "_GameAssets/Objects/Currencies/")]
    public CurrencyAsset[] currencyAssets;

    [AssetList(Path = "_GameAssets/Objects/Cards/")]
    public CardConfig[] cardConfigs;

    [AssetList(Path = "_GameAssets/Objects/Characters/")]
    public CharacterConfig[] characterConfigs;

    public SerializableDictionary<RoomType, Room> roomPrefabs;

    public CardConfig GetCard(string _cardId)
    {
        foreach (CardConfig _cardConfig in cardConfigs)
        {
            if (_cardConfig.Id == _cardId)
                return _cardConfig;
        }

        throw new System.InvalidOperationException($"No CardConfig with the id '{_cardId}' is listed in GameAssets.");
    }

    public CharacterConfig GetCharacter(string _characterId)
    {
        foreach (CharacterConfig _characterConfig in characterConfigs)
        {
            if (_characterConfig.Id == _characterId)
                return _characterConfig;
        }

        throw new System.InvalidOperationException($"No CharacterConfig with the id '{_characterId}' is listed in GameAssets.");
    }
}
