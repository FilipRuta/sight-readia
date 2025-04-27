using System;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using System.IO.Compression;

public static class MusicXMLFileManager
{
    public static string FolderPath { get; } = Path.GetFullPath(
        Path.Combine(Application.persistentDataPath, "SavedMusicXML")
    );
    
    
    /// <summary>
    /// Checks if filename is valid
    /// </summary>
    /// <param name="fileName">Filename to check</param>
    /// <returns></returns>
    private static bool IsValidFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return !string.IsNullOrEmpty(fileName) && !fileName.Any(c => invalidChars.Contains(c));
    }
    
    /// <summary>
    /// Saves a MusicXML file to the persistent storage.
    /// <param name="filename">Filename of song to save</param>
    /// <param name="content">Content of the musicxml file</param>
    /// </summary>
    public static void SaveMusicXML(string filename, string content)
    {
        EnsureDirectoryExists();
        if (!IsValidFileName(filename))
            throw new ArgumentException("Invalid file name");
        
        var fullPath = Path.Combine(FolderPath, filename + ".musicxml");
        if (File.Exists(fullPath))
        {
            Debug.LogError($"MusicXML file '{filename}' already exists!");
            throw new ArgumentException($"MusicXML file '{filename}' already exists!");
        }
        File.WriteAllText(fullPath, content);
        Debug.Log($"Saved MusicXML: {fullPath}");
    }

    /// <summary>
    /// Loads a MusicXML file from the persistent storage.
    /// <param name="filename">Filename of song to load</param>
    /// </summary>
    public static string LoadMusicXML(string filename)
    {
        var fullPath = Path.Combine(FolderPath, filename);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"MusicXML file '{filename}' not found!");
            return null;
        }

        if (filename.EndsWith(".mxl")) // Compressed MusicXML
        {
            return LoadMxl(fullPath);
        }
        return File.ReadAllText(fullPath);
    }
    
    /// <summary>
    /// Loads MusicXML content from a compressed MXL file and returns it as a string.
    /// </summary>
    /// <param name="path">The path to the MXL file.</param>
    /// <returns>The MusicXML content as a string.</returns>
    private static string LoadMxl(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"File not found at path: {path}");
        }

        using (var archive = ZipFile.OpenRead(path))
        {
            foreach (var entry in archive.Entries)
            {
                // Skip META-INF folder entries
                if (entry.FullName.StartsWith("META-INF", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Find the first .xml file in the archive
                if (entry.FullName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    using (var stream = entry.Open())
                    using (var reader = new StreamReader(stream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        throw new InvalidOperationException("No .xml file found in the MXL archive.");
    }
    
    /// <summary>
    /// Load musicxml file from any filepath
    /// </summary>
    /// <param name="filepath">Filepath of song to load</param>
    /// <returns></returns>
    public static string LoadMusicXMLFromFilepath(string filepath)
    {
        var fullPath = Path.GetFullPath(filepath);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"MusicXML file '{fullPath}' not found!");
            return null;
        }

        return File.ReadAllText(fullPath);
    }
    
    /// <summary>
    /// Delete musicxml file
    /// </summary>
    /// <param name="filename">Filename of the song to delete</param>
    public static void DeleteMusicXML(string filename)
    {
        var fullPath = Path.Combine(FolderPath, filename);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"MusicXML file '{fullPath}' not found!");
        }
        File.Delete(fullPath);
    }

    /// <summary>
    /// Returns a list of all MusicXML filenames inside the persistent storage.
    /// </summary>
    public static List<string> GetAllMusicXMLFiles()
    {        
        EnsureDirectoryExists();
        if (!Directory.Exists(FolderPath))
        {
            Debug.LogError("MusicXML folder not found!");
            return new List<string>();
        }
        
        var extensions = new[] { ".mxl", ".musicxml" };
        return Directory.GetFiles(FolderPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Ensures the MusicXML folder exists in persistent storage.
    /// </summary>
    private static void EnsureDirectoryExists()
    {
        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
            Debug.Log("Created folder: " + FolderPath);
        }
    }
}