namespace Disharmony;

public static partial class Autopatcher
{
    private class PatchWorker(PatchRegistry registry)
    {
        private PatchRegistry PatchRegistry { get; } = registry;

        public MethodInfo CreatePatchTranspiler(MethodInfo outer)
        {
            var patches = PatchRegistry.PatchesByMethod[outer];

            MethodInfo? iteratorMethod = outer.GetIteratorImplementation();

            if (iteratorMethod is not null)
            {
                if (patches.Any(p => p.HasBindingType(BindingType.State)))
                    throw new NotSupportedException("__state is not supported for compiler-generated iterator methods");
                if (patches.Any(p => p.HasBindingType(BindingType.Result)))
                    throw new NotSupportedException("__result is not supported for compiler-generated iterator methods");

                throw new NotImplementedException();
            }

            List<InstructionMatcher.Rule> rules = [];

            StateBuilder<Type> stateBuilder = new();

            foreach (var patch in patches)
            {
                ParameterBinding[] parameters = patch.parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].BindingType == BindingType.State)
                        parameters[i].Index = stateBuilder.GetOrAddStateLocal(patch.patchMethod.DeclaringType, parameters[i].Parameter.ParameterType, patch.patchMethod);
                }
            }

            if (stateBuilder.LocalTypes.Count > 0)
                rules.Add(stateBuilder.BuildRule());

            foreach (IGrouping<MemberInfo, PatchInfo> targetGroup in patches.GroupBy(patch => patch.inner))
            {
                var inner = targetGroup.Key;

                var ruleBuilder = new InfixRuleBuilder(outer, inner, targetGroup.ToList(), stateBuilder.LocalTypes);

                rules.Add(ruleBuilder.BuildRule());
            }

            var patchMatcher = new InstructionMatcher
            {
                Rules = rules,
                LocalTypes = stateBuilder.LocalTypes,
            };

            int version = IncrementVersion(outer);
            MethodInfo transpiler = MakeTranspiler([patchMatcher],
                $"{outer.DeclaringType?.FullName?.Replace('.', '_')}_{outer.Name}_Transpiler{version}");
            return transpiler;
        }

        public bool ShouldDebug(MethodInfo targetMethod) => PatchRegistry.PatchesByMethod[targetMethod].Any(p => p.debug);
    }
}
