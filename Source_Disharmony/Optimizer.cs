using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Disharmony;

/// <summary>
///     FIFO worklist which contains each value at most once. queue and hashSet always describe the
///     same membership; dequeuing permits the value to be enqueued again later.
/// </summary>
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

/// <summary>
///     Owns one shared IR whose canonical interpretation changes over the pipeline. The normal state
///     sequence is: no IR; unordered Stack form after MakeBasicBlocks; regular Variables form after
///     ConvertStackToVariables; Stack form again after ConvertVariablesToStack; emission-ordered
///     Stack form after AggressiveDeadCodeEliminationAndReorder and InsertBranches; then canonical
///     output after Emit. Future SSA form belongs between regular Variables form and lowering and
///     will use the same blocks/operations with an explicit additional <see cref="IrForm"/> state.
///     These are pass-boundary invariants: conversion workers temporarily build the destination
///     representation before changing Form, but no other pass may observe that mixed state.
/// </summary>
internal partial class Optimizer
{
    /// <summary>
    ///     A node in the normal CFG containing operations which execute consecutively unless an
    ///     operation throws. Normal branches, returns, explicit throws, and leaves occur only as
    ///     the final operation. Exceptional transfers are represented by the region hierarchy,
    ///     not by <see cref="incomingEdges"/> or <see cref="outgoingEdges"/>.
    /// </summary>
    internal class BasicBlock : RegionNode
    {
        // Non-canonical read-only projections of the edge fields below. They may contain the same
        // block more than once when distinct CFG edges share an endpoint. CFG mutations must use
        // the optimizer's edge helpers, not these projections or either endpoint collection.
        public BasicBlock? Next => fallthroughEdge?.Target;
        public IEnumerable<BasicBlock> Predecessors => incomingEdges.Select(edge => edge.Source);
        public IEnumerable<BasicBlock> Successors => outgoingEdges.Select(edge => edge.Target);

        // Canonical operation sequence in both IR forms. In Stack form the CIL evaluation stack is
        // implicit; in Variables form each operation's inputs/outputs are canonical. An absorbed
        // block may temporarily share these Op instances with its merger until dead-block removal.
        public readonly List<Op> ops = [];

        // Non-canonical emission metadata. This preserves one input label when available and is
        // otherwise assigned lazily if Emit needs a label. CFG edges, never labels, are canonical.
        public Label? label;

        // The canonical normal CFG after MakeBasicBlocks. Every edge occurs exactly once in its
        // source's outgoingEdges and target's incomingEdges, and its endpoints agree with those
        // collections. Every edge referenced by a branch Op is also in that Op's block's
        // outgoingEdges. fallthroughEdge is either null or one member of outgoingEdges identifying
        // the default continuation not encoded by the final operation. InsertBranches may turn a
        // non-adjacent default continuation into an explicit branch and clear fallthroughEdge.
        public readonly List<ControlFlowEdge> incomingEdges = [];
        public readonly List<ControlFlowEdge> outgoingEdges = [];
        public ControlFlowEdge? fallthroughEdge;

        // Canonical only in Variables form and deliberately empty in Stack form. In regular
        // Variables form these are shared mutable logical stack slots: every predecessor's natural
        // exit stack is identical by object identity to its target's entryStackVariables, and edge
        // assignments are empty. In future SSA Variables form these become block-entry SSA names;
        // predecessor-specific values may then be supplied by parallel incoming-edge assignments.
        public readonly List<Variable> entryStackVariables = [];
    }

    internal sealed class ControlFlowEdge(BasicBlock source, BasicBlock target)
    {
        // Canonical only in SSA Variables form and during conversion into or out of it. Stack form
        // and regular Variables form require every edge's list to be empty. Assignments on one edge
        // execute in parallel and are logical value transfers, not emitted CIL instructions.
        public readonly List<VariableAssignment> assignments = [];

        // Canonical endpoints after MakeBasicBlocks. Mutated only by the optimizer's edge helpers,
        // which also update endpoint collections, preserve fallthroughEdge, and invalidate cached
        // control-flow analyses.
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

        // Stable identity within one Variables-form interval. Unlike index, this is unique across
        // all variable kinds. IDs and the variable registry are reset when Variables form is built
        // or discarded; Variable objects are not canonical in Stack form.
        public required int id;
        public required VariableKind kind;

        // Canonical Variables-form type information. Argument types come from the method signature.
        // A Local type is set only from MethodBody metadata or a LocalBuilder, never inferred from
        // stores, and may therefore be null. StackSlot and Temporary types come from symbolic stack
        // analysis and may contain the special lattice-marker types below.
        public Type? type;

        // Canonical physical slot index for Argument and Local; -1 for logical StackSlot and
        // Temporary values. Distinct argument/local variables never represent the same slot.
        public int index = -1;

        // Optional canonical metadata for a Local created by a transpiler. When present, its index
        // and type agree with this Variable; pinned combines all authoritative metadata seen for the
        // slot. Null means only that no LocalBuilder was supplied, since the original MethodBody may
        // still provide authoritative type metadata.
        public LocalBuilder? localBuilder;

        // Canonical Variables-form pinned flag for Local; false for other variable kinds. It is
        // populated only from authoritative local metadata or a LocalBuilder.
        public bool pinned;

        // Canonical only in Variables form. True exactly when a remaining operation takes this
        // argument/local's address. Rewriting address operations can change the value, so such a
        // pass must recompute it; this is a current-IR summary, not historical escape information.
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
        ///     A logical evaluation-stack slot crossing a basic-block boundary. In regular
        ///     Variables form the same mutable slot may be defined by several predecessors; SSA
        ///     construction replaces that interpretation with single-definition values.
        /// </summary>
        StackSlot,

        /// <summary>A value produced by an operation within a basic block.</summary>
        Temporary,
    }

    internal sealed class VariableAssignment(Variable source, Variable destination)
    {
        // Valid only as an element of ControlFlowEdge.assignments in SSA Variables form. Source and
        // Destination participate in one parallel logical transfer; this is never emitted directly.
        public Variable Source { get; } = source;
        public Variable Destination { get; } = destination;
    }

    /// <summary>
    ///     Which interpretation of the shared block and operation data is canonical. The current
    ///     values describe Stack form and regular (non-SSA) Variables form. Future SSA will use the
    ///     same block/operation structures but must add an explicit state here: an SSA function with
    ///     no joins can have no edge assignments, so assignment-list contents are not a sufficient
    ///     form discriminator.
    /// </summary>
    internal enum IrForm
    {
        /// <summary>
        ///     Operations are executable CIL stack operations. Variable operands, counts, block
        ///     entry stacks, the variable registry, and edge assignments are empty/non-canonical.
        /// </summary>
        Stack,

        /// <summary>
        ///     Regular, non-SSA Variables form. Operation operands are canonical explicit variables,
        ///     stack operand counts retain the original CIL stack arity needed to lower back to Stack
        ///     form, and every CFG edge assignment list is empty.
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

    internal class Op(OpCode opcode, object? operand, IReadOnlyList<Op> prefixes)
    {
        public Op(OpCode opcode) : this(opcode, null, []) { }

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

        // Transient StackToVariableConverter result. InputIndex identifies an output which aliases
        // a popped input, as with both outputs of dup; a negative index denotes a new value.
        internal readonly record struct StackOutput(Type Type, int InputIndex = -1);

        // Transient StackToVariableConverter result recorded by symbolic execution so variable
        // materialization does not reinterpret the opcode or its original storage operand.
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

        // Non-canonical transient state owned only by one ConvertStackToVariables invocation.
        // Inputs are ordered from the deepest popped value to the top of the evaluation stack.
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

        public OperationEffects Effects => effectsCached ??= OperationEffectClassifier.Classify(this);
        private OperationEffects? effectsCached;
        public bool CanDiscardIfUnused => (Effects & OperationEffectClassifier.PreventsDiscard) == 0;

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

        // Canonical in both forms after MakeBasicBlocks bundles prefixes. Prefix Op objects do not
        // also occur in BasicBlock.ops; keeping them here prevents later passes from separating a
        // prefix from the operation it governs.
        public IReadOnlyList<Op> Prefixes => prefixes;

        // Canonical only in Variables form and empty/defaulted in Stack form. Evaluation-stack
        // values occupy inputs[0..stackInputCount) and outputs[0..stackOutputCount); explicit
        // argument/local operands follow them. The counts retain the operation's intrinsic CIL
        // stack arity even if a Variables-form optimization rewrites which values are used.
        public readonly List<Variable> inputs = [];
        public readonly List<Variable> outputs = [];
        public int stackInputCount;
        public int stackOutputCount;

        // Canonical in both forms. After MakeBasicBlocks, branch operands are ControlFlowEdge
        // objects rather than labels. In Variables form a storage opcode's encoded Operand may be
        // stale after rewriting; GetStorageAccess().Variable is then the canonical storage target.
        public OpCode Opcode { get; } = opcode;
        public object? Operand { get; } = operand;

        /// <summary>
        ///     Requires Variables form with valid stack counts. Returns the explicit storage operand
        ///     attached by <see cref="StackToVariableConverter"/>, or <see langword="null"/> for an
        ///     operation which does not directly access an argument or local. This is the canonical
        ///     storage-opcode decoder for variable-form passes.
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

        // Copies only opcode/encoded operand. It is suitable for Stack form and prefix logging, but
        // does not lower canonical Variables-form operands back to storage instructions.
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

    // Factories rather than shared instances: passes freely attach variable operands and prefixes
    // to returned Ops, so every use must receive a fresh object.
    private static class Ops
    {
        public static Op Nop => new(OpCodes.Nop);
        public static Op Ret => new(OpCodes.Ret);
        public static Op Pop => new(OpCodes.Pop);
    }

    /// <summary>
    ///     A node in the canonical lexical region-containment hierarchy built by MakeBasicBlocks.
    ///     This hierarchy is independent of normal CFG reachability and basic-block list order.
    /// </summary>
    internal class RegionNode
    {
        // Lexical-entry predicate only: true when this is the first child recorded by its parent.
        // It does not imply normal CFG reachability and is not the dominator-root predicate.
        public bool EntryPoint => parent == null || parent.entry == this;

        // Stable identity shared by Regions and BasicBlocks. IDs are unique after MakeBasicBlocks;
        // the synthetic root alone retains zero.
        public virtual string ID => $"#{id}";
        public int id = 0;

        // Canonical lexical parent after MakeBasicBlocks. A BasicBlock's parent is its immediate
        // containing Region; a non-root Region's parent is the surrounding Region.
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
    ///     canonical lexical containment hierarchy after MakeBasicBlocks. They do not form CFG
    ///     nodes. Their eventual emission positions are derived from entries and basic-block order,
    ///     not stored as a second independently mutable layout.
    /// </summary>
    internal class Region : RegionNode
    {
        public override string ID => parent == null ? "Root" : $"{harmonyBlock!.blockType} #{id}";

        // Canonical region kind and catch type after MakeBasicBlocks; null only for the synthetic
        // root. The marker's eventual output position is derived rather than stored here.
        public ExceptionBlock? harmonyBlock;

        // Canonical first lexical child after MakeBasicBlocks. It may be a nested Region, so callers
        // needing a block follow the recursive entry chain. Before aggressive reorder this child
        // need not be the earliest member of the Region in basicBlocks; afterward it is.
        public RegionNode? entry;

        // Canonical exception-group membership after MakeBasicBlocks. Null only for the synthetic
        // root; protected, filter, and handler Regions all point to their shared group.
        public ExceptionEntryGroup? exceptionEntryGroup;
    }

    /// <summary>
    ///     Exception entries which share a protected region, with their filter and handler regions
    ///     in CIL layout order.
    /// </summary>
    internal sealed class ExceptionEntryGroup(Region protectedRegion)
    {
        // Canonical protected body for this group. It is not repeated in associatedRegions.
        public Region ProtectedRegion { get; } = protectedRegion;

        // Canonical CIL layout order of the filters/handlers associated with ProtectedRegion. A
        // filtered entry contributes both its filter Region and its handler Region. This order is
        // independent of basicBlocks order until aggressive reorder reestablishes emission layout.
        public readonly List<Region> associatedRegions = [];

        // Returns the next Region in the group's required CIL layout chain. The argument must be
        // ProtectedRegion or a current associatedRegions member; null means the exception group ends.
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

    // Read-only collection views for passes and tests. Their elements remain mutable optimizer IR;
    // callers must respect the canonical-state and pass-precondition comments on the backing fields.
    internal IReadOnlyList<BasicBlock> BasicBlocks => basicBlocks;
    internal IReadOnlyList<Region> Regions => regions;
    internal IReadOnlyList<ExceptionEntryGroup> ExceptionEntryGroups => exceptionEntryGroups;
    internal IReadOnlyList<Variable> Variables => variables;
    internal IReadOnlyDictionary<int, Variable> ArgumentVariables => argumentVariables;
    internal IReadOnlyDictionary<int, Variable> LocalVariables => localVariables;

    // Canonical output only after Emit completes. Emit appends to this collection and therefore
    // requires it to be empty on entry; earlier passes must use the block/operation IR instead.
    public readonly InstructionList outputInstructions = [];

    // Canonical lexical hierarchy nodes after MakeBasicBlocks, including root exactly once. This
    // list records membership, not nesting or layout; parent/entry record nesting and the aggressive
    // reorder postcondition gives basicBlocks its eventual emission layout.
    private readonly List<Region> regions = [];

    // Canonical exception-group membership and handler/filter order after MakeBasicBlocks. The
    // normal CFG intentionally contains no implicit exceptional edges represented by these groups.
    private readonly List<ExceptionEntryGroup> exceptionEntryGroups = [];

    // Canonical normal-CFG node set after MakeBasicBlocks. Membership does not imply reachability:
    // CFG rewrites such as JumpThreading and MergeBlocks deliberately leave dead blocks for a later
    // removal pass. List order is initially input order and is non-canonical for analysis; only
    // AggressiveDeadCodeEliminationAndReorder establishes the final canonical emission order.
    private List<BasicBlock> basicBlocks = [];

    // Null means block dominance has not been computed or has been invalidated. A non-null tree is
    // canonical for the current block set, edge endpoints, and implicit exception-entry roots;
    // operation, IR-form, and block-order changes do not invalidate it. Edge mutations must use the
    // helpers below; block-set or implicit-root mutations must explicitly clear this cache.
    // Computation is explicit at pass entry, never hidden in a property.
    private DominatorTree? dominatorTree;

    // Canonical only in Variables form and empty in Stack form. variables owns every current
    // Variable. The two dictionaries are consistent subsets: each maps a physical slot index to
    // the one Argument/Local Variable for that slot, and every mapped value occurs in variables.
    // Logical values occur only in variables. nextVariableId is the next identity in this interval.
    private readonly List<Variable> variables = [];
    private readonly Dictionary<int, Variable> argumentVariables = [];
    private readonly Dictionary<int, Variable> localVariables = [];
    private int nextVariableId;

    // Stable synthetic hierarchy root. It becomes canonical when MakeBasicBlocks adds it to regions
    // and sets its entry; it has no parent, Harmony marker, or exception-entry group.
    private readonly Region root = new();

    // Shared ID allocator for BasicBlock and non-root Region nodes. IDs are stable and never reused.
    private int nextBlockId = 1;

    // Guard checked by Optimize. The current constructor sets it after deriving signature state, so
    // it is true for every successfully constructed instance; the false value is presently only a
    // reserved/legacy state rather than an observable optimizer phase.
    private readonly bool valid = false;

    // Immutable input/context. inputInstructions is authoritative only until MakeBasicBlocks builds
    // the IR. parameterTypes includes the instance receiver at index zero when method.HasThis.
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

    // Meaningful once MakeBasicBlocks has created the IR. Defaults to Stack, changes to Variables
    // only after conversion completes, and changes back only after all variable state is discarded.
    // SSA construction must eventually add and set a distinct value rather than infer SSA from data.
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
    ///     Logs whichever IR interpretation <see cref="Form"/> makes canonical. Before final
    ///     reordering, blocks are shown in their current non-canonical list order with region paths.
    ///     With <paramref name="structuredLayout"/>, derived region boundaries are also shown, so
    ///     that mode requires the aggressive-reorder postconditions. Logging never mutates the IR;
    ///     a displayed nop for an empty block is only a placeholder.
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
                        foreach (var prefix in op.Prefixes)
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
    ///     Derives a structured emission layout without storing a second canonical layout list.
    ///     Preconditions: aggressive reorder has removed dead regions and ordered basicBlocks so
    ///     every Region begins with its recursive entry block and is contiguous, and each associated
    ///     filter/handler immediately follows the preceding Region in its exception-entry group.
    ///     The returned list interleaves canonical Region nodes with the existing BasicBlocks; it
    ///     does not change either hierarchy or block order.
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

    /// <summary>
    ///     Runs the complete pipeline exactly once on a newly constructed optimizer. It builds Stack
    ///     form, temporarily converts to regular Variables form for variable-aware optimization,
    ///     lowers back to Stack form, restores dead-code and CIL layout invariants, and emits the
    ///     canonical output instruction list.
    /// </summary>
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

    /// <summary>
    ///     Preconditions: Stack form with a complete CFG, empty edge assignments, and no block
    ///     unreachable from every lexical Region entry. Postconditions: regular Variables form;
    ///     operation operands/counts, entryStackVariables, the variable registry, and addressTaken
    ///     are canonical, every edge assignment list is empty, and CFG, regions, and block order are
    ///     unchanged. Any cached dominator tree remains valid. The normal pipeline's first
    ///     SimpleDeadCodeElimination establishes the reachability precondition.
    /// </summary>
    private void ConvertStackToVariables()
    {
        new StackToVariableConverter(this).ConvertStackToVariables();
    }

    /// <summary>
    ///     Preconditions: regular Variables form with empty edge assignments, valid operation stack
    ///     counts, and identical natural exit/target entry stacks on every edge. Postconditions:
    ///     executable Stack form; variable operands, entry stacks, registries, and counts are
    ///     cleared/non-canonical. CFG, regions, block order, and cached dominance are unchanged.
    /// </summary>
    private void ConvertVariablesToStack()
    {
        new VariableToStackConverter(this).ConvertVariablesToStack();
    }

    /// <summary>
    ///     Requires regular Variables form, empty edge assignments, unique ownership of every Op,
    ///     and every retained block to be reachable from GetDominatorRoots. In the normal pipeline,
    ///     SimpleDeadCodeElimination immediately after MergeBlocks removes ordinary stranded blocks
    ///     and restores unique Op ownership; dominator computation enforces its narrower root-
    ///     reachability requirement. This pass computes and caches dominators if unavailable. It
    ///     changes only operation and variable data: CFG, regions, block order, and dominance remain
    ///     valid, edge assignments remain empty, and addressTaken is canonical on return.
    /// </summary>
    internal void ConservativeConstantPropagation()
    {
        new ConservativeConstantPropagator(this).Propagate();
    }

    /// <summary>
    ///     Explicitly returns the cached dominance result or computes it if absent. Requires a
    ///     complete CFG in either IR form and every retained block to be reachable from at least one
    ///     root returned by <see cref="GetDominatorRoots"/>. Block order, operation ownership, and
    ///     SSA edge assignments do not affect block dominance. The result remains valid until a CFG
    ///     or implicit-entry mutation calls <see cref="InvalidateControlFlowAnalyses"/>.
    /// </summary>
    private DominatorTree ComputeDominatorTreeIfNeeded()
    {
        return dominatorTree ??= DominatorTree.Compute(basicBlocks, GetDominatorRoots());
    }

    /// <summary>
    ///     Returns the explicit entries used for normal-CFG dominance: the recursive method entry
    ///     plus every filter and handler entry whose exceptional predecessor is absent from the
    ///     edge graph. A protected-region entry is reached normally and is not an extra root. This
    ///     root set is intentionally narrower than the lexical-entry root set used by
    ///     SimpleDeadCodeElimination.
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

    // Requires a complete, acyclic Region entry chain. This is hierarchy data and does not inspect
    // basicBlocks order; aggressive reorder is not required.
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

    /// <summary>
    ///     Invalidates all cached facts derived from CFG topology or implicit entry roots. This
    ///     cannot revoke a DominatorTree reference already held by a worker; a pass must not mutate
    ///     the CFG and then continue using such a captured analysis.
    /// </summary>
    private void InvalidateControlFlowAnalyses()
    {
        dominatorTree = null;
    }

    /// <summary>
    ///     Emits the canonical output from Stack form without changing CFG or operation state.
    ///     Preconditions: aggressive reorder's structured-region ordering, InsertBranches' guarantee
    ///     that every remaining fallthrough targets the next physical block, bundled prefixes, empty
    ///     edge assignments, and an empty outputInstructions list. Empty blocks receive an emitted
    ///     nop because labels and exception markers require a physical CIL instruction. On return,
    ///     outputInstructions is canonical; BasicBlock.label may also have been assigned lazily.
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
                        .. bb.ops.SelectMany(op => op.Prefixes.Append(op)).Select(ConvertToCodeInstruction),
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

    // Stack-form/emission conversion. It is also safe for prefix Ops during Variables-form logging,
    // because prefixes have no canonical variable operands. Branch operands remain CFG edges in the
    // IR and are replaced with lazily allocated labels only in the returned CodeInstruction.
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
    ///     Converts inputInstructions into the initial Stack-form IR. Preconditions: a freshly
    ///     constructed optimizer with empty block, region, variable, edge-assignment, and output
    ///     state. Postconditions: canonical normal CFG and lexical-region hierarchy; labels in
    ///     branch operands are replaced by edges, unconditional branches are represented as default
    ///     continuations, prefixes are bundled with their operations, and variable-form fields are
    ///     empty/non-canonical. Blocks remain in input order, may include dead code, and are not yet
    ///     guaranteed to satisfy final CIL emission order. Dominance is unavailable.
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
        List<Op> prefixes = [];

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

            if (inst.opcode.OpCodeType == OpCodeType.Prefix)
                prefixes.Add(new(inst.opcode, inst.operand, []));
            else
            {
                curBlock.ops.Add(new(inst.opcode, inst.operand, prefixes));
                prefixes = [];
            }

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
                    Label label => new(op.Opcode, GetTarget(label), op.Prefixes),
                    Label[] labels => new(op.Opcode, labels.Select(GetTarget).ToArray(), op.Prefixes),
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
                BasicBlock target => new(finalOperation.Opcode, AddControlFlowEdge(block, target), finalOperation.Prefixes),
                BasicBlock[] targets => new(finalOperation.Opcode,
                    targets.Select(target => AddControlFlowEdge(block, target)).ToArray(), finalOperation.Prefixes),
                _ => finalOperation,
            };
        }

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

    // CFG mutation primitives. They preserve the bidirectional endpoint-list invariant and keep
    // cached analysis state coherent by invalidating it. MoveControlFlowEdgeSource additionally
    // transfers fallthrough classification; redirecting a target does not change whether the edge
    // is its source's default continuation.
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

    /// <summary>
    ///     May run after MakeBasicBlocks in Stack, regular Variables, or SSA Variables form. It
    ///     removes only zero-effect nop operations, so variable/edge invariants, CFG topology,
    ///     dominance, regions, and block order remain unchanged. Empty blocks may remain.
    /// </summary>
    internal void NopElimination()
    {
        foreach (var block in basicBlocks)
            block.ops.RemoveAll(i => i.Opcode == OpCodes.Nop);
    }

    /// <summary>
    ///     Requires the complete CFG produced by MakeBasicBlocks in Stack form or regular Variables
    ///     form; every edge assignment list must be empty. It removes a conditional branch when all
    ///     of the block's outgoing edges target its default continuation, replacing the condition
    ///     consumption with equivalent pops. In Variables form those pops retain canonical variable
    ///     operands and stack counts. Removed edges invalidate dominance; region membership and
    ///     block order are unchanged.
    /// </summary>
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

    /// <summary>
    ///     Requires a complete CFG in Stack form or regular Variables form after empty blocks have
    ///     acquired compatible entry/exit stack state; SSA edge assignments must be absent. It
    ///     redirects default edges through short chains of empty, non-entry blocks within one
    ///     Region. Redirected edges invalidate dominance and may leave skipped blocks unreachable, so
    ///     SimpleDeadCodeElimination must run afterward before dominator computation or any pass
    ///     requiring every listed block to be live. Block order and region data are unchanged.
    /// </summary>
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
    ///     Requires a complete CFG in Stack form or regular Variables form with empty edge
    ///     assignments. It merges a same-Region, non-entry successor having one incoming edge into
    ///     its predecessor. CFG edge changes invalidate dominance. The absorbed successor remains
    ///     in basicBlocks as an unreachable node and temporarily shares its Op instances with the
    ///     merged predecessor.
    ///     SimpleDeadCodeElimination must run before dominator computation or any pass which indexes
    ///     operations by identity; the normal pipeline runs it immediately next. Region data and
    ///     block order are otherwise unchanged.
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
    ///     May run in Stack, regular Variables, or SSA Variables form after MakeBasicBlocks. It
    ///     removes blocks unreachable through normal edges from every lexical Region entry, because
    ///     handler/filter predecessors are absent from the CFG. Protected-region entries are also
    ///     roots here even though they are ordinary-flow entries for dominance. It removes attached
    ///     edges and their assignments, invalidating dominance when the graph changes. Relative block
    ///     order, operation/variable form, and Region metadata are unchanged; unreachable Regions
    ///     and exception groups are retained until AggressiveDeadCodeEliminationAndReorder.
    ///     Postcondition: every retained block is reachable from some lexical entry, but not
    ///     necessarily from the narrower root set used by dominator computation.
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
    ///     May run in either IR form, but the normal pipeline runs it in Stack form after
    ///     BranchInversion and before InsertBranches. Preconditions: a complete, bidirectionally
    ///     consistent CFG and a valid Region/exception-group hierarchy; no prior block ordering is
    ///     required. It removes unreachable blocks, Regions, and exception groups, invalidating
    ///     dominance if topology or implicit roots change, and establishes canonical emission order.
    ///     On return every Region begins with its recursive entry block and is contiguous, each
    ///     associated filter/handler immediately follows the preceding Region in its group, and a
    ///     stack-carrying backward edge has the forward predecessor required by CIL. Form-specific
    ///     operation and variable data, edge assignments, and CFG semantics are preserved.
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

    // Validates hierarchy membership and exception-group consistency before any reordering or
    // pruning occurs. It intentionally imposes no preexisting basicBlocks layout requirement.
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

    // Variables-form registry helpers used while StackToVariableConverter materializes canonical
    // operands. ArgumentVariables must already be initialized from parameterTypes. A previously
    // unseen local is created with unknown declared type; later stores never refine that metadata.
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

    // Adds one canonical Variables-form object to the owning registry. Callers adding an Argument
    // or Local must also add the same object to the corresponding index dictionary.
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

    /// <summary>
    ///     Requires Stack form with absent variable operands and empty edge assignments. The normal
    ///     pipeline runs it immediately after Variables-to-Stack lowering and before aggressive
    ///     reorder. It inverts brtrue or brfalse when doing so makes a single-predecessor target the
    ///     semantic fallthrough, helping the subsequent reorder choose a useful layout. Edge objects
    ///     and endpoints are unchanged, so CFG topology, dominance, Regions, and current block order
    ///     remain valid; only the explicit/default classification of two outgoing edges changes.
    /// </summary>
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
                    block.ops[^1] = new(OpCodes.Brtrue_S, fallthroughEdge, block.ops[^1].Prefixes);
                    block.fallthroughEdge = branchEdge;
                }

                if (finalInstruction.Opcode == OpCodes.Brtrue || finalInstruction.Opcode == OpCodes.Brtrue_S)
                {
                    block.ops[^1] = new(OpCodes.Brfalse_S, fallthroughEdge, block.ops[^1].Prefixes);
                    block.fallthroughEdge = branchEdge;
                }
            }
        }
    }

    /// <summary>
    ///     Requires Stack form, empty edge assignments, and the canonical emission order established
    ///     by AggressiveDeadCodeEliminationAndReorder; it runs immediately before Emit. It converts
    ///     every default continuation which does not target the next physical block into an explicit
    ///     branch. On return each remaining fallthroughEdge targets the following block, so Emit may
    ///     rely on physical fallthrough. CFG endpoints, dominance, Regions, and block order are
    ///     unchanged, but fallthroughEdge is cleared for each materialized branch.
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
            basicBlocks[i].ops.Add(new(OpCodes.Br_S, fallthroughEdge, []));
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
