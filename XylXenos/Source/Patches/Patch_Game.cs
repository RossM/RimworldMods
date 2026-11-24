namespace XylXenos.Patches;

[Patch(typeof(Game))]
public static class Patch_Game
{
    [Feature(FeatureNameOf.Compat)]
    [Prefix]
    [Target("ExposeSmallComponents")]
    public static void ExposeSmallComponents_Prefix()
    {
        if (!Scribe.EnterNode(Config.ModId))
        {
            ScribeMetaHeaderUtility.modListChanged = true;
            return;
        }

        try
        {
            string[] parts = ModLister.GetModWithIdentifier(Config.ModId).ModVersion.Split('.', 3);
            if (parts.Length < 2)
            {
                Log.Error("Xylthixlm.Xenotypes: Failed to parse mod version");
                return;
            }

            bool isAlpha = parts.Length >= 3 && parts[2].Contains("alpha", StringComparison.InvariantCultureIgnoreCase);
            string expectedVersion = $"{parts[0]}.{parts[1]}";
            string? actualVersion = expectedVersion;

            Scribe_Values.Look(ref actualVersion, "modVersion");
            if (Scribe.mode == LoadSaveMode.LoadingVars && (actualVersion != expectedVersion || isAlpha))
                ScribeMetaHeaderUtility.modListChanged = true;
        }
        finally
        {
            Scribe.ExitNode();
        }
    }
}
