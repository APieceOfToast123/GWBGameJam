using UnityEngine;
using UnityEngine.Serialization;

namespace GWBGameJam
{
    [CreateAssetMenu(fileName = "BakingConfig", menuName = "GWBGameJam/Configs/BakingConfig")]
    public class BakingConfig : ScriptableObject
    {
        [SerializeField, Min(0.1f), FormerlySerializedAs("_cookedDuration")]
        private float _softestCookDuration = 1.5f;
        [SerializeField, Min(0.1f)]
        private float _mediumCookDuration = 2f;
        [SerializeField, Min(0.1f)]
        private float _hardestCookDuration = 3f;
        [SerializeField, Min(0.1f)]
        private float _perfectWindowDuration = 1f;
        [SerializeField, Min(0.1f)]
        private float _burntWindowDuration = 1f;

        public float SoftestCookDuration => _softestCookDuration;
        public float MediumCookDuration => _mediumCookDuration;
        public float HardestCookDuration => _hardestCookDuration;
        public float PerfectWindowDuration => _perfectWindowDuration;
        public float BurntWindowDuration => _burntWindowDuration;

        private void OnValidate() => Validate();

        public void Validate()
        {
            _softestCookDuration = ValidatePositive(_softestCookDuration, nameof(SoftestCookDuration));
            _mediumCookDuration = ValidatePositive(_mediumCookDuration, nameof(MediumCookDuration));
            _hardestCookDuration = ValidatePositive(_hardestCookDuration, nameof(HardestCookDuration));
            _perfectWindowDuration = ValidatePositive(_perfectWindowDuration, nameof(PerfectWindowDuration));
            _burntWindowDuration = ValidatePositive(_burntWindowDuration, nameof(BurntWindowDuration));
        }

        private static float ValidatePositive(float value, string fieldName)
        {
            if (value >= 0.1f) return value;
            Debug.LogError($"[BakingConfig] {fieldName} 不能小于 0.1，已自动修正");
            return 0.1f;
        }
    }
}
