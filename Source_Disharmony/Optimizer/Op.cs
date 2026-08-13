namespace Disharmony.Optimizer;

/// <summary>
///     Represents an abstract operation.
/// </summary>
/// <remarks>
///     Unlike CIL instructions, <see cref="Op" />s are represented as a tree, where each operation has multiple
///     inputs and a single output. Assignments to variables, including arguments, locals, and stack slots, are represented
///     explicitly. Operations which might throw are only allowed at top level or as the input to an
///     <see cref="AssignmentOp" /> or <see cref="Branch" />.
/// </remarks>
/// <param name="Type">The type of value produced by the <see cref="Op" />.</param>
internal abstract record Op(Type Type);

/// <summary>
///     Represents an assignment to a <see cref="Variable" />.
/// </summary>
/// <param name="Output">The <see cref="Variable" /> that receives the value.</param>
/// <param name="Input">The <see cref="Op" /> that produces the value.</param>
internal sealed record AssignmentOp(Variable Output, Op Input) : Op(typeof(void));

/// <summary>
///     Represents an IL prefix.
/// </summary>
/// <param name="OpCode">The prefix opcode.</param>
/// <param name="Operand">The prefix operand.</param>
internal sealed record Prefix(OpCode OpCode, object Operand);

/// <summary>
///     Represents an IL instruction.
/// </summary>
/// <param name="OpCode"></param>
/// <param name="Operand"></param>
/// <param name="Prefixes"></param>
internal sealed record ILInstruction(OpCode OpCode, object Operand, IReadOnlyList<Prefix> Prefixes);

/// <summary>
///     Represents an IL operation.
/// </summary>
/// <remarks>
///     There is one input per stack slot popped by the IL opcode, and the output type is the type of the value pushed by
///     the IL opcode,
///     or <see cref="void" /> if it does not push a value. Numeric values use <see cref="int" />, <see cref="long" />,
///     <see cref="IntPtr" />, or <see cref="double" /> to represent the IL stack types.
/// </remarks>
/// <param name="OpCode">The IL opcode.</param>
/// <param name="Operand">The opcode operand.</param>
/// <param name="Prefixes">The prefixes applied to the instruction.</param>
/// <param name="Inputs">The <see cref="Op" />s that produce the instruction's stack inputs.</param>
/// <param name="Type">The type of value produced by the instruction.</param>
internal sealed record ILOp(ILInstruction IL, IReadOnlyList<Op> Inputs, Type Type) : Op(Type);

/// <summary>
///     Represents a variable which can store a value during execution, including <see cref="Local" />s,
///     <see cref="Argument" />s, <see cref="StackSlot" />s, and <see cref="Temporary" /> variables.
/// </summary>
/// <param name="Type">The type of value stored by the <see cref="Variable" />.</param>
internal abstract record Variable(Type Type) : Op(Type);

/// <summary>
///     Represents a stack slot from the incoming IL.
/// </summary>
/// <param name="Depth">The stack depth, where zero is the bottom of the stack.</param>
/// <param name="Type">The type of value stored in the slot.</param>
internal sealed record StackSlot(int Depth, Type Type, int Id = -1) : Variable(Type);

/// <summary>
///     Base class for <see cref="Argument" /> and <see cref="Local" /> variables.
/// </summary>
/// <param name="Index">The argument or local index.</param>
/// <param name="Type">The type of value stored by the variable.</param>
internal abstract record MemoryVariable(int Index, Type Type) : Variable(Type);

/// <summary>
///     Represents an IL argument.
/// </summary>
/// <param name="Index">The argument index.</param>
/// <param name="Type">The argument type.</param>
internal sealed record Argument(int Index, Type Type) : MemoryVariable(Index, Type);

/// <summary>
///     Represents an IL local.
/// </summary>
/// <remarks>
///     The type may be <see cref="TypeLattice.AnyType" /> if the method's metadata does not specify a type for the local.
///     In this
///     case, the local has to be optimized conservatively, retaining all loads and stores.
/// </remarks>
/// <param name="Index">The local index.</param>
/// <param name="Type">The local type.</param>
/// <param name="LocalBuilder">The builder for the emitted local, or <see langword="null" /> if one has not been created.</param>
internal sealed record Local(int Index, Type Type, LocalBuilder? LocalBuilder) : MemoryVariable(Index, Type);

/// <summary>
///     Represents a temporary variable created during optimization.
/// </summary>
/// <remarks>
///     This might be something that didn't exist in the original IL, or a replacement for a <see cref="StackSlot" /> or
///     <see cref="Local" /> that was removed
///     during optimization but needs to be recreated during IL emission.
/// </remarks>
/// <param name="Type">The type of value stored by the temporary.</param>
internal sealed record Temporary(Type Type) : Variable(Type);

/// <summary>
///     Represents a lack of value, such as the return value of a void method.
/// </summary>
internal sealed record VoidOp() : Op(typeof(void));
