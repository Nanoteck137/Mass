using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    public Decl Decl { get; private set; }
    public Type Type { get; set; }
    public ulong Val { get; set; }

    public Symbol(string name, SymbolKind kind, SymbolState state, Decl decl)
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

        AddGlobalType("u8", Type.U8Type);
        AddGlobalType("u16", Type.U16Type);
        AddGlobalType("u32", Type.U32Type);
        AddGlobalType("u64", Type.U64Type);

        AddGlobalType("s8", Type.U8Type);
        AddGlobalType("s16", Type.U16Type);
        AddGlobalType("s32", Type.U32Type);
        AddGlobalType("s64", Type.U64Type);
    }

    private void AddGlobalType(string name, Type type)
    {
        Symbol sym = new Symbol(name, SymbolKind.TYPE, SymbolState.RESOLVED, null)
        {
            Type = type
        };
        globalSymbols.Add(name, sym);
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
        return new ResolvedExpr(Type.U32Type, val, true);
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

    public void AddSymbol(Decl decl)
    {
        Debug.Assert(decl != null);
        Debug.Assert(decl.Name != null);
        Debug.Assert(GetSymbol(decl.Name) == null);

        SymbolKind kind = SymbolKind.NONE;
        if (decl is VarDecl)
        {
            kind = SymbolKind.VAR;
        }
        else if (decl is ConstDecl)
        {
            kind = SymbolKind.CONST;
        }
        else if (decl is FunctionDecl)
        {
            kind = SymbolKind.FUNC;
        }
        else
        {
            Debug.Assert(false);
        }

        Symbol sym = new Symbol(decl.Name, kind, SymbolState.UNRESOLVED, decl)
        {
            Type = Type.U32Type
        };
        globalSymbols.Add(decl.Name, sym);
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
                Log.Fatal($"{symbol.Name} is not a type", spec.Span);
            }
            return symbol.Type;
        }
        else if (spec is PtrTypespec ptrSpec)
        {
            return new PtrType(ResolveTypespec(ptrSpec.Type));
        }

        return null;
    }

    public ResolvedExpr ResolveExprBinary(BinaryOpExpr expr)
    {
        Debug.Assert(expr != null);
        Debug.Assert(expr is BinaryOpExpr);
        Debug.Assert(expr.Op == TokenType.PLUS);

        ResolvedExpr left = ResolveExpr(expr.Left);
        ResolvedExpr right = ResolveExpr(expr.Right);

        if (!(left.Type is IntType))
        {
            Log.Fatal("Left operand of + is not int", expr.Span);
        }

        if (right.Type.GetType() != left.Type.GetType())
        {
            Log.Fatal("Left and Right operand of + must have same type", expr.Span);
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

    public ResolvedExpr ResolveExprIdentifer(IdentifierExpr ident)
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
        else if (symbol.Kind == SymbolKind.FUNC)
        {
            return ResolvedRValue(symbol.Type);
        }
        else
        {
            Log.Fatal($"{ident.Value} must denote a var or const", ident.Span);
        }

        return null;
    }

    public ResolvedExpr ResolveExprCall(CallExpr call)
    {
        Debug.Assert(call != null);

        //TODO(patrik): Check the parameters

        ResolvedExpr func = ResolveExpr(call.Expr);
        if (!(func.Type is FunctionType))
        {
            Log.Fatal("Trying to call a non-function value", call.Span);
        }

        FunctionType funcType = (FunctionType)func.Type;

        return ResolvedRValue(funcType.ReturnType);
    }

    public ResolvedExpr ResolveExpr(Expr expr)
    {
        Debug.Assert(expr != null);

        if (expr is IntegerExpr number)
        {
            return new ResolvedExpr(Type.U32Type, number.Value, true);
        }
        else if (expr is BinaryOpExpr binary)
        {
            return ResolveExprBinary(binary);
        }
        else if (expr is IdentifierExpr ident)
        {
            return ResolveExprIdentifer(ident);
        }
        else if (expr is CallExpr call)
        {
            return ResolveExprCall(call);
        }
        else
        {
            Debug.Assert(false);
        }

        return null;
    }

    public Type ResolveConstDecl(Symbol symbol, ref ulong val)
    {
        Debug.Assert(symbol.Decl is ConstDecl);

        ConstDecl decl = (ConstDecl)symbol.Decl;

        ResolvedExpr result = ResolveExpr(decl.Value);
        val = result.Val;

        return result.Type;
    }

    public Type ResolveVarDecl(VarDecl decl)
    {
        Debug.Assert(decl != null);

        Type type = ResolveTypespec(decl.Type);

        ResolvedExpr result = ResolveExpr(decl.Value);

        if (!result.Type.Equals(type))
        {
            Log.Fatal("Declared var type does not match inferred type", null);
        }

        return type;
    }

    /*public Type ResolveFuncPrototype(FunctionPrototype prototype)
    {
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
    }*/

    public void ResolveStmt(Stmt stmt, Type returnType)
    {
        if (stmt is ReturnStmt returnStmt)
        {
            ResolvedExpr expr = ResolveExpr(returnStmt.Value);
            if (!returnType.Equals(expr.Type))
            {
                Log.Fatal("Return type mismatch", stmt.Span);
            }
        }
        else if (stmt is DeclStmt declStmt)
        {
            if (declStmt.Decl is VarDecl varDecl)
            {
                Type type = ResolveTypespec(varDecl.Type);

                ResolvedExpr result = ResolveExpr(varDecl.Value);

                if (!result.Type.Equals(type))
                {
                    Log.Fatal("Declared var type does not match inferred type", null);
                }

                PushVar(varDecl.Name, type);
            }
            else
            {
                Log.Fatal("Only supports var decls in other decls", null);
            }
        }
        else if (stmt is ExprStmt exprStmt)
        {
            ResolvedExpr result = ResolveExpr(exprStmt.Expr);
        }
        else
        {
            Debug.Assert(false);
        }
    }

    public void ResolveStmtBlock(StmtBlock block, Type returnType)
    {
        int scope = EnterScope();
        foreach (Stmt stmt in block.Stmts)
        {
            ResolveStmt(stmt, returnType);
        }
        LeaveScope(scope);
    }

    public void ResolveFuncBody(Symbol symbol)
    {
        Debug.Assert(symbol.Decl is FunctionDecl);

        FunctionDecl decl = (FunctionDecl)symbol.Decl;

        int scope = EnterScope();
        foreach (FunctionParameter param in decl.Parameters)
        {
            PushVar(param.Name, ResolveTypespec(param.Type));
        }

        Type returnType = Type.VoidType;
        if (decl.ReturnType != null)
            returnType = ResolveTypespec(decl.ReturnType);

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
            Log.Fatal("Cyclic Dependency", null);
            return;
        }

        symbol.State = SymbolState.RESOLVING;

        if (symbol.Decl is VarDecl varDecl)
        {
            symbol.Type = ResolveVarDecl(varDecl); //ResolveTypespec(varDecl.Type);
        }
        else if (symbol.Decl is FunctionDecl funcDecl)
        {
            //symbol.Type = ResolveFuncPrototype(funcDecl.Prototype);
            //ResolveFuncBody(symbol);
        }
        else if (symbol.Decl is ConstDecl)
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
            Log.Fatal($"Unknown symbol name: '{name}'", null);
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

        VarDecl decl = new VarDecl(new Identifier("foo"), new IdentifierTypespec(new Identifier("i32")), new Number(123));
        AddSymbol(decl);

        Symbol sym = GetSymbol("foo");
        Debug.Assert(sym != null);
        Debug.Assert(sym.Decl == decl);

        Typespec spec = new PtrTypespec(new IdentifierTypespec(new Identifier("i32")));
        Type type = ResolveTypespec(spec);

        Type test = new PtrType(Type.IntType);
        if (type.Equals(test))
        {
            Console.WriteLine();
        }

        ResolveSymbol(sym);*/

        int scope1 = EnterScope();

        PushVar("a", Type.U32Type);
        PushVar("b", Type.U32Type);
        Debug.Assert(GetSymbol("a") != null);
        Debug.Assert(GetSymbol("b") != null);

        int scope2 = EnterScope();

        PushVar("c", Type.U32Type);
        PushVar("d", Type.U32Type);
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

        Decl[] decls = new Decl[]
        {
            new ConstDecl(
                "A",
                new IdentifierTypespec(
                    new IdentifierExpr("s32")),
                new IntegerExpr(123)),

            new VarDecl(
                "b",
                new IdentifierTypespec(
                    new IdentifierExpr("s32")),
                new BinaryOpExpr(
                    new IdentifierExpr("A"),
                    new IntegerExpr(321),
                    TokenType.PLUS)),

            /*new FunctionDecl(
                new FunctionPrototype(
                    new IdentifierExpr("add"),
                    new List<FunctionParameter>() {
                        new FunctionParameter(
                            new IdentifierExpr("a"),
                            new IdentifierTypespec(new IdentifierExpr("s32"))),
                        new FunctionParameter(
                            new IdentifierExpr("b"),
                            new IdentifierTypespec(new IdentifierExpr("s32")))
                    },
                    new IdentifierTypespec(
                        new IdentifierExpr("s32")),
                    true),
                new StmtBlock(new List<Stmt>() {
                    new ReturnStmt(
                        new BinaryOpExpr(
                            new IdentifierExpr("a"),
                            new IdentifierExpr("b"),
                            Operation.ADD))
            }))*/
        };

        foreach (Decl decl in decls)
        {
            AddSymbol(decl);
        }

        ResolveSymbols();
    }
}
