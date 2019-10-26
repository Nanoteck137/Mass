using System;
using System.Collections.Generic;
using System.Text;

abstract class Expr
{
    public SourceSpan Span { get; set; }
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

enum Operation
{
    ADD,
    SUB,
    MUL,
    DIV
}

class BinaryOpExpr : Expr
{
    public Expr Left { get; private set; }
    public Expr Right { get; private set; }
    public Operation Op { get; private set; }

    public BinaryOpExpr(Expr left, Expr right, Operation op)
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