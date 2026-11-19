    using System;
    using System.Collections.Generic;
    using DG.Tweening;
    using MyBox;
    using UnityEngine;
    using UnityEngine.Serialization;

    public enum TutorialEvent
    {
        OnRunStart = 0,
        OnRunEnd = 1,
        OnClick = 2,
    }

    public enum TutorialTarget
    {
        UI = 0,
        World = 1,
    }

    [Serializable]
    public class TutorialStep
    {

        #region Fields
        public bool enabled = true;
        public string stepID;

        public TutorialEvent triggerEvent;

        public TutorialEvent validateEvent;

        public bool allowRetroActiveValidation;

        public TutorialTarget tutorialTarget;

        public RectTransform rectTarget;
        
        public Transform worldTarget;

        public float delay;

        [Space, Header("Highlight")]
        public bool useHighlight;

        public bool highlightModal;

        public bool highlightDespawnOnComplete;

        public float highlightFinalSize;

        public Vector3 highlightOffset;

        [Space, Header("Text Bubble")]
        public bool useTextBubble;

        public string title;

        [TextArea(10, 20)]
        public string text;

        public Vector3 offset;

        [Space, Header("Time Control")]
        public bool freezeTime;

        private bool m_active;
        private float m_previousTimeScale;
        private float m_stepActivationTime;
        private Dictionary<TutorialEvent, (Action subscribe, Action unsubscribe)> eventMap;
        #endregion

        private bool IsValidateEventOnClick() => validateEvent == TutorialEvent.OnClick;

        private Vector3 ResolutionBasedOffset(Vector3 pixelBasedOffset)
        {
            return new Vector3((pixelBasedOffset.x / 100) * Screen.width, (pixelBasedOffset.y / 100) * Screen.height, 0);
        }

        private void InitializeEventMap()
        {
            if (eventMap != null) return;

            eventMap = new Dictionary<TutorialEvent, (Action, Action)>
            {
                [TutorialEvent.OnRunStart] = (
                    () => GameManager.Instance.OnRunStart += TriggerTutorialStep,
                    () => GameManager.Instance.OnRunStart -= TriggerTutorialStep
                ),
                [TutorialEvent.OnRunEnd] = (
                    () => GameManager.Instance.OnRunEnd += TriggerTutorialStep,
                    () => GameManager.Instance.OnRunEnd -= TriggerTutorialStep
                )
            };
        }

        private Dictionary<TutorialEvent, (Action subscribe, Action unsubscribe)> GetValidationEventMap()
        {
            return new Dictionary<TutorialEvent, (Action, Action)>
            {
                [TutorialEvent.OnRunStart] = (
                    () => GameManager.Instance.OnRunStart += ValidateStep,
                    () => GameManager.Instance.OnRunStart -= ValidateStep
                ),
                [TutorialEvent.OnRunEnd] = (
                    () => GameManager.Instance.OnRunEnd += ValidateStep,
                    () => GameManager.Instance.OnRunEnd -= ValidateStep
                )
            };
        }

        public void SubscribeToEvents()
        {
            if (!enabled) return;

            InitializeEventMap();
            SubscribeToEvent(triggerEvent, eventMap);

            if (allowRetroActiveValidation)
            {
                var validationMap = GetValidationEventMap();
                SubscribeToEvent(validateEvent, validationMap);
            }
        }

        private void SubscribeToEvent(TutorialEvent eventType, Dictionary<TutorialEvent, (Action subscribe, Action unsubscribe)> map)
        {
            if (map.TryGetValue(eventType, out var actions))
            {
                actions.subscribe?.Invoke();
            }
        }

        private void UnsubscribeFromEvent(TutorialEvent eventType, Dictionary<TutorialEvent, (Action subscribe, Action unsubscribe)> map)
        {
            if (map.TryGetValue(eventType, out var actions))
            {
                actions.unsubscribe?.Invoke();
            }
        }

        private void UnsubscribeFromEvents()
        {
            InitializeEventMap();
            UnsubscribeFromEvent(triggerEvent, eventMap);

            var validationMap = GetValidationEventMap();
            UnsubscribeFromEvent(validateEvent, validationMap);
        }

private void TriggerTutorialStep()
{
    Vector3? safeWorldPos = null;
    if (tutorialTarget == TutorialTarget.World)
        safeWorldPos = worldTarget.position;

    DOVirtual.DelayedCall(delay, () =>
    {
        if (GameData.Instance.completedFtueSteps.Contains(stepID)) return;

        m_active = true;
        m_stepActivationTime = Time.realtimeSinceStartup;

        if (freezeTime)
        {
            m_previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (useHighlight)
        {
            switch (tutorialTarget)
            {
                case TutorialTarget.UI:
                    if (rectTarget != null)
                        TutorialUIManager.Instance.SpawnHighlight(
                            rectTarget.position, 
                            highlightFinalSize,
                            highlightDespawnOnComplete, 
                            highlightModal, 
                            ResolutionBasedOffset(highlightOffset));
                    break;
                case TutorialTarget.World:
                    if (safeWorldPos.HasValue)
                        TutorialUIManager.Instance.SpawnHighlightWorld(
                            safeWorldPos.Value, 
                            highlightFinalSize,
                            highlightDespawnOnComplete, 
                            highlightModal, 
                            ResolutionBasedOffset(highlightOffset));
                    break;
            }
        }

        if (useTextBubble)
        {
            switch (tutorialTarget)
            {
                case TutorialTarget.UI:
                    if (rectTarget != null)
                        TutorialUIManager.Instance.SpawnTextBubble(
                            rectTarget.position + ResolutionBasedOffset(offset),
                            title, text, validateEvent == TutorialEvent.OnClick);
                    break;
                case TutorialTarget.World:
                    if (safeWorldPos.HasValue)
                        TutorialUIManager.Instance.SpawnTextBubble(
                            CameraManager.Instance.MainCam.WorldToScreenPoint(safeWorldPos.Value) + ResolutionBasedOffset(offset),
                            title, text, validateEvent == TutorialEvent.OnClick);
                    break;
            }
        }

        if (!allowRetroActiveValidation)
        {
            var validationMap = GetValidationEventMap();
            SubscribeToEvent(validateEvent, validationMap);
        }

    }).SetUpdate(true);
}

        private void ValidateStep()
        {
            // Debug.Log($"Validating Tutorial Step: {stepID}");
            if (validateEvent == TutorialEvent.OnClick && m_active)
            {
                float timeSinceActivation = Time.realtimeSinceStartup - m_stepActivationTime;
                if (timeSinceActivation < GameConfig.Instance.gameSettings.delayBeforeCanValidateOnClick)
                {
                    return;
                }
            }

            if (!GameData.Instance.completedFtueSteps.Contains(stepID))
            {
                if (m_active)
                {
                    if (useHighlight)
                        TutorialUIManager.Instance.DespawnHighlight();

                    if (useTextBubble)
                        TutorialUIManager.Instance.DespawnTextBubble();

                    // Restore time scale if it was frozen
                    if (freezeTime)
                    {
                        Time.timeScale = m_previousTimeScale;
                    }
                }

                GameData.Instance.completedFtueSteps.Add(stepID);
                m_active = false;
            }

            TutorialManager.Instance.OnTutorialStepCompleted();
        }
    }

    public class TutorialManager : Singleton<TutorialManager>
    {
        public event Action onTutorialStepCompleted;

        [SerializeField] private List<TutorialStep> m_tutorialSteps;

        public void Init()
        {
            if (GameConfig.Instance.cheatSettings.noFTUE)
                return;

            foreach (TutorialStep tutorialStep in m_tutorialSteps)
                tutorialStep.SubscribeToEvents();
        }

        public void OnTutorialStepCompleted()
        {
            onTutorialStepCompleted?.Invoke();
        }
    }