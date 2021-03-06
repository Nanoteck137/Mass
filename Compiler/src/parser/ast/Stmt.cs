﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Mass.Compiler
{
    public abstract class Stmt
    {
        public SourceSpan Span { get; set; }
    }

    public class StmtBlock : Stmt
    {
        public List<Stmt> Stmts { get; private set; }

        public StmtBlock(List<Stmt> stmts)
        {
            this.Stmts = stmts;
        }
    }

    public class ElseIf
    {
        public Expr Cond { get; private set; }
        public StmtBlock Block { get; private set; }

        public ElseIf(Expr cond, StmtBlock block)
        {
            this.Cond = cond;
            this.Block = block;
        }
    }

    public class IfStmt : Stmt
    {
        public Expr Cond { get; private set; }
        public StmtBlock ThenBlock { get; private set; }
        public List<ElseIf> ElseIfs { get; private set; }
        public StmtBlock ElseBlock { get; private set; }

        public IfStmt(Expr cond, StmtBlock thenBlock, List<ElseIf> elseIfs, StmtBlock elseBlock)
        {
            this.Cond = cond;
            this.ThenBlock = thenBlock;
            this.ElseIfs = elseIfs;
            this.ElseBlock = elseBlock;
        }
    }


    public class InitStmt : Stmt
    {
        public IdentifierExpr Name { get; private set; }
        public Typespec Type { get; private set; }
        public Expr Value { get; private set; }

        public Type ResolvedType { get; set; }

        public InitStmt(IdentifierExpr name, Typespec type, Expr value)
        {
            this.Name = name;
            this.Type = type;
            this.Value = value;
        }
    }

    public class ForStmt : Stmt
    {
        public Stmt Init { get; private set; }
        public Expr Cond { get; private set; }
        public Stmt Next { get; private set; }
        public StmtBlock Block { get; private set; }

        public ForStmt(Stmt init, Expr cond, Stmt next, StmtBlock block)
        {
            this.Init = init;
            this.Cond = cond;
            this.Next = next;
            this.Block = block;
        }
    }

    public class WhileStmt : Stmt
    {
        public Expr Cond { get; private set; }
        public StmtBlock Block { get; private set; }
        public bool IsDoWhile { get; private set; }

        public WhileStmt(Expr cond, StmtBlock block, bool isDoWhile)
        {
            this.Cond = cond;
            this.Block = block;
            this.IsDoWhile = isDoWhile;
        }
    }

    class ReturnStmt : Stmt
    {
        public Expr Value { get; private set; }

        public ReturnStmt(Expr value)
        {
            this.Value = value;
        }
    }

    public class ContinueStmt : Stmt { }

    public class BreakStmt : Stmt { }

    public class AssignStmt : Stmt
    {
        public Expr Left { get; private set; }
        public Expr Right { get; private set; }
        public TokenType Op { get; private set; }

        public AssignStmt(Expr left, Expr right, TokenType op)
        {
            this.Left = left;
            this.Right = right;
            this.Op = op;
        }
    }

    public class ExprStmt : Stmt
    {
        public Expr Expr { get; private set; }

        public ExprStmt(Expr expr)
        {
            this.Expr = expr;
        }
    }
}