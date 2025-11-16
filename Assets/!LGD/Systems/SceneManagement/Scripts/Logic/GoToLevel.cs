using LGD.Utilities.Attributes;
using UnityEngine;

namespace LGD.SceneManagement.Logic
{
    public class GoToLevel : MonoBehaviour
    {
        [SerializeField, SceneDropdown] private string _levelToGoTo;

        public void LoadLevel()
        {
            SceneManager.Instance.GoToLevel(_levelToGoTo);
        }
    }
}