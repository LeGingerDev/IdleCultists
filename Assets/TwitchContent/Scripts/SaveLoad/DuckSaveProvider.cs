using System.Collections;

public class DuckSaveProvider : SaveLoadProviderBase<DuckRuntimeData>
{
    protected override IEnumerator CreateDefault()
    {
        _data.Clear();
        DebugManager.Log($"<color=green>Created default duck data:</color> 0 active ducks");
        yield return null;
    }

    protected override string GetSaveFileName()
    {
        return "twitch_stream_ducks.json";
    }
}

