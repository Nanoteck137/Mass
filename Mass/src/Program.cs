using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mass.Compiler;

namespace Mass
{
    class Program
    {
        static void PrintUsage(string executableName)
        {
            Console.WriteLine($"Usage: {executableName} [options] file");
            Console.WriteLine($"Options:");
            Console.WriteLine($"  --help     Display this infomation");
            Console.WriteLine($"  --version  Display the version number");
        }

        static void PrintVersion(string executableName)
        {
            Console.WriteLine($"{executableName} - v0.1");
        }

        static void Main(string[] args)
        {
            string path = Assembly.GetEntryAssembly().Location;
            string executableName = Path.GetFileNameWithoutExtension(path);
            // PrintUsage(executableName);


            Queue<string> arguments = new Queue<string>();
            foreach (string arg in args)
            {
                arguments.Enqueue(arg);
            }

            string currentArg = arguments.Dequeue();
            bool stop = false;
            while (!stop && currentArg.StartsWith("--"))
            {
                if (currentArg == "--help")
                {
                    PrintUsage(executableName);
                    stop = true;
                    break;
                }
                else if (currentArg == "--version")
                {
                    PrintVersion(executableName);
                    stop = true;
                    break;
                }

                if (!arguments.TryDequeue(out currentArg))
                {
                    PrintUsage(executableName);
                    break;
                }
            }

            return;
            string filePath = "test.ma";
            filePath = Path.GetFullPath(filePath);

            string fileContent = File.ReadAllText(filePath);

            Lexer lexer = new Lexer(Path.GetFileName(filePath), fileContent);
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
            gen.WriteToFile(Path.ChangeExtension(filePath, "ll"));

            gen.RunCode();
        }
    }
}
