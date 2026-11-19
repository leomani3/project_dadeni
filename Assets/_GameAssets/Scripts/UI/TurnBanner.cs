using Deckbuilder.Combat;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class TurnBanner : AUIElement
{
    [SerializeField, Required] private CanvasGroup _canvasGroup;
    [SerializeField, Required] private TMP_Text _label;

    [SerializeField] private string _placementText = "Choose your position";
    [SerializeField] private Color _placementColor = new Color(0.85f, 0.9f, 1f);
    [SerializeField] private string _playerTurnText = "Your turn";
    [SerializeField] private Color _playerTurnColor = new Color(0.4f, 1f, 0.7f);
    [SerializeField] private string _enemyTurnText = "Enemies turn";
    [SerializeField] private Color _enemyTurnColor = new Color(1f, 0.4f, 0.4f);

    [SerializeField] private Vector2 _slideOffset = new Vector2(0f, -60f);
    [SerializeField, Min(0f)] private float _showDuration = 0.25f;
    [SerializeField, Min(0f)] private float _holdDuration = 0.9f;
    [SerializeField, Min(0f)] private float _hideDuration = 0.25f;

    private Sequence _sequence;
    private Vector2 _restPosition;
    private bool _isRestPositionCaptured;

    public override void Init()
    {
        base.Init();

        if (!_isRestPositionCaptured)
        {
            _restPosition = m_content.anchoredPosition;
            _isRestPositionCaptured = true;
        }

        Hide();
    }

    public override void Hide()
    {
        KillSequence();
        ResetBanner();
    }

    public void Show(CombatPhase _phase)
    {
        if (!TryGetPhaseContent(_phase, out string _text, out Color _color))
            return;

        KillSequence();

        _label.text = _text;
        _label.color = _color;

        m_content.gameObject.SetActive(true);
        _canvasGroup.alpha = 0f;
        m_content.anchoredPosition = _restPosition - _slideOffset;

        _sequence = DOTween.Sequence();
        _sequence.Append(_canvasGroup.DOFade(1f, _showDuration));
        _sequence.Join(m_content.DOAnchorPos(_restPosition, _showDuration).SetEase(Ease.OutCubic));
        _sequence.AppendInterval(_holdDuration);
        _sequence.Append(_canvasGroup.DOFade(0f, _hideDuration));
        _sequence.Join(m_content.DOAnchorPos(_restPosition + _slideOffset, _hideDuration).SetEase(Ease.InCubic));
        _sequence.OnComplete(ResetBanner);
    }

    private bool TryGetPhaseContent(CombatPhase _phase, out string _text, out Color _color)
    {
        switch (_phase)
        {
            case CombatPhase.Placement:
                _text = _placementText;
                _color = _placementColor;
                return true;

            case CombatPhase.PlayerTurn:
                _text = _playerTurnText;
                _color = _playerTurnColor;
                return true;

            case CombatPhase.EnemyTurn:
                _text = _enemyTurnText;
                _color = _enemyTurnColor;
                return true;

            default:
                _text = null;
                _color = default;
                return false;
        }
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    private void ResetBanner()
    {
        _canvasGroup.alpha = 0f;
        m_content.anchoredPosition = _restPosition;
        m_content.gameObject.SetActive(false);
    }

    private void KillSequence()
    {
        if (_sequence.IsActive())
            _sequence.Kill();

        _sequence = null;
    }
}
