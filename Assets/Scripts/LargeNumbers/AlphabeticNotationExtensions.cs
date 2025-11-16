using LargeNumbers;
using System.Text;
using UnityEngine;

namespace LGD.Extensions
{
    public static class AlphabeticNotationExtensions
    {
        private static StringBuilder _stringBuilder = new StringBuilder(16);

        /// <summary>
        /// Formats AlphabeticNotation with automatic decimal places:
        /// - Under 1K (magnitude 0): 2 decimal places (e.g., "123.45", "99.99")
        /// - At K or higher (magnitude >= 1): 1 decimal place (e.g., "1.5K", "23.4M")
        /// This is the default formatting for the game and should be used everywhere.
        /// </summary>
        public static string FormatWithDecimals(this AlphabeticNotation value)
        {
            _stringBuilder.Clear();

            if (value.isZero)
                return "0";

            // Decide decimal places based on magnitude
            int decimalPlaces = value.magnitude == 0 ? 2 : 1;
            string coefficientStr = value.coefficient.ToString($"F{decimalPlaces}");
            _stringBuilder.Append(coefficientStr);

            if (value.magnitude > 0)
            {
                string magnitudeName = AlphabeticNotation.GetAlphabeticMagnitudeName(value.magnitude);
                _stringBuilder.Append(magnitudeName);
            }

            return _stringBuilder.ToString();
        }

        /// <summary>
        /// Formats AlphabeticNotation with explicit custom decimal places
        /// Example: FormatWithDecimals(3) -> "1.234K"
        /// </summary>
        public static string FormatWithDecimals(this AlphabeticNotation value, int decimalPlaces)
        {
            _stringBuilder.Clear();

            if (value.isZero)
                return "0";

            string coefficientStr = value.coefficient.ToString($"F{decimalPlaces}");
            _stringBuilder.Append(coefficientStr);

            if (value.magnitude > 0)
            {
                string magnitudeName = AlphabeticNotation.GetAlphabeticMagnitudeName(value.magnitude);
                _stringBuilder.Append(magnitudeName);
            }

            return _stringBuilder.ToString();
        }

        /// <summary>
        /// Formats as percentage for multiplicative stats
        /// Example: 0.5 -> "50%", 1.25 -> "125%"
        /// </summary>
        public static string FormatAsPercentage(this float value, int decimalPlaces = 1)
        {
            return (value * 100).ToString($"F{decimalPlaces}") + "%";
        }

        /// <summary>
        /// Shorthand format with automatic decimal places (2 under 1K, 1 at K+)
        /// Example: "123.45", "1.5K", "99.9M", "1.0aa"
        /// </summary>
        public static string ToShortString(this AlphabeticNotation value)
        {
            return value.FormatWithDecimals();
        }

        /// <summary>
        /// Check if value is greater than or equal to cost (for affordability checks)
        /// </summary>
        public static bool CanAfford(this AlphabeticNotation current, AlphabeticNotation cost)
        {
            return current >= cost;
        }

        /// <summary>
        /// Returns a color-coded string for UI (green if can afford, red if can't)
        /// </summary>
        public static string FormatWithAffordabilityColor(this AlphabeticNotation cost, AlphabeticNotation currentAmount)
        {
            bool canAfford = currentAmount >= cost;
            string colorTag = canAfford ? "<color=green>" : "<color=red>";
            return $"{colorTag}{cost.ToShortString()}</color>";
        }

        /// <summary>
        /// Gets percentage toward affording something (for progress bars)
        /// </summary>
        public static float GetAffordabilityPercent(this AlphabeticNotation current, AlphabeticNotation cost)
        {
            if (cost.isZero)
                return 1f;

            double currentValue = (double)current;
            double costValue = (double)cost;

            float percent = (float)(currentValue / costValue);
            return Mathf.Clamp01(percent);
        }

        /// <summary>
        /// Format as compact notation (removes trailing zeros)
        /// Example: "1K" instead of "1.00K"
        /// </summary>
        public static string ToCompactString(this AlphabeticNotation value)
        {
            if (value.isZero)
                return "0";

            double coefficient = System.Math.Round(value.coefficient, 2);
            bool isWhole = coefficient % 1 == 0;

            _stringBuilder.Clear();

            if (isWhole)
                _stringBuilder.Append(((int)coefficient).ToString());
            else
                _stringBuilder.Append(coefficient.ToString("0.##"));

            if (value.magnitude > 0)
            {
                string magnitudeName = AlphabeticNotation.GetAlphabeticMagnitudeName(value.magnitude);
                _stringBuilder.Append(magnitudeName);
            }

            return _stringBuilder.ToString();
        }
    }
}