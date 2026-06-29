using UnityEngine;

namespace GWBGameJam
{
    public class AutoDestroy : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _lifetime = 0.5f;

        private float _timer;

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= _lifetime)
                Destroy(gameObject);
        }
    }
}
