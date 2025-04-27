using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.Views
{
    public class PauseMenuView : View
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button saveSongButton;
        [SerializeField] private Button exitToMainMenuButton;
    
        [SerializeField] private TextMeshProUGUI songNameText;
        
        /// <inheritdoc/>
        public override void Initialize()
        {
            resumeButton.onClick.AddListener(OnResumeClicked);
            restartButton.onClick.AddListener(OnRestartClicked);
            saveSongButton.onClick.AddListener(() => MainManager.Instance.ViewManager.Show<SaveSongView>());
            exitToMainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        /// <inheritdoc/>
        public override void Show()
        {
            MainManager.Instance.GameManager.GameState = GameState.MENU;
            songNameText.text = MainManager.Instance.GameManager.SongName;
            if (songNameText.text is { Length: > 25 })
                songNameText.text = songNameText.text[..25] + "...";
            base.Show();
        }

        protected override void OnEscapePressed()
        {
            OnResumeClicked();
        }

        private void OnRestartClicked()
        {
            MainManager.Instance.GameManager.ResetGame();
            MainManager.Instance.GameManager.GameState = GameState.BEGIN;
            MainManager.Instance.ViewManager.ShowLast();
        } 

        private void OnResumeClicked()
        {
            MainManager.Instance.GameManager.GameState = GameState.PAUSED;
            MainManager.Instance.ViewManager.ShowLast();
        }
    
        private void OnMainMenuClicked()
        {
            MainManager.Instance.GameManager.GameState = GameState.MENU;
            MainManager.Instance.GameManager.ResetGame();
            MainManager.Instance.ViewManager.Show<MainMenuView>(clearHistory: true);
        }
    }
}
