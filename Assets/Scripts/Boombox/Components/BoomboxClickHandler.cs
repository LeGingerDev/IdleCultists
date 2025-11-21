using Audio.Managers;
using Audio.Models;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Simple click handler for the Boombox game object
/// Opens the Boombox UI when clicked
/// Attach this to the boombox sprite/object in the scene
/// </summary>
public class BoomboxClickHandler : MonoBehaviour
{
   /* [FoldoutGroup("References"), Required]
    [Tooltip("The Boombox UI panel to open when clicked")]
    [SerializeField] private BoomboxUI _boomboxUI;

    [FoldoutGroup("Settings")]
    [Tooltip("Play a sound effect when clicked")]
    [SerializeField] private AudioClipSO _clickSound;

    private void OnMouseDown()
    {
        OpenBoombox();
    }

    /// <summary>
    /// Opens the Boombox UI panel
    /// </summary>
    public void OpenBoombox()
    {
        if (_boomboxUI == null)
        {
            DebugManager.Error("[BoomboxClickHandler] Boombox UI reference is not assigned!");
            return;
        }

        // Play click sound if configured
        if (_clickSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(_clickSound);
        }

        // Open the panel
        _boomboxUI.Open();

        DebugManager.Log("[BoomboxClickHandler] <color=yellow>Boombox UI opened</color>");
    }

    /// <summary>
    /// Closes the Boombox UI panel
    /// </summary>
    public void CloseBoombox()
    {
        if (_boomboxUI != null)
        {
            _boomboxUI.Close();
        }
    }

#if UNITY_EDITOR
    [Button("Test Open Boombox"), FoldoutGroup("Testing")]
    private void TestOpenBoombox()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter play mode to test");
            return;
        }

        OpenBoombox();
    }
#endif*/
}

//Note to AI. Don't need this. I have my own component for opening panels.
