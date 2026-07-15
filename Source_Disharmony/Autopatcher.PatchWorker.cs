namespace Disharmony;

public static partial class Autopatcher
{
    private class PatchWorker(Assembly assembly)
    {
        public IEnumerable<MethodInfo> TargetMethods => PatchesByMethod.Keys;
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

                        MemberInfo? target = GetMember(infixTargetAttribute.type, infixTargetAttribute.memberName,
                            infixTargetAttribute.parameterTypes, infixTargetAttribute.genericTypes);
                        if (target == null)
                            throw new InvalidOperationException("null wrapped member");

                        foreach (var infixPatchAttribute in infixPatchAttributes)
                        {
                            var patchedType = infixPatchAttribute.type ?? harmonyAttribute.info.declaringType;

                            MethodInfo? caller = (MethodInfo?)GetMember(patchedType, infixPatchAttribute.methodName,
                                infixPatchAttribute.parameterTypes, infixPatchAttribute.genericTypes);
                            if (caller == null)
                                throw new InvalidOperationException("null target method");

                            if (!StateBuilders.TryGetValue(caller, out StateBuilder<Type> stateBuilder))
                                stateBuilder = StateBuilders[caller] = new();

                            var arguments = method.GetParameters().Select(param => BindParameter(param, caller, target, stateBuilder))
                                .ToArray();

                            Patches.Add(new()
                            {
                                caller = caller,
                                target = target,
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

            PatchesByMethod = Patches.GroupBy(patch => patch.caller).ToDictionary(g => g.Key, g => g.ToList());
        }

        public MethodInfo CreatePatchTranspiler(
            MethodInfo patchedMethod)
        {
            var patches = PatchesByMethod[patchedMethod];

            MethodInfo? iteratorMethod = patchedMethod.GetIteratorImplementation();

            if (iteratorMethod is not null)
            {
                if (patches.Any(p => p.HasBindingType(BindingType.State)))
                    throw new NotSupportedException("__state is not supported for compiler-generated iterator methods");
                if (patches.Any(p => p.HasBindingType(BindingType.Result)))
                    throw new NotSupportedException("__result is not supported for compiler-generated iterator methods");

                throw new NotImplementedException();
            }

            List<InstructionMatcher.Rule> rules = [];

            if (!StateBuilders.TryGetValue(patchedMethod, out var stateBuilder))
                stateBuilder = new();

            if (stateBuilder.LocalTypes.Count > 0)
                rules.Add(stateBuilder.BuildRule());

            foreach (IGrouping<MemberInfo, PatchInfo> targetGroup in patches.GroupBy(patch => patch.target))
            {
                var target = targetGroup.Key;
                var prefixes = targetGroup.Where(patch => patch.patchType == PatchType.InnerPrefix).ToList();
                var postfixes = targetGroup.Where(patch => patch.patchType == PatchType.InnerPostfix).ToList();

                rules.Add(new()
                {
                    LateGenerator = (_, _, generator) =>
                        RedirectRule_Core(generator, patchedMethod, target, null, prefixes, postfixes, stateBuilder.LocalTypes),
                });
            }

            var patchMatcher = new InstructionMatcher
            {
                Rules = rules,
                LocalTypes = stateBuilder.LocalTypes,
            };

            int version = IncrementVersion(patchedMethod);
            MethodInfo transpiler = MakeTranspiler([patchMatcher],
                $"{patchedMethod.DeclaringType?.FullName?.Replace('.', '_')}_{patchedMethod.Name}_Transpiler{version}", false);
            return transpiler;
        }

        public bool ShouldDebug(MethodInfo targetMethod) => PatchesByMethod[targetMethod].Any(p => p.debug);
    }
}
