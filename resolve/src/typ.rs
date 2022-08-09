use util::P;

#[derive(Copy, Clone, PartialEq, Debug)]
pub struct TypIndex {
    index: usize,
}

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
pub enum TypKind {
    Int(IntKind),
    Ptr(TypIndex),

    Function {
        params: Vec<TypIndex>,
        return_type: TypIndex,
    },
}

#[derive(Clone)]
pub struct Typ {
    kind: TypKind,
}

impl Typ {
    pub fn int(kind: IntKind) -> P<Typ> {
        P::new(Box::new(Typ {
            kind: TypKind::Int(kind),
        }))
    }

    pub fn ptr(base: TypIndex) -> P<Typ> {
        P::new(Box::new(Typ {
            kind: TypKind::Ptr(base),
        }))
    }

    pub fn function(params: Vec<TypIndex>, return_type: TypIndex) -> P<Typ> {
        P::new(Box::new(Typ {
            kind: TypKind::Function {
                params,
                return_type,
            },
        }))
    }
}

impl Typ {
    pub fn kind(&self) -> &TypKind {
        &self.kind
    }
}

pub struct Context {
    type_table: Vec<P<Typ>>,
}

impl Context {
    pub fn new() -> Self {
        Self {
            type_table: Vec::new(),
        }
    }

    pub fn add_type(&mut self, typ: P<Typ>) -> TypIndex {
        for (index, t) in self.type_table.iter().enumerate() {
            if self.same_typ(t, &typ) {
                return TypIndex { index };
            }
        }

        let index = self.type_table.len();
        self.type_table.push(typ);
        TypIndex { index }
    }

    pub fn get_type(&self, typ: TypIndex) -> &P<Typ> {
        &self.type_table[typ.index]
    }

    fn same_typ(&self, left: &P<Typ>, right: &P<Typ>) -> bool {
        // Check if both types are integers
        if let TypKind::Int(left_int_kind) = left.kind() {
            if let TypKind::Int(right_int_kind) = right.kind() {
                // Check if the types are the same integer kinds
                return left_int_kind == right_int_kind;
            }
        }

        if let TypKind::Ptr(left_base) = left.kind() {
            if let TypKind::Ptr(right_base) = right.kind() {
                return left_base == right_base;
            }
        }

        if let TypKind::Function {
            params,
            return_type,
        } = left.kind()
        {
            let left_params = params;
            let left_return_type = return_type;

            if let TypKind::Function {
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
