using System;
using System.Collections.Generic;
using System.Text;

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

class IdentifierTypespec : Typespec
{
    public IdentifierExpr Value { get; private set; }

    public IdentifierTypespec(IdentifierExpr value)
    {
        this.Value = value;
    }
}