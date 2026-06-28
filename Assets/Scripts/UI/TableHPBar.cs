using UnityEngine;
using UnityEngine.UI;

namespace GWBGameJam
{
    public class TableHPBar : MonoBehaviour
    {
        [SerializeField] private TableSystem _tableSystem;
        [SerializeField] private Image _fill;

        [Header("HP Color Thresholds")]
        [SerializeField] private Color _highHPColor = Color.green;
        [SerializeField] private Color _midHPColor  = Color.yellow;
        [SerializeField] private Color _lowHPColor  = Color.red;
        [SerializeField, Range(0f, 1f)] private float _midThreshold = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _lowThreshold = 0.25f;

        private bool _hasConfigError;

        private void Awake()
        {
            if (_tableSystem == null) { Debug.LogError("[TableHPBar] TableSystem 未赋值"); _hasConfigError = true; }
            if (_fill == null)        { Debug.LogError("[TableHPBar] Fill Image 未赋值");  _hasConfigError = true; }
        }

        private void OnEnable()
        {
            EventBus<OnMonsterReachedTable>.Subscribe(HandleMonsterReachedTable);
            EventBus<OnLevelStarted>.Subscribe(HandleLevelStarted);
        }

        private void OnDestroy()
        {
            EventBus<OnMonsterReachedTable>.Unsubscribe(HandleMonsterReachedTable);
            EventBus<OnLevelStarted>.Unsubscribe(HandleLevelStarted);
        }

        private void UpdateDisplay()
        {
            if (_hasConfigError) return;
            int maxHP = _tableSystem.GetMaxHP();
            if (maxHP <= 0) return;

            float ratio = (float)_tableSystem.GetCurrentHP() / maxHP;
            _fill.fillAmount = ratio;
            _fill.color = ratio <= _lowThreshold ? _lowHPColor
                        : ratio <= _midThreshold  ? _midHPColor
                        : _highHPColor;
        }

        private void HandleMonsterReachedTable(OnMonsterReachedTable e) => UpdateDisplay();
        private void HandleLevelStarted(OnLevelStarted e) => UpdateDisplay();
    }
}
