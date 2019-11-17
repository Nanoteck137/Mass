using System;
using System.Collections.Generic;
using System.IO;

using Mass.Compiler;

namespace Mass
{
    class Program
    {
        static void Main(string[] args)
        {
            string fileContent = File.ReadAllText("test.ma");

            Lexer lexer = new Lexer("test.ma", fileContent);
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
            gen.WriteToFile("test.ll");

            gen.RunCode();
        }
    }
}
