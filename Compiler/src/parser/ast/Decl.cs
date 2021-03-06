﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Mass.Compiler
{
    public abstract class DeclAttribute { }

    // TODO(patrik): Add an alias to the external function if the user wants it
    public class ExternalDeclAttribute : DeclAttribute { }
    public class InlineDeclAttribute : DeclAttribute { }
    public class ExportDeclAttribute : DeclAttribute { }

    public abstract class Decl
    {
        public string Name { get; protected set; }
        public List<DeclAttribute> Attributes { get; set; }

        public SourceSpan Span { get; set; }

        public DeclAttribute GetAttribute(System.Type type)
        {
            for (int i = 0; i < Attributes.Count; i++)
            {
                if (Attributes[i].GetType() == type)
                {
                    return Attributes[i];
                }
            }

            return null;
        }
    }

    public class VarDecl : Decl
    {
        public Typespec Type { get; private set; }
        public Expr Value { get; private set; }

        public VarDecl(string name, Typespec type, Expr value)
        {
            this.Name = name;
            this.Type = type;
            this.Value = value;
        }
    }

    public class ConstDecl : Decl
    {
        public Typespec Type { get; private set; }
        public Expr Value { get; private set; }

        public ConstDecl(string name, Typespec type, Expr value)
        {
            this.Name = name;
            this.Type = type;
            this.Value = value;
        }
    }

    public class FunctionParameter
    {
        public string Name { get; private set; }
        public Typespec Type { get; private set; }

        public SourceSpan Span { get; set; }

        public FunctionParameter(string name, Typespec type)
        {
            this.Name = name;
            this.Type = type;
        }
    }

    public class FunctionDecl : Decl
    {
        public List<FunctionParameter> Parameters { get; private set; }
        public Typespec ReturnType { get; private set; }
        public bool VarArgs { get; private set; }
        public StmtBlock Body { get; private set; }

        public FunctionDecl(string name, List<FunctionParameter> parameters, Typespec returnType, bool varArgs, StmtBlock body)
        {
            this.Name = name;
            this.Parameters = parameters;
            this.ReturnType = returnType;
            this.VarArgs = varArgs;
            this.Body = body;
        }
    }

    public class StructItem
    {
        public string Name { get; private set; }
        public Typespec Type { get; private set; }

        public StructItem(string name, Typespec type)
        {
            this.Name = name;
            this.Type = type;
        }
    }

    public class StructDecl : Decl
    {
        public List<StructItem> Items { get; private set; }
        public bool IsOpaque { get; private set; }

        public StructDecl(string name, List<StructItem> items, bool isOpaque)
        {
            this.Name = name;
            this.Items = items;
            this.IsOpaque = isOpaque;
        }
    }

    public class ImportDecl : Decl
    {
        public List<IdentifierExpr> Symbols { get; private set; }

        public ImportDecl(string name, List<IdentifierExpr> symbols)
        {
            this.Name = name;
            this.Symbols = symbols;
        }
    }

    public class UseDecl : Decl
    {
        public UseDecl(string name)
        {
            this.Name = name;
        }
    }

    public class NamespaceDecl : Decl
    {
        public NamespaceDecl(string name)
        {
            this.Name = name;
        }
    }
}