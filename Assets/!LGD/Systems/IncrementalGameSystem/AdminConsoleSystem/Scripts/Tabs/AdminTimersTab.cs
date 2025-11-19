using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Admin tab for viewing and managing active timers
/// </summary>
public class AdminTimersTab : AdminTabBase
{
    private List<GameTimer> _activeTimers = new List<GameTimer>();

    public override void RefreshData()
    {
        if (TimerManager.Instance != null)
        {
            _activeTimers = TimerManager.Instance.GetActiveTimers();
        }
    }

    public override void DrawTab()
    {
        GUILayout.Label("Timer Management", HeaderStyle);
        GUILayout.Space(5);

        if (TimerManager.Instance == null)
        {
            GUILayout.Label("TimerManager not found.");
            return;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", ButtonStyle))
        {
            RefreshData();
        }
        if (GUILayout.Button("Complete All Timers", ButtonStyle))
        {
            CompleteAllTimers();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        GUILayout.Label($"Active Timers: {_activeTimers.Count}");

        GUILayout.Space(10);

        if (_activeTimers.Count == 0)
        {
            GUILayout.Label("No active timers.");
            return;
        }

        ScrollPosition = GUILayout.BeginScrollView(ScrollPosition, GUILayout.Height(400));

        foreach (var timer in _activeTimers)
        {
            GUILayout.BeginVertical(BoxStyle);

            GUILayout.Label($"Context ID: {timer.contextId}", SubHeaderStyle);
            GUILayout.Label($"Duration: {timer.duration:F2}s");
            GUILayout.Label($"Remaining: {timer.GetTimeRemaining():F2}s");
            GUILayout.Label($"Progress: {(timer.GetProgress() * 100f):F1}%");
            GUILayout.Label($"Is Complete: {timer.IsComplete()}");

            GUILayout.BeginHorizontal();

            if (!timer.IsComplete())
            {
                if (GUILayout.Button("Complete Now", SmallButtonStyle, GUILayout.Width(120)))
                {
                    CompleteTimer(timer);
                }

                if (GUILayout.Button("Add 10s", SmallButtonStyle, GUILayout.Width(80)))
                {
                    AddTimeToTimer(timer, 10f);
                }

                if (GUILayout.Button("Remove 10s", SmallButtonStyle, GUILayout.Width(100)))
                {
                    RemoveTimeFromTimer(timer, 10f);
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        GUILayout.EndScrollView();
    }

    private void CompleteTimer(GameTimer timer)
    {
        // Set elapsed time to duration to complete it
        timer.SetElapsedTime(timer.duration);
        DebugManager.Log($"[Admin] Completed timer: {timer.contextId}");
    }

    private void AddTimeToTimer(GameTimer timer, float seconds)
    {
        timer.AddTime(seconds);
        DebugManager.Log($"[Admin] Added {seconds}s to timer: {timer.contextId}");
    }

    private void RemoveTimeFromTimer(GameTimer timer, float seconds)
    {
        float currentElapsed = timer.GetElapsedTime();
        timer.SetElapsedTime(Mathf.Max(0, currentElapsed + seconds));
        DebugManager.Log($"[Admin] Advanced timer by {seconds}s: {timer.contextId}");
    }

    private void CompleteAllTimers()
    {
        foreach (var timer in _activeTimers)
        {
            if (!timer.IsComplete())
            {
                timer.SetElapsedTime(timer.duration);
            }
        }
        DebugManager.Log("[Admin] Completed all timers");
    }
}
