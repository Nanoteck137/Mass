using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

abstract class NodeAST
{
    public SourceSpan Span { get; set; }
}
abstract class ExprAST : NodeAST
{
}

class NumberAST : ExprAST
{
    public ulong Number { get; private set; }

    public NumberAST(ulong number)
    {
        this.Number = number;
    }
}

class IdentifierAST : ExprAST
{
    public string Value { get; private set; }

    public IdentifierAST(string value)
    {
        this.Value = value;
    }
}

class StringAST : ExprAST
{
    public string Value { get; private set; }

    public StringAST(string value)
    {
        this.Value = value;
    }
}

enum Operation
{
    ADD,
    SUB,
    MUL,
    DIV
}

class BinaryOpExprAST : ExprAST
{
    public ExprAST Left { get; private set; }
    public ExprAST Right { get; private set; }
    public Operation Op { get; private set; }

    public BinaryOpExprAST(ExprAST left, ExprAST right, Operation op)
    {
        this.Left = left;
        this.Right = right;
        this.Op = op;
    }
}

class CallExprAST : ExprAST
{
    public ExprAST Expr { get; private set; }
    public List<ExprAST> Arguments { get; private set; }

    public CallExprAST(ExprAST expr, List<ExprAST> arguments)
    {
        this.Expr = expr;
        this.Arguments = arguments;
    }
}

abstract class DeclAST : NodeAST
{
    public IdentifierAST Name { get; protected set; }
}

abstract class Typespec : NodeAST
{
}

class PtrTypespec : Typespec
{
    public Typespec Type { get; private set; }

    public PtrTypespec(Typespec type)
    {
        this.Type = type;
    }
}

class IdentifierTypespec : Typespec
{
    public IdentifierAST Value { get; private set; }

    public IdentifierTypespec(IdentifierAST value)
    {
        this.Value = value;
    }
}

class VarDeclAST : DeclAST
{
    public Typespec Type { get; private set; }
    public ExprAST Value { get; private set; }

    public VarDeclAST(IdentifierAST name, Typespec type, ExprAST value)
    {
        this.Name = name;
        this.Type = type;
        this.Value = value;
    }
}

class FunctionParameter
{
    public IdentifierAST Name { get; private set; }
    public Typespec Type { get; private set; }

    public FunctionParameter(IdentifierAST name, Typespec type)
    {
        this.Name = name;
        this.Type = type;
    }
}

class FunctionPrototypeAST : NodeAST
{
    public IdentifierAST Name { get; private set; }
    public List<FunctionParameter> Parameters { get; private set; }
    public Typespec ReturnType { get; private set; }
    public bool VarArgs { get; private set; }

    public FunctionPrototypeAST(IdentifierAST name, List<FunctionParameter> parameters, Typespec returnType, bool varArgs)
    {
        this.Name = name;
        this.Parameters = parameters;
        this.ReturnType = returnType;
        this.VarArgs = varArgs;
    }
}

class FunctionDeclAST : DeclAST
{
    public FunctionPrototypeAST Prototype { get; private set; }
    public StmtBlock Body { get; private set; }

    public FunctionDeclAST(FunctionPrototypeAST prototype, StmtBlock body)
    {
        this.Prototype = prototype;
        this.Name = prototype.Name;
        this.Body = body;
    }
}

class ExternalDeclAST : DeclAST
{
    public FunctionPrototypeAST Prototype { get; private set; }

    public ExternalDeclAST(FunctionPrototypeAST prototype)
    {
        this.Prototype = prototype;
        this.Name = prototype.Name;
    }
}

class ConstDeclAST : DeclAST
{
    public Typespec Type { get; private set; }
    public ExprAST Value { get; private set; }

    public ConstDeclAST(IdentifierAST name, Typespec type, ExprAST value)
    {
        this.Name = name;
        this.Type = type;
        this.Value = value;
    }
}

abstract class StmtAST : NodeAST
{
}

class StmtBlock : StmtAST
{
    public List<StmtAST> Stmts { get; private set; }

    public StmtBlock(List<StmtAST> stmts)
    {
        this.Stmts = stmts;
    }
}

class ReturnStmtAST : StmtAST
{
    public ExprAST Value { get; private set; }

    public ReturnStmtAST(ExprAST value)
    {
        this.Value = value;
    }
}

class ExprStmtAST : StmtAST
{
    public ExprAST Expr { get; private set; }

    public ExprStmtAST(ExprAST expr)
    {
        this.Expr = expr;
    }
}

class DeclStmtAST : StmtAST
{
    public DeclAST Decl { get; private set; }

    public DeclStmtAST(DeclAST decl)
    {
        this.Decl = decl;
    }
}

class Parser
{
    private Lexer lexer;

    public Parser(Lexer lexer)
    {
        this.lexer = lexer;
    }

    private ExprAST ParseOperand()
    {
        switch (lexer.CurrentToken)
        {
            case TokenType.INTEGER:
            {
                NumberAST result = new NumberAST(lexer.CurrentNumber)
                {
                    Span = lexer.CurrentTokenSpan.Clone()
                };
                lexer.NextToken();

                return result;
            }

            case TokenType.IDENTIFIER:
            {
                IdentifierAST result = new IdentifierAST(lexer.CurrentIdentifier)
                {
                    Span = lexer.CurrentTokenSpan.Clone()
                };
                lexer.NextToken();

                return result;
            }

            case TokenType.STRING:
            {
                StringAST result = new StringAST(lexer.CurrentString)
                {
                    Span = lexer.CurrentTokenSpan.Clone()
                };
                lexer.NextToken();

                return result;
            }

            case TokenType.OPEN_PAREN:
            {
                SourceSpan firstSpan = lexer.CurrentTokenSpan.Clone();
                lexer.NextToken();

                ExprAST result = ParseExpr();

                SourceSpan lastSpan = lexer.CurrentTokenSpan.Clone();
                lexer.ExpectToken(TokenType.CLOSE_PAREN);

                result.Span = FirstLastSpan(firstSpan, lastSpan);

                return result;
            }

            default:
                //TODO: Error?!?!?!
                return null;
        }
    }

    private ExprAST ParseBase()
    {
        ExprAST expr = ParseOperand();

        if (lexer.CurrentToken == TokenType.OPEN_PAREN)
        {
            lexer.NextToken();

            List<ExprAST> arguments = new List<ExprAST>();

            if (lexer.CurrentToken != TokenType.CLOSE_PAREN)
            {
                ExprAST arg = ParseExpr();
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

            SourceSpan firstSpan = expr.Span;
            expr = new CallExprAST(expr, arguments)
            {
                Span = FirstLastSpan(firstSpan, lastSpan)
            };
        }

        return expr;
    }

    private ExprAST ParseMul()
    {
        ExprAST left = ParseBase();

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

            ExprAST right = ParseBase();

            SourceSpan leftSpan = left.Span;
            left = new BinaryOpExprAST(left, right, op)
            {
                Span = FirstLastSpan(leftSpan, right.Span)
            };
        }

        return left;
    }

    private ExprAST ParseAdd()
    {
        ExprAST left = ParseMul();

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

            ExprAST right = ParseMul();

            SourceSpan leftSpan = left.Span;
            left = new BinaryOpExprAST(left, right, op)
            {
                Span = FirstLastSpan(leftSpan, right.Span)
            };
        }

        return left;
    }

    private ExprAST ParseExpr()
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
        IdentifierAST identifier = new IdentifierAST(lexer.CurrentIdentifier);
        identifier.Span = lexer.CurrentTokenSpan.Clone();
        lexer.NextToken();

        Typespec result = new IdentifierTypespec(identifier);
        result.Span = identifier.Span.Clone();

        if (lexer.CurrentToken == TokenType.ASTERISK)
        {
            while (lexer.CurrentToken == TokenType.ASTERISK)
            {
                result = new PtrTypespec(result)
                {
                    Span = lexer.CurrentTokenSpan.Clone()
                };
                lexer.NextToken();
            }
        }

        return result;
    }

    private VarDeclAST ParseVarDecl()
    {
        SourceSpan firstSpan = lexer.CurrentTokenSpan.Clone();

        lexer.ExpectToken(TokenType.KEYWORD_VAR);
        lexer.ExpectToken(TokenType.IDENTIFIER, false);

        IdentifierAST name = new IdentifierAST(lexer.CurrentIdentifier)
        {
            Span = lexer.CurrentTokenSpan.Clone()
        };
        lexer.NextToken();

        lexer.ExpectToken(TokenType.COLON);

        Typespec type = ParseType();

        lexer.ExpectToken(TokenType.EQUAL);

        ExprAST value = ParseExpr();

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

        IdentifierAST name = new IdentifierAST(lexer.CurrentIdentifier)
        {
            Span = lexer.CurrentTokenSpan.Clone()
        };
        lexer.NextToken();

        lexer.ExpectToken(TokenType.COLON);

        Typespec type = ParseType();

        lexer.ExpectToken(TokenType.EQUAL);

        ExprAST value = ParseExpr();

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

        ExprAST expr = ParseExpr();

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
        ExprAST expr = ParseExpr();

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
            DeclStmtAST result = new DeclStmtAST(decl);
            result.Span = decl.Span.Clone();

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
                DeclStmtAST declStmt = ParseDeclStmt();
                if (declStmt != null)
                {
                    stmts.Add(declStmt);
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
        IdentifierAST paramName = new IdentifierAST(lexer.CurrentIdentifier)
        {
            Span = lexer.CurrentTokenSpan.Clone()
        };
        lexer.NextToken();

        lexer.ExpectToken(TokenType.COLON);

        /*lexer.ExpectToken(TokenType.IDENTIFIER, false);
        SourceSpan paramEnd = lexer.CurrentTokenSpan.Clone();
        IdentifierAST paramType = new IdentifierAST(lexer.CurrentIdentifier)
        {
            Span = lexer.CurrentTokenSpan.Clone()
        };
        lexer.NextToken();*/

        Typespec type = ParseType();
        SourceSpan paramEnd = type.Span.Clone();

        bool redefined = false;
        foreach (var item in parameters)
        {
            if (item.Name.Value == paramName.Value)
            {
                SourceSpan span = new SourceSpan(paramStart.FromLineNumber, paramStart.FromColumnNumber, paramEnd.ToLineNumber, paramEnd.ToColumnNumber);
                lexer.Error(string.Format("ERROR: Redefinition of parameter name '{0}'", paramName.Value), span);
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
        IdentifierAST name = new IdentifierAST(lexer.CurrentIdentifier)
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
        lexer.ExpectToken(TokenType.KEYWORD_EXTERNAL);

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
        else if (lexer.CurrentToken == TokenType.KEYWORD_EXTERNAL)
        {
            return ParseExternalDecl();
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
    }
}
