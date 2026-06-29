using UnityEngine;

namespace GWBGameJam
{
    public class DoughVisual : MonoBehaviour
    {
        [SerializeField] private DoughSystem _doughSystem;
        [SerializeField] private GameObject _doughVisual;
        [SerializeField] private RectTransform _doughRect;

        [SerializeField, Min(1f)] private float _maxScale = 3f;
        [SerializeField, Min(0.1f)] private float _growDuration = 3f;

        private float _heldTime;
        private bool _isPlayingState;
        private bool _isBakingIdle = true;
        private bool _hasConfigError;

        private void Awake()
        {
            if (_doughSystem == null) { Debug.LogError("[DoughVisual] DoughSystem 未赋值"); _hasConfigError = true; }
            if (_doughRect == null)   { Debug.LogError("[DoughVisual] _doughRect 未赋值");   _hasConfigError = true; }
        }

        private void OnEnable()
        {
            EventBus<OnGameStateChanged>.Subscribe(HandleGameStateChanged);
            EventBus<OnBakingStateChanged>.Subscribe(HandleBakingStateChanged);
            EventBus<OnDoughStateChanged>.Subscribe(HandleDoughStateChanged);
            EventBus<OnLevelStarted>.Subscribe(HandleLevelStarted);
        }

        private void OnDestroy()
        {
            EventBus<OnGameStateChanged>.Unsubscribe(HandleGameStateChanged);
            EventBus<OnBakingStateChanged>.Unsubscribe(HandleBakingStateChanged);
            EventBus<OnDoughStateChanged>.Unsubscribe(HandleDoughStateChanged);
            EventBus<OnLevelStarted>.Unsubscribe(HandleLevelStarted);
        }

        private void Update()
        {
            if (_hasConfigError) return;

            bool doughPresent = _doughSystem.GetCurrentDoughState() != DoughState.None;
            if (_doughVisual != null)
                _doughVisual.SetActive(doughPresent);

            if (!doughPresent) return;

            // 仅揉制阶段（Playing + BakingState==Idle，与 DoughSystem 输入门控一致）+ 任意鼠标键按住时累计长大；松开保持不缩回
            bool kneading = _isPlayingState && _isBakingIdle;
            if (kneading && (Input.GetMouseButton(0) || Input.GetMouseButton(1)))
                _heldTime = Mathf.Min(_heldTime + Time.deltaTime, _growDuration);

            float t = _heldTime / _growDuration;
            _doughRect.localScale = Vector3.one * Mathf.Lerp(1f, _maxScale, t);
        }

        private void ResetGrowth()
        {
            _heldTime = 0f;
            if (_doughRect != null)
                _doughRect.localScale = Vector3.one;
        }

        private void HandleGameStateChanged(OnGameStateChanged e) => _isPlayingState = e.NewState == GameState.Playing;

        private void HandleBakingStateChanged(OnBakingStateChanged e) => _isBakingIdle = e.NewState == BakingState.Idle;

        private void HandleDoughStateChanged(OnDoughStateChanged e)
        {
            // None → 有面团：开始新一坨，重置长大进度
            if (e.PreviousState == DoughState.None && e.NewState != DoughState.None)
                ResetGrowth();
        }

        private void HandleLevelStarted(OnLevelStarted e) => ResetGrowth();
    }
}
