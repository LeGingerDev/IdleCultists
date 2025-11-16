using LGD.Core;
using LGD.Core.Events;
using LGD.InteractionSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class ClickBase : BaseBehaviour
{
    [SerializeField, FoldoutGroup("ClickEvents")]
    protected UnityEvent onClickDown;
    [SerializeField, FoldoutGroup("ClickEvents")]
    protected UnityEvent onClickUp;


    [Topic(InteractionEventIds.ON_MOUSE_DOWN)]
    public void OnClickDownEvent(object sender, InteractionData clickData)
    {
        if (clickData == null || !clickData.HasTarget)
        {
            return;
        }
        if (clickData.Target != this.transform)
        {
            return;
        }
        
        onClickDown?.Invoke();
        OnMouseDownEvent(clickData);
    }

    [Topic(InteractionEventIds.ON_MOUSE_UP)]
    public void OnClickUpEvent(object sender, InteractionData clickData)
    {
        if (clickData == null || !clickData.HasTarget)
        {
            return;
        }
        if (clickData.Target != this.transform)
        {
            return;
        }

        onClickUp?.Invoke();
        OnMouseUpEvent(clickData);
    }

    public virtual void OnMouseDownEvent(InteractionData clickData)
    {

    }

    public virtual void OnMouseUpEvent(InteractionData clickData)
    {

    }
}
