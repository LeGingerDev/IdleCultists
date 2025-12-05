using UnityEngine;
using ScoredProductions.StreamLinked.EventSub.Events;
using System.Collections;
using YourNamespace.Twitch;
using System.Collections.Generic;

public class ConfettiCannonReward : TwitchRewardListener
{
    public List<ConfettiCannon> confettiCannons = new List<ConfettiCannon>();

    protected override IEnumerator ProcessRedemptionCoroutine(ChannelPointsCustomRewardRedemptionAdd redemption)
    {
        Debug.Log("Confetti Cannon Reward Triggered!");
        confettiCannons.ForEach(cannon => cannon.TriggerConfetti());
        yield return null;
    }

}