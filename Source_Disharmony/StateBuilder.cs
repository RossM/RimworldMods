namespace Disharmony;

internal class StateBuilder(RuleBuilderContext context) : RuleBuilder(context, EmptyInvocation.Instance)
{
    private readonly Dictionary<string, LocalTrackerBuilder> stateMap = new();

    private LocalTrackerBuilder GetOrAddStateLocal(string stateKey, Type localType, Invocation method)
    {
        localType = localType.NoRefType;

        if (stateMap.TryGetValue(stateKey, out var local))
            return local;

        local = output.AddLocal(localType);
        stateMap.Add(stateKey, local);
        return local;
    }

    public override IEnumerable<Rule> BuildRules()
    {
        if (stateMap.Count == 0)
            yield break;

        foreach (var local in stateMap.Values)
            output.EmitLocalInitializer(local);

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
                    parameter.local = GetOrAddStateLocal(parameter.stateKey, parameter.parameter.ParameterType, patch.patch);
                }
            }
        }
    }
}
