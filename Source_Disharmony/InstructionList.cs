using System.Collections;

namespace Disharmony;

internal class InstructionList : IEnumerable<CodeInstruction>
{
    public readonly List<CodeInstruction> Instructions = [];
    public readonly List<Type> LocalTypes = [];

    public IEnumerator<CodeInstruction> GetEnumerator() => Instructions.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(CodeInstruction instruction) => Instructions.Add(instruction);

    public void EmitLocalInitializer(int localIndex)
    {
        Type type = LocalTypes[localIndex];

        if (type.IsByRef)
            throw new NotImplementedException($"IsByRef targetType {type}");

        if (type.IsClass)
        {
            Add(new(OpCodes.Ldnull));
            Add(CodeInstruction.StoreLocal(localIndex));
        }
        else if (type.IsStruct())
        {
            Add(CodeInstruction.LoadLocalAddress(localIndex));
            Add(new(OpCodes.Initobj, type));
        }
        else if (type.IsValueType)
        {
            if (type == typeof(float))
                Add(new(OpCodes.Ldc_R4, (float)0));
            else if (type == typeof(double))
                Add(new(OpCodes.Ldc_R8, (double)0));
            else if (type == typeof(long) || type == typeof(ulong))
                Add(new(OpCodes.Ldc_I8, (long)0));
            else
                Add(new(OpCodes.Ldc_I4_0));

            Add(CodeInstruction.StoreLocal(localIndex));
        }
        else
            throw new NotImplementedException($"targetType {type}");
    }

    public void EmitLoad(Type parameterType, int localIndex)
    {
        if (parameterType.IsByRef)
        {
            Add(CodeInstruction.LoadLocalAddress(localIndex));
        }
        else
            Add(CodeInstruction.LoadLocal(localIndex));
    }

    public int AddLocal(Type type)
    {
        var localIndex = LocalTypes.Count;
        LocalTypes.Add(type);
        return localIndex;
    }
}
