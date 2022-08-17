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

#[derive(Clone)]
pub enum TyKind {
    Int(IntKind),
    Ptr(TyId),

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

        if let TyKind::Function {
            params,
            return_type,
        } = left.kind()
        {
            let left_params = params;
            let left_return_type = return_type;

            if let TyKind::Function {
                params,
                return_type,
            } = right.kind()
            {
                if left_params.len() != params.len() {
                    return false;
                }

                if left_return_type != return_type {
                    return false;
                }

                for i in 0..left_params.len() {
                    if left_params[i] != params[i] {
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
    }
}
