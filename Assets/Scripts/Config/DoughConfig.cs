using UnityEngine;
using UnityEngine.Serialization;

namespace GWBGameJam
{
    [CreateAssetMenu(fileName = "DoughConfig", menuName = "GWBGameJam/Configs/DoughConfig")]
    public class DoughConfig : ScriptableObject
    {
        [SerializeField, Min(0.1f), FormerlySerializedAs("_flourClickAmount")]
        private float _flourClickMin = 0.5f;
        [SerializeField, Min(0.1f)]
        private float _flourClickMax = 1f;
        [SerializeField, Min(0.1f), FormerlySerializedAs("WaterFillRate")]
        private float _waterFillRate = 0.5f;
        [SerializeField, Min(0.1f)]
        private float _waterSpeedMultiplierMin = 1f;
        [SerializeField, Min(0.1f)]
        private float _waterSpeedMultiplierMax = 3f;
        [SerializeField, Min(0f), FormerlySerializedAs("InitialRatio")]
        private float _initialRatio = 1f;
        [SerializeField, Min(0.1f), FormerlySerializedAs("MaxRatio")]
        private float _maxRatio = 3f;

        public float FlourClickMin => _flourClickMin;
        public float FlourClickMax => _flourClickMax;
        public float WaterFillRate => _waterFillRate;
        public float WaterSpeedMultiplierMin => _waterSpeedMultiplierMin;
        public float WaterSpeedMultiplierMax => _waterSpeedMultiplierMax;
        public float InitialRatio => _initialRatio;
        public float MaxRatio => _maxRatio;

        public void Validate()
        {
            if (_flourClickMax < _flourClickMin)
            {
                Debug.LogError("[DoughConfig] FlourClickMax 不能小于 FlourClickMin，已自动修正");
                _flourClickMax = _flourClickMin;
            }
            if (_waterSpeedMultiplierMax < _waterSpeedMultiplierMin)
            {
                Debug.LogError("[DoughConfig] WaterSpeedMultiplierMax 不能小于 WaterSpeedMultiplierMin，已自动修正");
                _waterSpeedMultiplierMax = _waterSpeedMultiplierMin;
            }
        }
    }
}
