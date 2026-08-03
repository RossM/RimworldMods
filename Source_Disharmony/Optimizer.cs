using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Disharmony;

internal class UniqueQueue<T> : IEnumerable<T>
{
    public int Count => queue.Count;
    private readonly Queue<T> queue = [];
    private readonly HashSet<T> hashSet = [];

    public bool Enqueue(T item)
    {
        if (!hashSet.Add(item))
            return false;
        queue.Enqueue(item);
        return true;
    }

    public T Dequeue()
    {
        T item = queue.Dequeue();
        hashSet.Remove(item);
        return item;
    }

    public IEnumerator<T> GetEnumerator() => queue.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)queue).GetEnumerator();
}

internal partial class Optimizer
{
    internal class BasicBlock : RegionNode
    {
        // Convenience projections only; CFG mutations must operate on the edge collections.
        public BasicBlock? Next => fallthroughEdge?.Target;
        public IEnumerable<BasicBlock> Predecessors => incomingEdges.Select(edge => edge.Source);
        public IEnumerable<BasicBlock> Successors => outgoingEdges.Select(edge => edge.Target);
        public readonly List<Op> ops = [];
        public Label? label;

        // The canonical normal-control-flow graph. fallthroughEdge is null when the final
        // instruction always transfers control; otherwise it identifies the default continuation.
        public readonly List<ControlFlowEdge> incomingEdges = [];
        public readonly List<ControlFlowEdge> outgoingEdges = [];
        public ControlFlowEdge? fallthroughEdge;

        // Canonical in Variables form and empty in Stack form. These are the mutable logical stack
        // slots present on entry. A slot may be defined by operations in several predecessor blocks;
        // that deliberate non-SSA representation keeps ordinary control-flow edges empty.
        public readonly List<Variable> entryStackVariables = [];
    }

    internal sealed class ControlFlowEdge(BasicBlock source, BasicBlock target)
    {
        // Reserved for SSA construction and destruction. Stack form and regular Variables form
        // require this list to be empty; assignments, when present, occur in parallel.
        public readonly List<VariableAssignment> assignments = [];

        // Mutated only by the optimizer's edge helpers, which keep both endpoint collections in sync.
        public BasicBlock Source { get; internal set; } = source;
        public BasicBlock Target { get; internal set; } = target;
    }

    internal sealed class Variable
    {
        public string Name => kind switch
        {
            VariableKind.Argument => $"a{index}",
            VariableKind.Local => $"l{index}",
            VariableKind.StackSlot => $"s{id}",
            VariableKind.Temporary => $"v{id}",
            _ => throw new ArgumentOutOfRangeException(),
        };

        // Stable optimizer identity; unlike index, this is unique across all variable kinds.
        public required int id;
        public required VariableKind kind;

        // For a Local this is set only from authoritative local metadata or a LocalBuilder, never
        // inferred from stores. StackSlot and Temporary types come from symbolic stack analysis.
        public Type? type;

        // Argument/local index. Logical evaluation-stack variables leave this at -1.
        public int index = -1;

        // Preserves both the identity and authoritative type of transpiler-created locals when known.
        public LocalBuilder? localBuilder;

        public bool pinned;

        // Canonical summary of address operations still present in Variables form. Rewriting a
        // known reference can remove every such operation, so passes which do that must recompute
        // this field rather than treating it as a permanent historical fact.
        public bool addressTaken;

        public override string ToString() => Name;
    }

    /// <summary>Identifies the storage or logical value represented by a variable.</summary>
    internal enum VariableKind
    {
        /// <summary>A mutable CIL argument slot, including <c>this</c> at index zero.</summary>
        Argument,

        /// <summary>A mutable CIL local. Its declared type may be unavailable.</summary>
        Local,

        /// <summary>
        ///     A mutable logical evaluation-stack slot. Before SSA, the same slot may be defined in
        ///     several predecessor blocks and used as an entry value by their common successor.
        /// </summary>
        StackSlot,

        /// <summary>A value produced by an operation within a basic block.</summary>
        Temporary,
    }

    internal sealed class VariableAssignment(Variable source, Variable destination)
    {
        // This is a logical value transfer on a CFG edge, not an instruction to emit.
        public Variable Source { get; } = source;
        public Variable Destination { get; } = destination;
    }

    /// <summary>Which interpretation of the shared block and operation data is currently valid.</summary>
    internal enum IrForm
    {
        /// <summary>Only the original CIL evaluation-stack representation is available.</summary>
        Stack,

        /// <summary>
        ///     Operation operands are canonical explicit variables and CFG edges are empty. Stack
        ///     operand counts retain enough CIL semantics to schedule these variables back onto the
        ///     evaluation stack without maintaining a separate operation representation.
        /// </summary>
        Variables,
    }

    // Symbolic stack analysis treats types as a lattice joined by CombineTypes.
    // UnknownType and AnyType can also be used as the element type of a managed pointer,
    // preserving the known byref shape even when the referent type is imprecise. NullType is the
    // CLI's transient null verification type and is the bottom of the reference-type sublattice.
    /// <summary>
    ///     The bottom type: no type evidence has reached this value yet. Joining it with another
    ///     type yields that type, so later control-flow information can refine it.
    /// </summary>
    internal struct UnknownType;

    /// <summary>
    ///     The top type: a value exists, but its compatible CIL type is unavailable. Joining it with
    ///     any other type remains <see cref="AnyType"/>; missing metadata uses this rather than
    ///     <see cref="UnknownType"/> because additional control-flow evidence cannot restore it.
    /// </summary>
    internal struct AnyType;

    /// <summary>
    ///     A null value produced directly by <c>ldnull</c>. It exists only on the evaluation stack
    ///     and is verifier-assignable to every CLI reference type, including managed pointers.
    /// </summary>
    internal struct NullType;

    internal class Op(OpCode opcode, object? operand = null)
    {
        /// <summary>How an instruction accesses storage outside the evaluation stack.</summary>
        internal enum VariableAccessKind
        {
            /// <summary>Loads the current value of an argument or local.</summary>
            Read,

            /// <summary>Replaces the current value of an argument or local.</summary>
            Write,

            /// <summary>Takes the storage location's address, preventing ordinary SSA promotion.</summary>
            Address,
        }

        /// <summary>How an instruction accesses a value through its first stack operand.</summary>
        internal enum IndirectAccessKind
        {
            Load,
            Store,
        }

        // InputIndex identifies an output which aliases a popped input, as with both outputs of dup.
        // A negative index means that executing the instruction produces a new value.
        internal readonly record struct StackOutput(Type Type, int InputIndex = -1);

        // Recorded during symbolic execution so variable materialization does not reinterpret CIL opcodes.
        internal readonly record struct VariableAccess(VariableKind VariableKind, int Index, VariableAccessKind Kind);

        /// <summary>
        ///     Describes the canonical variable operand of an argument/local access after stack
        ///     conversion. <see cref="EncodedVariableKind"/> records what the original opcode
        ///     names; an optimization may independently replace <see cref="Variable"/>.
        /// </summary>
        internal readonly record struct StorageAccess(
            VariableAccessKind Kind,
            VariableKind EncodedVariableKind,
            Variable Variable);

        // Instances live only inside ConvertStackToVariables. Inputs are ordered from the deepest
        // popped value to the top of the evaluation stack.
        internal sealed class StackTransition
        {
            public readonly List<Type> inputTypes = [];
            public readonly List<StackOutput> outputs = [];
            public readonly List<VariableAccess> variableAccesses = [];
            public bool clearsStack;
        }

        public bool IsLeave => Opcode == OpCodes.Leave_S || Opcode == OpCodes.Leave;
        public bool ClearsStack => Opcode == OpCodes.Ret || Opcode == OpCodes.Leave_S || Opcode == OpCodes.Leave;
        public bool IsUnconditionalBranch => Opcode == OpCodes.Br_S || Opcode == OpCodes.Br;
        public bool CanBranch => Opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch;

        // Computed rather than cached because prefixes remain mutable while the IR is assembled.
        public OperationEffects Effects => OperationEffectClassifier.Classify(this);
        public bool CanDiscardIfUnused => OperationEffectClassifier.CanDiscardIfUnused(Effects);

        public bool CanFallThrough =>
            Opcode.FlowControl is FlowControl.Next or FlowControl.Call or FlowControl.Meta or FlowControl.Cond_Branch or FlowControl.Break;

        public int StackPops =>
            Opcode.StackBehaviourPop switch
            {
                StackBehaviour.Pop0 => 0,
                StackBehaviour.Pop1 => 1,
                StackBehaviour.Pop1_pop1 => 2,
                StackBehaviour.Popi => 1,
                StackBehaviour.Popi_pop1 => 2,
                StackBehaviour.Popi_popi => 2,
                StackBehaviour.Popi_popi8 => 2,
                StackBehaviour.Popi_popi_popi => 3,
                StackBehaviour.Popi_popr4 => 2,
                StackBehaviour.Popi_popr8 => 2,
                StackBehaviour.Popref => 1,
                StackBehaviour.Popref_pop1 => 2,
                StackBehaviour.Popref_popi => 2,
                StackBehaviour.Popref_popi_popi => 3,
                StackBehaviour.Popref_popi_popi8 => 3,
                StackBehaviour.Popref_popi_popr4 => 3,
                StackBehaviour.Popref_popi_popr8 => 3,
                StackBehaviour.Popref_popi_popref => 3,
                StackBehaviour.Varpop => Operand switch
                {
                    MethodBase method => method.GetParameters().Length + (method is MethodInfo { IsStatic: false } ? 1 : 0),
                    _ => 0,
                },
                StackBehaviour.Popref_popi_pop1 => 3,
                _ => throw new ArgumentOutOfRangeException(),
            };

        public int Index => unchecked((ushort)Opcode.Value) switch
        {
            OpCodeValues.Ldarg_0 => 0,
            OpCodeValues.Ldarg_1 => 1,
            OpCodeValues.Ldarg_2 => 2,
            OpCodeValues.Ldarg_3 => 3,
            OpCodeValues.Ldarg or OpCodeValues.Ldarg_S => ToLocalIndex(Operand),
            OpCodeValues.Ldarga or OpCodeValues.Ldarga_S => ToLocalIndex(Operand),
            OpCodeValues.Starg or OpCodeValues.Starg_S => ToLocalIndex(Operand),
            OpCodeValues.Ldloc_0 => 0,
            OpCodeValues.Ldloc_1 => 1,
            OpCodeValues.Ldloc_2 => 2,
            OpCodeValues.Ldloc_3 => 3,
            OpCodeValues.Ldloc or OpCodeValues.Ldloc_S => ToLocalIndex(Operand),
            OpCodeValues.Ldloca or OpCodeValues.Ldloca_S => ToLocalIndex(Operand),
            OpCodeValues.Stloc_0 => 0,
            OpCodeValues.Stloc_1 => 1,
            OpCodeValues.Stloc_2 => 2,
            OpCodeValues.Stloc_3 => 3,
            OpCodeValues.Stloc or OpCodeValues.Stloc_S => ToLocalIndex(Operand),
            _ => throw new ArgumentOutOfRangeException(),
        };

        // Prefixes remain attached to the operation they govern so no later pass can separate them.
        public readonly List<Op> prefixes = [];

        // Canonical in Variables form and empty in Stack form. Evaluation-stack values precede
        // argument/local accesses in each list; the counts identify the boundary between them.
        public readonly List<Variable> inputs = [];
        public readonly List<Variable> outputs = [];
        public int stackInputCount;
        public int stackOutputCount;

        public OpCode Opcode { get; } = opcode;
        public object? Operand { get; } = operand;

        /// <summary>
        ///     Returns the explicit storage operand attached by <see cref="StackToVariableConverter"/>,
        ///     or <see langword="null"/> for an operation which does not directly access an argument
        ///     or local. This is the canonical storage-opcode decoder for variable-form passes.
        /// </summary>
        internal StorageAccess? GetStorageAccess()
        {
            ushort opcode = unchecked((ushort)Opcode.Value);
            return opcode switch
            {
                OpCodeValues.Ldarg_0 or OpCodeValues.Ldarg_1 or OpCodeValues.Ldarg_2 or OpCodeValues.Ldarg_3 or
                    OpCodeValues.Ldarg or OpCodeValues.Ldarg_S =>
                    new(VariableAccessKind.Read, VariableKind.Argument, inputs[stackInputCount]),
                OpCodeValues.Ldarga or OpCodeValues.Ldarga_S =>
                    new(VariableAccessKind.Address, VariableKind.Argument, inputs[stackInputCount]),
                OpCodeValues.Starg or OpCodeValues.Starg_S =>
                    new(VariableAccessKind.Write, VariableKind.Argument, outputs[stackOutputCount]),
                OpCodeValues.Ldloc_0 or OpCodeValues.Ldloc_1 or OpCodeValues.Ldloc_2 or OpCodeValues.Ldloc_3 or
                    OpCodeValues.Ldloc or OpCodeValues.Ldloc_S =>
                    new(VariableAccessKind.Read, VariableKind.Local, inputs[stackInputCount]),
                OpCodeValues.Ldloca or OpCodeValues.Ldloca_S =>
                    new(VariableAccessKind.Address, VariableKind.Local, inputs[stackInputCount]),
                OpCodeValues.Stloc_0 or OpCodeValues.Stloc_1 or OpCodeValues.Stloc_2 or OpCodeValues.Stloc_3 or
                    OpCodeValues.Stloc or OpCodeValues.Stloc_S =>
                    new(VariableAccessKind.Write, VariableKind.Local, outputs[stackOutputCount]),
                _ => null,
            };
        }

        /// <summary>
        ///     Classifies the <c>ldobj</c>/<c>stobj</c> and <c>ldind</c>/<c>stind</c> opcode
        ///     families. Other memory operations are not indirect value accesses for this purpose.
        /// </summary>
        internal IndirectAccessKind? GetIndirectAccessKind() =>
            unchecked((ushort)Opcode.Value) switch
            {
                OpCodeValues.Ldobj or
                    OpCodeValues.Ldind_I1 or OpCodeValues.Ldind_U1 or
                    OpCodeValues.Ldind_I2 or OpCodeValues.Ldind_U2 or
                    OpCodeValues.Ldind_I4 or OpCodeValues.Ldind_U4 or
                    OpCodeValues.Ldind_I8 or OpCodeValues.Ldind_I or
                    OpCodeValues.Ldind_R4 or OpCodeValues.Ldind_R8 or OpCodeValues.Ldind_Ref =>
                    IndirectAccessKind.Load,
                OpCodeValues.Stobj or
                    OpCodeValues.Stind_I1 or OpCodeValues.Stind_I2 or OpCodeValues.Stind_I4 or
                    OpCodeValues.Stind_I8 or OpCodeValues.Stind_I or
                    OpCodeValues.Stind_R4 or OpCodeValues.Stind_R8 or OpCodeValues.Stind_Ref =>
                    IndirectAccessKind.Store,
                _ => null,
            };

        public int GetStackPops(Type returnType)
        {
            if (Opcode == OpCodes.Ret)
                return returnType == typeof(void) ? 0 : 1;
            if (Opcode == OpCodes.Jmp)
                return 0;
            if (Opcode.StackBehaviourPop != StackBehaviour.Varpop || Operand is not MethodBase calledMethod)
                return StackPops;

            int receiverCount = Opcode != OpCodes.Newobj && !calledMethod.IsStatic ? 1 : 0;
            return calledMethod.GetParameters().Length + receiverCount;
        }

        private static int ToLocalIndex(object? value)
        {
            if (value is LocalBuilder lb)
                return lb.LocalIndex;
            return Convert.ToInt32(value);
        }

        public CodeInstruction ToCodeInstruction() => new(Opcode, Operand);

        public void Deconstruct(out OpCode opcode, out object? operand)
        {
            opcode = Opcode;
            operand = Operand;
        }

        /// <summary>
        ///     Returns the value encoded by a CIL literal-loading opcode, or false when this
        ///     operation is not a supported literal.
        /// </summary>
        public bool TryGetLiteral([NotNullWhen(true)] out ConstantValue? constant)
        {
            constant = unchecked((ushort)Opcode.Value) switch
            {
                OpCodeValues.Ldnull => ConstantValue.Null,
                OpCodeValues.Ldstr when Operand is string text => ConstantValue.FromString(text),
                OpCodeValues.Ldc_I4_M1 => ConstantValue.FromInt32(-1),
                OpCodeValues.Ldc_I4_0 => ConstantValue.FromInt32(0),
                OpCodeValues.Ldc_I4_1 => ConstantValue.FromInt32(1),
                OpCodeValues.Ldc_I4_2 => ConstantValue.FromInt32(2),
                OpCodeValues.Ldc_I4_3 => ConstantValue.FromInt32(3),
                OpCodeValues.Ldc_I4_4 => ConstantValue.FromInt32(4),
                OpCodeValues.Ldc_I4_5 => ConstantValue.FromInt32(5),
                OpCodeValues.Ldc_I4_6 => ConstantValue.FromInt32(6),
                OpCodeValues.Ldc_I4_7 => ConstantValue.FromInt32(7),
                OpCodeValues.Ldc_I4_8 => ConstantValue.FromInt32(8),
                OpCodeValues.Ldc_I4_S => ConstantValue.FromInt32(Convert.ToSByte(Operand)),
                OpCodeValues.Ldc_I4 => ConstantValue.FromInt32(Convert.ToInt32(Operand)),
                OpCodeValues.Ldc_I8 => ConstantValue.FromInt64(Convert.ToInt64(Operand)),
                OpCodeValues.Ldc_R4 => ConstantValue.FromFloat32(Convert.ToSingle(Operand)),
                OpCodeValues.Ldc_R8 => ConstantValue.FromFloat64(Convert.ToDouble(Operand)),
                _ => null,
            };
            return constant != null;
        }
    }

    private static class Ops
    {
        public static Op Nop => new(OpCodes.Nop);
        public static Op Ret => new(OpCodes.Ret);
        public static Op Pop => new(OpCodes.Pop);
    }

    /// <summary>A node in the lexical region-containment hierarchy.</summary>
    internal class RegionNode
    {
        public bool EntryPoint => parent == null || parent.entry == this;
        public virtual string ID => $"#{id}";
        public int id = 0;
        public Region? parent;

        public override string ToString() => ID;

        public bool HasAncestor(Region region)
        {
            for (RegionNode? b = this; b != null; b = b.parent)
            {
                if (b == region)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    ///     A synthetic root, protected region, filter region, or handler region. Regions form the
    ///     lexical containment hierarchy. Their emission positions are derived from the ordered
    ///     basic blocks and must not be treated as independently mutable state.
    /// </summary>
    internal class Region : RegionNode
    {
        public override string ID => parent == null ? "Root" : $"{harmonyBlock!.blockType} #{id}";

        // The Harmony marker which begins this lexical body; null only for the synthetic root.
        public ExceptionBlock? harmonyBlock;

        // The first nested region or basic block in this body. This is canonical hierarchy data,
        // independent of where either item currently appears in basic-block order.
        public RegionNode? entry;

        // Null only for the synthetic root. This explicitly associates regions belonging to
        // exception entries which share a protected region; their order is stored by the group.
        public ExceptionEntryGroup? exceptionEntryGroup;
    }

    /// <summary>
    ///     Exception entries which share a protected region, with their filter and handler regions
    ///     in CIL layout order.
    /// </summary>
    internal sealed class ExceptionEntryGroup(Region protectedRegion)
    {
        public Region ProtectedRegion { get; } = protectedRegion;

        // A filtered entry contributes both its filter region and handler region to this list.
        public readonly List<Region> associatedRegions = [];

        public Region? NextRegion(Region region)
        {
            if (region == ProtectedRegion)
                return associatedRegions.FirstOrDefault();

            int index = associatedRegions.IndexOf(region);
            if (index < 0)
                throw new ArgumentException("Region is not a member of this exception-entry group", nameof(region));
            return index + 1 < associatedRegions.Count ? associatedRegions[index + 1] : null;
        }

        // Exceptional local dataflow does not follow the normal block CFG. A handler may observe
        // a local written at any potentially throwing instruction in ProtectedRegion, and a store
        // whose value computation throws leaves the old local value visible. Consequently, future
        // SSA construction must either model instruction-level exceptional predecessors (and
        // their unsplittable transfers), or leave exception-exposed arguments and locals in
        // physical storage. Catch/filter entry stack values are supplied by the runtime and are a
        // separate concern from those locals.
    }

    internal IReadOnlyList<BasicBlock> BasicBlocks => basicBlocks;
    internal IReadOnlyList<Region> Regions => regions;
    internal IReadOnlyList<ExceptionEntryGroup> ExceptionEntryGroups => exceptionEntryGroups;
    internal IReadOnlyList<Variable> Variables => variables;
    internal IReadOnlyDictionary<int, Variable> ArgumentVariables => argumentVariables;
    internal IReadOnlyDictionary<int, Variable> LocalVariables => localVariables;

    public readonly InstructionList outputInstructions = [];
    // Canonical lexical hierarchy nodes, including the synthetic root.
    private readonly List<Region> regions = [];

    // Canonical groupings of exception entries which share the same protected region.
    private readonly List<ExceptionEntryGroup> exceptionEntryGroups = [];

    // Canonical normal-control-flow nodes. Their ordering changes only when a pass deliberately
    // establishes a new layout order; CFG relationships live on ControlFlowEdge instead.
    private List<BasicBlock> basicBlocks = [];

    // Block dominance is valid until the block set, normal edges, or implicit exception entries
    // change. It is computed explicitly at the start of passes which need it, never by a property.
    private DominatorTree? dominatorTree;

    // One canonical object represents each physical argument/local; logical stack values receive
    // distinct identities in variables as they are discovered.
    private readonly List<Variable> variables = [];
    private readonly Dictionary<int, Variable> argumentVariables = [];
    private readonly Dictionary<int, Variable> localVariables = [];
    private int nextVariableId;
    private readonly Region root = new();
    private int nextBlockId = 1;
    private readonly bool valid = false;
    private readonly MethodBase method;
    private readonly List<CodeInstruction> inputInstructions;
    private readonly ILGenerator generator;
    private readonly bool debug;
    private readonly List<Type> parameterTypes;
    private readonly Type returnType;

    public Optimizer(MethodBase method, List<CodeInstruction> inputInstructions, ILGenerator generator, bool debug)
    {
        this.method = method;
        this.inputInstructions = inputInstructions;
        this.generator = generator;
        this.debug = debug;

        if (method.HasThis)
            parameterTypes = [method.DeclaringType.CallableType, .. method.GetParameters().Types()];
        else
            parameterTypes = [.. method.GetParameters().Types()];

        returnType = method is MethodInfo methodInfo ? methodInfo.ReturnType : typeof(void);

        valid = true;
    }

    internal IrForm Form { get; private set; }

    private static bool IsSpecialType(Type type) =>
        type == typeof(AnyType) || type == typeof(UnknownType) || type == typeof(NullType) ||
        type.IsByRef && IsSpecialType(type.GetElementType()!);

    private static Type FromRef(Type type)
    {
        if (type.IsByRef)
            return type.GetElementType()!;
        if (IsSpecialType(type))
            return type;
        throw new InvalidOperationException();
    }

    private static bool IsBlockStart(ExceptionBlock b) => b.blockType != ExceptionBlockType.EndExceptionBlock;
    private static bool IsBlockEnd(ExceptionBlock b) => b.blockType == ExceptionBlockType.EndExceptionBlock;

    private void LogInstructions(string phase, IEnumerable<CodeInstruction> instructions)
    {
        if (!debug)
            return;

        int codePos = 0;

        FileLog.LogBuffered($"### Optimizer {phase}: {method.FullDescription()}");

        foreach (var codeInstruction in instructions)
            LogInstruction(codeInstruction, ref codePos);

        FileLog.LogBuffered("");
        FileLog.FlushBuffer();
    }

    /// <summary>
    ///     Before final reordering, logs basic blocks with their region paths without implying that
    ///     their current order is legal CIL layout. After reordering, also logs derived region
    ///     boundaries; <paramref name="structuredLayout"/> therefore requires the aggressive-pass
    ///     postconditions.
    /// </summary>
    private void LogBlocks(string phase, bool structuredLayout = false)
    {
        if (!debug)
            return;

        int codePos = 0;
        Stack<Region> regionStack = new();

        FileLog.LogBuffered($"### Optimizer {phase}: {method.FullDescription()}");

        IEnumerable<RegionNode> nodes = structuredLayout ? GetStructuredLayout() : basicBlocks;
        foreach (var block in nodes)
        {
            while (structuredLayout && regionStack.Count >= 1 && block.parent != regionStack.Peek())
            {
                Region exitedRegion = regionStack.Peek();
                if (exitedRegion.harmonyBlock != null &&
                    exitedRegion.exceptionEntryGroup?.NextRegion(exitedRegion) == null)
                    FileLog.LogILBlockEnd(codePos, new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
                regionStack.Pop();
            }

            FileLog.LogBuffered($"## Block:        {block.ID}");
            if (block is BasicBlock basicBlock)
            {
                if (!structuredLayout)
                {
                    FileLog.LogBuffered($"## Region Path:  {string.Join(" > ", GetRegionPath(basicBlock.parent).Select(r => r.ID))}");
                }
                FileLog.LogBuffered($"## Predecessors: {string.Join(", ", basicBlock.Predecessors.Select(b => b.ID))}");
                FileLog.LogBuffered($"## Successors:   {string.Join(", ", basicBlock.Successors.Select(b => b.ID))}");
                if (Form == IrForm.Variables)
                {
                    FileLog.LogBuffered($"## Entry Stack:  {string.Join(", ", basicBlock.entryStackVariables)}");
                    foreach (var edge in basicBlock.incomingEdges)
                    {
                        string assignments = string.Join(", ", edge.assignments.Select(assignment =>
                            $"{assignment.Destination} = {assignment.Source}"));
                        FileLog.LogBuffered($"## Assignments {edge.Source.ID} => {edge.Target.ID}: {assignments}");
                    }
                }
            }

            if (block is { EntryPoint: true, parent: not null })
                FileLog.LogBuffered($"## Entry Point:  {block.parent.ID}");

            if (block is BasicBlock { label: Label label })
                FileLog.LogIL(codePos, label);

            switch (block)
            {
                case Region region:
                {
                    regionStack.Push(region);
                    if (region.harmonyBlock != null)
                        FileLog.LogILBlockBegin(codePos, region.harmonyBlock);
                    break;
                }
                case BasicBlock bb:
                {
                    foreach (var op in bb.ops)
                    {
                        foreach (var prefix in op.prefixes)
                            LogInstruction(ConvertToCodeInstruction(prefix), ref codePos);
                        if (Form == IrForm.Variables)
                            LogVariableInstruction(op, ref codePos);
                        else
                            LogInstruction(ConvertToCodeInstruction(op), ref codePos);
                    }

                    if (bb.ops.Count == 0)
                        LogInstruction(Ops.Nop.ToCodeInstruction(), ref codePos);

                    break;
                }
            }

            if (block is BasicBlock { Next: not null } bb2)
                FileLog.LogBuffered($"IL_{codePos:X4}: // fallthrough => {bb2.Next.ID}");
        }

        while (structuredLayout && regionStack.Count > 0)
        {
            if (regionStack.Peek().harmonyBlock != null)
                FileLog.LogILBlockEnd(codePos, new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            regionStack.Pop();
        }

        FileLog.LogBuffered("");
        FileLog.FlushBuffer();
    }

    /// <summary>
    ///     Derives region-start markers from region entries and basic-block order. Every region
    ///     must begin with its recursive entry basic block, remain contiguous, and be followed by
    ///     the next filter/handler region in its exception-entry group. The aggressive reorder
    ///     pass establishes these preconditions.
    /// </summary>
    private List<RegionNode> GetStructuredLayout()
    {
        Dictionary<RegionNode, Region> regionByEntry = [];
        foreach (var region in regions.Where(region => region != root))
        {
            if (region.entry == null)
                throw new InvalidOperationException($"Region {region.ID} has no entry node");
            if (regionByEntry.ContainsKey(region.entry))
                throw new InvalidOperationException($"More than one region has entry node {region.entry.ID}");
            regionByEntry.Add(region.entry, region);
        }

        HashSet<Region> emittedRegions = [root];
        List<RegionNode> layout = [root];

        foreach (var block in basicBlocks)
        {
            AddRegionsBefore(block);
            layout.Add(block);
        }

        Region? missingRegion = regions.FirstOrDefault(region => !emittedRegions.Contains(region));
        if (missingRegion != null)
            throw new InvalidOperationException($"Region {missingRegion.ID} contains no retained basic block");

        return layout;

        void AddRegionsBefore(RegionNode entry)
        {
            if (!regionByEntry.TryGetValue(entry, out var region))
                return;
            if (!emittedRegions.Add(region))
                throw new InvalidOperationException($"Region {region.ID} has a cyclic entry chain");
            AddRegionsBefore(region);
            layout.Add(region);
        }
    }

    private static List<Region> GetRegionPath(Region? region)
    {
        List<Region> path = [];
        for (; region != null; region = region.parent)
            path.Add(region);
        path.Reverse();
        return path;
    }

    private static void LogVariableInstruction(Op op, ref int codePos)
    {
        string opcode = op.Opcode.ToString();
        if (op.Opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch)
            opcode += " =>";
        opcode = opcode.PadRight(10);

        string inputs = string.Join(", ", op.inputs);
        string outputs = string.Join(", ", op.outputs);
        string variables = (inputs.Length, outputs.Length) switch
        {
            (0, 0) => "",
            (0, _) => $"=> {outputs}",
            (_, 0) => inputs,
            _ => $"{inputs} => {outputs}",
        };
        string separator = variables.Length > 0 ? " " : "";
        FileLog.LogBuffered($"IL_{codePos:X4}: {opcode}{separator}{variables}");
        codePos += ReflectionTools.ILSize(op.Opcode);
    }

    private static void LogInstruction(CodeInstruction codeInstruction, ref int codePos)
    {
        foreach (var label in codeInstruction.labels)
            FileLog.LogIL(codePos, label);
        foreach (var block2 in codeInstruction.blocks)
            FileLog.LogILBlockBegin(codePos, block2);

        var code = codeInstruction.opcode;
        var operand = codeInstruction.operand;

        var realCode = true;
        switch (code.OperandType)
        {
            case OperandType.InlineNone:
                if (code == OpCodes.Nop && operand is string s)
                {
                    FileLog.LogILComment(codePos, s);
                    realCode = false;
                }
                else
                    FileLog.LogIL(codePos, code);

                break;

            default: FileLog.LogIL(codePos, code, operand); break;
        }

        foreach (var block2 in codeInstruction.blocks)
            FileLog.LogILBlockEnd(codePos, block2);
        if (realCode)
            codePos += ReflectionTools.ILSize(codeInstruction.opcode);
    }

    public List<CodeInstruction> Optimize()
    {
        if (!valid)
            return inputInstructions;

        LogInstructions("Input", inputInstructions);

        MakeBasicBlocks();
        LogBlocks(nameof(MakeBasicBlocks));

        NopElimination();
        LogBlocks(nameof(NopElimination));

        SimpleDeadCodeElimination();
        LogBlocks(nameof(SimpleDeadCodeElimination));

        ConvertStackToVariables();
        LogBlocks(nameof(ConvertStackToVariables));

        JumpThreading();
        LogBlocks(nameof(JumpThreading));

        SimpleDeadCodeElimination();
        LogBlocks(nameof(SimpleDeadCodeElimination));

        BranchElimination();
        LogBlocks(nameof(BranchElimination));

        MergeBlocks();
        LogBlocks(nameof(MergeBlocks));

        // MergeBlocks deliberately leaves absorbed successors in the block list. Remove them
        // before analyses which index operations by identity; the merged block and absorbed block
        // temporarily share the same Op objects.
        SimpleDeadCodeElimination();
        LogBlocks(nameof(SimpleDeadCodeElimination));

        ConservativeConstantPropagation();
        LogBlocks(nameof(ConservativeConstantPropagation));

        ConvertVariablesToStack();
        LogBlocks(nameof(ConvertVariablesToStack));

        BranchInversion();
        LogBlocks(nameof(BranchInversion));

        AggressiveDeadCodeEliminationAndReorder();
        LogBlocks(nameof(AggressiveDeadCodeEliminationAndReorder), true);

        InsertBranches();
        LogBlocks(nameof(InsertBranches), true);

        Emit();
        LogInstructions("Output", outputInstructions.instructions);

        return outputInstructions.instructions;
    }

    private void ConvertStackToVariables()
    {
        new StackToVariableConverter(this).ConvertStackToVariables();
    }

    private void ConvertVariablesToStack()
    {
        new VariableToStackConverter(this).ConvertVariablesToStack();
    }

    internal void ConservativeConstantPropagation()
    {
        new ConservativeConstantPropagator(this).Propagate();
    }

    private DominatorTree ComputeDominatorTreeIfNeeded()
    {
        return dominatorTree ??= DominatorTree.Compute(basicBlocks, GetDominatorRoots());
    }

    /// <summary>
    ///     Returns the explicit entries used for normal-CFG dominance: the recursive method entry
    ///     plus every filter and handler entry whose exceptional predecessor is absent from the
    ///     edge graph. A protected-region entry is reached normally and is not an extra root.
    /// </summary>
    private List<BasicBlock> GetDominatorRoots()
    {
        List<BasicBlock> roots = [GetRecursiveEntryBlock(root)];
        foreach (var entryGroup in exceptionEntryGroups)
        {
            foreach (var associatedRegion in entryGroup.associatedRegions)
                roots.Add(GetRecursiveEntryBlock(associatedRegion));
        }

        return roots;
    }

    private static BasicBlock GetRecursiveEntryBlock(Region region)
    {
        HashSet<Region> visited = [];
        RegionNode? entry = region;
        while (entry is Region entryRegion)
        {
            if (!visited.Add(entryRegion))
                throw new InvalidOperationException($"Region {entryRegion.ID} has a cyclic entry chain");
            entry = entryRegion.entry;
        }

        return entry as BasicBlock ??
               throw new InvalidOperationException($"Region {region.ID} has no recursive entry block");
    }

    private void InvalidateControlFlowAnalyses()
    {
        dominatorTree = null;
    }

    /// <summary>
    ///     Emits stack-form operations in the current basic-block order. The order must satisfy the
    ///     structured-region preconditions used by <see cref="GetStructuredLayout"/>.
    /// </summary>
    internal void Emit()
    {
        if (Form != IrForm.Stack)
            throw new InvalidOperationException($"Cannot emit {Form} form; convert it to stack form first");

        // Derive and materialize before mutating outputInstructions, so entry-chain errors cannot
        // leave behind a partially emitted method.
        List<RegionNode> emissionLayout = GetStructuredLayout();
        Stack<Region> regionStack = new();
        List<ExceptionBlock> harmonyBlocks = [];
        List<Label> labels = [];

        foreach (var block in emissionLayout)
        {
            while (regionStack.Count >= 1 && block.parent != regionStack.Peek())
            {
                Region exitedRegion = regionStack.Peek();
                if (exitedRegion.harmonyBlock != null &&
                    exitedRegion.exceptionEntryGroup?.NextRegion(exitedRegion) == null)
                    outputInstructions.instructions[^1].blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
                regionStack.Pop();
            }

            if (block is BasicBlock { label: Label label })
                labels.Add(label);

            switch (block)
            {
                case Region region:
                {
                    regionStack.Push(region);
                    if (region.harmonyBlock != null)
                        harmonyBlocks.Add(region.harmonyBlock);
                    break;
                }
                case BasicBlock bb:
                {
                    List<CodeInstruction> instructions =
                    [
                        .. bb.ops.SelectMany(op => op.prefixes.Append(op)).Select(ConvertToCodeInstruction),
                    ];
                    if (instructions.Count == 0)
                        instructions.Add(Ops.Nop.ToCodeInstruction());
                    instructions[0].labels.AddRange(labels);
                    labels.Clear();
                    instructions[0].blocks.AddRange(harmonyBlocks);
                    harmonyBlocks.Clear();
                    outputInstructions.instructions.AddRange(instructions);
                    break;
                }
            }
        }

        while (regionStack.Count > 0)
        {
            if (regionStack.Peek().harmonyBlock != null)
                outputInstructions.instructions[^1].blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            regionStack.Pop();
        }
    }

    private CodeInstruction ConvertToCodeInstruction(Op i)
    {
        var codeInstruction = i.ToCodeInstruction();
        codeInstruction.operand = codeInstruction.operand switch
        {
            ControlFlowEdge edge => GetLabel(edge),
            ControlFlowEdge[] edges => edges.Select(GetLabel).ToArray(),
            _ => codeInstruction.operand,
        };

        return codeInstruction;

        Label GetLabel(ControlFlowEdge edge) => edge.Target.label ??= generator.DefineLabel();
    }

    /// <summary>
    ///     Generates the canonical normal CFG and lexical-region hierarchy. Basic blocks initially
    ///     remain in input order; after CFG edges are created, that order is not canonical analysis
    ///     state and need not yet satisfy the final CIL layout restrictions.
    /// </summary>
    internal void MakeBasicBlocks()
    {
        InvalidateControlFlowAnalyses();
        Dictionary<Label, BasicBlock> labelToBlock = [];

        Region currentRegion = root;
        regions.Add(root);

        BasicBlock curBlock = new() { id = nextBlockId++, parent = currentRegion };
        basicBlocks.Add(curBlock);
        currentRegion.entry ??= curBlock;

        foreach (var inst in inputInstructions)
        {
            if (inst.labels.Count > 0 || inst.blocks.Any(IsBlockStart))
            {
                foreach (var harmonyBlock in inst.blocks.Where(IsBlockStart))
                    EnterRegion(harmonyBlock);

                NewBasicBlock();
                foreach (var label in inst.labels)
                    labelToBlock[label] = curBlock;
                if (inst.labels.Count >= 1)
                    curBlock.label = inst.labels[0];
            }

            curBlock.ops.Add(new(inst.opcode, inst.operand));

            if (inst.CanBranch || inst.blocks.Any(IsBlockEnd))
            {
                foreach (var _ in inst.blocks.Where(IsBlockEnd))
                    currentRegion = currentRegion.parent!;

                NewBasicBlock();
            }
        }

        if (curBlock.ops.Count == 0)
            basicBlocks.Remove(curBlock);

        Dictionary<BasicBlock, BasicBlock> fallthroughTargets = [];
        for (int i = 0; i < basicBlocks.Count - 1; i++)
        {
            if (CanFallThrough(basicBlocks[i]))
                fallthroughTargets.Add(basicBlocks[i], basicBlocks[i + 1]);
        }

        // Add a ret to the last basic block if one is missing (perhaps because of a poorly behaved transpiler)
        if (CanFallThrough(basicBlocks[^1]))
            basicBlocks[^1].ops.Add(Ops.Ret);

        // Convert branch instructions to point directly at the basic block
        foreach (var block in basicBlocks)
        {
            for (var index = 0; index < block.ops.Count; index++)
            {
                Op? op = block.ops[index];
                block.ops[index] = op.Operand switch
                {
                    Label label => new(op.Opcode, GetTarget(label)),
                    Label[] labels => new(op.Opcode, labels.Select(GetTarget).ToArray()),
                    _ => block.ops[index],
                };
            }

            BasicBlock GetTarget(Label label) => labelToBlock[label];
        }

        // Convert block-final unconditional branches to fallthrough
        foreach (var block in basicBlocks)
        {
            if (block.ops.Count == 0)
                continue;

            if (block.ops[^1].IsUnconditionalBranch)
            {
                fallthroughTargets[block] = (BasicBlock)block.ops[^1].Operand!;
                block.ops.RemoveAt(block.ops.Count - 1);
            }
        }

        foreach (var block in basicBlocks)
        {
            if (fallthroughTargets.TryGetValue(block, out var fallthroughTarget))
                block.fallthroughEdge = AddControlFlowEdge(block, fallthroughTarget);

            if (block.ops.Count == 0)
                continue;

            Op finalOperation = block.ops[^1];
            block.ops[^1] = finalOperation.Operand switch
            {
                BasicBlock target => new(finalOperation.Opcode, AddControlFlowEdge(block, target)),
                BasicBlock[] targets => new(finalOperation.Opcode,
                    targets.Select(target => AddControlFlowEdge(block, target)).ToArray()),
                _ => finalOperation,
            };
        }

        BundlePrefixes();

        return;

        void EnterRegion(ExceptionBlock harmonyBlock)
        {
            if (harmonyBlock.blockType == ExceptionBlockType.BeginExceptionBlock)
            {
                var newRegion = new Region
                {
                    id = nextBlockId++,
                    harmonyBlock = harmonyBlock,
                    parent = currentRegion,
                };
                var newEntryGroup = new ExceptionEntryGroup(newRegion);
                newRegion.exceptionEntryGroup = newEntryGroup;
                exceptionEntryGroups.Add(newEntryGroup);
                regions.Add(newRegion);
                currentRegion.entry ??= newRegion;
                currentRegion = newRegion;
            }
            else
            {
                ExceptionEntryGroup entryGroup = currentRegion.exceptionEntryGroup ??
                    throw new InvalidOperationException("Handler marker does not follow a protected or handler region");
                var newRegion = new Region
                {
                    id = nextBlockId++,
                    harmonyBlock = harmonyBlock,
                    parent = currentRegion.parent,
                    exceptionEntryGroup = entryGroup,
                };
                entryGroup.associatedRegions.Add(newRegion);
                regions.Add(newRegion);
                currentRegion = newRegion;
            }
        }

        void NewBasicBlock()
        {
            if (curBlock.ops.Count == 0)
            {
                curBlock.parent = currentRegion;
            }
            else
            {
                BasicBlock newBlock = new() { id = nextBlockId++, parent = currentRegion };
                basicBlocks.Add(newBlock);
                curBlock = newBlock;
            }

            currentRegion.entry ??= curBlock;
        }

        static bool CanFallThrough(BasicBlock basicBlock) =>
            basicBlock.ops.Count == 0 || basicBlock.ops[^1].CanFallThrough;
    }

    private void BundlePrefixes()
    {
        foreach (var block in basicBlocks)
        {
            List<Op> operations = [];
            List<Op> prefixes = [];
            foreach (var op in block.ops)
            {
                if (op.Opcode.OpCodeType == OpCodeType.Prefix)
                {
                    prefixes.Add(op);
                    continue;
                }

                op.prefixes.Clear();
                op.prefixes.AddRange(prefixes);
                prefixes.Clear();
                operations.Add(op);
            }

            if (prefixes.Count > 0)
                throw new InvalidOperationException($"Basic block {block.ID} ends in a CIL prefix");

            block.ops.Clear();
            block.ops.AddRange(operations);
        }
    }

    private ControlFlowEdge AddControlFlowEdge(BasicBlock source, BasicBlock target)
    {
        var edge = new ControlFlowEdge(source, target);
        source.outgoingEdges.Add(edge);
        target.incomingEdges.Add(edge);
        InvalidateControlFlowAnalyses();
        return edge;
    }

    private void RemoveControlFlowEdge(ControlFlowEdge edge)
    {
        if (!edge.Source.outgoingEdges.Remove(edge) || !edge.Target.incomingEdges.Remove(edge))
            throw new InvalidOperationException("Control-flow edge is not attached to both endpoint blocks");
        if (edge.Source.fallthroughEdge == edge)
            edge.Source.fallthroughEdge = null;
        InvalidateControlFlowAnalyses();
    }

    private void RedirectControlFlowEdge(ControlFlowEdge edge, BasicBlock target)
    {
        if (!edge.Target.incomingEdges.Remove(edge))
            throw new InvalidOperationException("Control-flow edge is not attached to its target block");
        edge.Target = target;
        target.incomingEdges.Add(edge);
        InvalidateControlFlowAnalyses();
    }

    private void MoveControlFlowEdgeSource(ControlFlowEdge edge, BasicBlock source)
    {
        BasicBlock oldSource = edge.Source;
        if (oldSource == source)
        {
            if (!source.outgoingEdges.Contains(edge))
                throw new InvalidOperationException("Control-flow edge is not attached to its source block");
            return;
        }

        bool isFallthrough = oldSource.fallthroughEdge == edge;
        if (isFallthrough && source.fallthroughEdge != null)
            throw new InvalidOperationException($"Basic block {source.ID} already has a fallthrough edge");
        if (!oldSource.outgoingEdges.Remove(edge))
            throw new InvalidOperationException("Control-flow edge is not attached to its source block");
        if (isFallthrough)
            oldSource.fallthroughEdge = null;
        edge.Source = source;
        source.outgoingEdges.Add(edge);
        if (isFallthrough)
            source.fallthroughEdge = edge;
        InvalidateControlFlowAnalyses();
    }

    internal void NopElimination()
    {
        foreach (var block in basicBlocks)
            block.ops.RemoveAll(i => i.Opcode == OpCodes.Nop);
    }

    internal void BranchElimination()
    {
        foreach (var block in basicBlocks)
        {
            if (block.ops.Count == 0)
                continue;
            if (block.fallthroughEdge is not { } fallthroughEdge)
                continue;
            if (block.outgoingEdges.Any(edge => edge.Target != fallthroughEdge.Target))
                continue;

            switch (block.ops[^1].Opcode)
            {
                // Brtrue, Brfalse
                case
                {
                    FlowControl: FlowControl.Cond_Branch, StackBehaviourPop: StackBehaviour.Popi, StackBehaviourPush: StackBehaviour.Push0,
                }:
                {
                    ReplaceBranchWithPops(block.ops[^1], 1);
                    RemoveBranchEdges();
                    break;
                }
                // Beq, Bge, Bgt, Ble, Blt, Bne_Un, Bge_Un, Bgt_Un, Ble_Un, Blt_Un
                case
                {
                    FlowControl: FlowControl.Cond_Branch, StackBehaviourPop: StackBehaviour.Pop1_pop1,
                    StackBehaviourPush: StackBehaviour.Push0,
                }:
                {
                    ReplaceBranchWithPops(block.ops[^1], 2);
                    RemoveBranchEdges();
                    break;
                }
            }

            void ReplaceBranchWithPops(Op branch, int popCount)
            {
                block.ops.RemoveAt(block.ops.Count - 1);
                if (Form == IrForm.Stack)
                {
                    for (int index = 0; index < popCount; index++)
                        block.ops.Add(Ops.Pop);
                    return;
                }

                if (branch.stackInputCount != popCount || branch.inputs.Count < popCount)
                    throw new InvalidOperationException($"Invalid variable operands on {branch.Opcode} in {block.ID}");

                // A branch records popped values from deepest to topmost; separate pop operations
                // must consume them in the reverse order in which they occur on the CIL stack.
                for (int index = popCount - 1; index >= 0; index--)
                {
                    Op pop = Ops.Pop;
                    pop.inputs.Add(branch.inputs[index]);
                    pop.stackInputCount = 1;
                    block.ops.Add(pop);
                }
            }

            void RemoveBranchEdges()
            {
                foreach (var edge in block.outgoingEdges.Where(edge => edge != fallthroughEdge).ToArray())
                    RemoveControlFlowEdge(edge);
            }
        }
    }

    internal void JumpThreading()
    {
        foreach (var block in basicBlocks)
        {
            ControlFlowEdge? fallthroughEdge = block.fallthroughEdge;
            if (fallthroughEdge == null)
                continue;
            BasicBlock fallthroughBlock = fallthroughEdge.Target;
            int iterations = 0;
            while (fallthroughBlock is { ops.Count: 0, EntryPoint: false, fallthroughEdge: not null } bb &&
                   bb.fallthroughEdge.Target.parent == bb.parent &&
                   iterations++ < 20)
                fallthroughBlock = bb.fallthroughEdge.Target;

            if (fallthroughEdge.Target != fallthroughBlock)
                RedirectControlFlowEdge(fallthroughEdge, fallthroughBlock);
        }
    }

    /// <summary>
    ///     Merges eligible successor operations and outgoing edges into their predecessor. An
    ///     absorbed successor remains in <c>basicBlocks</c> as an unreachable node until a later
    ///     dead-code pass removes it; no pass may treat list membership as reachability.
    /// </summary>
    internal void MergeBlocks()
    {
        for (int i = basicBlocks.Count - 1; i >= 0; i--)
        {
            var block = basicBlocks[i];
            if (block.outgoingEdges is not [var successorEdge])
                continue;
            if (block.ops.Count > 0 && block.ops[^1].CanBranch)
                continue;
            BasicBlock successor = successorEdge.Target;
            if (successor.incomingEdges.Count != 1 || successor.parent != block.parent || successor is not { EntryPoint: false })
                continue;

            block.ops.AddRange(successor.ops);
            RemoveControlFlowEdge(successorEdge);

            foreach (var edge in successor.outgoingEdges.ToArray())
                MoveControlFlowEdgeSource(edge, block);
        }
    }

    /// <summary>
    ///     Removes basic blocks unreachable through the normal CFG while treating every lexical
    ///     region entry as a root, since handler predecessors are intentionally absent from that
    ///     CFG. Region and exception-entry metadata are pruned by the aggressive pass instead.
    /// </summary>
    internal void SimpleDeadCodeElimination()
    {
        Queue<BasicBlock> queue = new();
        HashSet<BasicBlock> liveBlocks = [];

        // Every lexical region entry is an analysis root: handlers have implicit exceptional
        // predecessors which are intentionally absent from the normal-control-flow CFG.
        foreach (var block in basicBlocks)
        {
            if (block.EntryPoint)
                queue.Enqueue(block);
        }

        while (queue.Count > 0)
        {
            var block = queue.Dequeue();
            if (!liveBlocks.Add(block))
                continue;
            foreach (var edge in block.outgoingEdges)
                queue.Enqueue(edge.Target);
        }

        foreach (var deadBlock in basicBlocks.Where(block => !liveBlocks.Contains(block)).ToArray())
        {
            foreach (var edge in deadBlock.incomingEdges.Concat(deadBlock.outgoingEdges).Distinct().ToArray())
                RemoveControlFlowEdge(edge);
        }

        if (basicBlocks.RemoveAll(block => !liveBlocks.Contains(block)) > 0)
            InvalidateControlFlowAnalyses();
    }

    /// <summary>
    ///     Removes every node unreachable from a method or region entry and orders the retained
    ///     basic blocks for CIL emission. On return, every region begins with its recursive entry
    ///     basic block and is contiguous, associated filter and handler regions immediately follow
    ///     their protected region, and stack-carrying backward edges satisfy the CIL
    ///     forward-predecessor requirement.
    /// </summary>
    internal void AggressiveDeadCodeEliminationAndReorder()
    {
        ValidateAggressiveReorderPreconditions();

        // CIL permits a backward edge carrying evaluation-stack values only when the target also
        // has a forward incoming edge. Earlier optimizer passes may temporarily violate that
        // restriction; ordering blocks by first control-flow visit restores it before emission.
        List<BasicBlock> outputBlocks = [];
        HashSet<RegionNode> retainedNodes = [root];
        Stack<(Region region, LinkedList<RegionNode> queue)> stack = [];
        List<RegionNode> leavingBlocks = [];

        stack.Push((root, []));
        stack.Peek().queue.AddLast(root.entry!);

        while (stack.Count >= 1)
        {
            var (region, queue) = stack.Peek();

            if (queue.Count == 0)
            {
                stack.Pop();
                if (stack.Count > 0)
                {
                    (region, queue) = stack.Peek();
                    foreach (var leavingBlock in leavingBlocks.Where(b => b.parent == region))
                        queue.AddLast(leavingBlock);
                    leavingBlocks.RemoveAll(b => b.parent == region);
                }

                continue;
            }

            var block = queue.First.Value;
            queue.RemoveFirst();
            if (!block.HasAncestor(region))
                throw new InvalidOperationException();
            while (block.parent != region)
                block = block.parent!;

            if (!retainedNodes.Add(block))
                continue;
            if (block is BasicBlock retainedBlock)
                outputBlocks.Add(retainedBlock);

            if (debug)
                FileLog.LogBuffered($"{"".PadLeft(stack.Count * 2)}- {block.ID}");

            switch (block)
            {
                case Region chainedRegion:
                {
                    Region? nextRegion = chainedRegion.exceptionEntryGroup?.NextRegion(chainedRegion);
                    if (nextRegion != null)
                        queue.AddFirst(nextRegion);
                    break;
                }
                case BasicBlock { fallthroughEdge: not null } basicBlock: queue.AddFirst(basicBlock.fallthroughEdge.Target); break;
            }

            switch (block)
            {
                case Region nestedRegion:
                {
                    (region, queue) = (nestedRegion, []);
                    stack.Push((region, queue));
                    queue.AddLast(nestedRegion.entry!);
                    break;
                }
                case BasicBlock bb:
                {
                    foreach (var edge in bb.outgoingEdges)
                    {
                        BasicBlock successor = edge.Target;
                        if (!successor.HasAncestor(region))
                            leavingBlocks.Add(successor);
                        else if (edge != bb.fallthroughEdge)
                            queue.AddLast(successor);
                    }

                    break;
                }
                default: throw new InvalidOperationException();
            }
        }

        foreach (var deadBlock in basicBlocks.Where(block => !retainedNodes.Contains(block)).ToArray())
        {
            foreach (var edge in deadBlock.incomingEdges.Concat(deadBlock.outgoingEdges).Distinct().ToArray())
                RemoveControlFlowEdge(edge);
        }

        if (basicBlocks.Count != outputBlocks.Count)
            InvalidateControlFlowAnalyses();
        basicBlocks = outputBlocks;
        regions.RemoveAll(region => !retainedNodes.Contains(region));
        if (exceptionEntryGroups.RemoveAll(group => !retainedNodes.Contains(group.ProtectedRegion)) > 0)
            InvalidateControlFlowAnalyses();
    }

    private void ValidateAggressiveReorderPreconditions()
    {
        if (root.entry == null)
            throw new InvalidOperationException("The root region has no entry node");

        foreach (var region in regions)
        {
            if (region != root && region.parent == null)
                throw new InvalidOperationException($"Region {region.ID} is not attached to the region hierarchy");
            if (region.entry == null)
                throw new InvalidOperationException($"Region {region.ID} has no entry node");
            if (!region.entry.HasAncestor(region))
                throw new InvalidOperationException($"Entry node {region.entry.ID} is outside region {region.ID}");
        }

        foreach (var block in basicBlocks)
        {
            if (block.parent == null || !regions.Contains(block.parent))
                throw new InvalidOperationException($"Basic block {block.ID} has no retained parent region");
        }

        foreach (var group in exceptionEntryGroups)
        {
            if (!regions.Contains(group.ProtectedRegion) ||
                group.ProtectedRegion.exceptionEntryGroup != group ||
                group.associatedRegions.Any(region => !regions.Contains(region) || region.exceptionEntryGroup != group) ||
                group.associatedRegions.Distinct().Count() != group.associatedRegions.Count)
            {
                throw new InvalidOperationException("Exception-entry group membership is inconsistent");
            }
        }
    }

    private MethodBody? GetMethodBodyOrNull()
    {
        try
        {
            return method.GetMethodBody();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private Variable GetArgumentVariable(int index) => argumentVariables.TryGetValue(index, out var variable)
        ? variable
        : throw new InvalidOperationException($"Unknown argument #{index}");

    private Variable GetLocalVariable(int index)
    {
        if (localVariables.TryGetValue(index, out var variable))
            return variable;

        variable = NewVariable(VariableKind.Local, null, index);
        localVariables.Add(index, variable);
        return variable;
    }

    private Variable NewVariable(
        VariableKind kind,
        Type? type,
        int index = -1,
        LocalBuilder? localBuilder = null,
        bool pinned = false)
    {
        var variable = new Variable
        {
            id = nextVariableId++,
            kind = kind,
            type = type,
            index = index,
            localBuilder = localBuilder,
            pinned = pinned,
        };
        variables.Add(variable);
        return variable;
    }

    private static bool ReferencesLocal(Op op) => unchecked((ushort)op.Opcode.Value) is
        OpCodeValues.Ldloc_0 or OpCodeValues.Ldloc_1 or OpCodeValues.Ldloc_2 or OpCodeValues.Ldloc_3 or
        OpCodeValues.Ldloc or OpCodeValues.Ldloc_S or OpCodeValues.Ldloca or OpCodeValues.Ldloca_S or
        OpCodeValues.Stloc_0 or OpCodeValues.Stloc_1 or OpCodeValues.Stloc_2 or OpCodeValues.Stloc_3 or
        OpCodeValues.Stloc or OpCodeValues.Stloc_S;

    internal void BranchInversion()
    {
        foreach (var block in basicBlocks)
        {
            if (block.ops.Count == 0)
                continue;
            if (block.fallthroughEdge is not { } fallthroughEdge)
                continue;
            if (fallthroughEdge.Target.incomingEdges.Count == 1)
                continue;

            var finalInstruction = block.ops[^1];
            if (finalInstruction.Opcode.FlowControl is FlowControl.Cond_Branch &&
                finalInstruction.Operand is ControlFlowEdge { Target.incomingEdges.Count: 1 } branchEdge)
            {
                if (finalInstruction.Opcode == OpCodes.Brfalse || finalInstruction.Opcode == OpCodes.Brfalse_S)
                {
                    block.ops[^1] = new(OpCodes.Brtrue_S, fallthroughEdge);
                    block.fallthroughEdge = branchEdge;
                }

                if (finalInstruction.Opcode == OpCodes.Brtrue || finalInstruction.Opcode == OpCodes.Brtrue_S)
                {
                    block.ops[^1] = new(OpCodes.Brfalse_S, fallthroughEdge);
                    block.fallthroughEdge = branchEdge;
                }
            }
        }
    }

    /// <summary>
    ///     Treats the current basic-block order as the intended emission order. Converts every
    ///     semantic fallthrough which does not target the next physical block into an explicit
    ///     branch; afterward, remaining fallthrough edges match physical fallthrough. The normal
    ///     pipeline calls this after <see cref="AggressiveDeadCodeEliminationAndReorder"/>.
    /// </summary>
    internal void InsertBranches()
    {
        if (Form != IrForm.Stack)
            throw new InvalidOperationException($"Cannot insert emitting branches in {Form} form");

        for (int i = 0; i < basicBlocks.Count; i++)
        {
            ControlFlowEdge? fallthroughEdge = basicBlocks[i].fallthroughEdge;
            if (fallthroughEdge == null || i < basicBlocks.Count - 1 && fallthroughEdge.Target == basicBlocks[i + 1])
                continue;
            basicBlocks[i].ops.Add(new(OpCodes.Br_S, fallthroughEdge));
            basicBlocks[i].fallthroughEdge = null;
        }
    }

    private static List<Type> GetBaseTypes(Type type)
    {
        if (type.IsValueType || type.IsByRef)
            return [type];
        if (type.IsInterface)
            return [typeof(object), type];
        List<Type> output = [];
        for (Type? ancestor = type; ancestor != null; ancestor = ancestor.BaseType)
            output.Add(ancestor);
        output.Reverse();
        return output;
    }

    internal static Type CombineTypes(Type left, Type right)
    {
        if (left == typeof(UnknownType) || right == typeof(AnyType) || left == right)
            return right;
        if (right == typeof(UnknownType) || left == typeof(AnyType))
            return left;

        // ECMA-335 III.1.8.1.2.3 makes the transient null type verifier-assignable to every
        // reference type. Pointer types, including managed pointers, are reference types in the
        // CTS even though they are not object types.
        if (left == typeof(NullType))
            return IsReferenceType(right) ? right : typeof(void);
        if (right == typeof(NullType))
            return IsReferenceType(left) ? left : typeof(void);

        // Interfaces and their implementations have a direct least upper bound that is not visible
        // in either type's BaseType chain. Value types are excluded because CIL requires an explicit
        // box instruction before an unboxed value can join an object or interface stack type.
        if (!left.IsValueType && !right.IsValueType && !left.IsByRef && !right.IsByRef)
        {
            if (left.IsAssignableFrom(right))
                return left;
            if (right.IsAssignableFrom(left))
                return right;
        }

        if (left.IsByRef || right.IsByRef)
        {
            if (!left.IsByRef || !right.IsByRef)
                return typeof(void);
            Type elementType = CombineTypes(left.GetElementType()!, right.GetElementType()!);
            return elementType == typeof(void) ? typeof(void) : ToRef(elementType);
        }

        var leftTypes = GetBaseTypes(left);
        var rightTypes = GetBaseTypes(right);
        for (int i = Math.Min(leftTypes.Count, rightTypes.Count) - 1; i >= 0; i--)
        {
            if (leftTypes[i] == typeof(object) && TryGetCommonInterface(left, right, out Type? commonInterface))
                return commonInterface;

            if (leftTypes[i] == rightTypes[i])
                return leftTypes[i];
        }

        // No value is possible
        return typeof(void);
    }

    private static bool IsReferenceType(Type type)
    {
        if (type == typeof(NullType) || type.IsByRef || type.IsPointer)
            return true;
        if (type.IsGenericParameter)
            return false;

        return !type.IsValueType && !IsSpecialType(type);
    }

    private static bool TryGetCommonInterface(Type left, Type right, [NotNullWhen(true)] out Type? value)
    {
        HashSet<Type> interfaces = [.. left.GetInterfaces().Intersect(right.GetInterfaces())];
        List<Type> mostSpecific = [.. interfaces.Where(i =>
            !interfaces.Any(i2 => i != i2 && i.IsAssignableFrom(i2)))];
        value = mostSpecific.Count == 1 ? mostSpecific[0] : null;
        return mostSpecific.Count == 1;
    }


    private static List<Type> CombineTypeLists(
        IReadOnlyList<Type> left,
        IReadOnlyList<Type> right,
        bool padIfNeeded = false)
    {
        if (!padIfNeeded && left.Count != right.Count)
            throw new ArgumentException();

        int count = Math.Max(left.Count, right.Count);
        List<Type> output = new(count);
        for (int i = 0; i < count; i++)
        {
            Type leftType = i < left.Count ? left[i] : typeof(UnknownType);
            Type rightType = i < right.Count ? right[i] : typeof(UnknownType);
            output.Add(CombineTypes(leftType, rightType));
        }

        return output;
    }

    // Even when the referent type is imprecise, taking its address establishes that the stack value
    // is a managed pointer. Keeping the lattice marker as the element type retains both facts.
    private static Type ToRef(Type type) => type.MakeByRefType();
}
