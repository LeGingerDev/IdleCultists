using LGD.Tasks;
using System.Collections;
using UnityEngine;
namespace LGD.SceneManagement.Tasks
{
    public class WaitForSplashToFinish : TaskBase
    {
        private bool _hasSplashScreenFinished;

        public override IEnumerator ExecuteInternal()
        {
            _hasSplashScreenFinished = false;
            SceneManager.OnSceneLoadingFinished += SceneManager_OnSceneLoadingFinished;
            yield return new WaitUntil(() => _hasSplashScreenFinished);
            SceneManager.OnSceneLoadingFinished -= SceneManager_OnSceneLoadingFinished;
        }

        private void SceneManager_OnSceneLoadingFinished()
        {
            _hasSplashScreenFinished = true;
        }
    }
}
