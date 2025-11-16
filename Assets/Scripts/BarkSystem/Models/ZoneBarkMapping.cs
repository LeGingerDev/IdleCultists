using UnityEngine;
/// <summary>
/// Helper class for mapping zone ID keywords to bark contexts.
/// </summary>
[System.Serializable]
public class ZoneBarkMapping
{
    [Tooltip("Keyword to search for in zone ID (case-insensitive)")]
    public string zoneKeyword;

    [Tooltip("Bark context when first assigned to this zone")]
    public BarkContext reactiveBarkContext;

    [Tooltip("Bark context for periodic barks while in this zone")]
    public BarkContext ambientBarkContext;

    public ZoneBarkMapping(string keyword, BarkContext reactive, BarkContext ambient)
    {
        zoneKeyword = keyword;
        reactiveBarkContext = reactive;
        ambientBarkContext = ambient;
    }
}