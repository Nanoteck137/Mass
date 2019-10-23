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

    public override bool Equals(object obj)
    {
        if (GetType() == obj.GetType())
        {
            PtrType other = (PtrType)obj;
            if (Base.Equals(other.Base))
                return true;
        }

        return base.Equals(obj);
    }

    public static bool operator ==(PtrType obj1, PtrType obj2)
    {
        if (ReferenceEquals(obj1, obj2))
        {
            return true;
        }

        if (obj1 is null)
        {
            return false;
        }

        if (obj2 is null)
        {
            return false;
        }

        return obj1.Equals(obj2);
    }

    public static bool operator !=(PtrType obj1, PtrType obj2)
    {
        return !(obj1 == obj2);
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
    public bool IsLValue { get; private set; }
    public bool IsConst { get; private set; }
    public ulong Val { get; private set; }

    public ResolvedExpr(Type type, ulong val, bool isConst)
    {
        this.Type = type;
        this.Val = val;
        this.IsConst = isConst;
    }

    public ResolvedExpr(Type type, bool isLValue)
    {
        this.Type = type;
        this.IsLValue = IsLValue;
    }
}

class Resolver
{
    //TODO: Remove the lexer dependency, used for error and fatal messages
    private Lexer lexer;

    private List<Symbol> localSymbols;
    private Dictionary<string, Symbol> globalSymbols;

    public List<Symbol> ResolvedSymbols { get; private set; }

    public Resolver(Lexer lexer)
    {
        this.lexer = lexer;

        localSymbols = new List<Symbol>();
        globalSymbols = new Dictionary<string, Symbol>();
        ResolvedSymbols = new List<Symbol>();

        Symbol sym = new Symbol("i32", SymbolKind.TYPE, SymbolState.RESOLVED, null);
        sym.Type = Type.IntType;
        globalSymbols.Add("i32", sym);
    }

    //NOTE(patrik): Helper function
    private ResolvedExpr ResolvedRValue(Type type)
    {
        return new ResolvedExpr(type, false);
    }

    //NOTE(patrik): Helper function
    private ResolvedExpr ResolvedLValue(Type type)
    {
        return new ResolvedExpr(type, true);
    }

    private ResolvedExpr ResolvedConst(ulong val)
    {
        return new ResolvedExpr(Type.IntType, val, true);
    }

    public Symbol GetSymbol(string name)
    {
        for (int i = localSymbols.Count - 1; i >= 0; i--)
        {
            if (localSymbols[i].Name == name)
            {
                return localSymbols[i];
            }
        }

        if (globalSymbols.ContainsKey(name))
        {
            return globalSymbols[name];
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
        else if (decl is FunctionDeclAST)
        {
            kind = SymbolKind.FUNC;
        }
        else
        {
            Debug.Assert(false);
        }

        Symbol sym = new Symbol(decl.Name.Value, kind, SymbolState.UNRESOLVED, decl)
        {
            Type = Type.IntType
        };
        globalSymbols.Add(decl.Name.Value, sym);
    }

    public void PushVar(string name, Type type)
    {
        Symbol symbol = new Symbol(name, SymbolKind.VAR, SymbolState.RESOLVED, null)
        {
            Type = type
        };
        localSymbols.Add(symbol);
    }

    public int EnterScope()
    {
        //if (localSymbols.Count <= 0)
        //return 0;

        return localSymbols.Count - 1;
    }

    public void LeaveScope(int ptr)
    {
        int index = ptr + 1;
        int count = localSymbols.Count - (ptr + 1);
        localSymbols.RemoveRange(index, count);
    }

    public Type ResolveTypespec(Typespec spec)
    {
        if (spec is IdentifierTypespec identSpec)
        {
            Symbol symbol = ResolveName(identSpec.Value.Value);
            if (symbol.Kind != SymbolKind.TYPE)
            {
                lexer.Fatal(string.Format("{0} is not a type", symbol.Name));
            }
            return symbol.Type;
        }
        else if (spec is PtrTypespec ptrSpec)
        {
            return new PtrType(ResolveTypespec(ptrSpec.Type));
        }

        return null;
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
            return ResolvedLValue(symbol.Type);
        }
        else if (symbol.Kind == SymbolKind.CONST)
        {
            return ResolvedConst(symbol.Val);
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

    public Type ResolveVarDecl(VarDeclAST decl)
    {
        Debug.Assert(decl != null);

        Type type = ResolveTypespec(decl.Type);

        ResolvedExpr result = ResolveExpr(decl.Value);

        if (!result.Type.Equals(type))
        {
            lexer.Fatal("Declared var type does not match inferred type");
        }

        return type;
    }

    public Type ResolveFuncDecl(FunctionDeclAST decl)
    {
        FunctionPrototypeAST prototype = decl.Prototype;

        Type returnType = Type.VoidType;
        if (prototype.ReturnType != null)
            returnType = ResolveTypespec(prototype.ReturnType);
        List<FunctionParameterType> parameters = new List<FunctionParameterType>();

        foreach (FunctionParameter param in prototype.Parameters)
        {
            Type paramType = ResolveTypespec(param.Type);
            parameters.Add(new FunctionParameterType(param.Name.Value, paramType));
        }

        return new FunctionType(parameters, returnType, prototype.VarArgs);
    }

    public void ResolveStmt(StmtAST stmt, Type returnType)
    {
        if (stmt is ReturnStmtAST returnStmt)
        {
            ResolvedExpr expr = ResolveExpr(returnStmt.Value);
            if (!returnType.Equals(expr.Type))
            {
                lexer.Fatal("Return type mismatch");
            }
        }
        else if (stmt is DeclStmtAST declStmt)
        {
            if (declStmt.Decl is VarDeclAST varDecl)
            {
                Type type = ResolveTypespec(varDecl.Type);

                ResolvedExpr result = ResolveExpr(varDecl.Value);

                if (!result.Type.Equals(type))
                {
                    lexer.Fatal("Declared var type does not match inferred type");
                }

                PushVar(varDecl.Name.Value, type);
            }
            else
            {
                lexer.Fatal("Only supports var decls in other decls");
            }
        }
        else
        {
            Debug.Assert(false);
        }
    }

    public void ResolveStmtBlock(StmtBlock block, Type returnType)
    {
        int scope = EnterScope();
        foreach (StmtAST stmt in block.Stmts)
        {
            ResolveStmt(stmt, returnType);
        }
        LeaveScope(scope);
    }

    public void ResolveFuncBody(Symbol symbol)
    {
        Debug.Assert(symbol.Decl is FunctionDeclAST);

        FunctionDeclAST decl = (FunctionDeclAST)symbol.Decl;

        int scope = EnterScope();
        foreach (FunctionParameter param in decl.Prototype.Parameters)
        {
            PushVar(param.Name.Value, ResolveTypespec(param.Type));
        }

        Type returnType = Type.VoidType;
        if (decl.Prototype.ReturnType != null)
            returnType = ResolveTypespec(decl.Prototype.ReturnType);

        ResolveStmtBlock(decl.Body, returnType);
        LeaveScope(scope);
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
            symbol.Type = ResolveVarDecl(varDecl); //ResolveTypespec(varDecl.Type);
        }
        else if (symbol.Decl is FunctionDeclAST funcDecl)
        {
            symbol.Type = ResolveFuncDecl(funcDecl);
            ResolveFuncBody(symbol);
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
        }

        ResolveSymbol(sym);

        return sym;
    }

    public void ResolveSymbols()
    {
        foreach (var item in globalSymbols)
        {
            ResolveSymbol(item.Value);
        }
    }

    public void Test()
    {
        /*Debug.Assert(GetSymbol("foo") == null);

        VarDeclAST decl = new VarDeclAST(new IdentifierAST("foo"), new IdentifierTypespec(new IdentifierAST("i32")), new NumberAST(123));
        AddSymbol(decl);

        Symbol sym = GetSymbol("foo");
        Debug.Assert(sym != null);
        Debug.Assert(sym.Decl == decl);

        Typespec spec = new PtrTypespec(new IdentifierTypespec(new IdentifierAST("i32")));
        Type type = ResolveTypespec(spec);

        Type test = new PtrType(Type.IntType);
        if (type.Equals(test))
        {
            Console.WriteLine();
        }

        ResolveSymbol(sym);*/

        int scope1 = EnterScope();

        PushVar("a", Type.IntType);
        PushVar("b", Type.IntType);
        Debug.Assert(GetSymbol("a") != null);
        Debug.Assert(GetSymbol("b") != null);

        int scope2 = EnterScope();

        PushVar("c", Type.IntType);
        PushVar("d", Type.IntType);
        Debug.Assert(GetSymbol("a") != null);
        Debug.Assert(GetSymbol("b") != null);
        Debug.Assert(GetSymbol("c") != null);
        Debug.Assert(GetSymbol("d") != null);

        LeaveScope(scope2);
        Debug.Assert(GetSymbol("c") == null);
        Debug.Assert(GetSymbol("d") == null);

        LeaveScope(scope1);

        Debug.Assert(GetSymbol("a") == null);
        Debug.Assert(GetSymbol("b") == null);

        DeclAST[] decls = new DeclAST[]
        {
            new ConstDeclAST(
                new IdentifierAST("A"),
                new IdentifierTypespec(
                    new IdentifierAST("i32")),
                new NumberAST(123)),

            new VarDeclAST(
                new IdentifierAST("b"),
                new IdentifierTypespec(
                    new IdentifierAST("i32")),
                new BinaryOpExprAST(
                    new IdentifierAST("A"),
                    new NumberAST(321),
                    Operation.ADD)),

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
                    new IdentifierTypespec(
                        new IdentifierAST("i32")),
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
            AddSymbol(decl);
        }

        ResolveSymbols();
    }
}
