using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

class Parser
{
    private Lexer lexer;

    public Parser(Lexer lexer)
    {
        this.lexer = lexer;
    }

    private Expr ParseOperand()
    {
        switch (lexer.CurrentToken)
        {
            case TokenType.INTEGER:
            {
                IntegerExpr result = new IntegerExpr(lexer.CurrentInteger);
                /*{
                    Span = lexer.CurrentTokenSpan.Clone()
                };*/
                lexer.NextToken();

                return result;
            }

            case TokenType.IDENTIFIER:
            {
                IdentifierExpr result = new IdentifierExpr(lexer.CurrentIdentifier);
                /*{
                    Span = lexer.CurrentTokenSpan.Clone()
                };*/
                lexer.NextToken();

                return result;
            }

            case TokenType.STRING:
            {
                StringExpr result = new StringExpr(lexer.CurrentString);
                /*{
                    Span = lexer.CurrentTokenSpan.Clone()
                };*/
                lexer.NextToken();

                return result;
            }

            case TokenType.OPEN_PAREN:
            {
                SourceSpan firstSpan = lexer.CurrentTokenSpan.Clone();
                lexer.NextToken();

                Expr result = ParseExpr();

                SourceSpan lastSpan = lexer.CurrentTokenSpan.Clone();
                lexer.ExpectToken(TokenType.CLOSE_PAREN);

                //result.Span = FirstLastSpan(firstSpan, lastSpan);

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

        if (lexer.CurrentToken == TokenType.OPEN_PAREN)
        {
            lexer.NextToken();

            List<Expr> arguments = new List<Expr>();

            if (lexer.CurrentToken != TokenType.CLOSE_PAREN)
            {
                Expr arg = ParseExpr();
                arguments.Add(arg);

                while (lexer.CurrentToken != TokenType.CLOSE_PAREN)
                {
                    lexer.ExpectToken(TokenType.SEMIDOT);

                    arg = ParseExpr();

                    arguments.Add(arg);
                }
            }

            SourceSpan lastSpan = lexer.CurrentTokenSpan.Clone();
            lexer.ExpectToken(TokenType.CLOSE_PAREN);

            //SourceSpan firstSpan = expr.Span;
            expr = new CallExpr(expr, arguments);
            /*{
                Span = FirstLastSpan(firstSpan, lastSpan)
            };*/
        }

        return expr;
    }

    private Expr ParseMul()
    {
        Expr left = ParseBase();

        while (lexer.CurrentToken == TokenType.ASTERISK ||
                lexer.CurrentToken == TokenType.FORWORD_SLASH)
        {
            Operation op = Operation.MUL;

            if (lexer.CurrentToken == TokenType.ASTERISK)
            {
                op = Operation.MUL;
            }
            else if (lexer.CurrentToken == TokenType.FORWORD_SLASH)
            {
                op = Operation.DIV;
            }

            lexer.NextToken();

            Expr right = ParseBase();

            //SourceSpan leftSpan = left.Span;
            left = new BinaryOpExpr(left, right, op);
            /*{
                Span = FirstLastSpan(leftSpan, right.Span)
            };*/
        }

        return left;
    }

    private Expr ParseAdd()
    {
        Expr left = ParseMul();

        while (lexer.CurrentToken == TokenType.PLUS ||
                lexer.CurrentToken == TokenType.MINUS)
        {
            Operation op = Operation.ADD;

            if (lexer.CurrentToken == TokenType.PLUS)
            {
                op = Operation.ADD;
            }
            else if (lexer.CurrentToken == TokenType.MINUS)
            {
                op = Operation.SUB;
            }

            lexer.NextToken();

            Expr right = ParseMul();

            //SourceSpan leftSpan = left.Span;
            left = new BinaryOpExpr(left, right, op);
            /*{
                Span = FirstLastSpan(leftSpan, right.Span)
            };*/
        }

        return left;
    }

    private Expr ParseExpr()
    {
        return ParseAdd();
    }

    private SourceSpan FirstLastSpan(SourceSpan firstSpan, SourceSpan lastSpan)
    {
        return new SourceSpan(firstSpan.FromLineNumber, firstSpan.FromColumnNumber, lastSpan.ToLineNumber, lastSpan.ToColumnNumber);
    }

    private Typespec ParseType()
    {
        lexer.ExpectToken(TokenType.IDENTIFIER, false);
        IdentifierExpr identifier = new IdentifierExpr(lexer.CurrentIdentifier);
        /*{
            Span = lexer.CurrentTokenSpan.Clone()
        };*/
        lexer.NextToken();

        Typespec result = new IdentifierTypespec(identifier);
        /*{
            Span = identifier.Span.Clone()
        };*/

        if (lexer.CurrentToken == TokenType.ASTERISK)
        {
            while (lexer.CurrentToken == TokenType.ASTERISK)
            {
                result = new PtrTypespec(result);
                /*{
                    Span = lexer.CurrentTokenSpan.Clone()
                };*/
                lexer.NextToken();
            }
        }

        return result;
    }

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
        SourceSpan firstSpan = lexer.CurrentTokenSpan.Clone();
        lexer.ExpectToken(TokenType.KEYWORD_RET);

        Expr expr = ParseExpr();

        SourceSpan lastSpan = lexer.CurrentTokenSpan.Clone();
        lexer.ExpectToken(TokenType.SEMICOLON);

        ReturnStmtAST result = new ReturnStmtAST(expr)
        {
            Span = FirstLastSpan(firstSpan, lastSpan)
        };

        return result;
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

        lexer.Reset("4 + 2 * 8");
        lexer.NextToken();

        Expr expr = parser.ParseExpr();
        Debug.Assert(expr is BinaryOpExpr);
    }
}
