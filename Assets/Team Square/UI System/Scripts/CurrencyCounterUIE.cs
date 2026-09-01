using System.Collections;
using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Serialization;


public class CurrencyCounterUIE : AUIElement
{
    #region Dependencies        
    [TitleGroup("Dependencies")]
    [SerializeField, Required] private TMP_Text m_counterText;
    [SerializeField, Required] private Image m_currencyIcon;
    [SerializeField, Required] private RectTransform m_layoutGroupRect;
    public Transform IconT => m_currencyIcon.transform;
    #endregion

    #region Settings        
    [TitleGroup("Settings")]
    [SerializeField, Required] private CurrencyAsset m_currencyAsset;
    public CurrencyAsset CurrencyAsset => m_currencyAsset;

    [TitleGroup("Settings")]
    [SerializeField] private bool m_hideIfZero = false;

    [TitleGroup("Settings - Feedbacks")]
    [SerializeField] private float m_shakeStrength = 20f;
    [SerializeField] private float m_shakeDuration = 0.5f;
    [SerializeField] private int m_shakeVibrato = 10;

    [RangeAttribute(0.001f, .1f)]
    [SerializeField] private float m_LerpStrength = 0.01f;

    [SerializeField] private float m_punchDuration = 0.5f;
    [SerializeField] private float m_punchPower = 50f;
    [SerializeField] private int m_punchVibrato = 5;
    [SerializeField] private float m_punchElasticity = 0.65f;
    #endregion

    #region Variables
    private Vector2 m_originalPosition;
    private Tweener m_shakeTween;

    private Coroutine m_lerpCountCoroutine;
    private double m_targetValue;
    private double m_currentValue;

    private RectTransform m_textRectTransform;
    private Vector2 m_textOriginalPosition;
    private Tweener m_textPunchTween;
    #endregion

    public override void Init()
    {
        // this.Log("Init");

        if (GameData.Instance != null)
        {
            GameData.Instance.onCurrencyChanged += OnCurrencyChanged;
            GameData.Instance.onNotEnoughCurrency += OnNotEnoughCurrency;
            GameData.Instance.OnResetData += OnResetData;
            m_counterText.text = Mathf.RoundToInt((float)GameData.Instance.GetInventoryAmount(m_currencyAsset)).ToString();

            // Update visibility on init
            UpdateVisibility(GameData.Instance.GetInventoryAmount(m_currencyAsset));
        }
        // else this.LogWarning("GameData.Instance is null in CurrencyPanel.Init()");

        m_currencyIcon.sprite = m_currencyAsset.Icon;
        m_textRectTransform = m_counterText.GetComponent<RectTransform>();
    }

    private void OnCurrencyChanged(CurrencyAsset _currencyAsset, double _newValue)
    {

        if (_currencyAsset == m_currencyAsset)
        {
            // this.Log($"On currency change for {_currencyAsset}, new value: {_newValue}");
            m_targetValue = _newValue;

            if (_newValue > m_currentValue)
            {
                // this.Log($"{m_currencyAsset} currency changed");
                if (m_lerpCountCoroutine != null)
                {
                    StopCoroutine(m_lerpCountCoroutine);
                }

                if (!gameObject.activeSelf)
                    gameObject.SetActive(true);

                m_lerpCountCoroutine = StartCoroutine(LerpCountCR());


                TextPunchScale();
                // m_text.text = Mathf.RoundToInt((float)m_targetValue).ToString();
                // TextPunchPosition();
                // StartTextEffect();
            }
            else
            {
                m_currentValue = m_targetValue;
                m_counterText.text = Mathf.RoundToInt((float)m_targetValue).ToString();
            }

            // Update visibility based on new value
            UpdateVisibility(_newValue);
        }
    }

    private void OnNotEnoughCurrency(CurrencyAsset _currencyAsset)
    {
        if (_currencyAsset == m_currencyAsset)
        {
            //this.Log($"Not enough {_currencyAsset} currency!");
            ShakeContent();
        }
    }

    private void OnResetData()
    {
        double amount = GameData.Instance.GetInventoryAmount(m_currencyAsset);
        m_counterText.text = amount.ToString();
        UpdateVisibility(amount);
    }

    private void UpdateVisibility(double _value)
    {
        if (m_hideIfZero)
        {
            gameObject.SetActive(_value > 0);
        }
    }

    private void ShakeContent()
    {
        m_shakeTween?.Complete();

        // Reset la position
        //m_content.anchoredPosition = m_originalPosition;

        // Shake horizontal uniquement (axe X)
        m_shakeTween = m_content.DOShakeAnchorPos(
            m_shakeDuration,
            new Vector3(m_shakeStrength, 0, 0), // Shake sur X seulement
            m_shakeVibrato);
        //.OnComplete(() => m_content.anchoredPosition = m_originalPosition);
    }

    private IEnumerator LerpCountCR()
    {
        double startDiff = m_targetValue - m_currentValue;
        while (m_targetValue > m_currentValue + startDiff * 0.05f + 3)
        {
            m_currentValue = Mathf.Lerp((float)m_currentValue, (float)m_targetValue, m_LerpStrength);
            m_counterText.text = Mathf.RoundToInt((float)m_currentValue).ToString();
            yield return null;
        }
        m_currentValue = m_targetValue;
        m_counterText.text = Mathf.RoundToInt((float)m_targetValue).ToString();
        //LayoutRebuilder.ForceRebuildLayoutImmediate(m_layoutGroupRect);
    }

    private void TextPunchScale()
    {
        if (m_textPunchTween != null && m_textPunchTween.IsActive())
        {
            m_textPunchTween.Kill();
        }

        // Réinitialiser l'échelle à la valeur originale
        m_textRectTransform.localScale = Vector3.one;

        m_textPunchTween = m_textRectTransform.DOPunchScale(
            Vector3.one * (m_punchPower / 100f),
            m_punchDuration,
            m_punchVibrato,
            m_punchElasticity
        ).OnComplete(() => m_textRectTransform.localScale = Vector3.one);
    }

    [Button]
    private void TestPunch()
    {
        TextPunchScale();
    }
}