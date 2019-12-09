using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

using LLVMSharp;

namespace Mass.Compiler
{
    public class GenStmtBlockInfo
    {
        public bool HasBreakStmt { get; set; }
        public bool HasContinueStmt { get; set; }
    }

    public class LLVMGenerator : CodeGenerator, IDisposable
    {
        private LLVMModuleRef module;

        private Dictionary<string, Package> packages;
        private Package currentWorkingPackage;

        private Dictionary<string, LLVMValueRef> globals;
        private Dictionary<string, LLVMValueRef> locals;
        private Dictionary<string, LLVMTypeRef> structTypes;

        private Dictionary<string, LLVMValueRef> tempValues;

        private LLVMValueRef currentValuePtr;
        private LLVMBasicBlockRef currentEntryBlock;

        private LLVMBasicBlockRef currentLoopStart;
        private LLVMBasicBlockRef currentLoopEnd;

        private Type prevType;

        private string currentWorkingNamespace;

        public LLVMGenerator(Package package)
            : base(package)
        {
            module = LLVMModuleRef.CreateWithName(package.Name);

            packages = new Dictionary<string, Package>();
            currentWorkingPackage = null;

            globals = new Dictionary<string, LLVMValueRef>();
            locals = new Dictionary<string, LLVMValueRef>();
            structTypes = new Dictionary<string, LLVMTypeRef>();
            tempValues = new Dictionary<string, LLVMValueRef>();

            currentWorkingNamespace = "";
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
            else if (type is BoolType)
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
                string name = structType.Symbol.QualifiedName;
                if (structTypes.ContainsKey(name))
                    return structTypes[name];

                LLVMTypeRef result;
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
                            right = builder.BuildNSWAdd(varValue, right);
                        else
                            right = builder.BuildAdd(varValue, right);
                        builder.BuildStore(right, left);
                        break;
                    }
                case TokenType.MINUS_EQUALS:
                    {
                        LLVMValueRef varValue = builder.BuildLoad(left);
                        if (isSigned)
                            right = builder.BuildNSWSub(varValue, right);
                        else
                            right = builder.BuildSub(varValue, right);
                        builder.BuildStore(right, left);
                        break;
                    }
                case TokenType.MULTIPLY_EQUALS:
                    {
                        LLVMValueRef varValue = builder.BuildLoad(left);
                        if (isSigned)
                            right = builder.BuildNSWMul(varValue, right);
                        else
                            right = builder.BuildMul(varValue, right);
                        builder.BuildStore(right, left);
                        break;
                    }
                case TokenType.DIVIDE_EQUALS:
                    {
                        LLVMValueRef varValue = builder.BuildLoad(left);
                        if (isSigned)
                            right = builder.BuildSDiv(varValue, right);
                        else
                            right = builder.BuildUDiv(varValue, right);
                        builder.BuildStore(right, left);
                        break;
                    }
                case TokenType.MODULO_EQUALS:
                    {
                        LLVMValueRef varValue = builder.BuildLoad(left);
                        if (isSigned)
                            right = builder.BuildSRem(varValue, right);
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

        public Package TryGetPackage(Expr expr)
        {
            Debug.Assert(expr != null);

            if (expr is IdentifierExpr identExpr)
            {
                if (packages.ContainsKey(identExpr.Value))
                {
                    return packages[identExpr.Value];
                }
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
                LLVMValueRef ptr = null;
                if (locals.ContainsKey(identExpr.Value))
                    ptr = locals[identExpr.Value];
                else
                {
                    if (globals.ContainsKey(currentWorkingNamespace + "." + identExpr.Value))
                    {
                        ptr = globals[currentWorkingNamespace + "." + identExpr.Value];
                    }
                    else
                    {
                        string searchNamespace = currentWorkingNamespace;
                        bool found = false;

                        while (searchNamespace != "")
                        {
                            string symbolName = searchNamespace + "." + identExpr.Value;

                            if (globals.ContainsKey(symbolName))
                            {
                                ptr = globals[symbolName];
                                found = true;
                                break;
                            }

                            int index = searchNamespace.LastIndexOf('.');
                            if (index == -1)
                                break;

                            searchNamespace = searchNamespace.Substring(0, index);
                        }

                        if (!found)
                        {
                            if (globals.ContainsKey(identExpr.Value))
                            {
                                ptr = globals[identExpr.Value];
                            }
                            else
                            {
                                if (tempValues.ContainsKey(identExpr.Value))
                                {
                                    ptr = tempValues[identExpr.Value];
                                }
                                else
                                {
                                    Debug.Assert(false);
                                }
                            }
                        }
                    }
                }

                Debug.Assert(ptr != null);

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

                if (srcType is ArrayType && destType is PtrType)
                {
                    LLVMValueRef ptr = GenExpr(builder, castExpr.Expr);
                    // TODO(patrik): Change int64 to platform size
                    LLVMValueRef zero = LLVMValueRef.CreateConstInt(LLVMTypeRef.Int64, 0);
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
                else if (srcType is IntType srcIntType && destType is IntType destIntType)
                {
                    LLVMValueRef value = GenLoadedExpr(builder, castExpr.Expr);
                    bool isSigned = Type.IsTypeSigned(srcIntType);
                    if ((int)destIntType.Kind > (int)srcIntType.Kind)
                    {
                        if (isSigned)
                            result = builder.BuildSExt(value, GetType(destType));
                        else
                            result = builder.BuildZExt(value, GetType(destType));
                    }
                    else
                    {
                        result = builder.BuildTrunc(value, GetType(destType));
                    }
                }
                else if (srcType is PtrType && destType is PtrType)
                {
                    LLVMValueRef ptr = GenLoadedExpr(builder, castExpr.Expr);
                    result = builder.BuildBitCast(ptr, GetType(destType));
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

                LLVMValueRef result = null;
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
                    else if (leftType is BoolType && rightType is BoolType)
                    {
                        switch (binaryOpExpr.Op)
                        {
                            case TokenType.AND2:
                                {
                                    LLVMBasicBlockRef leftBlock = currentEntryBlock.InsertBasicBlock("land");
                                    LLVMBasicBlockRef rightBlock = currentEntryBlock.InsertBasicBlock("rand");
                                    LLVMBasicBlockRef endBlock = currentEntryBlock.InsertBasicBlock("endand");
                                    leftBlock.MoveAfter(currentEntryBlock);
                                    rightBlock.MoveAfter(leftBlock);
                                    endBlock.MoveAfter(rightBlock);

                                    builder.BuildBr(leftBlock);

                                    builder.PositionAtEnd(leftBlock);
                                    builder.BuildCondBr(left, rightBlock, endBlock);

                                    builder.PositionAtEnd(rightBlock);
                                    builder.BuildBr(endBlock);

                                    builder.PositionAtEnd(endBlock);
                                    LLVMTypeRef type = GetType(leftType);
                                    LLVMValueRef phi = builder.BuildPhi(type);
                                    phi.AddIncoming(new LLVMValueRef[] { LLVMValueRef.CreateConstInt(type, 0), right }, new LLVMBasicBlockRef[] { leftBlock, rightBlock }, 2);

                                    currentEntryBlock = endBlock;

                                    result = phi;
                                }
                                break;

                            case TokenType.OR2:
                                {
                                    LLVMBasicBlockRef leftBlock = currentEntryBlock.InsertBasicBlock("lor");
                                    LLVMBasicBlockRef rightBlock = currentEntryBlock.InsertBasicBlock("ror");
                                    LLVMBasicBlockRef endBlock = currentEntryBlock.InsertBasicBlock("endor");
                                    leftBlock.MoveAfter(currentEntryBlock);
                                    rightBlock.MoveAfter(leftBlock);
                                    endBlock.MoveAfter(rightBlock);

                                    builder.BuildBr(leftBlock);

                                    builder.PositionAtEnd(leftBlock);
                                    builder.BuildCondBr(left, endBlock, rightBlock);

                                    builder.PositionAtEnd(rightBlock);
                                    builder.BuildBr(endBlock);

                                    builder.PositionAtEnd(endBlock);
                                    LLVMTypeRef type = GetType(leftType);
                                    LLVMValueRef phi = builder.BuildPhi(type);
                                    phi.AddIncoming(new LLVMValueRef[] { LLVMValueRef.CreateConstInt(type, 1), right }, new LLVMBasicBlockRef[] { leftBlock, rightBlock }, 2);

                                    currentEntryBlock = endBlock;

                                    result = phi;
                                }
                                break;
                            default:
                                Debug.Assert(false);
                                break;
                        }
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
                        if (type is IntType intType)
                        {
                            if (Type.IsTypeSigned(intType))
                                return builder.BuildNSWSub(LLVMValueRef.CreateConstInt(GetType(type), 0), value);
                            else
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
                    case TokenType.NOT:
                        Debug.Assert(type is BoolType);

                        return builder.BuildXor(value, LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, 1));
                    default:
                        Debug.Assert(false);
                        break;
                }

                return null;
            }
            else if (expr is CallExpr callExpr)
            {
                Debug.Assert(callExpr.Expr.ResolvedType is FunctionType);
                FunctionType funcType = (FunctionType)callExpr.Expr.ResolvedType;
                LLVMValueRef func = GenExpr(builder, callExpr.Expr);

                LLVMValueRef[] arguments = new LLVMValueRef[callExpr.Arguments.Count];
                for (int i = 0; i < arguments.Length; i++)
                {
                    arguments[i] = GenLoadedExpr(builder, callExpr.Arguments[i]);

                    if (i >= funcType.Parameters.Count)
                    {
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
                        else if (callExpr.Arguments[i].ResolvedType is IntType intType)
                        {
                            switch (intType.Kind)
                            {
                                case IntKind.U8:
                                case IntKind.U16:
                                    arguments[i] = builder.BuildZExt(arguments[i], LLVMTypeRef.Int32);
                                    break;
                                case IntKind.S8:
                                case IntKind.S16:
                                    arguments[i] = builder.BuildSExt(arguments[i], LLVMTypeRef.Int32);
                                    break;
                            }
                        }
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
                else if (indexExpr.ResolvedType is IntType)
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
                if (!(fieldExpr.ResolvedType is StructType))
                {
                    // NOTE(patrik): Package Stuff
                    Symbol symbol = fieldExpr.ResolvedType.Symbol;
                    LLVMValueRef value = globals[symbol.QualifiedName];

                    if (load)
                        return builder.BuildLoad(value);
                    else
                        return value;
                }

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
                GenStmtBlock(builder, stmtBlock, out _);
            }
            else if (stmt is IfStmt ifStmt)
            {
                LLVMBasicBlockRef thenBlock = currentEntryBlock.InsertBasicBlock("then");
                LLVMBasicBlockRef elseBlock = currentEntryBlock.InsertBasicBlock("else");
                LLVMBasicBlockRef endBlock = currentEntryBlock.InsertBasicBlock("endif");

                thenBlock.MoveAfter(currentEntryBlock);
                elseBlock.MoveAfter(thenBlock);
                endBlock.MoveAfter(elseBlock);

                List<LLVMBasicBlockRef> elseIfBlocks = new List<LLVMBasicBlockRef>();
                for (int i = 0; i < ifStmt.ElseIfs.Count; i++)
                {
                    LLVMBasicBlockRef block = currentEntryBlock.InsertBasicBlock("elseifcond");
                    elseIfBlocks.Add(block);
                }

                elseIfBlocks.Add(elseBlock);

                LLVMValueRef cond = GenLoadedExpr(builder, ifStmt.Cond);
                builder.BuildCondBr(cond, thenBlock, elseIfBlocks[0]);

                // Then Block
                builder.PositionAtEnd(thenBlock);

                GenStmtBlockInfo blockInfo;
                GenStmtBlock(builder, ifStmt.ThenBlock, out blockInfo);
                if (!blockInfo.HasBreakStmt && !blockInfo.HasContinueStmt)
                    builder.BuildBr(endBlock);

                LLVMBasicBlockRef afterBlock = thenBlock;
                for (int i = 0; i < ifStmt.ElseIfs.Count; i++)
                {
                    ElseIf elseIf = ifStmt.ElseIfs[i];

                    LLVMBasicBlockRef elseIfCondBlock = elseIfBlocks[i];
                    LLVMBasicBlockRef elseIfThenBlock = currentEntryBlock.InsertBasicBlock("elseifthen");

                    elseIfCondBlock.MoveAfter(afterBlock);
                    elseIfThenBlock.MoveAfter(elseIfCondBlock);

                    builder.PositionAtEnd(elseIfCondBlock);
                    LLVMValueRef elseIfCond = GenLoadedExpr(builder, elseIf.Cond);
                    builder.BuildCondBr(elseIfCond, elseIfThenBlock, elseIfBlocks[i + 1]);

                    builder.PositionAtEnd(elseIfThenBlock);

                    GenStmtBlock(builder, elseIf.Block, out blockInfo);
                    if (!blockInfo.HasBreakStmt && !blockInfo.HasContinueStmt)
                        builder.BuildBr(endBlock);

                    afterBlock = elseIfThenBlock;
                }

                builder.PositionAtEnd(elseBlock);
                if (ifStmt.ElseBlock != null)
                {
                    GenStmtBlock(builder, ifStmt.ElseBlock, out blockInfo);
                    if (!blockInfo.HasBreakStmt && !blockInfo.HasContinueStmt)
                        builder.BuildBr(endBlock);
                }
                else
                {
                    builder.BuildBr(endBlock);
                }

                // End If
                builder.PositionAtEnd(endBlock);

                currentEntryBlock = endBlock;
            }
            else if (stmt is InitStmt initStmt)
            {
                LLVMTypeRef type = GetType(initStmt.ResolvedType);
                LLVMValueRef ptr = builder.BuildAlloca(type, initStmt.Name.Value);
                currentValuePtr = ptr;

                if (initStmt.Value != null)
                {
                    LLVMValueRef value = GenLoadedExpr(builder, initStmt.Value);
                    if (value != null)
                        builder.BuildStore(value, ptr);
                }

                currentValuePtr = null;
                locals[initStmt.Name.Value] = ptr;
            }
            else if (stmt is ForStmt forStmt)
            {
                LLVMBasicBlockRef oldStart = currentLoopStart;
                LLVMBasicBlockRef oldEnd = currentLoopEnd;

                LLVMBasicBlockRef condBlock = currentEntryBlock.InsertBasicBlock("for");
                LLVMBasicBlockRef thenBlock = currentEntryBlock.InsertBasicBlock("then");
                LLVMBasicBlockRef nextBlock = currentEntryBlock.InsertBasicBlock("next");
                LLVMBasicBlockRef endBlock = currentEntryBlock.InsertBasicBlock("endfor");

                condBlock.MoveAfter(currentEntryBlock);
                thenBlock.MoveAfter(condBlock);
                nextBlock.MoveAfter(thenBlock);
                endBlock.MoveAfter(nextBlock);

                currentLoopStart = nextBlock;
                currentLoopEnd = endBlock;

                GenStmtBlockInfo initStmtInfo = new GenStmtBlockInfo();
                GenStmt(builder, forStmt.Init, ref initStmtInfo);
                builder.BuildBr(condBlock);

                builder.PositionAtEnd(condBlock);
                LLVMValueRef cond = GenLoadedExpr(builder, forStmt.Cond);
                builder.BuildCondBr(cond, thenBlock, endBlock);

                builder.PositionAtEnd(thenBlock);
                GenStmtBlockInfo blockInfo;
                GenStmtBlock(builder, forStmt.Block, out blockInfo);
                builder.BuildBr(nextBlock);

                builder.PositionAtEnd(nextBlock);
                GenStmtBlockInfo nextStmtInfo = new GenStmtBlockInfo();
                GenStmt(builder, forStmt.Next, ref nextStmtInfo);
                builder.BuildBr(condBlock);

                builder.PositionAtEnd(endBlock);
                currentEntryBlock = endBlock;

                currentLoopStart = oldStart;
                currentLoopEnd = oldEnd;
            }
            else if (stmt is WhileStmt whileStmt)
            {
                LLVMBasicBlockRef oldStart = currentLoopStart;
                LLVMBasicBlockRef oldEnd = currentLoopEnd;

                LLVMBasicBlockRef whileBlock = currentEntryBlock.InsertBasicBlock("while");
                LLVMBasicBlockRef thenBlock = currentEntryBlock.InsertBasicBlock("then");
                LLVMBasicBlockRef endBlock = currentEntryBlock.InsertBasicBlock("endwhile");

                whileBlock.MoveAfter(currentEntryBlock);
                thenBlock.MoveAfter(whileBlock);
                endBlock.MoveAfter(thenBlock);

                currentLoopStart = whileBlock;
                currentLoopEnd = endBlock;

                if (whileStmt.IsDoWhile)
                    builder.BuildBr(thenBlock);
                else
                    builder.BuildBr(whileBlock);

                builder.PositionAtEnd(whileBlock);
                LLVMValueRef cond = GenExpr(builder, whileStmt.Cond);
                builder.BuildCondBr(cond, thenBlock, endBlock);

                builder.PositionAtEnd(thenBlock);

                GenStmtBlock(builder, whileStmt.Block, out GenStmtBlockInfo blockInfo);

                if (!blockInfo.HasBreakStmt && !blockInfo.HasContinueStmt)
                    builder.BuildBr(whileBlock);

                builder.PositionAtEnd(endBlock);

                currentEntryBlock = endBlock;

                currentLoopStart = oldStart;
                currentLoopEnd = oldEnd;
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
                if (assignStmt.Left.ResolvedType is FloatType)
                    isFloatingPoint = true;

                // TODO(patrik): Move this to a method because i have similar everywhare
                if (assignStmt.Left.ResolvedType is FloatType leftType && assignStmt.Right.ResolvedType is IntType)
                {
                    right = builder.BuildUIToFP(right, GetType(leftType));
                }

                if (isFloatingPoint)
                {
                    GenFloatingPointOperators(builder, left, right, assignStmt.Op);
                }
                else
                {
                    Debug.Assert(assignStmt.Left.ResolvedType is IntType);
                    GenIntegerOperators(builder, left, right, assignStmt.Op, (IntType)assignStmt.Left.ResolvedType);
                }

            }
            else if (stmt is ExprStmt exprStmt)
            {
                GenExpr(builder, exprStmt.Expr);
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

        private LLVMValueRef GenFuncDecl(FunctionDecl decl, Symbol symbol, Type funcType)
        {
            Debug.Assert(decl != null);
            Debug.Assert(funcType != null);
            Debug.Assert(funcType is FunctionType);

            string functionName = symbol.QualifiedName;
            if (decl.GetAttribute(typeof(ExternalDeclAttribute)) != null)
            {
                functionName = decl.Name;
            }

            LLVMValueRef func = module.AddFunction(functionName, GetType(funcType));
            for (int i = 0; i < decl.Parameters.Count; i++)
            {
                func.Params[i].Name = decl.Parameters[i].Name;
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

        private void GenFuncBody(FunctionDecl decl, Symbol sym)
        {
            Debug.Assert(decl != null);
            Debug.Assert(sym != null);

            Debug.Assert(sym.Type is FunctionType);

            Debug.Assert(globals.ContainsKey(sym.QualifiedName));
            LLVMValueRef func = globals[sym.QualifiedName];

            if (decl.Body != null)
            {
                LLVMBasicBlockRef entry = func.AppendBasicBlock("entry");
                currentEntryBlock = entry;

                LLVMBuilderRef builder = module.Context.CreateBuilder();
                builder.PositionAtEnd(entry);

                currentValuePtr = func;

                FunctionType type = (FunctionType)sym.Type;
                for (int i = 0; i < type.Parameters.Count; i++)
                {
                    FunctionParameterType param = type.Parameters[i];
                    LLVMValueRef ptr = builder.BuildAlloca(GetType(param.Type), param.Name);
                    builder.BuildStore(func.Params[i], ptr);

                    locals.Add(param.Name, ptr);
                }

                GenStmtBlock(builder, decl.Body, out _);

                if (type.ReturnType == Type.Void)
                {
                    builder.BuildRetVoid();
                }

                locals.Clear();
                currentValuePtr = null;
                currentEntryBlock = null;
            }
        }

        private void GenDecl(Symbol symbol)
        {
            Debug.Assert(symbol != null);
            Debug.Assert(symbol.Decl != null);

            Decl decl = symbol.Decl;

            currentWorkingNamespace = symbol.Namespace;

            /*
            ConstDecl
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
                if (globals.ContainsKey(functionDecl.Name))
                    return;
                LLVMValueRef value = GenFuncDecl(functionDecl, symbol, symbol.Type);

                //globals[functionDecl.Name] = value;
                globals[symbol.QualifiedName] = value;
            }
            else if (decl is StructDecl structDecl)
            {
                LLVMValueRef value = GenStructDecl(structDecl, symbol.Type);
                globals[symbol.QualifiedName] = value;
            }
            else
            {
                Debug.Assert(false);
            }

            currentWorkingNamespace = "";
        }

        public void GeneratePackage(Package package)
        {
            foreach (var import in package.Imports)
            {
                GeneratePackage(import.Value);
            }

            Console.WriteLine($"DEBUG: Generating code for package '{package.Name}'");

            currentWorkingPackage = package;
            foreach (Symbol symbol in package.Resolver.ResolvedSymbols)
            {
                GenDecl(symbol);
            }

            foreach (Symbol symbol in package.Resolver.ResolvedSymbols)
            {
                currentWorkingNamespace = symbol.Namespace;

                CompilationUnit unit = symbol.CompilationUnit;
                Debug.Assert(unit != null);

                foreach (UseDecl use in unit.Uses)
                {
                    string packageName = "";
                    if (use.Name.IndexOf(".") == -1)
                    {
                        packageName = use.Name;
                    }
                    else
                    {
                        string[] parts = use.Name.Split(".");
                        Debug.Assert(parts.Length > 0);

                        packageName = parts[0];
                    }

                    Package import = Package.GetImportPackage(packageName);
                    Symbol[] symbols = import.GetSymbolsFromNamespace(use.Name);

                    foreach (Symbol tempSymbol in symbols)
                    {
                        tempValues.Add(tempSymbol.Name, globals[tempSymbol.QualifiedName]);
                    }
                    //tempSymbols.AddRange(symbols);
                }


                if (symbol.Decl is FunctionDecl funcDecl)
                {
                    GenFuncBody(funcDecl, symbol);
                }
                currentWorkingNamespace = "";

                tempValues.Clear();
            }

            currentWorkingPackage = null;
        }

        public override void Generate()
        {
            GeneratePackage(this.Package);
        }

        public void DebugPrint()
        {
            string str = module.PrintToString();
            Console.WriteLine(str);

            // TODO(patrik): Move this verify
            module.Verify(LLVMVerifierFailureAction.LLVMPrintMessageAction);
        }

        public void WriteToFile(string fileName)
        {
            string content = module.PrintToString();
            File.WriteAllText(fileName, content);
        }

        public void RunCode()
        {
            Symbol symbol = Package.FindSymbol("main");
            Debug.Assert(symbol != null);

            LLVMExecutionEngineRef engine = module.CreateExecutionEngine();
            LLVMValueRef t = engine.FindFunction(symbol.QualifiedName);

            string[] args = new string[] { };
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
    }
}