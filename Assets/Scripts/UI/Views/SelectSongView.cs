using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class SelectSongView : View
    {
        [SerializeField] private Button backGameButton;
        [SerializeField] private TextMeshProUGUI pathToFiles;

        [SerializeField] private GameObject songItemPrefab;
        [SerializeField] private GameObject contentGameObject;

        private readonly List<GameObject> _musicItemObjects = new();

        /// <inheritdoc/>
        public override void Initialize()
        {
            backGameButton.onClick.AddListener(MainManager.Instance.ViewManager.ShowLast);
            pathToFiles.text = $"Path to MusicXML files: {MusicXMLFileManager.FolderPath}";
            pathToFiles.color = new Color(0, 0, 0, 0.5f);
        }

        /// <inheritdoc/>
        public override void Show()
        {
            base.Show();
            var musicFileNames = MusicXMLFileManager.GetAllMusicXMLFiles();
            foreach (var musicFile in musicFileNames)
            {
                var item = Instantiate(songItemPrefab, contentGameObject.transform);
                var buttons = item.GetComponentsInChildren<Button>();
                var deleteButton = buttons.FirstOrDefault(b => b.name == "Delete")!;
                var playButton = buttons.FirstOrDefault(b => b.name == "Play")!;
                item.GetComponentInChildren<TextMeshProUGUI>().text = Path.GetFileNameWithoutExtension(musicFile);
                var fileNameCopy = musicFile; // avoid referencing same filename in the lambda function
                playButton.onClick.AddListener(() => OnSongItemPlay(fileNameCopy));
                deleteButton.onClick.AddListener(() => OnSongItemDelete(fileNameCopy, item));
                _musicItemObjects.Add(item);
            }
        }

        private static void OnSongItemPlay(string filename)
        {
            try
            {
                var musicData = MusicXMLFileManager.LoadMusicXML(filename);
                MainManager.Instance.GameManager.MxlData = musicData;
                MainManager.Instance.GameManager.SongName = Path.GetFileNameWithoutExtension(filename);
                MainManager.Instance.ViewManager.Show<SelectGameOptionsView>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error while starting the game: {ex.Message}, {ex.StackTrace}");
                MainManager.Instance.ViewManager.ShowPopUp(ex.Message);
            }
        }

        private void OnSongItemDelete(string filename, GameObject item)
        {
            try
            {
                MusicXMLFileManager.DeleteMusicXML(filename);
                _musicItemObjects.Remove(item);
                Destroy(item);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error while deleting file: {ex.Message}, {ex.StackTrace}");
                MainManager.Instance.ViewManager.ShowPopUp(ex.Message);
            }
        }

        /// <inheritdoc/>
        public override void Hide()
        {
            foreach (var musicItem in _musicItemObjects)
            {
                Destroy(musicItem);
            }

            base.Hide();
        }
    }
}