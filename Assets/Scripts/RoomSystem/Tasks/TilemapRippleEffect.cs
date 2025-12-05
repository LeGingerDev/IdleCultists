using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using DG.Tweening;
using Audio.Managers;
using Audio.Core;

public class TilemapRippleEffect : MonoBehaviour
{
    [Title("References")]
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private TilemapCollider2D _tilemapCollider;
    [SerializeField] private Transform _rippleOrigin;
    [SerializeField] private TilemapRenderer _coverRenderer;

    [Title("Ripple Settings")]
    [SerializeField] private float _ringSpacing = 1f; // Distance between each circular wave
    [SerializeField] private float _delayBetweenRings = 0.05f;
    [SerializeField] private float _flipDuration = 0.2f;
    [SerializeField] private float _flipAngle = 180f;
    [SerializeField] private bool _rotateOnYAxis = true; // Makes tiles disappear

    [Title("Debug")]
    [SerializeField, ReadOnly] private int _totalTiles;
    [SerializeField, ReadOnly] private int _ringCount;

    private Coroutine _currentRippleCoroutine;
    private List<List<Vector3Int>> _cachedRings;

    private void Awake()
    {
        if (_tilemap == null)
            _tilemap = GetComponent<Tilemap>();

        if (_tilemapCollider == null)
            _tilemapCollider = GetComponent<TilemapCollider2D>();

        // Cache rings on awake for performance
        _cachedRings = GetTilesGroupedIntoRings();
        _ringCount = _cachedRings.Count;
        _totalTiles = _cachedRings.Sum(ring => ring.Count);
    }

    private void Start()
    {
        // Show cover by default (will be hidden by RoomController if room is unlocked)
        ShowCover();
    }

    #region Public API

    [Button("Test Ripple Effect", ButtonSizes.Large)]
    private void TestRipple()
    {
        if (_rippleOrigin == null)
        {
            Debug.LogWarning("Ripple origin transform is not assigned!");
            return;
        }

        StartRippleEffect();
    }

    [Button("Reset Tilemap", ButtonSizes.Medium)]
    private void TestReset()
    {
        SetLockedState();
    }

    [Button("Set Unlocked State", ButtonSizes.Medium)]
    private void TestUnlocked()
    {
        SetUnlockedState();
    }

    /// <summary>
    /// Start the ripple effect animation (for fresh room unlock)
    /// </summary>
    public void StartRippleEffect()
    {
        if (_currentRippleCoroutine != null)
            StopCoroutine(_currentRippleCoroutine);

        _currentRippleCoroutine = StartCoroutine(RippleEffectCoroutine());
    }

    /// <summary>
    /// Set tiles to locked state (rotation = 0, room hidden)
    /// </summary>
    public void SetLockedState()
    {
        StopAnyRunningEffect();
        SetAllTilesToRotation(0f);
        ShowCover();
        EnableCollider();
    }

    /// <summary>
    /// Set tiles to unlocked state (rotation = 180, room visible)
    /// </summary>
    public void SetUnlockedState()
    {
        StopAnyRunningEffect();
        SetAllTilesToRotation(_flipAngle);
        HideCover();
        DisableCollider();
    }

    #endregion

    #region Ripple Animation

    private IEnumerator RippleEffectCoroutine()
    {
        if (_cachedRings.Count == 0)
        {
            Debug.LogWarning("No tiles found in tilemap!");
            yield break;
        }

        // Reset all tiles to start position
        SetAllTilesToRotation(0f);

        // Animate each ring
        foreach (List<Vector3Int> ring in _cachedRings)
        {
            AnimateRing(ring);
            yield return new WaitForSeconds(_delayBetweenRings);
        }

        // Wait for the last ring's flip animation to complete
        yield return new WaitForSeconds(_flipDuration);

        // Disable the collider and hide cover after effect is complete
        DisableCollider();
        HideCover();

        _currentRippleCoroutine = null;
    }

    private void AnimateRing(List<Vector3Int> ring)
    {
        AudioManager.Instance.PlaySFX(AudioConstIds.CHECKERED_TILE_SOUND, true);

        // Animate all tiles in this ring simultaneously
        foreach (Vector3Int tilePos in ring)
        {
            AnimateTileFlip(tilePos);
        }
    }

    private void AnimateTileFlip(Vector3Int tilePosition)
    {
        float startRotation = 0f;
        float endRotation = _flipAngle;


        DOTween.To(() => startRotation,
                   x => ApplyRotationToTile(tilePosition, x),
                   endRotation,
                   _flipDuration)
               .SetEase(Ease.InOutSine)
               .SetTarget(this); // Allows DOTween.Kill(this) to work
    }

    #endregion

    #region Tile State Management

    private void SetAllTilesToRotation(float rotationDegrees)
    {
        if (_cachedRings == null || _cachedRings.Count == 0)
        {
            // Fallback if rings not cached yet
            SetAllTilesInBoundsToRotation(rotationDegrees);
            return;
        }

        foreach (List<Vector3Int> ring in _cachedRings)
        {
            foreach (Vector3Int tilePos in ring)
            {
                ApplyRotationToTile(tilePos, rotationDegrees);
            }
        }
    }

    private void SetAllTilesInBoundsToRotation(float rotationDegrees)
    {
        BoundsInt bounds = _tilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);

                if (_tilemap.HasTile(tilePos))
                {
                    ApplyRotationToTile(tilePos, rotationDegrees);
                }
            }
        }
    }

    private void ApplyRotationToTile(Vector3Int tilePosition, float rotationDegrees)
    {
        Quaternion rotation;

        if (_rotateOnYAxis)
        {
            // Rotate on Y axis - makes tiles appear to flip and disappear
            rotation = Quaternion.Euler(0, rotationDegrees, 0);
        }
        else
        {
            // Rotate on Z axis - spins the tile in place
            rotation = Quaternion.Euler(0, 0, rotationDegrees);
        }

        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(rotation);
        _tilemap.SetTransformMatrix(tilePosition, rotationMatrix);


    }

    #endregion

    #region Ring Calculation

    private List<List<Vector3Int>> GetTilesGroupedIntoRings()
    {
        Dictionary<int, List<Vector3Int>> ringGroups = new Dictionary<int, List<Vector3Int>>();
        Vector3 originWorldPos = _rippleOrigin.position;

        // Iterate through tilemap bounds
        BoundsInt bounds = _tilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);

                // Check if tile exists
                if (_tilemap.HasTile(tilePos))
                {
                    Vector3 tileWorldPos = _tilemap.CellToWorld(tilePos) + _tilemap.cellSize * 0.5f;
                    float distance = Vector3.Distance(originWorldPos, tileWorldPos);

                    // Group by distance ring
                    int ringIndex = Mathf.FloorToInt(distance / _ringSpacing);

                    if (!ringGroups.ContainsKey(ringIndex))
                        ringGroups[ringIndex] = new List<Vector3Int>();

                    ringGroups[ringIndex].Add(tilePos);
                }
            }
        }

        // Convert to sorted list
        return ringGroups.OrderBy(kvp => kvp.Key)
                        .Select(kvp => kvp.Value)
                        .ToList();
    }

    #endregion

    #region Cover Management

    private void ShowCover()
    {
        if (_coverRenderer != null)
        {
            _coverRenderer.enabled = true;
        }
    }

    private void HideCover()
    {
        if (_coverRenderer != null)
        {
            _coverRenderer.enabled = false;
        }
    }

    #endregion

    #region Collider Management

    private void DisableCollider()
    {
        if (_tilemapCollider != null)
        {
            _tilemapCollider.enabled = false;
        }
    }

    private void EnableCollider()
    {
        if (_tilemapCollider != null)
        {
            _tilemapCollider.enabled = true;
        }
    }

    #endregion

    #region Cleanup

    private void StopAnyRunningEffect()
    {
        if (_currentRippleCoroutine != null)
        {
            StopCoroutine(_currentRippleCoroutine);
            _currentRippleCoroutine = null;
        }

        // Kill any active DOTween animations on this component
        DOTween.Kill(this);
    }

    #endregion
}