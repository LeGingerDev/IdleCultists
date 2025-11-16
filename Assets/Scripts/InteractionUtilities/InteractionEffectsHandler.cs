using UnityEngine;
using Sirenix.OdinInspector;

public class InteractionEffectsHandler : MonoBehaviour
{
    [FoldoutGroup("Quick Add Effects")]
    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    private void AddHoverScale()
    {
        AddComponentIfMissing<TransformHoverScale>();
    }

    [FoldoutGroup("Quick Add Effects")]
    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    private void AddClickScale()
    {
        AddComponentIfMissing<TransformClickScale>();
    }

    [FoldoutGroup("Quick Add Effects")]
    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    private void AddSineWaveEffect()
    {
        AddComponentIfMissing<TransformSineWaveEffect>();
    }

    [FoldoutGroup("Quick Add Effects")]
    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    private void AddParticleClickEffect()
    {
        AddComponentIfMissing<ParticleSystemClickEffect>();
    }

    [FoldoutGroup("Quick Add Effects")]
    [Button(ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    private void AddSpriteHoverOutline()
    {
        AddComponentIfMissing<SpriteRendererHoverOutline>();
    }

    [FoldoutGroup("Quick Add Effects")]
    [Button(ButtonSizes.Large), GUIColor(0.3f, 1f, 0.3f)]
    private void AddAllCommonEffects()
    {
        AddComponentIfMissing<TransformHoverScale>();
        AddComponentIfMissing<TransformClickScale>();
        Debug.Log("Added common effects to " + gameObject.name);
    }

    [FoldoutGroup("Remove Effects")]
    [Button(ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
    private void RemoveHoverScale()
    {
        RemoveComponentIfExists<TransformHoverScale>();
    }

    [FoldoutGroup("Remove Effects")]
    [Button(ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
    private void RemoveClickScale()
    {
        RemoveComponentIfExists<TransformClickScale>();
    }

    [FoldoutGroup("Remove Effects")]
    [Button(ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
    private void RemoveSineWaveEffect()
    {
        RemoveComponentIfExists<TransformSineWaveEffect>();
    }

    [FoldoutGroup("Remove Effects")]
    [Button(ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
    private void RemoveParticleClickEffect()
    {
        RemoveComponentIfExists<ParticleSystemClickEffect>();
    }

    [FoldoutGroup("Remove Effects")]
    [Button(ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f)]
    private void RemoveSpriteHoverOutline()
    {
        RemoveComponentIfExists<SpriteRendererHoverOutline>();
    }

    [FoldoutGroup("Remove Effects")]
    [Button(ButtonSizes.Medium), GUIColor(1f, 0.3f, 0.3f)]
    private void RemoveAllEffects()
    {
        RemoveComponentIfExists<TransformHoverScale>();
        RemoveComponentIfExists<TransformClickScale>();
        RemoveComponentIfExists<TransformSineWaveEffect>();
        RemoveComponentIfExists<ParticleSystemClickEffect>();
        RemoveComponentIfExists<SpriteRendererHoverOutline>();
        Debug.Log("Removed all effects from " + gameObject.name);
    }

    [FoldoutGroup("Info")]
    [Button(ButtonSizes.Medium), GUIColor(0.8f, 0.8f, 0.8f)]
    private void ShowCurrentEffects()
    {
        string effectsList = "Effects on " + gameObject.name + ":\n";

        if (GetComponent<TransformHoverScale>() != null)
            effectsList += "- TransformHoverScale\n";

        if (GetComponent<TransformClickScale>() != null)
            effectsList += "- TransformClickScale\n";

        if (GetComponent<TransformSineWaveEffect>() != null)
            effectsList += "- TransformSineWaveEffect\n";

        if (GetComponent<ParticleSystemClickEffect>() != null)
            effectsList += "- ParticleSystemClickEffect\n";

        if (GetComponent<SpriteRendererHoverOutline>() != null)
            effectsList += "- SpriteRendererHoverOutline\n";

        if (effectsList == "Effects on " + gameObject.name + ":\n")
            effectsList += "No effects attached";

        Debug.Log(effectsList);
    }

    private void AddComponentIfMissing<T>() where T : Component
    {
        if (gameObject.GetComponent<T>() == null)
        {
            gameObject.AddComponent<T>();
            Debug.Log($"Added {typeof(T).Name} to {gameObject.name}");
        }
        else
        {
            Debug.Log($"{typeof(T).Name} already exists on {gameObject.name}");
        }
    }

    private void RemoveComponentIfExists<T>() where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component != null)
        {
            DestroyImmediate(component);
            Debug.Log($"Removed {typeof(T).Name} from {gameObject.name}");
        }
        else
        {
            Debug.Log($"{typeof(T).Name} doesn't exist on {gameObject.name}");
        }
    }
}