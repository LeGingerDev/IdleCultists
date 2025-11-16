using System.Collections;
using UnityEngine;
namespace LGD.Tasks.Generic
{
    public class WaitForTimeTask : TaskBase
    {
        [SerializeField]
        private float _timeToWait = 1f;
        public override IEnumerator ExecuteInternal()
        {
            yield return new WaitForSeconds(_timeToWait);
        }
    }
}