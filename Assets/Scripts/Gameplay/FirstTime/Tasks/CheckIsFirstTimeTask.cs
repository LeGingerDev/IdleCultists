using System.Collections;
using LGD.Tasks;
using UnityEngine;

public class CheckIsFirstTimeTask : TaskBase
{
    [SerializeField]
    private TaskManager _taskManager;

    public override IEnumerator ExecuteInternal()
    {
        if(AchievementManager.Instance.GetUnlockedAchievementCount() != 0)
        {
            _taskManager.Interrupt("Is not first time playing game. Cancelling first time tasks.");
            yield break;
        }
    }


}
