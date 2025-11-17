using LargeNumbers;
using LGD.Core.Events;
using LGD.ResourceSystem.Managers;
using LGD.ResourceSystem.Models;
using LGD.UIelements.Panels;
using LGD.Utilities.Data;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TomeOfSummoningPanel : SlidePanel
{
    [FoldoutGroup("Summoning")]
    [SerializeField, FoldoutGroup("Summoning/References")]
    private List<SummonPurchasableBlueprint> _purchasables = new List<SummonPurchasableBlueprint>();


    // The panel no longer instantiates displays. Displays should be placed as children
    // of `_summonDisplayParent` and wired up in their own components. We still keep
    // a list of child displays for optional panel-level tracking.
    private List<SummoningPurchasableDisplay> _createdDisplays = new();

    protected override void Start()
    {
        base.Start();
        // Collect any child displays (no instantiation).
        _createdDisplays = GetComponentsInChildren<SummoningPurchasableDisplay>(true).ToList();
    }
    protected override void OnClose()
    {
    }

    protected override void OnOpen()
    {
    }
}