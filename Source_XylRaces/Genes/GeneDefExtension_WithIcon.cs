using UnityEngine;
using Verse;

namespace XylRacesCore.Genes;

public class GeneDefExtension_WithIcon : GeneDefExtension
{
    [NoTranslate]
    public string iconPath;

    [Unsaved] 
    private Texture2D cachedIcon;

    public Texture2D Icon
    {
        get
        {
            cachedIcon ??= iconPath.NullOrEmpty()
                ? BaseContent.BadTex
                : ContentFinder<Texture2D>.Get(iconPath) ?? BaseContent.BadTex;
            return cachedIcon;
        }
    }
}