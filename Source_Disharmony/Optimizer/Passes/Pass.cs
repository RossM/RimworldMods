using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Disharmony.Optimizer.Passes
{
    internal abstract class Pass(Optimizer optimizer)
    {
        public Optimizer Optimizer { get; } = optimizer;
        public ControlFlowGraph ControlFlowGraph => Optimizer.cfg;

        public void Run()
        {
            RunInternal();

            ControlFlowGraph.Validate();
        }

        protected internal abstract void RunInternal();
    }
}
