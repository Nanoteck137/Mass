using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
class Val
{
    [FieldOffset(0)] public byte u8;
    [FieldOffset(0)] public ushort u16;
    [FieldOffset(0)] public uint u32;
    [FieldOffset(0)] public ulong u64;

    [FieldOffset(0)] public sbyte s8;
    [FieldOffset(0)] public short s16;
    [FieldOffset(0)] public int s32;
    [FieldOffset(0)] public long s64;

    [FieldOffset(0)] public float f32;
    [FieldOffset(0)] public double f64;
}

class Operand
{
    public Type Type { get; set; }
    public bool IsLValue { get; set; }
    public bool IsConst { get; set; }
    public Val Val { get; set; }

    public Operand(Type type, Val val, bool isConst)
    {
        this.Type = type;
        this.Val = val;
        this.IsConst = isConst;
    }

    public Operand(Type type, bool isLValue)
    {
        this.Type = type;
        this.IsLValue = isLValue;
    }
}

class Resolver
{
    private List<Symbol> localSymbols;
    private Dictionary<string, Symbol> globalSymbols;

    public List<Symbol> ResolvedSymbols { get; private set; }

    private readonly Dictionary<Type, int> typeRank;

    public Resolver()
    {
        localSymbols = new List<Symbol>();
        globalSymbols = new Dictionary<string, Symbol>();
        ResolvedSymbols = new List<Symbol>();

        AddGlobalType("u8", Type.U8);
        AddGlobalType("u16", Type.U16);
        AddGlobalType("u32", Type.U32);
        AddGlobalType("u64", Type.U64);

        AddGlobalType("s8", Type.S8);
        AddGlobalType("s16", Type.S16);
        AddGlobalType("s32", Type.S32);
        AddGlobalType("s64", Type.S64);

        AddGlobalType("f32", Type.F32);
        AddGlobalType("f64", Type.F64);

        typeRank = new Dictionary<Type, int>()
        {
            { Type.U8, 1 },
            { Type.S8, 1 },

            { Type.U16, 2 },
            { Type.S16, 2 },

            { Type.U32, 3 },
            { Type.S32, 3 },

            { Type.U64, 4 },
            { Type.S64, 4 },
        };
    }

    private int GetTypeRank(Type type)
    {
        Debug.Assert(typeRank.ContainsKey(type));

        return typeRank[type];
    }

    private void AddGlobalType(string name, Type type)
    {
        Symbol sym = new Symbol(name, SymbolKind.Type, SymbolState.Resolved, null)
        {
            Type = type
        };

        globalSymbols.Add(name, sym);
    }

    //NOTE(patrik): Helper function
    private Operand OperandRValue(Type type)
    {
        return new Operand(type, false);
    }

    //NOTE(patrik): Helper function
    private Operand OperandLValue(Type type)
    {
        return new Operand(type, true);
    }

    //NOTE(patrik): Helper function
    private Operand OperandConst(Type type, Val val)
    {
        return new Operand(type, val, true);
    }

    public Symbol GetSymbol(string name)
    {
        for (int i = localSymbols.Count - 1; i >= 0; i--)
        {
            if (localSymbols[i].Name == name)
            {
                return localSymbols[i];
            }
        }

        if (globalSymbols.ContainsKey(name))
        {
            return globalSymbols[name];
        }

        return null;
    }

    public void AddSymbol(Decl decl)
    {
        Debug.Assert(decl != null);
        Debug.Assert(decl.Name != null);
        Debug.Assert(GetSymbol(decl.Name) == null);

        SymbolKind kind = SymbolKind.None;
        if (decl is VarDecl)
        {
            kind = SymbolKind.Var;
        }
        else if (decl is ConstDecl)
        {
            kind = SymbolKind.Const;
        }
        else if (decl is FunctionDecl)
        {
            kind = SymbolKind.Func;
        }
        else if (decl is StructDecl)
        {
            kind = SymbolKind.Type;
        }
        else
        {
            Debug.Assert(false);
        }

        Symbol sym = new Symbol(decl.Name, kind, SymbolState.Unresolved, decl);
        globalSymbols.Add(decl.Name, sym);
    }

    public void PushVar(string name, Type type)
    {
        Symbol symbol = new Symbol(name, SymbolKind.Var, SymbolState.Resolved, null)
        {
            Type = type
        };
        localSymbols.Add(symbol);
    }

    public int EnterScope()
    {
        //if (localSymbols.Count <= 0)
        //return 0;

        return localSymbols.Count - 1;
    }

    public void LeaveScope(int ptr)
    {
        int index = ptr + 1;
        int count = localSymbols.Count - (ptr + 1);
        localSymbols.RemoveRange(index, count);
    }

    private bool IsConvertible(Type dest, Type src)
    {
        if (dest == src)
        {
            return true;
        }
        else if (dest.IsArithmetic && src.IsArithmetic)
        {
            return true;
        }

        return false;
    }

    private bool ConvertOperand(Operand operand, Type type)
    {
        if (operand.Type == type)
            return true;

        if (!IsConvertible(operand.Type, type))
        {
            return false;
        }

        if (operand.IsConst)
        {
            #region AutoGenerated from Script GenTypeConvertCode.py
            if (operand.Type == Type.S8)
            {
                if (type == Type.S8)
                {
                    operand.Val.s8 = (sbyte)operand.Val.s8;
                }
                else if (type == Type.S16)
                {
                    operand.Val.s16 = (short)operand.Val.s8;
                }
                else if (type == Type.S32)
                {
                    operand.Val.s32 = (int)operand.Val.s8;
                }
                else if (type == Type.S64)
                {
                    operand.Val.s64 = (long)operand.Val.s8;
                }
                else if (type == Type.U8)
                {
                    operand.Val.u8 = (byte)operand.Val.s8;
                }
                else if (type == Type.U16)
                {
                    operand.Val.u16 = (ushort)operand.Val.s8;
                }
                else if (type == Type.U32)
                {
                    operand.Val.u32 = (uint)operand.Val.s8;
                }
                else if (type == Type.U64)
                {
                    operand.Val.u64 = (ulong)operand.Val.s8;
                }
                else if (type == Type.F32)
                {
                    operand.Val.f32 = (float)operand.Val.s8;
                }
                else if (type == Type.F64)
                {
                    operand.Val.f64 = (double)operand.Val.s8;
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else if (operand.Type == Type.S16)
            {
                if (type == Type.S8)
                {
                    operand.Val.s8 = (sbyte)operand.Val.s16;
                }
                else if (type == Type.S16)
                {
                    operand.Val.s16 = (short)operand.Val.s16;
                }
                else if (type == Type.S32)
                {
                    operand.Val.s32 = (int)operand.Val.s16;
                }
                else if (type == Type.S64)
                {
                    operand.Val.s64 = (long)operand.Val.s16;
                }
                else if (type == Type.U8)
                {
                    operand.Val.u8 = (byte)operand.Val.s16;
                }
                else if (type == Type.U16)
                {
                    operand.Val.u16 = (ushort)operand.Val.s16;
                }
                else if (type == Type.U32)
                {
                    operand.Val.u32 = (uint)operand.Val.s16;
                }
                else if (type == Type.U64)
                {
                    operand.Val.u64 = (ulong)operand.Val.s16;
                }
                else if (type == Type.F32)
                {
                    operand.Val.f32 = (float)operand.Val.s16;
                }
                else if (type == Type.F64)
                {
                    operand.Val.f64 = (double)operand.Val.s16;
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else if (operand.Type == Type.S32)
            {
                if (type == Type.S8)
                {
                    operand.Val.s8 = (sbyte)operand.Val.s32;
                }
                else if (type == Type.S16)
                {
                    operand.Val.s16 = (short)operand.Val.s32;
                }
                else if (type == Type.S32)
                {
                    operand.Val.s32 = (int)operand.Val.s32;
                }
                else if (type == Type.S64)
                {
                    operand.Val.s64 = (long)operand.Val.s32;
                }
                else if (type == Type.U8)
                {
                    operand.Val.u8 = (byte)operand.Val.s32;
                }
                else if (type == Type.U16)
                {
                    operand.Val.u16 = (ushort)operand.Val.s32;
                }
                else if (type == Type.U32)
                {
                    operand.Val.u32 = (uint)operand.Val.s32;
                }
                else if (type == Type.U64)
                {
                    operand.Val.u64 = (ulong)operand.Val.s32;
                }
                else if (type == Type.F32)
                {
                    operand.Val.f32 = (float)operand.Val.s32;
                }
                else if (type == Type.F64)
                {
                    operand.Val.f64 = (double)operand.Val.s32;
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else if (operand.Type == Type.S64)
            {
                if (type == Type.S8)
                {
                    operand.Val.s8 = (sbyte)operand.Val.s64;
                }
                else if (type == Type.S16)
                {
                    operand.Val.s16 = (short)operand.Val.s64;
                }
                else if (type == Type.S32)
                {
                    operand.Val.s32 = (int)operand.Val.s64;
                }
                else if (type == Type.S64)
                {
                    operand.Val.s64 = (long)operand.Val.s64;
                }
                else if (type == Type.U8)
                {
                    operand.Val.u8 = (byte)operand.Val.s64;
                }
                else if (type == Type.U16)
                {
                    operand.Val.u16 = (ushort)operand.Val.s64;
                }
                else if (type == Type.U32)
                {
                    operand.Val.u32 = (uint)operand.Val.s64;
                }
                else if (type == Type.U64)
                {
                    operand.Val.u64 = (ulong)operand.Val.s64;
                }
                else if (type == Type.F32)
                {
                    operand.Val.f32 = (float)operand.Val.s64;
                }
                else if (type == Type.F64)
                {
                    operand.Val.f64 = (double)operand.Val.s64;
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else if (operand.Type == Type.U8)
            {
                if (type == Type.S8)
                {
                    operand.Val.s8 = (sbyte)operand.Val.u8;
                }
                else if (type == Type.S16)
                {
                    operand.Val.s16 = (short)operand.Val.u8;
                }
                else if (type == Type.S32)
                {
                    operand.Val.s32 = (int)operand.Val.u8;
                }
                else if (type == Type.S64)
                {
                    operand.Val.s64 = (long)operand.Val.u8;
                }
                else if (type == Type.U8)
                {
                    operand.Val.u8 = (byte)operand.Val.u8;
                }
                else if (type == Type.U16)
                {
                    operand.Val.u16 = (ushort)operand.Val.u8;
                }
                else if (type == Type.U32)
                {
                    operand.Val.u32 = (uint)operand.Val.u8;
                }
                else if (type == Type.U64)
                {
                    operand.Val.u64 = (ulong)operand.Val.u8;
                }
                else if (type == Type.F32)
                {
                    operand.Val.f32 = (float)operand.Val.u8;
                }
                else if (type == Type.F64)
                {
                    operand.Val.f64 = (double)operand.Val.u8;
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else if (operand.Type == Type.U16)
            {
                if (type == Type.S8)
                {
                    operand.Val.s8 = (sbyte)operand.Val.u16;
                }
                else if (type == Type.S16)
                {
                    operand.Val.s16 = (short)operand.Val.u16;
                }
                else if (type == Type.S32)
                {
                    operand.Val.s32 = (int)operand.Val.u16;
                }
                else if (type == Type.S64)
                {
                    operand.Val.s64 = (long)operand.Val.u16;
                }
                else if (type == Type.U8)
                {
                    operand.Val.u8 = (byte)operand.Val.u16;
                }
                else if (type == Type.U16)
                {
                    operand.Val.u16 = (ushort)operand.Val.u16;
                }
                else if (type == Type.U32)
                {
                    operand.Val.u32 = (uint)operand.Val.u16;
                }
                else if (type == Type.U64)
                {
                    operand.Val.u64 = (ulong)operand.Val.u16;
                }
                else if (type == Type.F32)
                {
                    operand.Val.f32 = (float)operand.Val.u16;
                }
                else if (type == Type.F64)
                {
                    operand.Val.f64 = (double)operand.Val.u16;
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else if (operand.Type == Type.U32)
            {
                if (type == Type.S8)
                {
                    operand.Val.s8 = (sbyte)operand.Val.u32;
                }
                else if (type == Type.S16)
                {
                    operand.Val.s16 = (short)operand.Val.u32;
                }
                else if (type == Type.S32)
                {
                    operand.Val.s32 = (int)operand.Val.u32;
                }
                else if (type == Type.S64)
                {
                    operand.Val.s64 = (long)operand.Val.u32;
                }
                else if (type == Type.U8)
                {
                    operand.Val.u8 = (byte)operand.Val.u32;
                }
                else if (type == Type.U16)
                {
                    operand.Val.u16 = (ushort)operand.Val.u32;
                }
                else if (type == Type.U32)
                {
                    operand.Val.u32 = (uint)operand.Val.u32;
                }
                else if (type == Type.U64)
                {
                    operand.Val.u64 = (ulong)operand.Val.u32;
                }
                else if (type == Type.F32)
                {
                    operand.Val.f32 = (float)operand.Val.u32;
                }
                else if (type == Type.F64)
                {
                    operand.Val.f64 = (double)operand.Val.u32;
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else if (operand.Type == Type.U64)
            {
                if (type == Type.S8)
                {
                    operand.Val.s8 = (sbyte)operand.Val.u64;
                }
                else if (type == Type.S16)
                {
                    operand.Val.s16 = (short)operand.Val.u64;
                }
                else if (type == Type.S32)
                {
                    operand.Val.s32 = (int)operand.Val.u64;
                }
                else if (type == Type.S64)
                {
                    operand.Val.s64 = (long)operand.Val.u64;
                }
                else if (type == Type.U8)
                {
                    operand.Val.u8 = (byte)operand.Val.u64;
                }
                else if (type == Type.U16)
                {
                    operand.Val.u16 = (ushort)operand.Val.u64;
                }
                else if (type == Type.U32)
                {
                    operand.Val.u32 = (uint)operand.Val.u64;
                }
                else if (type == Type.U64)
                {
                    operand.Val.u64 = (ulong)operand.Val.u64;
                }
                else if (type == Type.F32)
                {
                    operand.Val.f32 = (float)operand.Val.u64;
                }
                else if (type == Type.F64)
                {
                    operand.Val.f64 = (double)operand.Val.u64;
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else if (operand.Type == Type.F32)
            {
                if (type == Type.S8)
                {
                    operand.Val.s8 = (sbyte)operand.Val.f32;
                }
                else if (type == Type.S16)
                {
                    operand.Val.s16 = (short)operand.Val.f32;
                }
                else if (type == Type.S32)
                {
                    operand.Val.s32 = (int)operand.Val.f32;
                }
                else if (type == Type.S64)
                {
                    operand.Val.s64 = (long)operand.Val.f32;
                }
                else if (type == Type.U8)
                {
                    operand.Val.u8 = (byte)operand.Val.f32;
                }
                else if (type == Type.U16)
                {
                    operand.Val.u16 = (ushort)operand.Val.f32;
                }
                else if (type == Type.U32)
                {
                    operand.Val.u32 = (uint)operand.Val.f32;
                }
                else if (type == Type.U64)
                {
                    operand.Val.u64 = (ulong)operand.Val.f32;
                }
                else if (type == Type.F32)
                {
                    operand.Val.f32 = (float)operand.Val.f32;
                }
                else if (type == Type.F64)
                {
                    operand.Val.f64 = (double)operand.Val.f32;
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else if (operand.Type == Type.F64)
            {
                if (type == Type.S8)
                {
                    operand.Val.s8 = (sbyte)operand.Val.f64;
                }
                else if (type == Type.S16)
                {
                    operand.Val.s16 = (short)operand.Val.f64;
                }
                else if (type == Type.S32)
                {
                    operand.Val.s32 = (int)operand.Val.f64;
                }
                else if (type == Type.S64)
                {
                    operand.Val.s64 = (long)operand.Val.f64;
                }
                else if (type == Type.U8)
                {
                    operand.Val.u8 = (byte)operand.Val.f64;
                }
                else if (type == Type.U16)
                {
                    operand.Val.u16 = (ushort)operand.Val.f64;
                }
                else if (type == Type.U32)
                {
                    operand.Val.u32 = (uint)operand.Val.f64;
                }
                else if (type == Type.U64)
                {
                    operand.Val.u64 = (ulong)operand.Val.f64;
                }
                else if (type == Type.F32)
                {
                    operand.Val.f32 = (float)operand.Val.f64;
                }
                else if (type == Type.F64)
                {
                    operand.Val.f64 = (double)operand.Val.f64;
                }
                else
                {
                    Debug.Assert(false);
                }
            }
            else
            {
                operand.IsConst = false;
            }
            #endregion
        }

        operand.Type = type;
        return true;
    }

    private void PromoteOperand(Operand operand)
    {
        if (operand.Type is IntType intType)
        {
            switch (intType.Kind)
            {
                case IntKind.U8:
                case IntKind.S8:
                case IntKind.S16:
                case IntKind.U16:
                    ConvertOperand(operand, Type.S32);
                    break;
            }
        }
    }

    private void UnifyArithmeticOperands(Operand left, Operand right)
    {
        if (left.Type == Type.F64)
        {
            ConvertOperand(right, Type.F64);
        }
        else if (right.Type == Type.F64)
        {
            ConvertOperand(left, Type.F64);
        }
        else if (left.Type == Type.F32)
        {
            ConvertOperand(left, Type.F32);
        }
        else if (right.Type == Type.F32)
        {
            ConvertOperand(left, Type.F32);
        }
        else
        {
            PromoteOperand(left);
            PromoteOperand(right);
            if (left.Type != right.Type)
            {
                if (Type.IsTypeSigned(left.Type) == Type.IsTypeSigned(right.Type))
                {
                    if (GetTypeRank(left.Type) <= GetTypeRank(right.Type))
                    {
                        ConvertOperand(left, right.Type);
                    }
                    else
                    {
                        ConvertOperand(right, left.Type);
                    }
                }
                else if (Type.IsTypeSigned(left.Type) && GetTypeRank(right.Type) >= GetTypeRank(left.Type))
                {
                    ConvertOperand(left, right.Type);
                }
                else if (Type.IsTypeSigned(right.Type) && GetTypeRank(left.Type) >= GetTypeRank(right.Type))
                {
                    ConvertOperand(right, left.Type);
                }
                else if (Type.IsTypeSigned(left.Type) && left.Type.Size > right.Type.Size)
                {
                    ConvertOperand(right, left.Type);
                }
                else if (Type.IsTypeSigned(right.Type) && right.Type.Size > left.Type.Size)
                {
                    ConvertOperand(left, right.Type);
                }
                else
                {
                    Type type = Type.GetUnsignedType(Type.IsTypeSigned(left.Type) ? left.Type : right.Type);
                    ConvertOperand(left, type);
                    ConvertOperand(right, type);
                }
            }
        }

        Debug.Assert(left.Type == right.Type);
    }

    private Operand ResolveIdentifierExpr(IdentifierExpr expr)
    {
        Symbol symbol = ResolveName(expr.Value);
        if (symbol.Kind == SymbolKind.Var)
        {
            return OperandLValue(symbol.Type);
        }
        else if (symbol.Kind == SymbolKind.Const)
        {
            return OperandConst(symbol.Type, symbol.Val);
        }
        else if (symbol.Kind == SymbolKind.Func)
        {
            return OperandRValue(symbol.Type);
        }
        else
        {
            Log.Fatal($"{expr.Value} must be a var, const or func", null);
        }

        return null;
    }

    private Operand ResolveBinaryOpExpr(BinaryOpExpr expr)
    {
        Debug.Assert(expr != null);

        Operand left = ResolveExpr(expr.Left);
        Operand right = ResolveExpr(expr.Right);

        UnifyArithmeticOperands(left, right);

        // TODO(patrik): More type checking here and maybe const folding

        if (left.Type != right.Type)
        {
            Log.Fatal("left and right operand of + must have same type", null);
        }

        return OperandRValue(left.Type);
    }

    private Operand ResolveCallExpr(CallExpr expr)
    {
        Debug.Assert(expr != null);

        Operand func = ResolveExpr(expr.Expr);
        if (func.Type is FunctionType type)
        {
            if (expr.Arguments.Count < type.Parameters.Count)
            {
                Log.Fatal("Too few arguments for function call", null);
            }

            if (expr.Arguments.Count > type.Parameters.Count && !type.VarArgs)
            {
                Log.Fatal("Too many arguments for function call", null);
            }

            for (int i = 0; i < type.Parameters.Count; i++)
            {
                Type paramType = type.Parameters[i].Type;
                Operand argument = ResolveExpr(expr.Arguments[i]);
                if (argument.Type != paramType)
                {
                    Log.Fatal($"Function argument type mismatch with argument '{i + 1}'", null);
                }

                if (!ConvertOperand(argument, paramType))
                {
                    Log.Fatal($"Invalid type in function call argument '{i + 1}'", null);
                }
            }

            for (int i = type.Parameters.Count; i < expr.Arguments.Count; i++)
            {
                ResolveExpr(expr.Arguments[i]);
            }

            return OperandRValue(type.ReturnType);
        }
        else
        {
            Log.Fatal("Calling a non-function value", null);
        }

        return null;
    }

    private Operand ResolveIndexExpr(IndexExpr expr)
    {
        Debug.Assert(expr != null);
        return null;
    }

    private Operand ResolveCompoundExpr(CompoundExpr expr, Type expectedType)
    {
        if (expectedType == null && expr.Type == null)
        {
            Log.Fatal("Compound Literal extected a type", null);
        }

        Type type = null;
        if (expr.Type != null)
            type = ResolveTypespec(expr.Type);
        else
            type = expectedType;

        // TODO(patrik): Array type too
        if (!(type is StructType))
        {
            Log.Fatal("Compound literals can only be applied to struct types", null);
        }

        if (type is StructType structType)
        {
            int index = 0;
            for (int i = 0; i < expr.Fields.Count; i++)
            {
                CompoundField field = expr.Fields[i];
                if (field is IndexCompoundField)
                {
                    Log.Fatal("Index Compound fields are not allowed for struct compounds", null);
                }
                else if (field is NameCompoundField nameField)
                {
                    index = structType.GetItemIndex(nameField.Name.Value);
                    if (index == -1)
                    {
                        Log.Fatal("Named field in compound literal dose not exist", null);
                    }
                }

                if (index >= structType.Items.Count)
                {
                    Log.Fatal("Field initializer in struct compound out of range", null);
                }

                Type itemType = structType.Items[index].Type;
                Operand init = ResolveExpectedExpr(field.Init, itemType);

                if (!ConvertOperand(init, itemType))
                {
                    Log.Fatal("Illegal conversion in compound literal initializer", null);
                }

                index++;
            }
        }
        else
        {
            Debug.Assert(false);
        }

        return OperandRValue(type);
    }

    private Operand ResolveFieldExpr(FieldExpr expr)
    {
        Debug.Assert(false);
        return null;
    }

    private Operand ResolveExpectedExpr(Expr expr, Type expectedType)
    {
        /*
        BinaryOpExpr
        IndexExpr
        CompoundExpr
        FieldExpr
         */

        Operand result = null;
        if (expr is IntegerExpr integerExpr)
        {
            Val val = new Val
            {
                u64 = integerExpr.Value
            };

            result = OperandConst(Type.S32, val);
        }
        else if (expr is FloatExpr floatExpr)
        {
            result = OperandRValue(floatExpr.IsFloat ? Type.F32 : Type.F64);
        }
        else if (expr is StringExpr)
        {
            result = OperandRValue(new PtrType(Type.U8));
        }
        else if (expr is IdentifierExpr identExpr)
        {
            result = ResolveIdentifierExpr(identExpr);
        }
        else if (expr is BinaryOpExpr binaryOpExpr)
        {
            result = ResolveBinaryOpExpr(binaryOpExpr);
        }
        else if (expr is CallExpr callExpr)
        {
            result = ResolveCallExpr(callExpr);
        }
        else if (expr is IndexExpr indexExpr)
        {
            result = ResolveIndexExpr(indexExpr);
        }
        else if (expr is CompoundExpr compoundExpr)
        {
            result = ResolveCompoundExpr(compoundExpr, expectedType);
        }
        else if (expr is FieldExpr fieldExpr)
        {
            result = ResolveFieldExpr(fieldExpr);
        }
        else
        {
            Debug.Assert(false);
        }

        return result;
    }

    private Operand ResolveExpr(Expr expr)
    {
        return ResolveExpectedExpr(expr, null);
    }

    private Operand ResolveConstExpr(Expr expr)
    {
        Operand result = ResolveExpr(expr);
        if (!result.IsConst)
        {
            Log.Fatal("Expected const expr", null);
        }

        return result;
    }

    private Type ResolveTypespec(Typespec typespec)
    {
        if (typespec is IdentifierTypespec identTypespec)
        {
            Symbol symbol = ResolveName(identTypespec.Value.Value);
            return symbol.Type;
        }
        else if (typespec is PtrTypespec ptrTypespec)
        {
            PtrType result = new PtrType(ResolveTypespec(ptrTypespec.Type));
            return result;
        }
        else if (typespec is ArrayTypespec arrayTypespec)
        {
            int size = 0;
            if (arrayTypespec.Size != null)
            {
                Operand operand = ResolveConstExpr(arrayTypespec.Size);

                if (!operand.Type.IsInteger)
                {
                    Log.Fatal("Array size must be a integer", null);
                }

                ConvertOperand(operand, Type.S32);
                size = operand.Val.s32;
                if (size <= 0)
                {
                    Log.Fatal("Array size cant be negative", null);
                }
            }

            ArrayType result = new ArrayType(ResolveTypespec(arrayTypespec.Type), (ulong)size);
            return result;
        }
        else
        {
            Debug.Assert(false);
        }

        return null;
    }

    private Type ResolveVarDecl(VarDecl decl)
    {
        Type type = ResolveTypespec(decl.Type);

        if (decl.Value != null)
        {
            Operand expr = ResolveExpectedExpr(decl.Value, type);
            if (expr.Type != type)
            {
                Log.Fatal("Var type value mismatch", null);
            }
        }

        return type;
    }

    private Type ResolveConstDecl(ConstDecl decl)
    {
        Type type = ResolveTypespec(decl.Type);

        if (decl.Value != null)
        {
            Operand expr = ResolveConstExpr(decl.Value);
            if (expr.Type != type)
            {
                Log.Fatal("Var type value mismatch", null);
            }
        }

        return type;
    }

    private Type ResolveFuncDecl(FunctionDecl decl)
    {
        Type returnType = Type.Void;
        if (decl.ReturnType != null)
            returnType = ResolveTypespec(decl.ReturnType);

        List<FunctionParameterType> parameters = new List<FunctionParameterType>();
        foreach (FunctionParameter param in decl.Parameters)
        {
            string name = param.Name;
            Type type = ResolveTypespec(param.Type);
            parameters.Add(new FunctionParameterType(name, type));
        }

        return new FunctionType(parameters, returnType, decl.VarArgs);
    }

    private Type ResolveStructDecl(StructDecl decl)
    {
        Debug.Assert(decl != null);

        List<StructItemType> items = new List<StructItemType>();
        foreach (StructItem item in decl.Items)
        {
            string name = item.Name;
            Type type = ResolveTypespec(item.Type);

            items.Add(new StructItemType(name, type));
        }

        return new StructType(items);
    }

    public void ResolveSymbol(Symbol symbol)
    {
        if (symbol.State == SymbolState.Resolved)
        {
            return;
        }

        if (symbol.State == SymbolState.Resolving)
        {
            Log.Fatal("Cyclic Dependency", null);
            return;
        }

        symbol.State = SymbolState.Resolving;

        if (symbol.Decl is VarDecl varDecl)
        {
            symbol.Type = ResolveVarDecl(varDecl);
        }
        else if (symbol.Decl is ConstDecl constDecl)
        {
            symbol.Type = ResolveConstDecl(constDecl);
        }
        else if (symbol.Decl is FunctionDecl funcDecl)
        {
            symbol.Type = ResolveFuncDecl(funcDecl);
        }
        else if (symbol.Decl is StructDecl structDecl)
        {
            symbol.Type = ResolveStructDecl(structDecl);
        }
        else
        {
            Debug.Assert(false);
        }

        symbol.State = SymbolState.Resolved;
        ResolvedSymbols.Add(symbol);
    }

    public Symbol ResolveName(string name)
    {
        Symbol sym = GetSymbol(name);
        if (sym == null)
        {
            Log.Fatal($"Unknown symbol name: '{name}'", null);
        }

        ResolveSymbol(sym);

        return sym;
    }

    public void ResolveSymbols()
    {
        foreach (var item in globalSymbols)
        {
            ResolveSymbol(item.Value);
        }

        // TODO(patrik): Finalize Symbols here??
    }

    private void ResolveIfStmt(IfStmt stmt)
    {
        Debug.Assert(stmt != null);
        Debug.Assert(false);
    }

    private void ResolveForStmt(ForStmt stmt)
    {
        Debug.Assert(stmt != null);
        Debug.Assert(false);
    }

    private void ResolveWhileStmt(WhileStmt stmt)
    {
        Debug.Assert(stmt != null);
        Debug.Assert(false);
    }

    private void ResolveDoWhileStmt(DoWhileStmt stmt)
    {
        Debug.Assert(stmt != null);
        Debug.Assert(false);
    }

    private void ResolveReturnStmt(ReturnStmt stmt, Type returnType)
    {
        Debug.Assert(stmt != null);

        Operand expr = ResolveExpectedExpr(stmt.Value, returnType);
        if (expr.Type != returnType)
        {
            Log.Fatal("Return type mismatch", null);
        }
    }

    private void ResolveExprStmt(ExprStmt stmt)
    {
        Debug.Assert(stmt != null);

        if (stmt.Expr is CallExpr)
        {
            _ = ResolveExpr(stmt.Expr);
        }
        else
        {
            Log.Fatal("Only call expressions can be used a statement", null);
        }
    }

    private void ResolveDeclStmt(DeclStmt stmt)
    {
        Debug.Assert(stmt != null);

        if (stmt.Decl is VarDecl varDecl)
        {
            Type type = ResolveTypespec(varDecl.Type);
            Operand expr = ResolveExpectedExpr(varDecl.Value, type);
            if (expr.Type != expr.Type)
            {
                Log.Error("'Var decl stmt' value type mismatch", null);
            }

            PushVar(varDecl.Name, type);
        }
        else
        {
            Log.Fatal("Var decls is the only supported decl in stmts", null);
        }
    }

    private void ResolveStmt(Stmt stmt, Type returnType)
    {
        /*
        IfStmt
        ForStmt
        WhileStmt
        DoWhileStmt
        ContinueStmt
        BreakStmt
         */

        if (stmt is StmtBlock stmtBlock)
        {
            ResolveStmtBlock(stmtBlock, returnType);
        }
        else if (stmt is IfStmt ifStmt)
        {
            ResolveIfStmt(ifStmt);
        }
        else if (stmt is ForStmt forStmt)
        {
            ResolveForStmt(forStmt);
        }
        else if (stmt is WhileStmt whileStmt)
        {
            ResolveWhileStmt(whileStmt);
        }
        else if (stmt is DoWhileStmt doWhileStmt)
        {
            ResolveDoWhileStmt(doWhileStmt);
        }
        else if (stmt is ReturnStmt returnStmt)
        {
            ResolveReturnStmt(returnStmt, returnType);
        }
        else if (stmt is ContinueStmt) { }
        else if (stmt is BreakStmt) { }
        else if (stmt is ExprStmt exprStmt)
        {
            ResolveExprStmt(exprStmt);
        }
        else if (stmt is DeclStmt declStmt)
        {
            ResolveDeclStmt(declStmt);
        }
        else
        {
            Debug.Assert(false);
        }
    }

    private void ResolveStmtBlock(StmtBlock block, Type returnType)
    {
        int scope = EnterScope();

        foreach (Stmt stmt in block.Stmts)
        {
            ResolveStmt(stmt, returnType);
        }

        LeaveScope(scope);
    }

    private void ResolveFuncBody(Symbol symbol)
    {
        Debug.Assert(symbol != null);
        Debug.Assert(symbol.Kind == SymbolKind.Func);
        Debug.Assert(symbol.Decl is FunctionDecl);
        Debug.Assert(symbol.State == SymbolState.Resolved);
        Debug.Assert(symbol.Type is FunctionType);

        FunctionDecl decl = (FunctionDecl)symbol.Decl;
        FunctionType type = (FunctionType)symbol.Type;

        Debug.Assert(decl.Body != null);

        int scope = EnterScope();

        foreach (FunctionParameterType param in type.Parameters)
        {
            PushVar(param.Name, param.Type);
        }

        ResolveStmtBlock(decl.Body, type.ReturnType);

        LeaveScope(scope);
    }

    public void FinalizeSymbols()
    {
        // TODO(patrik): Resolve the function bodies
        foreach (Symbol symbol in ResolvedSymbols)
        {
            if (symbol.Kind == SymbolKind.Func)
            {
                FunctionDecl decl = (FunctionDecl)symbol.Decl;
                if (decl.Body != null)
                {
                    Console.WriteLine($"Resolve {decl.Name}");
                    ResolveFuncBody(symbol);
                }
            }
        }
    }

    public static void Test()
    {
        Resolver resolver = new Resolver();

        Typespec typespec = new ArrayTypespec(new IdentifierTypespec(new IdentifierExpr("s32")), new IntegerExpr(123));
        Type type = resolver.ResolveTypespec(typespec);

        Val val = new Val
        {
            u32 = 3
        };

        Operand operand = resolver.OperandConst(Type.U32, val);
        resolver.ConvertOperand(operand, Type.F32);

        Operand op1 = resolver.OperandRValue(Type.U64);
        Operand op2 = resolver.OperandRValue(Type.U16);
        resolver.UnifyArithmeticOperands(op1, op2);

        Lexer lexer = new Lexer("ResolverTest", "");
        Parser parser = new Parser(lexer);

        string[] code = new string[]
        {
            "struct R { h: s32; j: s32; }",
            "struct T { a: R; b: s64; c: s32; }",
            "var a: T = { { 1, 2 }, 2, 3 };",
            "var b: s32[4];",
            "func test(a: s32, ...);",
            "func add(val: T) -> s32 { var testVar: s32 = 321; test(3, 3.14f); ret testVar + 123; }",
        };

        foreach (string c in code)
        {
            lexer.Reset(c);
            lexer.NextToken();

            Decl decl = parser.ParseDecl();
            resolver.AddSymbol(decl);
        }

        resolver.ResolveSymbols();
        resolver.FinalizeSymbols();
    }
}
