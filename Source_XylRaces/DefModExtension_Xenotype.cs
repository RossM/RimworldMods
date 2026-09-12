namespace XylXenos;

[UsedFromXml]
public class DefModExtension_Xenotype : DefModExtension
{
    /// <summary>
    ///     Whether to allow custom backstories for this xenotype.
    /// </summary>
    public bool allowSolidBackstories = true;

    /// <summary>
    ///     If true, children where both parents are this xenotype will inherit all of their endogenes from one parent.
    ///     This is used to keep scaleborn lineages together.
    /// </summary>
    public bool lineageInheritance = false;

    /// <summary>
    ///     Memes which this xenotype prefers. Affects ideoligeon conversion.
    /// </summary>
    public List<MemeDef>? agreeingMemes;
    
    /// <summary>
    ///     Memes which this xenotype dislikes. Affects ideoligeon conversion.
    /// </summary>
    public List<MemeDef>? disagreeingMemes;
}
