﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using LLVMSharp;

namespace Mass.Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            // Lexer.Test();
            Parser.Test();
            // Printer.Test();
            // Type.Test();
            // Resolver.Test();
            // LLVMGenerator.Test();

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
            gen.WriteToFile("test.ll");

            gen.RunCode();
        }
    }
}