using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GWBGameJam
{
    public class RatioBar : MonoBehaviour
    {
        [SerializeField] private DoughSystem _doughSystem;

        [Header("Visual Elements")]
        [SerializeField] private RectTransform _indicatorRect;
        [SerializeField] private GameObject _indicatorVisual;
        [SerializeField] private RectTransform _flourTextureRect;
        [SerializeField] private Image _flourTextureImage;
        [SerializeField] private RectTransform _waterTextureRect;
        [SerializeField] private Image _waterTextureImage;
        [SerializeField] private SpriteFlipbook _flourFlipbook;
        [SerializeField] private SpriteFlipbook _waterFlipbook;
        [SerializeField] private TMP_Text _doughNameText;
        [SerializeField] private GameObject _validCheckmark;

        private bool _hasConfigError;
        private RectTransform _barRect;

        private void Awake()
        {
            _barRect = GetComponent<RectTransform>();

            if (_doughSystem == null)       { Debug.LogError("[RatioBar] DoughSystem 未赋值");        _hasConfigError = true; }
            if (_indicatorRect == null)     { Debug.LogError("[RatioBar] IndicatorRect 未赋值");      _hasConfigError = true; }
            if (_flourTextureRect == null)  { Debug.LogError("[RatioBar] FlourTextureRect 未赋值");   _hasConfigError = true; }
            if (_flourTextureImage == null) { Debug.LogError("[RatioBar] FlourTextureImage 未赋值");  _hasConfigError = true; }
            if (_waterTextureRect == null)  { Debug.LogError("[RatioBar] WaterTextureRect 未赋值");   _hasConfigError = true; }
            if (_waterTextureImage == null) { Debug.LogError("[RatioBar] WaterTextureImage 未赋值");  _hasConfigError = true; }
            if (_flourFlipbook == null)     { Debug.LogError("[RatioBar] FlourFlipbook 未赋值");      _hasConfigError = true; }
            if (_waterFlipbook == null)     { Debug.LogError("[RatioBar] WaterFlipbook 未赋值");      _hasConfigError = true; }

            if (!_hasConfigError)
                ConfigureVisualOrder();
        }

        private void Update()
        {
            if (_hasConfigError) return;

            UpdateIngredientAnimationInput();

            bool doughPresent = _doughSystem.GetCurrentDoughState() != DoughState.None;
            SetDoughVisualsActive(doughPresent);
            if (!doughPresent) return;

            UpdateTextureAreas(_indicatorRect.anchoredPosition.x);
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
            bool isValid = state == DoughState.Softest || state == DoughState.Medium || state == DoughState.Hardest;

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
    }
}
