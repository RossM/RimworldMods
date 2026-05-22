using System.Collections.Generic;
using System.Xml;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace XylXenos.Genes
{
    [UsedImplicitly]
    public class GeneDefExtension_JoyGivers : GeneDefExtension
    {
        public List<JoyGiverFactor> joyGiverChanceFactors;
    }

    public class JoyGiverFactor
    {
        public JoyGiverDef joyGiver;
        public float factor = 1.0f;

        [UsedImplicitly]
        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "joyGiver", xmlRoot.Name);
            factor = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
        }
    }
}
