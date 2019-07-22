using System;
using System.ComponentModel;
using Q = Rational;
using NUnit.Framework;
using UnityEngine.UI;

#pragma warning disable 618
public class RationalTest
{
    [TestCase(1, 1, ExpectedResult = "1")]
    [TestCase(33, 1, ExpectedResult = "33")]
    [TestCase(1, 2, ExpectedResult = "1/2")]
    [TestCase(2, 4, ExpectedResult = "2/4")]
    public string ToStringTest(Int32 numerator, Int32 denominator)
    {
        return new Rational(numerator, denominator).ToString();
    }

    [TestCase(+ 1, +1, "r", ExpectedResult = "1")]
    [TestCase(- 1, +1, "r", ExpectedResult = "-1")]
    [TestCase(+ 1, -1, "r", ExpectedResult = "-1")]
    [TestCase(+ 0, +1, "r", ExpectedResult = "0")]
    [TestCase(+ 1, +1, "R", ExpectedResult = "1/1")]
    [TestCase(- 1, -1, "R", ExpectedResult = "-1/-1")]
    [TestCase(+ 1, +2, "n", ExpectedResult = "1")]
    [TestCase(+ 2, +1, "n", ExpectedResult = "2")]
    [TestCase(+ 2, +1, "d", ExpectedResult = "1")]
    [TestCase(+ 8, +4, "i", ExpectedResult = "2")]
    [TestCase(+ 4, +8, "i", ExpectedResult = "0")]
    [TestCase(+ 4, -8, "i", ExpectedResult = "0")]
    [TestCase(+ 4, +8, "v", ExpectedResult = "0.5")]
    [TestCase(-12, -1, "v", ExpectedResult = "12")]
    [TestCase(- 6, -4, "f", ExpectedResult = "-2/-4")]
    [TestCase(+ 6, -4, "f.n", ExpectedResult = "2")]
    [TestCase(+ 4, -6, "f.d", ExpectedResult = "-6")]
    [TestCase(- 6, -4, "f.v", ExpectedResult = "0.5")]
    [TestCase(+11, +8, "iu + fu", ExpectedResult = "1u + 3/8u")]
    public string ToFormattedStringTest(Int32 numerator, Int32 denominator, string format)
    {
        return new Rational(numerator, denominator).ToString(format);
    }
    
    [TestCase(+ 1, +1, "iuf?( + fu)",     ExpectedResult = "1u")]
    [TestCase(+ 3, +2, "iuf?( + fu)",     ExpectedResult = "1u + 1/2u")]
    [TestCase(+ 1, +1, "iuf( + fu)",      ExpectedResult = "1u0( + 0u)")]
    [TestCase(+ 3, +2, "iuf?( + fu",      ExpectedResult = "1u1/2?( + 1/2u")]
    [TestCase(+ 1, +1, "iuf?(f?( + )fu)", ExpectedResult = "1u0u)")]
    [TestCase(+ 1, +1, "iu)f?( + fu)",    ExpectedResult = "1u)")]
    [TestCase(+ 1, +1, "iuf?( + (fu))",   ExpectedResult = "1u)")]
    [TestCase(+ 1, +1, "iuf?( + f?(fu))", ExpectedResult = "1u)")]
    [TestCase(+ 1, +1, "iuf?( + f?(fu)",  ExpectedResult = "1u")]
    public string ConditionalToStringTest(Int32 numerator, Int32 denominator, string format)
    {
        return new Rational(numerator, denominator).ToString(format);
    }

    [TestCase(0, 1, ExpectedResult = 0)]
    [TestCase(7, 3, ExpectedResult = 2)]
    [TestCase(-7, 3, ExpectedResult = -2)]
    public Int32 IntegerTest(Int32 numerator, Int32 denominator)
    {
        return new Rational(numerator, denominator).Integer;
    }

    [TestCase(1, 3, 7, 3)]
    [TestCase(-1, 3, -7, 3)]
    public void FractionTest(Int32 n0, Int32 d0, Int32 n1, Int32 d1)
    {
        Assert.AreEqual((Q)n0 / d0, ((Q)n1 / d1).Fraction);
    }

    [TestCase(1, 1, ExpectedResult = 1)]
    [TestCase(0, 1, ExpectedResult = 0)]
    [TestCase(-1, 1, ExpectedResult = -1)]
    [TestCase(1, -1, ExpectedResult = -1)]
    [TestCase(0, -1, ExpectedResult = 0)]
    [TestCase(-1, -1, ExpectedResult = 1)]
    public int SignTest(Int32 numerator, Int32 denominator)
    {
        return ((Q) numerator / denominator).Sign;
    }

    [Test]
    public void InverseTest()
    {
        Assert.AreEqual((Q)3 / 4, ((Q)4 / 3).Inverse);
    }

    [Test]
    public void InverseFailTest()
    {
        try
        {
            Assert.AreEqual(0, ((Q)0 / 3).Inverse);
            Assert.Fail();
        }
        catch (ArgumentException) { }
    }

    [Test]
    public void ConstructorErrorTest()
    {
        try
        {
            new Rational(2, 0);
            Assert.Fail();
        }
        catch (ArgumentException) { }
    }

    public class ConversionTest
    {
        [Test, Pairwise]
        public void OperatorExplicitTest(
            [Range(-4, 4)] Int32 numerator,
            [Range(-4, 4)] Int32 denominator)
        {
            if (denominator == 0) return;
            Assert.AreEqual((Q) numerator / denominator, new Rational(numerator, denominator));
        }
        
        [Test]
        public void FromFloatTest([Range(-1.2d, 1.2d, .1d)] double v)
        {
            v = (float) v;
            Assert.Less(Math.Abs(v - (double)(Q)v), .000001);
        }

        [Test]
        [TestCase(.1)]
        [TestCase(.9)]
        [TestCase(.11)]
        [TestCase(.100001)]
        [TestCase(double.Epsilon)]
        [TestCase(Math.PI)]
        public void FromDoubleTest(double val)
        {
            Assert.AreEqual(val, (double) new Rational(val));
        }

        [TestCase("1", ExpectedResult = 1)]
        [TestCase("-1", ExpectedResult = -1)]
        [TestCase("0", ExpectedResult = 0)]
        [TestCase("-0", ExpectedResult = -0)]
        [TestCase("12", ExpectedResult = 12)]
        [TestCase("-12", ExpectedResult = -12)]
        public Int32 ParseIntTest(string s)
        {
            return (Int32) Q.Parse(s);
        }

        [TestCase("1/3", 1, 3)]
        [TestCase("-1/3", -1, 3)]
        [TestCase("1/-3", 1, -3)]
        [TestCase("-1/-3", -1, -3)]
        [TestCase("999999/-999999", 999999, -999999)]
        [TestCase("-999999/-999999", -999999, -999999)]
        public void ParseFractionTest(string s, Int32 num, Int32 den)
        {
            Assert.AreEqual((Q) num / den, Q.Parse(s));
        }

        [TestCase("1.", 1, 1)]
        [TestCase("0.", 0, 1)]
        [TestCase(".0", 0, 1)]
        [TestCase(".25", 1, 4)]
        [TestCase(".20", 1, 5)]
        [TestCase(".3", 3, 10)]
        public void ParseFloatTest(string s, Int32 num, Int32 den)
        {
            Assert.AreEqual((Q) num / den, Q.Parse(s));
        }

        [TestCase((sbyte) 0)]
        [TestCase((sbyte) 2)]
        [TestCase((sbyte) 127)]
        [TestCase((sbyte) -128)]
        [TestCase(sbyte.MaxValue)]
        [TestCase(sbyte.MinValue)]
        public void SbyteRoundTripTest(sbyte val)
        {
            Assert.AreEqual(val, (sbyte) (Rational) val);
            Assert.AreEqual(val, (sbyte) new Rational(val));
        }

        [TestCase((byte) 0)]
        [TestCase((byte) 2)]
        [TestCase((byte) 255)]
        [TestCase(byte.MaxValue)]
        [TestCase(byte.MinValue)]
        public void ByteRoundTripTest(byte val)
        {
            Assert.AreEqual(val, (byte) (Rational) val);
            Assert.AreEqual(val, (byte) new Rational(val));
        }

        [TestCase((char) 0)]
        [TestCase((char) 2)]
        [TestCase((char) 255)]
        [TestCase(char.MaxValue)]
        [TestCase(char.MinValue)]
        public void CharRoundTripTest(char val)
        {
            Assert.AreEqual(val, (char) (Rational) val);
            Assert.AreEqual(val, (char) new Rational(val));
        }

        [TestCase((short) 0)]
        [TestCase((short) 2)]
        [TestCase((short) 4999)]
        [TestCase((short) -4999)]
        [TestCase(short.MaxValue)]
        [TestCase(short.MinValue)]
        public void ShortRoundTripTest(short val)
        {
            Assert.AreEqual(val, (short) (Rational) val);
            Assert.AreEqual(val, (short) new Rational(val));
        }

        [TestCase((ushort) 0)]
        [TestCase((ushort) 2)]
        [TestCase((ushort) 4999)]
        [TestCase(ushort.MaxValue)]
        [TestCase(ushort.MinValue)]
        public void UShortRoundTripTest(ushort val)
        {
            Assert.AreEqual(val, (ushort) (Rational) val);
            Assert.AreEqual(val, (ushort) new Rational(val));
        }

        [TestCase((int) 0)]
        [TestCase((int) 2)]
        [TestCase((int) 4999)]
        [TestCase((int) -4999)]
        [TestCase(int.MaxValue)]
        [TestCase(int.MinValue)]
        public void IntRoundTripTest(int val)
        {
            Assert.AreEqual(val, (int) (Rational) val);
            Assert.AreEqual(val, (int) new Rational(val));
        }

        [TestCase((uint) 0)]
        [TestCase((uint) 2)]
        [TestCase((uint) 4999)]
        [TestCase(uint.MaxValue)]
        [TestCase(uint.MinValue)]
        public void UIntRoundTripTest(uint val)
        {
            Assert.AreEqual(val, (uint) (Rational) val);
            Assert.AreEqual(val, (uint) new Rational(val));
        }


        [TestCase((long) 0)]
        [TestCase((long) 2)]
        [TestCase((long) 4999)]
        [TestCase((long) -4999)]
        [TestCase(long.MaxValue)]
        [TestCase(long.MinValue)]
        public void LongRoundTripTest(long val)
        {
            Assert.AreEqual(val, (long) (Rational) val);
            Assert.AreEqual(val, (long) new Rational(val));
        }

        [TestCase((ulong) 0)]
        [TestCase((ulong) 2)]
        [TestCase((ulong) 4999)]
        [TestCase(ulong.MaxValue)]
        [TestCase(ulong.MinValue)]
        public void ULongRoundTripTest(ulong val)
        {
            Assert.AreEqual(val, (ulong) (Rational) val);
            Assert.AreEqual(val, (ulong) new Rational(val));
        }

        [TestCase((float) 0)]
        [TestCase((float) 2)]
        [TestCase((float) 4999)]
        [TestCase((float) -4999)]
        [TestCase(float.MaxValue)]
        [TestCase(float.MinValue)]
        public void SingleRoundTripTest(float val)
        {
            Assert.AreEqual(val, (float) (Rational) val);
            Assert.AreEqual(val, (float) new Rational(val));
        }

        [TestCase((double) 0)]
        [TestCase((double) 2)]
        [TestCase((double) 4999)]
        [TestCase((double) -4999)]
        [TestCase(double.MaxValue)]
        [TestCase(double.MinValue)]
        public void DoubleRoundTripTest(double val)
        {
            Assert.AreEqual(val, (double) (Rational) val);
            Assert.AreEqual(val, (double) new Rational(val));
        }
    }

    public class MathOperatorsTest
    {

        [TestCase(1, 2, -1, 2)]
        [TestCase(1, 3, 1, -3)]
        public void OperatorUnaryNegationTest(Int32 n0, Int32 d0, Int32 n1, Int32 d1)
        {
            Assert.AreEqual((Q)n0 / d0, -(Q)n1 / d1);
        }
        
        [TestCase(1, 2, 1, 2, 0, 1)]
        [TestCase(1, 2, 0, 1, 1, 2)]
        [TestCase(1, 1, 1, 2, 1, 2)]
        [TestCase(-1, 2, 0, 1, -1, 2)]
        [TestCase(-1, 1, -1, 2, -1, 2)]
        [TestCase(0, 1, 0, 1, 0, 1)]
        [TestCase(0, 1, 1, 2, -1, 2)]
        [TestCase(1, 1, 1, 3, 4, 6)]
        [TestCase(1, 2, 1, 3, 1, 6)]
        [TestCase(1, 1, 104, 3, -202, 6)]
        public void OperatorAdditionTest(
            Int32 n0, Int32 d0, 
            Int32 n1, Int32 d1, 
            Int32 n2, Int32 d2)
        {
            Assert.AreEqual(((Q)n0 / d0), ((Q)n1 / d1) + ((Q)n2 / d2));
        }

        [TestCase(7, 12, 3, 4, 1, 6)]
        [TestCase(1, 4, 3, 4, 1, 2)]
        public void OperatorSubtractionTest(
            Int32 n0, Int32 d0, 
            Int32 n1, Int32 d1, 
            Int32 n2, Int32 d2)
        {
            Assert.AreEqual(((Q)n0 / d0), ((Q)n1 / d1) - ((Q)n2 / d2));
        }

        [TestCase(15, 8, 3, 4, 2, 5)]
        [TestCase(5, 1, 2, 1, 2, 5)]
        [TestCase(0, 1, 0, 1, 2, 5)]
        public void OperatorDivisionTest(
            Int32 n0, Int32 d0, 
            Int32 n1, Int32 d1, 
            Int32 n2, Int32 d2)
        {
            Assert.AreEqual(((Q)n0 / d0), ((Q)n1 / d1) / ((Q)n2 / d2));
        }

        [Test]
        public void OperatorDivisionFailTest()
        {
            try
            {
                Rational r = (Q)7 / 0;
                Assert.Fail();
            }
            catch (ArgumentException) { }
        }

        [TestCase(2, 7, 2, 5, 5, 7)]
        [TestCase(4, 5, 2, 5, 2, 1)]
        [TestCase(4, 5, 2, 1, 2, 5)]
        [TestCase(0, 1, 4, 5, 0, 1)]
        [TestCase(0, 1, 0, 1, 5, 6)]
        [TestCase(0, 1, 55, 1, 0, 1)]
        public void OperatorMultiplyTest(
            Int32 n0, Int32 d0, 
            Int32 n1, Int32 d1, 
            Int32 n2, Int32 d2)
        {
            Assert.AreEqual(((Q)n0 / d0), ((Q)n1 / d1) * ((Q)n2 / d2));
        }

        [TestCase(1, 1, 3, 1, 2, 1)]
        [TestCase(1, 2, 3, 2, 1, 1)]
        [TestCase(-1, 2, -3, 2, 1, 1)]
        [TestCase(1, 1, 3, 1, -2, 1)]
        [TestCase(1, 2, 3, 2, -1, 1)]
        [TestCase(-1, 2, -3, 2, -1, 1)]
        public void OperatorModulusTest(
            Int32 n0, Int32 d0, 
            Int32 n1, Int32 d1, 
            Int32 n2, Int32 d2)
        {
            Assert.AreEqual(((Q)n0 / d0), ((Q)n1 / d1) % ((Q)n2 / d2));
        }
    }

    public class ComparatorTests
    {
        [TestCase(1, 6, 1, 4, ExpectedResult = false)]
        [TestCase(1, 5, 1, 3, ExpectedResult = false)]
        [TestCase(1, 5, 1, 5, ExpectedResult = true)]
        [TestCase(20, 1, 0, 1, ExpectedResult = true)]
        [TestCase(20, 1, 1, 5, ExpectedResult = true)]
        [TestCase(20, 1, 2, 5, ExpectedResult = true)]
        [TestCase(20, 1, 3, 5, ExpectedResult = true)]
        [TestCase(20, 1, 4, 5, ExpectedResult = true)]
        [TestCase(20, 1, 1, 1, ExpectedResult = true)]
        [TestCase(0, 1, -1, 1, ExpectedResult = true)]
        [TestCase(-10, 1, -9, 1, ExpectedResult = false)]
        [TestCase(0, 1, 20, 1, ExpectedResult = false)]
        public bool OperatorGreaterThanOrEqualTest(
            Int32 n0, Int32 d0, 
            Int32 n1, Int32 d1)
        {
            return (Q) n0 / d0 >= (Q) n1 / d1;
        }

        [TestCase(1, 2, 1, 4, ExpectedResult = true)]
        [TestCase(1, 5, 1, 3, ExpectedResult = false)]
        [TestCase(1, 5, 1, 5, ExpectedResult = false)]
        public bool OperatorGreaterThanTest(
            Int32 n0, Int32 d0, 
            Int32 n1, Int32 d1)
        {
            return (Q) n0 / d0 > (Q) n1 / d1;
        }

        [TestCase(1, 4, 1, 6, ExpectedResult = false)]
        [TestCase(1, 5, 1, 3, ExpectedResult = true)]
        [TestCase(1, 5, 1, 5, ExpectedResult = false)]
        public bool OperatorLessThanTest(
            Int32 n0, Int32 d0, 
            Int32 n1, Int32 d1)
        {
            return (Q) n0 / d0 < (Q) n1 / d1;
        }

        [TestCase(1, 4, 1, 5, ExpectedResult = false)]
        [TestCase(1, 5, 1, 3, ExpectedResult = true)]
        [TestCase(1, 5, 1, 5, ExpectedResult = true)]
        public bool OperatorLessThanOrEqualTest(
            Int32 n0, Int32 d0, 
            Int32 n1, Int32 d1)
        {
            return (Q) n0 / d0 <= (Q) n1 / d1;
        }

        [Test, Sequential]
        public void CompareToIntTest([Range(-16, 17)] Int32 x, [Range(-17, 16)] Int32 y)
        {
            Assert.AreEqual(1, ((Q) x).CompareTo(y));
            Assert.AreEqual(-1, ((Q) y).CompareTo(x));
            Assert.AreEqual(x.CompareTo(-y), ((Q) x).CompareTo(-y));
            Assert.AreEqual((-x).CompareTo(y), (-(Q) x).CompareTo(y));
        }

        [Test, Pairwise]
        public void CompareFractionTest(
            [Range(0, 4)] Int32 n0,
            [Range(1, 4)] Int32 d0,
            [Range(0, 4)] Int32 n1,
            [Range(1, 4)] Int32 d1)
        {
            var i = 0;
            Console.WriteLine(i++);
            Assert.AreEqual(
                ((double) +n0 / +d0).CompareTo((double) +n1 / +d1),
                ((Q) (+n0) / (+d0)).CompareTo((Q) (+n1) / (+d1)));
            Console.WriteLine(i++);
            Assert.AreEqual(
                ((double) +n0 / -d0).CompareTo((double) +n1 / +d1),
                ((Q) (+n0) / (-d0)).CompareTo((Q) (+n1) / (+d1)));
            Console.WriteLine(i++);
            Assert.AreEqual(
                ((double) -n0 / +d0).CompareTo((double) +n1 / +d1),
                ((Q) (-n0) / (+d0)).CompareTo((Q) (+n1) / (+d1)));
            Console.WriteLine(i++);
            Assert.AreEqual(
                ((double) -n0 / -d0).CompareTo((double) +n1 / +d1),
                ((Q) (-n0) / (-d0)).CompareTo((Q) (+n1) / (+d1)));
            Console.WriteLine(i++);

            Assert.AreEqual(
                ((double) +n0 / +d0).CompareTo((double) +n1 / -d1),
                ((Q) (+n0) / (+d0)).CompareTo((Q) (+n1) / (-d1)));
            Console.WriteLine(i++);
            Assert.AreEqual(
                ((double) +n0 / -d0).CompareTo((double) +n1 / -d1),
                ((Q) (+n0) / (-d0)).CompareTo((Q) (+n1) / (-d1)));
            Console.WriteLine(i++);
            Assert.AreEqual(
                ((double) -n0 / +d0).CompareTo((double) +n1 / -d1),
                ((Q) (-n0) / (+d0)).CompareTo((Q) (+n1) / (-d1)));
            Console.WriteLine(i++);
            Assert.AreEqual(
                ((double) -n0 / -d0).CompareTo((double) +n1 / -d1),
                ((Q) (-n0) / (-d0)).CompareTo((Q) (+n1) / (-d1)));
            Console.WriteLine(i++);

            Assert.AreEqual(
                ((double) +n0 / +d0).CompareTo((double) -n1 / +d1),
                ((Q) (+n0) / (+d0)).CompareTo((Q) (-n1) / (+d1)));
            Console.WriteLine(i++);
            Assert.AreEqual(
                ((double) +n0 / -d0).CompareTo((double) -n1 / +d1),
                ((Q) (+n0) / (-d0)).CompareTo((Q) (-n1) / (+d1)));
            Console.WriteLine(i++);
            Assert.AreEqual(
                ((double) -n0 / +d0).CompareTo((double) -n1 / +d1),
                ((Q) (-n0) / (+d0)).CompareTo((Q) (-n1) / (+d1)));
            Console.WriteLine(i++);
            Assert.AreEqual(
                ((double) -n0 / -d0).CompareTo((double) -n1 / +d1),
                ((Q) (-n0) / (-d0)).CompareTo((Q) (-n1) / (+d1)));
            Console.WriteLine(i++);

            Assert.AreEqual(
                ((double) +n0 / +d0).CompareTo((double) -n1 / -d1),
                ((Q) (+n0) / (+d0)).CompareTo((Q) (-n1) / (-d1)));
            Console.WriteLine(i++);
            Assert.AreEqual(
                ((double) +n0 / -d0).CompareTo((double) -n1 / -d1),
                ((Q) (+n0) / (-d0)).CompareTo((Q) (-n1) / (-d1)));
            Console.WriteLine(i++);
            Assert.AreEqual(
                ((double) -n0 / +d0).CompareTo((double) -n1 / -d1),
                ((Q) (-n0) / (+d0)).CompareTo((Q) (-n1) / (-d1)));
            Console.WriteLine(i++);
            Assert.AreEqual(
                ((double) -n0 / -d0).CompareTo((double) -n1 / -d1),
                ((Q) (-n0) / (-d0)).CompareTo((Q) (-n1) / (-d1)));
            Console.WriteLine(i++);
        }

        [Test]
        public void CompareToObjectTest()
        {
            object o = new Rational(5, 6);
            Rational r = new Rational(5, 6);
            Assert.AreEqual(0, r.CompareTo(o));
            try
            {
                Assert.AreEqual(0, r.CompareTo("stringval"));
                Assert.Fail();
            }
            catch (ArgumentException) { }
        }
    }

    public class EqualityTests
    {
        [TestCase(null, ExpectedResult = false)]
        [TestCase("Hello", ExpectedResult = false)]
        public bool EqualsTest(object obj)
        {
            return new Rational().Equals(obj);
        }

        [Test]
        public void GetHashCodeTest()
        {
            const Int32 r = 16;
            Int32 hashCodeCollisions = 0;
            for (Int32 n0 = -r; n0 < r; n0++)
            {
                for (Int32 d0 = -r; n0 < r; n0++)
                {
                    for (Int32 n1 = -r; n0 < r; n0++)
                    {
                        for (Int32 d1 = -r; n0 < r; n0++)
                        {
                            var r0 = new Rational(n0, d0);
                            var r1 = new Rational(n1, d1);
                            try
                            {
                                Assert.IsFalse(r0 == r1 && r0.GetHashCode() != r1.GetHashCode());
                            }
                            catch (Exception e)
                            {
                                throw new Exception(
                                    $"{nameof(r0)}: {r0.ToString("R")} | {nameof(r1)}: {r1.ToString("R")}", e);
                            }

                            if (r0 != r1 && r0.GetHashCode() == r1.GetHashCode())
                            {
                                hashCodeCollisions++;
                            }
                        }
                    }
                }
            }

            Console.WriteLine($"Hash collisions: {(float) hashCodeCollisions / r * 100}%");
        }
    }

    public class MathFunctionTests
    {
        [TestCase(1, 3, 1, 3)]
        [TestCase(1, 3, -1, 3)]
        public void AbsTest(
            Int32 n0, Int32 d0, 
            Int32 n1, Int32 d1)
        {
            Assert.AreEqual((Q)n0 / d0, Rational.Abs((Q)n1 / d1));
        }

        [Test, Pairwise]
        public void RoundTest1(
            [Range(0, 4)] Int32 n, 
            [Range(1, 4)] Int32 d)
        {
            Assert.AreEqual(Math.Round((double)+n / +d), (double)Rational.Round((Q)(+n) / +d));
        }
        [Test, Pairwise]
        public void RoundTest2(
            [Range(0, 4)] Int32 n, 
            [Range(1, 4)] Int32 d)
        {
            Assert.AreEqual(Math.Round((double)-n / +d), (double)Rational.Round((Q)(-n) / +d));
        }
        [Test, Pairwise]
        public void RoundTest3(
            [Range(0, 4)] Int32 n, 
            [Range(1, 4)] Int32 d)
        {
            Assert.AreEqual(Math.Round((double)+n / -d), (double)Rational.Round((Q)(+n) / -d));
        }
        [Test, Pairwise]
        public void RoundTest4(
            [Range(0, 4)] Int32 n, 
            [Range(1, 4)] Int32 d)
        {
            Assert.AreEqual(Math.Round((double)-n / -d), (double)Rational.Round((Q)(-n) / -d));
        }

        [Test]
        public void FloorTest()
        {
            Assert.AreEqual(0, Math.Floor(.5));
            Assert.AreEqual((Q) 0, Rational.Floor((Q) 1 / 2));

            Assert.AreEqual(1, Math.Floor(1.0));
            Assert.AreEqual((Q) 1, Rational.Floor((Q) 1));

            Assert.AreEqual(-1, Math.Floor(-.5));
            Assert.AreEqual(-(Q) 1, Rational.Floor(-(Q) 1 / 2));
        }

        [Test]
        public void CeilingTest()
        {
            Assert.AreEqual(1, Math.Ceiling(.5));
            Assert.AreEqual((Q) 1, Rational.Ceiling((Q) 1 / 2));

            Assert.AreEqual(1, Math.Ceiling(1.0));
            Assert.AreEqual((Q) 1, Rational.Ceiling((Q) 1));

            Assert.AreEqual(0, Math.Ceiling(-.5));
            Assert.AreEqual((Q) 0, Rational.Ceiling(-(Q) 1 / 2));
        }

        [Test]
        public void MaxTest()
        {
            Assert.AreEqual((Q) 3 / 4, Rational.Max((Q) 3 / 4, (Q) 1 / 2));
            Assert.AreEqual((Q) 3 / 4, Rational.Max((Q) 1 / 2, (Q) 3 / 4));
            Assert.AreEqual((Q) 3 / 4, Rational.Max((Q) 3 / 4, (Q) 3 / 4));
        }

        [Test]
        public void PowTest()
        {
            Assert.AreEqual((Q) 1 / 4, Rational.Pow((Q) 1 / 2, 2));
            Assert.AreEqual((Q) 4, Rational.Pow((Q) 1 / 2, -2));
            Assert.AreEqual((Q) 1, Rational.Pow((Q) 1 / 2, 0));
        }

        [Test]
        public void MinTest()
        {
            Assert.AreEqual((Q) 1 / 2, Rational.Min((Q) 3 / 4, (Q) 1 / 2));
            Assert.AreEqual((Q) 1 / 2, Rational.Min((Q) 1 / 2, (Q) 3 / 4));
            Assert.AreEqual((Q) 3 / 4, Rational.Min((Q) 3 / 4, (Q) 3 / 4));
        }
    }
}
#pragma warning restore 618