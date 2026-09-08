namespace Disharmony.Tests;

public sealed class RefReturnTargets
{
    public int Primitive;
    public BindingReference Reference = new();
    public BindingStruct Structure;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ref int PrimitiveReference() => ref Primitive;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ref BindingReference ObjectReference() => ref Reference;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ref BindingStruct StructReference() => ref Structure;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public ref readonly int ReadonlyReference() => ref Primitive;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref int ArgumentReference(ref int value) => ref value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref int ArrayElement(int[] values, int index) => ref values[index];

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref int CallArrayElement(int[] values, int index) => ref ArrayElement(values, index);
}
