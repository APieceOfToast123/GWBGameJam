using UnityEngine;
using UnityEngine.UI;

namespace GWBGameJam
{
    public class BakingIndicator : MonoBehaviour
    {
        [SerializeField] private BakingSystem _bakingSystem;
        [SerializeField] private BakingConfig _bakingConfig;
        [SerializeField] private Image _fill;

        [Header("State Colors")]
        [SerializeField] private Color _idleColor        = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color _undercookedColor = Color.yellow;
        [SerializeField] private Color _cookedColor      = Color.green;
        [SerializeField] private Color _burntColor       = Color.red;

        private BakingState _currentState = BakingState.Idle;
        private bool _hasConfigError;

        private void Awake()
        {
            if (_bakingSystem == null) { Debug.LogError("[BakingIndicator] BakingSystem 未赋值");  _hasConfigError = true; }
            if (_bakingConfig == null) { Debug.LogError("[BakingIndicator] BakingConfig 未赋值");  _hasConfigError = true; }
            if (_fill == null)         { Debug.LogError("[BakingIndicator] Fill Image 未赋值");     _hasConfigError = true; }
        }

        private void OnEnable()
        {
            EventBus<OnBakingStateChanged>.Subscribe(HandleBakingStateChanged);
        }

        private void OnDestroy()
        {
            EventBus<OnBakingStateChanged>.Unsubscribe(HandleBakingStateChanged);
        }

        private void Update()
        {
            if (_hasConfigError) return;
            float t = _bakingSystem.GetBakingTimer();
            _fill.fillAmount = _currentState == BakingState.Idle
                ? 0f
                : Mathf.Clamp01(t / _bakingConfig.BurntForcedThrowDuration);
        }

        private void HandleBakingStateChanged(OnBakingStateChanged e)
        {
            if (_hasConfigError) return;
            _currentState = e.NewState;
            _fill.color = e.NewState switch
            {
                BakingState.Undercooked => _undercookedColor,
                BakingState.Cooked      => _cookedColor,
                BakingState.Burnt       => _burntColor,
                _                       => _idleColor,
            };
        }
    }
}
