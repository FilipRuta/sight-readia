using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class MainMenuView : View
    {
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button selectSongButton;
        [SerializeField] private Button exitGameButton;
        [SerializeField] private Button helpButton;
    
        [SerializeField] private TextMeshProUGUI hintText;
    

        private const string URL = "https://www.wikihow.com/Read-Music";
        
        /// <inheritdoc/>
        public override void Initialize()
        {
            startGameButton.onClick.AddListener(() => MainManager.Instance.ViewManager.Show<SelectGameOptionsView>());
            selectSongButton.onClick.AddListener(() => MainManager.Instance.ViewManager.Show<SelectSongView>());
            exitGameButton.onClick.AddListener(Application.Quit);
            helpButton.onClick.AddListener( () => Application.OpenURL(URL));
            hintText.text = $"Opens {URL}";
            var color = hintText.color;
            color.a = 0.0f;
            hintText.color = color;
        }

        /// <inheritdoc/>
        public override void Show()
        {
            base.Show();
            // Remove MXL data
            MainManager.Instance.GameManager.MxlData = null;
        }
    }
}
