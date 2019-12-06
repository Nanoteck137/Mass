using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Mass.Compiler
{
    public class CompilationUnit
    {
        public string FilePath { get; private set; }

        public List<Decl> Decls { get; private set; }
        public List<ImportDecl> Imports { get; private set; }

        public CompilationUnit(string filePath, List<Decl> decls)
        {
            this.FilePath = filePath;

            ProcessDecls(decls);
        }

        private void ProcessDecls(List<Decl> decls)
        {
            this.Decls = new List<Decl>();
            this.Imports = new List<ImportDecl>();

            foreach (Decl decl in decls)
            {
                if (decl is ImportDecl importDecl)
                {
                    this.Imports.Add(importDecl);
                }
                else
                {
                    this.Decls.Add(decl);
                }
            }
        }
    }
}
