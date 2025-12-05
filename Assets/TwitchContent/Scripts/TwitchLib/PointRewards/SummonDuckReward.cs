using System.Collections;
using System.Collections.Generic;
using Audio.Core;
using Audio.Managers;
using LGD.Utilities.Extensions;
using ScoredProductions.StreamLinked.EventSub.Events;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using YourNamespace.Twitch;
using Random = UnityEngine.Random;

public class SummonDuckReward : TwitchRewardListener
{
    public Transform leftSideSpawnPoint;
    public Transform rightSideSpawnPoint;
    public GameObject[] duckPrefab;

    public List<GameObject> activeDucks = new List<GameObject>();
    public int maxDucks = 20;

    protected override IEnumerator ProcessRedemptionCoroutine(ChannelPointsCustomRewardRedemptionAdd redemption)
    {
        DuckManager.Instance.RemoveEarliestDuckIfNeeded();
        StartCoroutine(DuckManager.Instance.GenerateNewDuck(redemption.user_name));
        yield return null;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                GameObject duck = activeDucks.Random();
                activeDucks.Remove(duck);
                Destroy(duck);
            }
        }
    }

    public GameObject GetRandomDuck() => activeDucks.Random();
}
