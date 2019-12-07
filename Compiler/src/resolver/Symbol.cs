using System;
using System.Collections.Generic;
using System.Text;

namespace Mass.Compiler
{
    public enum SymbolKind
    {
        None,
        Var,
        Const,
        Func,
        Type,
        Package
    }

    public enum SymbolState
    {
        Unresolved,
        Resolving,
        Resolved
    }

    public class Symbol
    {
        public string Name { get; private set; }
        public string QualifiedName { get; set; }

        public SymbolKind Kind { get; private set; }
        public SymbolState State { get; set; }

        public CompilationUnit CompilationUnit { get; private set; }
        public Decl Decl { get; private set; }
        public Package Package { get; set; }

        public Type Type { get; set; }
        public Val Val { get; set; }

        public Symbol(string name, SymbolKind kind, SymbolState state, Decl decl, CompilationUnit compilationUnit)
        {
            this.Name = name;

            this.Kind = kind;
            this.State = state;

            this.CompilationUnit = compilationUnit;
            this.Decl = decl;

            this.Package = null;
            this.Type = null;

            this.Val = new Val
            {
                u32 = 0
            };
        }

        public Symbol(string name, SymbolState state, Package package)
        {
            this.Name = name;
            this.State = state;
            this.Kind = SymbolKind.Package;
            this.Package = package;

            this.Decl = null;
            this.Type = null;

            this.Val = new Val
            {
                u32 = 0
            };
        }
    }
}