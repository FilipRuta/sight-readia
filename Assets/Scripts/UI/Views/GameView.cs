using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class GameView : View
    {
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pressAnyKeyLabel;
        [SerializeField] private TextMeshProUGUI currentScoreText;
        [SerializeField] private TextMeshProUGUI currentGameModeText;

        [SerializeField] private Button zoomInButton;
        [SerializeField] private Button zoomOutButton;

        [SerializeField] private GameObject timePanel;

        private const float ZoomLevelChange = 1.0f;
        private TextMeshProUGUI _timeNum;
        private bool _isPulsing;
        private Tween _pulseTween;
        
        /// <inheritdoc/>
        public override void Initialize()
        {
            pauseButton.onClick.AddListener(OnPauseClicked);
            GameManager.OnScoreChanged += UpdateScore;
            GameManager.OnTimeRemainingChanged += UpdateTimeRemaining;
            GameManager.OnAnyKeyPressedInPause += HidePressAnyKeyMessage;
            
            zoomInButton.onClick.AddListener(
                () => MainManager.Instance.PlayerManager.CameraController.ChangeZoomLevel(-ZoomLevelChange)
            );
            zoomOutButton.onClick.AddListener(
                () => MainManager.Instance.PlayerManager.CameraController.ChangeZoomLevel(ZoomLevelChange)
            );
            
            _timeNum = timePanel.GetComponentsInChildren<TextMeshProUGUI>().Last();
        }

        private void HidePressAnyKeyMessage()
        {
            pressAnyKeyLabel.SetActive(false);
        }

        /// <inheritdoc/>
        public override void Show()
        {
            base.Show();
            pressAnyKeyLabel.SetActive(true);
            SetVisualsBasedOnGameMode();
            UpdateScore();
            UpdateTimeRemaining();
        }

        private void SetVisualsBasedOnGameMode()
        {
            currentGameModeText.text = $"{MainManager.Instance.GameManager.GameMode}";
            timePanel.SetActive(MainManager.Instance.GameManager.UseTimer);
        }

        protected override void OnEscapePressed()
        {
            OnPauseClicked();
        }

        private void UpdateScore()
        {
            currentScoreText.text = $"{MainManager.Instance.GameManager.Score}";
            currentScoreText.transform.DOKill();
            currentScoreText.transform.DOScale(1.2f, 0.2f)
                .SetLoops(1, LoopType.Yoyo).OnKill(() => currentScoreText.transform.localScale = Vector3.one);
        }

        private void UpdateTimeRemaining()
        {
            const float timeCritical = 1;
            var remaining = MainManager.Instance.GameManager.TimeRemaining;
            _timeNum.text = $"{remaining:F1} s";
            _timeNum.color = remaining > timeCritical ? Color.black : Color.red;
        }

        private void OnPauseClicked()
        {
            MainManager.Instance.GameManager.GameState = GameState.MENU;
            MainManager.Instance.ViewManager.Show<PauseMenuView>();
        }
    }
}