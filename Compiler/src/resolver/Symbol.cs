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
        public string Namespace { get; private set; }
        public string QualifiedName { get; set; }

        public SymbolKind Kind { get; private set; }
        public SymbolState State { get; set; }

        public CompilationUnit CompilationUnit { get; private set; }
        public Decl Decl { get; private set; }

        public Type Type { get; set; }
        public Val Val { get; set; }

        public Symbol(string name, string namespaceName, SymbolKind kind, SymbolState state, Decl decl, CompilationUnit compilationUnit)
        {
            this.Name = name;
            this.Namespace = namespaceName;

            this.Kind = kind;
            this.State = state;

            this.CompilationUnit = compilationUnit;
            this.Decl = decl;

            this.Type = null;

            this.Val = new Val
            {
                u32 = 0
            };
        }

        public Symbol(string name, SymbolState state)
        {
            this.Name = name;
            this.State = state;

            this.Decl = null;
            this.Type = null;

            this.Val = new Val
            {
                u32 = 0
            };
        }
    }
}