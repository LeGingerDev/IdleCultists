using LGD.Core;
using LGD.Core.Events;
using LGD.InteractionSystem;
using UnityEngine;

public class HoverBase : BaseBehaviour
{
    private bool _wasHovering;

    private void Start()
    {
        SetHighlighting(false);
    }

    [Topic(InteractionEventIds.ON_INTERACTION_HOVER_ENTERED)]
    public void OnHoverEnter(object sender, InteractionData data)
    {
        if (data == null || !data.HasTarget)
        {
            ToggleHighlighting(false);
            return;
        }

        if (data.Target != this.transform)
        {
            ToggleHighlighting(false);
            return;
        }

        ToggleHighlighting(true);
    }

    [Topic(InteractionEventIds.ON_INTERACTION_HOVER_EXITED)]
    public void OnHoverExit(object sender, InteractionData data)
    {
        
        ToggleHighlighting(false);
    }

    public virtual void OnHoverStart()
    {

    }

    public virtual void OnHoverEnd()
    {

    }

    public void SetHighlighting(bool state)
    {
        _wasHovering = state;
        if (state)
            OnHoverStart();
        else
            OnHoverEnd();
    }

    public void ToggleHighlighting(bool currentState)
    {
        if (_wasHovering == currentState)
            return;

        _wasHovering = currentState;
        if (currentState)
            OnHoverStart();
        else
            OnHoverEnd();
    }
}
