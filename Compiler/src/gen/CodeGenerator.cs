using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Mass.Compiler
{
    public abstract class CodeGenerator
    {
        protected List<Symbol> symbols;

        public CodeGenerator(List<Symbol> symbols)
        {
            this.symbols = symbols;
        }

        public abstract void Generate();
    }
}