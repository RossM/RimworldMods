using JetBrains.Annotations;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace XylRacesCore.Genes
{
    [UsedImplicitly]
    public class GeneDefExtension_JoyGivers : DefModExtension
    {
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

        public List<JoyGiverFactor> joyGiverChanceFactors;
    }
}
