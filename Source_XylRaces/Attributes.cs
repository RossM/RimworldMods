using System;
using JetBrains.Annotations;
// ReSharper disable UnusedParameter.Local

namespace XylXenos
{
    // This attribute serves as documentation of which patches are to support which parts of the mod. It
    // has no actual effect.

    public class FeatureAttribute : Attribute
    {
#pragma warning disable CS9113 // Parameter is unread.
        public FeatureAttribute(params string[] featureNames)
        {
        }

        public FeatureAttribute(Config.Feature feature)
        {
        }

        public FeatureAttribute(Type feature)
        {
        }
#pragma warning restore CS9113 // Parameter is unread.
    }

    [MeansImplicitUse]
    public class UsedFromXmlAttribute : Attribute;
}
