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
    private CompoundField ParseCompoundField()
    {
        SourceSpan firstSpan = lexer.CurrentTokenSpan;
        // Format 1: expr
        //        2: identifier = expr
        //        3: [expr] = expr

        // [expr] = expr
        if (lexer.MatchToken(TokenType.OPEN_BRACKET))
        {
            lexer.NextToken();

            Expr index = ParseExpr();

            lexer.ExpectToken(TokenType.CLOSE_BRACKET);
            lexer.ExpectToken(TokenType.EQUAL);

            Expr init = ParseExpr();

            IndexCompoundField result = new IndexCompoundField(init, index)
            {
                Span = SourceSpan.FromTo(firstSpan, init.Span)
            };

            return result;
        }
        else
        {
            // 1: expr
            // 2: identifier = expr
            Expr expr = ParseExpr();
            if (lexer.MatchToken(TokenType.EQUAL))
            {
                lexer.NextToken();

                if (!(expr is IdentifierExpr))
                {
                    // TODO(patrik): Change from fatal??
                    Log.Fatal("Named initializer in compound literal must be preceded by field name", expr.Span);
                }

                Expr init = ParseExpr();

                NameCompoundField result = new NameCompoundField(init, (IdentifierExpr)expr)
                {
                    Span = SourceSpan.FromTo(expr.Span, init.Span)
                };

                return result;
            }
            else
            {
                CompoundField result = new CompoundField(expr)
                {
                    Span = expr.Span
                };

                return result;
            }
        }
    }

    private CompoundExpr ParseCompound(Typespec type)
    {
        // Format 1: { expr, ... }
        //        2: { identifier = expr, ... }
        //        3: { [expr] = expr, ... }

        SourceSpan firstSpan = lexer.CurrentTokenSpan;
        lexer.ExpectToken(TokenType.OPEN_BRACE);

        List<CompoundField> fields = new List<CompoundField>();
        while (!lexer.MatchToken(TokenType.CLOSE_BRACE))
        {
            CompoundField field = ParseCompoundField();
            fields.Add(field);

            if (!lexer.MatchToken(TokenType.COMMA))
            {
                break;
            }
            else
            {
                lexer.NextToken();
            }
        }

        SourceSpan lastSpan = lexer.CurrentTokenSpan;
        lexer.ExpectToken(TokenType.CLOSE_BRACE);

        CompoundExpr result = new CompoundExpr(type, fields)
        {
            Span = SourceSpan.FromTo(firstSpan, lastSpan)
        };

        return result;
    }

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
                IdentifierExpr ident = new IdentifierExpr(lexer.CurrentIdentifier)
                {
                    Span = lexer.CurrentTokenSpan
                };
                lexer.NextToken();

                if (lexer.MatchToken(TokenType.OPEN_BRACE))
                {
                    Typespec typespec = new IdentifierTypespec(ident)
                    {
                        Span = ident.Span
                    };

                    return ParseCompound(typespec);
                }
                else
                {
                    return ident;
                }
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

            case TokenType.OPEN_BRACE:
            {
                return ParseCompound(null);
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
               lexer.MatchToken(TokenType.OPEN_BRACKET) ||
               lexer.MatchToken(TokenType.DOT) ||
               lexer.MatchToken(TokenType.INC) ||
               lexer.MatchToken(TokenType.DEC))
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

                if (expr is IdentifierExpr identExpr)
                {
                    if (identExpr.Value == "addr")
                    {
                        expr = new SpecialFunctionCallExpr(SpecialFunctionKind.Addr, arguments)
                        {
                            Span = SourceSpan.FromTo(firstSpan, lastSpan)
                        };
                    }
                    else if (identExpr.Value == "deref")
                    {
                        expr = new SpecialFunctionCallExpr(SpecialFunctionKind.Deref, arguments)
                        {
                            Span = SourceSpan.FromTo(firstSpan, lastSpan)
                        };
                    }
                    else
                    {
                        expr = new CallExpr(expr, arguments)
                        {
                            Span = SourceSpan.FromTo(firstSpan, lastSpan)
                        };
                    }
                }
                else
                {
                    expr = new CallExpr(expr, arguments)
                    {
                        Span = SourceSpan.FromTo(firstSpan, lastSpan)
                    };
                }
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
            else if (lexer.MatchToken(TokenType.DOT))
            {
                lexer.NextToken();

                IdentifierExpr name = new IdentifierExpr(lexer.CurrentIdentifier)
                {
                    Span = lexer.CurrentTokenSpan
                };
                lexer.ExpectToken(TokenType.IDENTIFIER);

                SourceSpan firstSpan = expr.Span;
                expr = new FieldExpr(expr, name)
                {
                    Span = SourceSpan.FromTo(firstSpan, name.Span)
                };
            }
            else
            {
                Debug.Assert(lexer.MatchToken(TokenType.INC) || lexer.MatchToken(TokenType.DEC));

                TokenType op = lexer.CurrentToken;
                lexer.NextToken();

                expr = new ModifyExpr(op, true, expr);
            }
        }

        return expr;
    }

    private Expr ParseUnary()
    {
        if (lexer.MatchToken(TokenType.INC) ||
            lexer.MatchToken(TokenType.DEC))
        {
            TokenType op = lexer.CurrentToken;
            lexer.NextToken();

            return new ModifyExpr(op, false, ParseBase());
        }
        else
        {
            return ParseBase();
        }
    }

    private Expr ParseMul()
    {
        Expr left = ParseUnary();

        while (lexer.MatchToken(TokenType.MULTIPLY) ||
                lexer.MatchToken(TokenType.DIVIDE) ||
                lexer.MatchToken(TokenType.MODULO))
        {
            TokenType op = lexer.CurrentToken;

            lexer.NextToken();

            Expr right = ParseUnary();

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

    private Expr ParseCompare()
    {
        Expr expr = ParseAdd();

        while (lexer.MatchToken(TokenType.EQUAL2) ||
                lexer.MatchToken(TokenType.NOT_EQUAL) ||
                lexer.MatchToken(TokenType.GREATER_THEN) ||
                lexer.MatchToken(TokenType.LESS_THEN) ||
                lexer.MatchToken(TokenType.GREATER_EQUALS) ||
                lexer.MatchToken(TokenType.LESS_EQUALS))
        {
            TokenType op = lexer.CurrentToken;
            lexer.NextToken();

            Expr right = ParseAdd();
            expr = new BinaryOpExpr(expr, right, op);
        }

        return expr;
    }

    private Expr ParseAnd()
    {
        Expr expr = ParseCompare();
        while (lexer.MatchToken(TokenType.AND2))
        {
            lexer.NextToken();
            Expr right = ParseCompare();
            expr = new BinaryOpExpr(expr, right, TokenType.AND2);
        }

        return expr;
    }

    private Expr ParseOr()
    {
        Expr expr = ParseAnd();
        while (lexer.MatchToken(TokenType.OR2))
        {
            lexer.NextToken();
            Expr right = ParseAnd();
            expr = new BinaryOpExpr(expr, right, TokenType.AND2);
        }

        return expr;
    }

    private Expr ParseExpr()
    {
        return ParseOr();
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
                    Log.Error("Else block needs to be at the bottom of the if stmt", SourceSpan.FromTo(elseStart, elseBlock.Span));
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
                    Log.Error("Multiple elses", SourceSpan.FromTo(span, lastSpan));
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

    private Stmt ParseSimpleStmt()
    {
        // Format 1: expr = expr
        //        2: expr += expr
        //        3: expr -= expr
        //        4: expr *= expr
        //        5: expr /= expr
        //        6: expr %= expr
        Stmt result;

        Expr expr = ParseExpr();
        if (lexer.MatchToken(TokenType.EQUAL) ||
            lexer.MatchToken(TokenType.PLUS_EQUALS) ||
            lexer.MatchToken(TokenType.MINUS_EQUALS) ||
            lexer.MatchToken(TokenType.MULTIPLY_EQUALS) ||
            lexer.MatchToken(TokenType.DIVIDE_EQUALS) ||
            lexer.MatchToken(TokenType.MODULO_EQUALS))
        {
            TokenType op = lexer.CurrentToken;
            lexer.NextToken();

            Expr right = ParseExpr();
            result = new AssignStmt(expr, right, op);
        }
        else
        {
            result = new ExprStmt(expr);
        }

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
            Decl decl = ParseDecl();
            if (decl == null)
            {
                SourceSpan firstSpan = lexer.CurrentTokenSpan;
                Stmt stmt = ParseSimpleStmt();
                SourceSpan lastSpan = lexer.CurrentTokenSpan;
                lexer.ExpectToken(TokenType.SEMICOLON);

                stmt.Span = SourceSpan.FromTo(firstSpan, lastSpan);
                return stmt;
            }

            if (decl is VarDecl)
            {
                return new DeclStmt(decl);
            }
            else
            {
                // TODO: Better message
                Log.Fatal("Var decls is only supported decls in decls", null);
                return null;
            }
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
                        Log.Error("Multiple ellipsis in function decl", lexer.CurrentTokenSpan);
                    }

                    varArgsSpan = lexer.CurrentTokenSpan;
                    varArgs = true;

                    lexer.NextToken();
                }
                else
                {
                    if (varArgs)
                    {
                        Log.Error("Ellipsis must be last parameter in function decl", varArgsSpan);
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

        bool isOpaque = false;
        List<StructItem> items = new List<StructItem>();
        if (lexer.MatchToken(TokenType.OPEN_BRACE))
        {
            lexer.NextToken();

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
        }
        else
        {
            lexer.ExpectToken(TokenType.SEMICOLON);
            isOpaque = true;
        }

        StructDecl result = new StructDecl(name, items, isOpaque);
        return result;
    }

    private List<DeclAttribute> ParseDeclAttributes()
    {
        List<DeclAttribute> result = new List<DeclAttribute>();

        if (lexer.MatchToken(TokenType.HASHTAG))
        {
            while (lexer.MatchToken(TokenType.HASHTAG))
            {
                lexer.NextToken();
                string name = lexer.CurrentIdentifier;
                lexer.ExpectToken(TokenType.IDENTIFIER);

                if (name == "external")
                {
                    result.Add(new ExternalDeclAttribute());
                }
                else if (name == "inline")
                {
                    result.Add(new InlineDeclAttribute());
                }
                else
                {
                    //TODO(patrik): Error or warning
                    Debug.Assert(false);
                }
            }
        }

        return result;
    }

    public Decl ParseDecl()
    {
        List<DeclAttribute> attributes = ParseDeclAttributes();

        if (lexer.MatchToken(TokenType.KEYWORD_VAR))
        {
            Decl decl = ParseVarDecl();
            decl.Attributes = attributes;
            return decl;
        }
        else if (lexer.MatchToken(TokenType.KEYWORD_CONST))
        {
            Decl decl = ParseConstDecl();
            decl.Attributes = attributes;
            return decl;
        }
        else if (lexer.MatchToken(TokenType.KEYWORD_FUNC))
        {
            Decl decl = ParseFuncDecl();
            decl.Attributes = attributes;
            return decl;
        }
        else if (lexer.MatchToken(TokenType.KEYWORD_STRUCT))
        {
            Decl decl = ParseStructDecl();
            decl.Attributes = attributes;
            return decl;
        }

        return null;
    }

    #endregion

    public List<Decl> Parse()
    {
        List<Decl> result = new List<Decl>();

        while (!lexer.MatchToken(TokenType.EOF))
        {
            result.Add(ParseDecl());
        }

        return result;
    }

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

        lexer.Reset("T { test = 1, [4] = 2 }");
        lexer.NextToken();
        Expr expr5 = parser.ParseExpr();
        Debug.Assert(expr5 is CompoundExpr);

        lexer.Reset("{ test = 1, [4] = 2 }");
        lexer.NextToken();
        Expr expr6 = parser.ParseExpr();
        Debug.Assert(expr6 is CompoundExpr);

        lexer.Reset("test.a");
        lexer.NextToken();
        Expr expr7 = parser.ParseExpr();
        Debug.Assert(expr7 is FieldExpr);

        lexer.Reset("123 >= 21 && 21 < 3");
        lexer.NextToken();
        Expr expr8 = parser.ParseExpr();
        Debug.Assert(expr8 is BinaryOpExpr);

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

        lexer.Reset("a = 2;");
        lexer.NextToken();
        Stmt stmt6 = parser.ParseStmt();
        Debug.Assert(stmt6 is AssignStmt);
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
