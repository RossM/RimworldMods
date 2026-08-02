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

        foreach (FieldInfo valueField in typeof(OpCodeValues).GetFields(flags))
        {
            FieldInfo? opCodeField = typeof(OpCodes).GetField(valueField.Name, flags);
            Assert.That(opCodeField, Is.Not.Null, $"OpCodes.{valueField.Name} does not exist");
            Assert.That(opCodeField!.FieldType, Is.EqualTo(typeof(OpCode)));

            int expected = unchecked((ushort)((OpCode)opCodeField.GetValue(null)!).Value);
            Assert.That(valueField.GetRawConstantValue(), Is.EqualTo(expected),
                $"OpCodeValues.{valueField.Name} does not match OpCodes.{valueField.Name}.Value");
        }
    }
}
