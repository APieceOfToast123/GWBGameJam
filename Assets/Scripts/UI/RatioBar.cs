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
        [SerializeField, Min(0f)] private float _textureLength = 100f;

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

            if (!_hasConfigError)
                ConfigureVisualOrder();
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
            // 面粉块贴指针左侧、水块贴右侧，等长固定，随指针一起移动
            float half = _textureLength * 0.5f;
            SetTextureBlock(_flourTextureRect, indicatorX - half);
            SetTextureBlock(_waterTextureRect, indicatorX + half);
        }

        private void SetTextureBlock(RectTransform rect, float centerX)
        {
            if (rect == null) return;
            rect.anchoredPosition = new Vector2(centerX, rect.anchoredPosition.y);
            rect.sizeDelta = new Vector2(_textureLength, rect.sizeDelta.y);
        }
    }
}
