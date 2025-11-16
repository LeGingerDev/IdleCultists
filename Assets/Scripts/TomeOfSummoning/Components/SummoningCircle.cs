using LGD.Core;
using LGD.Core.Events;
using UnityEngine;
using UnityEngine.Events;

public class SummoningCircle : BaseBehaviour
{
    [SerializeField] private Renderer _circleRenderer;

    // Shader keyword names from your images
    private const string KEYWORD_CHANGECOLOR = "CHANGECOLOR_ON";
    private const string KEYWORD_OFFSETUV = "OFFSETUV_ON";
    private const string KEYWORD_DISTORT = "DISTORT_ON";

    public UnityEvent OnSummoningEffectEnabled;
    public UnityEvent OnSummoningEffectDisabled;

    private void Start()
    {
        DisableSummoningEffect();
    }

    [Topic(SummoningEventIds.ON_ANY_SUMMONING_ACTIVE)]
    public void OnAnySummoningActive(object sender)
    {
        EnableSummoningEffect();
    }

    [Topic(SummoningEventIds.ON_NO_SUMMONING_ACTIVE)]
    public void OnNoSummoningActive(object sender)
    {
        DisableSummoningEffect();
    }

    private void EnableSummoningEffect()
    {
        OnSummoningEffectEnabled?.Invoke();

        if (_circleRenderer == null) return;

        _circleRenderer.material.EnableKeyword(KEYWORD_CHANGECOLOR);
        _circleRenderer.material.EnableKeyword(KEYWORD_OFFSETUV);
        _circleRenderer.material.EnableKeyword(KEYWORD_DISTORT);

        Debug.Log("<color=cyan>Summoning circle shader effects ENABLED</color>");
    }

    private void DisableSummoningEffect()
    {
        OnSummoningEffectDisabled?.Invoke();

        if (_circleRenderer == null) return;

        _circleRenderer.material.DisableKeyword(KEYWORD_CHANGECOLOR);
        _circleRenderer.material.DisableKeyword(KEYWORD_OFFSETUV);
        _circleRenderer.material.DisableKeyword(KEYWORD_DISTORT);

        Debug.Log("<color=cyan>Summoning circle shader effects DISABLED</color>");
    }
}
