using System;
using System.Collections.Generic;
using System.Text;

abstract class Expr
{
    public SourceSpan Span { get; set; }
    public Type ResolvedType { get; set; }
}

class IntegerExpr : Expr
{
    public ulong Value { get; private set; }

    public IntegerExpr(ulong value)
    {
        this.Value = value;
    }
}

class FloatExpr : Expr
{
    public double Value { get; private set; }
    public bool IsFloat { get; private set; }

    public FloatExpr(double value, bool isFloat)
    {
        this.Value = value;
        this.IsFloat = isFloat;
    }
}

class IdentifierExpr : Expr
{
    public string Value { get; private set; }

    public IdentifierExpr(string value)
    {
        this.Value = value;
    }
}

class StringExpr : Expr
{
    public string Value { get; private set; }

    public StringExpr(string value)
    {
        this.Value = value;
    }
}

class BinaryOpExpr : Expr
{
    public Expr Left { get; private set; }
    public Expr Right { get; private set; }
    public TokenType Op { get; private set; }

    public BinaryOpExpr(Expr left, Expr right, TokenType op)
    {
        this.Left = left;
        this.Right = right;
        this.Op = op;
    }
}

class CallExpr : Expr
{
    public Expr Expr { get; private set; }
    public List<Expr> Arguments { get; private set; }

    public CallExpr(Expr expr, List<Expr> arguments)
    {
        this.Expr = expr;
        this.Arguments = arguments;
    }
}

class IndexExpr : Expr
{
    public Expr Expr { get; private set; }
    public Expr Index { get; private set; }

    public IndexExpr(Expr expr, Expr index)
    {
        this.Expr = expr;
        this.Index = index;
    }
}

class CompoundField
{
    public Expr Init { get; private set; }
    public SourceSpan Span { get; set; }

    public CompoundField(Expr init)
    {
        this.Init = init;
    }
}

class NameCompoundField : CompoundField
{
    public IdentifierExpr Name { get; private set; }

    public NameCompoundField(Expr init, IdentifierExpr name)
        : base(init)
    {
        this.Name = name;
    }
}

class IndexCompoundField : CompoundField
{
    public Expr Index { get; private set; }

    public IndexCompoundField(Expr init, Expr index)
        : base(init)
    {
        this.Index = index;
    }
}

class CompoundExpr : Expr
{
    public Typespec Type { get; private set; }
    public List<CompoundField> Fields { get; private set; }

    public CompoundExpr(Typespec type, List<CompoundField> fields)
    {
        this.Type = type;
        this.Fields = fields;
    }
}

class FieldExpr : Expr
{
    public Expr Expr { get; private set; }
    public IdentifierExpr Name { get; private set; }

    public FieldExpr(Expr expr, IdentifierExpr name)
    {
        this.Expr = expr;
        this.Name = name;
    }
}