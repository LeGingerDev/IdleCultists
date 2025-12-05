using UnityEngine;
using Sirenix.OdinInspector;
using System.IO;
using System;

public class ScreenshotUtility : MonoBehaviour
{
    [FoldoutGroup("Settings")]
    [SerializeField, FolderPath(AbsolutePath = true)]
    private string _screenshotFolderPath = "";

    [FoldoutGroup("Settings")]
    [SerializeField]
    private string _screenshotPrefix = "Screenshot";

    [Button("Take Screenshot", ButtonSizes.Large)]
    private void TakeScreenshot()
    {
        if (string.IsNullOrEmpty(_screenshotFolderPath))
        {
            Debug.LogError("Screenshot folder path is not set!");
            return;
        }

        if (!Directory.Exists(_screenshotFolderPath))
        {
            Debug.LogError($"Folder does not exist: {_screenshotFolderPath}");
            return;
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"{_screenshotPrefix}_{timestamp}.png";
        string fullPath = Path.Combine(_screenshotFolderPath, fileName);

        ScreenCapture.CaptureScreenshot(fullPath);
        
        Debug.Log($"Screenshot saved to: {fullPath}");
    }
}