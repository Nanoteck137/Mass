using LLVMSharp;
using System;
using System.Collections.Generic;
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

    private LLVMModuleRef module;

    private Dictionary<string, LLVMValueRef> globals;

    private Dictionary<string, FunctionGen> genFunctions;

    public CodeGenerator(Lexer lexer, string moduleName)
    {
        this.lexer = lexer;
        module = LLVMModuleRef.CreateWithName(moduleName);

        globals = new Dictionary<string, LLVMValueRef>();
        genFunctions = new Dictionary<string, FunctionGen>();
    }

    public void Dispose()
    {
        module.Dispose();
    }

    private LLVMTypeRef GetType(Type type)
    {
        if (type is IntType)
        {
            return LLVMTypeRef.Int32;
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
        if (expr is NumberAST numberExpr)
        {
            return LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, numberExpr.Number);
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

    private void GenerateStmtDecl(LLVMBuilderRef builder, DeclStmtAST decl)
    {
    }

    private void GenerateStmt(LLVMBuilderRef builder, StmtAST stmt)
    {
        if (stmt is DeclStmtAST decl)
        {
            GenerateStmtDecl(builder, decl);
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

            localArgumentPtrs[func.Params[i].Name] = ptr;
        }

        FunctionGen functionGen = new FunctionGen(func, localArgumentPtrs);

        GenerateStmtBlock(builder, decl.Body);

        builder.BuildRet(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 123));

        globals[decl.Name.Value] = func;
        genFunctions[decl.Name.Value] = functionGen;
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
    }

    public void Generate(Resolver resolver)
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

        module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
    }
}