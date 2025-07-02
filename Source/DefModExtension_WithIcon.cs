using UnityEngine;
using Verse;

namespace XylRacesCore;

public class DefModExtension_WithIcon : DefModExtension
{
    [NoTranslate]
    public string iconPath = "Things/Item/Resource/Milk";

    [Unsaved(false)] 
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