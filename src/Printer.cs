using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

class Printer
{
    private static int indent = 0;

    public static void PrintNewline()
    {
        string identStr = "                                  ";
        Console.Write("\n{0}", identStr.Substring(0, indent * 2));
    }

    public static void PrintExpr(Expr expr)
    {
        if (expr is IntegerExpr integerExpr)
        {
            Console.Write("{0}", integerExpr.Value);
        }
        else if (expr is FloatExpr floatExpr)
        {
            Console.Write("{0}", floatExpr.Value.ToString(CultureInfo.InvariantCulture));
            if (floatExpr.IsFloat)
                Console.Write("f");
        }
        else if (expr is IdentifierExpr identExpr)
        {
            Console.Write("{0}", identExpr.Value);
        }
        else if (expr is StringExpr strExpr)
        {
            Console.Write("\"{0}\"", strExpr.Value);
        }
        else if (expr is BinaryOpExpr binaryOpExpr)
        {
            Console.Write("({0} ", binaryOpExpr.Op.ToString());
            PrintExpr(binaryOpExpr.Left);
            Console.Write(" ");
            PrintExpr(binaryOpExpr.Right);
            Console.Write(")");
        }
        else if (expr is CallExpr callExpr)
        {
            Console.Write("(");
            PrintExpr(callExpr.Expr);
            foreach (Expr arg in callExpr.Arguments)
            {
                Console.Write(" ");
                PrintExpr(arg);
            }
            Console.Write(")");
        }
        else if (expr is IndexExpr indexExpr)
        {
            Console.Write("(index ");
            PrintExpr(indexExpr.Expr);
            Console.Write(" ");
            PrintExpr(indexExpr.Index);
            Console.Write(")");
        }
        else
        {
            Debug.Assert(false);
        }

        /*if (expr is IdentifierExpr ident)
        {
            Console.Write("{0}", ident.Value);
        }
        else if (expr is IntegerExpr number)
        {
            Console.Write("{0}", number.Value);
        }
        else if (expr is StringExpr str)
        {
            Console.Write("\"{0}\"", str.Value);
        }
        else if (expr is BinaryOpExpr binary)
        {
            Console.Write("({0} ", binary.Op.ToString());
            PrintExpr(binary.Left);
            Console.Write(" ");
            PrintExpr(binary.Right);
            Console.Write(")");
        }
        else if (expr is CallExpr call)
        {
            Console.Write("(");
            PrintExpr(call.Expr);
            foreach (Expr arg in call.Arguments)
            {
                Console.Write(" ");
                PrintExpr(arg);
            }
            Console.Write(")");
        }
        else
        {
            Debug.Assert(false);
        }*/
    }

    /*public void PrintStmt(Stmt stmt)
    {
        Debug.Assert(stmt != null);

        if (stmt is ReturnStmt returnStmt)
        {
            Console.Write("(return ");
            PrintExpr(returnStmt.Value);
            Console.Write(")");
        }
        else if (stmt is ExprStmt exprStmt)
        {
            PrintExpr(exprStmt.Expr);
        }
        else if (stmt is DeclStmt declStmt)
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

        foreach (Stmt stmt in block.Stmts)
        {
            PrintNewline();
            PrintStmt(stmt);
        }

        indent--;
        Console.Write(")");
    }

    public void PrintDecl(Decl decl)
    {
        Debug.Assert(decl != null);

        if (decl is ConstDecl constDecl)
        {
            Console.Write("(const {0} ", constDecl.Name.Value);
            PrintTypespec(constDecl.Type);
            Console.Write(" ");
            PrintExpr(constDecl.Value);
            Console.Write(")");
        }
        else if (decl is VarDecl varDecl)
        {
            Console.Write("(var {0} ", varDecl.Name.Value);
            PrintTypespec(varDecl.Type);
            Console.Write(" ");
            PrintExpr(varDecl.Value);
            Console.Write(")");
        }
        else if (decl is FunctionDecl funcDecl)
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
                Console.Write("void");
            }

            indent++;
            PrintNewline();
            PrintStmtBlock(funcDecl.Body);
            indent--;

            Console.Write(")");
        }
        else if (decl is ExternalDecl externDecl)
        {
            Console.Write("(external {0} ", externDecl.Prototype.Name);
            Console.Write("(");
            foreach (FunctionParameter param in externDecl.Prototype.Parameters)
            {
                Console.Write(" {0} ", param.Name.Value);
                PrintTypespec(param.Type);
            }

            if (externDecl.Prototype.VarArgs)
            {
                Console.Write(" ...");
            }

            Console.Write(" ) ");

            if (externDecl.Prototype.ReturnType != null)
            {
                PrintTypespec(externDecl.Prototype.ReturnType);
            }
            else
            {
                Console.Write("void");
            }

            Console.Write(")");
        }
        else
        {
            Debug.Assert(false);
        }
    }*/

    public static void Test()
    {
        // Integer x
        // Float x
        // Identifier
        // String
        // BinaryOp x
        // Call
        // Index
        Expr expr = new BinaryOpExpr(new IntegerExpr(123), new FloatExpr(3.14, true), TokenType.PLUS);
        PrintExpr(expr);
    }
}
