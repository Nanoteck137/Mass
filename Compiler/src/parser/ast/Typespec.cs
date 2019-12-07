using System;
using System.Collections.Generic;
using System.Text;

namespace Mass.Compiler
{
    public abstract class Typespec
    {
        public SourceSpan Span { get; set; }
    }

    public class PtrTypespec : Typespec
    {
        public Typespec Type { get; private set; }

        public PtrTypespec(Typespec type)
        {
            this.Type = type;
        }
    }

    public class ArrayTypespec : Typespec
    {
        public Typespec Type { get; private set; }
        public Expr Size { get; private set; }

        public ArrayTypespec(Typespec type, Expr size)
        {
            this.Type = type;
            this.Size = size;
        }
    }

    public class IdentifierTypespec : Typespec
    {
        public IdentifierExpr[] Values { get; private set; }

        public IdentifierTypespec(IdentifierExpr[] values)
        {
            this.Values = values;
        }
    }
}