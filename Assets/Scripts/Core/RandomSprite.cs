using UnityEngine;
using UnityEngine.UI;

namespace GWBGameJam
{
    public class RandomSprite : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Image _image;
        [SerializeField] private Sprite[] _sprites;

        private void Awake()
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
            if (_image == null)
                _image = GetComponent<Image>();

            if (_sprites == null || _sprites.Length == 0)
                return;

            Sprite chosen = _sprites[Random.Range(0, _sprites.Length)];
            if (_renderer != null)
                _renderer.sprite = chosen;
            if (_image != null)
                _image.sprite = chosen;
        }
    }
}
