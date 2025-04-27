using System;
using UnityEngine;

public enum GameMode
{
    CLASSIC,
    TRAINING
}

public enum GameState
{
    MENU,
    BEGIN,
    RUNNING,
    PAUSED,
    END,
}

public class GameManager : MonoBehaviour
{
    public bool PlayGrandStaff { get; set; }
    public bool UseTimer { get; set; }
    public bool ShowNoteNamesInNoteHeads { get; set; }
    
    public bool AlwaysShowStaffHead { get; set; }
    public bool TreatChordsAsIndividualNotes { get; set; }

    public string InputDeviceName { get; set; }
    public string OutputDeviceName { get; set; }

    public int HighScore { get; private set; }

    public GameMode GameMode { get; set; }

    public GameState GameState { get; set; }

    public string SongName { get; set; }

    public bool ScoreIsGrandStaff => _gameLogic.MusicScore.IsGrandStaff;

    public bool ScoreIsVisible => GameState is GameState.RUNNING or GameState.PAUSED;

    public static event Action OnScoreChanged;
    public static event Action OnTimeRemainingChanged;
    public static event Action OnAnyKeyPressedInPause;

    private GameLogic _gameLogic;

    public int MaxTimeLimit { get; set; } = 5; // Time in seconds
    private float _timeRemaining;

    public float TimeRemaining
    {
        get => _timeRemaining;
        set
        {
            _timeRemaining = value;
            OnTimeRemainingChanged?.Invoke();
        }
    }

    public string MxlData { get; set; }

    private int _score = 0;

    public int Score
    {
        get => _score;
        private set
        {
            _score = value;
            OnScoreChanged?.Invoke();
        }
    }

    public void Start()
    {
        ScoreDataManager.LoadScores();
        Score = 0;
        GameState = GameState.MENU;
        _gameLogic = GetComponent<GameLogic>();
    }

    /// <summary>
    /// Add amount of score to player
    /// </summary>
    /// <param name="scoreToAdd">Score amount to add</param>
    public void AddScore(int scoreToAdd = 1)
    {
        Score += scoreToAdd;
        Debug.Log($"Score: {Score}");
    }

    /// <summary>
    /// Reset player's score and delete music score
    /// </summary>
    public void ResetGame()
    {
        ResetPlayerScore();
        _gameLogic.MusicScore.DeleteScore();
    }

    /// <summary>
    /// Reset player's score and high score
    /// </summary>
    private void ResetPlayerScore()
    {
        Score = 0;
        HighScore = 0;
    }

    /// <summary>
    /// Save high score of player. Score is saved only if the song is saved and if game was played in timed mode
    /// </summary>
    public void SaveHighScores()
    {
        if (string.IsNullOrEmpty(SongName) || !UseTimer)
            return;
        ScoreDataManager.UpdateHighScore(SongName, GameMode, MaxTimeLimit, Score);
        ScoreDataManager.SaveScores();
        Debug.Log("HighScores saved");
        HighScore = ScoreDataManager.GetHighScore(SongName, GameMode, MaxTimeLimit);
    }

    /// <summary>
    /// Load saved musicxml file
    /// </summary>
    /// <param name="mxlFilePath">Filepath to musicxml file</param>
    public void LoadMxlData(string mxlFilePath)
    {
        MxlData = MusicXMLFileManager.LoadMusicXMLFromFilepath(mxlFilePath);
    }

    /// <summary>
    /// Notifies all listeners that the game was unpaused
    /// </summary>
    public static void GameUnpaused()
    {
        OnAnyKeyPressedInPause?.Invoke();
    }
}