using Sirenix.OdinInspector;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CA_", menuName = "Currency")]
[Serializable]
public class CurrencyAsset : ScriptableObject
{
    [TitleGroup("Settings")]
    [SerializeField] protected Currency m_currency;
    [SerializeField] protected string m_displayName;
    [PreviewField(ObjectFieldAlignment.Center, Height = 100f)]
    [SerializeField] protected Sprite m_icon;
    [SerializeField] private string m_spriteAssetString = "<sprite=\"\" name=\"\">";
    public TrackedValueType[] trackedValuesWithCurrencyGained;

    public string SpriteAssetString => m_spriteAssetString;
    public Currency Currency => m_currency;
    public Sprite Icon => m_icon;
    public string DisplayName => m_displayName;
}