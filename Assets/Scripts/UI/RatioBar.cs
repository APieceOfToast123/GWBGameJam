using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        [SerializeField] private RectTransform _flourTextureRect;
        [SerializeField] private Image _flourTextureImage;
        [SerializeField] private RectTransform _waterTextureRect;
        [SerializeField] private Image _waterTextureImage;
        [SerializeField] private SpriteFlipbook _flourFlipbook;
        [SerializeField] private SpriteFlipbook _waterFlipbook;
        [SerializeField] private RectTransform _softestRefLine;
        [SerializeField] private RectTransform _mediumRefLine;
        [SerializeField] private RectTransform _hardestRefLine;
        [SerializeField] private TMP_Text _doughNameText;
        [SerializeField] private GameObject _validCheckmark;

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
            if (_flourTextureRect == null)  { Debug.LogError("[RatioBar] FlourTextureRect is not assigned");  _hasConfigError = true; }
            if (_flourTextureImage == null) { Debug.LogError("[RatioBar] FlourTextureImage is not assigned"); _hasConfigError = true; }
            if (_waterTextureRect == null)  { Debug.LogError("[RatioBar] WaterTextureRect is not assigned");  _hasConfigError = true; }
            if (_waterTextureImage == null) { Debug.LogError("[RatioBar] WaterTextureImage is not assigned"); _hasConfigError = true; }
            if (_flourFlipbook == null) { Debug.LogError("[RatioBar] FlourFlipbook 未赋值"); _hasConfigError = true; }
            if (_waterFlipbook == null) { Debug.LogError("[RatioBar] WaterFlipbook 未赋值"); _hasConfigError = true; }

            if (!_hasConfigError)
                ConfigureVisualOrder();
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

            UpdateIngredientAnimationInput();

            if (!_refLinesInitialized && _barRect.rect.width > 0f)
            {
                InitRefLines();
                _refLinesInitialized = true;
            }

            bool doughPresent = _doughSystem.GetCurrentDoughState() != DoughState.None;
            SetDoughVisualsActive(doughPresent);

            if (!doughPresent || _indicatorRect == null) return;

            _displayedRatio = Mathf.Lerp(_displayedRatio, _doughSystem.GetCurrentRatio(), _elasticSpeed * Time.deltaTime);
            float indicatorX = UpdateIndicatorPosition(_displayedRatio);
            UpdateTextureAreas(indicatorX);
            UpdateDoughStateFeedback();
        }

        private void UpdateIngredientAnimationInput()
        {
            if (!_doughSystem.IsInputActive())
            {
                _waterFlipbook.SetSpeedMultiplier(1f);
                return;
            }

            if (Input.GetMouseButtonDown(0))
                _flourFlipbook.TriggerPulse();

            if (Input.GetMouseButtonDown(1))
                _waterFlipbook.TriggerPulse();

            _waterFlipbook.SetSpeedMultiplier(Input.GetMouseButton(1) ? 3f : 1f);
        }

        private void ConfigureVisualOrder()
        {
            _flourTextureRect.SetAsFirstSibling();
            _waterTextureRect.SetAsFirstSibling();
            _indicatorRect.SetAsLastSibling();
        }

        private void SetDoughVisualsActive(bool isActive)
        {
            if (_indicatorVisual != null)
                _indicatorVisual.SetActive(isActive);
            if (_flourTextureImage != null)
                _flourTextureImage.gameObject.SetActive(isActive);
            if (_waterTextureImage != null)
                _waterTextureImage.gameObject.SetActive(isActive);
        }

        private void UpdateDoughStateFeedback()
        {
            DoughState state = _doughSystem.GetCurrentDoughState();
            float center = _boundaryConfig.GetCenterRatio(state);
            bool isValid = center >= 0f
                           && Mathf.Abs(_doughSystem.GetCurrentRatio() - center)
                           <= _boundaryConfig.ToleranceHalfWidth;

            if (_validCheckmark != null)
                _validCheckmark.SetActive(isValid);
            if (_doughNameText != null)
                _doughNameText.text = state switch
                {
                    DoughState.TooSoft => "浆糊",
                    DoughState.Softest => "最软面包",
                    DoughState.Medium => "中等面包",
                    DoughState.Hardest => "最硬面包",
                    DoughState.TooHard => "干面团",
                    _ => string.Empty
                };
        }

        private float UpdateIndicatorPosition(float ratio)
        {
            float barWidth = _barRect.rect.width;
            float normalized = 1f - Mathf.Clamp01(ratio / _doughConfig.MaxRatio);
            float x = Mathf.Lerp(-barWidth * 0.5f, barWidth * 0.5f, normalized);
            _indicatorRect.anchoredPosition = new Vector2(x, _indicatorRect.anchoredPosition.y);
            return x;
        }

        private void UpdateTextureAreas(float indicatorX)
        {
            float halfWidth = _barRect.rect.width * 0.5f;
            SetTextureArea(_flourTextureRect, -halfWidth, indicatorX);
            SetTextureArea(_waterTextureRect, indicatorX, halfWidth);
        }

        private static void SetTextureArea(RectTransform rect, float leftX, float rightX)
        {
            if (rect == null) return;

            float width = Mathf.Max(0f, rightX - leftX);
            rect.anchoredPosition = new Vector2((leftX + rightX) * 0.5f, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
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
