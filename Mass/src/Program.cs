using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mass.Compiler;

namespace Mass
{
    class CompilerOptions
    {
        public string OutputPath { get; set; }
    }

    class Option
    {
        public string Command { get; private set; }
        public string Desc { get; private set; }
        public string[] Parameters { get; private set; }
        public Func<string[], CompilerOptions, bool> Callback { get; private set; }

        public int CharWidth
        {
            get
            {
                int width = this.Command.Length;
                if (this.Parameters != null)
                {
                    foreach (string param in this.Parameters)
                        width += param.Length + 3;
                }

                return width;
            }
        }

        public Option(string command, string desc, Func<string[], CompilerOptions, bool> callback, string[] parameters = null)
        {
            this.Command = command;
            this.Parameters = parameters;
            this.Desc = desc;
            this.Callback += callback;
        }
    }

    class Program
    {
        static void PrintUsage(string executableName)
        {
            Option[] options = new Option[]
            {
                new Option("--help", "Display this infomation", (args, compilerOptions) => { return false; }),
                new Option("--version", "Display the version number", (args, compilerOptions) => { return false; }),
                new Option("-o", "Set the output path", (args, compilerOptions) => { return false; }, new string[] { "file" }),
            };

            Console.WriteLine($"Usage: {executableName} [options] file");
            Console.WriteLine($"Options:");

            int maxCommandCharWidth = 0;
            foreach (Option option in options)
            {
                int width = option.CharWidth;

                if (width > maxCommandCharWidth)
                    maxCommandCharWidth = width;
            }

            foreach (Option option in options)
            {
                int padding = maxCommandCharWidth - option.CharWidth;
                string paddingStr = "".PadLeft(padding);
                string paramsStr = "";
                if (option.Parameters != null)
                {
                    foreach (string param in option.Parameters)
                    {
                        paramsStr += $" <{param}>";
                    }
                }
                Console.WriteLine($"  {option.Command}{paramsStr}{paddingStr}  {option.Desc}");
            }
        }

        static void PrintVersion(string executableName)
        {
            Console.WriteLine($"{executableName} - v0.1");
        }

        static void Main(string[] args)
        {
            string path = Assembly.GetEntryAssembly().Location;
            string executableName = Path.GetFileNameWithoutExtension(path);

            Queue<string> arguments = new Queue<string>();
            foreach (string arg in args)
            {
                arguments.Enqueue(arg);
            }

            CompilerOptions options = new CompilerOptions();

            string currentArg = arguments.Dequeue();
            bool stop = false;
            while (!stop && currentArg.StartsWith("-"))
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
                else if (currentArg == "-o")
                {
                    string outputPath = arguments.Dequeue();
                    options.OutputPath = outputPath;
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
