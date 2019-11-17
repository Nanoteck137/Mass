using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Mass.Compiler
{

    [Serializable]
    public class FatalErrorException : Exception
    {
        public FatalErrorException() { }
        public FatalErrorException(string message) : base(message) { }
        public FatalErrorException(string message, Exception inner) : base(message, inner) { }
        protected FatalErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class Log
    {
        private Log() { }

        private static string Print(string type, string message, SourceSpan span)
        {
            if (span == null)
            {
                string text = $"{type}: {message}";
                Console.WriteLine(text);
                return text;
            }
            else
            {
                string text = $"{span.FileName}({span.FromLineNumber}:{span.FromColumnNumber}, {span.ToLineNumber}:{span.ToColumnNumber}): error: {message}";
                Console.WriteLine(text);
                return text;
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
            string text = Print("fatal", message, span);

            throw new FatalErrorException(text);
        }
    }
}