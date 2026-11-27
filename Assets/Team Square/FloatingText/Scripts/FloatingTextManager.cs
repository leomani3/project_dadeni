using Lean.Pool;
using MyBox;
using UnityEngine;
using Utils;

public class FloatingTextManager : Singleton<FloatingTextManager>
{
    [SerializeField] private FloatingText _floatingTextPrefab;
    [SerializeField] private RectTransform _textParent;
    [SerializeField] private SerializableDictionary<FloatingTextType, FloatingTextConfig> _configs = new SerializableDictionary<FloatingTextType, FloatingTextConfig>();

    public float SpawnWorldText(Vector3 worldPosition, string message, FloatingTextType type, Color? color = null, float stackOffset = 0f)
    {
        FloatingTextConfig config = _configs[type];
        SpawnText().PlayAtWorldPosition(worldPosition, message, config, color ?? config.color, stackOffset);
        return config.spawnDuration + config.stayDuration + config.despawnDuration;
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
