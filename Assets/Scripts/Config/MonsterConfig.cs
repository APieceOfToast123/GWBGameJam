using UnityEngine;
using UnityEngine.Serialization;

namespace GWBGameJam
{
    [CreateAssetMenu(fileName = "MonsterConfig", menuName = "GWBGameJam/Configs/MonsterConfig")]
    public class MonsterConfig : ScriptableObject
    {
        [SerializeField, Min(0.2f), FormerlySerializedAs("MoveIntervalSeconds")]
        private float _moveIntervalSeconds = 1f;
        [SerializeField, Min(4), FormerlySerializedAs("MoveStepCount")]
        private int _moveStepCount = 8;
        [SerializeField, Min(0.05f), FormerlySerializedAs("PendingMoveThreshold")]
        private float _pendingMoveThreshold = 0.5f;
        [SerializeField, FormerlySerializedAs("ScaleCurve")]
        private AnimationCurve _scaleCurve = DefaultScaleCurve();
        [SerializeField, Min(0.05f), FormerlySerializedAs("MoveDuration")]
        private float _moveDuration = 0.3f;
        [SerializeField, Min(1), FormerlySerializedAs("WrongHitFlashCount")]
        private int _wrongHitFlashCount = 2;
        [SerializeField, Min(0.05f), FormerlySerializedAs("WrongHitFlashDuration")]
        private float _wrongHitFlashDuration = 0.1f;
        [SerializeField, Range(1, 4)]
        private int _maxMonstersPerLane = 2;

        public float MoveIntervalSeconds => _moveIntervalSeconds;
        public int MoveStepCount => _moveStepCount;
        public float PendingMoveThreshold => _pendingMoveThreshold;
        public AnimationCurve ScaleCurve => _scaleCurve;
        public float MoveDuration => _moveDuration;
        public int WrongHitFlashCount => _wrongHitFlashCount;
        public float WrongHitFlashDuration => _wrongHitFlashDuration;
        public int MaxMonstersPerLane => _maxMonstersPerLane;

        private void OnValidate() => Validate();

        public void Validate()
        {
            if (_moveDuration >= _moveIntervalSeconds)
            {
                Debug.LogError("[MonsterConfig] MoveDuration 必须小于 MoveIntervalSeconds，已自动修正");
                _moveDuration = _moveIntervalSeconds * 0.3f;
            }
            if (_pendingMoveThreshold >= _moveIntervalSeconds)
            {
                Debug.LogError("[MonsterConfig] PendingMoveThreshold 必须小于 MoveIntervalSeconds，已自动修正");
                _pendingMoveThreshold = _moveIntervalSeconds * 0.5f;
            }
            if (_maxMonstersPerLane < 1 || _maxMonstersPerLane > 4)
            {
                Debug.LogError("[MonsterConfig] MaxMonstersPerLane 必须在 1~4，已强制设为 2");
                _maxMonstersPerLane = 2;
            }
        }

        private static AnimationCurve DefaultScaleCurve()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.01f),
                new Keyframe(2f, 0.50f),
                new Keyframe(5f, 1.00f),
                new Keyframe(7f, 1.50f));
        }
    }
}
