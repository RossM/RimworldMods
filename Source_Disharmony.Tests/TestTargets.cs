namespace Disharmony.Tests
{
    public sealed class BindingReference
    {
        public int Value;
    }

    public struct BindingStruct
    {
        public int Value;
    }

    public static class StaticMethodTargets
    {
        public static void Void() { }
        public static void IntArgument(int value) { }
        public static void StringArgument(string value) { }
        public static void StructArgument(BindingStruct value) { }
        public static void RefIntArgument(ref int value) { }
        public static void RefStringArgument(ref string value) { }
        public static void RefStructArgument(ref BindingStruct value) { }
        public static int IntIdentity(int value) => value;
        public static string StringIdentity(string value) => value;
        public static BindingStruct StructIdentity(BindingStruct value) => value;
        public static int IntResult() => 1;
        public static string StringResult() => "original";
        public static BindingStruct StructResult() => new BindingStruct { Value = 1 };
        public static int RegistrationResultA() => 1;
        public static int RegistrationResultB() => 2;
        public static int MutableProperty { get; set; }
        public static void OverloadedVoid(int value) { }
        public static void OverloadedVoid(string value) { }
        public static T GenericIdentity<T>(T value) => value;

        public static void ThrowingIntArgument(int value) =>
            Assert.Fail("The target should have been skipped.");

        public static int ThrowingIntResult()
        {
            Assert.Fail("The target should have been skipped.");
            return 1;
        }

        public static string ThrowingStringResult()
        {
            Assert.Fail("The target should have been skipped.");
            return "original";
        }

        public static BindingStruct ThrowingStructResult()
        {
            Assert.Fail("The target should have been skipped.");
            return new BindingStruct { Value = 1 };
        }
    }

    public sealed class ClassMethodTargets
    {
        public int foo;
        public int primitiveField;
        public BindingReference referenceField = new BindingReference();
        public BindingStruct structField;
        public int Value { get; private set; }

        public void Void() { }
        public ClassMethodTargets Self() => this;

        public int IntIdentity(int value)
        {
            Value = value;
            return Value;
        }

        public int IntSum(int first, int second)
        {
            Value = first + second;
            return Value;
        }

        public int IntResult()
        {
            Value = 1;
            return Value;
        }

        public void CallStaticVoid() => InnerStaticMethodTargets.Void();
        public int CallStaticVoidAndReturnValue()
        {
            InnerStaticMethodTargets.Void();
            return Value;
        }

        public void CallInnerWithoutField(InstanceMethodTargetsWithoutFields inner) => inner.Void();
        public void CallInnerWithField(InnerInstanceMethodTargets inner) => inner.Void();

        public IEnumerable<int> EnumerateIdentity(int outerValue)
        {
            _ = InnerStaticMethodTargets.IntIdentity(outerValue);
            yield return outerValue;
        }

        public IEnumerable<BindingReference> EnumerateReferenceIdentity(BindingReference outerValue)
        {
            _ = InnerStaticMethodTargets.StringIdentity(outerValue.Value.ToString());
            yield return outerValue;
        }

        public IEnumerable<BindingStruct> EnumerateStructIdentity(BindingStruct outerValue)
        {
            _ = InnerStaticMethodTargets.StructIdentity(outerValue);
            yield return outerValue;
        }

        public IEnumerable<int> EnumerateDeclaringInstanceValue()
        {
            _ = InnerStaticMethodTargets.IntResult();
            yield return foo;
        }
    }

    public struct StructMethodTargets
    {
        public int foo;
        public int Value { get; private set; }

        public int IntIdentity(int value)
        {
            Value = value;
            return Value;
        }

        public int IntResult()
        {
            Value = 1;
            return Value;
        }

        public void CallInnerWithoutField(InstanceMethodTargetsWithoutFields inner) => inner.Void();
        public void CallInnerWithField(ref InnerStructMethodTargets inner) => inner.Void();
        public void CallInnerWithFieldByValue(InnerStructMethodTargets inner) => inner.Void();

        public IEnumerable<int> EnumerateDeclaringInstanceValue()
        {
            _ = InnerStaticMethodTargets.IntResult();
            yield return foo;
        }
    }

    public sealed class InnerInstanceMethodTargets
    {
        public int foo;
        public void Void() { }
    }

    public struct InnerStructMethodTargets
    {
        public static int FieldObserved;
        public int foo;
        public void Void() => FieldObserved = foo;
    }

    public sealed class InstanceMethodTargetsWithoutFields
    {
        public void Void() { }
    }

    public static class InnerStaticMethodTargets
    {
        public static int Field = 1;
        public static int Property => 1;

        public static void Void() { }
        public static void IntArgument(int value) { }
        public static void StringArgument(string value) { }
        public static void StructArgument(BindingStruct value) { }
        public static void RefIntArgument(ref int value) { }
        public static void RefStringArgument(ref string value) { }
        public static void RefStructArgument(ref BindingStruct value) { }
        public static int IntIdentity(int value) => value;
        public static string StringIdentity(string value) => value;
        public static BindingStruct StructIdentity(BindingStruct value) => value;
        public static int IntResult() => 1;
        public static string StringResult() => "original";
        public static BindingStruct StructResult() => new BindingStruct { Value = 1 };
    }

    public static class ExceptionHandlingTargets
    {
        public static void CallInTryBlock(bool throwException)
        {
            try
            {
                if (throwException)
                    throw new InvalidOperationException();

                InnerStaticMethodTargets.Void();
            }
            catch (InvalidOperationException)
            {
            }
        }

        public static void CallInCatchBlock(bool throwException)
        {
            try
            {
                if (throwException)
                    throw new InvalidOperationException();
            }
            catch (InvalidOperationException)
            {
                InnerStaticMethodTargets.Void();
            }
        }

        public static void CallInFinallyBlock(bool throwException)
        {
            try
            {
                if (throwException)
                    throw new InvalidOperationException();
            }
            finally
            {
                InnerStaticMethodTargets.Void();
            }
        }
    }

    public static class ConstantTargets
    {
        public const int IntValue = 42;
        public const int IntReplacement = 43;
        public const long LongValue = 42000000000L;
        public const long LongReplacement = 43000000000L;
        public const float FloatValue = 4.25f;
        public const float FloatReplacement = 5.25f;
        public const double DoubleValue = 8.5;
        public const double DoubleReplacement = 9.5;
        public const string StringValue = "original";
        public const string StringReplacement = "patched";

        public static int IntResult() => IntValue;
        public static int Int_SpecialEncoding_ValueMinus1_Result() => -1;
        public static int Int_SpecialEncoding_Value0_Result() => 0;
        public static int Int_SpecialEncoding_Value1_Result() => 1;
        public static int Int_SpecialEncoding_Value2_Result() => 2;
        public static int Int_SpecialEncoding_Value3_Result() => 3;
        public static int Int_SpecialEncoding_Value4_Result() => 4;
        public static int Int_SpecialEncoding_Value5_Result() => 5;
        public static int Int_SpecialEncoding_Value6_Result() => 6;
        public static int Int_SpecialEncoding_Value7_Result() => 7;
        public static int Int_SpecialEncoding_Value8_Result() => 8;
        public static int Int_SignedByteEncoding_ValueMinus128_Result() => -128;
        public static int Int_Int32Encoding_ValueMinus129_Result() => -129;
        public static int Int_SignedByteEncoding_Value127_Result() => 127;
        public static int Int_Int32Encoding_Value128_Result() => 128;
        public static long LongResult() => LongValue;
        public static float FloatResult() => FloatValue;
        public static double DoubleResult() => DoubleValue;
        public static string StringResult() => StringValue;
    }

    public static class OuterStaticMethodTargets
    {
        public static int IntResult() => InnerStaticMethodTargets.IntResult();
        public static string StringResult() => InnerStaticMethodTargets.StringResult();
        public static BindingStruct StructResult() => InnerStaticMethodTargets.StructResult();
        public static int FieldResult() => InnerStaticMethodTargets.Field;
        public static int PropertyResult() => InnerStaticMethodTargets.Property;
        public static int ReadInstanceField(InnerInstanceMethodTargets inner) => inner.foo;
        public static int ReadStructField(InnerStructMethodTargets inner) => inner.foo;
        public static void IntArgument(int value) => InnerStaticMethodTargets.IntArgument(value);
        public static void StringArgument(string value) => InnerStaticMethodTargets.StringArgument(value);
        public static void StructArgument(BindingStruct value) => InnerStaticMethodTargets.StructArgument(value);
        public static int IntIdentity(int value) => InnerStaticMethodTargets.IntIdentity(value);
        public static string StringIdentity(string value) => InnerStaticMethodTargets.StringIdentity(value);
        public static BindingStruct StructIdentity(BindingStruct value) => InnerStaticMethodTargets.StructIdentity(value);
        public static void RefIntArgument(ref int value) => InnerStaticMethodTargets.RefIntArgument(ref value);
        public static void RefStringArgument(ref string value) => InnerStaticMethodTargets.RefStringArgument(ref value);
        public static void RefStructArgument(ref BindingStruct value) => InnerStaticMethodTargets.RefStructArgument(ref value);
        public static void OuterArgument(int outerValue) => InnerStaticMethodTargets.Void();
        public static void OuterReferenceTypeArgument(string outerValue) => InnerStaticMethodTargets.Void();
        public static void OuterStructArgument(BindingStruct outerValue) => InnerStaticMethodTargets.Void();
        public static void SameNamedArgument(int value) => InnerStaticMethodTargets.IntArgument(value + 41);
        public static void SameNamedReferenceTypeArgument(string value) => InnerStaticMethodTargets.StringArgument("inner");
        public static void SameNamedStructArgument(BindingStruct value) =>
            InnerStaticMethodTargets.StructArgument(new BindingStruct { Value = 42 });
        public static int SameNamedRefArgument(ref int value)
        {
            int innerValue = 1;
            InnerStaticMethodTargets.RefIntArgument(ref innerValue);
            return innerValue;
        }

        public static string SameNamedRefReferenceTypeArgument(ref string value)
        {
            string innerValue = "inner";
            InnerStaticMethodTargets.RefStringArgument(ref innerValue);
            return innerValue;
        }

        public static BindingStruct SameNamedRefStructArgument(ref BindingStruct value)
        {
            var innerValue = new BindingStruct { Value = 1 };
            InnerStaticMethodTargets.RefStructArgument(ref innerValue);
            return innerValue;
        }

        public static IEnumerable<int> EnumerateIntResult()
        {
            yield return InnerStaticMethodTargets.IntResult();
        }

    }

    public static class LocalFunctionTargets
    {
        public static int InvokeAnonymousLambda(int value)
        {
            Func<int, int> lambda = lambdaParameter => lambdaParameter;
            return lambda(value);
        }

        public static int CapturedVariableMethod(int value)
        {
            int captured = value;
            _ = LocalMethod();
            return captured;

            int LocalMethod() => captured;
        }

        public static BindingReference CapturedReferenceVariableMethod(BindingReference value)
        {
            BindingReference captured = value;
            _ = LocalMethod();
            return captured;

            BindingReference LocalMethod() => captured;
        }

        public static BindingStruct CapturedStructVariableMethod(BindingStruct value)
        {
            BindingStruct captured = value;
            _ = LocalMethod();
            return captured;

            BindingStruct LocalMethod() => captured;
        }

        public static IEnumerable<int> PrimitiveLocalIterator(int enclosingValue)
        {
            return LocalIterator();

            IEnumerable<int> LocalIterator()
            {
                _ = InnerStaticMethodTargets.IntIdentity(enclosingValue);
                yield return enclosingValue;
            }
        }

        public static IEnumerable<BindingReference> ReferenceTypeLocalIterator(BindingReference enclosingValue)
        {
            return LocalIterator();

            IEnumerable<BindingReference> LocalIterator()
            {
                _ = InnerStaticMethodTargets.StringIdentity(enclosingValue.Value.ToString());
                yield return enclosingValue;
            }
        }

        public static IEnumerable<BindingStruct> StructLocalIterator(BindingStruct enclosingValue)
        {
            return LocalIterator();

            IEnumerable<BindingStruct> LocalIterator()
            {
                _ = InnerStaticMethodTargets.StructIdentity(enclosingValue);
                yield return enclosingValue;
            }
        }
    }
}

namespace Disharmony.Tests.ReflectionFixtures
{
    public static class LookupTarget
    {
        public static class NestedTarget
        {
            public static void Method(int value)
            {
            }
        }

        public static int Field;
        public static int Property { get; set; }
        public static int ReadOnlyProperty => 1;
        public static int WriteOnlyProperty { set { } }

        public static void Method(int value)
        {
        }

        public static void RefMethod(ref int value)
        {
        }

        public static void InMethod(in int value)
        {
        }

        public static void OutMethod(out int value) => value = 0;

        public static void OverloadedMethod(int value)
        {
        }

        public static void OverloadedMethod(string value)
        {
        }

        public static void GenericMethod<T>(T value)
        {
        }

        public static void NonGenericMethod(int value)
        {
        }

        public static void MixedMethod(int value)
        {
        }

        public static void MixedMethod<T>(T value)
        {
        }

        public static Func<int, int> StaticLocalMethodContainer()
        {
            return StaticLocalMethod;

            static int StaticLocalMethod(int value) => value;
        }

        public static Func<int> CapturedLocalMethodContainer(int value)
        {
            return CapturedLocalMethod;

            int CapturedLocalMethod() => value;
        }

        public static Func<int, int> LambdaContainer() => value => value;
    }
}
