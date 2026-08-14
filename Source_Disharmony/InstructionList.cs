using System.Collections;

namespace Disharmony;

internal class InstructionList(ILGenerator generator) : IEnumerable<CodeInstruction>
{
    public readonly List<CodeInstruction> instructions = [];
    public List<LocalTrackerBuilder> locals = [];
    public readonly ILGenerator generator = generator;

    public IEnumerator<CodeInstruction> GetEnumerator() => instructions.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(CodeInstruction instruction) => instructions.Add(instruction);

    // ReSharper disable once ParameterHidesMember
    public void AddRange(IEnumerable<CodeInstruction> instructions) => this.instructions.AddRange(instructions);

    public void EmitLocalInitializer(LocalTrackerBuilder localIndex)
    {
        Type type = localIndex.Type;

        if (type.IsByRef)
            throw new NotImplementedException($"IsByRef targetType {type}");

        if (type.IsClass)
        {
            Add(new(OpCodes.Ldnull));
            Add(localIndex.Store());
        }
        else if (type.IsPrimitive || type.IsEnum)
        {
            var underlyingType = type.IsEnum ? type.GetEnumUnderlyingType() : type;

            if (underlyingType == typeof(float))
                Add(new(OpCodes.Ldc_R4, (float)0));
            else if (underlyingType == typeof(double))
                Add(new(OpCodes.Ldc_R8, (double)0));
            else if (underlyingType == typeof(long) || underlyingType == typeof(ulong))
                Add(new(OpCodes.Ldc_I8, (long)0));
            else
                Add(new(OpCodes.Ldc_I4_0));

            Add(localIndex.Store());
        }
        else if (type.IsValueType)
        {
            Add(localIndex.Load(true));
            Add(new(OpCodes.Initobj, type));
        }
        else
        {
            throw new NotImplementedException($"targetType {type}");
        }
    }

    public LocalTrackerBuilder AddLocal(Type type)
    {
        var local = new LocalTrackerBuilder(generator.DeclareLocal(type));
        locals.Add(local);
        return local;
    }
}
