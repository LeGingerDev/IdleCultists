using LGD.Utilities.Attributes;
using UnityEngine;
namespace LGD.Core.Application
{
    public class ApplicationEventsButton : BaseBehaviour
    {
        [SerializeField, ConstDropdown(typeof(ApplicationEventIds))]
        private string _eventID;

        public void TriggerEvent()
        {
            Publish(_eventID);
        }
    }
}