using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using LLVMSharp;

class LLVMGenerator : CodeGenerator
{
    private LLVMModuleRef module;

    private Dictionary<string, LLVMValueRef> globals;

    public LLVMGenerator(Resolver resolver)
        : base(resolver)
    {
        module = LLVMModuleRef.CreateWithName("NO NAME");
        globals = new Dictionary<string, LLVMValueRef>();
    }

    private LLVMTypeRef GetType(Type type)
    {
        /*
        FloatType
        PtrType
        ArrayType
        VoidType
        FunctionType
        StructType
         */

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
                default:
                    Debug.Assert(false);
                    break;
            }
        }
        else if (type is FloatType floatType)
        {
            switch (floatType.Kind)
            {
                case FloatKind.F32:
                    return LLVMTypeRef.Float;
                case FloatKind.F64:
                    return LLVMTypeRef.Double;
                default:
                    Debug.Assert(false);
                    break;
            }
        }
        else if (type is PtrType ptrType)
        {
            return LLVMTypeRef.CreatePointer(GetType(ptrType.Base), 0);
        }
        else if (type is ArrayType arrayType)
        {
            return LLVMTypeRef.CreateArray(GetType(arrayType.Base), (uint)arrayType.Count);
        }
        else if (type is VoidType)
        {
            return LLVMTypeRef.Void;
        }
        else if (type is FunctionType functionType)
        {
            LLVMTypeRef returnType = GetType(functionType.ReturnType);
            LLVMTypeRef[] paramTypes = new LLVMTypeRef[functionType.Parameters.Count];
            for (int i = 0; i < functionType.Parameters.Count; i++)
            {
                paramTypes[i] = GetType(functionType.Parameters[i].Type);
            }

            return LLVMTypeRef.CreateFunction(returnType, paramTypes, functionType.VarArgs);
        }
        else if (type is StructType structType)
        {
            Debug.Assert(false);
        }
        else
        {
            Debug.Assert(false);
        }

        return null;
    }

    private LLVMValueRef GenConstExpr(Expr expr)
    {
        /*
        FloatExpr
        IdentifierExpr
        StringExpr
        BinaryOpExpr
        CallExpr
        IndexExpr
        CompoundExpr
        FieldExpr
         */

        if (expr is IntegerExpr integerExpr)
        {
            LLVMTypeRef type = GetType(integerExpr.ResolvedType);
            return LLVMValueRef.CreateConstInt(type, integerExpr.Value);
        }
        else if (expr is FloatExpr floatExpr)
        {
        }
        else if (expr is StringExpr)
        {
        }
        else if (expr is IdentifierExpr identExpr)
        {
            Debug.Assert(false);
        }
        else if (expr is BinaryOpExpr binaryOpExpr)
        {
            LLVMValueRef left = GenConstExpr(binaryOpExpr.Left);
            LLVMValueRef right = GenConstExpr(binaryOpExpr.Right);

            return LLVMValueRef.CreateConstAdd(left, right);
        }
        else if (expr is CallExpr callExpr)
        {
        }
        else if (expr is IndexExpr indexExpr)
        {
        }
        else if (expr is CompoundExpr compoundExpr)
        {
        }
        else if (expr is FieldExpr fieldExpr)
        {
        }
        else
        {
            Debug.Assert(false);
        }

        return null;
    }

    private LLVMValueRef GenVarDecl(VarDecl decl, Type varType)
    {
        LLVMTypeRef type = GetType(varType);
        LLVMValueRef varDef = module.AddGlobal(type, decl.Name);
        varDef.IsGlobalConstant = false;
        varDef.IsExternallyInitialized = false;

        if (decl.Value != null)
        {
            varDef.Initializer = GenConstExpr(decl.Value);
        }
        else
        {
            varDef.Linkage = LLVMLinkage.LLVMCommonLinkage;
            varDef.Initializer = LLVMValueRef.CreateConstNull(type);
        }

        return varDef;
    }

    private LLVMValueRef GenConstDecl(ConstDecl decl)
    {
        Debug.Assert(false);
        return null;
    }

    private LLVMValueRef GenFuncDecl(FunctionDecl decl, Type funcType)
    {
        Debug.Assert(decl != null);
        Debug.Assert(funcType != null);
        Debug.Assert(funcType is FunctionType);

        LLVMValueRef func = module.AddFunction(decl.Name, GetType(funcType));
        for (int i = 0; i < decl.Parameters.Count; i++)
        {
            func.Params[i].Name = decl.Parameters[i].Name;
        }

        //GenFuncBody();

        return func;
    }

    private LLVMValueRef GenStructDecl(StructDecl decl)
    {
        Debug.Assert(false);
        return null;
    }

    private void GenDecl(Symbol symbol)
    {
        Decl decl = symbol.Decl;

        /*
        ConstDecl
        FunctionDecl
        StructDecl
         */

        if (decl is VarDecl varDecl)
        {
            LLVMValueRef value = GenVarDecl(varDecl, symbol.Type);
            globals[varDecl.Name] = value;
        }
        else if (decl is ConstDecl constDecl)
        {
            LLVMValueRef value = GenConstDecl(constDecl);
            globals[constDecl.Name] = value;
        }
        else if (decl is FunctionDecl functionDecl)
        {
            LLVMValueRef value = GenFuncDecl(functionDecl, symbol.Type);
            globals[functionDecl.Name] = value;
        }
        else if (decl is StructDecl structDecl)
        {

        }
        else
        {
            Debug.Assert(false);
        }
    }

    public override void Generate()
    {
        foreach (Symbol symbol in resolver.ResolvedSymbols)
        {
            GenDecl(symbol);
        }
    }

    public void DebugPrint()
    {
        module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
        string str = module.PrintToString();
        Console.WriteLine(str);
    }

    public static void Test()
    {
        Lexer lexer = new Lexer("LLVM Code Generator Test", "");
        Parser parser = new Parser(lexer);
        Resolver resolver = new Resolver();
        LLVMGenerator gen = new LLVMGenerator(resolver);

        string[] code = new string[]
        {
            "var a: s8 = 1;",
            "var b: s16 = 2;",
            "var c: s32 = 3;",
            "var d: s64 = 4;",
            "var e: s32[4];",
            "var f: s32*;",
            "func add(a: s32, b: s32) {}",
        };

        foreach (string c in code)
        {
            lexer.Reset(c);
            lexer.NextToken();

            Decl decl = parser.ParseDecl();
            resolver.AddSymbol(decl);
        }

        resolver.ResolveSymbols();
        resolver.FinalizeSymbols();

        gen.Generate();
        gen.DebugPrint();
    }
}
