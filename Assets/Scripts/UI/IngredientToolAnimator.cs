using UnityEngine;
using UnityEngine.UI;

namespace GWBGameJam
{
    public sealed class IngredientToolAnimator : MonoBehaviour
    {
        [SerializeField] private IngredientType _ingredient = IngredientType.Flour;
        [SerializeField] private Image _targetImage;
        [SerializeField] private Sprite[] _frames;
        [SerializeField, Min(1f)] private float _framesPerSecond = 18f;
        [SerializeField, Min(1f)] private float _pulseOvershoot = 1.12f;

        private int _frameIndex;
        private float _frameTimer;
        private float _animationTimer;
        private float _animationDuration;
        private Vector3 _baseScale;
        private bool _isPlaying;
        private bool _hasConfigError;

        private void Awake()
        {
            ValidateConfig();
            if (_hasConfigError) return;

            _baseScale = _targetImage.rectTransform.localScale;
            _animationDuration = _frames.Length / _framesPerSecond;
            ApplyFrame(0);
        }

        private void OnEnable()
        {
            if (_hasConfigError) return;
            EventBus<OnIngredientUsed>.Subscribe(HandleIngredientUsed);
        }

        private void OnDisable()
        {
            EventBus<OnIngredientUsed>.Unsubscribe(HandleIngredientUsed);
        }

        private void Update()
        {
            if (_hasConfigError || !_isPlaying) return;

            _frameTimer += Time.deltaTime;
            _animationTimer += Time.deltaTime;
            float frameInterval = 1f / _framesPerSecond;

            while (_frameTimer >= frameInterval && _frameIndex < _frames.Length - 1)
            {
                _frameTimer -= frameInterval;
                ApplyFrame(_frameIndex + 1);
            }

            float normalized = Mathf.Clamp01(_animationTimer / _animationDuration);
            float pulse = Mathf.Sin(normalized * Mathf.PI);
            _targetImage.rectTransform.localScale = _baseScale * Mathf.Lerp(1f, _pulseOvershoot, pulse);

            if (_animationTimer < _animationDuration) return;

            _isPlaying = false;
            _targetImage.rectTransform.localScale = _baseScale;
            ApplyFrame(0);
        }

        private void HandleIngredientUsed(OnIngredientUsed e)
        {
            if (_hasConfigError || e.Ingredient != _ingredient) return;

            _frameIndex = 0;
            _frameTimer = 0f;
            _animationTimer = 0f;
            _isPlaying = true;
            ApplyFrame(0);
        }

        private void ApplyFrame(int index)
        {
            _frameIndex = index;
            _targetImage.sprite = _frames[_frameIndex];
        }

        private void ValidateConfig()
        {
            if (_targetImage == null)
            {
                Debug.LogError("[IngredientToolAnimator] TargetImage 未赋值，组件已停用");
                _hasConfigError = true;
                enabled = false;
                return;
            }

            if (_frames == null || _frames.Length == 0)
            {
                Debug.LogError("[IngredientToolAnimator] Frames 为空，组件已停用");
                _hasConfigError = true;
                enabled = false;
                return;
            }

            for (int i = 0; i < _frames.Length; i++)
            {
                if (_frames[i] != null) continue;

                Debug.LogError("[IngredientToolAnimator] Frames 中存在空引用，组件已停用");
                _hasConfigError = true;
                enabled = false;
                return;
            }

            if (_framesPerSecond <= 0f)
            {
                Debug.LogError("[IngredientToolAnimator] FramesPerSecond 必须大于 0，已强制设为 18");
                _framesPerSecond = 18f;
            }

            if (_pulseOvershoot < 1f)
            {
                Debug.LogError("[IngredientToolAnimator] PulseOvershoot 不能小于 1，已强制设为 1.12");
                _pulseOvershoot = 1.12f;
            }
        }
    }
}
