using System;
using System.Collections.Generic;
using System.IO;

namespace Mass.Compiler
{

    public class MassCompiler
    {
        private MassCompiler() { }

        public static CompilationUnit CompileText(string text, string filePath)
        {
            Lexer lexer = new Lexer(filePath, text);
            Parser parser = new Parser(lexer);

            List<Decl> decls = parser.Parse();

            CompilationUnit result = new CompilationUnit(filePath, decls);
            return result;
        }

        public static CompilationUnit CompileFile(string filePath)
        {
            string fileContent = File.ReadAllText(filePath);
            return CompileText(fileContent, filePath);
        }
    }

}