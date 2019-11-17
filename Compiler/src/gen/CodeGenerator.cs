using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

abstract class CodeGenerator
{
    protected Resolver resolver;

    public CodeGenerator(Resolver resolver)
    {
        this.resolver = resolver;
    }

    public abstract void Generate();
}
