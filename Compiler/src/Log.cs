using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Mass.Compiler
{
    class Log
    {
        private Log() { }

        private static void Print(string type, string message, SourceSpan span)
        {
            if (span == null)
            {
                Console.WriteLine($"{type}: {message}");
            }
            else
            {
                Console.WriteLine($"{span.FileName}({span.FromLineNumber}:{span.FromColumnNumber}, {span.ToLineNumber}:{span.ToColumnNumber}): error: {message}");
            }
        }

        public static void Note(string message, SourceSpan span)
        {
            Print("note", message, span);
        }

        public static void Error(string message, SourceSpan span)
        {
            Print("error", message, span);
        }

        public static void Warning(string message, SourceSpan span)
        {
            Print("warning", message, span);
        }

        public static void Fatal(string message, SourceSpan span)
        {
            Print("fatal", message, span);
            Debugger.Break();
            Environment.Exit(-1);
        }
    }
}