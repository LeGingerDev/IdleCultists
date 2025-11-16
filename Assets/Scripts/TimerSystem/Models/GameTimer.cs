using LGD.Utilities.Data;
using System;

[Serializable]
public class GameTimer
{
    public string timerId;
    public string contextId;
    public float maxTime;
    public float timeRemaining;
    public bool IsComplete => timeRemaining <= 0f;

    public GameTimer(string contextId, float maxDuration)
    {
        timerId = Guid.NewGuid().ToString();
        this.contextId = contextId;
        maxTime = maxDuration;
        timeRemaining = maxDuration;
    }

    public void Tick(float deltaTime)
    {
        if (timeRemaining > 0f)
            timeRemaining -= deltaTime;
    }

    public ValueChange GetValueChange()
    {
        return new ValueChange(timeRemaining, maxTime);
    }
}