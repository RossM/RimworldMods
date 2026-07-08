using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

var path = args.Length > 0
    ? args[0]
    : Path.Combine("Source_XylRaces", "ExternalAnnotations", "Assembly-CSharp.xml");
path = Path.GetFullPath(path);

var bytes = File.ReadAllBytes(path);
var hadUtf8Bom = bytes is [0xEF, 0xBB, 0xBF, ..];
var text = Encoding.UTF8.GetString(bytes);
if (hadUtf8Bom && text.Length > 0 && text[0] == '\uFEFF')
{
    text = text[1..];
}

var members = ReadTopLevelMembers(text).ToList();
var sortedMembers = members
    .OrderBy(static member => SortKey(member.Name), StringComparer.Ordinal)
    .ThenBy(static member => member.Name, StringComparer.Ordinal)
    .ToList();

var output = new StringBuilder(text);
var offsetDelta = 0;
for (var i = 0; i < members.Count; i++)
{
    var destination = members[i];
    var replacement = sortedMembers[i].Text;
    var start = destination.Start + offsetDelta;
    var length = destination.End - destination.Start;

    output.Remove(start, length);
    output.Insert(start, replacement);
    offsetDelta += replacement.Length - length;
}

File.WriteAllText(path, output.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: hadUtf8Bom));

Console.WriteLine($"Sorted {members.Count} <member> elements in {path}.");

static IEnumerable<MemberBlock> ReadTopLevelMembers(string text)
{
    var lineStarts = GetLineStarts(text);
    var settings = new XmlReaderSettings
    {
        DtdProcessing = DtdProcessing.Prohibit,
        IgnoreComments = false,
        IgnoreWhitespace = false
    };

    using var stringReader = new StringReader(text);
    using var reader = XmlReader.Create(stringReader, settings);
    var lineInfo = (IXmlLineInfo)reader;
    var sawAssembly = false;
    string? memberName = null;
    int? memberStart = null;

    while (reader.Read())
    {
        if (reader is { NodeType: XmlNodeType.Element, Depth: 0 })
        {
            if (reader.Name != "assembly")
            {
                throw new InvalidOperationException("Expected top-level <assembly> element.");
            }

            sawAssembly = true;
        }

        if (reader is { NodeType: XmlNodeType.Element, Depth: 1, Name: "member" })
        {
            memberName = reader.GetAttribute("name") ?? "";
            memberStart = GetOffset(lineStarts, lineInfo);

            if (reader.IsEmptyElement)
            {
                var memberEnd = FindTagEnd(text, memberStart.Value) + 1;
                yield return new MemberBlock(memberName, memberStart.Value, memberEnd, text[memberStart.Value..memberEnd]);
                memberName = null;
                memberStart = null;
            }
        }
        else if (reader is { NodeType: XmlNodeType.EndElement, Depth: 1, Name: "member" })
        {
            if (memberName is null || memberStart is null)
            {
                throw new InvalidOperationException("Found </member> without a matching top-level <member>.");
            }

            var memberEnd = FindTagEnd(text, GetOffset(lineStarts, lineInfo)) + 1;
            yield return new MemberBlock(memberName, memberStart.Value, memberEnd, text[memberStart.Value..memberEnd]);
            memberName = null;
            memberStart = null;
        }
    }

    if (!sawAssembly)
    {
        throw new InvalidOperationException("Expected top-level <assembly> element.");
    }
}

static List<int> GetLineStarts(string text)
{
    var lineStarts = new List<int> { 0 };
    for (var i = 0; i < text.Length; i++)
    {
        if (text[i] == '\n')
        {
            lineStarts.Add(i + 1);
        }
    }

    return lineStarts;
}

static int GetOffset(IReadOnlyList<int> lineStarts, IXmlLineInfo lineInfo)
{
    return lineStarts[lineInfo.LineNumber - 1] + lineInfo.LinePosition - 1;
}

static int FindTagEnd(string text, int start)
{
    var end = text.IndexOf('>', start);
    if (end < 0)
    {
        throw new InvalidOperationException($"Could not find tag end after offset {start}.");
    }

    return end;
}

static string SortKey(string name)
{
    var colonIndex = name.IndexOf(':');
    return colonIndex >= 0 ? name[(colonIndex + 1)..] : name;
}

internal sealed record MemberBlock(string Name, int Start, int End, string Text);
