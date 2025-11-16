using System;

[Serializable]
public class RoomRuntimeData
{
    public string roomId;
    public bool isUnlocked;
    public DateTime timeUnlocked;

    public RoomRuntimeData(string id)
    {
        roomId = id;
        isUnlocked = false;
        timeUnlocked = DateTime.MinValue;
    }

    public void Unlock()
    {
        isUnlocked = true;
        timeUnlocked = DateTime.Now;
    }
}