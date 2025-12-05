using Audio.Core;
using Audio.Managers;
using LGD.Core;
using LGD.Core.Events;
using LGD.UIElements.ToastSystem;

public class AchievementPopupHandler : BaseBehaviour
{
    [Topic(AchievementEventIds.ON_ACHIEVEMENT_UNLOCKED)]
    public void OnAchievementUnlocked(object sender, AchievementRuntimeData achievement)
    {

        AchievementData data = achievement.GetData();

        ToastData toastData = new ToastData()
        {
            type = ToastType.Success,
            message = $"Achievement Unlocked:\n{data.title}",
            icon = data.icon,
        };

        AudioManager.Instance.PlaySFX(AudioConstIds.ACHIEVEMENT_UNLOCKED, true);

        ToastManager.Instance.SpawnToast(toastData);
    }
}