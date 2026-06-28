using UnityEngine;

namespace GWBGameJam
{
    public class TableSystem : MonoBehaviour
    {
        [SerializeField] private TableConfig _config;

        private int _currentHP;
        private bool _hasConfigError;

        public int GetCurrentHP() => _currentHP;
        public int GetMaxHP() => _hasConfigError ? 0 : _config.MaxHits;

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError("[TableSystem] TableConfig 未赋值");
                _hasConfigError = true;
                return;
            }
            _config.Validate();
            _currentHP = _config.MaxHits;
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

        private void HandleMonsterReachedTable(OnMonsterReachedTable e)
        {
            if (_hasConfigError || _currentHP <= 0) return;
            _currentHP--;
            if (_currentHP <= 0)
                EventBus<OnTableDestroyed>.Publish(new OnTableDestroyed());
        }

        private void HandleLevelStarted(OnLevelStarted e)
        {
            if (_hasConfigError) return;
            _currentHP = _config.MaxHits;
        }
    }
}
