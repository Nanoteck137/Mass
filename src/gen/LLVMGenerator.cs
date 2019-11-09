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
            }
        }
        else if (type is FloatType floatType)
        {
            Debug.Assert(false);
        }
        else if (type is PtrType ptrType)
        {
            Debug.Assert(false);
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
            LLVMTypeRef type = GetType(symbol.Type);
            LLVMValueRef varDef = module.AddGlobal(type, varDecl.Name);
            varDef.IsGlobalConstant = false;
            varDef.Initializer = GenConstExpr(varDecl.Value);

            globals[varDecl.Name] = varDef;
        }
        else if (decl is ConstDecl constDecl)
        {

        }
        else if (decl is FunctionDecl functionDecl)
        {
            LLVMValueRef func = module.AddFunction(functionDecl.Name, GetType(symbol.Type));
            for (int i = 0; i < functionDecl.Parameters.Count; i++)
            {
                func.Params[i].Name = functionDecl.Parameters[i].Name;
            }
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
            "var a: s8 = 123;",
            "var b: s64 = 321;",
            "func add(a: s32, b: s32) {}"
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
