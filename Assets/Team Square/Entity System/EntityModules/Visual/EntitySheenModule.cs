using DG.Tweening;
using UnityEngine;

public class EntitySheenModule : EntityModule
{
    [SerializeField, Min(0f)] private float _sheenHoldDuration = 0.05f;
    [SerializeField, Min(0f)] private float _sheenFadeDuration = 0.15f;
    [SerializeField] private Renderer[] _renderers;
    [SerializeField] private string _emissionColorProperty = "_EmissionColor";
    [SerializeField] private string _emissionKeyword = "_Emission";

    private Material[] _materials;
    private Color[] _originalEmissionColors;
    private Sequence _sheenSequence;

    protected override void OnInitialize()
    {
        base.OnInitialize();

        _materials = new Material[_renderers.Length];
        _originalEmissionColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;

            Material material = _renderers[i].material;
            _materials[i] = material;

            material.EnableKeyword(_emissionKeyword);

            _originalEmissionColors[i] = material.HasProperty(_emissionColorProperty)
                ? material.GetColor(_emissionColorProperty)
                : Color.black;
        }
    }

    public override void CacheReferences()
    {
        base.CacheReferences();
        _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
    }

    public void PlayWhiteSheen()
    {
        if (_materials == null) return;

        _sheenSequence?.Kill(complete: false);
        _sheenSequence = DOTween.Sequence().SetLink(Owner.gameObject);

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;

            Material material = _materials[i];
            if (material == null || !material.HasProperty(_emissionColorProperty)) continue;

            material.SetColor(_emissionColorProperty, Color.white);

            Tween fadeBackToOriginalColor = material
                .DOColor(_originalEmissionColors[i], _emissionColorProperty, _sheenFadeDuration)
                .SetEase(Ease.OutQuad);

            _sheenSequence.Insert(_sheenHoldDuration, fadeBackToOriginalColor);
        }

        _sheenSequence.Play();
    }
}
