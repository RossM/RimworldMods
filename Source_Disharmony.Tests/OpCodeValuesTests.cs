using System.Reflection;
using System.Reflection.Emit;

namespace Disharmony.Tests;

[TestFixture]
public sealed class OpCodeValuesTests
{
    [Test]
    public void ConstantsMatchRuntimeOpCodes()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;

        Dictionary<string, OpCode> opCodes = [];
        HashSet<OpCode> seenOpCodes = [];

        foreach (FieldInfo opCodeField in typeof(OpCodes).GetFields(flags))
            opCodes[opCodeField.Name] = (OpCode)opCodeField.GetValue(null)!;

        foreach (FieldInfo valueField in typeof(OpCodeValues).GetFields(flags))
        {
            var opCode = opCodes[valueField.Name];
            seenOpCodes.Add(opCode);

            int expected = unchecked((ushort)opCode.Value);
            Assert.That(valueField.GetRawConstantValue(), Is.EqualTo(expected),
                $"OpCodeValues.{valueField.Name} does not match OpCodes.{valueField.Name}.Value");
        }

        foreach (var opCode in opCodes.Values.Where(o => o.FlowControl == FlowControl.Next))
        {
            Assert.That(seenOpCodes.Contains(opCode), Is.True);
        }
    }
}
