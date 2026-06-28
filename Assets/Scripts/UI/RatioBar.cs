using UnityEngine;

namespace GWBGameJam
{
    public class RatioBar : MonoBehaviour
    {
        [SerializeField] private DoughSystem _doughSystem;
        [SerializeField] private DoughConfig _doughConfig;
        [SerializeField] private DoughStateBoundaryConfig _boundaryConfig;

        [Header("Visual Elements")]
        [SerializeField] private RectTransform _indicatorRect;
        [SerializeField] private GameObject _indicatorVisual;
        [SerializeField] private RectTransform _softestRefLine;
        [SerializeField] private RectTransform _mediumRefLine;
        [SerializeField] private RectTransform _hardestRefLine;

        [SerializeField, Min(0.1f)] private float _elasticSpeed = 12f;

        private float _displayedRatio;
        private bool _hasConfigError;
        private bool _refLinesInitialized;
        private RectTransform _barRect;

        private void Awake()
        {
            _barRect = GetComponent<RectTransform>();

            if (_doughSystem == null)    { Debug.LogError("[RatioBar] DoughSystem 未赋值");              _hasConfigError = true; }
            if (_doughConfig == null)    { Debug.LogError("[RatioBar] DoughConfig 未赋值");              _hasConfigError = true; }
            if (_boundaryConfig == null) { Debug.LogError("[RatioBar] DoughStateBoundaryConfig 未赋值"); _hasConfigError = true; }
            if (_indicatorRect == null)  { Debug.LogError("[RatioBar] IndicatorRect 未赋值");            _hasConfigError = true; }
        }

        private void OnEnable()
        {
            EventBus<OnDoughStateChanged>.Subscribe(HandleDoughStateChanged);
            EventBus<OnLevelStarted>.Subscribe(HandleLevelStarted);
        }

        private void OnDestroy()
        {
            EventBus<OnDoughStateChanged>.Unsubscribe(HandleDoughStateChanged);
            EventBus<OnLevelStarted>.Unsubscribe(HandleLevelStarted);
        }

        private void Update()
        {
            if (_hasConfigError) return;

            if (!_refLinesInitialized && _barRect.rect.width > 0f)
            {
                InitRefLines();
                _refLinesInitialized = true;
            }

            bool doughPresent = _doughSystem.GetCurrentDoughState() != DoughState.None;
            if (_indicatorVisual != null)
                _indicatorVisual.SetActive(doughPresent);

            if (!doughPresent || _indicatorRect == null) return;

            _displayedRatio = Mathf.Lerp(_displayedRatio, _doughSystem.GetCurrentRatio(), _elasticSpeed * Time.deltaTime);
            UpdateIndicatorPosition(_displayedRatio);
        }

        private void UpdateIndicatorPosition(float ratio)
        {
            float barWidth = _barRect.rect.width;
            float normalized = 1f - Mathf.Clamp01(ratio / _doughConfig.MaxRatio);
            _indicatorRect.anchoredPosition = new Vector2(
                Mathf.Lerp(-barWidth * 0.5f, barWidth * 0.5f, normalized),
                _indicatorRect.anchoredPosition.y
            );
        }

        private void InitRefLines()
        {
            float barWidth = _barRect.rect.width;
            SetRefLine(_softestRefLine, DoughState.Softest, barWidth);
            SetRefLine(_mediumRefLine,  DoughState.Medium,  barWidth);
            SetRefLine(_hardestRefLine, DoughState.Hardest, barWidth);
        }

        private void SetRefLine(RectTransform refLine, DoughState state, float barWidth)
        {
            if (refLine == null) return;
            float center = _boundaryConfig.GetCenterRatio(state);
            if (center < 0f) return;

            float normalized = 1f - Mathf.Clamp01(center / _doughConfig.MaxRatio);
            float x = Mathf.Lerp(-barWidth * 0.5f, barWidth * 0.5f, normalized);
            float lineWidth = _boundaryConfig.ToleranceHalfWidth / _doughConfig.MaxRatio * barWidth * 2f;

            refLine.anchoredPosition = new Vector2(x, refLine.anchoredPosition.y);
            refLine.sizeDelta = new Vector2(lineWidth, refLine.sizeDelta.y);
        }

        private void HandleDoughStateChanged(OnDoughStateChanged e)
        {
            if (_hasConfigError || _doughConfig == null) return;
            // Returning from throw: snap to InitialRatio to avoid stale elastic position
            if (e.PreviousState == DoughState.None)
                _displayedRatio = _doughConfig.InitialRatio;
        }

        private void HandleLevelStarted(OnLevelStarted e)
        {
            if (_hasConfigError || _doughConfig == null) return;
            _displayedRatio = _doughConfig.InitialRatio;
        }
    }
}
