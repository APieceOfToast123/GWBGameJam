using UnityEngine;
using UnityEngine.UI;

namespace GWBGameJam
{
    public class TableHPBar : MonoBehaviour
    {
        [SerializeField] private TableSystem _tableSystem;
        [SerializeField] private Image[] _hearts;
        [SerializeField] private Sprite _fullSprite;
        [SerializeField] private Sprite _emptySprite;

        private int _displayHP;
        private bool _hasConfigError;

        private void Awake()
        {
            if (_tableSystem == null)                  { Debug.LogError("[TableHPBar] TableSystem 未赋值"); _hasConfigError = true; }
            if (_hearts == null || _hearts.Length == 0) { Debug.LogError("[TableHPBar] Hearts 未配置");      _hasConfigError = true; }
            if (_fullSprite == null)                   { Debug.LogError("[TableHPBar] FullSprite 未赋值");  _hasConfigError = true; }
            if (_emptySprite == null)                  { Debug.LogError("[TableHPBar] EmptySprite 未赋值"); _hasConfigError = true; }
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

        // 自维护血量镜像，不依赖 TableSystem 与本组件的事件回调顺序
        private void HandleLevelStarted(OnLevelStarted e)
        {
            if (_hasConfigError) return;
            _displayHP = _tableSystem.GetMaxHP();
            UpdateDisplay();
        }

        private void HandleMonsterReachedTable(OnMonsterReachedTable e)
        {
            if (_hasConfigError) return;
            _displayHP = Mathf.Max(0, _displayHP - 1);
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            for (int i = 0; i < _hearts.Length; i++)
            {
                if (_hearts[i] == null) continue;
                _hearts[i].sprite = i < _displayHP ? _fullSprite : _emptySprite;
            }
        }
    }
}
