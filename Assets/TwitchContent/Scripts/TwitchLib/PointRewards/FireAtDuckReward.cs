using System.Collections;
using ScoredProductions.StreamLinked.EventSub.Events;
using YourNamespace.Twitch;

public class FireAtDuckReward : TwitchRewardListener
{
    public FireballCannon _fireballCannon;

    protected override IEnumerator ProcessRedemptionCoroutine(ChannelPointsCustomRewardRedemptionAdd redemption)
    {
        _fireballCannon.TriggerLaunchFireball(redemption.user_name);
        yield return null;
    }


}