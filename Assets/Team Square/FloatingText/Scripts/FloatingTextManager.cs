using Lean.Pool;
using MyBox;
using UnityEngine;

public class FloatingTextManager : Singleton<FloatingTextManager>
{
    [SerializeField] private UIFloatingText _uiFloatingTextPrefab;
    [SerializeField] private WorldFloatingText _worldFloatingTextPrefab;
    [SerializeField] private Transform _uiTextParent;
    [SerializeField] private FloatingTextConfig m_defaultConfig;

    public void SpawnUIText(Vector3 screenPos, string text, FloatingTextConfig config)
    {
        Transform parent = _uiTextParent != null ? _uiTextParent : transform;

        UIFloatingText spawnedText = LeanPool.Spawn(_uiFloatingTextPrefab, screenPos, Quaternion.identity, parent);
        spawnedText.Init(text, config != null ? config : m_defaultConfig);
        spawnedText.Play();
    }

    public void SpawnWorldText(Vector3 worldpos, string text, FloatingTextConfig config = null)
    {
        WorldFloatingText spawnedText = LeanPool.Spawn(_worldFloatingTextPrefab, worldpos, Quaternion.identity);
        spawnedText.Init(text, config != null ? config : m_defaultConfig);
        spawnedText.Play();
    }
}
