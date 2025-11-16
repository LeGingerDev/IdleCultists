using LGD.Core;
using UnityEngine;
using Sirenix.OdinInspector;

public class SpriteRendererHoverOutline : HoverBase
{
    private const string OUTLINE_KEYWORD = "OUTBASE_ON";

    [FoldoutGroup("Target Settings")]
    [SerializeField] private bool _useCustomTarget = false;

    [FoldoutGroup("Target Settings")]
    [SerializeField, ShowIf("_useCustomTarget")] private SpriteRenderer _target;

    private SpriteRenderer _spriteRenderer;
    private Material _mat;

    private void Awake()
    {
        _spriteRenderer = _useCustomTarget ? _target : GetComponent<SpriteRenderer>();
        _mat = _spriteRenderer.material;
    }

    public override void OnHoverStart()
    {
        _mat.EnableKeyword(OUTLINE_KEYWORD);
    }

    public override void OnHoverEnd()
    {
        _mat.DisableKeyword(OUTLINE_KEYWORD);
    }
}