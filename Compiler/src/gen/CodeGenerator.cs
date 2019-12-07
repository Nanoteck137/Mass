using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Mass.Compiler
{
    public abstract class CodeGenerator
    {
        public Package Package { get; private set; }

        public CodeGenerator(Package package)
        {
            this.Package = package;
        }

        public abstract void Generate();
    }
}