using System.Xml;

namespace Xylib.Tests;

[TestFixture(TestType.Playing)]
public sealed class XmlOperationTests : SeededTestFixture
{
    [Test]
    public void MergeCombinesContainersAndSameClassExtensions()
    {
        var xml = new XmlDocument();
        xml.LoadXml("<Defs><GeneDef><tags><li>A</li></tags><modExtensions><li Class='Example'><first>1</first></li></modExtensions></GeneDef></Defs>");
        var value = new XmlDocument();
        value.LoadXml("<value><tags><li>B</li></tags><modExtensions><li Class='Example'><second>2</second></li></modExtensions><newField>3</newField></value>");
        var operation = new PatchOperationMerge { value = new XmlContainer { node = value.DocumentElement } };
        typeof(PatchOperationPathed).GetField("xpath", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(operation, "/Defs/GeneDef");
        Expect.IsTrue(operation.Apply(xml));
        Expect.AreEqual(1, xml.SelectNodes("/Defs/GeneDef/tags")!.Count);
        Expect.AreEqual(2, xml.SelectNodes("/Defs/GeneDef/tags/li")!.Count);
        Expect.AreEqual(1, xml.SelectNodes("/Defs/GeneDef/modExtensions/li")!.Count);
        Expect.AreEqual("2", xml.SelectSingleNode("/Defs/GeneDef/modExtensions/li/second")!.InnerText);
        Expect.AreEqual("3", xml.SelectSingleNode("/Defs/GeneDef/newField")!.InnerText);
    }

    [Test]
    public void ReplaceGenesDoesNotChainAndDisablesOriginals()
    {
        var xml = new XmlDocument();
        xml.LoadXml("<Defs><GeneDef><defName>A</defName></GeneDef><GeneDef><defName>B</defName></GeneDef><XenotypeDef><genes><li>A</li><li>B</li></genes></XenotypeDef></Defs>");
        var operation = new PatchOperationReplaceGenes
        {
            entries = [new() { original = "A", replacement = "B" }, new() { original = "B", replacement = "C" }],
        };
        Expect.IsTrue(operation.Apply(xml));
        Expect.AreEqual("B,C", string.Join(",", xml.SelectNodes("/Defs/XenotypeDef/genes/li")!.Cast<XmlNode>().Select(n => n.InnerText)));
        Expect.AreEqual(2, xml.SelectNodes("/Defs/GeneDef[canGenerateInGeneSet='false']")!.Count);
        Expect.AreEqual(2, xml.SelectNodes("/Defs/GeneDef/modExtensions/li/showInXenotypeCreation[text()='false']")!.Count);
    }

    [Test]
    public void ReplaceGenesMissingOriginalLeavesInputUnchanged()
    {
        var xml = new XmlDocument();
        xml.LoadXml("<Defs><GeneDef><defName>A</defName></GeneDef><XenotypeDef><genes><li>A</li></genes></XenotypeDef></Defs>");
        string before = xml.OuterXml;
        var operation = new PatchOperationReplaceGenes { entries = [new() { original = "Missing", replacement = "B" }] };
        Expect.IsFalse(operation.Apply(xml));
        Expect.AreEqual(before, xml.OuterXml);
    }
}
