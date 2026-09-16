using System;
using System.Globalization;
using System.Text;

namespace Farm.Core
{
    /// <summary>
    /// Formats a <see cref="BigNumber"/> as 3 significant digits plus a magnitude suffix:
    /// 0-999 plain, then K/M/B, then bijective base-26 letters (a..z, aa..az, ...) every 3 more decades.
    /// </summary>
    public static class NumberFormatter
    {
        private const int ThousandExponent = 3;
        private const int MillionExponent = 6;
        private const int BillionExponent = 9;
        private const int LetterStartExponent = 12;
        private const int GroupSize = 3;

        public static string Format(BigNumber number)
        {
            if (number.IsZero) return "0";

            bool negative = number.Mantissa < 0d;
            BigNumber magnitude = negative ? -number : number;
            string formatted = FormatMagnitude(magnitude);
            return negative ? "-" + formatted : formatted;
        }

        private static string FormatMagnitude(BigNumber number)
        {
            if (number.Exponent < ThousandExponent)
            {
                double rounded = Math.Round(number.ToDouble(), MidpointRounding.AwayFromZero);
                if (rounded < 1000d)
                {
                    return rounded.ToString("0", CultureInfo.InvariantCulture);
                }

                // Rounding promoted this value (e.g. 999.6 -> 1000): re-enter as a BigNumber so it lands in the K group.
                number = new BigNumber(rounded, 0);
            }

            // Round to 3 significant digits up front; BigNumber's normalization absorbs any mantissa carry
            // (e.g. 9.996 -> 10.0 becomes mantissa 1.00 with exponent+1), so group selection below is always correct.
            number = new BigNumber(Math.Round(number.Mantissa, 2, MidpointRounding.AwayFromZero), number.Exponent);

            int groupBase;
            string suffix;

            if (number.Exponent < MillionExponent)
            {
                groupBase = ThousandExponent;
                suffix = "K";
            }
            else if (number.Exponent < BillionExponent)
            {
                groupBase = MillionExponent;
                suffix = "M";
            }
            else if (number.Exponent < LetterStartExponent)
            {
                groupBase = BillionExponent;
                suffix = "B";
            }
            else
            {
                int letterIndex = (number.Exponent - LetterStartExponent) / GroupSize;
                groupBase = LetterStartExponent + letterIndex * GroupSize;
                suffix = LetterSuffix(letterIndex);
            }

            int offsetInGroup = number.Exponent - groupBase; // 0, 1 or 2
            double scaled = number.Mantissa * Math.Pow(10d, offsetInGroup);
            int decimals = Math.Max(0, 2 - offsetInGroup);
            return scaled.ToString("F" + decimals, CultureInfo.InvariantCulture) + suffix;
        }

        private static string LetterSuffix(int zeroBasedIndex)
        {
            // Bijective base-26 numbering: 0->a, 25->z, 26->aa, 27->ab, ...
            int n = zeroBasedIndex + 1;
            var builder = new StringBuilder();
            while (n > 0)
            {
                int remainder = (n - 1) % 26;
                builder.Insert(0, (char)('a' + remainder));
                n = (n - 1) / 26;
            }

            return builder.ToString();
        }
    }
}
