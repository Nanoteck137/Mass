using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Mass.Compiler
{
    public class CompileUnit
    {
        public string FilePath { get; private set; }
        public List<Decl> Decls { get; private set; }
        public List<Symbol> ResolvedSymbols { get; private set; }

        public Dictionary<string, CompileUnit> Imports { get; private set; }

        public void Import(CompileUnit unit)
        {
            if (Imports.ContainsKey(unit.FilePath))
                Debug.Assert(false);

            Imports.Add(unit.FilePath, unit);
        }

        public void Resolve()
        {
            Resolver resolver = new Resolver();

            foreach (Decl decl in Decls)
            {
                resolver.AddSymbol(decl);
            }

            resolver.ResolveSymbols();
            resolver.FinalizeSymbols();

            ResolvedSymbols = resolver.ResolvedSymbols;
        }

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
