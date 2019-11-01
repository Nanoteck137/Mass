using System;
using System.Collections.Generic;
using System.Text;

abstract class Type
{
    public static Type U8Type { get; } = new IntType(IntKind.U8);
    public static Type U16Type { get; } = new IntType(IntKind.U16);
    public static Type U32Type { get; } = new IntType(IntKind.U32);
    public static Type U64Type { get; } = new IntType(IntKind.U64);

    public static Type S8Type { get; } = new IntType(IntKind.S8);
    public static Type S16Type { get; } = new IntType(IntKind.S16);
    public static Type S32Type { get; } = new IntType(IntKind.S32);
    public static Type S64Type { get; } = new IntType(IntKind.S64);

    public static Type VoidType { get; } = new VoidType();

    public static void Test()
    {

    }
}

enum IntKind
{
    U8,
    U16,
    U32,
    U64,

    S8,
    S16,
    S32,
    S64,
};

class IntType : Type
{
    public IntKind Kind { get; private set; }

    public IntType(IntKind kind)
    {
        this.Kind = kind;
    }
}

class PtrType : Type
{
    public Type Base { get; private set; }

    public PtrType(Type basee)
    {
        this.Base = basee;
    }

    public override bool Equals(object obj)
    {
        if (GetType() == obj.GetType())
        {
            PtrType other = (PtrType)obj;
            if (Base.Equals(other.Base))
                return true;
        }

        return base.Equals(obj);
    }

    public static bool operator ==(PtrType obj1, PtrType obj2)
    {
        if (ReferenceEquals(obj1, obj2))
        {
            return true;
        }

        if (obj1 is null)
        {
            return false;
        }

        if (obj2 is null)
        {
            return false;
        }

        return obj1.Equals(obj2);
    }

    public static bool operator !=(PtrType obj1, PtrType obj2)
    {
        return !(obj1 == obj2);
    }
}

class VoidType : Type
{
    public VoidType() { }
}

class FunctionParameterType
{
    public string Name { get; private set; }
    public Type Type { get; private set; }

    public FunctionParameterType(string name, Type type)
    {
        this.Name = name;
        this.Type = type;
    }
}

class FunctionType : Type
{
    public List<FunctionParameterType> Parameters { get; private set; }
    public Type ReturnType { get; private set; }
    public bool VarArgs { get; private set; }

    public FunctionType(List<FunctionParameterType> parameters, Type returnType, bool varArgs)
    {
        this.Parameters = parameters;
        this.ReturnType = returnType;
        this.VarArgs = varArgs;
    }
}