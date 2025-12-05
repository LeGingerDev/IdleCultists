using System.Collections;
using LGD.Tasks;
using UnityEngine;

public class WaitForTaskManagerTask : TaskBase
{
    [SerializeField]
    private TaskManager _taskManager;

    public override IEnumerator ExecuteInternal()
    {
        yield return _taskManager.Execute();
    }


}
