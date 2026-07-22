namespace Disharmony.Tests;

public static class InnerParameterBindingPatchMethods
{
    public static ClassMethodTargets? CallerObserved;
    public static ClassMethodTargets? ReplacementCaller;
    public static int FieldObserved;
    public static int CapturedVariableObserved;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallStaticVoid))]
    public static void CaptureCallerPrefix(ClassMethodTargets __caller) => CallerObserved = __caller;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallStaticVoid))]
    public static void CaptureCallerPostfix(ClassMethodTargets __caller) => CallerObserved = __caller;

    [InnerPrefix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallStaticVoidAndReturnValue))]
    public static void WriteCallerPrefix(ref ClassMethodTargets __caller) => __caller = ReplacementCaller!;

    [InnerPostfix(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallStaticVoidAndReturnValue))]
    public static void WriteCallerPostfix(ref ClassMethodTargets __caller) => __caller = ReplacementCaller!;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithoutField))]
    public static void ReadOuterFieldPrefix(int ___foo) => FieldObserved = ___foo;

    [InnerPostfix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void ReadInnerFieldPostfix(int ___foo) => FieldObserved = ___foo;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithoutField))]
    public static void WriteOuterFieldPrefix(ref int ___foo) => ___foo = 42;

    [InnerPostfix(typeof(InnerInstanceMethodTargets), nameof(InnerInstanceMethodTargets.Void))]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.CallInnerWithField))]
    public static void WriteInnerFieldPostfix(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithoutField))]
    public static void ReadOuterStructFieldPrefix(int ___foo) => FieldObserved = ___foo;

    [InnerPostfix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithField))]
    public static void ReadInnerStructFieldPostfix(int ___foo) => FieldObserved = ___foo;

    [InnerPrefix(typeof(InstanceMethodTargetsWithoutFields), nameof(InstanceMethodTargetsWithoutFields.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithoutField))]
    public static void WriteOuterStructFieldPrefix(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithField))]
    public static void WriteInnerStructFieldPrefix(ref int ___foo) => ___foo = 42;

    [InnerPrefix(typeof(InnerStructMethodTargets), nameof(InnerStructMethodTargets.Void))]
    [Target(typeof(StructMethodTargets), nameof(StructMethodTargets.CallInnerWithFieldByValue))]
    public static void WriteInnerStructFieldPassedByValuePrefix(ref int ___foo) => ___foo = 42;

    [Prefix]
    [Target(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    public static void ReadCapturedVariablePrefix(int captured) => CapturedVariableObserved = captured;

    [Postfix]
    [Target(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    public static void ReadCapturedVariablePostfix(int captured) => CapturedVariableObserved = captured;

    [InnerPrefix(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedVariableMethod))]
    public static void ReadCapturedVariableInnerPrefix(int captured) => CapturedVariableObserved = captured;

    [InnerPostfix(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedVariableMethod))]
    public static void ReadCapturedVariableInnerPostfix(int captured) => CapturedVariableObserved = captured;

    [Prefix]
    [Target(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    public static void WriteCapturedVariablePrefix(ref int captured) => captured = 42;

    [Postfix]
    [Target(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    public static void WriteCapturedVariablePostfix(ref int captured) => captured = 42;

    [InnerPrefix(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedVariableMethod))]
    public static void WriteCapturedVariableInnerPrefix(ref int captured) => captured = 42;

    [InnerPostfix(typeof(LocalFunctionTargets), "CapturedVariableMethod.LocalMethod")]
    [Target(typeof(LocalFunctionTargets), nameof(LocalFunctionTargets.CapturedVariableMethod))]
    public static void WriteCapturedVariableInnerPostfix(ref int captured) => captured = 42;
}