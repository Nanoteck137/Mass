using LLVMSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

class FunctionGen
{
    public LLVMValueRef Func { get; private set; }
    public Dictionary<string, LLVMValueRef> LocalArgumentPtrs { get; private set; }

    public FunctionGen(LLVMValueRef func, Dictionary<string, LLVMValueRef> localArgumentPtrs)
    {
        this.Func = func;
        this.LocalArgumentPtrs = localArgumentPtrs;
    }
}

class CodeGenerator : IDisposable
{
    private Lexer lexer;
    private Resolver resolver;

    private LLVMModuleRef module;
    public LLVMModuleRef Module { get { return module; } }

    private Dictionary<string, LLVMValueRef> globals;
    private Dictionary<string, LLVMValueRef> locals;
    private Dictionary<string, LLVMValueRef> strings;

    private Dictionary<string, FunctionGen> genFunctions;

    public CodeGenerator(Lexer lexer, Resolver resolver, string moduleName)
    {
        this.lexer = lexer;
        this.resolver = resolver;

        module = LLVMModuleRef.CreateWithName(moduleName);

        globals = new Dictionary<string, LLVMValueRef>();
        locals = new Dictionary<string, LLVMValueRef>();
        strings = new Dictionary<string, LLVMValueRef>();
        genFunctions = new Dictionary<string, FunctionGen>();
    }

    public void Dispose()
    {
        module.Dispose();
    }

    private LLVMValueRef GetStringValue(LLVMBuilderRef builder, string str)
    {

        if (strings.ContainsKey(str))
        {
            return strings[str];
        }

        /*LLVMTypeRef type = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
        LLVMValueRef value = module.AddGlobal(type, string.Format("str_{0}", strings.Count));
        value.Initializer = LLVMValueRef.CreateConstRealOfString(type, str);*/

        LLVMValueRef value = builder.BuildGlobalStringPtr(str);
        strings[str] = value;

        return value;
    }

    /*private LLVMValueRef GetStrPtr(LLVMBuilderRef builder, string str)
    {
        return GetStringValue(str);
    }*/

    private void PushLocalValue(string name, LLVMValueRef value)
    {
        if (locals.ContainsKey(name))
        {
            lexer.Fatal(string.Format("'{0}' is already defined local value", name));
        }
        else
        {
            locals.Add(name, value);
        }
    }

    public LLVMValueRef GetValueFromName(string name)
    {
        if (locals.ContainsKey(name))
        {
            return locals[name];
        }

        if (globals.ContainsKey(name))
        {
            return globals[name];
        }

        lexer.Fatal(string.Format("Undefined Value name '{0}'", name));

        return null;
    }

    private LLVMTypeRef GetType(Type type)
    {
        if (type is IntType intType)
        {
            switch (intType.Kind)
            {
                case IntKind.U8:
                case IntKind.S8:
                    return LLVMTypeRef.Int8;
                case IntKind.U16:
                case IntKind.S16:
                    return LLVMTypeRef.Int16;
                case IntKind.U32:
                case IntKind.S32:
                    return LLVMTypeRef.Int32;
                case IntKind.U64:
                case IntKind.S64:
                    return LLVMTypeRef.Int64;
            }

            return null;
        }
        else if (type is PtrType ptrType)
        {
            return LLVMTypeRef.CreatePointer(GetType(ptrType.Base), 0);
        }
        else if (type is VoidType)
        {
            return LLVMTypeRef.Void;
        }
        else if (type is FunctionType funcType)
        {
            LLVMTypeRef[] paramTypes = new LLVMTypeRef[funcType.Parameters.Count];
            for (int i = 0; i < funcType.Parameters.Count; i++)
            {
                paramTypes[i] = GetType(funcType.Parameters[i].Type);
            }

            LLVMTypeRef result = LLVMTypeRef.CreateFunction(GetType(funcType.ReturnType), paramTypes, funcType.VarArgs);

            return result;
        }

        return null;
    }

    private LLVMValueRef CreateConstantExprFold(ExprAST expr)
    {
        if (expr is IntegerExprAST numberExpr)
        {
            return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, numberExpr.Integer);
        }
        else if (expr is BinaryOpExprAST binaryExpr)
        {
            switch (binaryExpr.Op)
            {
                case Operation.ADD:
                    return LLVMValueRef.CreateConstAdd(CreateConstantExprFold(binaryExpr.Left),
                                                       CreateConstantExprFold(binaryExpr.Right));
                case Operation.SUB:
                    return LLVMValueRef.CreateConstSub(CreateConstantExprFold(binaryExpr.Left),
                                                       CreateConstantExprFold(binaryExpr.Right));
                case Operation.MUL:
                    return LLVMValueRef.CreateConstMul(CreateConstantExprFold(binaryExpr.Left),
                                                       CreateConstantExprFold(binaryExpr.Right));
                case Operation.DIV:
                    return LLVMValueRef.CreateConstUDiv(CreateConstantExprFold(binaryExpr.Left),
                                                        CreateConstantExprFold(binaryExpr.Right));
            }
        }
        else
        {
            lexer.Fatal("Unknown Expr kind");
        }

        return null;
    }

    private void GenerateVarDecl(Symbol symbol, VarDeclAST decl)
    {
        LLVMTypeRef genType = GetType(symbol.Type);

        LLVMValueRef varDef = module.AddGlobal(genType, decl.Name.Value);
        varDef.IsGlobalConstant = false;
        varDef.Initializer = CreateConstantExprFold(decl.Value);

        globals[decl.Name.Value] = varDef;
    }

    private LLVMValueRef GenerateExpr(LLVMBuilderRef builder, ExprAST expr)
    {
        if (expr is IntegerExprAST numberExpr)
        {
            return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, numberExpr.Integer);
        }
        else if (expr is IdentifierExprAST identExpr)
        {
            //if (identExpr.)
            //ResolvedExpr result = resolver.ResolveExpr(expr);
            LLVMValueRef value = GetValueFromName(identExpr.Value);
            Symbol symbol = resolver.GetSymbol(identExpr.Value);
            if (symbol != null && symbol.Kind == SymbolKind.FUNC)
            {
                return value;
            }

            return builder.BuildLoad(value);
        }
        else if (expr is BinaryOpExprAST binaryExpr)
        {
            LLVMValueRef left = GenerateExpr(builder, binaryExpr.Left);
            LLVMValueRef right = GenerateExpr(builder, binaryExpr.Right);

            Debug.Assert(binaryExpr.Op == Operation.ADD);

            switch (binaryExpr.Op)
            {
                case Operation.ADD:
                    return builder.BuildAdd(left, right, "addRes");
            }
        }
        else if (expr is CallExprAST callExpr)
        {
            LLVMValueRef function = GenerateExpr(builder, callExpr.Expr);
            LLVMValueRef[] arguments = new LLVMValueRef[callExpr.Arguments.Count];
            for (int i = 0; i < callExpr.Arguments.Count; i++)
            {
                arguments[i] = GenerateExpr(builder, callExpr.Arguments[i]);
            }
            return builder.BuildCall(function, arguments);
        }
        else if (expr is StringExprAST str)
        {
            return GetStringValue(builder, str.Value);
        }
        else
        {
            Debug.Assert(false);
        }

        return null;
    }

    private void GenerateStmt(LLVMBuilderRef builder, StmtAST stmt)
    {
        if (stmt is DeclStmtAST declStmt)
        {
            if (declStmt.Decl is VarDeclAST varDecl)
            {
                LLVMTypeRef type = GetType(resolver.ResolveTypespec(varDecl.Type));
                LLVMValueRef ptr = builder.BuildAlloca(type, varDecl.Name.Value);

                LLVMValueRef value = GenerateExpr(builder, varDecl.Value);
                builder.BuildStore(value, ptr);

                PushLocalValue(varDecl.Name.Value, ptr);
            }
            else
            {
                Debug.Assert(false);
            }
            //GenerateStmtDecl(builder, decl);
        }
        else if (stmt is ReturnStmtAST returnStmt)
        {
            LLVMValueRef retValue = GenerateExpr(builder, returnStmt.Value);
            builder.BuildRet(retValue);
        }
        else if (stmt is ExprStmtAST exprStmt)
        {
            GenerateExpr(builder, exprStmt.Expr);
        }
        else
        {
            Debug.Assert(false);
        }
    }

    private void GenerateStmtBlock(LLVMBuilderRef builder, StmtBlock stmtBlock)
    {
        foreach (StmtAST stmt in stmtBlock.Stmts)
        {
            GenerateStmt(builder, stmt);
        }
    }

    private void GenerateFuncDecl(Symbol symbol, FunctionDeclAST decl)
    {
        FunctionType funcType = (FunctionType)symbol.Type;
        LLVMValueRef func = module.AddFunction(decl.Name.Value, GetType(symbol.Type));
        func.Linkage = LLVMLinkage.LLVMExternalLinkage;
        for (int i = 0; i < funcType.Parameters.Count; i++)
        {
            func.Params[i].Name = funcType.Parameters[i].Name;
        }

        LLVMBasicBlockRef entry = func.AppendBasicBlock("entry");
        LLVMBuilderRef builder = module.Context.CreateBuilder();
        builder.PositionAtEnd(entry);

        Dictionary<string, LLVMValueRef> localArgumentPtrs = new Dictionary<string, LLVMValueRef>();

        // NOTE(patrik): Create local storage for the arguments
        for (int i = 0; i < func.ParamsCount; i++)
        {
            //TODO(patrik): Save the ptrs for local access later
            LLVMValueRef ptr = builder.BuildAlloca(func.Params[i].TypeOf, string.Format("s_{0}", func.Params[i].Name));
            builder.BuildStore(func.Params[i], ptr);

            locals[func.Params[i].Name] = ptr;
            localArgumentPtrs[func.Params[i].Name] = ptr;
        }

        FunctionGen functionGen = new FunctionGen(func, localArgumentPtrs);

        GenerateStmtBlock(builder, decl.Body);

        if (funcType.ReturnType.Equals(Type.VoidType))
        {
            builder.BuildRetVoid();
        }

        //builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 123));

        locals.Clear();

        globals[decl.Name.Value] = func;
        genFunctions[decl.Name.Value] = functionGen;
    }

    private void GenerateExternDecl(Symbol symbol, ExternalDeclAST decl)
    {
        Debug.Assert(symbol != null);
        Debug.Assert(decl != null);

        FunctionType funcType = (FunctionType)symbol.Type;
        LLVMValueRef func = module.AddFunction(decl.Name.Value, GetType(symbol.Type));
        func.Linkage = LLVMLinkage.LLVMExternalLinkage;
        for (int i = 0; i < funcType.Parameters.Count; i++)
        {
            func.Params[i].Name = funcType.Parameters[i].Name;
        }

        globals[decl.Name.Value] = func;
    }

    private void GenerateSymbol(Symbol symbol)
    {
        if (symbol.Decl is VarDeclAST varDecl)
        {
            GenerateVarDecl(symbol, varDecl);
        }
        else if (symbol.Decl is FunctionDeclAST funcDecl)
        {
            GenerateFuncDecl(symbol, funcDecl);
        }
        else if (symbol.Decl is ExternalDeclAST externDecl)
        {
            GenerateExternDecl(symbol, externDecl);
        }
        else
        {
            Debug.Assert(false);
        }
    }

    public void Generate()
    {
        foreach (Symbol symbol in resolver.ResolvedSymbols)
        {
            GenerateSymbol(symbol);
        }
    }

    public void Test()
    {
        string test = module.PrintToString();
        Console.WriteLine(test);

        File.WriteAllText("test.ll", test);

        module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
    }
}