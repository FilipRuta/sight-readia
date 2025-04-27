using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    private Camera _camera;
    
    public void Start()
    {
        _camera = GetComponent<Camera>();
    }

    /// <summary>
    /// Changes the zoom level of the camera.
    /// </summary>
    /// <param name="change">The amount to change the orthographic size by.</param>
    public void ChangeZoomLevel(float change)
    {
        _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize + change, 1f, 30f);;
    }
    
    /// <summary>
    /// Moves the camera to a specified x-axis position smoothly.
    /// </summary>
    /// <param name="xPosition">The target x position for the camera.</param>
    public void MoveCameraTo(float xPosition)
    {
        transform.DOKill();
        transform.DOMoveX(xPosition, 0.2f).SetEase(Ease.InOutSine);
    }

    /// <summary>
    /// Adjusts the camera position and zoom level to fit a given range.
    /// </summary>
    /// <param name="left">The left boundary of the area to fit.</param>
    /// <param name="right">The right boundary of the area to fit.</param>
    /// <param name="top">The top boundary of the area to fit.</param>
    /// <param name="bottom">The bottom boundary of the area to fit.</param>
    public void FitCamera(float left, float right, float top, float bottom)
    {
        const int padding = 5;
        const float moveTime = 0.5f; // time in seconds
        
        var targetPosition = _camera.transform.position;
        targetPosition.x = (left + right) / 2f;
        targetPosition.y = (top + bottom) / 2f;
        
        var width = Mathf.Abs(right - left) / 2f;
        var height = Mathf.Abs(top - bottom) / 2f;
        var requiredOrthoSize = Mathf.Max(height + padding, (width + padding) / _camera.aspect);

        // Smooth transition using DOTween
        _camera.transform.DOKill();
        _camera.transform.DOMove(targetPosition, moveTime);
        DOTween.To(() => _camera.orthographicSize, x => _camera.orthographicSize = x, requiredOrthoSize, moveTime);
    }
}
