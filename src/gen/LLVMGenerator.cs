using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using LLVMSharp;

class GenStmtBlockInfo
{
    public bool HasBreakStmt { get; set; }
    public bool HasContinueStmt { get; set; }
}

class LLVMGenerator : CodeGenerator, IDisposable
{
    private LLVMModuleRef module;

    private Dictionary<string, LLVMValueRef> globals;
    private Dictionary<string, LLVMValueRef> locals;
    private Dictionary<string, LLVMTypeRef> structTypes;

    // private Symbol currentSymbol;
    private LLVMValueRef currentValuePtr;
    private LLVMBasicBlockRef currentEntryBlock;

    private LLVMBasicBlockRef currentLoopStart;
    private LLVMBasicBlockRef currentLoopEnd;

    private Type prevType;

    public LLVMGenerator(Resolver resolver)
        : base(resolver)
    {
        module = LLVMModuleRef.CreateWithName("NO NAME");
        globals = new Dictionary<string, LLVMValueRef>();
        locals = new Dictionary<string, LLVMValueRef>();
        structTypes = new Dictionary<string, LLVMTypeRef>();
    }

    public void Dispose()
    {
        module.Dispose();
    }

    private LLVMTypeRef GetType(Type type)
    {
        if (type is IntType intType)
        {
            switch (intType.Kind)
            {
                case IntKind.U8:
                case IntKind.S8:
                    return LLVMTypeRef.Int8;
                case IntKind.U16:
                case IntKind.S16:
                    return LLVMTypeRef.Int16;
                case IntKind.U32:
                case IntKind.S32:
                    return LLVMTypeRef.Int32;
                case IntKind.U64:
                case IntKind.S64:
                    return LLVMTypeRef.Int64;
                default:
                    Debug.Assert(false);
                    break;
            }
        }
        else if (type is BoolType boolType)
        {
            return LLVMTypeRef.Int1;
        }
        else if (type is FloatType floatType)
        {
            switch (floatType.Kind)
            {
                case FloatKind.F32:
                    return LLVMTypeRef.Float;
                case FloatKind.F64:
                    return LLVMTypeRef.Double;
                default:
                    Debug.Assert(false);
                    break;
            }
        }
        else if (type is PtrType ptrType)
        {
            return LLVMTypeRef.CreatePointer(GetType(ptrType.Base), 0);
        }
        else if (type is ArrayType arrayType)
        {
            return LLVMTypeRef.CreateArray(GetType(arrayType.Base), (uint)arrayType.Count);
        }
        else if (type is VoidType)
        {
            return LLVMTypeRef.Void;
        }
        else if (type is FunctionType functionType)
        {
            LLVMTypeRef returnType = GetType(functionType.ReturnType);
            LLVMTypeRef[] paramTypes = new LLVMTypeRef[functionType.Parameters.Count];
            for (int i = 0; i < functionType.Parameters.Count; i++)
            {
                paramTypes[i] = GetType(functionType.Parameters[i].Type);
            }

            return LLVMTypeRef.CreateFunction(returnType, paramTypes, functionType.VarArgs);
        }
        else if (type is StructType structType)
        {
            string name = structType.Symbol.Name;
            if (structTypes.ContainsKey(name))
                return structTypes[name];

            LLVMTypeRef result = null;
            if (structType.IsOpaque)
            {
                result = LLVMContextRef.Global.CreateNamedStruct("struct." + name);
            }
            else
            {
                LLVMTypeRef[] items = new LLVMTypeRef[structType.Items.Count];
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = GetType(structType.Items[i].Type);
                }

                result = LLVMContextRef.Global.CreateNamedStruct("struct." + name);
                result.StructSetBody(items, false);
            }

            structTypes[name] = result;
            return result;
        }
        else
        {
            Debug.Assert(false);
        }

        return null;
    }

    private LLVMValueRef GenConstExpr(Expr expr)
    {
        /*
        FloatExpr
        IdentifierExpr
        StringExpr
        BinaryOpExpr
        CallExpr
        IndexExpr
        CompoundExpr
        FieldExpr
         */

        if (expr is IntegerExpr integerExpr)
        {
            LLVMTypeRef type = GetType(integerExpr.ResolvedType);
            return LLVMValueRef.CreateConstInt(type, integerExpr.Value);
        }
        else if (expr is FloatExpr floatExpr)
        {
            Debug.Assert(false);
        }
        else if (expr is StringExpr)
        {
            Debug.Assert(false);
        }
        else if (expr is IdentifierExpr identExpr)
        {
            Debug.Assert(false);
        }
        else if (expr is BinaryOpExpr binaryOpExpr)
        {
            LLVMValueRef left = GenConstExpr(binaryOpExpr.Left);
            LLVMValueRef right = GenConstExpr(binaryOpExpr.Right);

            return LLVMValueRef.CreateConstAdd(left, right);
        }
        else if (expr is CallExpr callExpr)
        {
            Debug.Assert(false);
        }
        else if (expr is IndexExpr indexExpr)
        {
            Debug.Assert(false);
        }
        else if (expr is CompoundExpr compoundExpr)
        {
            if (compoundExpr.ResolvedType is StructType structType)
            {
                LLVMValueRef[] values = new LLVMValueRef[structType.Items.Count];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = LLVMValueRef.CreateConstNull(GetType(structType.Items[i].Type));
                }

                int index = 0;
                for (int i = 0; i < compoundExpr.Fields.Count; i++)
                {
                    //TODO(patrik): CompoundFields
                    CompoundField field = compoundExpr.Fields[i];
                    if (field is NameCompoundField name)
                    {
                        index = structType.GetItemIndex(name.Name.Value);
                        values[index] = GenConstExpr(field.Init);
                    }
                    else
                    {
                        values[index] = GenConstExpr(field.Init);
                    }

                    index++;
                }

                return LLVMValueRef.CreateConstNamedStruct(GetType(structType), values);
            }
            else if (compoundExpr.ResolvedType is ArrayType arrayType)
            {
                LLVMTypeRef elementType = GetType(arrayType.Base);
                LLVMValueRef[] values = new LLVMValueRef[arrayType.Count];

                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = LLVMValueRef.CreateConstNull(elementType);
                }

                int index = 0;
                for (int i = 0; i < compoundExpr.Fields.Count; i++)
                {
                    CompoundField field = compoundExpr.Fields[i];
                    if (field is IndexCompoundField indexField)
                    {
                        //index = structType.GetItemIndex(name.Name.Value);
                        //index = index.Index;
                        IntegerExpr intExpr = (IntegerExpr)indexField.Index;
                        index = (int)intExpr.Value;
                        values[index] = GenConstExpr(field.Init);
                    }
                    else
                    {
                        values[index] = GenConstExpr(field.Init);
                    }

                    index++;
                }

                return LLVMValueRef.CreateConstArray(elementType, values);
            }
            else
            {
                Debug.Assert(false);
            }
        }
        else if (expr is FieldExpr fieldExpr)
        {
            Debug.Assert(false);
        }
        else
        {
            Debug.Assert(false);
        }

        return null;
    }

    private LLVMValueRef GenIntegerOperators(LLVMBuilderRef builder, LLVMValueRef left, LLVMValueRef right, TokenType op, IntType type)
    {
        bool isSigned = Type.IsTypeSigned(type);

        switch (op)
        {
            case TokenType.PLUS:
                if (isSigned)
                    return builder.BuildNSWAdd(left, right);
                else
                    return builder.BuildAdd(left, right);
            case TokenType.MINUS:
                if (isSigned)
                    return builder.BuildNSWSub(left, right);
                else
                    return builder.BuildSub(left, right);
            case TokenType.MULTIPLY:
                if (isSigned)
                    return builder.BuildNSWMul(left, right);
                else
                    return builder.BuildMul(left, right);
            case TokenType.DIVIDE:
                if (isSigned)
                    return builder.BuildSDiv(left, right);
                else
                    return builder.BuildUDiv(left, right);
            case TokenType.MODULO:
                if (isSigned)
                    return builder.BuildSRem(left, right);
                else
                    return builder.BuildURem(left, right);
            case TokenType.EQUAL2:
                return builder.BuildICmp(LLVMIntPredicate.LLVMIntEQ, left, right);
            case TokenType.NOT_EQUAL:
                return builder.BuildICmp(LLVMIntPredicate.LLVMIntNE, left, right);
            case TokenType.GREATER_THEN:
                if (isSigned)
                    return builder.BuildICmp(LLVMIntPredicate.LLVMIntSGT, left, right);
                else
                    return builder.BuildICmp(LLVMIntPredicate.LLVMIntUGT, left, right);
            case TokenType.LESS_THEN:
                if (isSigned)
                    return builder.BuildICmp(LLVMIntPredicate.LLVMIntSLT, left, right);
                else
                    return builder.BuildICmp(LLVMIntPredicate.LLVMIntULT, left, right);
            case TokenType.GREATER_EQUALS:
                if (isSigned)
                    return builder.BuildICmp(LLVMIntPredicate.LLVMIntSGE, left, right);
                else
                    return builder.BuildICmp(LLVMIntPredicate.LLVMIntUGE, left, right);
            case TokenType.LESS_EQUALS:
                if (isSigned)
                    return builder.BuildICmp(LLVMIntPredicate.LLVMIntSLE, left, right);
                else
                    return builder.BuildICmp(LLVMIntPredicate.LLVMIntULE, left, right);

            case TokenType.EQUAL:
                builder.BuildStore(right, left);
                break;
            case TokenType.PLUS_EQUALS:
            {
                LLVMValueRef varValue = builder.BuildLoad(left);
                if (isSigned)
                    right = builder.BuildNSWAdd(left, right);
                else
                    right = builder.BuildAdd(left, right);
                builder.BuildStore(right, left);
                break;
            }
            case TokenType.MINUS_EQUALS:
            {
                LLVMValueRef varValue = builder.BuildLoad(left);
                if (isSigned)
                    right = builder.BuildNSWSub(left, right);
                else
                    right = builder.BuildSub(varValue, right);
                builder.BuildStore(right, left);
                break;
            }
            case TokenType.MULTIPLY_EQUALS:
            {
                LLVMValueRef varValue = builder.BuildLoad(left);
                if (isSigned)
                    right = builder.BuildNSWMul(left, right);
                else
                    right = builder.BuildMul(varValue, right);
                builder.BuildStore(right, left);
                break;
            }
            case TokenType.DIVIDE_EQUALS:
            {
                LLVMValueRef varValue = builder.BuildLoad(left);
                if (isSigned)
                    right = builder.BuildSDiv(left, right);
                else
                    right = builder.BuildUDiv(varValue, right);
                builder.BuildStore(right, left);
                break;
            }
            case TokenType.MODULO_EQUALS:
            {
                LLVMValueRef varValue = builder.BuildLoad(left);
                if (isSigned)
                    right = builder.BuildSRem(left, right);
                else
                    right = builder.BuildURem(varValue, right);
                builder.BuildStore(right, left);
                break;
            }

            default:
                Debug.Assert(false);
                break;
        }

        return null;
    }

    private LLVMValueRef GenPointerOperators(LLVMBuilderRef builder, LLVMValueRef ptr, LLVMValueRef value, TokenType op)
    {
        // TODO(patrik): Change this to platform ptr size
        value = builder.BuildZExt(value, LLVMTypeRef.Int64);

        switch (op)
        {
            case TokenType.PLUS:
                break;
            case TokenType.MINUS:
                // NOTE(patrik): Negate the value +val -> -val
                value = LLVMValueRef.CreateConstSub(LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0), value);
                break;
            default:
                Debug.Assert(false);
                break;
        }

        return builder.BuildInBoundsGEP(ptr, new LLVMValueRef[] { value });
    }

    private LLVMValueRef GenFloatingPointOperators(LLVMBuilderRef builder, LLVMValueRef left, LLVMValueRef right, TokenType op)
    {
        switch (op)
        {
            case TokenType.PLUS:
                return builder.BuildFAdd(left, right);
            case TokenType.MINUS:
                return builder.BuildFSub(left, right);
            case TokenType.MULTIPLY:
                return builder.BuildFMul(left, right);
            case TokenType.DIVIDE:
                return builder.BuildFDiv(left, right);
            case TokenType.MODULO:
                return builder.BuildFRem(left, right);
            case TokenType.EQUAL2:
                return builder.BuildFCmp(LLVMRealPredicate.LLVMRealOEQ, left, right);
            case TokenType.NOT_EQUAL:
                return builder.BuildFCmp(LLVMRealPredicate.LLVMRealONE, left, right);
            case TokenType.GREATER_THEN:
                return builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGT, left, right);
            case TokenType.LESS_THEN:
                return builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLT, left, right);
            case TokenType.GREATER_EQUALS:
                return builder.BuildFCmp(LLVMRealPredicate.LLVMRealOGE, left, right);
            case TokenType.LESS_EQUALS:
                return builder.BuildFCmp(LLVMRealPredicate.LLVMRealOLE, left, right);

            case TokenType.EQUAL:
                builder.BuildStore(right, left);
                break;
            case TokenType.PLUS_EQUALS:
            {
                LLVMValueRef varValue = builder.BuildLoad(left);
                right = builder.BuildFAdd(varValue, right);
                builder.BuildStore(right, left);
                break;
            }
            case TokenType.MINUS_EQUALS:
            {
                LLVMValueRef varValue = builder.BuildLoad(left);
                right = builder.BuildFSub(varValue, right);
                builder.BuildStore(right, left);
                break;
            }
            case TokenType.MULTIPLY_EQUALS:
            {
                LLVMValueRef varValue = builder.BuildLoad(left);
                right = builder.BuildFMul(varValue, right);
                builder.BuildStore(right, left);
                break;
            }
            case TokenType.DIVIDE_EQUALS:
            {
                LLVMValueRef varValue = builder.BuildLoad(left);
                right = builder.BuildFDiv(varValue, right);
                builder.BuildStore(right, left);
                break;
            }
            case TokenType.MODULO_EQUALS:
            {
                LLVMValueRef varValue = builder.BuildLoad(left);
                right = builder.BuildFRem(varValue, right);
                builder.BuildStore(right, left);
                break;
            }
            default:
                Debug.Assert(false);
                break;
        }

        return null;
    }

    private LLVMValueRef GenExpr(LLVMBuilderRef builder, Expr expr, bool load = false)
    {
        if (expr is IntegerExpr integerExpr)
        {
            LLVMTypeRef type = GetType(integerExpr.ResolvedType);
            return LLVMValueRef.CreateConstInt(type, integerExpr.Value);
        }
        else if (expr is FloatExpr floatExpr)
        {
            return LLVMValueRef.CreateConstReal(GetType(floatExpr.ResolvedType), floatExpr.Value);
        }
        else if (expr is StringExpr strExpr)
        {
            return builder.BuildGlobalStringPtr(strExpr.Value, "str");
        }
        else if (expr is IdentifierExpr identExpr)
        {
            LLVMValueRef ptr;
            if (locals.ContainsKey(identExpr.Value))
                ptr = locals[identExpr.Value];
            else
                ptr = globals[identExpr.Value];

            if (load)
                return builder.BuildLoad(ptr);
            else
                return ptr;
        }
        else if (expr is CastExpr castExpr)
        {
            Type srcType = castExpr.Expr.ResolvedType;
            Type destType = castExpr.ResolvedType;

            LLVMValueRef result = null;

            if (srcType is ArrayType arrayType && destType is PtrType ptrType)
            {
                LLVMValueRef ptr = GenExpr(builder, castExpr.Expr);
                LLVMValueRef zero = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0);
                LLVMValueRef elementPtr = builder.BuildGEP(ptr, new LLVMValueRef[] { zero, zero });

                builder.BuildStore(elementPtr, currentValuePtr);
            }
            else if (srcType is FloatType && destType is IntType)
            {
                bool isSigned = Type.IsTypeSigned(destType);

                LLVMValueRef value = GenLoadedExpr(builder, castExpr.Expr);
                if (isSigned)
                    result = builder.BuildFPToSI(value, GetType(destType));
                else
                    result = builder.BuildFPToUI(value, GetType(destType));
            }
            else if (srcType is IntType && destType is FloatType)
            {
                bool isSigned = Type.IsTypeSigned(srcType);

                LLVMValueRef value = GenLoadedExpr(builder, castExpr.Expr);
                if (isSigned)
                    result = builder.BuildSIToFP(value, GetType(destType));
                else
                    result = builder.BuildUIToFP(value, GetType(destType));
            }
            else
            {
                Debug.Assert(false);
            }

            return result;
        }
        else if (expr is BinaryOpExpr binaryOpExpr)
        {
            LLVMValueRef left = GenLoadedExpr(builder, binaryOpExpr.Left);
            LLVMValueRef right = GenLoadedExpr(builder, binaryOpExpr.Right);

            bool isFloatingPoint = false;

            Type leftType = binaryOpExpr.Left.ResolvedType;
            Type rightType = binaryOpExpr.Right.ResolvedType;

            if (leftType is FloatType && rightType is IntType)
            {
                bool isSigned = Type.IsTypeSigned(rightType);
                if (isSigned)
                    right = builder.BuildSIToFP(right, GetType(leftType));
                else
                    right = builder.BuildUIToFP(right, GetType(leftType));
                isFloatingPoint = true;
            }
            else if (rightType is FloatType && leftType is IntType)
            {
                bool isSigned = Type.IsTypeSigned(leftType);
                if (isSigned)
                    left = builder.BuildSIToFP(left, GetType(rightType));
                else
                    left = builder.BuildUIToFP(left, GetType(rightType));

                isFloatingPoint = true;
            }
            else if (leftType is FloatType && rightType is FloatType)
            {
                isFloatingPoint = true;
            }

            LLVMValueRef result;
            if (isFloatingPoint)
            {
                result = GenFloatingPointOperators(builder, left, right, binaryOpExpr.Op);
            }
            else
            {
                if (leftType is PtrType)
                {
                    result = GenPointerOperators(builder, left, right, binaryOpExpr.Op);
                }
                else if (rightType is PtrType)
                {
                    result = GenPointerOperators(builder, right, left, binaryOpExpr.Op);
                }
                else
                {
                    result = GenIntegerOperators(builder, left, right, binaryOpExpr.Op, (IntType)leftType);
                }
            }

            Debug.Assert(result != null);
            return result;
        }
        else if (expr is ModifyExpr modifyExpr)
        {
            LLVMValueRef ptr = GenExpr(builder, modifyExpr.Expr);
            LLVMValueRef val = builder.BuildLoad(ptr);
            LLVMValueRef result = val;

            switch (modifyExpr.Op)
            {
                case TokenType.INC:
                    val = builder.BuildAdd(val, LLVMValueRef.CreateConstInt(GetType(modifyExpr.ResolvedType), 1));
                    if (!modifyExpr.Post)
                    {
                        result = val;
                    }
                    builder.BuildStore(val, ptr);
                    break;
                case TokenType.DEC:
                    val = builder.BuildSub(val, LLVMValueRef.CreateConstInt(GetType(modifyExpr.ResolvedType), 1));
                    if (!modifyExpr.Post)
                    {
                        result = val;
                    }
                    builder.BuildStore(val, ptr);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

            return result;
        }
        else if (expr is UnaryExpr unaryExpr)
        {
            Type type = unaryExpr.ResolvedType;

            LLVMValueRef value = GenLoadedExpr(builder, unaryExpr.Expr);

            switch (unaryExpr.Op)
            {
                case TokenType.MINUS:
                    if (type is IntType)
                    {
                        //TODO(patrik): Integer Signed and unsigned
                        return builder.BuildSub(LLVMValueRef.CreateConstInt(GetType(type), 0), value);
                    }
                    else if (type is FloatType)
                    {
                        return builder.BuildFSub(LLVMValueRef.CreateConstReal(GetType(type), -0.0), value);
                    }
                    else
                    {
                        Debug.Assert(false);
                        return null;
                    }
                default:
                    Debug.Assert(false);
                    break;
            }

            return null;
        }
        else if (expr is CallExpr callExpr)
        {
            LLVMValueRef func = GenExpr(builder, callExpr.Expr);

            LLVMValueRef[] arguments = new LLVMValueRef[callExpr.Arguments.Count];
            for (int i = 0; i < arguments.Length; i++)
            {
                arguments[i] = GenLoadedExpr(builder, callExpr.Arguments[i]);
                if (callExpr.Arguments[i].ResolvedType is FloatType floatType)
                {
                    if (floatType.Kind == FloatKind.F32)
                    {
                        // TODO(patrik): We need to convert float to double if the argument is part of the varargs
                        arguments[i] = builder.BuildFPExt(arguments[i], LLVMTypeRef.Double);
                    }
                }
                else if (callExpr.Arguments[i].ResolvedType is BoolType)
                {
                    arguments[i] = builder.BuildZExt(arguments[i], LLVMTypeRef.Int32);
                }
            }

            return builder.BuildCall(func, arguments);
        }
        else if (expr is SpecialFunctionCallExpr sfCallExpr)
        {
            if (sfCallExpr.Kind == SpecialFunctionKind.Addr)
            {
                LLVMValueRef value = GenExpr(builder, sfCallExpr.Arguments[0]);

                return value;
            }
            else if (sfCallExpr.Kind == SpecialFunctionKind.Deref)
            {
                LLVMValueRef ptr = GenLoadedExpr(builder, sfCallExpr.Arguments[0]);
                LLVMValueRef value = builder.BuildLoad(ptr);

                return value;
            }
            else
            {
                Debug.Assert(false);
            }


        }
        else if (expr is IndexExpr indexExpr)
        {
            LLVMValueRef ptr = GenExpr(builder, indexExpr.Expr);

            LLVMValueRef index = GenLoadedExpr(builder, indexExpr.Index);
            LLVMValueRef elementPtr;
            if (indexExpr.Expr.ResolvedType is ArrayType)
                elementPtr = builder.BuildGEP(ptr, new LLVMValueRef[] { LLVMValueRef.CreateConstInt(LLVMTypeRef.Int32, 0), index });
            else if (indexExpr.ResolvedType is PtrType || prevType is PtrType)
            {
                ptr = builder.BuildLoad(ptr);
                elementPtr = builder.BuildGEP(ptr, new LLVMValueRef[] { index });
            }
            else
            {
                elementPtr = builder.BuildGEP(ptr, new LLVMValueRef[] { index });
            }

            prevType = indexExpr.ResolvedType;

            if (load)
                return builder.BuildLoad(elementPtr);
            else
                return elementPtr;
        }
        else if (expr is CompoundExpr compoundExpr)
        {
            if (compoundExpr.ResolvedType is StructType structType)
            {
                LLVMValueRef[] values = new LLVMValueRef[structType.Items.Count];
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = LLVMValueRef.CreateConstNull(GetType(structType.Items[i].Type));
                }

                int index = 0;
                for (int i = 0; i < compoundExpr.Fields.Count; i++)
                {
                    CompoundField field = compoundExpr.Fields[i];
                    if (field is NameCompoundField name)
                    {
                        index = structType.GetItemIndex(name.Name.Value);
                        values[index] = GenConstExpr(field.Init);
                    }
                    else
                    {
                        values[index] = GenConstExpr(field.Init);
                    }

                    index++;
                }

                return LLVMValueRef.CreateConstNamedStruct(GetType(structType), values);
            }
            else if (compoundExpr.ResolvedType is ArrayType arrayType)
            {
                LLVMTypeRef elementType = GetType(arrayType.Base);
                LLVMValueRef[] values = new LLVMValueRef[arrayType.Count];

                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = LLVMValueRef.CreateConstNull(elementType);
                }

                int index = 0;
                for (int i = 0; i < compoundExpr.Fields.Count; i++)
                {
                    CompoundField field = compoundExpr.Fields[i];
                    if (field is IndexCompoundField indexField)
                    {
                        IntegerExpr intExpr = (IntegerExpr)indexField.Index;
                        index = (int)intExpr.Value;
                        values[index] = GenConstExpr(field.Init);
                    }
                    else
                    {
                        values[index] = GenConstExpr(field.Init);
                    }

                    index++;
                }

                LLVMTypeRef llvmArrayType = GetType(arrayType);
                LLVMValueRef init = LLVMValueRef.CreateConstArray(elementType, values);
                LLVMValueRef varInit = module.AddGlobal(llvmArrayType, "test");
                varInit.Initializer = init;
                varInit.Linkage = LLVMLinkage.LLVMLinkerPrivateLinkage;
                varInit.IsGlobalConstant = true;

                unsafe
                {
                    LLVMValueRef size = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, (ulong)arrayType.Size);
                    LLVM.BuildMemCpy(builder, currentValuePtr, 0, varInit, 0, size);
                }

                return null;
            }
            else
            {
                Debug.Assert(false);
            }
        }
        else if (expr is FieldExpr fieldExpr)
        {
            StructType t = (StructType)fieldExpr.Expr.ResolvedType;
            int index = t.GetItemIndex(fieldExpr.Name.Value);
            LLVMValueRef ptr = GenExpr(builder, fieldExpr.Expr);
            LLVMValueRef fieldPtr = builder.BuildStructGEP(ptr, (uint)index);

            if (load)
                return builder.BuildLoad(fieldPtr);
            else
                return fieldPtr;
        }
        else
        {
            Debug.Assert(false);
        }

        return null;
    }

    private LLVMValueRef GenLoadedExpr(LLVMBuilderRef builder, Expr expr)
    {
        return GenExpr(builder, expr, true);
    }

    private void GenStmt(LLVMBuilderRef builder, Stmt stmt, ref GenStmtBlockInfo info)
    {
        Debug.Assert(stmt != null);

        if (stmt is StmtBlock stmtBlock)
        {
            GenStmtBlockInfo blockInfo;
            GenStmtBlock(builder, stmtBlock, out blockInfo);
        }
        else if (stmt is IfStmt ifStmt)
        {
            LLVMValueRef cond = GenExpr(builder, ifStmt.Cond);

            LLVMBasicBlockRef then = currentEntryBlock.InsertBasicBlock("then");
            then.MoveAfter(currentEntryBlock);

            LLVMBasicBlockRef endif = currentEntryBlock.InsertBasicBlock("endif");
            endif.MoveAfter(then);

            builder.BuildCondBr(cond, then, endif);

            builder.PositionAtEnd(then);


            GenStmtBlockInfo blockInfo;
            GenStmtBlock(builder, ifStmt.ThenBlock, out blockInfo);
            if (!blockInfo.HasBreakStmt && !blockInfo.HasContinueStmt)
                builder.BuildBr(endif);

            builder.PositionAtEnd(endif);

            currentEntryBlock = endif;
        }
        else if (stmt is ForStmt forStmt)
        {
            Debug.Assert(false);
        }
        else if (stmt is WhileStmt whileStmt)
        {
            LLVMBasicBlockRef oldStart = currentLoopStart;
            LLVMBasicBlockRef oldEnd = currentLoopEnd;

            LLVMBasicBlockRef whileBlock = currentEntryBlock.InsertBasicBlock("while");
            whileBlock.MoveAfter(currentEntryBlock);
            currentLoopStart = whileBlock;

            LLVMBasicBlockRef then = currentEntryBlock.InsertBasicBlock("then");
            then.MoveAfter(whileBlock);

            LLVMBasicBlockRef endWhile = currentEntryBlock.InsertBasicBlock("endwhile");
            endWhile.MoveAfter(then);
            currentLoopEnd = endWhile;

            builder.BuildBr(whileBlock);

            builder.PositionAtEnd(whileBlock);
            LLVMValueRef cond = GenExpr(builder, whileStmt.Cond);
            builder.BuildCondBr(cond, then, endWhile);

            builder.PositionAtEnd(then);

            GenStmtBlockInfo blockInfo;
            GenStmtBlock(builder, whileStmt.Block, out blockInfo);

            if (!blockInfo.HasBreakStmt && !blockInfo.HasContinueStmt)
                builder.BuildBr(whileBlock);

            builder.PositionAtEnd(endWhile);

            currentEntryBlock = endWhile;

            currentLoopStart = oldStart;
            currentLoopEnd = oldEnd;
        }
        else if (stmt is DoWhileStmt doWhileStmt)
        {
            LLVMBasicBlockRef then = currentEntryBlock.InsertBasicBlock("then");
            then.MoveAfter(currentEntryBlock);

            LLVMBasicBlockRef whileBlock = currentEntryBlock.InsertBasicBlock("dowhile");
            whileBlock.MoveAfter(then);

            LLVMBasicBlockRef endWhile = currentEntryBlock.InsertBasicBlock("enddowhile");
            endWhile.MoveAfter(then);

            builder.BuildBr(then);

            builder.PositionAtEnd(then);

            GenStmtBlockInfo blockInfo;
            GenStmtBlock(builder, doWhileStmt.Block, out blockInfo);
            if (!blockInfo.HasBreakStmt && !blockInfo.HasContinueStmt)
                builder.BuildBr(whileBlock);

            builder.PositionAtEnd(whileBlock);
            LLVMValueRef cond = GenExpr(builder, doWhileStmt.Cond);
            builder.BuildCondBr(cond, then, endWhile);

            builder.PositionAtEnd(endWhile);

            currentEntryBlock = endWhile;
        }
        else if (stmt is ReturnStmt returnStmt)
        {
            LLVMValueRef value = GenLoadedExpr(builder, returnStmt.Value);
            builder.BuildRet(value);
        }
        else if (stmt is ContinueStmt)
        {
            Debug.Assert(currentLoopStart != null);

            info.HasContinueStmt = true;
            builder.BuildBr(currentLoopStart);
        }
        else if (stmt is BreakStmt)
        {
            Debug.Assert(currentLoopEnd != null);

            info.HasBreakStmt = true;
            builder.BuildBr(currentLoopEnd);
        }
        else if (stmt is AssignStmt assignStmt)
        {
            LLVMValueRef left = GenExpr(builder, assignStmt.Left);
            LLVMValueRef right = GenLoadedExpr(builder, assignStmt.Right);

            bool isFloatingPoint = false;
            if (assignStmt.Left.ResolvedType is FloatType leftType && assignStmt.Right.ResolvedType is IntType)
            {
                isFloatingPoint = true;
                right = builder.BuildUIToFP(right, GetType(leftType));
            }

            if (isFloatingPoint)
            {
                GenFloatingPointOperators(builder, left, right, assignStmt.Op);
            }
            else
            {
                GenIntegerOperators(builder, left, right, assignStmt.Op, (IntType)assignStmt.Left.ResolvedType);
            }

        }
        else if (stmt is ExprStmt exprStmt)
        {
            GenExpr(builder, exprStmt.Expr);
        }
        else if (stmt is DeclStmt declStmt)
        {
            Debug.Assert(declStmt.Decl is VarDecl);
            VarDecl decl = (VarDecl)declStmt.Decl;

            LLVMTypeRef type = GetType(resolver.ResolveTypespec(decl.Type));
            LLVMValueRef ptr = builder.BuildAlloca(type, decl.Name);
            currentValuePtr = ptr;

            if (decl.Value != null)
            {
                LLVMValueRef value = GenLoadedExpr(builder, decl.Value);
                if (value != null)
                    builder.BuildStore(value, ptr);
            }

            currentValuePtr = null;
            locals[decl.Name] = ptr;
        }
        else
        {
            Debug.Assert(false);
        }
    }

    private void GenStmtBlock(LLVMBuilderRef builder, StmtBlock block, out GenStmtBlockInfo info)
    {
        Debug.Assert(block != null);

        info = new GenStmtBlockInfo();

        foreach (Stmt stmt in block.Stmts)
        {
            GenStmt(builder, stmt, ref info);
        }
    }

    private LLVMValueRef GenVarDecl(VarDecl decl, Type varType)
    {
        LLVMTypeRef type = GetType(varType);
        LLVMValueRef varDef = module.AddGlobal(type, decl.Name);
        varDef.IsGlobalConstant = false;
        varDef.IsExternallyInitialized = false;

        if (decl.Value != null)
        {
            varDef.Initializer = GenConstExpr(decl.Value);
        }
        else
        {
            varDef.Linkage = LLVMLinkage.LLVMCommonLinkage;
            varDef.Initializer = LLVMValueRef.CreateConstNull(type);
        }

        return varDef;
    }

    private LLVMValueRef GenConstDecl(ConstDecl decl)
    {
        Debug.Assert(false);
        return null;
    }

    private LLVMValueRef GenFuncDecl(FunctionDecl decl, Type funcType)
    {
        Debug.Assert(decl != null);
        Debug.Assert(funcType != null);
        Debug.Assert(funcType is FunctionType);

        LLVMValueRef func = module.AddFunction(decl.Name, GetType(funcType));
        for (int i = 0; i < decl.Parameters.Count; i++)
        {
            func.Params[i].Name = decl.Parameters[i].Name;
        }

        if (decl.Body != null)
        {
            LLVMBasicBlockRef entry = func.AppendBasicBlock("entry");
            currentEntryBlock = entry;

            LLVMBuilderRef builder = module.Context.CreateBuilder();
            builder.PositionAtEnd(entry);

            currentValuePtr = func;

            FunctionType type = (FunctionType)funcType;
            for (int i = 0; i < type.Parameters.Count; i++)
            {
                FunctionParameterType param = type.Parameters[i];
                LLVMValueRef ptr = builder.BuildAlloca(GetType(param.Type), param.Name);
                builder.BuildStore(func.Params[i], ptr);

                locals.Add(param.Name, ptr);
            }

            GenStmtBlockInfo blockInfo;
            GenStmtBlock(builder, decl.Body, out blockInfo);

            if (type.ReturnType == Type.Void)
            {
                builder.BuildRetVoid();
            }

            locals.Clear();
            currentValuePtr = null;
            currentEntryBlock = null;
        }

        return func;
    }

    private LLVMValueRef GenStructDecl(StructDecl decl, Type structType)
    {
        /*Debug.Assert(decl != null);
        Debug.Assert(structType != null);
        Debug.Assert(structType is StructType);*/

        return null;
    }

    private void GenDecl(Symbol symbol)
    {
        Decl decl = symbol.Decl;

        /*
        ConstDecl
        FunctionDecl
        StructDecl
         */

        if (decl is VarDecl varDecl)
        {
            LLVMValueRef value = GenVarDecl(varDecl, symbol.Type);
            globals[varDecl.Name] = value;
        }
        else if (decl is ConstDecl constDecl)
        {
            LLVMValueRef value = GenConstDecl(constDecl);
            globals[constDecl.Name] = value;
        }
        else if (decl is FunctionDecl functionDecl)
        {
            LLVMValueRef value = GenFuncDecl(functionDecl, symbol.Type);
            globals[functionDecl.Name] = value;
        }
        else if (decl is StructDecl structDecl)
        {
            LLVMValueRef value = GenStructDecl(structDecl, symbol.Type);
            globals[structDecl.Name] = value;
        }
        else
        {
            Debug.Assert(false);
        }
    }

    public override void Generate()
    {
        foreach (Symbol symbol in resolver.ResolvedSymbols)
        {
            GenDecl(symbol);
        }
    }

    public void DebugPrint()
    {
        string str = module.PrintToString();
        Console.WriteLine(str);

        module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
    }

    public void WriteToFile(string fileName)
    {
        string content = module.PrintToString();
        File.WriteAllText(fileName, content);
    }

    public void RunCode()
    {
        LLVMExecutionEngineRef engine = module.CreateExecutionEngine();

        LLVMValueRef t = engine.FindFunction("main");

        string[] args = new string[] { "test.ma", "testarg" };
        engine.RunFunctionAsMain(t, (uint)args.Length, args, new string[] { });
    }

    private static bool initialized = false;

    public static void Setup()
    {
        if (initialized)
            return;

        LLVM.LinkInMCJIT();
        LLVM.InitializeX86TargetMC();
        LLVM.InitializeX86Target();
        LLVM.InitializeX86TargetInfo();
        LLVM.InitializeX86AsmParser();
        LLVM.InitializeX86AsmPrinter();

        initialized = true;
    }

    public static void Test()
    {
        Lexer lexer = new Lexer("LLVM Code Generator Test", "");
        Parser parser = new Parser(lexer);
        Resolver resolver = new Resolver();
        using LLVMGenerator gen = new LLVMGenerator(resolver);

        string[] code = new string[]
        {
            "var a: s32[2][2] = { { 1, 2 }, { 3, 4 } };",
            "struct R { c: s32; d: s32; e: s32; }",
            "struct T { a: R; b: s32; }",
            "var structTest: T = { { 321, 2, 3 }, 4 };",
            "func printf(format: u8*, ...) -> s32;",
            "func test() { printf(\"Before: %d\n\", a[0][0]); a[0][0] = 123; printf(\"After: %d\n\", a[0][0]); }",
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

        gen.Generate();
        gen.DebugPrint();

        Console.WriteLine("----------- RUNNING PROGRAM -----------");
        Setup();

        LLVMExecutionEngineRef engine = gen.module.CreateExecutionEngine();

        LLVMValueRef t = engine.FindFunction("test");
        engine.RunFunction(t, new LLVMGenericValueRef[] { });
    }
}
