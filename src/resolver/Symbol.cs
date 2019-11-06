using System;
using System.Collections.Generic;
using System.Text;

enum SymbolKind
{
    None,
    Var,
    Const,
    Func,
    Type
}

enum SymbolState
{
    Unresolved,
    Resolving,
    Resolved
}

class Symbol
{
    public string Name { get; private set; }
    public SymbolKind Kind { get; private set; }
    public SymbolState State { get; set; }
    public Decl Decl { get; private set; }

    public Type Type { get; set; }
    public Val Val { get; set; }

    public Symbol(string name, SymbolKind kind, SymbolState state, Decl decl)
    {
        this.Name = name;
        this.State = state;
        this.Kind = kind;
        this.Decl = decl;
        this.Type = null;

        this.Val = new Val
        {
            u32 = 0
        };
    }
}
