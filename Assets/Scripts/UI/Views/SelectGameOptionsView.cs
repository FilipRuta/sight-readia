using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PythonCommunication;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UI.Views
{
    public class SelectGameOptionsView : View
    {
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button backGameButton;

        [SerializeField] private GameObject chooseScaleGroup;
        [SerializeField] private GameObject timerDropdownObject;
        [SerializeField] private GameObject loadingAnimation;

        [SerializeField] private TMP_Dropdown gameModeDropdown;
        [SerializeField] private TMP_Dropdown inputDeviceDropdown;
        [SerializeField] private TMP_Dropdown outputDeviceDropdown;
        [SerializeField] private TMP_Dropdown chooseScaleDropdown;
        [SerializeField] private TMP_Dropdown timerDropdown;

        [SerializeField] private Toggle playGrandStaffToggle;
        [SerializeField] private Toggle showNoteHeadNamesToggle;
        [SerializeField] private Toggle alwaysShowStaffHeadToggle;
        [SerializeField] private Toggle timerToggle;

        [SerializeField] private TextMeshProUGUI grandStaffText;
        [SerializeField] private TextMeshProUGUI grandStaffSubnote;

        private static readonly Dictionary<string, int> ScaleToFifths = new()
        {
            { "C", 0 },
            { "G", 1 },
            { "D", 2 },
            { "A", 3 },
            { "E", 4 },
            { "B", 5 },
            { "F#", 6 },
            { "C#", 7 },
            { "F", -1 },
            { "Bb", -2 },
            { "Eb", -3 },
            { "Ab", -4 },
            { "Db", -5 },
            { "Gb", -6 },
            { "Cb", -7 }
        };

        private static readonly List<int> TimerOptions = new() { 3, 5, 10, 20 };

        private CanvasGroup _canvasGroup;
        private bool _isGenerating;

        /// <inheritdoc/>
        public override void Initialize()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            backGameButton.onClick.AddListener(MainManager.Instance.ViewManager.ShowLast);
            gameModeDropdown.onValueChanged.AddListener(OnGameModeDropdownValueChanged);
            timerToggle.onValueChanged.AddListener(ShowTimerDropdownElement);
            loadingAnimation.SetActive(false);

            playGrandStaffToggle.isOn = MainManager.Instance.GameManager.PlayGrandStaff;
            timerToggle.isOn = MainManager.Instance.GameManager.UseTimer;
            alwaysShowStaffHeadToggle.isOn = MainManager.Instance.GameManager.AlwaysShowStaffHead;
            
            ShowTimerDropdownElement(MainManager.Instance.GameManager.UseTimer);

            SetGameModeDropdownOptions();
            SetDeviceDropdownOptions();
            SetScaleDropdownOptions();
            SetTimerDropdownOptions();
        }

        /// <inheritdoc/>
        protected override void OnEscapePressed()
        {
            if (!_isGenerating)
                base.OnEscapePressed();
        }

        private void ShowTimerDropdownElement(bool isOn)
        {
            timerDropdownObject.SetActive(isOn);
        }

        private void SetGrandStaffText()
        {
            if (MainManager.Instance.GameManager.MxlData == null)
            {
                grandStaffText.text = "Generate grand staff";
                grandStaffSubnote.text = "";
            }
            else
            {
                grandStaffText.text = "Play grand staff";
                grandStaffSubnote.text = "(If song has 2 staves)";
            }
        }

        private void SetTimerDropdownOptions()
        {
            timerDropdown.ClearOptions();
            timerDropdown.AddOptions(TimerOptions.Select(t => $"{t} sec").ToList());
        }

        private void SetGameModeDropdownOptions()
        {
            gameModeDropdown.ClearOptions();
            var options = Enum.GetNames(typeof(GameMode));
            gameModeDropdown.AddOptions(new List<string>(options));
            MainManager.Instance.GameManager.GameMode = (GameMode)gameModeDropdown.value;
        }

        private void SetDeviceDropdownOptions()
        {
            inputDeviceDropdown.ClearOptions();
            outputDeviceDropdown.ClearOptions();
            var inputOptions = MainManager.Instance.DeviceConnector.GetInputDevices();
            inputDeviceDropdown.AddOptions(inputOptions);
            var outputOptions = MainManager.Instance.DeviceConnector.GetOutputDevices();
            outputDeviceDropdown.AddOptions(outputOptions);
        }

        private void SetScaleDropdownOptions()
        {
            chooseScaleDropdown.ClearOptions();
            var options = new List<string> { "Random" };
            options.AddRange(ScaleToFifths.Keys.ToList());
            chooseScaleDropdown.AddOptions(options);
        }

        /// <inheritdoc/>
        public override void Show()
        {
            base.Show();
            SetGrandStaffText();
            startGameButton.onClick.RemoveAllListeners();
            if (MainManager.Instance.GameManager.MxlData == null)
            {
                startGameButton.onClick.AddListener(GenerateAndStart);
                chooseScaleGroup.SetActive(true);
            }
            else
            {
                chooseScaleGroup.SetActive(false);
                startGameButton.onClick.AddListener(StartGame);
            }
        }

        private void OnGameModeDropdownValueChanged(int index)
        {
            var selectedMode = (GameMode)index;
            MainManager.Instance.GameManager.GameMode = selectedMode;
            // Training has always visible staff head by default
            alwaysShowStaffHeadToggle.interactable = selectedMode != GameMode.TRAINING;
        }

        private async Task<ServerResponse> RequestMusicData(int fifths, bool generateGrandStaff)
        {
            return await MainManager.Instance.PythonConnector.SendRequestToGenerator(
                ServerMessages.SEND_MUSICXML,
                new Dictionary<string, dynamic>
                {
                    { "fifths", fifths },
                    { "grand_staff", generateGrandStaff }
                }
            );
        }

        private async void GenerateAndStart()
        {
            try
            {
                MainManager.Instance.GameManager.SongName = null;
                _canvasGroup.interactable = false;
                loadingAnimation.SetActive(true);
                var fifthsOption = chooseScaleDropdown.options[chooseScaleDropdown.value].text;
                var fifths = fifthsOption == "Random" ? Random.Range(-7, 8) : ScaleToFifths[fifthsOption];
                var generateGrandStaff = playGrandStaffToggle.isOn;
                await MainManager.Instance.PythonConnector.ConnectWithTimeout();
                _isGenerating = true;
                var responseData = await RequestMusicData(fifths, generateGrandStaff);
                if (responseData.statusCode != 200)
                {
                    MainManager.Instance.ViewManager.ShowPopUp(responseData.content);
                    return;
                }

                try
                {
                    MainManager.Instance.GameManager.MxlData = responseData.content;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error when loading MXL data: {ex.Message}, {ex.StackTrace}");
                    MainManager.Instance.ViewManager.ShowPopUp(ex.Message);
                    return;
                }

                StartGame();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error when generating song: {ex.Message}, {ex.StackTrace}");
                MainManager.Instance.ViewManager.ShowPopUp(ex.Message);
            }
            finally
            {
                _canvasGroup.interactable = true;
                _isGenerating = false;
                loadingAnimation.SetActive(false);
            }
        }

        private void StartGame()
        {
            try
            {
                MainManager.Instance.GameManager.InputDeviceName =
                    inputDeviceDropdown.options[inputDeviceDropdown.value].text;
                MainManager.Instance.GameManager.OutputDeviceName =
                    outputDeviceDropdown.options[outputDeviceDropdown.value].text;

                MainManager.Instance.GameManager.PlayGrandStaff = playGrandStaffToggle.isOn;
                MainManager.Instance.GameManager.ShowNoteNamesInNoteHeads = showNoteHeadNamesToggle.isOn;
                MainManager.Instance.GameManager.UseTimer = timerToggle.isOn;
                MainManager.Instance.GameManager.MaxTimeLimit = TimerOptions[timerDropdown.value];
                MainManager.Instance.GameManager.AlwaysShowStaffHead = alwaysShowStaffHeadToggle.isOn;

                MainManager.Instance.GameManager.GameState = GameState.BEGIN;
                MainManager.Instance.ViewManager.Show<GameView>(clearHistory: true, remember: false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error while starting the game: {ex.Message}, {ex.StackTrace}");
                MainManager.Instance.ViewManager.ShowPopUp(ex.Message);
            }
        }
    }
}