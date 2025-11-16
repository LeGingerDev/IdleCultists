using LGD.Core.Events;
using LGD.Core.Singleton;
using LGD.UIElements.ConfirmPopup;
using LGD.Utilities.Extensions;
using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;

namespace LGD.Core.Application
{
    public class ApplicationManager : MonoSingleton<ApplicationManager>
    {
        [SerializeField, FoldoutGroup("Settings")]
        private int _fixedRefreshRate = -1; // You can set this to 120 if you want

        protected override void Awake()
        {
            base.Awake();
            UnityEngine.Application.runInBackground = true;
            SetFixedRefreshRate();
        }

        private void SetFixedRefreshRate()
        {
            UnityEngine.Application.targetFrameRate = _fixedRefreshRate;
            QualitySettings.vSyncCount = 0;
        }

        [Topic(ApplicationEventIds.ON_PAUSED_TOGGLED)]
        public void TogglePause(object sender, bool isPaused)
        {
            ObjectExtensions.FindObjectsOfInterface<IPausable>(true).ToList().ForEach(i =>
            {
                if (isPaused) i.OnPaused();
                else i.OnUnpaused();
            });
        }

        [Topic(ApplicationEventIds.ON_GAME_CLOSED_REQUESTED)]
        public void CloseApplication(object sender)
        {
            ConfirmPopupData closeData = new ConfirmPopupData
            {
                title = "Close Application",
                message = "Are you sure you want to close the application?",
                confirmButtonText = "Yes",
                cancelButtonText = "No",
                onConfirm = () => UnityEngine.Application.Quit(),
                onCancel = () => { }
            };

            ConfirmPopup.Instance.Open(closeData);
        }

    }
}