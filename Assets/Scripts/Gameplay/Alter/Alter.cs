using LargeNumbers;
using LGD.InteractionSystem;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using UnityEngine;

public class Alter : ClickBase
{
    [SerializeField]
    private Resource _resourceGain;

    public override void OnMouseUpEvent(InteractionData clickData)
    {
        base.OnMouseUpEvent(clickData);

        AlphabeticNotation baseAmount = StatManager.Instance.QueryStat(StatType.DevotionPerClick);
        int procMultiplier = GetProcMultiplier();

        switch(procMultiplier)
        {
            case 1:
                Publish(OrbOfDevotionEventIds.ON_ORB_CLICK);
                break;
            case 2:
                Publish(OrbOfDevotionEventIds.ON_ORB_DOUBLE_PROC);
                Debug.Log("Double Devotion Proc!");
                break;
            case 3:
                Publish(OrbOfDevotionEventIds.ON_ORB_TRIPLE_PROC);
                Debug.Log("Triple Devotion Proc!");
                break;
        }

        AddResourceWithMultiplier(baseAmount, procMultiplier);
        InvokeClickEvents(procMultiplier);
    }

    private int GetProcMultiplier()
    {
        if (DidTripleProc()) return 3;
        if (DidDoubleProc()) return 2;
        return 1;
    }

    private void AddResourceWithMultiplier(AlphabeticNotation baseAmount, int multiplier)
    {
        AlphabeticNotation totalAmount = baseAmount * multiplier;
        ResourceManager.Instance.AddResource(_resourceGain, totalAmount);
    }

    private void InvokeClickEvents(int count)
    {
        for (int i = 0; i < count; i++)
        {
            onClickUp?.Invoke();
        }
    }

    private bool DidDoubleProc()
    {
        // Proc chances are percentages, so they stay as AlphabeticNotation but we convert to double for comparison
        AlphabeticNotation chanceNotation = StatManager.Instance.QueryStat(StatType.DoubleDevotionChance);
        float chance = (float)(double)chanceNotation; // Convert to float for percentage check
        return Random.value * 100 < chance;
    }

    private bool DidTripleProc()
    {
        // Proc chances are percentages, so they stay as AlphabeticNotation but we convert to double for comparison
        AlphabeticNotation chanceNotation = StatManager.Instance.QueryStat(StatType.TripleDevotionChance);
        float chance = (float)(double)chanceNotation; // Convert to float for percentage check
        return Random.value * 100 < chance;
    }
}