using OutputDeviceHandling;
using PythonCommunication;
using UI;
using UnityEngine;

/// <summary>
/// Main manager serves as a facade pattern for holding access to other managers in a single place.
/// </summary>
public class MainManager : MonoBehaviour
{
    public static MainManager Instance;

    public GameManager GameManager { get; private set; }
    public PlayerManager PlayerManager { get; private set; }
    public PythonConnector PythonConnector { get; private set; }
    public ViewManager ViewManager { get; private set; }
    public DeviceConnector DeviceConnector { get; private set; }
    public PrefabLoader PrefabLoader { get; private set; }

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        GameManager = GetComponentInChildren<GameManager>();
        PlayerManager = GetComponentInChildren<PlayerManager>();
        PythonConnector = GetComponentInChildren<PythonConnector>();
        ViewManager = GetComponentInChildren<ViewManager>();
        DeviceConnector = GetComponentInChildren<DeviceConnector>();
        PrefabLoader = GetComponentInChildren<PrefabLoader>();
    }
}
