using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Mass.Compiler
{
    public class Parser
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
                            Typespec typespec = new IdentifierTypespec(new IdentifierExpr[] { ident })
                            {
                                Span = ident.Span
                            };

                            CompoundExpr result = ParseCompound(typespec);
                            result.Span = SourceSpan.FromTo(ident.Span, result.Span);
                            return result;
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
                    Debug.Assert(false);
                    return null;
            }
        }

        private Expr ParseBase()
        {
            Expr expr = ParseOperand();

            while (lexer.MatchToken(TokenType.OPEN_PAREN) ||
                   lexer.MatchToken(TokenType.OPEN_BRACKET) ||
                   lexer.MatchToken(TokenType.DOT) ||
                   lexer.MatchToken(TokenType.KEYWORD_AS) ||
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

                    expr = new FieldExpr(expr, name)
                    {
                        Span = SourceSpan.FromTo(expr.Span, name.Span)
                    };
                }
                else if (lexer.MatchToken(TokenType.KEYWORD_AS))
                {
                    lexer.NextToken();

                    Typespec type = ParseTypespec();

                    expr = new CastExpr(expr, type)
                    {
                        Span = SourceSpan.FromTo(expr.Span, type.Span)
                    };
                }
                else
                {
                    Debug.Assert(lexer.MatchToken(TokenType.INC) || lexer.MatchToken(TokenType.DEC));

                    TokenType op = lexer.CurrentToken;
                    SourceSpan lastSpan = lexer.CurrentTokenSpan;
                    lexer.NextToken();

                    expr = new ModifyExpr(op, true, expr)
                    {
                        Span = SourceSpan.FromTo(expr.Span, lastSpan)
                    };
                }
            }

            return expr;
        }

        private Expr ParseUnary()
        {
            if (lexer.MatchToken(TokenType.INC) ||
                lexer.MatchToken(TokenType.DEC) ||
                lexer.MatchToken(TokenType.MINUS) ||
                lexer.MatchToken(TokenType.NOT))
            {
                TokenType op = lexer.CurrentToken;
                SourceSpan firstSpan = lexer.CurrentTokenSpan;
                lexer.NextToken();

                Expr expr = ParseBase();

                if (op == TokenType.INC || op == TokenType.DEC)
                {
                    ModifyExpr result = new ModifyExpr(op, false, expr)
                    {
                        Span = SourceSpan.FromTo(firstSpan, expr.Span)
                    };
                    return result;
                }
                else
                {
                    UnaryExpr result = new UnaryExpr(op, expr)
                    {
                        Span = SourceSpan.FromTo(firstSpan, expr.Span)
                    };
                    return result;
                }

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

                left = new BinaryOpExpr(left, right, op)
                {
                    Span = SourceSpan.FromTo(left.Span, right.Span)
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

                left = new BinaryOpExpr(left, right, op)
                {
                    Span = SourceSpan.FromTo(left.Span, right.Span)
                };
            }

            return left;
        }

        private Expr ParseCompare()
        {
            Expr left = ParseAdd();

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
                left = new BinaryOpExpr(left, right, op)
                {
                    Span = SourceSpan.FromTo(left.Span, right.Span)
                };
            }

            return left;
        }

        private Expr ParseAnd()
        {
            Expr left = ParseCompare();
            while (lexer.MatchToken(TokenType.AND2))
            {
                lexer.NextToken();
                Expr right = ParseCompare();
                left = new BinaryOpExpr(left, right, TokenType.AND2)
                {
                    Span = SourceSpan.FromTo(left.Span, right.Span)
                };
            }

            return left;
        }

        private Expr ParseOr()
        {
            Expr left = ParseAnd();
            while (lexer.MatchToken(TokenType.OR2))
            {
                lexer.NextToken();
                Expr right = ParseAnd();
                left = new BinaryOpExpr(left, right, TokenType.OR2)
                {
                    Span = SourceSpan.FromTo(left.Span, right.Span)
                };
            }

            return left;
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
                    {
                        Log.Error("Multiple elses", SourceSpan.FromTo(span, lastSpan));
                    }
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

        private Stmt ParseInitStmt()
        {
            // Format 1: var i: s32 = 0
            //        2: var i: s32

            if (lexer.MatchToken(TokenType.KEYWORD_VAR))
            {
                SourceSpan firstSpan = lexer.CurrentTokenSpan;
                lexer.NextToken();

                IdentifierExpr identifier = new IdentifierExpr(lexer.CurrentIdentifier);
                lexer.ExpectToken(TokenType.IDENTIFIER);
                lexer.ExpectToken(TokenType.COLON);

                Typespec type = ParseTypespec();

                Expr value = null;
                SourceSpan lastSpan = null;
                if (lexer.MatchToken(TokenType.EQUAL))
                {
                    lexer.NextToken();

                    value = ParseExpr();
                    lastSpan = value.Span;
                }
                else
                {
                    lastSpan = type.Span;
                }

                InitStmt result = new InitStmt(identifier, type, value)
                {
                    Span = SourceSpan.FromTo(firstSpan, lastSpan)
                };
                return result;
            }

            return null;
        }

        private Stmt ParseForStmt()
        {
            // Format 1: for(init; cond; next) stmt_block

            lexer.ExpectToken(TokenType.KEYWORD_FOR);

            Stmt init = null;
            Expr cond = null;
            Stmt next = null;

            lexer.ExpectToken(TokenType.OPEN_PAREN);

            if (!lexer.MatchToken(TokenType.SEMICOLON))
            {
                init = ParseSimpleStmt();
                lexer.ExpectToken(TokenType.SEMICOLON);
            }

            if (!lexer.MatchToken(TokenType.SEMICOLON))
            {
                cond = ParseExpr();
                lexer.ExpectToken(TokenType.SEMICOLON);
            }

            if (!lexer.MatchToken(TokenType.CLOSE_PAREN))
            {
                next = ParseSimpleStmt();
            }

            lexer.ExpectToken(TokenType.CLOSE_PAREN);

            StmtBlock block = ParseStmtBlock();

            return new ForStmt(init, cond, next, block);
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

            WhileStmt result = new WhileStmt(expr, block, false)
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

            WhileStmt result = new WhileStmt(cond, block, true)
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
            // Format 1: init
            //        2: expr = expr
            //        3: expr += expr
            //        4: expr -= expr
            //        5: expr *= expr
            //        6: expr /= expr
            //        7: expr %= expr
            Stmt stmt = ParseInitStmt();

            if (stmt == null)
            {
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
                    stmt = new AssignStmt(expr, right, op)
                    {
                        Span = SourceSpan.FromTo(expr.Span, right.Span)
                    };
                }
                else
                {
                    stmt = new ExprStmt(expr)
                    {
                        Span = expr.Span
                    };
                }
            }

            return stmt;
        }

        private Stmt ParseStmt()
        {
            if (lexer.MatchToken(TokenType.KEYWORD_IF))
            {
                return ParseIfStmt();
            }
            else if (lexer.MatchToken(TokenType.KEYWORD_FOR))
            {
                return ParseForStmt();
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
                Stmt result = ParseSimpleStmt();
                SourceSpan lastSpan = lexer.CurrentTokenSpan;
                lexer.ExpectToken(TokenType.SEMICOLON);
                result.Span = SourceSpan.FromTo(result.Span, lastSpan);

                return result;
            }
        }
        #endregion

        #region Typespec Parsing
        private Typespec ParseTypespecBase()
        {
            if (lexer.MatchToken(TokenType.IDENTIFIER))
            {
                SourceSpan firstSpan = lexer.CurrentTokenSpan;

                List<IdentifierExpr> idents = new List<IdentifierExpr>();

                IdentifierExpr identifier = new IdentifierExpr(lexer.CurrentIdentifier)
                {
                    Span = lexer.CurrentTokenSpan
                };
                idents.Add(identifier);

                lexer.ExpectToken(TokenType.IDENTIFIER);

                while (lexer.MatchToken(TokenType.DOT))
                {
                    lexer.NextToken();

                    identifier = new IdentifierExpr(lexer.CurrentIdentifier)
                    {
                        Span = lexer.CurrentTokenSpan
                    };
                    idents.Add(identifier);
                    lexer.ExpectToken(TokenType.IDENTIFIER);
                }

                Typespec result = new IdentifierTypespec(idents.ToArray())
                {
                    Span = SourceSpan.FromTo(firstSpan, identifier.Span)
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
            SourceSpan firstSpan = lexer.CurrentTokenSpan;
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

            SourceSpan lastSpan = lexer.CurrentTokenSpan;
            lexer.ExpectToken(TokenType.SEMICOLON);

            VarDecl result = new VarDecl(name, type, expr)
            {
                Span = SourceSpan.FromTo(firstSpan, lastSpan)
            };

            return result;
        }

        private Decl ParseConstDecl()
        {
            // const test: s32 = 123;
            SourceSpan firstSpan = lexer.CurrentTokenSpan;
            lexer.ExpectToken(TokenType.KEYWORD_CONST);

            string name = lexer.CurrentIdentifier;
            lexer.ExpectToken(TokenType.IDENTIFIER);
            lexer.ExpectToken(TokenType.COLON);

            Typespec type = ParseTypespec();

            lexer.ExpectToken(TokenType.EQUAL);

            Expr expr = ParseExpr();

            SourceSpan lastSpan = lexer.CurrentTokenSpan;
            lexer.ExpectToken(TokenType.SEMICOLON);

            ConstDecl result = new ConstDecl(name, type, expr)
            {
                Span = SourceSpan.FromTo(firstSpan, lastSpan)
            };

            return result;
        }

        private FunctionParameter ParseFuncParam()
        {
            string paramName = lexer.CurrentIdentifier;
            SourceSpan firstSpan = lexer.CurrentTokenSpan;
            lexer.ExpectToken(TokenType.IDENTIFIER);
            lexer.ExpectToken(TokenType.COLON);

            Typespec paramType = ParseTypespec();

            FunctionParameter result = new FunctionParameter(paramName, paramType)
            {
                Span = SourceSpan.FromTo(firstSpan, paramType.Span)
            };
            return result;
        }

        private Decl ParseFuncDecl()
        {
            // Format: func test(x: s32, y: s32, ...) { stmt* }
            //         func test(x: s32, y: s32, ...) -> s32 { stmt* }
            //         func test(x: s32, y: s32, ...);
            //         func test(x: s32, y: s32, ...) -> s32;

            SourceSpan firstSpan = lexer.CurrentTokenSpan;
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
            SourceSpan lastSpan;
            if (lexer.MatchToken(TokenType.OPEN_BRACE))
            {
                block = ParseStmtBlock();
                lastSpan = block.Span;
            }
            else
            {
                lastSpan = lexer.CurrentTokenSpan;
                lexer.ExpectToken(TokenType.SEMICOLON);
            }

            FunctionDecl result = new FunctionDecl(name, parameters, returnType, varArgs, block)
            {
                Span = SourceSpan.FromTo(firstSpan, lastSpan)
            };
            return result;
        }

        private Decl ParseStructDecl()
        {
            // Format: struct Test { x: s32; y: s32; }
            SourceSpan firstSpan = lexer.CurrentTokenSpan;
            lexer.ExpectToken(TokenType.KEYWORD_STRUCT);

            string name = lexer.CurrentIdentifier;
            lexer.ExpectToken(TokenType.IDENTIFIER);

            bool isOpaque = false;
            List<StructItem> items = new List<StructItem>();
            SourceSpan lastSpan;
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

                lastSpan = lexer.CurrentTokenSpan;
                lexer.ExpectToken(TokenType.CLOSE_BRACE);
            }
            else
            {
                lastSpan = lexer.CurrentTokenSpan;
                lexer.ExpectToken(TokenType.SEMICOLON);
                isOpaque = true;
            }

            StructDecl result = new StructDecl(name, items, isOpaque)
            {
                Span = SourceSpan.FromTo(firstSpan, lastSpan)
            };
            return result;
        }

        private Decl ParseImportDecl()
        {
            // Format 1: import test
            //        2: import { } from test

            lexer.ExpectToken(TokenType.KEYWORD_IMPORT);

            string name = null;
            List<IdentifierExpr> symbols = new List<IdentifierExpr>();

            if (lexer.MatchToken(TokenType.IDENTIFIER))
            {
                name = lexer.CurrentIdentifier;
                lexer.NextToken();
            }
            else if (lexer.MatchToken(TokenType.OPEN_BRACE))
            {
                lexer.NextToken();

                if (lexer.MatchToken(TokenType.IDENTIFIER))
                {
                    IdentifierExpr sym = new IdentifierExpr(lexer.CurrentIdentifier)
                    {
                        Span = lexer.CurrentTokenSpan
                    };
                    symbols.Add(sym);
                    lexer.NextToken();

                    while (!lexer.MatchToken(TokenType.CLOSE_BRACE))
                    {
                        lexer.ExpectToken(TokenType.COMMA);

                        sym = new IdentifierExpr(lexer.CurrentIdentifier)
                        {
                            Span = lexer.CurrentTokenSpan
                        };
                        symbols.Add(sym);
                        lexer.ExpectToken(TokenType.IDENTIFIER);
                    }
                }

                lexer.ExpectToken(TokenType.CLOSE_BRACE);
                lexer.ExpectToken(TokenType.KEYWORD_FROM);

                name = lexer.CurrentIdentifier;
                lexer.ExpectToken(TokenType.IDENTIFIER);
            }
            else
            {
                Debug.Assert(false);
            }

            lexer.ExpectToken(TokenType.SEMICOLON);

            ImportDecl result = new ImportDecl(name, symbols);
            return result;
        }

        private Decl ParseUseDecl()
        {
            // Format 1: use namespace identifier ('.' identifier)*

            SourceSpan firstSpan = lexer.CurrentTokenSpan;

            lexer.ExpectToken(TokenType.KEYWORD_USE);
            lexer.ExpectToken(TokenType.KEYWORD_NAMESPACE);

            string namespaceName = lexer.CurrentIdentifier;
            lexer.ExpectToken(TokenType.IDENTIFIER);

            while (lexer.MatchToken(TokenType.DOT))
            {
                lexer.NextToken();

                namespaceName += $".{lexer.CurrentIdentifier}";
                lexer.ExpectToken(TokenType.IDENTIFIER);
            }

            lexer.ExpectToken(TokenType.SEMICOLON);

            SourceSpan lastSpan = lexer.CurrentTokenSpan;

            NamespaceDecl result = new NamespaceDecl(namespaceName)
            {
                Span = SourceSpan.FromTo(firstSpan, lastSpan)
            };

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
                    else if (name == "export")
                    {
                        result.Add(new ExportDeclAttribute());
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
            else if (lexer.MatchToken(TokenType.KEYWORD_IMPORT))
            {
                Decl decl = ParseImportDecl();
                decl.Attributes = attributes;
                return decl;
            }
            else if (lexer.MatchToken(TokenType.KEYWORD_USE))
            {
                Decl decl = ParseUseDecl();
                return decl;
            }
            else
            {
                Debug.Assert(false);
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
    }
}