using System;
using DG.Tweening;
using UnityEngine;

[Serializable]
public class ClickFeedbackSettings
{
    public Color color = new(0.4f, 1f, 0.6f, 1f);
    public float duration = 0.45f;
    public Ease ease = Ease.OutCubic;
    public float startRadius = 0.75f;
    public float endRadius = 0.28f;
    public float lineWidth = 0.045f;
    public float opaqueRatio = 0.35f;

    public bool useChevrons = true;
    public int chevronCount = 4;
    public float chevronSize = 0.16f;
    public float chevronAngleOffset = 45f;
    public float chevronStartRadiusScale = 1.25f;
    public float chevronEndRadiusScale = 0.45f;

    public bool usePulseRing = true;
    public float pulseEndRadiusScale = 1.7f;
    public float pulseWidthScale = 0.6f;
    public float pulseAlphaScale = 0.45f;
}
