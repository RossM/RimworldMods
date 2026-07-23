namespace Disharmony.Tests;

public static class InnerPostfixConstantPatches
{
    public static int IntObserved;
    public static long LongObserved;
    public static float FloatObserved;
    public static double DoubleObserved;
    public static string? StringObserved;

    [InnerPostfixConstant(ConstantTargets.IntValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.IntResult))]
    public static void InnerPostfixConstant_Int_Result_ReadByValue(int __result) => IntObserved = __result;

    [InnerPostfixConstant(ConstantTargets.IntValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.IntResult))]
    public static void InnerPostfixConstant_Int_Result_ReadByReference(ref int __result) => IntObserved = __result;

    [InnerPostfixConstant(ConstantTargets.IntValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.IntResult))]
    public static void InnerPostfixConstant_Int_Result_WriteByReference(ref int __result) =>
        __result = ConstantTargets.IntReplacement;

    [InnerPostfixConstant(ConstantTargets.LongValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.LongResult))]
    public static void InnerPostfixConstant_Long_Result_ReadByValue(long __result) => LongObserved = __result;

    [InnerPostfixConstant(ConstantTargets.LongValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.LongResult))]
    public static void InnerPostfixConstant_Long_Result_ReadByReference(ref long __result) => LongObserved = __result;

    [InnerPostfixConstant(ConstantTargets.LongValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.LongResult))]
    public static void InnerPostfixConstant_Long_Result_WriteByReference(ref long __result) =>
        __result = ConstantTargets.LongReplacement;

    [InnerPostfixConstant(ConstantTargets.FloatValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.FloatResult))]
    public static void InnerPostfixConstant_Float_Result_ReadByValue(float __result) => FloatObserved = __result;

    [InnerPostfixConstant(ConstantTargets.FloatValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.FloatResult))]
    public static void InnerPostfixConstant_Float_Result_ReadByReference(ref float __result) => FloatObserved = __result;

    [InnerPostfixConstant(ConstantTargets.FloatValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.FloatResult))]
    public static void InnerPostfixConstant_Float_Result_WriteByReference(ref float __result) =>
        __result = ConstantTargets.FloatReplacement;

    [InnerPostfixConstant(ConstantTargets.DoubleValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.DoubleResult))]
    public static void InnerPostfixConstant_Double_Result_ReadByValue(double __result) => DoubleObserved = __result;

    [InnerPostfixConstant(ConstantTargets.DoubleValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.DoubleResult))]
    public static void InnerPostfixConstant_Double_Result_ReadByReference(ref double __result) => DoubleObserved = __result;

    [InnerPostfixConstant(ConstantTargets.DoubleValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.DoubleResult))]
    public static void InnerPostfixConstant_Double_Result_WriteByReference(ref double __result) =>
        __result = ConstantTargets.DoubleReplacement;

    [InnerPostfixConstant(ConstantTargets.StringValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.StringResult))]
    public static void InnerPostfixConstant_String_Result_ReadByValue(string __result) => StringObserved = __result;

    [InnerPostfixConstant(ConstantTargets.StringValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.StringResult))]
    public static void InnerPostfixConstant_String_Result_ReadByReference(ref string __result) => StringObserved = __result;

    [InnerPostfixConstant(ConstantTargets.StringValue)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.StringResult))]
    public static void InnerPostfixConstant_String_Result_WriteByReference(ref string __result) =>
        __result = ConstantTargets.StringReplacement;

    [InnerPostfixConstant(-1)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.Int_SpecialEncoding_ValueMinus1_Result))]
    public static void InnerPostfixConstant_Int_SpecialEncoding_ValueMinus1_Result_ReadByValue(int __result) =>
        IntObserved = __result;

    [InnerPostfixConstant(0)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.Int_SpecialEncoding_Value0_Result))]
    public static void InnerPostfixConstant_Int_SpecialEncoding_Value0_Result_ReadByValue(int __result) => IntObserved = __result;

    [InnerPostfixConstant(1)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.Int_SpecialEncoding_Value1_Result))]
    public static void InnerPostfixConstant_Int_SpecialEncoding_Value1_Result_ReadByValue(int __result) => IntObserved = __result;

    [InnerPostfixConstant(2)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.Int_SpecialEncoding_Value2_Result))]
    public static void InnerPostfixConstant_Int_SpecialEncoding_Value2_Result_ReadByValue(int __result) => IntObserved = __result;

    [InnerPostfixConstant(3)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.Int_SpecialEncoding_Value3_Result))]
    public static void InnerPostfixConstant_Int_SpecialEncoding_Value3_Result_ReadByValue(int __result) => IntObserved = __result;

    [InnerPostfixConstant(4)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.Int_SpecialEncoding_Value4_Result))]
    public static void InnerPostfixConstant_Int_SpecialEncoding_Value4_Result_ReadByValue(int __result) => IntObserved = __result;

    [InnerPostfixConstant(5)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.Int_SpecialEncoding_Value5_Result))]
    public static void InnerPostfixConstant_Int_SpecialEncoding_Value5_Result_ReadByValue(int __result) => IntObserved = __result;

    [InnerPostfixConstant(6)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.Int_SpecialEncoding_Value6_Result))]
    public static void InnerPostfixConstant_Int_SpecialEncoding_Value6_Result_ReadByValue(int __result) => IntObserved = __result;

    [InnerPostfixConstant(7)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.Int_SpecialEncoding_Value7_Result))]
    public static void InnerPostfixConstant_Int_SpecialEncoding_Value7_Result_ReadByValue(int __result) => IntObserved = __result;

    [InnerPostfixConstant(8)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.Int_SpecialEncoding_Value8_Result))]
    public static void InnerPostfixConstant_Int_SpecialEncoding_Value8_Result_ReadByValue(int __result) => IntObserved = __result;

    [InnerPostfixConstant(-128)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.Int_SignedByteEncoding_ValueMinus128_Result))]
    public static void InnerPostfixConstant_Int_SignedByteEncoding_ValueMinus128_Result_ReadByValue(int __result) =>
        IntObserved = __result;

    [InnerPostfixConstant(-129)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.Int_Int32Encoding_ValueMinus129_Result))]
    public static void InnerPostfixConstant_Int_Int32Encoding_ValueMinus129_Result_ReadByValue(int __result) =>
        IntObserved = __result;

    [InnerPostfixConstant(127)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.Int_SignedByteEncoding_Value127_Result))]
    public static void InnerPostfixConstant_Int_SignedByteEncoding_Value127_Result_ReadByValue(int __result) =>
        IntObserved = __result;

    [InnerPostfixConstant(128)]
    [Target(typeof(ConstantTargets), nameof(ConstantTargets.Int_Int32Encoding_Value128_Result))]
    public static void InnerPostfixConstant_Int_Int32Encoding_Value128_Result_ReadByValue(int __result) => IntObserved = __result;
}

[TestFixture]
public sealed class InnerPostfixConstantTests : PatchTestBase
{
    [Test]
    public void InnerPostfixConstant_Int_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_Result_ReadByValue));

        int result = ConstantTargets.IntResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.IntValue));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(ConstantTargets.IntValue));
    }

    [Test]
    public void InnerPostfixConstant_Int_Result_ReadByReference()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_Result_ReadByReference));

        int result = ConstantTargets.IntResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.IntValue));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(ConstantTargets.IntValue));
    }

    [Test]
    public void InnerPostfixConstant_Int_Result_WriteByReference()
    {
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_Result_WriteByReference));

        int result = ConstantTargets.IntResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.IntReplacement));
    }

    [Test]
    public void InnerPostfixConstant_Long_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.LongObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Long_Result_ReadByValue));

        long result = ConstantTargets.LongResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.LongValue));
        Assert.That(InnerPostfixConstantPatches.LongObserved, Is.EqualTo(ConstantTargets.LongValue));
    }

    [Test]
    public void InnerPostfixConstant_Long_Result_ReadByReference()
    {
        InnerPostfixConstantPatches.LongObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Long_Result_ReadByReference));

        long result = ConstantTargets.LongResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.LongValue));
        Assert.That(InnerPostfixConstantPatches.LongObserved, Is.EqualTo(ConstantTargets.LongValue));
    }

    [Test]
    public void InnerPostfixConstant_Long_Result_WriteByReference()
    {
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Long_Result_WriteByReference));

        long result = ConstantTargets.LongResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.LongReplacement));
    }

    [Test]
    public void InnerPostfixConstant_Float_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.FloatObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Float_Result_ReadByValue));

        float result = ConstantTargets.FloatResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.FloatValue));
        Assert.That(InnerPostfixConstantPatches.FloatObserved, Is.EqualTo(ConstantTargets.FloatValue));
    }

    [Test]
    public void InnerPostfixConstant_Float_Result_ReadByReference()
    {
        InnerPostfixConstantPatches.FloatObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Float_Result_ReadByReference));

        float result = ConstantTargets.FloatResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.FloatValue));
        Assert.That(InnerPostfixConstantPatches.FloatObserved, Is.EqualTo(ConstantTargets.FloatValue));
    }

    [Test]
    public void InnerPostfixConstant_Float_Result_WriteByReference()
    {
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Float_Result_WriteByReference));

        float result = ConstantTargets.FloatResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.FloatReplacement));
    }

    [Test]
    public void InnerPostfixConstant_Double_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.DoubleObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Double_Result_ReadByValue));

        double result = ConstantTargets.DoubleResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.DoubleValue));
        Assert.That(InnerPostfixConstantPatches.DoubleObserved, Is.EqualTo(ConstantTargets.DoubleValue));
    }

    [Test]
    public void InnerPostfixConstant_Double_Result_ReadByReference()
    {
        InnerPostfixConstantPatches.DoubleObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Double_Result_ReadByReference));

        double result = ConstantTargets.DoubleResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.DoubleValue));
        Assert.That(InnerPostfixConstantPatches.DoubleObserved, Is.EqualTo(ConstantTargets.DoubleValue));
    }

    [Test]
    public void InnerPostfixConstant_Double_Result_WriteByReference()
    {
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Double_Result_WriteByReference));

        double result = ConstantTargets.DoubleResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.DoubleReplacement));
    }

    [Test]
    public void InnerPostfixConstant_String_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.StringObserved = null;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_String_Result_ReadByValue));

        string result = ConstantTargets.StringResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.StringValue));
        Assert.That(InnerPostfixConstantPatches.StringObserved, Is.EqualTo(ConstantTargets.StringValue));
    }

    [Test]
    public void InnerPostfixConstant_String_Result_ReadByReference()
    {
        InnerPostfixConstantPatches.StringObserved = null;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_String_Result_ReadByReference));

        string result = ConstantTargets.StringResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.StringValue));
        Assert.That(InnerPostfixConstantPatches.StringObserved, Is.EqualTo(ConstantTargets.StringValue));
    }

    [Test]
    public void InnerPostfixConstant_String_Result_WriteByReference()
    {
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_String_Result_WriteByReference));

        string result = ConstantTargets.StringResult();

        Assert.That(result, Is.EqualTo(ConstantTargets.StringReplacement));
    }

    [Test]
    public void InnerPostfixConstant_Int_SpecialEncoding_ValueMinus1_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_SpecialEncoding_ValueMinus1_Result_ReadByValue));

        int result = ConstantTargets.Int_SpecialEncoding_ValueMinus1_Result();

        Assert.That(result, Is.EqualTo(-1));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(-1));
    }

    [Test]
    public void InnerPostfixConstant_Int_SpecialEncoding_Value0_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = -1;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_SpecialEncoding_Value0_Result_ReadByValue));

        int result = ConstantTargets.Int_SpecialEncoding_Value0_Result();

        Assert.That(result, Is.Zero);
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.Zero);
    }

    [Test]
    public void InnerPostfixConstant_Int_SpecialEncoding_Value1_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_SpecialEncoding_Value1_Result_ReadByValue));

        int result = ConstantTargets.Int_SpecialEncoding_Value1_Result();

        Assert.That(result, Is.EqualTo(1));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(1));
    }

    [Test]
    public void InnerPostfixConstant_Int_SpecialEncoding_Value2_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_SpecialEncoding_Value2_Result_ReadByValue));

        int result = ConstantTargets.Int_SpecialEncoding_Value2_Result();

        Assert.That(result, Is.EqualTo(2));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(2));
    }

    [Test]
    public void InnerPostfixConstant_Int_SpecialEncoding_Value3_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_SpecialEncoding_Value3_Result_ReadByValue));

        int result = ConstantTargets.Int_SpecialEncoding_Value3_Result();

        Assert.That(result, Is.EqualTo(3));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(3));
    }

    [Test]
    public void InnerPostfixConstant_Int_SpecialEncoding_Value4_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_SpecialEncoding_Value4_Result_ReadByValue));

        int result = ConstantTargets.Int_SpecialEncoding_Value4_Result();

        Assert.That(result, Is.EqualTo(4));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(4));
    }

    [Test]
    public void InnerPostfixConstant_Int_SpecialEncoding_Value5_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_SpecialEncoding_Value5_Result_ReadByValue));

        int result = ConstantTargets.Int_SpecialEncoding_Value5_Result();

        Assert.That(result, Is.EqualTo(5));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(5));
    }

    [Test]
    public void InnerPostfixConstant_Int_SpecialEncoding_Value6_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_SpecialEncoding_Value6_Result_ReadByValue));

        int result = ConstantTargets.Int_SpecialEncoding_Value6_Result();

        Assert.That(result, Is.EqualTo(6));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(6));
    }

    [Test]
    public void InnerPostfixConstant_Int_SpecialEncoding_Value7_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_SpecialEncoding_Value7_Result_ReadByValue));

        int result = ConstantTargets.Int_SpecialEncoding_Value7_Result();

        Assert.That(result, Is.EqualTo(7));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(7));
    }

    [Test]
    public void InnerPostfixConstant_Int_SpecialEncoding_Value8_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_SpecialEncoding_Value8_Result_ReadByValue));

        int result = ConstantTargets.Int_SpecialEncoding_Value8_Result();

        Assert.That(result, Is.EqualTo(8));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(8));
    }

    [Test]
    public void InnerPostfixConstant_Int_SignedByteEncoding_ValueMinus128_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_SignedByteEncoding_ValueMinus128_Result_ReadByValue));

        int result = ConstantTargets.Int_SignedByteEncoding_ValueMinus128_Result();

        Assert.That(result, Is.EqualTo(-128));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(-128));
    }

    [Test]
    public void InnerPostfixConstant_Int_Int32Encoding_ValueMinus129_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_Int32Encoding_ValueMinus129_Result_ReadByValue));

        int result = ConstantTargets.Int_Int32Encoding_ValueMinus129_Result();

        Assert.That(result, Is.EqualTo(-129));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(-129));
    }

    [Test]
    public void InnerPostfixConstant_Int_SignedByteEncoding_Value127_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_SignedByteEncoding_Value127_Result_ReadByValue));

        int result = ConstantTargets.Int_SignedByteEncoding_Value127_Result();

        Assert.That(result, Is.EqualTo(127));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(127));
    }

    [Test]
    public void InnerPostfixConstant_Int_Int32Encoding_Value128_Result_ReadByValue()
    {
        InnerPostfixConstantPatches.IntObserved = 0;
        ApplyPatch(
            typeof(InnerPostfixConstantPatches),
            nameof(InnerPostfixConstantPatches.InnerPostfixConstant_Int_Int32Encoding_Value128_Result_ReadByValue));

        int result = ConstantTargets.Int_Int32Encoding_Value128_Result();

        Assert.That(result, Is.EqualTo(128));
        Assert.That(InnerPostfixConstantPatches.IntObserved, Is.EqualTo(128));
    }
}
