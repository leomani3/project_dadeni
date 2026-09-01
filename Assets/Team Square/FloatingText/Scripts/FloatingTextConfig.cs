using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "FloatingTextConfig", menuName = "ScriptableObjects/FloatingTextConfig")]
public class FloatingTextConfig : ScriptableObject
{
    public Color color;
    public TMP_FontAsset font;
    public float fontSize = 15f;
    
    [Header("Spawn")]
    public float spawnDuration = 0.2f;
    //scale in
    public bool enableScaleIn;
    [ShowIf(nameof(enableScaleIn))] public Ease scaleInEase = Ease.OutExpo;
    //fade in
    public bool enableFadeIn;
    
    [Header("Stay")]
    public float stayDuration = 0.5f;
    
    [Header("Despawn")]
    public float despawnDuration = 0.2f;
    //scale out
    public bool enableScaleOut;
    //fade out
    public bool enableFadeOut;
    
    [Header("Movement")]
    public float YOffset = 50f;
    public bool randomXMovement;
    [ShowIf(nameof(randomXMovement))]public Vector2 minMaxXOffset;
}