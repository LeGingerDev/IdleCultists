using LGD.Core;
using LGD.Core.Events;
using LGD.Tasks;
using UnityEngine;
namespace LGD.Core.Application
{
    public class MainMenuManager : BaseBehaviour
    {
        public TaskManager _startGameTM;

        [Topic(ApplicationEventIds.ON_PLAY_SELECTED)]
        public void OnStartGameTriggered(object sender)
        {
            StartCoroutine(_startGameTM?.Execute());
        }
    }
}