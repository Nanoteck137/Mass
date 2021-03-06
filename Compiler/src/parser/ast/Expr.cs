﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Mass.Compiler
{
    public abstract class Expr
    {
        public SourceSpan Span { get; set; }
        public Type ResolvedType { get; set; }
    }

    public class IntegerExpr : Expr
    {
        public ulong Value { get; private set; }

        public IntegerExpr(ulong value)
        {
            this.Value = value;
        }
    }

    public class FloatExpr : Expr
    {
        public double Value { get; private set; }
        public bool IsFloat { get; private set; }

        public FloatExpr(double value, bool isFloat)
        {
            this.Value = value;
            this.IsFloat = isFloat;
        }
    }

    public class IdentifierExpr : Expr
    {
        public string Value { get; private set; }

        public IdentifierExpr(string value)
        {
            this.Value = value;
        }
    }

    public class StringExpr : Expr
    {
        public string Value { get; private set; }

        public StringExpr(string value)
        {
            this.Value = value;
        }
    }

    public class CastExpr : Expr
    {
        public Expr Expr { get; private set; }
        public Typespec Type { get; private set; }

        public CastExpr(Expr expr, Typespec type)
        {
            this.Expr = expr;
            this.Type = type;
        }
    }

    public class BinaryOpExpr : Expr
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

    public class ModifyExpr : Expr
    {
        public TokenType Op { get; private set; }
        public bool Post { get; private set; }
        public Expr Expr { get; private set; }

        public ModifyExpr(TokenType op, bool post, Expr expr)
        {
            this.Op = op;
            this.Post = post;
            this.Expr = expr;
        }
    }

    public class UnaryExpr : Expr
    {
        public TokenType Op { get; private set; }
        public Expr Expr { get; private set; }

        public UnaryExpr(TokenType op, Expr expr)
        {
            this.Op = op;
            this.Expr = expr;
        }
    }

    public class CallExpr : Expr
    {
        public Expr Expr { get; private set; }
        public List<Expr> Arguments { get; private set; }

        public CallExpr(Expr expr, List<Expr> arguments)
        {
            this.Expr = expr;
            this.Arguments = arguments;
        }
    }

    public enum SpecialFunctionKind
    {
        Addr,
        Deref,
    };

    public class SpecialFunctionCallExpr : Expr
    {
        public SpecialFunctionKind Kind { get; private set; }
        public List<Expr> Arguments { get; private set; }

        public SpecialFunctionCallExpr(SpecialFunctionKind kind, List<Expr> arguments)
        {
            this.Kind = kind;
            this.Arguments = arguments;
        }
    }

    public class IndexExpr : Expr
    {
        public Expr Expr { get; private set; }
        public Expr Index { get; private set; }

        public IndexExpr(Expr expr, Expr index)
        {
            this.Expr = expr;
            this.Index = index;
        }
    }

    public class CompoundField
    {
        public Expr Init { get; private set; }
        public SourceSpan Span { get; set; }

        public CompoundField(Expr init)
        {
            this.Init = init;
        }
    }

    public class NameCompoundField : CompoundField
    {
        public IdentifierExpr Name { get; private set; }

        public NameCompoundField(Expr init, IdentifierExpr name)
            : base(init)
        {
            this.Name = name;
        }
    }

    public class IndexCompoundField : CompoundField
    {
        public Expr Index { get; private set; }

        public IndexCompoundField(Expr init, Expr index)
            : base(init)
        {
            this.Index = index;
        }
    }

    public class CompoundExpr : Expr
    {
        public Typespec Type { get; private set; }
        public List<CompoundField> Fields { get; private set; }

        public CompoundExpr(Typespec type, List<CompoundField> fields)
        {
            this.Type = type;
            this.Fields = fields;
        }
    }

    public class FieldExpr : Expr
    {
        public Expr Expr { get; private set; }
        public IdentifierExpr Name { get; private set; }

        public FieldExpr(Expr expr, IdentifierExpr name)
        {
            this.Expr = expr;
            this.Name = name;
        }
    }
}