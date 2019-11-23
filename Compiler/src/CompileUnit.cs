using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mass.Compiler
{
    public class CompileUnit
    {
        public string FilePath { get; private set; }
        public List<Decl> Decls { get; private set; }

        public static CompileUnit CompileFile(string filePath)
        {
            string fileContent = File.ReadAllText(filePath);

            string fileName = Path.GetFileName(filePath);
            Lexer lexer = new Lexer(fileName, fileContent);
            Parser parser = new Parser(lexer);

            List<Decl> decls = parser.Parse();

            CompileUnit result = new CompileUnit
            {
                Decls = decls
            };

            return result;
        }
    }
}
