using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    private List<Symbol> localSymbols;
    private Dictionary<string, Symbol> globalSymbols;

    public List<Symbol> ResolvedSymbols { get; private set; }

    public Resolver()
    {
        localSymbols = new List<Symbol>();
        globalSymbols = new Dictionary<string, Symbol>();
        ResolvedSymbols = new List<Symbol>();

        AddGlobalType("u8", Type.U8);
        AddGlobalType("u16", Type.U16);
        AddGlobalType("u32", Type.U32);
        AddGlobalType("u64", Type.U64);

        AddGlobalType("s8", Type.S8);
        AddGlobalType("s16", Type.S16);
        AddGlobalType("s32", Type.S32);
        AddGlobalType("s64", Type.S64);

        AddGlobalType("f32", Type.F32);
        AddGlobalType("f64", Type.F64);
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
        return new ResolvedExpr(Type.S32, val, true);
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

        Symbol sym = new Symbol(decl.Name, kind, SymbolState.UNRESOLVED, decl);
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

    /*public Type ResolveTypespec(Typespec spec)
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
    }*/

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

    /*public void ResolveStmt(Stmt stmt, Type returnType)
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
    }*/

    private ResolvedExpr ResolveIdentifierExpr(IdentifierExpr expr)
    {
        Symbol symbol = ResolveName(expr.Value);
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
            Log.Fatal($"{expr.Value} must be a var or const", null);
        }

        return null;
    }

    private ResolvedExpr ResolveBinaryOpExpr(BinaryOpExpr expr)
    {
        return null;
    }

    private ResolvedExpr ResolveCallExpr(CallExpr expr)
    {
        return null;
    }

    private ResolvedExpr ResolveIndexExpr(IndexExpr epxr)
    {
        return null;
    }

    private ResolvedExpr ResolveExpr(Expr expr)
    {
        /*
        IntegerExpr x
        FloatExpr x
        IdentifierExpr x
        StringExpr x
        BinaryOpExpr
        CallExpr
        IndexExpr
         */
        if (expr is IntegerExpr integerExpr)
        {
            return ResolvedConst(integerExpr.Value);
        }
        else if (expr is FloatExpr floatExpr)
        {
            return ResolvedRValue(floatExpr.IsFloat ? Type.F32 : Type.F64);
        }
        else if (expr is StringExpr strExpr)
        {
            return ResolvedRValue(new PtrType(Type.U8));
        }
        else if (expr is IdentifierExpr identExpr)
        {
            return ResolveIdentifierExpr(identExpr);
        }
        else if (expr is BinaryOpExpr binaryOpExpr)
        {
            return ResolveBinaryOpExpr(binaryOpExpr);
        }
        else if (expr is CallExpr callExpr)
        {
            return ResolveCallExpr(callExpr);
        }
        else if (expr is IndexExpr indexExpr)
        {
            return ResolveIndexExpr(indexExpr);
        }
        else
        {
            Debug.Assert(false);
        }

        return null;
    }

    private Type ResolveTypespec(Typespec typespec)
    {
        /*
         PtrTypespec
         ArrayTypespec
         IdentifierTypespec
         */

        if (typespec is IdentifierTypespec identTypespec)
        {
            Symbol symbol = ResolveName(identTypespec.Value.Value);
            return symbol.Type;
        }
        else
        {
            Debug.Assert(false);
        }

        return null;
    }

    private Type ResolveVarDecl(VarDecl decl)
    {
        Type type = ResolveTypespec(decl.Type);

        if (decl.Value != null)
        {
            ResolvedExpr expr = ResolveExpr(decl.Value);
            if (expr.Type != type)
            {
                Log.Fatal("Var type value mismatch", null);
            }
        }

        return type;
    }

    private Type ResolveConstDecl(ConstDecl decl)
    {
        Debug.Assert(false);
        return null;
    }

    private Type ResolveFuncDecl(FunctionDecl decl)
    {
        Debug.Assert(false);
        return null;
    }

    private Type ResolveStructDecl(StructDecl decl)
    {
        Debug.Assert(false);
        return null;
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

        /*
        VarDecl

        var test: s32 = a + 123;

        ConstDecl
        FunctionDecl
        StructDecl
         */

        if (symbol.Decl is VarDecl varDecl)
        {
            symbol.Type = ResolveVarDecl(varDecl);
        }
        else if (symbol.Decl is ConstDecl constDecl)
        {
            symbol.Type = ResolveConstDecl(constDecl);
        }
        else if (symbol.Decl is FunctionDecl funcDecl)
        {
            symbol.Type = ResolveFuncDecl(funcDecl);
        }
        else if (symbol.Decl is StructDecl structDecl)
        {
            symbol.Type = ResolveStructDecl(structDecl);
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

    public static void Test()
    {
        Resolver resolver = new Resolver();

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

        /*int scope1 = EnterScope();

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
        Debug.Assert(GetSymbol("b") == null);*/

        Lexer lexer = new Lexer("ResolverTest", "");
        Parser parser = new Parser(lexer);

        string[] code = new string[]
        {
            "var a: s32 = b;",
            "var b: s32 = c;",
        };

        foreach (string c in code)
        {
            lexer.Reset(c);
            lexer.NextToken();

            Decl decl = parser.ParseDecl();
            resolver.AddSymbol(decl);
        }

        resolver.ResolveSymbols();
    }
}
