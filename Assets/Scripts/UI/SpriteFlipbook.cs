using UnityEngine;
using UnityEngine.UI;

namespace GWBGameJam
{
    public sealed class SpriteFlipbook : MonoBehaviour
    {
        [SerializeField] private Image _targetImage;
        [SerializeField] private Sprite[] _frames;
        [SerializeField, Min(0.1f)] private float _baseFps = 6f;
        [SerializeField, Min(0.1f)] private float _burstFps = 30f;
        [SerializeField, Min(0.01f)] private float _pulseDuration = 0.2f;
        [SerializeField, Min(1f)] private float _pulseOvershoot = 1.1f;

        private float _frameTimer;
        private float _pulseTimer;
        private float _speedMultiplier = 1f;
        private int _frameIndex;
        private Vector3 _baseScale;
        private bool _hasBaseScale;

        private void Awake()
        {
            if (_targetImage != null)
            {
                _baseScale = _targetImage.rectTransform.localScale;
                _hasBaseScale = true;
            }

            ValidateConfig();
            if (!enabled)
                return;

            ApplyFrame(0);
        }

        private void OnDisable()
        {
            if (_targetImage != null && _hasBaseScale)
                _targetImage.rectTransform.localScale = _baseScale;

            _pulseTimer = 0f;
            _speedMultiplier = 1f;
        }

        private void Update()
        {
            float fps = _pulseTimer > 0f ? _burstFps : _baseFps * _speedMultiplier;
            AnimateFrames(fps);
            AnimatePulse();
        }

        public void TriggerPulse()
        {
            if (!enabled)
                return;

            _pulseTimer = _pulseDuration;
            _targetImage.rectTransform.localScale = _baseScale * _pulseOvershoot;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            _speedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        public void ResetToFirstFrame()
        {
            _frameTimer = 0f;
            ApplyFrame(0);
        }

        private void AnimateFrames(float fps)
        {
            _frameTimer += Time.deltaTime;
            float frameInterval = 1f / fps;

            while (_frameTimer >= frameInterval)
            {
                _frameTimer -= frameInterval;
                ApplyFrame((_frameIndex + 1) % _frames.Length);
            }
        }

        private void AnimatePulse()
        {
            if (_pulseTimer <= 0f)
                return;

            _pulseTimer = Mathf.Max(0f, _pulseTimer - Time.deltaTime);
            float normalized = _pulseTimer / _pulseDuration;
            float scale = Mathf.Lerp(1f, _pulseOvershoot, normalized);
            _targetImage.rectTransform.localScale = _baseScale * scale;
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
                Debug.LogError("[SpriteFlipbook] TargetImage 未赋值，组件已禁用");
                enabled = false;
                return;
            }

            if (_frames == null || _frames.Length == 0)
            {
                Debug.LogError("[SpriteFlipbook] Frames 为空，组件已禁用");
                enabled = false;
                return;
            }

            for (int i = 0; i < _frames.Length; i++)
            {
                if (_frames[i] != null)
                    continue;

                Debug.LogError("[SpriteFlipbook] Frames 中存在空引用，组件已禁用");
                enabled = false;
                return;
            }

            if (_baseFps <= 0f)
            {
                Debug.LogError("[SpriteFlipbook] BaseFps 必须大于 0，已强制设为 6");
                _baseFps = 6f;
            }

            if (_burstFps <= 0f)
            {
                Debug.LogError("[SpriteFlipbook] BurstFps 必须大于 0，已强制设为 30");
                _burstFps = 30f;
            }

            if (_pulseDuration <= 0f)
            {
                Debug.LogError("[SpriteFlipbook] PulseDuration 必须大于 0，已强制设为 0.2");
                _pulseDuration = 0.2f;
            }

            if (_pulseOvershoot < 1f)
            {
                Debug.LogError("[SpriteFlipbook] PulseOvershoot 不能小于 1，已强制设为 1.1");
                _pulseOvershoot = 1.1f;
            }
        }
    }
}
