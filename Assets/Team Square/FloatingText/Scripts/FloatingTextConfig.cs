using DG.Tweening;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "FloatingTextConfig", menuName = "ScriptableObjects/FloatingTextConfig")]
public class FloatingTextConfig : ScriptableObject
{
    public Color color = Color.white;
    public TMP_FontAsset font;
    public float fontSize = 24f;

    [Header("Spawn")]
    public float spawnDuration = 0.2f;
    public bool enableScaleIn;
    public Ease scaleInEase = Ease.OutExpo;
    public bool enableFadeIn;

    [Header("Stay")]
    public float stayDuration = 0.5f;

    [Header("Despawn")]
    public float despawnDuration = 0.2f;
    public bool enableScaleOut;
    public bool enableFadeOut;

    [Header("Movement")]
    public float YOffset = 45f;
    public bool randomXMovement;
    public Vector2 minMaxXOffset;
}
