using LargeNumbers;

/// <summary>
/// Breaks down a stat into its base, additive, and multiplicative components
/// </summary>
public class StatBreakdown
{
    public StatType statType;
    public AlphabeticNotation baseValue;
    public AlphabeticNotation additiveTotal;
    public float multiplicativeTotal; // As percentage (0.5 = +50%)
    public AlphabeticNotation finalValue;

    public bool HasAdditiveModifiers => !additiveTotal.isZero;
    public bool HasMultiplicativeModifiers => multiplicativeTotal > 0f;

    public StatBreakdown(StatType statType, AlphabeticNotation baseValue, AlphabeticNotation additiveTotal, float multiplicativeTotal, AlphabeticNotation finalValue)
    {
        this.statType = statType;
        this.baseValue = baseValue;
        this.additiveTotal = additiveTotal;
        this.multiplicativeTotal = multiplicativeTotal;
        this.finalValue = finalValue;
    }
}