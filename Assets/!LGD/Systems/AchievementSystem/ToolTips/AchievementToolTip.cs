using Sirenix.OdinInspector;
using TMPro;
using ToolTipSystem.Components;
using UnityEngine;
using UnityEngine.UI;

public class AchievementToolTip : ToolTip<AchievementRuntimeData>
{
    [SerializeField, FoldoutGroup("References")]
    private Image _backgroundImage;
    [SerializeField, FoldoutGroup("References")]
    private Color _unlockedColor;
    [SerializeField, FoldoutGroup("References")]
    private Color _lockedColor;
    [SerializeField, FoldoutGroup("References")]
    private TextMeshProUGUI _achievementNameText;
    [SerializeField, FoldoutGroup("References")]
    private TextMeshProUGUI _achievementDescriptionText;
    [SerializeField, FoldoutGroup("References")]
    private GameObject _isLockedVisual;

    public override void Show(AchievementRuntimeData runtimeData)
    {
        AchievementData data = runtimeData.GetData();
        _achievementNameText.text = data.title;
        _achievementDescriptionText.text = data.description;
        _isLockedVisual.SetActive(!runtimeData.isUnlocked);
        _backgroundImage.color = runtimeData.isUnlocked ? _unlockedColor : _lockedColor;
    }

}
