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
        [SerializeField] private TMP_Text _doughNameText;
        [SerializeField] private GameObject _validCheckmark;

        private bool _hasConfigError;
        private RectTransform _textureBoundsRect;

        private void Awake()
        {
            if (_doughSystem == null)       { Debug.LogError("[RatioBar] DoughSystem 未赋值");        _hasConfigError = true; }
            if (_indicatorRect == null)     { Debug.LogError("[RatioBar] IndicatorRect 未赋值");      _hasConfigError = true; }
            if (_flourTextureRect == null)  { Debug.LogError("[RatioBar] FlourTextureRect 未赋值");   _hasConfigError = true; }
            if (_flourTextureImage == null) { Debug.LogError("[RatioBar] FlourTextureImage 未赋值");  _hasConfigError = true; }
            if (_waterTextureRect == null)  { Debug.LogError("[RatioBar] WaterTextureRect 未赋值");   _hasConfigError = true; }
            if (_waterTextureImage == null) { Debug.LogError("[RatioBar] WaterTextureImage 未赋值");  _hasConfigError = true; }

            _textureBoundsRect = _flourTextureRect != null ? _flourTextureRect.parent as RectTransform : null;
            if (_textureBoundsRect == null || _waterTextureRect == null || _waterTextureRect.parent != _textureBoundsRect)
            {
                Debug.LogError("[RatioBar] FlourTextureRect 与 WaterTextureRect 必须位于同一个 RectTransform 父节点下");
                _hasConfigError = true;
            }

            if (!_hasConfigError)
            {
                ConfigureVisualOrder();
                ConfigureTextureImage(_flourTextureImage);
                ConfigureTextureImage(_waterTextureImage);
            }
        }

        private void Update()
        {
            if (_hasConfigError) return;

            bool doughPresent = _doughSystem.GetCurrentDoughState() != DoughState.None;
            SetDoughVisualsActive(doughPresent);
            if (!doughPresent) return;

            UpdateTextureAreas(_indicatorRect.anchoredPosition.x);
            UpdateDoughStateFeedback();
        }

        private void ConfigureVisualOrder()
        {
            _flourTextureRect.SetAsFirstSibling();
            _waterTextureRect.SetAsFirstSibling();
            _indicatorRect.SetAsLastSibling();
        }

        private void ConfigureTextureImage(Image image)
        {
            if (image == null) return;
            image.raycastTarget = false;
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
            float halfWidth = _textureBoundsRect.rect.width * 0.5f;
            float innerPadding = Mathf.Min(_doughSystem.GetBarInnerPadding(), halfWidth);
            float left = -halfWidth + innerPadding;
            float right = halfWidth - innerPadding;
            float lineHalfWidth = Mathf.Min(_doughSystem.GetIndicatorLineHalfWidth(), (right - left) * 0.5f);
            float clampedX = Mathf.Clamp(indicatorX, left + lineHalfWidth, right - lineHalfWidth);

            SetTextureSpan(_flourTextureRect, left, clampedX);
            SetTextureSpan(_waterTextureRect, clampedX, right);
        }

        private void SetTextureSpan(RectTransform rect, float left, float right)
        {
            if (rect == null) return;

            float width = Mathf.Max(0f, right - left);
            float centerX = left + width * 0.5f;
            rect.anchoredPosition = new Vector2(centerX, rect.anchoredPosition.y);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _textureBoundsRect.rect.height);
        }
    }
}
