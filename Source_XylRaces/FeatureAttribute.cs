using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XylRacesCore
{
    // This attribute serves as documentation of which patches are to support which parts of the mod. It
    // has no actual effect.
#pragma warning disable CS9113 // Parameter is unread.
    public class FeatureAttribute(params string[] featureNames) : Attribute
#pragma warning restore CS9113 // Parameter is unread.
    {
    }
}
