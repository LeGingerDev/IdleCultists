using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerSaveProvider : SaveLoadProviderBase<GameTimer>
{
    protected override string GetSaveFileName()
    {
        return "timers.json";
    }

    protected override IEnumerator CreateDefault()
    {
        _data.Clear();

        // Timers are dynamic - start with empty list
        // No registry to sync with, timers are created at runtime
        Debug.Log($"<color=green>Created default timer data:</color> 0 active timers");

        yield return null;
    }
}