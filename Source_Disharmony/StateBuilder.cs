namespace Disharmony;

/// <summary>
///     This class manages the state variables used by patches and generates rules to initialize them at the beginning of
///     the method.
/// </summary>
/// <param name="context"></param>
internal class StateBuilder(RuleBuilderContext context) : RuleBuilder(context, EmptyInvocation.Instance)
{
    private readonly Dictionary<string, LocalTrackerBuilder> stateMap = [];

    private LocalTrackerBuilder GetOrAddStateLocal(string stateKey, Type localType)
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

    public void AssignStateVariableIndexes(IReadOnlyList<PatchInfo> patches)
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
                    parameter.local = GetOrAddStateLocal(parameter.stateKey, parameter.parameter.ParameterType);
                }
            }
        }
    }
}
