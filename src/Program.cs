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
            /*Lexer.Test();
            Parser.Test();
            Printer.Test();
            Type.Test();
            Resolver.Test();
            LLVMGenerator.Test();*/

            string fileContent = File.ReadAllText("test.ma");

            Lexer lexer = new Lexer("test.ma", fileContent);
            lexer.NextToken();

            Parser parser = new Parser(lexer);

            List<Decl> root = parser.Parse();

            Resolver resolver = new Resolver();

            foreach (Decl decl in root)
            {
                resolver.AddSymbol(decl);
            }

            resolver.ResolveSymbols();
            resolver.FinalizeSymbols();

            LLVMGenerator.Setup();

            using LLVMGenerator gen = new LLVMGenerator(resolver);
            gen.Generate();
            gen.DebugPrint();

            gen.RunCode();

            /*
            LLVMPassManagerRef passManager = module.CreateFunctionPassManager();
            passManager.AddInstructionCombiningPass();
            passManager.AddReassociatePass();
            passManager.AddGVNPass();
            passManager.AddCFGSimplificationPass();
            passManager.InitializeFunctionPassManager();*/

            //passManager.RunFunctionPassManager(sumFunc);

            /*
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
