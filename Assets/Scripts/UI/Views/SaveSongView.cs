using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class SaveSongView : View
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Button saveButton;

        [SerializeField] private TMP_InputField inputText;
        [SerializeField] private TextMeshProUGUI saveStatusText;

        private TextMeshProUGUI _saveButtonText;
        
        /// <inheritdoc/>
        public override void Initialize()
        {
            backButton.onClick.AddListener(MainManager.Instance.ViewManager.ShowLast);
            saveButton.onClick.AddListener(SaveMusicFile);
            _saveButtonText = saveButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        /// <inheritdoc/>
        public override void Show()
        {
            base.Show();
            saveStatusText.text = "";

            _saveButtonText.text = "Save";
            saveButton.interactable = true;
        }

        private void SaveMusicFile()
        {
            saveStatusText.text = "";
            if (MainManager.Instance.GameManager.MxlData == null)
            {
                saveStatusText.text = "No musicxml data available";
                return;
            }

            if (string.IsNullOrEmpty(inputText.text))
            {
                saveStatusText.text = "Enter a filename";
                return;
            }

            try
            {
                var songName = inputText.text;
                MusicXMLFileManager.SaveMusicXML(songName, MainManager.Instance.GameManager.MxlData);
                MainManager.Instance.GameManager.SongName = songName;
                MainManager.Instance.GameManager.SaveHighScores();
                
                _saveButtonText.text = "Saved!";
                saveButton.interactable = false;
            }
            catch (Exception e)
            {
                saveStatusText.text = e.Message;
            }
        }
    }
}