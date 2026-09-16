using System;
using System.Globalization;

namespace Farm.Core
{
    /// <summary>
    /// Decimal number stored as mantissa*10^exponent so currency can grow far beyond double/long range
    /// with near-zero GC (struct, no allocations on arithmetic).
    /// </summary>
    [Serializable]
    public readonly struct BigNumber : IComparable<BigNumber>, IEquatable<BigNumber>
    {
        public readonly double Mantissa;
        public readonly int Exponent;

        public static readonly BigNumber Zero = new BigNumber(0d, 0);

        public BigNumber(double mantissa, int exponent)
        {
            Normalize(mantissa, exponent, out Mantissa, out Exponent);
        }

        public BigNumber(double value) : this(value, 0)
        {
        }

        public BigNumber(long value) : this((double)value, 0)
        {
        }

        public bool IsZero => Mantissa == 0d;

        private static void Normalize(double mantissa, int exponent, out double outMantissa, out int outExponent)
        {
            if (mantissa == 0d || double.IsNaN(mantissa))
            {
                outMantissa = 0d;
                outExponent = 0;
                return;
            }

            bool negative = mantissa < 0d;
            if (negative) mantissa = -mantissa;

            if (mantissa >= 10d || mantissa < 1d)
            {
                // Log10 jumps straight to the right magnitude instead of looping per decade.
                int shift = (int)Math.Floor(Math.Log10(mantissa));
                mantissa /= Math.Pow(10d, shift);
                exponent += shift;

                // Log10 rounding can land just outside [1, 10); these loops correct that.
                while (mantissa >= 10d)
                {
                    mantissa /= 10d;
                    exponent++;
                }

                while (mantissa < 1d)
                {
                    mantissa *= 10d;
                    exponent--;
                }
            }

            // Discards floating-point noise from arithmetic (e.g. 5.999999999999999 -> 6) so equal values compare equal.
            mantissa = Math.Round(mantissa, 10, MidpointRounding.AwayFromZero);
            if (mantissa >= 10d)
            {
                mantissa /= 10d;
                exponent++;
            }

            outMantissa = negative ? -mantissa : mantissa;
            outExponent = exponent;
        }

        public double ToDouble() => Mantissa * Math.Pow(10d, Exponent);

        public static BigNumber operator +(BigNumber a, BigNumber b)
        {
            if (a.IsZero) return b;
            if (b.IsZero) return a;

            BigNumber larger = a.Exponent >= b.Exponent ? a : b;
            BigNumber smaller = a.Exponent >= b.Exponent ? b : a;
            int expDiff = larger.Exponent - smaller.Exponent;

            // Beyond ~17 orders of magnitude the smaller term can't affect a double mantissa.
            if (expDiff > 17) return larger;

            double combined = larger.Mantissa + smaller.Mantissa / Math.Pow(10d, expDiff);
            return new BigNumber(combined, larger.Exponent);
        }

        public static BigNumber operator -(BigNumber value) => new BigNumber(-value.Mantissa, value.Exponent);

        public static BigNumber operator -(BigNumber a, BigNumber b) => a + (-b);

        public static BigNumber operator *(BigNumber a, double scalar) => new BigNumber(a.Mantissa * scalar, a.Exponent);

        public static BigNumber operator *(BigNumber a, BigNumber b) => new BigNumber(a.Mantissa * b.Mantissa, a.Exponent + b.Exponent);

        public static bool operator >(BigNumber a, BigNumber b) => a.CompareTo(b) > 0;
        public static bool operator <(BigNumber a, BigNumber b) => a.CompareTo(b) < 0;
        public static bool operator >=(BigNumber a, BigNumber b) => a.CompareTo(b) >= 0;
        public static bool operator <=(BigNumber a, BigNumber b) => a.CompareTo(b) <= 0;
        public static bool operator ==(BigNumber a, BigNumber b) => a.Equals(b);
        public static bool operator !=(BigNumber a, BigNumber b) => !a.Equals(b);

        public static implicit operator BigNumber(long value) => new BigNumber(value);
        public static implicit operator BigNumber(int value) => new BigNumber(value);

        public int CompareTo(BigNumber other)
        {
            int signSelf = Math.Sign(Mantissa);
            int signOther = Math.Sign(other.Mantissa);
            if (signSelf != signOther) return signSelf.CompareTo(signOther);
            if (signSelf == 0) return 0;

            int exponentCompare = Exponent.CompareTo(other.Exponent);
            if (exponentCompare != 0) return signSelf * exponentCompare;

            return signSelf * Mantissa.CompareTo(other.Mantissa);
        }

        public bool Equals(BigNumber other) => Mantissa.Equals(other.Mantissa) && Exponent == other.Exponent;

        public override bool Equals(object obj) => obj is BigNumber other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Mantissa, Exponent);

        /// <summary>Culture-invariant, lossless representation used for saving (avoids formatted-string precision loss).</summary>
        public string ToRawString() => Mantissa.ToString("R", CultureInfo.InvariantCulture) + "|" + Exponent.ToString(CultureInfo.InvariantCulture);

        public static BigNumber Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return Zero;

            int separatorIndex = raw.IndexOf('|');
            if (separatorIndex < 0) return Zero;

            double mantissa = double.Parse(raw.Substring(0, separatorIndex), CultureInfo.InvariantCulture);
            int exponent = int.Parse(raw.Substring(separatorIndex + 1), CultureInfo.InvariantCulture);
            return new BigNumber(mantissa, exponent);
        }

        public override string ToString() => NumberFormatter.Format(this);
    }
}
