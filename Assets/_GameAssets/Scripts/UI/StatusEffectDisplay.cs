using Deckbuilder.StatusEffects;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class StatusEffectDisplay : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _stacksLabel;

    private void Reset()
    {
        _icon = GetComponent<Image>();
        _stacksLabel = GetComponentInChildren<TMP_Text>();
    }

    public void Refresh(StatusEffect statusEffect)
    {
        _icon.sprite = statusEffect.Config.Icon;
        _stacksLabel.text = statusEffect.Stacks.ToString();
    }
}
