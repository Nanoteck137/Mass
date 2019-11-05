using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

abstract class Type
{
    public static Type U8 { get; } = new IntType(IntKind.U8);
    public static Type U16 { get; } = new IntType(IntKind.U16);
    public static Type U32 { get; } = new IntType(IntKind.U32);
    public static Type U64 { get; } = new IntType(IntKind.U64);

    public static Type S8 { get; } = new IntType(IntKind.S8);
    public static Type S16 { get; } = new IntType(IntKind.S16);
    public static Type S32 { get; } = new IntType(IntKind.S32);
    public static Type S64 { get; } = new IntType(IntKind.S64);

    public static Type F32 { get; } = new FloatType(FloatKind.F32);
    public static Type F64 { get; } = new FloatType(FloatKind.F64);

    public static Type Void { get; } = new VoidType();

    public static void Test()
    {
        Type type1 = new PtrType(Type.S32);
        Type type2 = new PtrType(Type.U32);
        Debug.Assert(type1 != type2);

        Type type3 = new PtrType(Type.U32);
        Type type4 = new PtrType(Type.U32);
        Debug.Assert(type3 == type4);

        Type type5 = new ArrayType(Type.U32, 3);
        Type type6 = new ArrayType(Type.U32, 3);
        Debug.Assert(type5 == type6);

        Type type7 = Type.U32;
        Type type8 = Type.U32;
        Debug.Assert(type7 == type8);

        Type type9 = new FunctionType(
            new List<FunctionParameterType>()
            {
                new FunctionParameterType("a", Type.S32),
                new FunctionParameterType("b", Type.S32),
            }, Type.S32, false);

        Type type10 = new FunctionType(
            new List<FunctionParameterType>()
            {
                new FunctionParameterType("a", Type.S32),
                new FunctionParameterType("b", Type.S32),
            }, Type.S32, false);

        Debug.Assert(type9 == type10);
    }

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public static bool operator ==(Type obj1, Type obj2)
    {
        if (obj1 is null)
        {
            return false;
        }

        if (obj2 is null)
        {
            return false;
        }

        if (ReferenceEquals(obj1, obj2))
        {
            return true;
        }

        return obj1.Equals(obj2);
    }

    public static bool operator !=(Type obj1, Type obj2)
    {
        return !(obj1 == obj2);
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

    public override bool Equals(object obj)
    {
        if (GetType() == obj.GetType())
        {
            IntType other = (IntType)obj;
            if (Kind == other.Kind)
                return true;
        }

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), this.Kind);
    }
}

enum FloatKind
{
    F32,
    F64
}

class FloatType : Type
{
    public FloatKind Kind { get; private set; }

    public FloatType(FloatKind kind)
    {
        this.Kind = kind;
    }

    public override bool Equals(object obj)
    {
        if (GetType() == obj.GetType())
        {
            FloatType other = (FloatType)obj;
            if (Kind == other.Kind)
                return true;
        }

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), this.Kind);
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

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Base);
    }
}

class ArrayType : Type
{
    public Type Base { get; private set; }
    public ulong Size { get; private set; }

    public ArrayType(Type basee, ulong size)
    {
        this.Base = basee;
        this.Size = size;
    }

    public override bool Equals(object obj)
    {
        if (GetType() == obj.GetType())
        {
            ArrayType other = (ArrayType)obj;
            if (Base.Equals(other.Base) && this.Size == other.Size)
                return true;
        }

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Base, this.Size);
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

    public override bool Equals(object obj)
    {
        if (GetType() == obj.GetType())
        {
            FunctionParameterType other = (FunctionParameterType)obj;
            if (Name.Equals(other.Name) && Type.Equals(other.Type))
                return true;
        }

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(this.Name, this.Type);
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

    public override bool Equals(object obj)
    {
        if (GetType() == obj.GetType())
        {
            FunctionType other = (FunctionType)obj;
            if (ReturnType.Equals(other.ReturnType) && VarArgs == other.VarArgs)
            {
                if (Parameters.Count != other.Parameters.Count)
                    return false;

                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (!Parameters[i].Equals(other.Parameters[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), this.Parameters, this.ReturnType, this.VarArgs);
    }
}
