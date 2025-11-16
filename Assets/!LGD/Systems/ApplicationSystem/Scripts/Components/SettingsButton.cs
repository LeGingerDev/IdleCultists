using UnityEngine;

namespace LGD.Core.Application
{

    /// <summary>
    /// This is just to trigger the settings. You can attach this to a button or check for the escape key.
    /// </summary>
    public class SettingsButton : BaseBehaviour
    {
        public PanelType panelType = PanelType.Settings;
        public bool isCheckingKeyPress;

        private void Update()
        {
            if (!isCheckingKeyPress) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OpenSettings(true);
            }
        }

        public void OpenSettings(bool isByKey = false)
        {
            Publish(PanelEventIds.ON_PANEL_OPEN_REQUESTED, panelType);
        }
    }
}