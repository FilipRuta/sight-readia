using UnityEngine;
using System.Collections.Generic;

public class PrefabLoader: MonoBehaviour
{
    private readonly Dictionary<string, GameObject> _loadedPrefabs = new ();

    /// <summary>
    /// Get prefab by its name. Prefabs once loaded are cached for faster access.
    /// </summary>
    /// <param name="prefabName">Name of the prefab</param>
    /// <returns></returns>
    public GameObject GetPrefab(string prefabName)
    {
        if (_loadedPrefabs.ContainsKey(prefabName))
            return _loadedPrefabs[prefabName];
        
        var prefab = Resources.Load<GameObject>($"Prefabs/{prefabName}");
        if (prefab == null)
        {
            Debug.LogError($"Failed to load prefab: {prefabName}");
            return null;
        }
        _loadedPrefabs[prefabName] = prefab;
        return _loadedPrefabs[prefabName];
    }
}