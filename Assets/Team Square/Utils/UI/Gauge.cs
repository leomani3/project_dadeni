using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace Utils.UI
{
    public partial class Gauge : MonoBehaviour
    {
        [TitleGroup("Dependencies")]
        [Required]
        [SerializeField] private RectTransform m_gauge;
        [Required]
        [SerializeField] private RectTransform m_smoothGauge;
        [Required]
        [SerializeField] private RectTransform m_blickT;


        [TitleGroup("Settings")]
        [SerializeField] private float m_startPercent = 0;
        [SerializeField] private float m_freezeGaugeDuration = 0.2f;
        [SerializeField] private float m_smoothGaugeDuration = 0.4f;
        [SerializeField] private Ease m_smoothGaugeEase = Ease.InQuart;


        private void Awake()
        {
            gameObject.SetActive(false);
            SetGaugeFillAmount(m_startPercent, true);
            m_blickT.localScale = Vector3.zero;
        }

        public void SetVisible(bool _setVisible)
        {
            gameObject.SetActive(_setVisible);
        }


        #region Set Fill
        public void SetGaugeFillAmount(float _fillPercent) =>
            SetGaugeFillAmount(_fillPercent, false, m_freezeGaugeDuration, m_smoothGaugeDuration, m_smoothGaugeEase);

        public void SetGaugeFillAmount(float _fillPercent, bool _instant) =>
            SetGaugeFillAmount(_fillPercent, _instant, m_freezeGaugeDuration, m_smoothGaugeDuration, m_smoothGaugeEase);

        public void SetGaugeFillAmount(float _fillPercent, bool _instant, float _freezeGaugeDuration, float _smoothGaugeDuration, Ease _smoothGaugeEase)
        {
            // this.Log($"Set fill amount: {_fillPercent}");
            if (_fillPercent <= 0f)
                _fillPercent = 0f;

            m_gauge.DOKill();

            if (_instant)
            {
                m_gauge.anchorMax = new Vector2(_fillPercent, 1f);
                m_smoothGauge.anchorMax = new Vector2(_fillPercent, 1f);
            }
            else
            {
                m_gauge.anchorMax = new Vector2(_fillPercent, 1f);
                m_smoothGauge.DOAnchorMax(new Vector2(_fillPercent, 1f), _smoothGaugeDuration).SetEase(_smoothGaugeEase).SetDelay(_freezeGaugeDuration);
            }
        }
        #endregion


        public void Blick()
        {
            m_blickT.DOKill();
            m_blickT.localScale = Vector3.one;
            m_blickT.DOScale(0f, .08f).SetEase(Ease.OutSine);
        }


        #region Debug
        private float m_curentFillAmount = 1f;
        [TitleGroup("Debug")]
        [Button]
        public void TestDamage()
        {
            SetGaugeFillAmount(m_curentFillAmount - 0.2f, false, m_freezeGaugeDuration, m_smoothGaugeDuration, m_smoothGaugeEase);
            m_curentFillAmount -= 0.2f;
        }
        [TitleGroup("Debug")]
        [Button]
        public void Refill()
        {
            SetGaugeFillAmount(1, false, m_freezeGaugeDuration, m_smoothGaugeDuration, m_smoothGaugeEase);
            m_curentFillAmount = 1;
        }
        #endregion
    }
}
