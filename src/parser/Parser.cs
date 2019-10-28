using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

/* TODO(patrik):
 *  Fix SourceSpan for binary operator parsing
 */

class Parser
{
    private Lexer lexer;

    public Parser(Lexer lexer)
    {
        this.lexer = lexer;
    }

    #region Expr Parsing
    private Expr ParseOperand()
    {
        switch (lexer.CurrentToken)
        {
            case TokenType.INTEGER:
            {
                IntegerExpr result = new IntegerExpr(lexer.CurrentInteger)
                {
                    Span = lexer.CurrentTokenSpan
                };
                lexer.NextToken();

                return result;
            }

            case TokenType.FLOAT:
            {
                FloatExpr result = new FloatExpr(lexer.CurrentFloat, lexer.TokenMod == TokenMod.FLOAT)
                {
                    Span = lexer.CurrentTokenSpan
                };
                lexer.NextToken();

                return result;
            }

            case TokenType.IDENTIFIER:
            {
                IdentifierExpr result = new IdentifierExpr(lexer.CurrentIdentifier)
                {
                    Span = lexer.CurrentTokenSpan
                };
                lexer.NextToken();

                return result;
            }

            case TokenType.STRING:
            {
                StringExpr result = new StringExpr(lexer.CurrentString)
                {
                    Span = lexer.CurrentTokenSpan
                };
                lexer.NextToken();

                return result;
            }

            case TokenType.OPEN_PAREN:
            {
                SourceSpan firstSpan = lexer.CurrentTokenSpan;
                lexer.NextToken();

                Expr result = ParseExpr();

                SourceSpan lastSpan = lexer.CurrentTokenSpan;
                lexer.ExpectToken(TokenType.CLOSE_PAREN);

                result.Span = SourceSpan.FromTo(firstSpan, lastSpan);

                return result;
            }

            default:
                //TODO: Error?!?!?!
                return null;
        }
    }

    private Expr ParseBase()
    {
        Expr expr = ParseOperand();

        while (lexer.MatchToken(TokenType.OPEN_PAREN) ||
                lexer.MatchToken(TokenType.OPEN_BRACKET))
        {
            if (lexer.MatchToken(TokenType.OPEN_PAREN))
            {
                SourceSpan firstSpan = lexer.CurrentTokenSpan;

                lexer.NextToken();

                List<Expr> arguments = new List<Expr>();

                if (!lexer.MatchToken(TokenType.CLOSE_PAREN))
                {
                    Expr arg = ParseExpr();
                    arguments.Add(arg);

                    while (!lexer.MatchToken(TokenType.CLOSE_PAREN))
                    {
                        lexer.ExpectToken(TokenType.COMMA);

                        arg = ParseExpr();

                        arguments.Add(arg);
                    }
                }

                SourceSpan lastSpan = lexer.CurrentTokenSpan;
                lexer.ExpectToken(TokenType.CLOSE_PAREN);

                expr = new CallExpr(expr, arguments)
                {
                    Span = SourceSpan.FromTo(firstSpan, lastSpan)
                };
            }
            else if (lexer.MatchToken(TokenType.OPEN_BRACKET))
            {
                SourceSpan firstSpan = lexer.CurrentTokenSpan;

                lexer.NextToken();

                Expr index = ParseExpr();

                SourceSpan lastSpan = lexer.CurrentTokenSpan;
                lexer.ExpectToken(TokenType.CLOSE_BRACKET);

                expr = new IndexExpr(expr, index)
                {
                    Span = SourceSpan.FromTo(firstSpan, lastSpan)
                };
            }
        }

        return expr;
    }

    private Expr ParseMul()
    {
        Expr left = ParseBase();

        while (lexer.MatchToken(TokenType.MULTIPLY) ||
                lexer.MatchToken(TokenType.DIVIDE) ||
                lexer.MatchToken(TokenType.MODULO))
        {
            TokenType op = lexer.CurrentToken;

            lexer.NextToken();

            Expr right = ParseBase();

            SourceSpan leftSpan = left.Span;
            left = new BinaryOpExpr(left, right, op)
            {
                Span = SourceSpan.FromTo(leftSpan, right.Span)
            };
        }

        return left;
    }

    private Expr ParseAdd()
    {
        Expr left = ParseMul();

        while (lexer.CurrentToken == TokenType.PLUS ||
                lexer.CurrentToken == TokenType.MINUS)
        {
            TokenType op = lexer.CurrentToken;

            lexer.NextToken();

            Expr right = ParseMul();

            SourceSpan leftSpan = left.Span;
            left = new BinaryOpExpr(left, right, op)
            {
                Span = SourceSpan.FromTo(leftSpan, right.Span)
            };
        }

        return left;
    }

    private Expr ParseExpr()
    {
        return ParseAdd();
    }
    #endregion

    #region Stmt Parsing
    private StmtBlock ParseStmtBlock()
    {
        // Fromat: { stmts* }

        SourceSpan firstSpan = lexer.CurrentTokenSpan;
        lexer.ExpectToken(TokenType.OPEN_BRACE);

        List<Stmt> stmts = new List<Stmt>();
        while (!lexer.MatchToken(TokenType.CLOSE_BRACE))
        {
            stmts.Add(ParseStmt());
        }

        SourceSpan lastSpan = lexer.CurrentTokenSpan;
        lexer.ExpectToken(TokenType.CLOSE_BRACE);

        StmtBlock result = new StmtBlock(stmts)
        {
            Span = SourceSpan.FromTo(firstSpan, lastSpan)
        };

        return result;
    }

    private Stmt ParseIfStmt()
    {
        SourceSpan firstSpan = lexer.CurrentTokenSpan;
        lexer.ExpectToken(TokenType.KEYWORD_IF);

        lexer.ExpectToken(TokenType.OPEN_PAREN);

        Expr condExpr = ParseExpr();

        lexer.ExpectToken(TokenType.CLOSE_PAREN);

        StmtBlock thenBlock = ParseStmtBlock();
        SourceSpan lastSpan = thenBlock.Span;

        List<ElseIf> elseIfs = new List<ElseIf>();
        SourceSpan elseStart = null;
        StmtBlock elseBlock = null;

        while (lexer.MatchToken(TokenType.KEYWORD_ELSE))
        {
            SourceSpan span = lexer.CurrentTokenSpan;
            lexer.NextToken();

            if (lexer.MatchToken(TokenType.KEYWORD_IF))
            {
                if (elseBlock != null)
                    lexer.Error("Else block needs to be at the bottom of the if stmt", SourceSpan.FromTo(elseStart, elseBlock.Span));
                lexer.NextToken();

                lexer.ExpectToken(TokenType.OPEN_PAREN);
                Expr expr = ParseExpr();
                lexer.ExpectToken(TokenType.CLOSE_PAREN);
                StmtBlock block = ParseStmtBlock();

                elseIfs.Add(new ElseIf(expr, block));
            }
            else
            {
                StmtBlock block = ParseStmtBlock();
                lastSpan = block.Span;

                if (elseBlock != null)
                    lexer.Error("Multiple elses", SourceSpan.FromTo(span, lastSpan));
                else
                {
                    elseBlock = block;
                    elseStart = span;
                }
            }
        }

        IfStmt result = new IfStmt(condExpr, thenBlock, elseIfs, elseBlock)
        {
            Span = SourceSpan.FromTo(firstSpan, lastSpan)
        };

        return result;
    }

    private Stmt ParseForStmt()
    {
        return null;
    }

    private Stmt ParseWhileStmt()
    {
        SourceSpan firstSpan = lexer.CurrentTokenSpan;
        lexer.ExpectToken(TokenType.KEYWORD_WHILE);

        lexer.ExpectToken(TokenType.OPEN_PAREN);

        Expr expr = ParseExpr();

        lexer.ExpectToken(TokenType.CLOSE_PAREN);

        StmtBlock block = ParseStmtBlock();

        SourceSpan lastSpan = block.Span;

        WhileStmt result = new WhileStmt(expr, block)
        {
            Span = SourceSpan.FromTo(firstSpan, lastSpan)
        };

        return result;
    }

    private Stmt ParseDoWhileStmt()
    {
        SourceSpan firstSpan = lexer.CurrentTokenSpan;
        lexer.ExpectToken(TokenType.KEYWORD_DO);

        StmtBlock block = ParseStmtBlock();

        lexer.ExpectToken(TokenType.KEYWORD_WHILE);
        lexer.ExpectToken(TokenType.OPEN_PAREN);

        Expr cond = ParseExpr();

        lexer.ExpectToken(TokenType.CLOSE_PAREN);

        SourceSpan lastSpan = lexer.CurrentTokenSpan;
        lexer.ExpectToken(TokenType.SEMICOLON);


        DoWhileStmt result = new DoWhileStmt(cond, block)
        {
            Span = SourceSpan.FromTo(firstSpan, lastSpan)
        };

        return result;
    }

    private Stmt ParseReturnStmt()
    {
        SourceSpan firstSpan = lexer.CurrentTokenSpan;
        lexer.ExpectToken(TokenType.KEYWORD_RET);

        Expr expr = ParseExpr();

        SourceSpan lastSpan = lexer.CurrentTokenSpan;
        lexer.ExpectToken(TokenType.SEMICOLON);

        ReturnStmt result = new ReturnStmt(expr)
        {
            Span = SourceSpan.FromTo(firstSpan, lastSpan)
        };

        return result;
    }

    private Stmt ParseContinueStmt()
    {
        SourceSpan firstSpan = lexer.CurrentTokenSpan;
        lexer.ExpectToken(TokenType.KEYWORD_CONTINUE);

        SourceSpan lastSpan = lexer.CurrentTokenSpan;
        lexer.ExpectToken(TokenType.SEMICOLON);

        ContinueStmt result = new ContinueStmt
        {
            Span = SourceSpan.FromTo(firstSpan, lastSpan)
        };

        return result;
    }

    private Stmt ParseBreakStmt()
    {
        SourceSpan firstSpan = lexer.CurrentTokenSpan;
        lexer.ExpectToken(TokenType.KEYWORD_BREAK);

        SourceSpan lastSpan = lexer.CurrentTokenSpan;
        lexer.ExpectToken(TokenType.SEMICOLON);

        BreakStmt result = new BreakStmt
        {
            Span = SourceSpan.FromTo(firstSpan, lastSpan)
        };

        return result;
    }

    private Stmt ParseStmt()
    {
        if (lexer.MatchToken(TokenType.KEYWORD_IF))
        {
            return ParseIfStmt();
        }
        else if (lexer.MatchToken(TokenType.KEYWORD_FOR))
        {
            return ParseForStmt(); // TODO
        }
        else if (lexer.MatchToken(TokenType.KEYWORD_WHILE))
        {
            return ParseWhileStmt();
        }
        else if (lexer.MatchToken(TokenType.KEYWORD_DO))
        {
            return ParseDoWhileStmt();
        }
        else if (lexer.MatchToken(TokenType.KEYWORD_RET))
        {
            return ParseReturnStmt();
        }
        else if (lexer.MatchToken(TokenType.KEYWORD_CONTINUE))
        {
            return ParseContinueStmt();
        }
        else if (lexer.MatchToken(TokenType.KEYWORD_BREAK))
        {
            return ParseBreakStmt();
        }
        else
        {
            Debug.Assert(false);
            return null;
        }
    }
    #endregion

    #region Typespec Parsing
    private Typespec ParseTypespecBase()
    {
        if (lexer.MatchToken(TokenType.IDENTIFIER))
        {
            IdentifierExpr identifier = new IdentifierExpr(lexer.CurrentIdentifier)
            {
                Span = lexer.CurrentTokenSpan
            };

            lexer.ExpectToken(TokenType.IDENTIFIER);

            Typespec result = new IdentifierTypespec(identifier)
            {
                Span = identifier.Span.Clone()
            };

            return result;
        }
        else
        {
            Debug.Assert(false);
        }

        return null;
    }

    private Typespec ParseTypespec()
    {
        Typespec type = ParseTypespecBase();

        while (lexer.MatchToken(TokenType.MULTIPLY) ||
            lexer.MatchToken(TokenType.OPEN_BRACKET))
        {
            if (lexer.MatchToken(TokenType.MULTIPLY))
            {
                type = new PtrTypespec(type)
                {
                    Span = lexer.CurrentTokenSpan
                };
                lexer.NextToken();
            }
            else if (lexer.MatchToken(TokenType.OPEN_BRACKET))
            {
                SourceSpan firstSpan = lexer.CurrentTokenSpan;
                lexer.NextToken();

                Expr size = null;
                if (!lexer.MatchToken(TokenType.CLOSE_BRACKET))
                {
                    size = ParseExpr();
                }

                SourceSpan lastSpan = lexer.CurrentTokenSpan;
                lexer.ExpectToken(TokenType.CLOSE_BRACKET);

                type = new ArrayTypespec(type, size)
                {
                    Span = SourceSpan.FromTo(firstSpan, lastSpan)
                };
            }
            else
            {
                Debug.Assert(false);
            }
        }

        return type;
    }
    #endregion

    #region Decl Parsing

    private Decl ParseVarDecl()
    {
        // Format: var test: s32 = 123;
        lexer.ExpectToken(TokenType.KEYWORD_VAR);

        string name = lexer.CurrentIdentifier;
        lexer.ExpectToken(TokenType.IDENTIFIER);

        lexer.ExpectToken(TokenType.COLON);

        Typespec type = ParseTypespec();
        Expr expr = null;

        if (lexer.MatchToken(TokenType.EQUAL))
        {
            lexer.NextToken();

            expr = ParseExpr();
        }

        lexer.ExpectToken(TokenType.SEMICOLON);

        VarDecl result = new VarDecl(name, type, expr);

        return result;
    }

    private Decl ParseConstDecl()
    {
        // const test: s32 = 123;
        lexer.ExpectToken(TokenType.KEYWORD_CONST);

        string name = lexer.CurrentIdentifier;
        lexer.ExpectToken(TokenType.IDENTIFIER);
        lexer.ExpectToken(TokenType.COLON);

        Typespec type = ParseTypespec();

        lexer.ExpectToken(TokenType.EQUAL);

        Expr expr = ParseExpr();

        lexer.ExpectToken(TokenType.SEMICOLON);

        ConstDecl result = new ConstDecl(name, type, expr);
        return result;
    }

    private FunctionParameter ParseFuncParam()
    {
        string paramName = lexer.CurrentIdentifier;
        lexer.ExpectToken(TokenType.IDENTIFIER);
        lexer.ExpectToken(TokenType.COLON);

        Typespec paramType = ParseTypespec();

        return new FunctionParameter(paramName, paramType);
    }

    private Decl ParseFuncDecl()
    {
        // Format: func test(x: s32, y: s32, ...) { stmt* }
        //         func test(x: s32, y: s32, ...) -> s32 { stmt* }
        //         func test(x: s32, y: s32, ...);
        //         func test(x: s32, y: s32, ...) -> s32;
        lexer.ExpectToken(TokenType.KEYWORD_FUNC);

        string name = lexer.CurrentIdentifier;
        lexer.ExpectToken(TokenType.IDENTIFIER);

        List<FunctionParameter> parameters = new List<FunctionParameter>();
        bool varArgs = false;
        SourceSpan varArgsSpan = null;

        lexer.ExpectToken(TokenType.OPEN_PAREN);

        if (!lexer.MatchToken(TokenType.CLOSE_PAREN))
        {
            parameters.Add(ParseFuncParam());

            while (lexer.MatchToken(TokenType.COMMA))
            {
                lexer.NextToken();
                if (lexer.MatchToken(TokenType.DOT3))
                {
                    if (varArgs)
                    {
                        lexer.Error("Multiple ellipsis in function decl", lexer.CurrentTokenSpan);
                    }

                    varArgsSpan = lexer.CurrentTokenSpan;
                    varArgs = true;

                    lexer.NextToken();
                }
                else
                {
                    if (varArgs)
                    {
                        lexer.Error("Ellipsis must be last parameter in function decl", varArgsSpan);
                    }
                    parameters.Add(ParseFuncParam());
                }
            }
        }

        lexer.ExpectToken(TokenType.CLOSE_PAREN);

        Typespec returnType = null;
        if (lexer.MatchToken(TokenType.ARROW))
        {
            lexer.NextToken();

            returnType = ParseTypespec();
        }

        StmtBlock block = null;
        if (lexer.MatchToken(TokenType.OPEN_BRACE))
        {
            block = ParseStmtBlock();
        }
        else
        {
            lexer.ExpectToken(TokenType.SEMICOLON);
        }

        FunctionDecl result = new FunctionDecl(name, parameters, returnType, varArgs, block);
        return result;
    }

    private Decl ParseStructDecl()
    {
        // Format: struct Test { x: s32; y: s32; }

        lexer.ExpectToken(TokenType.KEYWORD_STRUCT);

        string name = lexer.CurrentIdentifier;
        lexer.ExpectToken(TokenType.IDENTIFIER);

        lexer.ExpectToken(TokenType.OPEN_BRACE);

        List<StructItem> items = new List<StructItem>();
        while (!lexer.MatchToken(TokenType.CLOSE_BRACE))
        {
            string itemName = lexer.CurrentIdentifier;
            lexer.ExpectToken(TokenType.IDENTIFIER);
            lexer.ExpectToken(TokenType.COLON);
            Typespec itemType = ParseTypespec();
            lexer.ExpectToken(TokenType.SEMICOLON);

            items.Add(new StructItem(itemName, itemType));
        }

        lexer.ExpectToken(TokenType.CLOSE_BRACE);

        StructDecl result = new StructDecl(name, items);
        return result;
    }

    private Decl ParseDecl()
    {
        if (lexer.MatchToken(TokenType.KEYWORD_VAR))
        {
            return ParseVarDecl();
        }
        else if (lexer.MatchToken(TokenType.KEYWORD_CONST))
        {
            return ParseConstDecl();
        }
        else if (lexer.MatchToken(TokenType.KEYWORD_FUNC))
        {
            return ParseFuncDecl();
        }
        else if (lexer.MatchToken(TokenType.KEYWORD_STRUCT))
        {
            return ParseStructDecl();
        }
        else
        {
            Debug.Assert(false);
        }

        return null;
    }

    #endregion

    /*private VarDeclAST ParseVarDecl()
    {
        SourceSpan firstSpan = lexer.CurrentTokenSpan.Clone();

        lexer.ExpectToken(TokenType.KEYWORD_VAR);
        lexer.ExpectToken(TokenType.IDENTIFIER, false);

        IdentifierExpr name = new IdentifierExpr(lexer.CurrentIdentifier)
        {
            Span = lexer.CurrentTokenSpan.Clone()
        };
        lexer.NextToken();

        lexer.ExpectToken(TokenType.COLON);

        Typespec type = ParseType();

        lexer.ExpectToken(TokenType.EQUAL);

        Expr value = ParseExpr();

        SourceSpan lastSpan = lexer.CurrentTokenSpan.Clone();
        lexer.ExpectToken(TokenType.SEMICOLON);

        VarDeclAST result = new VarDeclAST(name, type, value)
        {
            Span = FirstLastSpan(firstSpan, lastSpan)
        };

        return result;
    }

    private ConstDeclAST ParseConstDecl()
    {
        SourceSpan firstSpan = lexer.CurrentTokenSpan.Clone();

        lexer.ExpectToken(TokenType.KEYWORD_CONST);
        lexer.ExpectToken(TokenType.IDENTIFIER, false);

        IdentifierExpr name = new IdentifierExpr(lexer.CurrentIdentifier)
        {
            Span = lexer.CurrentTokenSpan.Clone()
        };
        lexer.NextToken();

        lexer.ExpectToken(TokenType.COLON);

        Typespec type = ParseType();

        lexer.ExpectToken(TokenType.EQUAL);

        Expr value = ParseExpr();

        SourceSpan lastSpan = lexer.CurrentTokenSpan.Clone();
        lexer.ExpectToken(TokenType.SEMICOLON);

        ConstDeclAST result = new ConstDeclAST(name, type, value)
        {
            Span = FirstLastSpan(firstSpan, lastSpan)
        };

        return result;
    }

    private StmtAST ParseReturnStmt()
    {

    }

    private ExprStmtAST ParseExprStmt()
    {
        Expr expr = ParseExpr();

        SourceSpan lastSpan = lexer.CurrentTokenSpan.Clone();
        lexer.ExpectToken(TokenType.SEMICOLON);

        ExprStmtAST result = new ExprStmtAST(expr)
        {
            Span = FirstLastSpan(expr.Span, lastSpan)
        };

        return result;
    }

    private DeclStmtAST ParseDeclStmt()
    {
        DeclAST decl = ParseDecl();
        if (decl != null)
        {
            DeclStmtAST result = new DeclStmtAST(decl)
            {
                Span = decl.Span.Clone()
            };

            return result;
        }
        else
        {
            return null;
        }
    }

    private StmtBlock ParseStmtBlock()
    {
        SourceSpan firstSpan = lexer.CurrentTokenSpan.Clone();
        lexer.ExpectToken(TokenType.OPEN_BRACE);

        List<StmtAST> stmts = new List<StmtAST>();
        while (lexer.CurrentToken != TokenType.CLOSE_BRACE)
        {
            if (lexer.CurrentToken == TokenType.KEYWORD_RET)
            {
                stmts.Add(ParseReturnStmt());
            }
            else
            {
                // TODO(patrik): Maybe support decl parsing inside a decl
                if (lexer.CurrentToken == TokenType.KEYWORD_VAR)
                {
                    VarDeclAST decl = ParseVarDecl();
                    DeclStmtAST stmt = new DeclStmtAST(decl);
                    stmt.Span = decl.Span.Clone();

                    stmts.Add(stmt);
                }
                else
                {
                    stmts.Add(ParseExprStmt());
                }
            }
        }

        SourceSpan lastSpan = lexer.CurrentTokenSpan.Clone();
        lexer.ExpectToken(TokenType.CLOSE_BRACE);

        StmtBlock stmtBlock = new StmtBlock(stmts)
        {
            Span = FirstLastSpan(firstSpan, lastSpan)
        };

        return stmtBlock;
    }

    private void ParseFunctionParameter(List<FunctionParameter> parameters)
    {
        SourceSpan paramStart = lexer.CurrentTokenSpan.Clone();
        IdentifierExpr paramName = new IdentifierExpr(lexer.CurrentIdentifier)
        {
            Span = lexer.CurrentTokenSpan.Clone()
        };
        lexer.NextToken();

        lexer.ExpectToken(TokenType.COLON);

        Typespec type = ParseType();
        SourceSpan paramEnd = type.Span.Clone();

        bool redefined = false;
        foreach (var item in parameters)
        {
            if (item.Name.Value == paramName.Value)
            {
                SourceSpan span = new SourceSpan(paramStart.FromLineNumber, paramStart.FromColumnNumber, paramEnd.ToLineNumber, paramEnd.ToColumnNumber);
                lexer.Error(string.Format("Redefinition of parameter name '{0}'", paramName.Value), span);
                redefined = true;
            }
        }

        if (!redefined)
        {
            parameters.Add(new FunctionParameter(paramName, type));
        }
    }

    private FunctionPrototypeAST ParseFunctionPrototype()
    {
        SourceSpan firstSpan = lexer.CurrentTokenSpan.Clone();
        lexer.ExpectToken(TokenType.KEYWORD_FUNC);

        lexer.ExpectToken(TokenType.IDENTIFIER, false);
        IdentifierExpr name = new IdentifierExpr(lexer.CurrentIdentifier)
        {
            Span = lexer.CurrentTokenSpan.Clone()
        };
        lexer.NextToken();

        lexer.ExpectToken(TokenType.OPEN_PAREN);

        List<FunctionParameter> parameters = new List<FunctionParameter>();

        bool varArgs = false;

        if (lexer.CurrentToken == TokenType.IDENTIFIER)
        {
            ParseFunctionParameter(parameters);

            while (lexer.CurrentToken != TokenType.CLOSE_PAREN)
            {
                lexer.ExpectToken(TokenType.SEMIDOT);

                if (lexer.CurrentToken == TokenType.DOT3)
                {
                    varArgs = true;
                    lexer.NextToken();
                }
                else
                {
                    ParseFunctionParameter(parameters);
                }
            }
        }
        else if (lexer.CurrentToken == TokenType.DOT3)
        {
            varArgs = true;
            lexer.NextToken();
        }

        SourceSpan lastSpan = lexer.CurrentTokenSpan.Clone();
        lexer.ExpectToken(TokenType.CLOSE_PAREN);

        Typespec returnType = null;
        if (lexer.CurrentToken == TokenType.ARROW)
        {
            lexer.NextToken();

            returnType = ParseType();
            lastSpan = returnType.Span.Clone();
        }

        FunctionPrototypeAST result = new FunctionPrototypeAST(name, parameters, returnType, varArgs)
        {
            Span = FirstLastSpan(firstSpan, lastSpan)
        };

        return result;
    }

    private FunctionDeclAST ParseFunctionDecl()
    {
        SourceSpan firstSpan = lexer.CurrentTokenSpan.Clone();

        FunctionPrototypeAST prototype = ParseFunctionPrototype();

        StmtBlock stmtBlock = ParseStmtBlock();
        SourceSpan lastSpan = stmtBlock.Span;

        FunctionDeclAST result = new FunctionDeclAST(prototype, stmtBlock)
        {
            Span = FirstLastSpan(firstSpan, lastSpan)
        };

        return result;
    }

    private ExternalDeclAST ParseExternalDecl()
    {
        SourceSpan firstSpan = lexer.CurrentTokenSpan.Clone();
        //lexer.ExpectToken(TokenType.KEYWORD_EXTERNAL);
        Debug.Assert(false);

        FunctionPrototypeAST prototype = ParseFunctionPrototype();

        SourceSpan lastSpan = lexer.CurrentTokenSpan.Clone();
        lexer.ExpectToken(TokenType.SEMICOLON);

        ExternalDeclAST result = new ExternalDeclAST(prototype)
        {
            Span = FirstLastSpan(firstSpan, lastSpan)
        };

        return result;
    }

    private DeclAST ParseDecl()
    {
        Debug.Assert(false);

        if (lexer.CurrentToken == TokenType.KEYWORD_VAR)
        {
            return ParseVarDecl();
        }
        else if (lexer.CurrentToken == TokenType.KEYWORD_CONST)
        {
            return ParseConstDecl();
        }
        else if (lexer.CurrentToken == TokenType.KEYWORD_FUNC)
        {
            return ParseFunctionDecl();
        }
        else
        {
            return null;
        }
    }

    public List<DeclAST> Parse()
    {
        List<DeclAST> result = new List<DeclAST>();

        while (lexer.CurrentToken != TokenType.EOF)
        {
            DeclAST decl = ParseDecl();
            if (decl != null)
            {
                result.Add(decl);
            }
        }

        return result;
    }*/

    public static void Test()
    {
        Lexer lexer = new Lexer("Parser Test", "");
        Parser parser = new Parser(lexer);

        #region Exprs Testing
        lexer.Reset("4 + 2 * 8 + 3.14f");
        lexer.NextToken();
        Expr expr = parser.ParseExpr();
        Debug.Assert(expr is BinaryOpExpr);

        lexer.Reset("test(123, 321, 3.14f, \"Wooh\") + 123");
        lexer.NextToken();
        Expr expr2 = parser.ParseExpr();
        Debug.Assert(expr2 is BinaryOpExpr);

        lexer.Reset("test[0]");
        lexer.NextToken();
        Expr expr3 = parser.ParseExpr();
        Debug.Assert(expr3 is IndexExpr);

        lexer.Reset("test(321)[0][1]");
        lexer.NextToken();
        Expr expr4 = parser.ParseExpr();
        Debug.Assert(expr4 is IndexExpr);
        #endregion

        #region Stmts Testing
        lexer.Reset("ret 123;");
        lexer.NextToken();
        Stmt stmt = parser.ParseStmt();
        Debug.Assert(stmt is ReturnStmt);

        lexer.Reset("{ ret 123; ret 321; }");
        lexer.NextToken();
        StmtBlock block = parser.ParseStmtBlock();

        lexer.Reset("while(1) { ret 123; }");
        lexer.NextToken();
        Stmt stmt2 = parser.ParseStmt();
        Debug.Assert(stmt2 is WhileStmt);

        lexer.Reset("if(1) { continue; } else { break; }");
        lexer.NextToken();
        Stmt stmt3 = parser.ParseStmt();
        Debug.Assert(stmt3 is IfStmt);

        lexer.Reset("if(1) { continue; } else if(2) { ret 123; } else if(1) { continue; } else { break; }");
        lexer.NextToken();
        Stmt stmt4 = parser.ParseStmt();
        Debug.Assert(stmt4 is IfStmt);

        lexer.Reset("do { continue; } while(1);");
        lexer.NextToken();
        Stmt stmt5 = parser.ParseStmt();
        Debug.Assert(stmt5 is DoWhileStmt);
        #endregion

        #region Typespec Testing
        lexer.Reset("s32**");
        lexer.NextToken();
        Typespec typespec = parser.ParseTypespec();
        Debug.Assert(typespec is PtrTypespec);

        lexer.Reset("s32*[4]*");
        lexer.NextToken();
        Typespec typespec2 = parser.ParseTypespec();
        Debug.Assert(typespec2 is PtrTypespec);
        #endregion

        #region Decl Testing
        lexer.Reset("var test: s32 = 123;");
        lexer.NextToken();
        Decl decl = parser.ParseDecl();
        Debug.Assert(decl is VarDecl);
        Debug.Assert(lexer.MatchToken(TokenType.EOF));

        lexer.Reset("var test: s32;");
        lexer.NextToken();
        Decl decl2 = parser.ParseDecl();
        Debug.Assert(decl2 is VarDecl);
        Debug.Assert(lexer.MatchToken(TokenType.EOF));

        lexer.Reset("struct Test { x: s32; y: s32; }");
        lexer.NextToken();
        Decl decl3 = parser.ParseDecl();
        Debug.Assert(decl3 is StructDecl);
        Debug.Assert(lexer.MatchToken(TokenType.EOF));

        lexer.Reset("const test: s32 = 123;");
        lexer.NextToken();
        Decl decl4 = parser.ParseDecl();
        Debug.Assert(decl4 is ConstDecl);
        Debug.Assert(lexer.MatchToken(TokenType.EOF));

        lexer.Reset("func test() { ret 123; }");
        lexer.NextToken();
        Decl decl5 = parser.ParseDecl();
        Debug.Assert(decl5 is FunctionDecl);
        Debug.Assert(lexer.MatchToken(TokenType.EOF));

        lexer.Reset("func test();");
        lexer.NextToken();
        Decl decl6 = parser.ParseDecl();
        Debug.Assert(decl6 is FunctionDecl);
        Debug.Assert(lexer.MatchToken(TokenType.EOF));

        lexer.Reset("func test() -> s32 { ret 123; }");
        lexer.NextToken();
        Decl decl7 = parser.ParseDecl();
        Debug.Assert(decl7 is FunctionDecl);
        Debug.Assert(lexer.MatchToken(TokenType.EOF));

        lexer.Reset("func test() -> s32;");
        lexer.NextToken();
        Decl decl8 = parser.ParseDecl();
        Debug.Assert(decl8 is FunctionDecl);
        Debug.Assert(lexer.MatchToken(TokenType.EOF));

        lexer.Reset("func test(x: s32, y: s32, ...) { ret 123; }");
        lexer.NextToken();
        Decl decl9 = parser.ParseDecl();
        Debug.Assert(decl9 is FunctionDecl);
        Debug.Assert(lexer.MatchToken(TokenType.EOF));

        #endregion
    }
}
