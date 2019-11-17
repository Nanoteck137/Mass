using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Mass.Compiler
{
    class Printer
    {
        private static int indent = 0;

        public static void PrintNewline()
        {
            string identStr = "                                  ";
            Console.Write("\n{0}", identStr.Substring(0, indent * 2));
        }

        public static void PrintCompoundField(CompoundField field)
        {
            if (field is NameCompoundField nameField)
            {
                Console.Write("(name ");
                PrintExpr(nameField.Name);
                Console.Write(" ");
                PrintExpr(nameField.Init);
                Console.Write(")");
            }
            else if (field is IndexCompoundField indexField)
            {
                Console.Write("(index ");
                PrintExpr(indexField.Index);
                Console.Write(" ");
                PrintExpr(indexField.Init);
                Console.Write(")");
            }
            else
            {
                Console.Write("(nil ");
                PrintExpr(field.Init);
                Console.Write(")");
            }
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
                Console.Write("\"{0}\"", Regex.Escape(strExpr.Value));
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
                Console.Write(" ");

                Console.Write("(");
                foreach (Expr arg in callExpr.Arguments)
                {
                    Console.Write(" ");
                    PrintExpr(arg);
                }
                Console.Write(")");

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
            else if (expr is CompoundExpr compoundExpr)
            {
                Console.Write("(compound");
                if (compoundExpr.Type != null)
                {
                    Console.Write(" ");
                    PrintTypespec(compoundExpr.Type);
                }

                foreach (CompoundField field in compoundExpr.Fields)
                {
                    Console.Write(" ");
                    PrintCompoundField(field);
                }

                Console.Write(")");
            }
            else
            {
                Debug.Assert(false);
            }
        }

        public static void PrintStmtBlock(StmtBlock block)
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

        public static void PrintStmt(Stmt stmt)
        {
            /*
            ForStmt
            */

            if (stmt is StmtBlock stmtBlock)
            {
                PrintStmtBlock(stmtBlock);
            }
            else if (stmt is IfStmt ifStmt)
            {
                Console.Write("(if ");
                PrintExpr(ifStmt.Cond);

                indent++;
                PrintNewline();

                PrintStmtBlock(ifStmt.ThenBlock);

                foreach (ElseIf elseIf in ifStmt.ElseIfs)
                {
                    PrintNewline();
                    Console.Write("elseif ");
                    indent++;
                    PrintExpr(elseIf.Cond);
                    PrintNewline();
                    PrintStmtBlock(elseIf.Block);
                    indent--;
                }

                if (ifStmt.ElseBlock != null)
                {
                    PrintNewline();
                    Console.Write("else ");
                    indent++;
                    PrintNewline();
                    PrintStmtBlock(ifStmt.ElseBlock);
                    indent--;
                }

                indent--;

                Console.Write(")");
            }
            else if (stmt is ForStmt forStmt)
            {
                Console.Write("(for ");

                PrintStmtBlock(forStmt.Block);

                Console.Write(")");
            }
            else if (stmt is WhileStmt whileStmt)
            {
                Console.Write("(while ");
                PrintExpr(whileStmt.Cond);

                indent++;
                PrintNewline();
                PrintStmtBlock(whileStmt.Block);
                indent--;

                Console.Write(")");
            }
            else if (stmt is DoWhileStmt doWhileStmt)
            {
                Console.Write("(do while ");
                PrintExpr(doWhileStmt.Cond);

                indent++;
                PrintNewline();
                PrintStmtBlock(doWhileStmt.Block);
                indent--;
            }
            else if (stmt is ReturnStmt returnStmt)
            {
                Console.Write("(return ");
                PrintExpr(returnStmt.Value);
                Console.Write(")");
            }
            else if (stmt is ContinueStmt)
            {
                Console.Write("(continue)");
            }
            else if (stmt is BreakStmt)
            {
                Console.Write("(break)");
            }
            else if (stmt is ExprStmt exprStmt)
            {
                PrintExpr(exprStmt.Expr);
            }
            /*else if (stmt is DeclStmt declStmt)
            {
                PrintDecl(declStmt.Decl);
            }*/
            else
            {
                Debug.Assert(false);
            }
        }

        public static void PrintTypespec(Typespec spec)
        {
            if (spec is PtrTypespec ptr)
            {
                Console.Write("(ptr ");
                PrintTypespec(ptr.Type);
                Console.Write(")");
            }
            else if (spec is ArrayTypespec array)
            {
                Console.Write("(array ");
                PrintTypespec(array.Type);
                Console.Write(" ");
                PrintExpr(array.Size);
                Console.Write(")");
            }
            else if (spec is IdentifierTypespec ident)
            {
                Console.Write("{0}", ident.Value.Value);
            }
            else
            {
                Debug.Assert(false);
            }
        }

        public static void PrintDecl(Decl decl)
        {
            if (decl is VarDecl varDecl)
            {
                Console.Write("(var ");
                Console.Write(varDecl.Name);
                Console.Write(" ");
                PrintTypespec(varDecl.Type);
                Console.Write(" ");
                PrintExpr(varDecl.Value);
                Console.Write(")");
            }
            else if (decl is ConstDecl constDecl)
            {
                Console.Write("(const ");
                Console.Write(constDecl.Name);
                Console.Write(" ");
                PrintTypespec(constDecl.Type);
                Console.Write(" ");
                PrintExpr(constDecl.Value);
                Console.Write(")");
            }
            else if (decl is FunctionDecl funcDecl)
            {
                Console.Write("(func {0} ", funcDecl.Name);
                Console.Write("(");

                foreach (FunctionParameter param in funcDecl.Parameters)
                {
                    Console.Write(" {0} ", param.Name);
                    PrintTypespec(param.Type);
                }

                if (funcDecl.VarArgs)
                {
                    Console.Write(" ...");
                }

                Console.Write(" ) ");

                if (funcDecl.ReturnType != null)
                {
                    PrintTypespec(funcDecl.ReturnType);
                }
                else
                {
                    Console.Write("void");
                }

                if (funcDecl.Body != null)
                {
                    indent++;
                    PrintNewline();
                    PrintStmtBlock(funcDecl.Body);
                    indent--;
                }

                Console.Write(")");
            }
            else if (decl is StructDecl structDecl)
            {
                Console.Write("(struct {0} ", structDecl.Name);
                indent++;

                foreach (StructItem item in structDecl.Items)
                {
                    PrintNewline();
                    Console.Write("(item {0} ", item.Name);
                    PrintTypespec(item.Type);
                    Console.Write(")");
                }

                indent--;
                Console.Write(")");
            }
            else
            {
                Debug.Assert(false);
            }
        }

        public static void PrintDeclList(List<Decl> decls)
        {
            foreach (Decl decl in decls)
            {
                PrintDecl(decl);
                Console.WriteLine();
                Console.WriteLine();
            }
        }

        public static void Test()
        {
            /*Expr[] exprs = new Expr[]
            {
                new BinaryOpExpr(new IntegerExpr(123), new FloatExpr(3.14, true), TokenType.PLUS),
                new CallExpr(
                    new IdentifierExpr("printf"),
                    new List<Expr>()
                    {
                        new StringExpr("Hello %s\n"),
                        new IndexExpr(
                            new IdentifierExpr("arr"),
                            new IntegerExpr(2)),
                    }
                ),
            };

            foreach (Expr expr in exprs)
            {
                PrintExpr(expr);
                Console.WriteLine();
            }*/

            /*
            ForStmt
            */

            Stmt[] stmts = new Stmt[]
            {
            new IfStmt(
                new IntegerExpr(1),
                new StmtBlock(
                    new List<Stmt>() {
                        new ReturnStmt(
                            new IntegerExpr(123)),
                        new ContinueStmt(),
                        new BreakStmt()
                    }),
                new List<ElseIf>() {
                    new ElseIf(
                        new IntegerExpr(1),
                        new StmtBlock(
                            new List<Stmt>() {
                                new BreakStmt()
                            }))
                },
                new StmtBlock(
                    new List<Stmt>() {
                        new ContinueStmt()
                    }
                )),

            new WhileStmt(
                new IntegerExpr(1),
                new StmtBlock(
                    new List<Stmt>()
                    {
                        new BreakStmt()
                    })),

            new DoWhileStmt(
                new IntegerExpr(1),
                new StmtBlock(
                    new List<Stmt>()
                    {
                        new BreakStmt()
                    })),

                /*new ForStmt(
                    new DeclStmt(
                        new VarDecl("i",
                        new IdentifierTypespec(
                            new IdentifierExpr("s32")),
                        new IntegerExpr(0))),
                    null,
                    null,
                    new StmtBlock(new List<Stmt>() {
                        new ContinueStmt()
                    }))*/

                //new DeclStmt(new VarDecl("a", new IdentifierTypespec(new IdentifierExpr("s32")), new IntegerExpr(321)))
            };

            foreach (Stmt stmt in stmts)
            {
                //PrintStmt(stmt);
                //Console.WriteLine();
            }

            Typespec[] types = new Typespec[]
            {
            new IdentifierTypespec(new IdentifierExpr("s32")),
            new PtrTypespec(new IdentifierTypespec(new IdentifierExpr("s32"))),
            new ArrayTypespec(new PtrTypespec(new IdentifierTypespec(new IdentifierExpr("s32"))), new IntegerExpr(4)),
            };

            foreach (Typespec typespec in types)
            {
                //PrintTypespec(typespec);
                //Console.WriteLine();
            }

            Typespec type = new IdentifierTypespec(new IdentifierExpr("s32"));
            Decl[] decls = new Decl[]
            {
            new VarDecl("a", new IdentifierTypespec(new IdentifierExpr("s32")), new IntegerExpr(123)),
            new ConstDecl("a", new IdentifierTypespec(new IdentifierExpr("s32")), new IntegerExpr(123)),
            new FunctionDecl(
                "add",
                new List<FunctionParameter>()
                {
                    new FunctionParameter("a", type),
                    new FunctionParameter("b", type)
                },
                type,
                false,
                new StmtBlock(new List<Stmt>() {
                    //new DeclStmt(new VarDecl("a", new IdentifierTypespec(new IdentifierExpr("s32")), new IntegerExpr(321)))
                })),

            new StructDecl("vec2", new List<StructItem>()
            {
                new StructItem("x", new IdentifierTypespec(new IdentifierExpr("f32"))),
                new StructItem("y", new IdentifierTypespec(new IdentifierExpr("f32"))),
            }, false),
            };

            foreach (Decl decl in decls)
            {
                //PrintDecl(decl);
                //Console.WriteLine();
            }

            Lexer lexer = new Lexer("Printer Test", "");
            Parser parser = new Parser(lexer);

            string[] code = new string[]
            {
            "var t: s32 = 123;",
            "var t: T = T { [1] = 1, test = 2, 3 };",
            };

            foreach (string c in code)
            {
                lexer.Reset(c);
                lexer.NextToken();

                Decl decl = parser.ParseDecl();
                PrintDecl(decl);
                Console.WriteLine();
            }
        }
    }
}