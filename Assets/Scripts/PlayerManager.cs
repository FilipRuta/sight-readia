using System;
using InputControllers;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public IInputController InputController { get; private set; }

    public CameraController CameraController { get; private set; }

    private GameManager _gameManager;
    private GameObject _line;
    private Vector3 _startingPos;

    private void Start()
    {
        _gameManager = MainManager.Instance.GameManager;
        _startingPos = transform.position;
        CameraController = GetComponent<CameraController>();
        SetupLineScroller();
    }

    /// <summary>
    /// Sets the input controller based on the provided device name.
    /// </summary>
    /// <param name="inputDeviceName">The name of the input device.</param>
    public void SetInputController(string inputDeviceName)
    {
        if (InputController != null && inputDeviceName == InputController.Name)
            return; // Same controller, dont recreate
        if (InputController is MonoBehaviour monoBehaviour)
        {
            Destroy(monoBehaviour); // Destroy the old input controller
        }
        Type newInputControllerType;
        if (inputDeviceName == Constants.DefaultInputDevice)
            newInputControllerType = typeof(KeyboardInputController);
        else
            newInputControllerType = typeof(MidiInputController);

        InputController = (IInputController)gameObject.AddComponent(newInputControllerType);
        InputController.Initialize(inputDeviceName);
    }
    
    /// <summary>
    /// Sets up the scrolling line indicator.
    /// </summary>
    private void SetupLineScroller()
    {
        _line = Instantiate(MainManager.Instance.PrefabLoader.GetPrefab("Line"), transform, worldPositionStays: true);
        _line.SetActive(false);
        const int overlap = 100;
        var linePositions = new Vector3[]
        {
            new(0, overlap, 0),
            new(0,  -overlap - 4, 0)
        };
        _line.GetComponent<LineRenderer>().SetPositions(linePositions);
        ToggleLineVisibility(false);
    }

    /// <summary>
    /// Toggles the visibility of the scrolling line.
    /// </summary>
    /// <param name="show">True to show the line, false to hide it.</param>
    private void ToggleLineVisibility(bool show)
    {
        _line.SetActive(show);
    }

    /// <summary>
    /// Resets the player to the starting position, adjusting Y position for grand staff mode if necessary.
    /// </summary>
    public void ResetPlayer()
    {
        transform.position = _startingPos;
        if (_gameManager.PlayGrandStaff && _gameManager.ScoreIsGrandStaff)
            transform.position = new Vector3(_startingPos.x, -(Constants.SpaceBetweenStaves / 2 + Constants.LineSpacing * 4), _startingPos.z);
    }
    
    void Update()
    {
        ToggleLineVisibility(_gameManager.GameState == GameState.RUNNING && _gameManager.GameMode == GameMode.CLASSIC);
        if (!_gameManager.ScoreIsVisible)
            return;
        
        // Check if any key is pressed
        if (_gameManager.GameState == GameState.PAUSED && (Input.anyKeyDown || InputController.AnyNotesPlayed))
        {
            _gameManager.GameState = GameState.RUNNING;
            GameManager.GameUnpaused();
        }
    }
}
