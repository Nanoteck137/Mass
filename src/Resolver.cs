using System;
using System.Collections.Generic;
using System.Diagnostics;

abstract class Type
{
    public static Type IntType { get; } = new IntType();
    public static Type VoidType { get; } = new VoidType();
}

class IntType : Type
{
    public IntType() { }
}

class PtrType : Type
{
    public Type Base { get; private set; }

    public PtrType(Type basee)
    {
        this.Base = basee;
    }
}

class VoidType : Type
{
    public VoidType() { }
}

class FunctionParameterType
{
    public string Name { get; private set; }
    public Type Type { get; private set; }

    public FunctionParameterType(string name, Type type)
    {
        this.Name = name;
        this.Type = type;
    }
}

class FunctionType : Type
{
    public List<FunctionParameterType> Parameters { get; private set; }
    public Type ReturnType { get; private set; }
    public bool VarArgs { get; private set; }

    public FunctionType(List<FunctionParameterType> parameters, Type returnType, bool varArgs)
    {
        this.Parameters = parameters;
        this.ReturnType = returnType;
        this.VarArgs = varArgs;
    }
}

enum SymbolKind
{
    NONE,
    VAR,
    CONST,
    FUNC,
    TYPE
}

enum SymbolState
{
    UNRESOLVED,
    RESOLVING,
    RESOLVED
}

class Symbol
{
    public string Name { get; private set; }
    public SymbolKind Kind { get; private set; }
    public SymbolState State { get; set; }
    public DeclAST Decl { get; private set; }
    public Type Type { get; set; }
    public ulong Val { get; set; }

    public Symbol(string name, SymbolKind kind, SymbolState state, DeclAST decl)
    {
        this.Name = name;
        this.State = state;
        this.Kind = kind;
        this.Decl = decl;
        this.Type = null;
        this.Val = 0;
    }
}

class ResolvedExpr
{
    public Type Type { get; private set; }
    public ulong Val { get; private set; }
    public bool IsConst { get; private set; }

    public ResolvedExpr(Type type, ulong val, bool isConst)
    {
        this.Type = type;
        this.Val = val;
        this.IsConst = isConst;
    }
}

class Resolver
{


    //TODO: Remove the lexer dependency, used for error and fatal messages
    private Lexer lexer;

    private Dictionary<string, Symbol> symbolTable;

    public List<Symbol> ResolvedSymbols { get; private set; }

    public Resolver(Lexer lexer)
    {
        this.lexer = lexer;
        symbolTable = new Dictionary<string, Symbol>();
        ResolvedSymbols = new List<Symbol>();
    }

    public Symbol GetSymbol(string name)
    {
        if (symbolTable.ContainsKey(name))
        {
            return symbolTable[name];
        }

        return null;
    }

    public void AddSymbol(DeclAST decl)
    {
        Debug.Assert(decl != null);
        Debug.Assert(decl.Name != null);
        Debug.Assert(GetSymbol(decl.Name.Value) == null);

        SymbolKind kind = SymbolKind.NONE;
        if (decl is VarDeclAST)
        {
            kind = SymbolKind.VAR;
        }
        else if (decl is ConstDeclAST)
        {
            kind = SymbolKind.CONST;
        }
        else
        {
            Debug.Assert(false);
        }

        symbolTable.Add(decl.Name.Value, new Symbol(decl.Name.Value, kind, SymbolState.UNRESOLVED, decl));
    }

    public void EnterScope()
    {

    }

    public void LeaveScope()
    {

    }

    public Type ResolveTypespec(Typespec spec)
    {
        if (spec is IdentifierTypespec identSpec)
        {
            if (identSpec.Value.Value == "i32")
            {
                return Type.IntType;
            }
            else
            {
                lexer.Fatal(string.Format("Unknown Identifer type: '{0}'", identSpec.Value.Value));
            }
        }
        else if (spec is PtrTypespec ptrSpec)
        {
            return new PtrType(ResolveTypespec(ptrSpec.Type));
        }

        return null;
    }

    public void ResolveFuncDecl(Symbol symbol)
    {
        FunctionDeclAST decl = (FunctionDeclAST)symbol.Decl;
        FunctionPrototypeAST prototype = decl.Prototype;

        Type returnType = new VoidType();
        if (prototype.ReturnType != null)
            returnType = ResolveTypespec(prototype.ReturnType);
        List<FunctionParameterType> parameters = new List<FunctionParameterType>();

        foreach (FunctionParameter param in prototype.Parameters)
        {
            Type paramType = ResolveTypespec(param.Type);
            parameters.Add(new FunctionParameterType(param.Name.Value, paramType));
        }

        symbol.Type = new FunctionType(parameters, returnType, prototype.VarArgs);
    }

    public ResolvedExpr ResolveExprBinary(BinaryOpExprAST expr)
    {
        Debug.Assert(expr != null);
        Debug.Assert(expr is BinaryOpExprAST);
        Debug.Assert(expr.Op == Operation.ADD);

        ResolvedExpr left = ResolveExpr(expr.Left);
        ResolvedExpr right = ResolveExpr(expr.Right);

        if (!(left.Type is IntType))
        {
            lexer.Fatal("Left operand of + is not int");
        }

        if (right.Type.GetType() != left.Type.GetType())
        {
            lexer.Fatal("Left and Right operand of + must have same type");
        }

        Type type = left.Type;
        bool isConst = false;
        ulong val = 0;

        if (left.IsConst && right.IsConst)
        {
            isConst = true;
            val = left.Val + right.Val;
        }

        return new ResolvedExpr(type, val, isConst);
    }

    public ResolvedExpr ResolveExprIdentifer(IdentifierAST ident)
    {
        Debug.Assert(ident != null);

        Symbol symbol = ResolveName(ident.Value);

        if (symbol.Kind == SymbolKind.VAR)
        {
            return new ResolvedExpr(symbol.Type, 0, false);
        }
        else if (symbol.Kind == SymbolKind.CONST)
        {
            return new ResolvedExpr(symbol.Type, symbol.Val, true);
        }
        else
        {
            lexer.Fatal(string.Format("{0} must denote a var or const", ident.Value));
        }

        return null;
    }

    public ResolvedExpr ResolveExpr(ExprAST expr)
    {
        Debug.Assert(expr != null);

        if (expr is NumberAST number)
        {
            return new ResolvedExpr(Type.IntType, number.Number, true);
        }
        else if (expr is BinaryOpExprAST binary)
        {
            return ResolveExprBinary(binary);
        }
        else if (expr is IdentifierAST ident)
        {
            return ResolveExprIdentifer(ident);
        }
        else
        {
            Debug.Assert(false);
        }

        return null;
    }

    public Type ResolveConstDecl(Symbol symbol, ref ulong val)
    {
        Debug.Assert(symbol.Decl is ConstDeclAST);

        ConstDeclAST decl = (ConstDeclAST)symbol.Decl;

        ResolvedExpr result = ResolveExpr(decl.Value);
        val = result.Val;

        return result.Type;
    }

    public void ResolveSymbol(Symbol symbol)
    {
        if (symbol.State == SymbolState.RESOLVED)
        {
            return;
        }

        if (symbol.State == SymbolState.RESOLVING)
        {
            lexer.Fatal("Cyclic Dependency");
            return;
        }

        symbol.State = SymbolState.RESOLVING;

        if (symbol.Decl is VarDeclAST varDecl)
        {
            symbol.Type = ResolveTypespec(varDecl.Type);
        }
        else if (symbol.Decl is FunctionDeclAST)
        {
            ResolveFuncDecl(symbol);
        }
        else if (symbol.Decl is ConstDeclAST)
        {
            ulong val = 0;
            symbol.Type = ResolveConstDecl(symbol, ref val);
            symbol.Val = val;
        }
        else
        {
            Debug.Assert(false);
        }

        symbol.State = SymbolState.RESOLVED;
        ResolvedSymbols.Add(symbol);
    }

    public Symbol ResolveName(string name)
    {
        Symbol sym = GetSymbol(name);
        if (sym == null)
        {
            lexer.Fatal(string.Format("Unknown symbol name: '{0}'", name));
            return null;
        }

        ResolveSymbol(sym);

        return sym;
    }

    public void ResolveSymbols()
    {
        foreach (var item in symbolTable)
        {
            ResolveSymbol(item.Value);
        }
    }

    public void Test()
    {
        Debug.Assert(GetSymbol("foo") == null);

        VarDeclAST decl = new VarDeclAST(new IdentifierAST("foo"), new IdentifierTypespec(new IdentifierAST("int")), new NumberAST(123));
        AddSymbol(decl);

        Symbol sym = GetSymbol("foo");
        Debug.Assert(sym != null);
        Debug.Assert(sym.Decl == decl);

        Typespec spec = new PtrTypespec(new IdentifierTypespec(new IdentifierAST("int")));
        Type type = ResolveTypespec(spec);

        ResolveSymbol(sym);
    }
}
