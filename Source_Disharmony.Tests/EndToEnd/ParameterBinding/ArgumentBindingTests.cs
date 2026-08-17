namespace Disharmony.Tests.EndToEnd.ParameterBinding;

public static partial class ArgumentBindingPatches
{
    public static int ValueObserved;
    public static string? ReferenceObserved;
    public static BindingStruct StructObserved;
    public static int InnerObserved;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefIntArgument))]
    public static void Prefix_RefArgument_Primitive_ReadByValue(int value) => ValueObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefIntArgument))]
    public static void Prefix_RefArgument_Primitive_WriteByReference(ref int value) => value = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStringArgument))]
    public static void Prefix_RefArgument_ReferenceType_ReadByValue(string value) => ReferenceObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStringArgument))]
    public static void Prefix_RefArgument_ReferenceType_WriteByReference(ref string value) => value = "patched";

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStructArgument))]
    public static void Prefix_RefArgument_Struct_ReadByValue(BindingStruct value) => StructObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStructArgument))]
    public static void Prefix_RefArgument_Struct_WriteByReference(ref BindingStruct value) =>
        value = new BindingStruct { Value = 42 };

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void Prefix_Argument_Primitive_ReadByValue(int value) => ValueObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void Postfix_Argument_Primitive_ReadByValue(int value) => ValueObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringArgument))]
    public static void Prefix_Argument_ReferenceType_ReadByValue(string value) => ReferenceObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructArgument))]
    public static void Prefix_Argument_Struct_ReadByValue(BindingStruct value) => StructObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringArgument))]
    public static void Postfix_Argument_ReferenceType_ReadByValue(string value) => ReferenceObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntIdentity))]
    public static void Prefix_Argument_Primitive_WriteByReference(ref int value) => value = 42;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefIntArgument))]
    public static void Postfix_Argument_Primitive_WriteByReference(ref int value) => value = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringIdentity))]
    public static void Prefix_Argument_ReferenceType_WriteByReference(ref string value) => value = "patched";

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructIdentity))]
    public static void Prefix_Argument_Struct_WriteByReference(ref BindingStruct value) =>
        value = new BindingStruct { Value = 42 };

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStringArgument))]
    public static void Postfix_Argument_ReferenceType_WriteByReference(ref string value) => value = "patched";

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPrefix_OuterArgument_Primitive_ReadByValue_WhenInnerHasNoMatch(int outerValue) => InnerObserved = outerValue;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterReferenceTypeArgument))]
    public static void InnerPrefix_OuterArgument_ReferenceType_ReadByValue_WhenInnerHasNoMatch(string outerValue) =>
        ReferenceObserved = outerValue;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterStructArgument))]
    public static void InnerPrefix_OuterArgument_Struct_ReadByValue_WhenInnerHasNoMatch(BindingStruct outerValue) =>
        StructObserved = outerValue;

    [Postfix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPostfix_OuterArgument_Primitive_ReadByValue_WhenInnerHasNoMatch(int outerValue) => InnerObserved = outerValue;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPrefix_OuterArgument_Primitive_WriteByReference_Rejected(ref int outerValue) => outerValue = 42;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterReferenceTypeArgument))]
    public static void InnerPrefix_OuterArgument_ReferenceType_WriteByReference_Rejected(ref string outerValue) =>
        outerValue = "patched";

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterStructArgument))]
    public static void InnerPrefix_OuterArgument_Struct_WriteByReference_Rejected(ref BindingStruct outerValue) =>
        outerValue = new BindingStruct { Value = 42 };

    [Postfix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPostfix_OuterArgument_Primitive_WriteByReference_Rejected(ref int outerValue) => outerValue = 42;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedArgument))]
    public static void InnerPrefix_InnerArgument_Primitive_ReadByValue_WhenOuterHasSameName(int value) => InnerObserved = value;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedReferenceTypeArgument))]
    public static void InnerPrefix_InnerArgument_ReferenceType_ReadByValue_WhenOuterHasSameName(string value) =>
        ReferenceObserved = value;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedStructArgument))]
    public static void InnerPrefix_InnerArgument_Struct_ReadByValue_WhenOuterHasSameName(BindingStruct value) =>
        StructObserved = value;

    [Postfix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedArgument))]
    public static void InnerPostfix_InnerArgument_Primitive_ReadByValue_WhenOuterHasSameName(int value) => InnerObserved = value;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefArgument))]
    public static void InnerPrefix_InnerArgument_Primitive_WriteByReference_WhenOuterHasSameName(ref int value) => value = 42;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefStringArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefReferenceTypeArgument))]
    public static void InnerPrefix_InnerArgument_ReferenceType_WriteByReference_WhenOuterHasSameName(ref string value) =>
        value = "patched";

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefStructArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefStructArgument))]
    public static void InnerPrefix_InnerArgument_Struct_WriteByReference_WhenOuterHasSameName(ref BindingStruct value) =>
        value = new BindingStruct { Value = 42 };

    [Postfix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefArgument))]
    public static void InnerPostfix_InnerArgument_Primitive_WriteByReference_WhenOuterHasSameName(ref int value) => value = 42;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntArgument))]
    public static void InnerPrefix_InnerArgument_Primitive_ReadByValue(int value) => InnerObserved = value;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringArgument))]
    public static void InnerPrefix_InnerArgument_ReferenceType_ReadByValue(string value) => ReferenceObserved = value;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructArgument))]
    public static void InnerPrefix_InnerArgument_Struct_ReadByValue(BindingStruct value) => StructObserved = value;

    [Postfix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntArgument))]
    public static void InnerPostfix_InnerArgument_Primitive_ReadByValue(int value) => InnerObserved = value;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntIdentity))]
    public static void InnerPrefix_InnerArgument_Primitive_WriteByReference(ref int value) => value = 42;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringIdentity))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringIdentity))]
    public static void InnerPrefix_InnerArgument_ReferenceType_WriteByReference(ref string value) => value = "patched";

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructIdentity))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructIdentity))]
    public static void InnerPrefix_InnerArgument_Struct_WriteByReference(ref BindingStruct value) =>
        value = new BindingStruct { Value = 42 };

    [Postfix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.RefIntArgument))]
    public static void InnerPostfix_InnerArgument_Primitive_WriteByReference(ref int value) => value = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntIdentity))]
    public static void Prefix_ParameterAttribute_StaticMethod_Index0_Primitive_WriteByReference([Parameter(0)] ref int replacement) =>
        replacement = 42;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void Prefix_ParameterAttribute_StaticMethod_Index0_Primitive_ReadByValue([Parameter(0)] int argument) =>
        ValueObserved = argument;

    [Prefix]
    [Target(typeof(LocalFunctionTargets), "InvokeAnonymousLambda.*")]
    public static void Prefix_ParameterAttribute_AnonymousLambda_Index0_Primitive_ReadByValue([Parameter(0)] int argument) =>
        ValueObserved = argument;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.IntIdentity))]
    public static void Prefix_ParameterAttribute_InstanceMethod_Index0_Primitive_ReadByValue(
        [Parameter(0)] int argument) => ValueObserved = argument;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.IntIdentity))]
    public static void Prefix_ParameterAttribute_InstanceMethod_Index0_Primitive_ReadByReference(
        [Parameter(0)] ref int argument) => ValueObserved = argument;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.IntIdentity))]
    public static void Prefix_ParameterAttribute_InstanceMethod_Index0_Primitive_WriteByReference(
        [Parameter(0)] ref int argument) => argument = 42;

    [Prefix]
    [Target(typeof(ClassMethodTargets), nameof(ClassMethodTargets.IntSum))]
    public static void Prefix_ParameterAttribute_InstanceMethod_Index1_Primitive_ReadByValue(
        [Parameter(1)] int argument) => ValueObserved = argument;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringArgument))]
    public static void Prefix_ParameterAttribute_StaticMethod_Index0_ReferenceType_ReadByValue([Parameter(0)] string argument) =>
        ReferenceObserved = argument;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringIdentity))]
    public static void Prefix_ParameterAttribute_StaticMethod_Index0_ReferenceType_WriteByReference([Parameter(0)] ref string argument) =>
        argument = "patched";

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructArgument))]
    public static void Prefix_ParameterAttribute_StaticMethod_Index0_Struct_ReadByValue([Parameter(0)] BindingStruct argument) =>
        StructObserved = argument;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructIdentity))]
    public static void Prefix_ParameterAttribute_StaticMethod_Index0_Struct_WriteByReference([Parameter(0)] ref BindingStruct argument) =>
        argument = new BindingStruct { Value = 42 };

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedArgument))]
    public static void InnerPrefix_ParameterAttribute_OuterScope_Primitive_ReadByValue(
        [Parameter("value", Scope.Outer)] int outerValue) => InnerObserved = outerValue;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedArgument))]
    public static void InnerPrefix_ParameterAttribute_InnerScope_Primitive_ReadByValue(
        [Parameter("value", Scope.Inner)] int innerValue) => InnerObserved = innerValue;
}

public static partial class ArgumentBindingPatches
{
    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void Postfix_ValueArgument_Primitive_ReadByReference_Rejected(ref int value) =>
        ValueObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntArgument))]
    public static void Postfix_ParameterAttribute_ValueArgument_Primitive_WriteByReference_Rejected(
        [Parameter(0)] ref int value) => value = 42;

    [Postfix]
    [Target(typeof(LocalFunctionTargets), "InvokeAnonymousLambda.*")]
    public static void Postfix_ParameterAttribute_AnonymousLambda_Index0_Primitive_ReadByReference_Rejected(
        [Parameter(0)] ref int argument) => ValueObserved = argument;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPrefix_ParameterAttribute_OuterArgument_Primitive_ReadByReference_Rejected(
        [Parameter(0, Scope.Outer)] ref int outerValue) => InnerObserved = outerValue;

    [Postfix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPostfix_ParameterAttribute_OuterArgument_Primitive_WriteByReference_Rejected(
        [Parameter(0, Scope.Outer)] ref int outerValue) => outerValue = 42;

    [Postfix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntArgument))]
    public static void InnerPostfix_InnerValueArgument_Primitive_ReadByReference_Rejected(ref int value) =>
        InnerObserved = value;

    [Postfix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntArgument))]
    public static void InnerPostfix_InnerValueArgument_Primitive_WriteByReference_Rejected(ref int value) =>
        value = 42;
}

[TestFixture]
public sealed partial class ArgumentBindingTests
{
    [Test]
    public void Postfix_ValueArgument_Primitive_ReadByReference_Rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(ArgumentBindingPatches),
                nameof(ArgumentBindingPatches.Postfix_ValueArgument_Primitive_ReadByReference_Rejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
    }

    [Test]
    public void Postfix_ParameterAttribute_ValueArgument_Primitive_WriteByReference_Rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(ArgumentBindingPatches),
                nameof(ArgumentBindingPatches.Postfix_ParameterAttribute_ValueArgument_Primitive_WriteByReference_Rejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
    }

    [Test]
    public void Postfix_ParameterAttribute_AnonymousLambda_Index0_Primitive_ReadByReference_Rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(ArgumentBindingPatches),
                nameof(ArgumentBindingPatches.Postfix_ParameterAttribute_AnonymousLambda_Index0_Primitive_ReadByReference_Rejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
    }

    [Test]
    public void InnerPrefix_ParameterAttribute_OuterArgument_Primitive_ReadByReference_Rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(ArgumentBindingPatches),
                nameof(ArgumentBindingPatches.InnerPrefix_ParameterAttribute_OuterArgument_Primitive_ReadByReference_Rejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
    }

    [Test]
    public void InnerPostfix_ParameterAttribute_OuterArgument_Primitive_WriteByReference_Rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(ArgumentBindingPatches),
                nameof(ArgumentBindingPatches.InnerPostfix_ParameterAttribute_OuterArgument_Primitive_WriteByReference_Rejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
    }

    [Test]
    public void InnerPostfix_InnerValueArgument_Primitive_ReadByReference_Rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(ArgumentBindingPatches),
                nameof(ArgumentBindingPatches.InnerPostfix_InnerValueArgument_Primitive_ReadByReference_Rejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
    }

    [Test]
    public void InnerPostfix_InnerValueArgument_Primitive_WriteByReference_Rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(
                typeof(ArgumentBindingPatches),
                nameof(ArgumentBindingPatches.InnerPostfix_InnerValueArgument_Primitive_WriteByReference_Rejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
    }
}

public static partial class ArgumentBindingPatches
{
    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefIntArgument))]
    public static void Prefix_RefArgument_Primitive_ReadByReference(ref int value) => ValueObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStringArgument))]
    public static void Prefix_RefArgument_ReferenceType_ReadByReference(ref string value) => ReferenceObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStructArgument))]
    public static void Prefix_RefArgument_Struct_ReadByReference(ref BindingStruct value) => StructObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntIdentity))]
    public static void Prefix_Argument_Primitive_ReadByReference(ref int value) => ValueObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefIntArgument))]
    public static void Postfix_Argument_Primitive_ReadByReference(ref int value) => ValueObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringIdentity))]
    public static void Prefix_Argument_ReferenceType_ReadByReference(ref string value) => ReferenceObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructIdentity))]
    public static void Prefix_Argument_Struct_ReadByReference(ref BindingStruct value) => StructObserved = value;

    [Postfix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.RefStringArgument))]
    public static void Postfix_Argument_ReferenceType_ReadByReference(ref string value) => ReferenceObserved = value;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.IntIdentity))]
    public static void Prefix_ParameterAttribute_StaticMethod_Index0_Primitive_ReadByReference(
        [Parameter(0)] ref int argument) => ValueObserved = argument;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StringIdentity))]
    public static void Prefix_ParameterAttribute_StaticMethod_Index0_ReferenceType_ReadByReference(
        [Parameter(0)] ref string argument) => ReferenceObserved = argument;

    [Prefix]
    [Target(typeof(StaticMethodTargets), nameof(StaticMethodTargets.StructIdentity))]
    public static void Prefix_ParameterAttribute_StaticMethod_Index0_Struct_ReadByReference(
        [Parameter(0)] ref BindingStruct argument) => StructObserved = argument;
}

[TestFixture]
public sealed partial class ArgumentBindingTests
{
    [Test]
    public void Prefix_RefArgument_Primitive_ReadByReference()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_RefArgument_Primitive_ReadByReference));
        int value = 42;
        StaticMethodTargets.RefIntArgument(ref value);
        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_RefArgument_ReferenceType_ReadByReference()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_RefArgument_ReferenceType_ReadByReference));
        string value = "original";
        StaticMethodTargets.RefStringArgument(ref value);
        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Prefix_RefArgument_Struct_ReadByReference()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_RefArgument_Struct_ReadByReference));
        var value = new BindingStruct { Value = 42 };
        StaticMethodTargets.RefStructArgument(ref value);
        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_Argument_Primitive_ReadByReference()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_Argument_Primitive_ReadByReference));
        StaticMethodTargets.IntIdentity(42);
        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_Argument_Primitive_ReadByReference()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Postfix_Argument_Primitive_ReadByReference));
        int value = 42;
        StaticMethodTargets.RefIntArgument(ref value);
        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_Argument_ReferenceType_ReadByReference()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_Argument_ReferenceType_ReadByReference));
        StaticMethodTargets.StringIdentity("original");
        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Prefix_Argument_Struct_ReadByReference()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_Argument_Struct_ReadByReference));
        StaticMethodTargets.StructIdentity(new BindingStruct { Value = 42 });
        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_Argument_ReferenceType_ReadByReference()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Postfix_Argument_ReferenceType_ReadByReference));
        string value = "original";
        StaticMethodTargets.RefStringArgument(ref value);
        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Prefix_ParameterAttribute_StaticMethod_Index0_Primitive_ReadByReference()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.Prefix_ParameterAttribute_StaticMethod_Index0_Primitive_ReadByReference));
        StaticMethodTargets.IntIdentity(42);
        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_ParameterAttribute_StaticMethod_Index0_ReferenceType_ReadByReference()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.Prefix_ParameterAttribute_StaticMethod_Index0_ReferenceType_ReadByReference));
        StaticMethodTargets.StringIdentity("original");
        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Prefix_ParameterAttribute_StaticMethod_Index0_Struct_ReadByReference()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.Prefix_ParameterAttribute_StaticMethod_Index0_Struct_ReadByReference));
        StaticMethodTargets.StructIdentity(new BindingStruct { Value = 42 });
        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }
}

public static partial class ArgumentBindingPatches
{
    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPrefix_OuterArgument_Primitive_ReadByReference_Rejected(ref int outerValue) => InnerObserved = outerValue;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterReferenceTypeArgument))]
    public static void InnerPrefix_OuterArgument_ReferenceType_ReadByReference_Rejected(ref string outerValue) =>
        ReferenceObserved = outerValue;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterStructArgument))]
    public static void InnerPrefix_OuterArgument_Struct_ReadByReference_Rejected(ref BindingStruct outerValue) =>
        StructObserved = outerValue;

    [Postfix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.Void))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.OuterArgument))]
    public static void InnerPostfix_OuterArgument_Primitive_ReadByReference_Rejected(ref int outerValue) => InnerObserved = outerValue;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefArgument))]
    public static void InnerPrefix_InnerArgument_Primitive_ReadByReference_WhenOuterHasSameName(ref int value) =>
        InnerObserved = value;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefStringArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefReferenceTypeArgument))]
    public static void InnerPrefix_InnerArgument_ReferenceType_ReadByReference_WhenOuterHasSameName(
        ref string value) => ReferenceObserved = value;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefStructArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefStructArgument))]
    public static void InnerPrefix_InnerArgument_Struct_ReadByReference_WhenOuterHasSameName(
        ref BindingStruct value) => StructObserved = value;

    [Postfix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.SameNamedRefArgument))]
    public static void InnerPostfix_InnerArgument_Primitive_ReadByReference_WhenOuterHasSameName(ref int value) =>
        InnerObserved = value;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.IntIdentity))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.IntIdentity))]
    public static void InnerPrefix_InnerArgument_Primitive_ReadByReference(ref int value) => InnerObserved = value;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StringIdentity))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StringIdentity))]
    public static void InnerPrefix_InnerArgument_ReferenceType_ReadByReference(ref string value) =>
        ReferenceObserved = value;

    [Prefix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.StructIdentity))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.StructIdentity))]
    public static void InnerPrefix_InnerArgument_Struct_ReadByReference(ref BindingStruct value) => StructObserved = value;

    [Postfix] [Inner(typeof(InnerStaticMethodTargets), nameof(InnerStaticMethodTargets.RefIntArgument))]
    [Target(typeof(OuterStaticMethodTargets), nameof(OuterStaticMethodTargets.RefIntArgument))]
    public static void InnerPostfix_InnerArgument_Primitive_ReadByReference(ref int value) => InnerObserved = value;
}

[TestFixture]
public sealed partial class ArgumentBindingTests
{
    private static void AssertOuterReferenceBindingRejected(string patchName)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => ApplyPatch(typeof(ArgumentBindingPatches), patchName));
        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
    }

    [Test]
    public void InnerPrefix_OuterArgument_Primitive_ReadByReference_Rejected() =>
        AssertOuterReferenceBindingRejected(nameof(ArgumentBindingPatches.InnerPrefix_OuterArgument_Primitive_ReadByReference_Rejected));

    [Test]
    public void InnerPrefix_OuterArgument_ReferenceType_ReadByReference_Rejected() =>
        AssertOuterReferenceBindingRejected(nameof(ArgumentBindingPatches
            .InnerPrefix_OuterArgument_ReferenceType_ReadByReference_Rejected));

    [Test]
    public void InnerPrefix_OuterArgument_Struct_ReadByReference_Rejected() =>
        AssertOuterReferenceBindingRejected(nameof(ArgumentBindingPatches.InnerPrefix_OuterArgument_Struct_ReadByReference_Rejected));

    [Test]
    public void InnerPostfix_OuterArgument_Primitive_ReadByReference_Rejected() =>
        AssertOuterReferenceBindingRejected(nameof(ArgumentBindingPatches.InnerPostfix_OuterArgument_Primitive_ReadByReference_Rejected));

    [Test]
    public void InnerPrefix_InnerArgument_Primitive_ReadByReference_WhenOuterHasSameName()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_Primitive_ReadByReference_WhenOuterHasSameName));
        int outer = 7;
        OuterStaticMethodTargets.SameNamedRefArgument(ref outer);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefix_InnerArgument_ReferenceType_ReadByReference_WhenOuterHasSameName()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_ReferenceType_ReadByReference_WhenOuterHasSameName));
        string outer = "outer";
        OuterStaticMethodTargets.SameNamedRefReferenceTypeArgument(ref outer);
        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("inner"));
    }

    [Test]
    public void InnerPrefix_InnerArgument_Struct_ReadByReference_WhenOuterHasSameName()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_Struct_ReadByReference_WhenOuterHasSameName));
        var outer = new BindingStruct { Value = 7 };
        OuterStaticMethodTargets.SameNamedRefStructArgument(ref outer);
        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(1));
    }

    [Test]
    public void InnerPostfix_InnerArgument_Primitive_ReadByReference_WhenOuterHasSameName()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPostfix_InnerArgument_Primitive_ReadByReference_WhenOuterHasSameName));
        int outer = 7;
        OuterStaticMethodTargets.SameNamedRefArgument(ref outer);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefix_InnerArgument_Primitive_ReadByReference()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_Primitive_ReadByReference));
        OuterStaticMethodTargets.IntIdentity(42);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_InnerArgument_ReferenceType_ReadByReference()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_ReferenceType_ReadByReference));
        OuterStaticMethodTargets.StringIdentity("original");
        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void InnerPrefix_InnerArgument_Struct_ReadByReference()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_Struct_ReadByReference));
        OuterStaticMethodTargets.StructIdentity(new BindingStruct { Value = 42 });
        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_InnerArgument_Primitive_ReadByReference()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPostfix_InnerArgument_Primitive_ReadByReference));
        int value = 42;
        OuterStaticMethodTargets.RefIntArgument(ref value);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }
}

[TestFixture]
public sealed partial class ArgumentBindingTests
{
    [Test]
    public void Prefix_RefArgument_Primitive_ReadByValue()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_RefArgument_Primitive_ReadByValue));
        int value = 42;

        StaticMethodTargets.RefIntArgument(ref value);

        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_RefArgument_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_RefArgument_Primitive_WriteByReference));
        int value = 1;

        StaticMethodTargets.RefIntArgument(ref value);

        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_RefArgument_ReferenceType_ReadByValue()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_RefArgument_ReferenceType_ReadByValue));
        string value = "original";

        StaticMethodTargets.RefStringArgument(ref value);

        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Prefix_RefArgument_ReferenceType_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_RefArgument_ReferenceType_WriteByReference));
        string value = "original";

        StaticMethodTargets.RefStringArgument(ref value);

        Assert.That(value, Is.EqualTo("patched"));
    }

    [Test]
    public void Prefix_RefArgument_Struct_ReadByValue()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_RefArgument_Struct_ReadByValue));
        var value = new BindingStruct { Value = 42 };

        StaticMethodTargets.RefStructArgument(ref value);

        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_RefArgument_Struct_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_RefArgument_Struct_WriteByReference));
        var value = new BindingStruct { Value = 1 };

        StaticMethodTargets.RefStructArgument(ref value);

        Assert.That(value.Value, Is.EqualTo(42));
    }
}

[TestFixture]
public sealed partial class ArgumentBindingTests : PatchTestBase
{
    [Test]
    public void Prefix_Argument_Primitive_ReadByValue()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_Argument_Primitive_ReadByValue));
        StaticMethodTargets.IntArgument(42);

        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_Argument_Primitive_ReadByValue()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Postfix_Argument_Primitive_ReadByValue));
        StaticMethodTargets.IntArgument(42);

        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_Argument_ReferenceType_ReadByValue()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_Argument_ReferenceType_ReadByValue));
        StaticMethodTargets.StringArgument("original");

        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Prefix_Argument_Struct_ReadByValue()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_Argument_Struct_ReadByValue));

        StaticMethodTargets.StructArgument(new BindingStruct { Value = 42 });

        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_Argument_ReferenceType_ReadByValue()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Postfix_Argument_ReferenceType_ReadByValue));
        StaticMethodTargets.StringArgument("original");

        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Prefix_Argument_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_Argument_Primitive_WriteByReference));
        Assert.That(StaticMethodTargets.IntIdentity(1), Is.EqualTo(42));
    }

    [Test]
    public void Postfix_Argument_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Postfix_Argument_Primitive_WriteByReference));
        int value = 1;
        StaticMethodTargets.RefIntArgument(ref value);
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_Argument_ReferenceType_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_Argument_ReferenceType_WriteByReference));
        Assert.That(StaticMethodTargets.StringIdentity("original"), Is.EqualTo("patched"));
    }

    [Test]
    public void Prefix_Argument_Struct_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Prefix_Argument_Struct_WriteByReference));

        BindingStruct result = StaticMethodTargets.StructIdentity(new BindingStruct { Value = 1 });

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void Postfix_Argument_ReferenceType_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.Postfix_Argument_ReferenceType_WriteByReference));
        string value = "original";
        StaticMethodTargets.RefStringArgument(ref value);
        Assert.That(value, Is.EqualTo("patched"));
    }
}

[TestFixture]
public sealed partial class ArgumentBindingTests
{
    [Test]
    public void InnerPrefix_OuterArgument_Primitive_ReadByValue_WhenInnerHasNoMatch()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPrefix_OuterArgument_Primitive_ReadByValue_WhenInnerHasNoMatch));
        OuterStaticMethodTargets.OuterArgument(42);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_OuterArgument_ReferenceType_ReadByValue_WhenInnerHasNoMatch()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPrefix_OuterArgument_ReferenceType_ReadByValue_WhenInnerHasNoMatch));

        OuterStaticMethodTargets.OuterReferenceTypeArgument("original");

        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void InnerPrefix_OuterArgument_Struct_ReadByValue_WhenInnerHasNoMatch()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPrefix_OuterArgument_Struct_ReadByValue_WhenInnerHasNoMatch));

        OuterStaticMethodTargets.OuterStructArgument(new BindingStruct { Value = 42 });

        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_OuterArgument_Primitive_ReadByValue_WhenInnerHasNoMatch()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPostfix_OuterArgument_Primitive_ReadByValue_WhenInnerHasNoMatch));
        OuterStaticMethodTargets.OuterArgument(42);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_OuterArgument_Primitive_WriteByReference_Rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(typeof(ArgumentBindingPatches),
                nameof(ArgumentBindingPatches.InnerPrefix_OuterArgument_Primitive_WriteByReference_Rejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
    }

    [Test]
    public void InnerPrefix_OuterArgument_ReferenceType_WriteByReference_Rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(typeof(ArgumentBindingPatches),
                nameof(ArgumentBindingPatches.InnerPrefix_OuterArgument_ReferenceType_WriteByReference_Rejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
    }

    [Test]
    public void InnerPrefix_OuterArgument_Struct_WriteByReference_Rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(typeof(ArgumentBindingPatches),
                nameof(ArgumentBindingPatches.InnerPrefix_OuterArgument_Struct_WriteByReference_Rejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
    }

    [Test]
    public void InnerPostfix_OuterArgument_Primitive_WriteByReference_Rejected()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ApplyPatch(typeof(ArgumentBindingPatches),
                nameof(ArgumentBindingPatches.InnerPostfix_OuterArgument_Primitive_WriteByReference_Rejected)));

        Assert.That(exception!.InnerException, Is.TypeOf<ParameterBindingException>());
    }

    [Test]
    public void InnerPrefix_InnerArgument_Primitive_ReadByValue_WhenOuterHasSameName()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_Primitive_ReadByValue_WhenOuterHasSameName));
        OuterStaticMethodTargets.SameNamedArgument(1);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_InnerArgument_ReferenceType_ReadByValue_WhenOuterHasSameName()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_ReferenceType_ReadByValue_WhenOuterHasSameName));

        OuterStaticMethodTargets.SameNamedReferenceTypeArgument("outer");

        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("inner"));
    }

    [Test]
    public void InnerPrefix_InnerArgument_Struct_ReadByValue_WhenOuterHasSameName()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_Struct_ReadByValue_WhenOuterHasSameName));

        OuterStaticMethodTargets.SameNamedStructArgument(new BindingStruct { Value = 1 });

        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_InnerArgument_Primitive_ReadByValue_WhenOuterHasSameName()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPostfix_InnerArgument_Primitive_ReadByValue_WhenOuterHasSameName));
        OuterStaticMethodTargets.SameNamedArgument(1);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_InnerArgument_Primitive_WriteByReference_WhenOuterHasSameName()
    {
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_Primitive_WriteByReference_WhenOuterHasSameName));
        int outerValue = 7;

        int result = OuterStaticMethodTargets.SameNamedRefArgument(ref outerValue);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(outerValue, Is.EqualTo(7));
    }

    [Test]
    public void InnerPrefix_InnerArgument_ReferenceType_WriteByReference_WhenOuterHasSameName()
    {
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_ReferenceType_WriteByReference_WhenOuterHasSameName));
        string outerValue = "outer";

        string result = OuterStaticMethodTargets.SameNamedRefReferenceTypeArgument(ref outerValue);

        Assert.That(result, Is.EqualTo("patched"));
        Assert.That(outerValue, Is.EqualTo("outer"));
    }

    [Test]
    public void InnerPrefix_InnerArgument_Struct_WriteByReference_WhenOuterHasSameName()
    {
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_Struct_WriteByReference_WhenOuterHasSameName));
        var outerValue = new BindingStruct { Value = 7 };

        BindingStruct result = OuterStaticMethodTargets.SameNamedRefStructArgument(ref outerValue);

        Assert.That(result.Value, Is.EqualTo(42));
        Assert.That(outerValue.Value, Is.EqualTo(7));
    }

    [Test]
    public void InnerPostfix_InnerArgument_Primitive_WriteByReference_WhenOuterHasSameName()
    {
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPostfix_InnerArgument_Primitive_WriteByReference_WhenOuterHasSameName));
        int outerValue = 7;

        int result = OuterStaticMethodTargets.SameNamedRefArgument(ref outerValue);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(outerValue, Is.EqualTo(7));
    }

    [Test]
    public void InnerPrefix_InnerArgument_Primitive_ReadByValue()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_Primitive_ReadByValue));
        OuterStaticMethodTargets.IntArgument(42);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_InnerArgument_ReferenceType_ReadByValue()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_ReferenceType_ReadByValue));

        OuterStaticMethodTargets.StringArgument("original");

        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void InnerPrefix_InnerArgument_Struct_ReadByValue()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_Struct_ReadByValue));

        OuterStaticMethodTargets.StructArgument(new BindingStruct { Value = 42 });

        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_InnerArgument_Primitive_ReadByValue()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPostfix_InnerArgument_Primitive_ReadByValue));
        OuterStaticMethodTargets.IntArgument(42);
        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_InnerArgument_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_Primitive_WriteByReference));
        Assert.That(OuterStaticMethodTargets.IntIdentity(1), Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_InnerArgument_ReferenceType_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_ReferenceType_WriteByReference));

        string result = OuterStaticMethodTargets.StringIdentity("original");

        Assert.That(result, Is.EqualTo("patched"));
    }

    [Test]
    public void InnerPrefix_InnerArgument_Struct_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPrefix_InnerArgument_Struct_WriteByReference));

        BindingStruct result = OuterStaticMethodTargets.StructIdentity(new BindingStruct { Value = 1 });

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPostfix_InnerArgument_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches), nameof(ArgumentBindingPatches.InnerPostfix_InnerArgument_Primitive_WriteByReference));
        int value = 1;
        OuterStaticMethodTargets.RefIntArgument(ref value);
        Assert.That(value, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_ParameterAttribute_StaticMethod_Index0_Primitive_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.Prefix_ParameterAttribute_StaticMethod_Index0_Primitive_WriteByReference));

        int result = StaticMethodTargets.IntIdentity(1);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_ParameterAttribute_StaticMethod_Index0_Primitive_ReadByValue()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.Prefix_ParameterAttribute_StaticMethod_Index0_Primitive_ReadByValue));

        StaticMethodTargets.IntArgument(42);

        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_ParameterAttribute_AnonymousLambda_Index0_Primitive_ReadByValue()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(
            typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.Prefix_ParameterAttribute_AnonymousLambda_Index0_Primitive_ReadByValue));

        int result = LocalFunctionTargets.InvokeAnonymousLambda(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_ParameterAttribute_InstanceMethod_Index0_Primitive_ReadByValue()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(
            typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.Prefix_ParameterAttribute_InstanceMethod_Index0_Primitive_ReadByValue));

        int result = new ClassMethodTargets().IntIdentity(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_ParameterAttribute_InstanceMethod_Index0_Primitive_ReadByReference()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(
            typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.Prefix_ParameterAttribute_InstanceMethod_Index0_Primitive_ReadByReference));

        int result = new ClassMethodTargets().IntIdentity(42);

        Assert.That(result, Is.EqualTo(42));
        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_ParameterAttribute_InstanceMethod_Index0_Primitive_WriteByReference()
    {
        ApplyPatch(
            typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.Prefix_ParameterAttribute_InstanceMethod_Index0_Primitive_WriteByReference));

        int result = new ClassMethodTargets().IntIdentity(1);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_ParameterAttribute_InstanceMethod_Index1_Primitive_ReadByValue()
    {
        ArgumentBindingPatches.ValueObserved = 0;
        ApplyPatch(
            typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.Prefix_ParameterAttribute_InstanceMethod_Index1_Primitive_ReadByValue));

        int result = new ClassMethodTargets().IntSum(1, 42);

        Assert.That(result, Is.EqualTo(43));
        Assert.That(ArgumentBindingPatches.ValueObserved, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_ParameterAttribute_StaticMethod_Index0_ReferenceType_ReadByValue()
    {
        ArgumentBindingPatches.ReferenceObserved = null;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.Prefix_ParameterAttribute_StaticMethod_Index0_ReferenceType_ReadByValue));

        StaticMethodTargets.StringArgument("original");

        Assert.That(ArgumentBindingPatches.ReferenceObserved, Is.EqualTo("original"));
    }

    [Test]
    public void Prefix_ParameterAttribute_StaticMethod_Index0_ReferenceType_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.Prefix_ParameterAttribute_StaticMethod_Index0_ReferenceType_WriteByReference));

        string result = StaticMethodTargets.StringIdentity("original");

        Assert.That(result, Is.EqualTo("patched"));
    }

    [Test]
    public void Prefix_ParameterAttribute_StaticMethod_Index0_Struct_ReadByValue()
    {
        ArgumentBindingPatches.StructObserved = default;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.Prefix_ParameterAttribute_StaticMethod_Index0_Struct_ReadByValue));

        StaticMethodTargets.StructArgument(new BindingStruct { Value = 42 });

        Assert.That(ArgumentBindingPatches.StructObserved.Value, Is.EqualTo(42));
    }

    [Test]
    public void Prefix_ParameterAttribute_StaticMethod_Index0_Struct_WriteByReference()
    {
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.Prefix_ParameterAttribute_StaticMethod_Index0_Struct_WriteByReference));

        BindingStruct result = StaticMethodTargets.StructIdentity(new BindingStruct { Value = 1 });

        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void InnerPrefix_ParameterAttribute_OuterScope_Primitive_ReadByValue()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPrefix_ParameterAttribute_OuterScope_Primitive_ReadByValue));

        OuterStaticMethodTargets.SameNamedArgument(1);

        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(1));
    }

    [Test]
    public void InnerPrefix_ParameterAttribute_InnerScope_Primitive_ReadByValue()
    {
        ArgumentBindingPatches.InnerObserved = 0;
        ApplyPatch(typeof(ArgumentBindingPatches),
            nameof(ArgumentBindingPatches.InnerPrefix_ParameterAttribute_InnerScope_Primitive_ReadByValue));

        OuterStaticMethodTargets.SameNamedArgument(1);

        Assert.That(ArgumentBindingPatches.InnerObserved, Is.EqualTo(42));
    }
}
