using Sirenix.OdinInspector;
using ToolTipSystem.Components;
using UnityEngine;
using UnityEngine.UI;

public class AchievementVisualDisplay : ToolTipBase<AchievementRuntimeData>
{
    private const string GREYSCALE_SHADER_KEYWORD = "GREYSCALE_ON";

    [SerializeField, FoldoutGroup("References")]
    private AchievementRuntimeData _runtimeData;
    [SerializeField, FoldoutGroup("References")]
    private Image[] _toggleImages;
    [SerializeField, FoldoutGroup("References")]
    private Image _achievementIcon;

    private Material[] _imageMaterialInstances;

    public override AchievementRuntimeData Data => _runtimeData;

    public void Initialise(AchievementRuntimeData runtimeData)
    {
        _runtimeData = runtimeData;
        AchievementData achievementData = runtimeData.GetData();
        _achievementIcon.sprite = achievementData.icon;
        ToggleAchievement(runtimeData.isUnlocked);
    }

    public void UpdateDisplay()
    {
        ToggleAchievement(_runtimeData.isUnlocked);
    }

    public void ToggleAchievement(bool isUnlocked)
    {
        // Clean up old instances
        CleanupMaterialInstances();

        // Create fresh instances with correct keywords
        _imageMaterialInstances = new Material[_toggleImages.Length];

        for (int i = 0; i < _toggleImages.Length; i++)
        {
            // Create new instance from the original material
            _imageMaterialInstances[i] = new Material(_toggleImages[i].material);

            // Set the keyword on the fresh instance
            if (isUnlocked)
                _imageMaterialInstances[i].DisableKeyword(GREYSCALE_SHADER_KEYWORD);
            else
                _imageMaterialInstances[i].EnableKeyword(GREYSCALE_SHADER_KEYWORD);

            // Assign it to the image
            _toggleImages[i].material = _imageMaterialInstances[i];
        }
    }

    private void CleanupMaterialInstances()
    {
        if (_imageMaterialInstances != null)
        {
            foreach (Material mat in _imageMaterialInstances)
            {
                if (mat != null)
                    Destroy(mat);
            }
        }
    }

    private void OnDestroy()
    {
        CleanupMaterialInstances();
    }
}