using DG.Tweening;
using UnityEngine;

public class SummoningScriptEffectsHandler : MonoBehaviour
{
[SerializeField]
private float _opacityDuration = 0.2f;
    private SpriteRenderer _spriteRenderer;

    

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetOpacity(float opacity)
    {
        //use dotween to fade the sprite to the target opacity over the specified duration
        if (_spriteRenderer != null)
        {
            Color targetColor = _spriteRenderer.color;
            targetColor.a = opacity;
            _spriteRenderer.DOColor(targetColor, _opacityDuration);
        }
    }   
}
