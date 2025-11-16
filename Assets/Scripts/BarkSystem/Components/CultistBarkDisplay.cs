using DG.Tweening;
using LGD.Core;
using LGD.Core.Events;
using Sirenix.OdinInspector;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays barks above a cultist's head in a speech bubble.
/// Listens for ON_BARK_REQUESTED events and shows them if the entity ID matches.
/// </summary>
[RequireComponent(typeof(EntityController))]
public class CultistBarkDisplay : BaseBehaviour
{
    [SerializeField, FoldoutGroup("UI")]
    private GameObject _bubbleContainer;

    [SerializeField, FoldoutGroup("UI")]
    private TextMeshProUGUI _barkText;

    [SerializeField, FoldoutGroup("UI")]
    private CanvasGroup _canvasGroup;

    [SerializeField, FoldoutGroup("Settings")]
    private float _displayDuration = 3f;

    [SerializeField, FoldoutGroup("Settings")]
    private float _fadeInDuration = 0.2f;

    [SerializeField, FoldoutGroup("Settings")]
    private float _fadeOutDuration = 0.3f;

    [SerializeField, FoldoutGroup("Settings")]
    [Tooltip("Minimum time between barks to prevent spam")]
    private float _cooldownBetweenBarks = 2f;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private string _lastBarkShown;

    [SerializeField, ReadOnly, FoldoutGroup("Debug")]
    private float _lastBarkTime;

    private EntityController _entityController;
    private Coroutine _displayCoroutine;
    private Tween _fadeTween;

    private void Awake()
    {
        _entityController = GetComponent<EntityController>();
        HideBubble();
    }

    [Topic(BarkEventIds.ON_BARK_REQUESTED)]
    public void OnBarkRequested(object sender, EntityRuntimeData entity, string bark)
    {
        // Only respond if this bark is for THIS specific entity
        if (_entityController.RuntimeData.uniqueId != entity.uniqueId)
            return;

        // Cooldown check to prevent spam
        if (Time.time - _lastBarkTime < _cooldownBetweenBarks)
        {
            Debug.Log($"[CultistBarkDisplay] Bark cooldown active, skipping: \"{bark}\"");
            return;
        }

        ShowBark(bark);
    }

    private void ShowBark(string text)
    {
        // Stop any existing display coroutine
        if (_displayCoroutine != null)
            StopCoroutine(_displayCoroutine);

        KillFadeTween();

        // Update bark text
        _barkText.text = text;
        _bubbleContainer.SetActive(true);

        // Track for debug
        _lastBarkShown = text;
        _lastBarkTime = Time.time;

        // Fade in
        _fadeTween = _canvasGroup.DOFade(1f, _fadeInDuration).From(0f);

        // Start hide timer
        _displayCoroutine = StartCoroutine(HideAfterDelay());

        Debug.Log($"[CultistBarkDisplay] Showing bark: \"{text}\"");
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(_displayDuration);
        HideBubble();
    }

    private void HideBubble()
    {
        KillFadeTween();

        // Fade out
        _fadeTween = _canvasGroup.DOFade(0f, _fadeOutDuration).OnComplete(() =>
        {
            _bubbleContainer.SetActive(false);
        });
    }

    private void KillFadeTween()
    {
        if (_fadeTween != null && _fadeTween.IsActive())
        {
            _fadeTween.Kill();
        }
    }

    private void OnDestroy()
    {
        KillFadeTween();
    }

    #region Debug Helpers

    [Button("Test Bark"), FoldoutGroup("Debug")]
    private void DebugTestBark(string testBark = "Test bark!")
    {
        ShowBark(testBark);
    }

    #endregion
}