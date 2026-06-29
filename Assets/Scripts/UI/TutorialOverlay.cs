using UnityEngine;
using UnityEngine.UI;

namespace GWBGameJam
{
    public class TutorialOverlay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _tutorialCanvas;
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeBackdropButton;

        [Header("Startup")]
        [SerializeField] private bool _showOnStart = true;

        private float _previousTimeScale = 1f;
        private GameState _currentGameState = GameState.Playing;
        private bool _isOpen;
        private bool _hasConfigError;

        private void Awake()
        {
            ValidateConfig();

            if (_tutorialCanvas != null)
                _tutorialCanvas.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus<OnGameStateChanged>.Subscribe(HandleGameStateChanged);

            if (_openButton != null)
                _openButton.onClick.AddListener(OpenTutorial);

            if (_closeBackdropButton != null)
                _closeBackdropButton.onClick.AddListener(CloseTutorial);
        }

        private void Start()
        {
            if (_showOnStart)
                OpenTutorial();
        }

        private void OnDisable()
        {
            EventBus<OnGameStateChanged>.Unsubscribe(HandleGameStateChanged);

            if (_openButton != null)
                _openButton.onClick.RemoveListener(OpenTutorial);

            if (_closeBackdropButton != null)
                _closeBackdropButton.onClick.RemoveListener(CloseTutorial);

            if (_isOpen)
                RestoreTimeScale();
        }

        public void OpenTutorial()
        {
            if (_hasConfigError || _isOpen)
                return;

            _previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;

            _tutorialCanvas.SetActive(true);
            _isOpen = true;
        }

        public void CloseTutorial()
        {
            if (_hasConfigError || !_isOpen)
                return;

            _tutorialCanvas.SetActive(false);
            RestoreTimeScale();
        }

        private void ValidateConfig()
        {
            if (_tutorialCanvas == null)
            {
                Debug.LogError("[TutorialOverlay] TutorialCanvas is not assigned.");
                _hasConfigError = true;
            }

            if (_openButton == null)
            {
                Debug.LogError("[TutorialOverlay] OpenButton is not assigned.");
                _hasConfigError = true;
            }

            if (_closeBackdropButton == null)
            {
                Debug.LogError("[TutorialOverlay] CloseBackdropButton is not assigned.");
                _hasConfigError = true;
            }
        }

        private void HandleGameStateChanged(OnGameStateChanged e)
        {
            _currentGameState = e.NewState;
        }

        private void RestoreTimeScale()
        {
            if (_currentGameState != GameState.Paused)
                Time.timeScale = _previousTimeScale > 0f ? _previousTimeScale : 1f;

            _isOpen = false;
        }
    }
}
