using System;
using System.Collections.Generic;
using System.Linq;
using UI.Views;
using SheetMusic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
    public MusicScore MusicScore {get; private set;}
    private GameManager _gameManager;
    
    public void Start()
    {
        _gameManager = MainManager.Instance.GameManager;
    }
    
    /// <summary>
    /// Retrieves the notes to be played after the current note. 
    /// If the note holds a chord notes, all chord notes are returned.
    /// </summary>
    /// <param name="note">The current note to find next notes for.</param>
    /// <returns>An enumerable of MIDI codes for the next notes to be played.</returns>
    private static IEnumerable<int> GetNextNotes(Note note)
    {
        // If the note is not part of a chord, return the note itself
        if (MainManager.Instance.GameManager.TreatChordsAsIndividualNotes || !note.IsChord)
            return new List<int> { note.MidiCode };
        
        // Retrieve chord notes (non-null, as IsChord ensures this is valid)
        var chordNotes = note.GetChordNotes()!;
        return chordNotes.Select(n => n.MidiCode);
    }
    
    /// <summary>
    /// Checks the correctly played notes by the player.
    /// </summary>
    /// <returns>
    /// Returns a tuple indicating whether all notes were played correctly and the set of correctly played notes
    /// as the midi codes.
    /// </returns>
    private (bool AllPlayedCorrectly, HashSet<int> CorrectlyPlayedNotes) GetCorrectlyPlayedNotes()
    {
        var correctlyPlayedNotes = new HashSet<int>();        
        var notesToBePlayed = new HashSet<int>();
        var notesPlayedByPlayer = MainManager.Instance.PlayerManager.InputController.NotesBeingPlayed().ToList();
        
        if (notesPlayedByPlayer.Count == 0)
            return (false, null);
        
        foreach (var note in MusicScore.UpcomingNotes)
        {
            notesToBePlayed.UnionWith(GetNextNotes(note));
            correctlyPlayedNotes.UnionWith((notesToBePlayed).Intersect(notesPlayedByPlayer));
        }
        
        var allPlayedCorrectly = notesToBePlayed.SetEquals(notesPlayedByPlayer);

        return (allPlayedCorrectly, correctlyPlayedNotes);
    }
    
    /// <summary>
    /// Processes the notes played by the player, checking for correctness and updating the score.
    /// </summary>
    /// <returns>True if all notes were played correctly, otherwise false.</returns>
    private bool ProcessPlayedNotes()
    {
        if (MusicScore.WaitForRelease)  // wait till all keys are released before checking next note
        {
            if (MainManager.Instance.PlayerManager.InputController.AnyNotesPlayed)
                return false;
            MusicScore.WaitForRelease = false;
        }

        var playedCorrectly = GetCorrectlyPlayedNotes();
        if (playedCorrectly.CorrectlyPlayedNotes == null)
        {
            MusicScore.ResetNextNoteVisuals();
            return playedCorrectly.AllPlayedCorrectly;
        }
        MusicScore.VisualizePlayedNotes(playedCorrectly.CorrectlyPlayedNotes);
        if (playedCorrectly.AllPlayedCorrectly)
            _gameManager.AddScore(playedCorrectly.CorrectlyPlayedNotes.Count());
        
        return playedCorrectly.AllPlayedCorrectly;
    }

    /// <summary>
    /// Moves to the next note in the score.
    /// </summary>
    private void MoveToNextNote()
    {
        if (MusicScore.UpcomingNotes is { Count: > 0 })
            MainManager.Instance.PlayerManager.CameraController.MoveCameraTo(MusicScore.GetXPositionOfUpcomingNote());
    }

    /// <summary>
    /// Resets the game timer.
    /// </summary>
    private void ResetTimer()
    {
        _gameManager.TimeRemaining = _gameManager.MaxTimeLimit;
    }
    
    /// <summary>
    /// Updates the game timer. If time runs out, ends the game.
    /// </summary>
    private void UpdateTimer()
    {
        if (_gameManager.TimeRemaining > 0)
        {
            _gameManager.TimeRemaining -= Time.deltaTime;
        }
        else
        {
            // Time's up
            _gameManager.TimeRemaining = 0;
            ShowEndScreen();
        }
    }
    
    /// <summary>
    /// Handles the Free Play game mode where the game advances based on notes being played correctly
    /// in chronological order.
    /// </summary>
    private void HandleFreePlay(bool allPlayedCorrectly)
    {
        if (allPlayedCorrectly)
        {
            MusicScore.UpdateUpcomingSymbol();
            MoveToNextNote();
            MusicScore.WaitForRelease = true;
            ResetTimer();
        }

        if (_gameManager.UseTimer)
        {
            UpdateTimer();
        }

    }

    /// <summary>
    /// Handles the Training game mode, where individual notes within the measure are highlighted in random order.
    /// </summary>
    private void HandleStaticMeasureTraining(bool allPlayedCorrectly)
    {
        if (allPlayedCorrectly)
        {
            MusicScore.UpdateUpcomingTrainingNoteAndRender();
            ResetTimer();
            MusicScore.WaitForRelease = true;
        }
        if (!MainManager.Instance.PlayerManager.InputController.AnyNotesPlayed)
            MusicScore.HighlightNextNotes();
        
        if (_gameManager.UseTimer)
        {
            UpdateTimer();
        }
    }

    private void GamePreparationErrorMessage(string errorMessage)
    {
        _gameManager.GameState = GameState.END;
        MainManager.Instance.ViewManager.Show<MainMenuView>(clearHistory: true);
        MainManager.Instance.ViewManager.ShowPopUp(errorMessage);
    }
    
    /// <summary>
    /// Prepares the game by parsing the music score, initializing devices, and setting the game state.
    /// </summary>
    private void PrepareGame()
    {
        var parser = new MusicXMLParser();
        try
        {
            MusicScore = parser.Parse(_gameManager.MxlData, _gameManager.PlayGrandStaff);
        }
        catch (FormatException e)
        {
            GamePreparationErrorMessage($"Error while parsing the Mxl file. ({e.Message})");
            Debug.LogError($"{e.Message}, {e.StackTrace}");
            return;
        }
        catch (Exception e)
        {
            GamePreparationErrorMessage($"Error while parsing the Mxl file.");
            Debug.LogError($"{e.Message}, {e.StackTrace}");
            return;
        }

        try
        {
            MainManager.Instance.PlayerManager.SetInputController(_gameManager.InputDeviceName);
            MainManager.Instance.PlayerManager.ResetPlayer();
            MainManager.Instance.DeviceConnector.SetInputDevice(MainManager.Instance.PlayerManager.InputController.GetInputDevice());
        }
        catch (Exception e)
        {
            GamePreparationErrorMessage(
                "Error while setting up the input device. Make sure no other application uses the device."
            );
            Debug.LogError($"{e.Message}, {e.StackTrace}");
            return;
        }
        
        try {
            MainManager.Instance.DeviceConnector.SetOutputDevice(_gameManager.OutputDeviceName);
            MainManager.Instance.DeviceConnector.CreateConnection();
        }
        catch (Exception e)
        {
            GamePreparationErrorMessage(
                "Error while setting up the input device. Make sure no other application uses the device."
            );
            Debug.LogError($"{e.Message}, {e.StackTrace}");
            return;
        }
        
        
        _gameManager.TimeRemaining = _gameManager.MaxTimeLimit;
        _gameManager.TreatChordsAsIndividualNotes = false;
        
        GameManager.OnAnyKeyPressedInPause -= MoveToNextNote;
        try
        {
            switch (_gameManager.GameMode)
            {
                case GameMode.TRAINING:
                    _gameManager.AlwaysShowStaffHead = true;
                    _gameManager.TreatChordsAsIndividualNotes = true;
                    MusicScore.UpdateUpcomingTrainingNoteAndRender();
                    break;
                case GameMode.CLASSIC:
                    MusicScore.UpdateUpcomingSymbol();
                    MusicScore.RenderWholeScore(_gameManager.PlayGrandStaff);
                    GameManager.OnAnyKeyPressedInPause += MoveToNextNote;
                    break;
            }
        }
        catch (Exception e)
        {
            GamePreparationErrorMessage("Error during game preparation.");
            Debug.LogError($"{e.Message}, {e.StackTrace}");
            return;
        }
        Debug.Log("Game started successfully.");
        _gameManager.GameState = GameState.PAUSED;
    }
    
    /// <summary>
    /// Displays the end screen with the final score when the game ends.
    /// </summary>
    private void ShowEndScreen()
    {
        _gameManager.GameState = GameState.END;
        _gameManager.SaveHighScores();
        MainManager.Instance.ViewManager.Show<FinalScoreView>();
    }
    
    public void Update()
    {
        if (_gameManager.GameState == GameState.BEGIN)
        {
            PrepareGame();
        }
        if (_gameManager.GameState != GameState.RUNNING)
        {
            MainManager.Instance.DeviceConnector.Disconnect();
            return;
        }
        MainManager.Instance.DeviceConnector.Connect();

        if (MusicScore.EndOfScore)
        {
            ShowEndScreen();
            return;
        }
        var allPlayedCorrectly = ProcessPlayedNotes();
        switch (_gameManager.GameMode)
        {
            case GameMode.CLASSIC:
                HandleFreePlay(allPlayedCorrectly);
                break;
            case GameMode.TRAINING:
                HandleStaticMeasureTraining(allPlayedCorrectly);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}