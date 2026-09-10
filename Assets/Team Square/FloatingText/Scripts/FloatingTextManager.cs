using Lean.Pool;
using MyBox;
using UnityEngine;
using Utils;

public class FloatingTextManager : Singleton<FloatingTextManager>
{
    [SerializeField] private FloatingText _floatingTextPrefab;
    [SerializeField] private RectTransform _textParent;
    [SerializeField] private SerializableDictionary<FloatingTextType, FloatingTextConfig> _configs = new SerializableDictionary<FloatingTextType, FloatingTextConfig>();

    public void SpawnWorldText(Vector3 worldPosition, string message, FloatingTextType type)
    {
        SpawnText().PlayAtWorldPosition(worldPosition, message, _configs[type]);
    }

    public void SpawnScreenText(Vector3 screenPosition, string message, FloatingTextType type)
    {
        SpawnText().PlayAtScreenPosition(screenPosition, message, _configs[type]);
    }

    private FloatingText SpawnText()
    {
        return LeanPool.Spawn(_floatingTextPrefab, _textParent);
    }
}
