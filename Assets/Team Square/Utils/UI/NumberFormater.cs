using UnityEngine;
using System;

namespace Utils.UI
{
    public static class NumberFormatter
    {
        private static readonly string[] suffixes = new string[]
        {
        "", "k", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc"
        };

        /// <summary>
        /// Formate un nombre avec les règles :
        /// - Max 4 chiffres
        /// - Décimales uniquement si < 1
        /// - À partir de 10M, affiche seulement les milliers (arrondi)
        /// </summary>
        public static string FormatValue(double value)
        {
            if (value == 0) return "0";

            bool isNegative = value < 0;
            value = Math.Abs(value);

            // Cas 1 : Valeurs < 1 → Affiche avec décimales
            if (value < 1)
            {
                // Limite à 4 chiffres total (ex: 0.123, 0.012, 0.001)
                if (value >= 0.1)
                    return FormatWithSign(value.ToString("F2"), isNegative); // 0.12
                else if (value >= 0.01)
                    return FormatWithSign(value.ToString("F3"), isNegative); // 0.012
                else
                    return FormatWithSign(value.ToString("F3"), isNegative); // 0.001
            }

            // Cas 2 : Valeurs >= 1 et < 10M → Affiche normalement avec suffixes
            if (value < 10_000_000) // < 10M
            {
                return FormatWithSign(FormatNormal(value), isNegative);
            }

            // Cas 3 : Valeurs >= 10M → Arrondi aux milliers
            return FormatWithSign(FormatRoundedThousands(value), isNegative);
        }

        /// <summary>
        /// Format normal : max 4 chiffres, pas de décimales si >= 1
        /// </summary>
        private static string FormatNormal(double value)
        {
            int suffixIndex = 0;

            // Trouve le bon suffixe pour rester sous 4 chiffres
            while (value >= 10000 && suffixIndex < suffixes.Length - 1)
            {
                value /= 1000;
                suffixIndex++;
            }

            // Détermine le nombre de décimales selon la magnitude
            string formatted;
            if (value >= 1000) // 1000-9999
            {
                formatted = value.ToString("F0"); // Pas de décimales
            }
            else if (value >= 100) // 100-999
            {
                formatted = value.ToString("0"); // Pas de décimales
            }
            else if (value >= 10) // 10-99
            {
                formatted = value.ToString("0.#"); // 1 décimale si < 100
            }
            else // 1-9
            {
                formatted = value.ToString("0.##"); // 2 décimales si < 10
            }

            if (formatted.Contains("."))
            {
                formatted = formatted.TrimEnd('0').TrimEnd('.');
            }

            return formatted + suffixes[suffixIndex];
        }

        /// <summary>
        /// Format arrondi aux milliers pour valeurs >= 10M
        /// </summary>
        private static string FormatRoundedThousands(double value)
        {
            int suffixIndex = 0;

            // Divise par 1000 jusqu'à être gérable
            while (value >= 10000 && suffixIndex < suffixes.Length - 1)
            {
                value /= 1000;
                suffixIndex++;
            }

            // Arrondit aux milliers (pas de décimales)
            double rounded = Math.Round(value);

            return rounded.ToString("F0") + suffixes[suffixIndex];
        }

        private static string FormatWithSign(string formatted, bool isNegative)
        {
            return isNegative ? "-" + formatted : formatted;
        }

        /// <summary>
        /// Version pour ulong
        /// </summary>
        public static string FormatValue(ulong value)
        {
            return FormatValue((double)value);
        }

        /// <summary>
        /// Version pour float
        /// </summary>
        public static string FormatValue(float value)
        {
            return FormatValue((double)value);
        }
    }

    // Extension methods
    public static class NumberFormatterExtensions
    {
        public static string ToSmartString(this double value)
        {
            return NumberFormatter.FormatValue(value);
        }

        public static string ToSmartString(this ulong value)
        {
            return NumberFormatter.FormatValue(value);
        }

        public static string ToSmartString(this float value)
        {
            return NumberFormatter.FormatValue(value);
        }

        public static string ToSmartString(this int value)
        {
            return NumberFormatter.FormatValue(value);
        }
    }
}

// // Valeurs < 1 (avec décimales)
// 0.5.ToGameString()          → "0.50"
// 0.123.ToGameString()        → "0.12"
// 0.0456.ToGameString()       → "0.046"
// 0.001.ToGameString()        → "0.001"

// // Valeurs entre 1 et 10M (max 4 chiffres, pas de décimales si >= 1)
// 1.ToGameString()            → "1"
// 8.5.ToGameString()          → "8.5"
// 89.ToGameString()           → "89"
// 890.ToGameString()          → "890"
// 8900.ToGameString()         → "8900"
// 89900.ToGameString()        → "89.9k"  // Passe en k pour rester sous 4 chiffres
// 899000.ToGameString()       → "899k"
// 8990000.ToGameString()      → "8990k"  // < 10M donc pas arrondi

// // Valeurs >= 10M (arrondi aux milliers)
// 10_000_000.ToGameString()   → "10000k" ou "10M" (selon suffixe)
// 12_345_678.ToGameString()   → "12M"    // Arrondi : 12.3M → 12M
// 89_900_000.ToGameString()   → "90M"    // Arrondi : 89.9M → 90M
// 1_234_567_890.ToGameString()→ "1B"     // Arrondi aux milliards
// 9_876_543_210.ToGameString()→ "10B"    // Arrondi : 9.88B → 10B