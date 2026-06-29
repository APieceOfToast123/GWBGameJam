using UnityEngine;
using UnityEngine.UI;

namespace GWBGameJam
{
    public sealed class MainMenuBreadWalker : MonoBehaviour
    {
        [SerializeField] private RectTransform _content;
        [SerializeField] private Image[] _strips;
        [SerializeField] private Sprite _frameA;
        [SerializeField] private Sprite _frameB;
        [SerializeField] private float _moveSpeed = 80f;
        [SerializeField] private float _frameInterval = 0.22f;
        [SerializeField] private float _loopWidth = 700f;

        private Vector2 _startAnchoredPosition;
        private float _frameTimer;
        private bool _useFrameB;

        private void Awake()
        {
            ValidateReferences();
            if (!enabled)
            {
                return;
            }

            _startAnchoredPosition = _content.anchoredPosition;
            ApplyFrame(_frameA);
        }

        private void Update()
        {
            AnimateFrame();
            MoveContent();
        }

        private void ValidateReferences()
        {
            if (_content == null)
            {
                Debug.LogError("[MainMenuBreadWalker] Content reference is missing. Bread walk animation disabled.");
                enabled = false;
                return;
            }

            if (_strips == null || _strips.Length == 0)
            {
                Debug.LogError("[MainMenuBreadWalker] Strip references are missing. Bread walk animation disabled.");
                enabled = false;
                return;
            }

            if (_frameA == null || _frameB == null)
            {
                Debug.LogError("[MainMenuBreadWalker] Bread frame references are missing. Bread walk animation disabled.");
                enabled = false;
                return;
            }

            if (_moveSpeed <= 0f)
            {
                Debug.LogError("[MainMenuBreadWalker] MoveSpeed must be greater than 0. Forced to 80.");
                _moveSpeed = 80f;
            }

            if (_frameInterval <= 0f)
            {
                Debug.LogError("[MainMenuBreadWalker] FrameInterval must be greater than 0. Forced to 0.22.");
                _frameInterval = 0.22f;
            }

            if (_loopWidth <= 0f)
            {
                Debug.LogError("[MainMenuBreadWalker] LoopWidth must be greater than 0. Forced to 700.");
                _loopWidth = 700f;
            }
        }

        private void AnimateFrame()
        {
            _frameTimer += Time.deltaTime;
            if (_frameTimer < _frameInterval)
            {
                return;
            }

            _frameTimer -= _frameInterval;
            _useFrameB = !_useFrameB;
            ApplyFrame(_useFrameB ? _frameB : _frameA);
        }

        private void MoveContent()
        {
            Vector2 position = _content.anchoredPosition;
            position.x += _moveSpeed * Time.deltaTime;

            if (position.x >= _startAnchoredPosition.x + _loopWidth)
            {
                position.x -= _loopWidth;
            }

            _content.anchoredPosition = position;
        }

        private void ApplyFrame(Sprite sprite)
        {
            for (int i = 0; i < _strips.Length; i++)
            {
                if (_strips[i] != null)
                {
                    _strips[i].sprite = sprite;
                }
            }
        }
    }
}
