using System;
using System.Text;

public struct Rational : IComparable<Rational>, IComparable, IEquatable<Rational>
{
	public readonly Int32 numerator;
	public readonly Int32 denominator;

	#region Helper Properties
	public bool Negative => Sign > 0;

	public int Sign => Math.Sign(numerator) * Math.Sign(denominator);

	public Rational Fraction => this - Integer;

	public Int32 Integer => numerator / denominator;

	public Rational Inverse => new Rational(denominator, numerator);
	#endregion

	#region Constructors
	public Rational(Int32 numerator, Int32 denominator = 1)
	{
		if(denominator == 0) throw new ArgumentException("Denominator must be non-zero");
		this.numerator = numerator;
		this.denominator = denominator;
	}

	[Obsolete("Double to Rational is an inaccurate approximation")]
	public Rational(double value)
	{
		var mul = GetIntegerMultiplier(value);
		mul /= Gcd((long) (value * mul), mul);
		numerator = (Int32)(value * mul);
		denominator = (Int32)mul;
		Console.WriteLine($"value: {value} | final error: {(double)this - value}");
	}
	#endregion

	#region Conversions
	public static implicit operator Rational(Int32 numerator) => new Rational(numerator);

	[Obsolete("Double to Rational is an inaccurate approximation")]
	public static explicit operator Rational(double value) => new Rational(value);

	public static explicit operator double(Rational ratio) => (double)ratio.numerator / ratio.denominator;

	public static explicit operator Int32(Rational ratio) => ratio.numerator / ratio.denominator;
	#endregion

	#region Mathematical Operators
	public static Rational operator *(Rational left, Rational right)
	{
		return new Rational(left.numerator * right.numerator, left.denominator * right.denominator);
	}

	public static Rational operator /(Rational left, Rational right)
	{
		return left * right.Inverse;
	}

	public static Rational operator %(Rational left, Rational right)
	{
		return left - (left / right).Integer * right;
	}

	public static Rational operator +(Rational left, Rational right)
	{
		return new Rational(
			left.numerator * right.denominator + right.numerator * left.denominator,
			left.denominator * right.denominator);
	}

	public static Rational operator -(Rational left, Rational right)
	{
		return left + -right;
	}

	public static Rational operator -(Rational rational) => new Rational(rational.numerator * -1, rational.denominator);
	#endregion

	#region Comparators
	public static bool operator <(Rational left, Rational right) => left.CompareTo(right) < 0;

	public static bool operator <=(Rational left, Rational right) => left.CompareTo(right) <= 0;

	public static bool operator >(Rational left, Rational right) => left.CompareTo(right) > 0;

	public static bool operator >=(Rational left, Rational right) => left.CompareTo(right) >= 0;

	public int CompareTo(object obj) => obj is Rational rational ? CompareTo(rational) : throw new ArgumentException($"Object must be of type {nameof(Rational)}.");

	public int CompareTo(Rational other)
	{
		if (this == other) return 0;
		if (Sign > other.Sign) return 1;
		if (Sign < other.Sign) return -1;
		if (Sign > 0)
		{
			if (numerator >= other.numerator && denominator <= other.denominator) return 1;
			if (numerator <= other.numerator && denominator >= other.denominator) return -1;
			return checked ((numerator * other.denominator)
				.CompareTo(other.numerator * denominator));
		}

		if (Math.Abs(numerator) >= Math.Abs(other.numerator) &&
		    Math.Abs(denominator) <= Math.Abs(other.denominator)) return -1;
		if (Math.Abs(numerator) <= Math.Abs(other.numerator) &&
		    Math.Abs(denominator) >= Math.Abs(other.denominator)) return 1;
		return checked (-Math.Abs(numerator * other.denominator)
			.CompareTo(Math.Abs(other.numerator * denominator)));
	}

	#endregion

	#region Equality
	public static bool operator ==(Rational left, Rational right) => left.Equals(right);

	public static bool operator !=(Rational left, Rational right) => !(left == right);

	public override bool Equals(object obj)
	{
		return (obj is Rational rational) && this == rational;
	}

	public bool Equals(Rational other)
	{
		var norm = Reduced();
		other = other.Reduced();
		return norm.numerator == other.numerator && norm.denominator == other.denominator;
	}

	public override int GetHashCode()
	{
		var r = Reduced();
		return (r.numerator.GetHashCode() * 397) ^ r.denominator.GetHashCode();
	}
	#endregion

	#region Math Functions
	public static Rational Abs(Rational ratio)
	{
		return new Rational(Math.Abs(ratio.numerator), Math.Abs(ratio.denominator));
	}

	public static Rational Round(Rational ratio)
	{
		var integer = ratio.Integer;
		if (ratio == integer) return ratio;
		
		var fraction = ratio.Fraction;
		if (fraction < new Rational(1, 2)) return integer + (integer < 0 ? 1 : 0);
		if (fraction > new Rational(1, 2)) return integer + (integer < 0 ? 0 : 1);
		if (integer % 2 == 0) return integer;
		return integer + 1;
	}

	public static Rational Ceiling(Rational ratio)
	{
		var integer = ratio.Integer;
		if (ratio == integer) return ratio;
		if (ratio >= 0) return integer + 1;
		return integer;
	}

	public static Rational Floor(Rational ratio)
	{
		var integer = ratio.Integer;
		if (ratio == integer) return ratio;
		if (ratio >= 0) return integer;
		return integer - 1;
	}

	public static Rational Max(Rational a, Rational b) => a >= b ? a : b;

	public static Rational Min(Rational a, Rational b) => a <= b ? a : b;

	public static Rational Pow(Rational rational, Int32 power)
	{
		if (power < 0)
		{
			return new Rational((Int32)Math.Pow(rational.denominator, -power), (Int32)Math.Pow(rational.numerator, -power));
		}
		if (power == 0)
		{
			return new Rational(1);
		}
		return new Rational((Int32)Math.Pow(rational.numerator, power), (Int32)Math.Pow(rational.denominator, power));
	}
	#endregion

	#region Parse
	public static Rational Parse(string s)
	{
		if (s == null) throw new ArgumentNullException(nameof(s));
		if (TryParse(s, out var rational))
		{
			return rational;
		}
		throw new FormatException($"{nameof(s)} is not in the correct format");
	}

	public static bool TryParse(string s, out Rational rational)
	{
		if (Int32.TryParse(s, out var integer))
		{
			rational = new Rational(integer);
			return true;
		}

		if (double.TryParse(s, out var floating))
		{
			rational = new Rational(floating);
			return true;
		}

		var split = s.Split('/');
		if (split.Length == 2)
		{
			if (Int32.TryParse(split[0], out var numerator) &&
				Int32.TryParse(split[1], out var denominator))
			{
				rational = new Rational(numerator, denominator);
				return true;
			}
		}
		rational = default;
		return false;
	}
	#endregion

	#region ToString
	public string ToString(string format)
	{
		if (format == null) throw new ArgumentNullException(nameof(format));
		var sb = new StringBuilder(format);

		if (format.Contains("f?(")) RemoveConditional(sb, Math.Abs(denominator) != 1);
		if (format.Contains("f.v")) sb.Replace("f.v", Fraction.ToString("v"));
		if (format.Contains("f.d")) sb.Replace("f.d", Fraction.ToString("d"));
		if (format.Contains("f.n")) sb.Replace("f.n", Fraction.ToString("n"));
		if (format.Contains("f")) sb.Replace("f", Fraction.ToString());
		
		if (format.Contains("r")) sb.Replace("r", ToString());
		if (format.Contains("R")) sb.Replace("R", $"{numerator}/{denominator}");

		if (format.Contains("n")) sb.Replace("n", numerator.ToString());
		if (format.Contains("d")) sb.Replace("d", denominator.ToString());

		if (format.Contains("i")) sb.Replace("i", Integer.ToString());
		if (format.Contains("v")) sb.Replace("v", ((double)this).ToString());

		return sb.ToString();
	}

	public override string ToString()
	{
		return Math.Abs(denominator) == 1 ? $"{numerator * Math.Sign(denominator)}" : $"{numerator}/{denominator}";
	}

	private static StringBuilder RemoveConditional(StringBuilder sb, bool condition)
	{
		bool inCondition = false;
		int startIndex = 0;
		//Console.WriteLine($"condition: {condition}");
		for (int i = 0; i < sb.Length; i++)
		{
			//LogState(i.ToString(), sb.ToString());
			if (i + 2 < sb.Length &&
			    sb[i + 0] == 'f' &&
			    sb[i + 1] == '?' &&
			    sb[i + 2] == '(')
			{
				if (!inCondition)
				{
					startIndex = i;
					//LogState("s", sb.ToString(i, 3).PadLeft(i + 3));
				}

				inCondition = true;
			}
			else if (sb[i] == ')' && inCondition)
			{

				//LogState("c", sb.ToString(i, 1).PadLeft(i + 1));

				if (!condition)
				{
					//LogState("r", sb.ToString(startIndex, i - startIndex).PadLeft(i));
					sb.Remove(startIndex, i - startIndex + 1);
					i = startIndex;
				}
				else
				{
					//LogState("r", sb.ToString(i, 1).PadLeft(i + 1));
					sb.Remove(i, 1);
					//LogState("t", sb.ToString());
					//LogState("r", sb.ToString(startIndex, 3).PadLeft(startIndex + 3));
					sb.Remove(startIndex, 3);
				}

				inCondition = false;
			}
		}

		//LogState("f", sb.ToString());

		return sb;

		void LogState(string t, string s)
		{
			Console.WriteLine($"{t.PadLeft(2)}: {s}");
		}
	}
	#endregion

	#region Hidden Functions
	private static long Gcd(long a, long b)
	{
		while (b != 0)
		{
			var tmp = a % b;
			a = b;
			b = tmp;
		}

		return a == 0 ? b : a;
	}

	private static long GetIntegerMultiplier(double value)
	{
		Int32 bestMul = 1;
		var bestError = double.PositiveInfinity;
		TryFindBestMultiplier(2);
		if (bestError == 0) return bestMul;
		TryFindBestMultiplier(10);
		
		return bestMul;
		
		void TryFindBestMultiplier(int numBase)
		{
			for (int mul = 1; mul < Int32.MaxValue / numBase; mul *= numBase)
			{
				var multiplied = value * mul;
				var error = multiplied - Math.Floor(multiplied);
				Console.WriteLine($"base: {numBase} | exp: {Math.Log(mul, numBase)} | mul: {mul} | error: {error}");
				if (error == 0)
				{
					bestMul = mul;
					bestError = error;
					break;
				}
				if (error < bestError)
				{
					bestMul = mul;
					bestError = error;
				}
			}
		}
	}
	#endregion

	public Rational Reduced()
	{
		var gcd = (Int32)Gcd(numerator, denominator);
		return new Rational(numerator / gcd, denominator / gcd);
	}
}