﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Mass.Compiler;

namespace Mass
{
    class CompilerOptions
    {
        public string OutputPath { get; set; } = "";
        public bool DebugInfo { get; set; } = false;
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
        private const string VERSION = "v1.0-alpha";

        private readonly Dictionary<string, Option> options;
        private readonly string executableName;

        void PrintUsage()
        {
            Console.WriteLine($"Usage: {executableName} [options] dir");
            Console.WriteLine($"Options:");

            int maxCommandCharWidth = 0;
            foreach (Option option in options.Values)
            {
                int width = option.CharWidth;

                if (width > maxCommandCharWidth)
                    maxCommandCharWidth = width;
            }

            foreach (Option option in options.Values)
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

        bool PrintUsageOption(string[] args, CompilerOptions compilerOptions)
        {
            PrintUsage();

            return true;
        }

        bool PrintVersionOption(string[] args, CompilerOptions compilerOptions)
        {
            Console.WriteLine($"{executableName} - {VERSION}");

            return true;
        }

        public Program(string[] args)
        {
            this.options = new Dictionary<string, Option>
            {
                { "--help", new Option("--help", "Display this infomation", PrintUsageOption) },
                { "--version", new Option("--version", "Display the version number", PrintVersionOption) },
                { "-o", new Option(
                                "-o",
                                "Set the output path",
                                (args, compilerOptions) =>
                                {
                                    compilerOptions.OutputPath = args[0];
                                    return false;
                                },
                                new string[] { "file" }) },
                { "-d", new Option(
                                "-d",
                                "Display debug infomation",
                                (args, compilerOptions) =>
                                {
                                    compilerOptions.DebugInfo = true;
                                    return false;
                                }) }
            };

            string path = Assembly.GetEntryAssembly().Location;
            this.executableName = Path.GetFileNameWithoutExtension(path);

            bool error = ParseOptions(ref args, out CompilerOptions compilerOption, out bool exit);
            if (error)
            {
                PrintUsage();
            }
            else if (!exit)
            {
                if (args.Length > 1)
                {
                    Console.WriteLine("Too many arguments");
                }
                else if (args.Length <= 0)
                {
                    Console.WriteLine("No input file");
                }
                else
                {
                    string filePath = args[0];
                    Console.WriteLine($"DEBUG: Trying to compile project at '{filePath}'");
                    StartCompiling(filePath, compilerOption);
                }


            }
        }

        bool ParseOptions(ref string[] args, out CompilerOptions compilerOptions, out bool exit)
        {
            bool error = false;
            exit = false;
            compilerOptions = new CompilerOptions();

            int i = 0;
            for (; i < args.Length; i++)
            {
                string command = args[i];
                if (!command.Contains("-"))
                    break;

                if (!options.ContainsKey(command))
                {
                    Console.WriteLine($"Unknown Option - '{command}'");
                    break;
                }

                Option option = options[command];
                string[] commandArgs = null;
                if (option.Parameters != null && option.Parameters.Length > 0)
                {
                    if (option.Parameters.Length >= args.Length)
                    {
                        int index = i;
                        foreach (string param in option.Parameters)
                        {
                            index++;
                            if (index >= args.Length)
                            {
                                Console.WriteLine($"Missing '{param}' after '{command}'");
                            }
                        }
                        error = true;
                        break;
                    }

                    commandArgs = args[(i + 1)..(option.Parameters.Length + 1)];
                    i += option.Parameters.Length;
                }

                exit = option.Callback(commandArgs, compilerOptions);
            }

            args = args[i..args.Length];
            return error;
        }

        void StartCompiling(string dirPath, CompilerOptions options)
        {
            dirPath = Path.GetFullPath(dirPath);

            Package libc = PackageManager.FindPackage("libc");
            Package main = PackageManager.FindPackageInDir(dirPath);
            Debug.Assert(main != null);

            main.ImportPackage(libc);

            PackageManager.ResolvePackage(main);

            LLVMGenerator.Setup();

            using LLVMGenerator gen = new LLVMGenerator(main);
            gen.Generate();

            if (options.DebugInfo)
                gen.DebugPrint();

            string outputPath = "";
            if (options.OutputPath == "")
            {
                outputPath = Path.ChangeExtension(main.Name, "ll");
            }
            else
            {
                outputPath = options.OutputPath;
            }

            gen.WriteToFile(outputPath);
            gen.RunCode();
        }

        static void Main(string[] args)
        {
            new Program(args);
        }
    }
}
