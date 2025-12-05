using LGD.Core;
using LGD.Core.Events;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class ConfettiCannon : BaseBehaviour
{

    [SerializeField]
    private UnityEvent _onConfettiTriggered;

    [Topic(EntityEventIds.ON_ENTITY_SPAWNED)]
    [Button]
    public void OnEntitySpawned(object sender, EntityRuntimeData entity, bool fromLoading)
    {
        if (fromLoading)
            return;
        _onConfettiTriggered?.Invoke();
    }

    public void TriggerConfetti()
    {
        _onConfettiTriggered?.Invoke();
    }
}
