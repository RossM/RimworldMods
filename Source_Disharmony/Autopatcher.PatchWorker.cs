using JetBrains.Annotations;

namespace Disharmony;

public static partial class Autopatcher
{
    private class PatchWorker(PatchRegistry registry)
    {
        private static readonly object trampolineLock = new();

        // Only access while holding trampolineLock
        private static readonly Dictionary<MethodBase, MethodInfo> trampolines = new();
        private static readonly Dictionary<MethodBase, IntPtr> replacementTargets = new();
        private static int trampolineCount;

        private PatchRegistry PatchRegistry { get; } = registry;

        public HarmonyMethod? GetHarmonyMethod(MethodInfo outer)
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

            List<InstructionMatcher> matchers = [];
            List<InstructionMatcher.Rule> rules = [];

            StateBuilder<Type> stateBuilder = new();

            foreach (var patch in patches)
            {
                ParameterBinding[] parameters = patch.parameters;
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].BindingType == BindingType.State)
                        parameters[i].Index = stateBuilder.GetOrAddStateLocal(patch.patchMethod.DeclaringType,
                            parameters[i].Parameter.ParameterType, patch.patchMethod);
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
                CrossRuleLocalTypes = stateBuilder.LocalTypes,
            };
            matchers.Add(patchMatcher);

            rules = [];

            foreach (var patch in patches.Where(p => p.inline))
            {
                var ruleBuilder = new InlineRuleBuilder(patch);

                var rule = ruleBuilder.BuildRule();
                if (rule != null)
                    rules.Add(rule);
            }

            if (rules.Count > 0)
            {
                var inlineMatcher = new InstructionMatcher
                {
                    Rules = rules,
                };
                matchers.Add(inlineMatcher);
            }

            InstructionMatcher[] matchersArray = matchers.ToArray();
            if (TryUpdateTranspiler(outer, matchersArray))
            {
                FileLog.Log($"# GetHarmonyMethod: Reusing transpiler for {outer.FullName}");

                // Using null as our HarmonyMethod will cause Harmony to simply rerun the patch, including
                // the updated transpiler.
                return null;
            }

            MethodInfo transpiler = MakeTranspiler(matchersArray,
                $"{outer.DeclaringType?.FullName?.Replace('.', '_')}_{outer.Name}_Transpiler", outer);

            bool debug = PatchRegistry.PatchesByMethod[outer].Any(p => p.debug);

            HarmonyMethod harmonyMethod = new(transpiler, priority: Priority.LowerThanNormal) { debug = debug };
            return harmonyMethod;
        }

        [UsedImplicitly]
        public static void ResolveTrampoline(MethodBase method)
        {
            FileLog.Log($"UpdateMethod {method.FullName}");

            lock (trampolineLock)
            {
                // If we can't remove the method, we lost a race and some other thread has
                // already replaced the trampoline
                if (!trampolines.Remove(method))
                    return;

                FileLog.Log($"!!! Resolving trampoline to {method.FullName}");

                harmony.Patch(method);
            }
        }

        public static MethodInfo ApplyTrampoline(MethodInfo method)
        {
            FileLog.Log($"ApplyTrampoline {method.FullName}");

            lock (trampolineLock)
            {
                if (trampolines.TryGetValue(method, out var existingTrampoline))
                    return existingTrampoline;

                FileLog.Log($"!!! Applying trampoline to {method.FullName}");

                string trampolineName = $"_Trampoline{trampolineCount}";
                trampolineCount++;
                var trampoline = MakeTrampoline(method, trampolineName);

                harmonyDetourMethod(method, trampoline);
                FileLog.Log($"Method: {method}");
                FileLog.Log($"Trampoline: {trampoline}");

                trampolines[method] = trampoline;

                return trampoline;
            }
        }
    }
}
