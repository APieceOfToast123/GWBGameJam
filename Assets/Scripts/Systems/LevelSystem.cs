using System.Collections.Generic;
using UnityEngine;

namespace GWBGameJam
{
    public class LevelSystem : MonoBehaviour
    {
        [SerializeField] private LevelConfig _levelConfig;
        [SerializeField] private MonsterSystem _monsterSystem;
        [SerializeField] private MonsterData[] _availableMonsterTypes;

        private LevelData _currentLevelData;
        private int _currentLevelIndex;
        private int _spawnedCount;
        private int _exitedCount;
        private float _spawnTimer;
        private bool _isPlayingState;
        private bool _isSpawningActive;
        private bool _hasConfigError;

        public int GetExitedCount() => _exitedCount;
        public int GetTotalMonsters() => _currentLevelData != null ? _currentLevelData.TotalMonsters : 0;
        public int GetCurrentLevelIndex() => _currentLevelIndex;

        private void Awake()
        {
            ValidateConfig();
        }

        private void ValidateConfig()
        {
            if (_levelConfig == null)
            {
                Debug.LogError("[LevelSystem] LevelConfig 未赋值");
                _hasConfigError = true;
            }
            if (_monsterSystem == null)
            {
                Debug.LogError("[LevelSystem] MonsterSystem 未赋值");
                _hasConfigError = true;
            }
            if (_availableMonsterTypes == null || _availableMonsterTypes.Length == 0)
            {
                Debug.LogError("[LevelSystem] _availableMonsterTypes 为空，请在 Inspector 中配置怪物类型");
                _hasConfigError = true;
            }
            if (_levelConfig != null && _levelConfig.LevelCount == 0)
            {
                Debug.LogError("[LevelSystem] LevelConfig.Levels 为空");
                _hasConfigError = true;
            }
        }

        private void OnEnable()
        {
            EventBus<OnLevelStarted>.Subscribe(HandleLevelStarted);
            EventBus<OnMonsterDefeated>.Subscribe(HandleMonsterDefeated);
            EventBus<OnMonsterReachedTable>.Subscribe(HandleMonsterReachedTable);
            EventBus<OnGameStateChanged>.Subscribe(HandleGameStateChanged);
        }

        private void OnDestroy()
        {
            EventBus<OnLevelStarted>.Unsubscribe(HandleLevelStarted);
            EventBus<OnMonsterDefeated>.Unsubscribe(HandleMonsterDefeated);
            EventBus<OnMonsterReachedTable>.Unsubscribe(HandleMonsterReachedTable);
            EventBus<OnGameStateChanged>.Unsubscribe(HandleGameStateChanged);
        }

        private void Update()
        {
            if (_hasConfigError || !_isPlayingState || !_isSpawningActive) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _currentLevelData.SpawnIntervalSeconds)
                TrySpawn();
        }

        private void TrySpawn()
        {
            if (_spawnedCount >= _currentLevelData.TotalMonsters)
            {
                _isSpawningActive = false;
                return;
            }

            var available = new List<int>();
            for (int i = 0; i < 5; i++)
                if (!_monsterSystem.IsLaneFull(i))
                    available.Add(i);

            if (available.Count == 0)
            {
                _spawnTimer = 0f;
                return;
            }

            int laneIndex = available[Random.Range(0, available.Count)];
            MonsterData data = _availableMonsterTypes[Random.Range(0, _availableMonsterTypes.Length)];

            bool spawned = _monsterSystem.SpawnMonster(laneIndex, data);
            if (spawned)
                _spawnedCount++;
            else
                Debug.LogWarning($"[LevelSystem] SpawnMonster({laneIndex}) 返回 false，本次生成跳过");

            _spawnTimer = 0f;
        }

        private void CheckLevelCleared()
        {
            if (_exitedCount >= _currentLevelData.TotalMonsters)
                EventBus<OnLevelCleared>.Publish(new OnLevelCleared());
        }

        private void HandleLevelStarted(OnLevelStarted e)
        {
            if (_hasConfigError) return;

            _currentLevelIndex = e.LevelIndex;
            if (_currentLevelIndex >= _levelConfig.LevelCount)
            {
                Debug.LogWarning($"[LevelSystem] levelIndex {_currentLevelIndex} 越界，使用最后一关数据");
                _currentLevelIndex = _levelConfig.LevelCount - 1;
            }

            _currentLevelData = _levelConfig.GetLevel(_currentLevelIndex);
            _spawnedCount = 0;
            _exitedCount = 0;
            // 初始化为一个完整间隔，使进入 PLAYING 的第一帧立即触发首次生成（t=0 出怪）
            _spawnTimer = _currentLevelData.SpawnIntervalSeconds;
            _isSpawningActive = true;
        }

        private void HandleMonsterDefeated(OnMonsterDefeated e)
        {
            if (_currentLevelData == null || _exitedCount >= _currentLevelData.TotalMonsters) return;
            _exitedCount++;
            CheckLevelCleared();
        }

        private void HandleMonsterReachedTable(OnMonsterReachedTable e)
        {
            if (_currentLevelData == null || _exitedCount >= _currentLevelData.TotalMonsters) return;
            _exitedCount++;
            CheckLevelCleared();
        }

        private void HandleGameStateChanged(OnGameStateChanged e)
        {
            _isPlayingState = e.NewState == GameState.Playing;
        }
    }
}
