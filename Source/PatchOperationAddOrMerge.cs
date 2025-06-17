using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

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
    public class PatchOperationAddOrMerge : PatchOperationPathed
    {
        private enum Order
        {
            Append, 
            Prepend
        }

        private XmlContainer value;

        private Order order = Order.Prepend;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            XmlNode node = value.node;
            bool result = false;

            foreach (object item in xml.SelectNodes(xpath))
            {
                result = true;
                XmlNode xmlNode = item as XmlNode;
                if (order == Order.Append)
                {
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        var existingNode = xmlNode.ChildNodes.OfType<XmlNode>().FirstOrDefault(xn => xn.Name == childNode.Name);
                        if (existingNode != null)
                        {
                            foreach (XmlNode grandchildNode in childNode.ChildNodes)
                                existingNode.AppendChild(xmlNode.OwnerDocument.ImportNode(grandchildNode, deep: true));
                        }
                        else
                            xmlNode.AppendChild(xmlNode.OwnerDocument.ImportNode(childNode, deep: true));
                    }
                }
                else if (order == Order.Prepend)
                {
                    for (int num = node.ChildNodes.Count - 1; num >= 0; num--)
                    {
                        var childNode = node.ChildNodes[num];
                        var existingNode = xmlNode.ChildNodes.OfType<XmlNode>().FirstOrDefault(xn => xn.Name == childNode.Name);
                        if (existingNode != null)
                        {
                            foreach (XmlNode grandchildNode in childNode.ChildNodes)
                                existingNode.PrependChild(xmlNode.OwnerDocument.ImportNode(grandchildNode, deep: true));
                        }
                        else
                            xmlNode.PrependChild(xmlNode.OwnerDocument.ImportNode(childNode, deep: true));
                    }
                }
            }
            return result;
        }
    }
}
