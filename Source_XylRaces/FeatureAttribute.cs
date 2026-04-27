using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XylRacesCore
{
    // This attribute serves as documentation of which patches are to support which parts of the mod. It
    // has no actual effect.
    public class FeatureAttribute(params string[] featureNames) : Attribute
    {
    }
}
