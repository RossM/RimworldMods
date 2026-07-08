using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace XylXenos;

[UsedFromXml]
public class DefModExtension_Thought_Need : DefModExtension
{
    public required NeedDef need;

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    public override IEnumerable<string> ConfigErrors()
    {
        if (need is null)
            yield return $"{nameof(need)} is null";
    }
}

[UsedFromXml]
public class ThoughtWorker_Need : ThoughtWorker
{
    public DefModExtension_Thought_Need DefExt => def.GetModExtension<DefModExtension_Thought_Need>();

    public Func<Need, int> CurStageGetter => field ??= MakeGetter(DefExt.need.needClass);

    private static readonly Dictionary<Type, Func<Need, int>> getterCache = new();

    private static Func<Need, int> MakeGetter(Type needType)
    {
        if (getterCache.TryGetValue(needType, out var func))
            return func;

        // Creates a method that does:
        //   Need need => (int)((Need_Foo)need).CurCategory
        ParameterExpression need = Expression.Parameter(typeof(Need), "need");
        Expression curCategory = Expression.Property(Expression.Convert(need, needType), "CurCategory");
        Expression result = Expression.Convert(curCategory, typeof(int));
        getterCache[needType] = func = Expression.Lambda<Func<Need, int>>(result, need).Compile();

        return func;
    }

    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (ThoughtUtility.ThoughtNullified(p, def))
            return ThoughtState.Inactive;

        var need = p.needs.TryGetNeed(DefExt.need);
        if (need == null)
            return ThoughtState.Inactive;

        int stage = CurStageGetter(need);

        if (stage < 0 || stage >= def.stages.Count)
            return ThoughtState.Inactive;
        return ThoughtState.ActiveAtStage(stage);
    }
}
