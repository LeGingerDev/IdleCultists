using LGD.Core;
using UnityEngine;

public class TimeSetterManager : BaseBehaviour
{
    [SerializeField, Range(0.01f, 10f)] private float _baseFixedDelta = 0.02f;
    [SerializeField, Range(0.1f, 10f)] private float _timeIncrement = 0.5f;
    [SerializeField] private KeyCode _toggleKey = KeyCode.Space;

    private float _currentScale = 1f;
    private bool _showUI = true;
    private float _simulatedTime; // Tracks time passed accounting for timeScale
    private Rect _windowRect; // For dragging support

    private void Start()
    {
        ApplyTimeScale(_currentScale);
        // Default position (bottom-left)
        _windowRect = new Rect(10, Screen.height - 110, 340, 90);
    }

    private void Update()
    {
        // Update simulated time with scaled delta
        _simulatedTime += Time.deltaTime;

        // Toggle UI
        if (Input.GetKeyDown(_toggleKey))
            _showUI = !_showUI;

        // Listen for number keys 0–9 (top row)
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(i.ToString()))
            {
                SetTimeByKey(i);
                break;
            }
        }
    }

    private void SetTimeByKey(int keyNumber)
    {
        // Example: Key 1 -> 1x, Key 2 -> 1.5x, ..., Key 0 -> 5x
        _currentScale = keyNumber == 0 ? 5f : 1f + (keyNumber - 1) * _timeIncrement;
        ApplyTimeScale(_currentScale);
    }

    private void ApplyTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = _baseFixedDelta * Time.timeScale;
    }

    private void OnGUI()
    {
        if (!_showUI)
            return;

        // Draw the draggable window
        _windowRect = GUI.Window(0, _windowRect, DrawWindow, "⏱ Time Controller");
    }

    private void DrawWindow(int id)
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = Color.white }
        };

        int totalSeconds = Mathf.FloorToInt(_simulatedTime);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        string timeFormatted = $"{hours:00}:{minutes:00}:{seconds:00}";

        GUILayout.BeginVertical();

        GUILayout.Label($"Time Scale: x{_currentScale:0.0}", labelStyle);
        GUILayout.Label($"Simulated Time: {timeFormatted}", labelStyle);
        GUILayout.Label($"[Press '{_toggleKey}' to Hide]", labelStyle);

        GUILayout.EndVertical();

        // Makes the whole window draggable
        GUI.DragWindow();
    }
}
