using UnityEngine;

namespace GWBGameJam
{
    [CreateAssetMenu(fileName = "DoughConfig", menuName = "GWBGameJam/Configs/DoughConfig")]
    public class DoughConfig : ScriptableObject
    {
        [SerializeField, Range(0.01f, 1f)] private float _flourStep = 0.1f;
        [SerializeField, Min(0.01f)] private float _flourFactorMin = 0.5f;
        [SerializeField, Min(0.01f)] private float _flourFactorMax = 2f;
        [SerializeField, Range(0.01f, 2f)] private float _waterSpeed = 0.3f;
        [SerializeField, Min(0.01f)] private float _waterFactorMin = 0.5f;
        [SerializeField, Min(0.01f)] private float _waterFactorMax = 3f;
        [SerializeField, Range(0f, 1f)] private float _initialPos = 0.5f;

        public float FlourStep => _flourStep;
        public float FlourFactorMin => _flourFactorMin;
        public float FlourFactorMax => _flourFactorMax;
        public float WaterSpeed => _waterSpeed;
        public float WaterFactorMin => _waterFactorMin;
        public float WaterFactorMax => _waterFactorMax;
        public float InitialPos => _initialPos;

        public void Validate()
        {
            if (_flourFactorMax < _flourFactorMin)
            {
                Debug.LogError("[DoughConfig] FlourFactorMax 不能小于 FlourFactorMin，已自动修正");
                _flourFactorMax = _flourFactorMin;
            }
            if (_waterFactorMax < _waterFactorMin)
            {
                Debug.LogError("[DoughConfig] WaterFactorMax 不能小于 WaterFactorMin，已自动修正");
                _waterFactorMax = _waterFactorMin;
            }
        }
    }
}
