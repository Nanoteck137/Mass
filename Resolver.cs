using System;
using System.Collections.Generic;
using System.Diagnostics;

abstract class Type { }

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

enum SymbolState
{
    UNRESOLVED,
    RESOLVING,
    RESOLVED
}

class Symbol
{
    public string Name { get; private set; }
    public SymbolState State { get; set; }
    public DeclAST Decl { get; private set; }
    public Type Type { get; set; }

    public Symbol(string name, SymbolState state, DeclAST decl)
    {
        this.Name = name;
        this.State = state;
        this.Decl = decl;
        this.Type = null;
    }
}

class Resolver
{
    //TODO: Remove the lexer dependency
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

        symbolTable.Add(decl.Name.Value, new Symbol(decl.Name.Value, SymbolState.UNRESOLVED, decl));
    }

    public void EnterScope()
    {

    }

    public void LeaveScope()
    {

    }

    public Type ResolveType(Typespec spec)
    {
        if (spec is IdentifierTypespec identSpec)
        {
            if (identSpec.Value.Value == "i32")
            {
                return new IntType();
            }
            else
            {
                lexer.Fatal(string.Format("Unknown Identifer type: '{0}'", identSpec.Value.Value));
            }
        }
        else if (spec is PtrTypespec ptrSpec)
        {
            return new PtrType(ResolveType(ptrSpec.Type));
        }

        return null;
    }

    public void ResolveFuncDecl(Symbol symbol)
    {
        FunctionDeclAST decl = (FunctionDeclAST)symbol.Decl;
        FunctionPrototypeAST prototype = decl.Prototype;

        Type returnType = new VoidType();
        if (prototype.ReturnType != null)
            returnType = ResolveType(prototype.ReturnType);
        List<FunctionParameterType> parameters = new List<FunctionParameterType>();

        foreach (FunctionParameter param in prototype.Parameters)
        {
            Type paramType = ResolveType(param.Type);
            parameters.Add(new FunctionParameterType(param.Name.Value, paramType));
        }

        symbol.Type = new FunctionType(parameters, returnType, prototype.VarArgs);
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
            symbol.Type = ResolveType(varDecl.Type);
            symbol.State = SymbolState.RESOLVED;
        }
        else if (symbol.Decl is FunctionDeclAST)
        {
            ResolveFuncDecl(symbol);
            symbol.State = SymbolState.RESOLVED;
        }

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
        Type type = ResolveType(spec);

        ResolveSymbol(sym);
    }
}
