using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using LLVMSharp;

namespace Mass
{
    class Program
    {
        static void Main(string[] args)
        {
            string fileContent = File.ReadAllText("test.ma");

            //Lexer lexer = new Lexer("test.ma", fileContent);
            //lexer.NextToken();

            Lexer.Test();
            Parser.Test();
            Printer.Test();

            return;
            /*Parser parser = new Parser(lexer);

            List<DeclAST> root = parser.Parse();

            Printer printer = new Printer();
            //printer.Test();
            foreach (DeclAST decl in root)
            {
                printer.PrintDecl(decl);
                Console.WriteLine();
            }

            Resolver resolver = new Resolver(lexer);
            //resolver.Test();

            foreach (DeclAST decl in root)
            {
                resolver.AddSymbol(decl);
            }

            resolver.ResolveSymbols();

            CodeGenerator codeGenerator = new CodeGenerator(lexer, resolver, lexer.FileName);
            codeGenerator.Generate();
            codeGenerator.Test();*/


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
            Console.WriteLine(str);*/

            /*LLVMModuleRef module = codeGenerator.Module;

            LLVMValueRef func = codeGenerator.GetValueFromName("main");
            //LLVMValueRef sumFunc = module.GetNamedFunction("add");

            LLVMPassManagerRef passManager = module.CreateFunctionPassManager();
            passManager.AddInstructionCombiningPass();
            passManager.AddReassociatePass();
            passManager.AddGVNPass();
            passManager.AddCFGSimplificationPass();
            passManager.InitializeFunctionPassManager();*/

            //passManager.RunFunctionPassManager(sumFunc);

            /*str = module.PrintToString();
            Console.WriteLine("New Module");
            Console.WriteLine(str);*/

            /*codeGenerator.Test();

            LLVM.LinkInMCJIT();
            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();

            LLVMMCJITCompilerOptions options = new LLVMMCJITCompilerOptions()
            {
                NoFramePointerElim = 1
            };

            LLVMExecutionEngineRef engine = module.CreateExecutionEngine();
            LLVMValueRef t = engine.FindFunction("test");
            LLVMGenericValueRef val = new LLVMGenericValueRef();
            LLVMGenericValueRef one = val.CreateInt(LLVMTypeRef.Int32, 123, false);
            LLVMGenericValueRef two = val.CreateInt(LLVMTypeRef.Int32, 321, false);*/

            /*LLVMGenericValueRef[] funcArgs = new LLVMGenericValueRef[]
            {
                one, two
            };*/

            //int res = engine.RunFunctionAsMain(func, 0, null, null);

            /*unsafe
            {
                ulong wooh = LLVM.GenericValueToInt(res, 0);
                Console.WriteLine("Result: {0}", wooh);
            }*/

            //codeGenerator.Dispose();
        }
    }
}
