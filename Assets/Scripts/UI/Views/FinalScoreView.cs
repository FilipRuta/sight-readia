using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class FinalScoreView : View
    {
        [SerializeField] private Button restartButton;

        [SerializeField] private Button saveSongButton;

        [SerializeField] private Button exitToMainMenuButton;

        [SerializeField] private TextMeshProUGUI currentScoreText;

        [SerializeField] private TextMeshProUGUI topScoreText;

        [SerializeField] private TextMeshProUGUI gameModeText;

        /// <inheritdoc/>
        public override void Initialize()
        {
            restartButton.onClick.AddListener(OnRestartClicked);
            saveSongButton.onClick.AddListener(() => MainManager.Instance.ViewManager.Show<SaveSongView>());
            exitToMainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        /// <inheritdoc/>
        protected override void OnEscapePressed()
        {
        }

        private void OnRestartClicked()
        {
            MainManager.Instance.GameManager.ResetGame();
            MainManager.Instance.GameManager.GameState = GameState.BEGIN;
            MainManager.Instance.ViewManager.ShowLast();
        }

        /// <inheritdoc/>
        public override void Show()
        {
            MainManager.Instance.GameManager.GameState = GameState.MENU;
            base.Show();
            SetLabels();
        }
        
        private void SetLabels()
        {
            currentScoreText.text = $"Score: {MainManager.Instance.GameManager.Score}";
            if (MainManager.Instance.GameManager.UseTimer)
                if (string.IsNullOrEmpty(MainManager.Instance.GameManager.SongName))
                    topScoreText.text = "Save song to track top score.";
                else
                    topScoreText.text = $"Top Score: {MainManager.Instance.GameManager.HighScore}";
            else
                topScoreText.text = "Top Score only in timed mode.";
            gameModeText.text = MainManager.Instance.GameManager.GameMode.ToString();
        }
        
        private void OnMainMenuClicked()
        {
            MainManager.Instance.GameManager.ResetGame();
            MainManager.Instance.ViewManager.Show<MainMenuView>(clearHistory: true);
        }
    }
}