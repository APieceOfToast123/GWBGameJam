using System.Collections.Generic;
using UnityEngine;

namespace GWBGameJam
{
    public class MonsterSystem : MonoBehaviour
    {
        private const int LaneCount = 5;

        [SerializeField] private MonsterConfig _config;
        [SerializeField] private LaneManager _laneManager;
        [SerializeField] private Transform _monsterContainer;
        [SerializeField] private MonsterController _monsterPrefab;

        private readonly List<MonsterController>[] _monstersByLane =
            new List<MonsterController>[LaneCount];
        private bool _hasConfigError;

        private void Awake()
        {
            for (int i = 0; i < LaneCount; i++)
                _monstersByLane[i] = new List<MonsterController>();
            ValidateConfig();
        }

        private void ValidateConfig()
        {
            if (_config == null) { Debug.LogError("[MonsterSystem] MonsterConfig 未赋值"); _hasConfigError = true; }
            if (_laneManager == null) { Debug.LogError("[MonsterSystem] LaneManager 未赋值"); _hasConfigError = true; }
            if (_monsterContainer == null) { Debug.LogError("[MonsterSystem] MonsterContainer Transform 未赋值"); _hasConfigError = true; }
            if (_monsterPrefab == null) { Debug.LogError("[MonsterSystem] MonsterController Prefab 未赋值"); _hasConfigError = true; }
            if (!_hasConfigError)
                _config.Validate();
        }

        private void OnEnable()
        {
            EventBus<OnLevelStarted>.Subscribe(HandleLevelStarted);
        }

        private void OnDestroy()
        {
            EventBus<OnLevelStarted>.Unsubscribe(HandleLevelStarted);
        }

        public bool SpawnMonster(int laneIndex, MonsterData data)
        {
            if (_hasConfigError || !IsValidLane(laneIndex) || IsLaneFull(laneIndex)) return false;
            if (data == null)
            {
                Debug.LogWarning($"[MonsterSystem] SpawnMonster: lane {laneIndex} 的 MonsterData 为 null");
                return false;
            }

            data.ValidateTargetDoughState();
            MonsterController monster = Instantiate(_monsterPrefab, _monsterContainer);
            monster.Initialize(laneIndex, data, _config, _laneManager);
            GetLaneList(laneIndex).Add(monster);
            EventBus<OnMonsterSpawned>.Publish(new OnMonsterSpawned(laneIndex, data));
            return true;
        }

        public IReadOnlyList<MonsterController> GetMonstersInLane(int laneIndex)
        {
            if (!IsValidLane(laneIndex)) return System.Array.Empty<MonsterController>();
            List<MonsterController> monsters = GetLaneList(laneIndex);
            var snapshot = new List<MonsterController>(monsters);
            snapshot.Sort((a, b) => b.PositionIndex.CompareTo(a.PositionIndex));
            return snapshot;
        }

        public MonsterController GetMonsterInLane(int laneIndex)
        {
            IReadOnlyList<MonsterController> monsters = GetMonstersInLane(laneIndex);
            return monsters.Count > 0 ? monsters[0] : null;
        }

        public int GetMonsterCountInLane(int laneIndex)
        {
            return IsValidLane(laneIndex) ? GetLaneList(laneIndex).Count : 0;
        }

        public bool IsLaneOccupied(int laneIndex) => GetMonsterCountInLane(laneIndex) > 0;

        public bool IsLaneFull(int laneIndex)
        {
            return IsValidLane(laneIndex)
                   && GetMonsterCountInLane(laneIndex) >= _config.MaxMonstersPerLane;
        }

        public void DefeatMonster(MonsterController monster)
        {
            if (monster == null || !IsValidLane(monster.LaneIndex)) return;
            if (!GetLaneList(monster.LaneIndex).Remove(monster)) return;

            monster.Defeat();
        }

        public void TriggerWrongHitFeedback(MonsterController monster)
        {
            if (monster != null)
                monster.TriggerWrongHitFeedback();
        }

        public Vector2 GetTargetPosition(int laneIndex)
        {
            MonsterController monster = GetMonsterInLane(laneIndex);
            return monster != null ? monster.GetTargetPosition() : Vector2.zero;
        }

        private List<MonsterController> GetLaneList(int laneIndex)
        {
            List<MonsterController> monsters = _monstersByLane[laneIndex];
            monsters.RemoveAll(monster => monster == null);
            return monsters;
        }

        private static bool IsValidLane(int laneIndex) => laneIndex >= 0 && laneIndex < LaneCount;

        private void HandleLevelStarted(OnLevelStarted e)
        {
            for (int laneIndex = 0; laneIndex < LaneCount; laneIndex++)
            {
                List<MonsterController> monsters = GetLaneList(laneIndex);
                for (int i = monsters.Count - 1; i >= 0; i--)
                {
                    if (monsters[i] != null)
                        Destroy(monsters[i].gameObject);
                }
                monsters.Clear();
            }
        }
    }
}
