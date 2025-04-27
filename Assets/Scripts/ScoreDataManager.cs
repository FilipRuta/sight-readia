using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class ScoreData
{
    public Dictionary<string, Dictionary<GameMode, Dictionary<int, int>>> SongHighScores = new();
}

public static class DictionaryExtensions
{
    /// <summary>
    /// Gets the value for the specified key or adds a new value created by the value factory.
    /// </summary>
    /// <typeparam name="TKey">The type of the dictionary key.</typeparam>
    /// <typeparam name="TValue">The type of the dictionary value.</typeparam>
    /// <param name="dict">The dictionary to operate on.</param>
    /// <param name="key">The key to retrieve or add.</param>
    /// <param name="valueFactory">The function to create a new value if the key does not exist.</param>
    /// <returns>The existing or newly created value.</returns>
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key,
        Func<TValue> valueFactory)
    {
        if (!dict.TryGetValue(key, out TValue value))
        {
            value = valueFactory();
            dict[key] = value;
        }

        return value;
    }
}

public static class ScoreDataManager
{
    private static ScoreData _data;
    private static readonly string _path = Application.persistentDataPath + "/scores.json";

    /// <summary>
    /// Saves the current score data to a JSON file at persistent path.
    /// </summary>
    public static void SaveScores()
    {
        try
        {
            var dirPath = Path.GetDirectoryName(_path);
            if (dirPath == null)
            {
                Debug.LogError($"Error saving scores to {_path}");
                return;
            }

            Directory.CreateDirectory(dirPath);
            var json = JsonConvert.SerializeObject(_data); // Pretty-print JSON
            File.WriteAllText(_path, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"{e.Message}, {e.StackTrace}");
        }
    }

    /// <summary>
    /// Retrieves the high score for a specific song, game mode, and time limit.
    /// </summary>
    /// <param name="songFilename">The filename of the song.</param>
    /// <param name="gameMode">The game mode.</param>
    /// <param name="maxTimeLimit">The maximum time limit.</param>
    /// <returns>The highest recorded score for the given parameters.</returns>
    public static int GetHighScore(string songFilename, GameMode gameMode, int maxTimeLimit)
    {
        return _data?.SongHighScores
                   .TryGetValue(songFilename, out var songModes) == true
               && songModes.TryGetValue(gameMode, out var timeLimitScores) == true
               && timeLimitScores.TryGetValue(maxTimeLimit, out var highScore)
            ? highScore
            : 0;
    }

    /// <summary>
    /// Loads score data from the JSON file.
    /// </summary>
    /// <returns>True if the scores were successfully loaded, otherwise false.</returns>
    public static bool LoadScores()
    {
        try
        {
            if (File.Exists(_path))
            {
                var json = File.ReadAllText(_path);
                _data = string.IsNullOrEmpty(json)
                    ? new ScoreData()
                    : JsonConvert.DeserializeObject<ScoreData>(json);
            }
            else
                _data = new ScoreData();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading scores: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Updates the high score for a specific song, game mode, and time limit.
    /// </summary>
    /// <param name="songFilename">The filename of the song.</param>
    /// <param name="gameMode">The game mode.</param>
    /// <param name="maxTimeLimit">The maximum time limit.</param>
    /// <param name="newScore">The new high score to update.</param>
    public static void UpdateHighScore(string songFilename, GameMode gameMode, int maxTimeLimit, int newScore)
    {
        if (_data == null)
        {
            var success = LoadScores();
            if (!success)
                return;
        }

        var songScores =
            _data!.SongHighScores.GetOrAdd(songFilename, () => new Dictionary<GameMode, Dictionary<int, int>>());
        var modeScores = songScores.GetOrAdd(gameMode, () => new Dictionary<int, int>());

        if (!modeScores.TryGetValue(maxTimeLimit, out var currentHighScore) || newScore > currentHighScore)
        {
            modeScores[maxTimeLimit] = newScore;
        }
    }
}