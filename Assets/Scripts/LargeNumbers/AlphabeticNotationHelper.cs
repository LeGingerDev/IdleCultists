using LargeNumbers;
using UnityEngine;

namespace LGD.Utilities
{
    /// <summary>
    /// Static helper class for common AlphabeticNotation operations
    /// </summary>
    public static class AlphabeticNotationHelper
    {
        /// <summary>
        /// Clamps a value between min and max
        /// </summary>
        public static AlphabeticNotation Clamp(AlphabeticNotation value, AlphabeticNotation min, AlphabeticNotation max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Returns the minimum of two values
        /// </summary>
        public static AlphabeticNotation Min(AlphabeticNotation a, AlphabeticNotation b)
        {
            return a < b ? a : b;
        }

        /// <summary>
        /// Returns the maximum of two values
        /// </summary>
        public static AlphabeticNotation Max(AlphabeticNotation a, AlphabeticNotation b)
        {
            return a > b ? a : b;
        }

        /// <summary>
        /// Lerp between two AlphabeticNotation values
        /// </summary>
        public static AlphabeticNotation Lerp(AlphabeticNotation from, AlphabeticNotation to, float t)
        {
            t = Mathf.Clamp01(t);

            double fromValue = (double)from;
            double toValue = (double)to;
            double result = fromValue + (toValue - fromValue) * t;

            return new AlphabeticNotation(result);
        }

        /// <summary>
        /// Calculates percentage increase from base to new value
        /// Example: base=100, new=150 -> returns 0.5 (50% increase)
        /// </summary>
        public static float CalculatePercentageIncrease(AlphabeticNotation baseValue, AlphabeticNotation newValue)
        {
            if (baseValue.isZero)
                return newValue.isZero ? 0f : float.MaxValue;

            double baseDouble = (double)baseValue;
            double newDouble = (double)newValue;

            return (float)((newDouble - baseDouble) / baseDouble);
        }

        /// <summary>
        /// Applies a percentage modifier to a value
        /// Example: ApplyPercentage(100, 0.5) -> 150 (100 * 1.5)
        /// </summary>
        public static AlphabeticNotation ApplyPercentage(AlphabeticNotation baseValue, float percentage)
        {
            return baseValue * (1f + percentage);
        }

        /// <summary>
        /// Calculate exponential growth: base * (rate ^ tier)
        /// </summary>
        public static AlphabeticNotation CalculateExponentialGrowth(double baseValue, double rate, int tier)
        {
            double result = baseValue * System.Math.Pow(rate, tier - 1);
            return new AlphabeticNotation(result);
        }

        /// <summary>
        /// Calculate linear growth: base + (increment * tier)
        /// </summary>
        public static AlphabeticNotation CalculateLinearGrowth(double baseValue, double increment, int tier)
        {
            double result = baseValue + (increment * (tier - 1));
            return new AlphabeticNotation(result);
        }

        /// <summary>
        /// Parse a string to AlphabeticNotation (useful for save/load)
        /// </summary>
        public static bool TryParse(string text, out AlphabeticNotation result)
        {
            return AlphabeticNotation.GetAlphabeticNotationFromString(text, out result);
        }

        /// <summary>
        /// Rounds down to nearest whole coefficient
        /// Example: 1.7K -> 1K
        /// </summary>
        public static AlphabeticNotation Floor(AlphabeticNotation value)
        {
            double flooredCoefficient = System.Math.Floor(value.coefficient);
            return new AlphabeticNotation(flooredCoefficient, value.magnitude);
        }

        /// <summary>
        /// Rounds up to nearest whole coefficient
        /// Example: 1.3K -> 2K
        /// </summary>
        public static AlphabeticNotation Ceil(AlphabeticNotation value)
        {
            double ceiledCoefficient = System.Math.Ceiling(value.coefficient);
            return new AlphabeticNotation(ceiledCoefficient, value.magnitude);
        }

        /// <summary>
        /// Rounds to nearest whole coefficient
        /// Example: 1.4K -> 1K, 1.6K -> 2K
        /// </summary>
        public static AlphabeticNotation Round(AlphabeticNotation value)
        {
            double roundedCoefficient = System.Math.Round(value.coefficient);
            return new AlphabeticNotation(roundedCoefficient, value.magnitude);
        }

        /// <summary>
        /// Checks if a value is effectively zero (within small tolerance)
        /// </summary>
        public static bool IsEffectivelyZero(AlphabeticNotation value, double tolerance = 0.001)
        {
            return value.coefficient < tolerance && value.magnitude == 0;
        }

        /// <summary>
        /// Get abbreviated magnitude name (K, M, B, T, aa, ab, etc.)
        /// </summary>
        public static string GetMagnitudeName(int magnitude)
        {
            return AlphabeticNotation.GetAlphabeticMagnitudeName(magnitude);
        }
    }
}