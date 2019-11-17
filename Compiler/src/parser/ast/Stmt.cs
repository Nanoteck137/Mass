using System;
using System.Collections.Generic;
using System.Text;

namespace Mass.Compiler
{
    abstract class Stmt
    {
        public SourceSpan Span { get; set; }
    }

    class StmtBlock : Stmt
    {
        public List<Stmt> Stmts { get; private set; }

        public StmtBlock(List<Stmt> stmts)
        {
            this.Stmts = stmts;
        }
    }

    class ElseIf
    {
        public Expr Cond { get; private set; }
        public StmtBlock Block { get; private set; }

        public ElseIf(Expr cond, StmtBlock block)
        {
            this.Cond = cond;
            this.Block = block;
        }
    }

    class IfStmt : Stmt
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


    class InitStmt : Stmt
    {
        public IdentifierExpr Name { get; private set; }
        public Typespec Type { get; private set; }
        public Expr Value { get; private set; }

        public InitStmt(IdentifierExpr name, Typespec type, Expr value)
        {
            this.Name = name;
            this.Type = type;
            this.Value = value;
        }
    }

    class ForStmt : Stmt
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

    class WhileStmt : Stmt
    {
        public Expr Cond { get; private set; }
        public StmtBlock Block { get; private set; }

        public WhileStmt(Expr cond, StmtBlock block)
        {
            this.Cond = cond;
            this.Block = block;
        }
    }

    class DoWhileStmt : Stmt
    {
        public Expr Cond { get; private set; }
        public StmtBlock Block { get; private set; }

        public DoWhileStmt(Expr cond, StmtBlock block)
        {
            this.Cond = cond;
            this.Block = block;
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

    class ContinueStmt : Stmt { }

    class BreakStmt : Stmt { }

    class AssignStmt : Stmt
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

    class ExprStmt : Stmt
    {
        public Expr Expr { get; private set; }

        public ExprStmt(Expr expr)
        {
            this.Expr = expr;
        }
    }
}