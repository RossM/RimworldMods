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
    /// <summary>Which interpretation of the shared block and operation data is currently valid.</summary>
    internal enum IrForm
    {
        /// <summary>Only the original CIL evaluation-stack representation is available.</summary>
        Stack,

        /// <summary>
        /// Operations and CFG edges also have explicit variables. The original stack schedule is
        /// retained for emission, so entering this form does not introduce runtime copies.
        /// </summary>
        Variables,
    }

    // These two types are used to track special cases in type analysis
    private struct UnknownType;

    private struct AnyType;

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

        // InputIndex identifies an output which aliases a popped input, as with both outputs of dup.
        // A negative index means that executing the instruction produces a new value.
        internal readonly record struct StackOutput(Type Type, int InputIndex = -1);

        // This is recorded during symbolic execution so later passes do not reinterpret CIL opcodes.
        internal readonly record struct VariableAccess(VariableKind VariableKind, int Index, VariableAccessKind Kind);

        // Canonical only in Stack form. SymbolicExecute records the complete evaluation-stack
        // transition here; ConvertStackToVariables consumes and then clears it. Inputs are ordered
        // from the deepest popped value to the top of the stack.
        internal readonly List<Type> inputTypes = [];
        internal readonly List<StackOutput> stackOutputs = [];
        internal readonly List<VariableAccess> variableAccesses = [];
        internal bool clearsStack;

        // Prefixes remain attached to the operation they govern so no later pass can separate them.
        public readonly List<Op> prefixes = [];

        // Canonical in Variables form and empty before conversion. These include both evaluation-
        // stack values and argument/local accesses.
        public readonly List<Variable> inputs = [];
        public readonly List<Variable> outputs = [];

        public bool IsLeave => Opcode == OpCodes.Leave_S || Opcode == OpCodes.Leave;
        public bool ClearsStack => Opcode == OpCodes.Ret || Opcode == OpCodes.Leave_S || Opcode == OpCodes.Leave;
        public bool IsUnconditionalBranch => Opcode == OpCodes.Br_S || Opcode == OpCodes.Br;
        public bool CanBranch => Opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch;

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

        public OpCode Opcode { get; } = opcode;
        public object? Operand { get; } = operand;
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
            _ => throw new ArgumentOutOfRangeException()
        };

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
    }

    private static class Ops
    {
        public static Op Nop => new(OpCodes.Nop);
        public static Op Ret => new(OpCodes.Ret);
        public static Op Pop => new(OpCodes.Pop);
    }

    internal class Block
    {
        public bool EntryPoint => parent == null || parent.entry == this;
        public virtual string ID => $"#{id}";
        public int id = 0;
        public Label? label;
        public readonly List<Block> successors = [];
        public readonly List<Block> predecessors = [];
        public Region? parent;

        // Canonical only in Stack form. DeduceTypes computes these facts; variable conversion
        // consumes and clears them so later analysis cannot accidentally use stale type state.
        public List<Type> entryLocals = [];
        public List<Type> entryStack = [];
        public List<Type> exitLocals = [];
        public List<Type> exitStack = [];

        /// <summary>
        ///     For BasicBlocks, has the next block in the flow of control. For Regions, has the next
        ///     exception region in the chain.
        /// </summary>
        public Block? next;

        public override string ToString() => ID;

        public bool HasAncestor(Region region)
        {
            for (Block? b = this; b != null; b = b.parent)
            {
                if (b == region)
                    return true;
            }

            return false;
        }
    }

    internal class Region : Block
    {
        public override string ID => parent == null ? "Root" : $"{harmonyBlock!.blockType} #{id}";
        public ExceptionBlock? harmonyBlock;
        public Block? entry;
        public int depth;
    }

    public readonly InstructionList output = [];
    private readonly List<Block> allBlocks = [];
    private List<BasicBlock> basicBlocks = [];

    // One canonical object represents each physical argument/local; logical stack values receive
    // distinct identities in variables as they are discovered.
    private readonly List<Variable> variables = [];
    private readonly Dictionary<int, Variable> argumentVariables = [];
    private readonly Dictionary<int, Variable> localVariables = [];
    private int nextVariableId;

    internal IReadOnlyList<Block> Blocks => allBlocks;
    internal IReadOnlyList<BasicBlock> BasicBlocks => basicBlocks;
    internal IrForm Form { get; private set; }
    internal IReadOnlyList<Variable> Variables => variables;
    internal IReadOnlyDictionary<int, Variable> ArgumentVariables => argumentVariables;
    internal IReadOnlyDictionary<int, Variable> LocalVariables => localVariables;
    private readonly Region root = new();
    private int nextBlockId = 1;
    private bool valid = false;
    private readonly MethodBase method;
    private readonly List<CodeInstruction> inputInstructions;
    private readonly ILGenerator generator;
    private readonly bool debug;
    private List<Type> parameterTypes;
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

    private void LogBlocks(string phase)
    {
        if (!debug)
            return;

        int codePos = 0;
        Stack<Region> regionStack = new();

        FileLog.LogBuffered($"### Optimizer {phase}: {method.FullDescription()}");

        foreach (var block in allBlocks)
        {
            while (regionStack.Count >= 1 && block.parent != regionStack.Peek())
            {
                if (regionStack.Peek().harmonyBlock != null && regionStack.Peek().next == null)
                    FileLog.LogILBlockEnd(codePos, new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
                regionStack.Pop();
            }

            FileLog.LogBuffered($"## Block:        {block.ID}");
            FileLog.LogBuffered($"## Predecessors: {string.Join(", ", block.predecessors.Select(b => b.ID))}");
            FileLog.LogBuffered($"## Successors:   {string.Join(", ", block.successors.Select(b => b.ID))}");
            if (block is { EntryPoint: true, parent: not null })
                FileLog.LogBuffered($"## Entry Point:  {block.parent.ID}");
            if (block is { entryLocals.Count: > 0 })
                FileLog.LogBuffered($"## Locals:       {string.Join(", ", block.entryLocals.Select(t => t.FullName))}");
            if (block is { entryStack.Count: > 0 })
                FileLog.LogBuffered($"## Stack:        {string.Join(", ", block.entryStack.Select(t => t.FullName))}");

            if (block.label is Label label)
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

                    if (Form == IrForm.Variables)
                    {
                        foreach (var edge in bb.outgoingEdges.Where(edge => edge.assignments.Count > 0))
                        {
                            string assignments = string.Join(", ", edge.assignments.Select(assignment =>
                                $"{assignment.Destination} = {assignment.Source}"));
                            FileLog.LogBuffered($"## Edge {edge.Source.ID} => {edge.Target.ID}: {assignments}");
                        }
                    }
                    break;
                }
            }

            if (block.next != null)
                FileLog.LogBuffered($"IL_{codePos:X4}: // fallthrough => {block.next.ID}");
        }

        while (regionStack.Count > 0)
        {
            if (regionStack.Peek().harmonyBlock != null)
                FileLog.LogILBlockEnd(codePos, new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            regionStack.Pop();
        }

        FileLog.LogBuffered("");
        FileLog.FlushBuffer();
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

        JumpThreading();
        LogBlocks(nameof(JumpThreading));

        SimpleDeadCodeElimination();
        LogBlocks(nameof(SimpleDeadCodeElimination));

        BranchElimination();
        LogBlocks(nameof(BranchElimination));

        MergeBlocks();
        LogBlocks(nameof(MergeBlocks));

        BranchInversion();
        LogBlocks(nameof(BranchInversion));

        AggressiveDeadCodeEliminationAndReorder();
        LogBlocks(nameof(AggressiveDeadCodeEliminationAndReorder));

        DeduceTypes();
        LogBlocks(nameof(DeduceTypes));

        ConvertStackToVariables();
        LogBlocks(nameof(ConvertStackToVariables));

        InsertBranches();
        LogBlocks(nameof(InsertBranches));

        Emit();
        LogInstructions("Output", output.instructions);

        return output.instructions;
    }

    internal void Emit()
    {
        Stack<Region> regionStack = new();
        List<ExceptionBlock> harmonyBlocks = [];
        List<Label> labels = [];

        foreach (var block in allBlocks)
        {
            while (regionStack.Count >= 1 && block.parent != regionStack.Peek())
            {
                if (regionStack.Peek().harmonyBlock != null && regionStack.Peek().next == null)
                    output.instructions[^1].blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
                regionStack.Pop();
            }

            if (block.label is Label label)
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
                    output.instructions.AddRange(instructions);
                    break;
                }
            }
        }

        while (regionStack.Count > 0)
        {
            if (regionStack.Peek().harmonyBlock != null)
                output.instructions[^1].blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
            regionStack.Pop();
        }
    }

    private CodeInstruction ConvertToCodeInstruction(Op i)
    {
        var codeInstruction = i.ToCodeInstruction();
        codeInstruction.operand = codeInstruction.operand switch
        {
            Block blockTarget => GetLabel(blockTarget),
            Block[] blocksTarget => blocksTarget.Select(GetLabel).ToArray(),
            _ => codeInstruction.operand,
        };

        return codeInstruction;

        Label GetLabel(Block block) => block.label ??= generator.DefineLabel();
    }

    /// <summary>
    ///     Generate basic blocks.
    /// </summary>
    internal void MakeBasicBlocks()
    {
        Dictionary<Label, BasicBlock> labelToBlock = [];

        Region exceptionRegion = root;
        allBlocks.Add(root);

        BasicBlock curBlock = new() { id = nextBlockId++, parent = exceptionRegion };
        allBlocks.Add(curBlock);
        exceptionRegion.entry ??= curBlock;

        foreach (var inst in inputInstructions)
        {
            if (inst.labels.Count > 0 || inst.blocks.Any(IsBlockStart))
            {
                foreach (var harmonyBlock in inst.blocks.Where(IsBlockStart))
                    EnterExceptionRegion(harmonyBlock);

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
                    exceptionRegion = exceptionRegion.parent!;

                NewBasicBlock();
            }
        }

        if (curBlock.ops.Count == 0)
            allBlocks.Remove(curBlock);

        basicBlocks = [.. allBlocks.OfType<BasicBlock>()];

        for (int i = 0; i < basicBlocks.Count - 1; i++)
        {
            if (CanFallThrough(basicBlocks[i]))
                basicBlocks[i].next = basicBlocks[i + 1];
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

            Block GetTarget(Label label) => labelToBlock[label];
        }

        // Convert block-final unconditional branches to fallthrough
        foreach (var block in basicBlocks)
        {
            if (block.ops.Count == 0)
                continue;

            if (block.ops[^1].IsUnconditionalBranch)
            {
                block.next = (BasicBlock?)block.ops[^1].Operand;
                block.ops.RemoveAt(block.ops.Count - 1);
            }
        }

        foreach (var block in basicBlocks)
        {
            if (block.next is not null)
                block.successors.Add(block.next);

            if (block.ops.Count == 0)
                continue;

            switch (block.ops[^1].Operand)
            {
                case Block label:
                {
                    block.successors.Add(label);
                    break;
                }
                case Block[] labels:
                {
                    block.successors.AddRange(labels);
                    break;
                }
            }
        }

        UpdatePredecessors();
        BundlePrefixes();

        return;

        void EnterExceptionRegion(ExceptionBlock harmonyBlock)
        {
            if (harmonyBlock.blockType == ExceptionBlockType.BeginExceptionBlock)
            {
                var newRegion = new Region
                {
                    id = nextBlockId++,
                    depth = exceptionRegion.depth + 1,
                    harmonyBlock = harmonyBlock,
                    parent = exceptionRegion,
                };
                allBlocks.Add(newRegion);
                exceptionRegion.entry ??= newRegion;
                exceptionRegion = newRegion;
            }
            else
            {
                var newRegion = new Region
                {
                    id = nextBlockId++,
                    depth = exceptionRegion.depth,
                    harmonyBlock = harmonyBlock,
                    parent = exceptionRegion.parent,
                };
                allBlocks.Add(newRegion);
                exceptionRegion.next = newRegion;
                exceptionRegion = newRegion;
            }
        }

        void NewBasicBlock()
        {
            if (curBlock.ops.Count == 0)
            {
                curBlock.parent = exceptionRegion;
            }
            else
            {
                BasicBlock newBlock = new() { id = nextBlockId++, parent = exceptionRegion };
                allBlocks.Add(newBlock);
                curBlock = newBlock;
            }

            exceptionRegion.entry ??= curBlock;
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

    internal void NopElimination()
    {
        foreach (var block in basicBlocks)
            block.ops.RemoveAll(i => i.Opcode == OpCodes.Nop);
    }

    internal void BranchElimination()
    {
        bool changed = false;

        foreach (var block in basicBlocks)
        {
            if (block.ops.Count == 0)
                continue;
            if (block.next is null)
                continue;
            if (block.successors.Any(s => s != block.next))
                continue;

            switch (block.ops[^1].Opcode)
            {
                // Brtrue, Brfalse
                case
                {
                    FlowControl: FlowControl.Cond_Branch, StackBehaviourPop: StackBehaviour.Popi, StackBehaviourPush: StackBehaviour.Push0,
                }:
                {
                    block.ops[^1] = Ops.Pop;
                    block.successors.Clear();
                    block.successors.Add(block.next);
                    changed = true;
                    break;
                }
                // Beq, Bge, Bgt, Ble, Blt, Bne_Un, Bge_Un, Bgt_Un, Ble_Un, Blt_Un
                case
                {
                    FlowControl: FlowControl.Cond_Branch, StackBehaviourPop: StackBehaviour.Pop1_pop1,
                    StackBehaviourPush: StackBehaviour.Push0,
                }:
                {
                    block.ops[^1] = Ops.Pop;
                    block.ops.Add(Ops.Pop);
                    block.successors.Clear();
                    block.successors.Add(block.next);
                    changed = true;
                    break;
                }
            }
        }

        if (changed)
            UpdatePredecessors();
    }

    internal void JumpThreading()
    {
        foreach (var block in basicBlocks)
        {
            Block? fallthroughBlock = block.next;
            if (fallthroughBlock == null)
                continue;
            int iterations = 0;
            while (fallthroughBlock is BasicBlock { ops.Count: 0, EntryPoint: false } bb &&
                   bb.next!.parent == bb.parent &&
                   iterations++ < 20)
                fallthroughBlock = bb.next;

            if (block.next != fallthroughBlock)
            {
                block.successors.Remove(block.next!);
                block.successors.Add(fallthroughBlock);
                block.next = fallthroughBlock;
            }
        }

        UpdatePredecessors();
    }

    internal void MergeBlocks()
    {
        for (int i = basicBlocks.Count - 1; i >= 0; i--)
        {
            var block = basicBlocks[i];
            if (block.successors is not [var successor])
                continue;
            if (block.ops.Count > 0 && block.ops[^1].CanBranch)
                continue;
            if (successor.predecessors.Count != 1 || successor.parent != block.parent ||
                successor is not BasicBlock { EntryPoint: false } bb)
                continue;
            block.ops.AddRange(bb.ops);
            block.next = bb.next;
            block.successors.Clear();
            block.successors.AddRange(bb.successors);
        }

        UpdatePredecessors();
    }

    internal void SimpleDeadCodeElimination()
    {
        Queue<Block> queue = new();
        HashSet<Block> liveBlocks = [];

        foreach (var block in allBlocks)
        {
            if (block.EntryPoint)
                queue.Enqueue(block);
        }

        while (queue.Count > 0)
        {
            var block = queue.Dequeue();
            if (!liveBlocks.Add(block))
                continue;
            foreach (var successor in block.successors)
                queue.Enqueue(successor);
        }

        allBlocks.RemoveAll(b => b is BasicBlock && !liveBlocks.Contains(b));
        basicBlocks = [.. allBlocks.OfType<BasicBlock>()];

        UpdatePredecessors();
    }

    internal void AggressiveDeadCodeEliminationAndReorder()
    {
        List<Block> outputBlocks = [root];
        HashSet<Block> visited = [];
        Stack<(Region region, LinkedList<Block> queue)> stack = [];
        List<Block> leavingBlocks = [];

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

            if (!visited.Add(block))
                continue;
            outputBlocks.Add(block);

            if (debug)
                FileLog.LogBuffered($"{"".PadLeft(stack.Count * 2)}- {block.ID}");

            if (block.next != null)
                queue.AddFirst(block.next);

            switch (block)
            {
                case Region r2:
                {
                    (region, queue) = (r2, []);
                    stack.Push((region, queue));
                    queue.AddLast(r2.entry!);
                    break;
                }
                case BasicBlock bb:
                {
                    foreach (var successor in bb.successors)
                    {
                        if (!successor.HasAncestor(region))
                            leavingBlocks.Add(successor);
                        else if (successor != bb.next)
                            queue.AddLast(successor);
                    }

                    break;
                }
                default: throw new InvalidOperationException();
            }
        }

        allBlocks.Clear();
        allBlocks.AddRange(outputBlocks);
        basicBlocks = [.. allBlocks.OfType<BasicBlock>()];
    }

    internal void DeduceTypes()
    {
        UniqueQueue<Block> worklist = [];
        foreach (var block in allBlocks)
            worklist.Enqueue(block);

        while (worklist.Count > 0)
        {
            var block = worklist.Dequeue();

            switch (block)
            {
                case Region region:
                {
                    region.exitLocals = region.entryLocals;
                    region.exitStack = region.entryStack;

                    if (region.harmonyBlock is { blockType: ExceptionBlockType.BeginCatchBlock })
                        region.exitStack = [region.harmonyBlock.catchType];

                    if (region.entry != null)
                        UpdateSuccessor(region, region.entry);
                    break;
                }
                case BasicBlock bb:
                {
                    bb.SymbolicExecute(parameterTypes, returnType);

                    foreach (var successor in block.successors)
                        UpdateSuccessor(block, successor);
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        return;

        void UpdateSuccessor(Block block, Block successor)
        {
            var entryLocals = CombineTypeLists(successor.entryLocals, block.exitLocals, true);

            List<Type> entryStack = successor.entryStack.Count == 0 && block.exitStack.Count > 0
                ? block.exitStack
                : CombineTypeLists(successor.entryStack, block.exitStack);

            if (entryLocals.SequenceEqual(successor.entryLocals) && entryStack.SequenceEqual(successor.entryStack))
                return;

            successor.entryLocals = entryLocals;
            successor.entryStack = entryStack;
            worklist.Enqueue(successor);
        }
    }

    internal void ConvertStackToVariables()
    {
        if (Form != IrForm.Stack)
            throw new InvalidOperationException($"Cannot convert {Form} form to variables");

        InitializeVariables();

        foreach (var block in basicBlocks)
        {
            block.entryStackVariables.Clear();
            for (int index = 0; index < block.entryStack.Count; index++)
                block.entryStackVariables.Add(NewVariable(VariableKind.EntryStackSlot, block.entryStack[index], index, block));
        }

        foreach (var block in basicBlocks)
            MaterializeBlockVariables(block);

        BuildVariableEdges();
        DiscardStackAnalysis();
        Form = IrForm.Variables;
    }

    private void DiscardStackAnalysis()
    {
        foreach (var block in allBlocks)
        {
            block.entryLocals.Clear();
            block.entryStack.Clear();
            block.exitLocals.Clear();
            block.exitStack.Clear();
        }

        foreach (var op in basicBlocks.SelectMany(block => block.ops))
        {
            op.inputTypes.Clear();
            op.stackOutputs.Clear();
            op.variableAccesses.Clear();
            op.clearsStack = false;
        }
    }

    private void InitializeVariables()
    {
        variables.Clear();
        argumentVariables.Clear();
        localVariables.Clear();
        nextVariableId = 0;

        for (int index = 0; index < parameterTypes.Count; index++)
            argumentVariables.Add(index, NewVariable(VariableKind.Argument, parameterTypes[index], index));

        MethodBody? methodBody = GetMethodBodyOrNull();
        if (methodBody != null)
        {
            foreach (var local in methodBody.LocalVariables)
            {
                localVariables.Add(local.LocalIndex,
                    NewVariable(VariableKind.Local, local.LocalType, local.LocalIndex, pinned: local.IsPinned));
            }
        }

        foreach (var op in basicBlocks.SelectMany(block => block.ops))
        {
            if (!ReferencesLocal(op) || op.Operand is not LocalBuilder localBuilder)
                continue;

            if (localVariables.TryGetValue(localBuilder.LocalIndex, out var local))
            {
                if (local.type != localBuilder.LocalType)
                    throw new InvalidOperationException($"Conflicting types for local #{localBuilder.LocalIndex}");
                local.localBuilder ??= localBuilder;
                local.pinned |= localBuilder.IsPinned;
            }
            else
            {
                localVariables.Add(localBuilder.LocalIndex,
                    NewVariable(VariableKind.Local, localBuilder.LocalType, localBuilder.LocalIndex,
                        localBuilder: localBuilder, pinned: localBuilder.IsPinned));
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

    private void MaterializeBlockVariables(BasicBlock block)
    {
        List<Variable> stack = [.. block.entryStackVariables];

        foreach (var op in block.ops)
        {
            op.inputs.Clear();
            op.outputs.Clear();

            int inputCount = op.inputTypes.Count;
            if (inputCount > stack.Count)
                throw new InvalidOperationException($"{op.Opcode} reads {inputCount} values from a stack of {stack.Count}");

            int firstInput = stack.Count - inputCount;
            op.inputs.AddRange(stack.Skip(firstInput));
            stack.RemoveRange(firstInput, inputCount);

            foreach (var output in op.stackOutputs)
            {
                Variable variable = output.InputIndex >= 0
                    ? op.inputs[output.InputIndex]
                    : NewVariable(VariableKind.Temporary, output.Type);
                op.outputs.Add(variable);
                stack.Add(variable);
            }

            if (op.clearsStack)
                stack.Clear();

            foreach (var access in op.variableAccesses)
            {
                Variable variable = access.VariableKind switch
                {
                    VariableKind.Argument => GetArgumentVariable(access.Index),
                    VariableKind.Local => GetLocalVariable(access.Index),
                    _ => throw new InvalidOperationException($"Invalid explicit variable kind {access.VariableKind}"),
                };

                switch (access.Kind)
                {
                    case Op.VariableAccessKind.Read:
                        op.inputs.Add(variable);
                        break;
                    case Op.VariableAccessKind.Write:
                        op.outputs.Add(variable);
                        break;
                    case Op.VariableAccessKind.Address:
                        variable.addressTaken = true;
                        op.inputs.Add(variable);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        block.exitStackVariables.Clear();
        block.exitStackVariables.AddRange(stack);
        if (block.exitStackVariables.Count != block.exitStack.Count)
            throw new InvalidOperationException($"Variable stack disagrees with type stack at the exit of {block.ID}");
    }

    private void BuildVariableEdges()
    {
        foreach (var block in basicBlocks)
        {
            block.incomingEdges.Clear();
            block.outgoingEdges.Clear();
        }

        foreach (var source in basicBlocks)
        {
            bool emittedFallThrough = false;
            foreach (Block successor in source.successors)
            {
                if (successor is not BasicBlock target)
                    throw new InvalidOperationException($"Basic block {source.ID} has non-basic successor {successor.ID}");

                bool isFallThrough = !emittedFallThrough && source.next == target;
                emittedFallThrough |= isFallThrough;
                var edge = new ControlFlowEdge(source, target,
                    isFallThrough ? ControlFlowEdgeKind.FallThrough : ControlFlowEdgeKind.Branch);

                if (source.exitStackVariables.Count != target.entryStackVariables.Count)
                {
                    throw new InvalidOperationException(
                        $"Stack depth mismatch on edge {source.ID} => {target.ID}: " +
                        $"{source.exitStackVariables.Count} != {target.entryStackVariables.Count}");
                }

                for (int index = 0; index < source.exitStackVariables.Count; index++)
                {
                    edge.assignments.Add(new VariableAssignment(
                        source.exitStackVariables[index], target.entryStackVariables[index]));
                }

                source.outgoingEdges.Add(edge);
                target.incomingEdges.Add(edge);
            }
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
        BasicBlock? block = null,
        LocalBuilder? localBuilder = null,
        bool pinned = false)
    {
        var variable = new Variable
        {
            id = nextVariableId++,
            kind = kind,
            type = type,
            index = index,
            block = block,
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
            if (block.next is null)
                continue;
            if (block.next.predecessors.Count == 1)
                continue;

            var finalInstruction = block.ops[^1];
            if (finalInstruction.Opcode.FlowControl is FlowControl.Cond_Branch &&
                finalInstruction.Operand is Block { predecessors.Count: 1 } label)
            {
                if (finalInstruction.Opcode == OpCodes.Brfalse || finalInstruction.Opcode == OpCodes.Brfalse_S)
                {
                    block.ops[^1] = new(OpCodes.Brtrue_S, block.next);
                    block.next = label;
                }

                if (finalInstruction.Opcode == OpCodes.Brtrue || finalInstruction.Opcode == OpCodes.Brtrue_S)
                {
                    block.ops[^1] = new(OpCodes.Brfalse_S, block.next);
                    block.next = label;
                }
            }
        }
    }

    internal void InsertBranches()
    {
        for (int i = 0; i < basicBlocks.Count; i++)
        {
            Block? fallthroughBlock = basicBlocks[i].next;
            if (fallthroughBlock == null || i < basicBlocks.Count - 1 && fallthroughBlock == basicBlocks[i + 1])
                continue;
            basicBlocks[i].ops.Add(new(OpCodes.Br_S, fallthroughBlock));
            basicBlocks[i].next = null;
        }
    }

    /// <summary>
    ///     Update predecessor lists. Should be called whenever control flow changes.
    /// </summary>
    private void UpdatePredecessors()
    {
        foreach (var block in basicBlocks)
            block.predecessors.Clear();

        foreach (var block in basicBlocks)
        foreach (var successor in block.successors)
            successor.predecessors.Add(block);
    }

    private static List<Type> GetBaseTypes(Type type)
    {
        if (type.IsValueType || type.IsByRef)
            return [type];
        List<Type> output = [];
        for (Type? ancestor = type; ancestor != null; ancestor = ancestor.BaseType)
            output.Add(ancestor);
        output.Reverse();
        return output;
    }

    private static Type CombineTypes(Type left, Type right)
    {
        if (left == typeof(UnknownType) || right == typeof(AnyType) || left == right)
            return right;
        if (right == typeof(UnknownType) || left == typeof(AnyType))
            return left;

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

    private static bool TryGetCommonInterface(Type left, Type right, [NotNullWhen(true)] out Type? value)
    {
        HashSet<Type> interfaces = [.. left.GetInterfaces().Intersect(right.GetInterfaces())];
        value = interfaces.FirstOrDefault(i => !interfaces.Any(i2 => i != i2 && i.IsAssignableFrom(i2)));
        return value != null;
    }


    private static List<Type> CombineTypeLists(List<Type> left, List<Type> right, bool padIfNeeded = false)
    {
        if (!padIfNeeded && left.Count != right.Count)
            throw new ArgumentException();

        while (left.Count < right.Count)
            left.Add(typeof(UnknownType));
        while (right.Count < left.Count)
            right.Add(typeof(UnknownType));

        List<Type> output = new List<Type>(left.Count);
        for (int i = 0; i < left.Count; i++)
            output.Add(CombineTypes(left[i], right[i]));
        return output;
    }
}
