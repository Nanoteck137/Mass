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

    private Dictionary<Type, int> typeRank;

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

        /*
        [TYPE_CHAR] = 1,
        [TYPE_SCHAR] = 1,
        [TYPE_UCHAR] = 1,
        [TYPE_SHORT] = 2,
        [TYPE_USHORT] = 2,
        [TYPE_INT] = 3,
        [TYPE_UINT] = 3,
        [TYPE_LONG] = 4,
        [TYPE_ULONG] = 4,
        [TYPE_LONGLONG] = 5,
        [TYPE_ULONGLONG] = 5,
         */

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

    private void ConvertOperand(Operand operand, Type type)
    {
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

    private Type UnsignedType(Type type)
    {
        if (type is IntType intType)
        {
            switch (intType.Kind)
            {
                case IntKind.S8:
                case IntKind.U8:
                    return Type.U8;
                case IntKind.S16:
                case IntKind.U16:
                    return Type.U16;
                case IntKind.S32:
                case IntKind.U32:
                    return Type.U32;
                case IntKind.S64:
                case IntKind.U64:
                    return Type.U64;
                default:
                    Debug.Assert(false);
                    return null;
            }
        }
        else
        {
            Debug.Assert(false);
            return null;
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
                    Type type = UnsignedType(Type.IsTypeSigned(left.Type) ? left.Type : right.Type);
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
        else
        {
            Log.Fatal($"{expr.Value} must be a var or const", null);
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
        Debug.Assert(false);

        Debug.Assert(expr != null);

        Operand func = ResolveExpr(expr.Expr);
        /*if (func.Type is FunctionType)
        {

        }*/

        return null;
    }

    private Operand ResolveIndexExpr(IndexExpr expr)
    {
        Debug.Assert(expr != null);
        return null;
    }

    private Operand ResolveExpr(Expr expr)
    {
        /*
        IntegerExpr x
        FloatExpr x
        IdentifierExpr x
        StringExpr x
        BinaryOpExpr
        CallExpr
        IndexExpr
         */
        if (expr is IntegerExpr integerExpr)
        {
            Val val = new Val
            {
                u64 = integerExpr.Value
            };

            return OperandConst(Type.S32, val);
        }
        else if (expr is FloatExpr floatExpr)
        {
            return OperandRValue(floatExpr.IsFloat ? Type.F32 : Type.F64);
        }
        else if (expr is StringExpr)
        {
            return OperandRValue(new PtrType(Type.U8));
        }
        else if (expr is IdentifierExpr identExpr)
        {
            return ResolveIdentifierExpr(identExpr);
        }
        else if (expr is BinaryOpExpr binaryOpExpr)
        {
            return ResolveBinaryOpExpr(binaryOpExpr);
        }
        else if (expr is CallExpr callExpr)
        {
            return ResolveCallExpr(callExpr);
        }
        else if (expr is IndexExpr indexExpr)
        {
            return ResolveIndexExpr(indexExpr);
        }
        else
        {
            Debug.Assert(false);
        }

        return null;
    }

    private Type ResolveTypespec(Typespec typespec)
    {
        /*
         PtrTypespec
         ArrayTypespec
         IdentifierTypespec
         */

        if (typespec is IdentifierTypespec identTypespec)
        {
            Symbol symbol = ResolveName(identTypespec.Value.Value);
            return symbol.Type;
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
            Operand expr = ResolveExpr(decl.Value);
            if (expr.Type != type)
            {
                Log.Fatal("Var type value mismatch", null);
            }
        }

        return type;
    }

    private Type ResolveConstDecl(ConstDecl decl)
    {
        Debug.Assert(false);
        return null;
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

        /*
        VarDecl x
        ConstDecl
        FunctionDecl x
        StructDecl
         */

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
    }

    public static void Test()
    {
        Resolver resolver = new Resolver();

        Val val = new Val();
        val.u32 = 3;

        Operand operand = resolver.OperandConst(Type.U32, val);
        resolver.ConvertOperand(operand, Type.F32);

        Operand op1 = resolver.OperandRValue(Type.U64);
        Operand op2 = resolver.OperandRValue(Type.U16);
        resolver.UnifyArithmeticOperands(op1, op2);

        Lexer lexer = new Lexer("ResolverTest", "");
        Parser parser = new Parser(lexer);

        string[] code = new string[]
        {
            /*"struct T { a: s32; }",
            "func test(a: s32, b: s32) -> s32 {}",
            "var a: T = b;",
            "var b: s32 = 3 + 5;",*/

            "var a: s32 = 3 + 6;"
        };

        foreach (string c in code)
        {
            lexer.Reset(c);
            lexer.NextToken();

            Decl decl = parser.ParseDecl();
            resolver.AddSymbol(decl);
        }

        resolver.ResolveSymbols();
    }
}
