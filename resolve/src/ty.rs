use ast::Ident;
use util::P;

#[derive(Copy, Clone, PartialEq, Debug)]
pub struct TyId(usize);

#[derive(Copy, Clone, PartialEq, Debug)]
pub enum IntKind {
    U8,
    U16,
    U32,
    U64,
    S8,
    S16,
    S32,
    S64,
}

#[derive(Copy, Clone, PartialEq, Debug)]
pub struct StructField {
    name: Ident,
    ty: TyId,
}

impl StructField {
    pub fn new(name: Ident, ty: TyId) -> Self {
        Self { name, ty }
    }
}

#[derive(Clone)]
pub enum TyKind {
    Int(IntKind),
    Ptr(TyId),
    Void,

    Array {
        base: TyId,
        count: usize,
    },

    Struct {
        fields: Vec<StructField>,
    },

    Function {
        params: Vec<TyId>,
        return_type: TyId,
    },
}

#[derive(Clone)]
pub struct Ty {
    kind: TyKind,
}

impl Ty {
    pub fn int(kind: IntKind) -> P<Ty> {
        P::new(Box::new(Ty {
            kind: TyKind::Int(kind),
        }))
    }

    pub fn ptr(base: TyId) -> P<Ty> {
        P::new(Box::new(Ty {
            kind: TyKind::Ptr(base),
        }))
    }

    pub fn void() -> P<Ty> {
        P::new(Box::new(Ty { kind: TyKind::Void }))
    }
    pub fn array(base: TyId, count: usize) -> P<Ty> {
        P::new(Box::new(Ty {
            kind: TyKind::Array { base, count },
        }))
    }

    pub fn r#struct(fields: Vec<StructField>) -> P<Ty> {
        P::new(Box::new(Ty {
            kind: TyKind::Struct { fields },
        }))
    }

    pub fn function(params: Vec<TyId>, return_type: TyId) -> P<Ty> {
        P::new(Box::new(Ty {
            kind: TyKind::Function {
                params,
                return_type,
            },
        }))
    }
}

impl Ty {
    pub fn kind(&self) -> &TyKind {
        &self.kind
    }
}

pub struct Context {
    type_table: Vec<P<Ty>>,
}

impl Context {
    pub fn new() -> Self {
        Self {
            type_table: Vec::new(),
        }
    }

    pub fn add_type(&mut self, typ: P<Ty>) -> TyId {
        for (index, t) in self.type_table.iter().enumerate() {
            if self.same_typ(t, &typ) {
                return TyId(index);
            }
        }

        let index = self.type_table.len();
        self.type_table.push(typ);

        TyId(index)
    }

    pub fn get_type(&self, typ: TyId) -> &P<Ty> {
        &self.type_table[typ.0]
    }

    fn same_typ(&self, left: &P<Ty>, right: &P<Ty>) -> bool {
        // Check if both types are integers
        if let TyKind::Int(left_int_kind) = left.kind() {
            if let TyKind::Int(right_int_kind) = right.kind() {
                // Check if the types are the same integer kinds
                return left_int_kind == right_int_kind;
            }
        }

        if let TyKind::Ptr(left_base) = left.kind() {
            if let TyKind::Ptr(right_base) = right.kind() {
                return left_base == right_base;
            }
        }

        if let TyKind::Array {
            base: left_base,
            count: left_count,
        } = left.kind()
        {
            if let TyKind::Array {
                base: right_base,
                count: right_count,
            } = right.kind()
            {
                if left_base != right_base {
                    return false;
                }

                if left_count != right_count {
                    return false;
                }

                return true;
            }
        }

        if let TyKind::Function {
            params: left_params,
            return_type: left_return_type,
        } = left.kind()
        {
            if let TyKind::Function {
                params: right_params,
                return_type: right_return_type,
            } = right.kind()
            {
                if left_params.len() != right_params.len() {
                    return false;
                }

                if left_return_type != right_return_type {
                    return false;
                }

                for i in 0..left_params.len() {
                    if left_params[i] != right_params[i] {
                        return false;
                    }
                }

                return true;
            }
        }

        return false;
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_typ() {
        let mut context = Context::new();
        let u32_typ = context.add_type(Ty::int(IntKind::U32));
        let u64_typ = context.add_type(Ty::int(IntKind::U64));

        assert_eq!(u32_typ, context.add_type(Ty::int(IntKind::U32)));
        assert_ne!(u32_typ, u64_typ);

        let ptr_typ = context.add_type(Ty::ptr(u32_typ));
        assert_eq!(ptr_typ, context.add_type(Ty::ptr(u32_typ)));
        assert_ne!(ptr_typ, context.add_type(Ty::ptr(u64_typ)));
        assert_ne!(ptr_typ, u32_typ);

        let func_typ =
            context.add_type(Ty::function(vec![u32_typ, u64_typ], u32_typ));
        assert_eq!(
            func_typ,
            context.add_type(Ty::function(vec![u32_typ, u64_typ], u32_typ))
        );
        assert_ne!(
            func_typ,
            context.add_type(Ty::function(vec![u32_typ, u64_typ], u64_typ))
        );
        assert_ne!(
            func_typ,
            context.add_type(Ty::function(vec![u64_typ, u64_typ], u32_typ))
        );
        assert_ne!(
            func_typ,
            context.add_type(Ty::function(vec![u64_typ], u32_typ))
        );

        let array_typ = context.add_type(Ty::array(u32_typ, 10));
        assert_eq!(array_typ, context.add_type(Ty::array(u32_typ, 10)));
        assert_ne!(array_typ, context.add_type(Ty::array(u64_typ, 10)));
        assert_ne!(array_typ, context.add_type(Ty::array(u32_typ, 11)));
        assert_ne!(array_typ, context.add_type(Ty::array(u64_typ, 11)));

        let struct_typ = context.add_type(Ty::r#struct(vec![]));
        assert_ne!(struct_typ, context.add_type(Ty::r#struct(vec![])));
    }
}
