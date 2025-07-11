using System;
using System.Linq;
using System.Xml;
using JetBrains.Annotations;
using Verse;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace XylRacesCore
{
    // This works like PatchOperationAdd, except if the node to be added already exists, the new node's children are added
    // to the existing mode. For example, if the existing node is
    //
    // <def>
    //   <list>
    //     <li>Foo</li>
    //   </list>
    // </def>
    //
    // and value is
    //
    // <list>
    //   <li>Bar</li>
    // </list>
    //
    // the result will be
    //
    // <def>
    //   <list>
    //     <li>Foo</li>
    //     <li>Bar</li>
    //   </list>
    // </def>
    [UsedImplicitly]
    public class PatchOperationAddOrMerge : PatchOperationPathed
    {
        private enum Order
        {
            Append, 
            Prepend
        }

        private XmlContainer value;

        private readonly Order order = Order.Prepend;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            XmlNode node = value.node;
            var result = false;

            if (xml == null)
                throw new ArgumentNullException(nameof(xml));

            foreach (XmlNode xmlNode in xml.SelectNodes(xpath)!)
            {
                result = true;
                XmlDocument xmlNodeOwnerDocument = xmlNode.OwnerDocument;
                if (xmlNodeOwnerDocument == null) 
                    continue;
                if (order == Order.Append)
                {
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        var existingNode = xmlNode.ChildNodes.OfType<XmlNode>()
                            .FirstOrDefault(xn => xn.Name == childNode.Name);
                        if (existingNode != null)
                        {
                            foreach (XmlNode grandchildNode in childNode.ChildNodes)
                                existingNode.AppendChild(
                                    xmlNodeOwnerDocument.ImportNode(grandchildNode, deep: true));
                        }
                        else
                            xmlNode.AppendChild(xmlNodeOwnerDocument.ImportNode(childNode, deep: true));
                    }
                }
                else if (order == Order.Prepend)
                {
                    for (int num = node.ChildNodes.Count - 1; num >= 0; num--)
                    {
                        var childNode = node.ChildNodes[num];
                        var existingNode = xmlNode.ChildNodes.OfType<XmlNode>()
                            .FirstOrDefault(xn => xn.Name == childNode.Name);
                        if (existingNode != null)
                        {
                            foreach (XmlNode grandchildNode in childNode.ChildNodes)
                                existingNode.PrependChild(
                                    xmlNodeOwnerDocument.ImportNode(grandchildNode, deep: true));
                        }
                        else
                            xmlNode.PrependChild(xmlNodeOwnerDocument.ImportNode(childNode, deep: true));
                    }
                }
            }

            return result;
        }
    }
}
