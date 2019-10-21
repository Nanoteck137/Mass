using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using LLVMSharp;

namespace Mass
{
    class Program
    {
        static void Main(string[] args)
        {
            string fileContent = File.ReadAllText("test.ma");

            Lexer lexer = new Lexer("test.ma", fileContent);
            lexer.NextToken();

            Parser parser = new Parser(lexer);

            List<DeclAST> root = parser.Parse();

            Resolver resolver = new Resolver(lexer);
            //resolver.Test();
            resolver.AddSymbol(root[0]);
            resolver.AddSymbol(root[1]);
            //resolver.AddSymbol(root[2]);

            resolver.ResolveSymbols();

            CodeGenerator codeGenerator = new CodeGenerator(lexer, lexer.FileName);
            codeGenerator.Generate(resolver);
            codeGenerator.Test();

            codeGenerator.Dispose();

            /*
            LLVMTypeRef[] funcParams = new LLVMTypeRef[]
            {
                LLVMTypeRef.Int32, LLVMTypeRef.Int32
            };

            LLVMTypeRef sumFuncType = LLVMTypeRef.CreateFunction(LLVMTypeRef.Int32, funcParams);
            LLVMValueRef sumFunc = module.AddFunction("add", sumFuncType);
            sumFunc.Linkage = LLVMLinkage.LLVMExternalLinkage;

            sumFunc.Params[0].Name = "a";
            sumFunc.Params[1].Name = "b";

            LLVMBasicBlockRef entry = sumFunc.AppendBasicBlock("entry");

            LLVMBuilderRef builder = module.Context.CreateBuilder();
            builder.PositionAtEnd(entry);

            LLVMValueRef retVal = builder.BuildAdd(sumFunc.Params[0], sumFunc.Params[1], "tmp");
            builder.BuildRet(retVal);

            string str = module.PrintToString();
            Console.WriteLine("Old Module");
            Console.WriteLine(str);

            LLVMPassManagerRef passManager = module.CreateFunctionPassManager();
            passManager.AddInstructionCombiningPass();
            passManager.AddReassociatePass();
            passManager.AddGVNPass();
            passManager.AddCFGSimplificationPass();
            passManager.InitializeFunctionPassManager();

            passManager.RunFunctionPassManager(sumFunc);

            str = module.PrintToString();
            Console.WriteLine("New Module");
            Console.WriteLine(str);

            LLVM.LinkInMCJIT();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetMC();

            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();

            LLVMMCJITCompilerOptions options = new LLVMMCJITCompilerOptions()
            {
                NoFramePointerElim = 1
            };


            LLVMExecutionEngineRef engine = module.CreateInterpreter(); //module.CreateMCJITCompiler(ref options);
            LLVMGenericValueRef val = new LLVMGenericValueRef();
            LLVMGenericValueRef one = val.CreateInt(LLVMTypeRef.Int32, 123, false);
            LLVMGenericValueRef two = val.CreateInt(LLVMTypeRef.Int32, 321, false);

            LLVMGenericValueRef[] funcArgs = new LLVMGenericValueRef[]
            {
                one, two
            };

            LLVMGenericValueRef res = engine.RunFunction(sumFunc, funcArgs);

            unsafe
            {
                ulong wooh = LLVM.GenericValueToInt(res, 0);
                Console.WriteLine("Result: {0}", wooh);
            }

            module.Dispose();*/
        }
    }
}
