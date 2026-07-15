namespace Disharmony;

public static partial class Autopatcher
{
    private class PatchWorker(Assembly assembly)
    {
        public IEnumerable<MethodInfo> PatchedMethods => PatchesByMethod.Keys;
        private Dictionary<MethodInfo, List<PatchInfo>> PatchesByMethod = new();
        private Assembly Assembly { get; } = assembly;
        private Dictionary<MethodInfo, StateBuilder<Type>> StateBuilders { get; } = new();
        private List<PatchInfo> Patches { get; } = [];

        public void CollectPatches()
        {
            foreach (TypeInfo type in Assembly.DefinedTypes)
            {
                var harmonyAttribute = (HarmonyPatch?)Attribute.GetCustomAttribute(type, typeof(HarmonyPatch));
                if (harmonyAttribute == null)
                    continue;

                foreach (MethodInfo method in type.DeclaredMethods)
                {
                    try
                    {
                        var infixTargetAttribute
                            = (PatchTypeAttribute?)Attribute.GetCustomAttribute(method, typeof(InnerPrefixAttribute)) ??
                              (PatchTypeAttribute?)Attribute.GetCustomAttribute(method, typeof(InnerPostfixAttribute));
                        var infixPatchAttributes = Attribute.GetCustomAttributes(method, typeof(TargetAttribute))
                            .Cast<TargetAttribute>().ToArray();
                        bool debug = Attribute.GetCustomAttribute(method, typeof(DebugAttribute)) != null;

                        if (infixTargetAttribute == null)
                            continue;

                        MemberInfo? inner = GetMember(infixTargetAttribute.type, infixTargetAttribute.memberName,
                            infixTargetAttribute.parameterTypes, infixTargetAttribute.genericTypes);
                        if (inner == null)
                            throw new InvalidOperationException("null wrapped member");

                        foreach (var infixPatchAttribute in infixPatchAttributes)
                        {
                            var patchedType = infixPatchAttribute.type ?? harmonyAttribute.info.declaringType;

                            MethodInfo? outer = (MethodInfo?)GetMember(patchedType, infixPatchAttribute.methodName,
                                infixPatchAttribute.parameterTypes, infixPatchAttribute.genericTypes);
                            if (outer == null)
                                throw new InvalidOperationException("null target method");

                            if (!StateBuilders.TryGetValue(outer, out StateBuilder<Type> stateBuilder))
                                stateBuilder = StateBuilders[outer] = new();

                            var arguments = method.GetParameters().Select(param => BindParameter(param, outer, inner, stateBuilder))
                                .ToArray();

                            Patches.Add(new()
                            {
                                outer = outer,
                                inner = inner,
                                patchMethod = method,
                                patchType = infixTargetAttribute.patchType,
                                parameters = arguments,
                                debug = debug,
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"Error processing {type}:{method}", e);
                    }
                }
            }

            PatchesByMethod = Patches.GroupBy(patch => patch.outer).ToDictionary(g => g.Key, g => g.ToList());
        }

        public MethodInfo CreatePatchTranspiler(MethodInfo outer)
        {
            var patches = PatchesByMethod[outer];

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

            if (!StateBuilders.TryGetValue(outer, out var stateBuilder))
                stateBuilder = new();

            if (stateBuilder.LocalTypes.Count > 0)
                rules.Add(stateBuilder.BuildRule());

            foreach (IGrouping<MemberInfo, PatchInfo> targetGroup in patches.GroupBy(patch => patch.inner))
            {
                var inner = targetGroup.Key;
                var prefixes = targetGroup.Where(patch => patch.patchType == PatchType.InnerPrefix).ToList();
                var postfixes = targetGroup.Where(patch => patch.patchType == PatchType.InnerPostfix).ToList();

                rules.Add(new()
                {
                    LateGenerator = (_, _, generator) =>
                        RedirectRule_Core(generator, outer, inner, null, prefixes, postfixes, stateBuilder.LocalTypes),
                });
            }

            var patchMatcher = new InstructionMatcher
            {
                Rules = rules,
                LocalTypes = stateBuilder.LocalTypes,
            };

            int version = IncrementVersion(outer);
            MethodInfo transpiler = MakeTranspiler([patchMatcher],
                $"{outer.DeclaringType?.FullName?.Replace('.', '_')}_{outer.Name}_Transpiler{version}", false);
            return transpiler;
        }

        public bool ShouldDebug(MethodInfo targetMethod) => PatchesByMethod[targetMethod].Any(p => p.debug);
    }
}
