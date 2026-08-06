namespace Disharmony.Optimizer;

/// <summary>
///     Represents an abstract operation,
/// </summary>
/// <remarks>
///     Unlike CIL instructions, operations are represented as a tree of operations, where each operation has multiple
///     inputs and a single output. Assignments to variables, including arguments, locals, and stack slots, are represented
///     explicitly. Operations which might throw are only allowed as the input to an assignment operation or branch.
/// </remarks>
/// <param name="Type"></param>
internal abstract record Op(Type Type);

/// <summary>
///     Represents an assignment to a variable.
/// </summary>
/// <param name="Output"></param>
/// <param name="Input"></param>
internal sealed record AssignmentOp(Variable Output, Op Input) : Op(typeof(void));

/// <summary>
///     Represents an IL prefix.
/// </summary>
/// <param name="OpCode"></param>
/// <param name="Operand"></param>
internal sealed record Prefix(OpCode OpCode, object Operand);

/// <summary>
///     Represents an IL operation.
/// </summary>
/// <remarks>
///     There is one input per stack slot popped by the IL opcode, and the output type is the type of the value pushed by the IL opcode,
///     or void if it does not push a value. Numeric values are always of type int, long, IntPtr, or double, representing the IL stack types.
/// </remarks>
/// <param name="OpCode"></param>
/// <param name="Operand"></param>
/// <param name="Prefixes"></param>
/// <param name="Inputs"></param>
/// <param name="Type"></param>
internal sealed record ILOp(OpCode OpCode, object Operand, IReadOnlyList<Prefix> Prefixes, IReadOnlyList<Op> Inputs, Type Type) : Op(Type);

/// <summary>
///     Represents a variable which can store a value during execution, including locals, arguments, stack slots, and temporaries.
/// </summary>
/// <param name="Type"></param>
internal abstract record Variable(Type Type) : Op(Type);

/// <summary>
///     Represents a stack slot from the incoming IL.
/// </summary>
/// <param name="Depth"></param>
/// <param name="Type"></param>
internal sealed record StackSlot(int Depth, Type Type) : Variable(Type);

/// <summary>
///     Base class for argument and local variables.
/// </summary>
/// <param name="Index"></param>
/// <param name="Type"></param>
internal abstract record MemoryVariable(int Index, Type Type) : Variable(Type);

/// <summary>
///     Represents an IL argument.
/// </summary>
/// <param name="Index"></param>
/// <param name="Type"></param>
internal sealed record Argument(int Index, Type Type) : MemoryVariable(Index, Type);

/// <summary>
///     Represents an IL local.
/// </summary>
/// <remarks>
///     The type may be <see cref="TypeLattice.AnyType"/> if the method's metadata does not specify a type for the local. In this
///     case, the local has to be optimized conservatively, retaining all loads and stores.
/// </remarks>
/// <param name="Index"></param>
/// <param name="Type"></param>
/// <param name="LocalBuilder"></param>
internal sealed record Local(int Index, Type Type, LocalBuilder? LocalBuilder) : MemoryVariable(Index, Type);

/// <summary>
///     Represents a temporary variable created during optimization.
/// </summary>
/// <remarks>
///     This might be something that didn't exist in the original IL, or a replacement for a stack slot or local that was removed
///     during optimization but needs to be recreated during IL emission.
/// </remarks>
/// <param name="Type"></param>
internal sealed record Temporary(Type Type) : Variable(Type);

/// <summary>
///     Represents a lack of value, such as the return value of a void method.
/// </summary>
internal sealed record VoidOp() : Op(typeof(void));