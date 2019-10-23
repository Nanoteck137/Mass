using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

class Printer
{
    private int indent;

    public Printer()
    {
        this.indent = 0;
    }

    public void PrintNewline()
    {
        string identStr = "                                  ";
        Console.Write("\n{0}", identStr.Substring(0, indent * 2));
    }

    public void PrintExpr(ExprAST expr)
    {
        if (expr is IdentifierAST ident)
        {
            Console.Write("{0}", ident.Value);
        }
        else if (expr is NumberAST number)
        {
            Console.Write("{0}", number.Number);
        }
        else if (expr is StringAST str)
        {
            Console.Write("\"{0}\"", str.Value);
        }
        else if (expr is BinaryOpExprAST binary)
        {
            Console.Write("({0} ", binary.Op.ToString());
            PrintExpr(binary.Left);
            Console.Write(" ");
            PrintExpr(binary.Right);
            Console.Write(")");
        }
        else if (expr is CallExprAST call)
        {
            Console.Write("(");
            PrintExpr(call.Expr);
            foreach (ExprAST arg in call.Arguments)
            {
                Console.Write(" ");
                PrintExpr(arg);
            }
            Console.Write(")");
        }
        else
        {
            Debug.Assert(false);
        }
    }

    public void PrintStmt(StmtAST stmt)
    {
        Debug.Assert(stmt != null);

        if (stmt is ReturnStmtAST returnStmt)
        {
            Console.Write("(return ");
            PrintExpr(returnStmt.Value);
            Console.Write(")");
        }
        else if (stmt is ExprStmtAST exprStmt)
        {
            PrintExpr(exprStmt.Expr);
        }
        else if (stmt is DeclStmtAST declStmt)
        {
            PrintDecl(declStmt.Decl);
        }
        else
        {
            Debug.Assert(false);
        }
    }

    public void PrintTypespec(Typespec spec)
    {
        if (spec is IdentifierTypespec ident)
        {
            Console.Write("{0}", ident.Value.Value);
        }
        else if (spec is PtrTypespec ptr)
        {
            Console.Write("(ptr ");
            PrintTypespec(ptr.Type);
            Console.Write(")");
        }
        else
        {
            Debug.Assert(false);
        }
    }

    public void PrintStmtBlock(StmtBlock block)
    {
        Console.Write("(block");
        indent++;

        foreach (StmtAST stmt in block.Stmts)
        {
            PrintNewline();
            PrintStmt(stmt);
        }

        indent--;
        Console.Write(")");
    }

    public void PrintDecl(DeclAST decl)
    {
        Debug.Assert(decl != null);

        if (decl is ConstDeclAST constDecl)
        {
            Console.Write("(const {0} ", constDecl.Name.Value);
            PrintTypespec(constDecl.Type);
            Console.Write(" ");
            PrintExpr(constDecl.Value);
            Console.Write(")");
        }
        else if (decl is VarDeclAST varDecl)
        {
            Console.Write("(var {0} ", varDecl.Name.Value);
            PrintTypespec(varDecl.Type);
            Console.Write(" ");
            PrintExpr(varDecl.Value);
            Console.Write(")");
        }
        else if (decl is FunctionDeclAST funcDecl)
        {
            Console.Write("(func {0} ", funcDecl.Prototype.Name.Value);
            Console.Write("(");

            foreach (FunctionParameter param in funcDecl.Prototype.Parameters)
            {
                Console.Write(" {0} ", param.Name.Value);
                PrintTypespec(param.Type);
            }

            if (funcDecl.Prototype.VarArgs)
            {
                Console.Write(" ...");
            }

            Console.Write(" ) ");

            if (funcDecl.Prototype.ReturnType != null)
            {
                PrintTypespec(funcDecl.Prototype.ReturnType);
            }
            else
            {
                Console.Write("nil");
            }

            indent++;
            PrintNewline();
            PrintStmtBlock(funcDecl.Body);
            indent--;

            Console.Write(")");
        }
        else
        {
            Debug.Assert(false);
        }
    }

    public void Test()
    {
        ExprAST[] exprs = new ExprAST[]
        {
            new BinaryOpExprAST(new NumberAST(123), new NumberAST(321), Operation.ADD),
            new CallExprAST(new IdentifierAST("fact"), new List<ExprAST>() { new NumberAST(42) }),
        };

        foreach (ExprAST expr in exprs)
        {
            PrintExpr(expr);
            Console.WriteLine();
        }

        Console.WriteLine();

        StmtAST[] stmts = new StmtAST[]
        {
            new ReturnStmtAST(new NumberAST(32)),
            new ExprStmtAST(new CallExprAST(new IdentifierAST("printf"), new List<ExprAST>() { new NumberAST(123), new NumberAST(321) }))
        };

        Console.WriteLine();

        foreach (StmtAST stmt in stmts)
        {
            PrintStmt(stmt);
            Console.WriteLine();
        }

        Console.WriteLine();

        DeclAST[] decls = new DeclAST[] {
            new ConstDeclAST(new IdentifierAST("A"), new IdentifierTypespec(new IdentifierAST("i32")), new NumberAST(123)),
            new VarDeclAST(new IdentifierAST("test"), new IdentifierTypespec(new IdentifierAST("i32")), new NumberAST(4)),
        new FunctionDeclAST(
            new FunctionPrototypeAST(
                new IdentifierAST("add"),
                new List<FunctionParameter>() {
                        new FunctionParameter(
                            new IdentifierAST("a"),
                            new IdentifierTypespec(new IdentifierAST("i32"))),
                        new FunctionParameter(
                            new IdentifierAST("b"),
                            new IdentifierTypespec(new IdentifierAST("i32")))
                },
                new IdentifierTypespec(new IdentifierAST("i32")),
                true),
            new StmtBlock(new List<StmtAST>() {
                    new ReturnStmtAST(
                        new BinaryOpExprAST(
                            new IdentifierAST("a"),
                            new IdentifierAST("b"),
                            Operation.ADD))
            }))
        };

        foreach (DeclAST decl in decls)
        {
            PrintDecl(decl);
            Console.WriteLine();
        }
    }
}
