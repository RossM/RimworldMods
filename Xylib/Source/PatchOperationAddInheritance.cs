using System.Xml;

namespace Xylib
{
    [UsedFromXml]
    public class PatchOperationAddInheritance : PatchOperation
    {
        public required string defType;
        public required string parent;
        public string? child;
        public List<string>? children;

        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors())
                yield return error;

            if (defType is null)
                yield return $"{nameof(defType)} is null";
            if (parent is null)
                yield return $"{nameof(parent)} is null";
            if (child is null && children is null)
                yield return $"{nameof(child)} and {nameof(children)} are both null";
        }

        protected override bool ApplyWorker(XmlDocument xml)
        {
            ArgumentNullException.ThrowIfNull(xml);

            var defs = xml.SelectNodes($"/Defs/{defType}")!.OfType<XmlElement>().ToList();
            var parentDef = defs.LastOrDefault(def => def["defName"]?.InnerText == parent);
            if (parentDef is null)
                return false;

            var parentName = parentDef.GetAttributeNode("Name")?.Value;
            if (parentName is null)
            {
                parentName = $"{defType}.{parent}";
                parentDef.SetAttribute("Name", parentName);
            }

            HashSet<string> childNames = [];
            if (child is not null)
                childNames.Add(child);
            if (children is not null)
                childNames.AddRange(children);

            var foundChild = false;
            foreach (var def in defs)
            {
                if (def["defName"] is not { } defName || !childNames.Contains(defName.InnerText))
                    continue;

                def.SetAttribute("ParentName", parentName);
                foundChild = true;
            }

            return foundChild;
        }
    }
}
