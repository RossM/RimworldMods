using System.Xml;

namespace XylXenos;

public class JoyGiverFactor
{
    public JoyGiverDef joyGiver;
    public float factor = 1.0f;

    [UsedFromReflection]
    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "joyGiver", xmlRoot.Name);
        factor = ParseHelper.FromString<float>(xmlRoot.FirstChild.Value);
    }
}

[UsedFromXml]
public class GeneCompProperties_JoyGiverChances : GeneCompProperties
{
    public List<JoyGiverFactor> factors;
}
