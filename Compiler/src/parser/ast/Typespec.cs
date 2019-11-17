using System;
using System.Collections.Generic;
using System.Text;

namespace Mass.Compiler
{
    abstract class Typespec
    {
        public SourceSpan Span { get; set; }
    }

    class PtrTypespec : Typespec
    {
        public Typespec Type { get; private set; }

        public PtrTypespec(Typespec type)
        {
            this.Type = type;
        }
    }

    class ArrayTypespec : Typespec
    {
        public Typespec Type { get; private set; }
        public Expr Size { get; private set; }

        public ArrayTypespec(Typespec type, Expr size)
        {
            this.Type = type;
            this.Size = size;
        }
    }

    class IdentifierTypespec : Typespec
    {
        public IdentifierExpr Value { get; private set; }

        public IdentifierTypespec(IdentifierExpr value)
        {
            this.Value = value;
        }
    }
}