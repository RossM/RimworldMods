using System;
using NUnit.Framework;

namespace Disharmony.Tests
{
    public static class StaticMethodTargets
    {
        public static void Void() { }
        public static void IntArgument(int value) { }
        public static void StringArgument(string value) { }
        public static void RefIntArgument(ref int value) { }
        public static void RefStringArgument(ref string value) { }
        public static int IntIdentity(int value) => value;
        public static string StringIdentity(string value) => value;
        public static int IntResult() => 1;
        public static string StringResult() => "original";

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
    }

    public sealed class ClassMethodTargets
    {
        public int foo;
        public int Value { get; private set; }

        public void Void() { }

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

        public void CallStaticVoid() => InnerStaticMethodTargets.Void();
        public void CallInnerWithoutField(InstanceMethodTargetsWithoutFields inner) => inner.Void();
        public void CallInnerWithField(InnerInstanceMethodTargets inner) => inner.Void();
    }

    public struct StructMethodTargets
    {
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
    }

    public sealed class InnerInstanceMethodTargets
    {
        public int foo;
        public void Void() { }
    }

    public sealed class InstanceMethodTargetsWithoutFields
    {
        public void Void() { }
    }

    public static class InnerStaticMethodTargets
    {
        public static void Void() { }
        public static void IntArgument(int value) { }
        public static void RefIntArgument(ref int value) { }
        public static int IntIdentity(int value) => value;
        public static int IntResult() => 1;
    }

    public static class OuterStaticMethodTargets
    {
        public static int IntResult() => InnerStaticMethodTargets.IntResult();
        public static void IntArgument(int value) => InnerStaticMethodTargets.IntArgument(value);
        public static int IntIdentity(int value) => InnerStaticMethodTargets.IntIdentity(value);
        public static void RefIntArgument(ref int value) => InnerStaticMethodTargets.RefIntArgument(ref value);
        public static void OuterArgument(int outerValue) => InnerStaticMethodTargets.Void();
        public static void SameNamedArgument(int value) => InnerStaticMethodTargets.IntArgument(value + 41);
    }

    public static class LocalFunctionTargets
    {
        public static int CapturedVariableMethod(int value)
        {
            int captured = value;
            return LocalMethod();

            int LocalMethod() => captured;
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
    }
}
