using LGD.SceneManagement;
using LGD.Utilities.Attributes;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
namespace LGD.Tasks.Generic
{
    public class GoToSceneTask : TaskBase, ITask
    {
        [SerializeField, SceneDropdown] private string _sceneName;

        [SerializeField] private bool _forceWait;

        [SerializeField, ShowIf("@_forceWait")]
        private float _forceDuration;


        public override IEnumerator ExecuteInternal()
        {
            yield return null;
            if (_forceWait)
                yield return new WaitForSeconds(_forceDuration);
            SceneManager.Instance.GoToLevel(_sceneName);
        }
    }
}