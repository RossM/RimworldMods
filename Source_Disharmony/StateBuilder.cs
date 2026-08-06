namespace Disharmony;

internal class StateBuilder(RuleBuilderContext context) : RuleBuilder(context, EmptyInvocation.Instance)
{
    private List<Type> LocalTypes => output.localTypes;
    private readonly Dictionary<string, (int index, Type type)> stateMap = new();

    private int GetOrAddStateLocal(string stateKey, Type localType, Invocation method)
    {
        localType = localType.NoRefType;

        if (stateMap.TryGetValue(stateKey, out var tuple))
        {
            (int index, Type existingType) = tuple;

            if (existingType == localType)
                return index;

            throw new ArgumentException(
                $"{method.FullName} declares __state of type {localType} which conflicts with existing type {existingType}");
        }

        int newIndex = LocalTypes.Count;
        stateMap.Add(stateKey, (newIndex, localType));
        LocalTypes.Add(localType);
        return newIndex;
    }

    public override IEnumerable<Rule> BuildRules()
    {
        if (LocalTypes.Count == 0)
            yield break;

        for (int index = 0; index < LocalTypes.Count; index++)
            output.EmitLocalInitializer(index);

        yield return new Rule
        {
            priority = 100,
            mode = OutputMode.MethodPrefix,
            output = [.. output.instructions],
            name = "state variable initialization",
        };
    }

    public void AssignStateVariableIndexes(List<PatchInfo> patches)
    {
        foreach (var patch in patches)
        {
            ParameterBinding[] parameters = patch.parameters;
            foreach (ParameterBinding parameter in parameters)
            {
                if (parameter.bindingType == BindingType.State)
                {
                    if (parameter.stateKey is null)
                        throw new InvalidOperationException("Null StateKey");
                    parameter.index = GetOrAddStateLocal(parameter.stateKey, parameter.parameter.ParameterType, patch.patch);
                }
            }
        }
    }
}
