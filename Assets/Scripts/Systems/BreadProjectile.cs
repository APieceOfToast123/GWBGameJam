using UnityEngine;

namespace GWBGameJam
{
    public sealed class BreadProjectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        private bool _hasConfigError;

        private void Awake()
        {
            if (_spriteRenderer != null) return;

            Debug.LogError("[BreadProjectile] SpriteRenderer 未赋值，无法切换投射物图片");
            _hasConfigError = true;
        }

        public void SetSprite(Sprite sprite)
        {
            if (_hasConfigError || sprite == null) return;
            _spriteRenderer.sprite = sprite;
        }
    }
}
