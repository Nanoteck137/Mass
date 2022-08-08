use std::collections::HashMap;

use by_address::ByAddress;
use util::P;

#[derive(Copy, Clone, PartialEq, Debug)]
enum IntKind {
    U8,
    U16,
    U32,
    U64,
    S8,
    S16,
    S32,
    S64,
}

enum TypKind {
    Int(IntKind),
    Ptr(P<Typ>),

    Function {
        params: Vec<P<Typ>>,
        return_type: P<Typ>,
    },
}

struct Typ {
    kind: TypKind,
}

impl Typ {
    fn int(kind: IntKind) -> P<Typ> {
        P::new(Box::new(Typ {
            kind: TypKind::Int(kind),
        }))
    }

    fn ptr(base: P<Typ>) -> P<Typ> {
        P::new(Box::new(Typ {
            kind: TypKind::Ptr(base),
        }))
    }

    fn function(params: Vec<P<Typ>>, return_type: P<Typ>) -> P<Typ> {
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

fn same_typ(left: &P<Typ>, right: &P<Typ>) -> bool {
    // Check if both types are integers
    if let TypKind::Int(left_int_kind) = left.kind() {
        if let TypKind::Int(right_int_kind) = right.kind() {
            // Check if the types are the same integer kinds
            return left_int_kind == right_int_kind;
        }
    }

    if let TypKind::Ptr(left_base) = left.kind() {
        if let TypKind::Ptr(right_base) = right.kind() {
            return same_typ(left_base, right_base);
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

            if !same_typ(left_return_type, return_type) {
                return false;
            }

            for i in 0..left_params.len() {
                if !same_typ(&left_params[i], &params[i]) {
                    return false;
                }
            }

            return true;
        }
    }

    return false;
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn type_test() {
        let int_type = Typ::int(IntKind::U32);
        assert!(!same_typ(&int_type, &Typ::int(IntKind::U64)));
        assert!(same_typ(&int_type, &Typ::int(IntKind::U32)));

        let int_ptr_type = Typ::ptr(Typ::int(IntKind::U32));
        assert!(same_typ(&int_ptr_type, &Typ::ptr(Typ::int(IntKind::U32))));
        assert!(!same_typ(&int_ptr_type, &Typ::ptr(Typ::int(IntKind::U16))));
        assert!(!same_typ(
            &int_ptr_type,
            &Typ::ptr(Typ::ptr(Typ::int(IntKind::U16)))
        ));

        let func_type = Typ::function(
            vec![Typ::int(IntKind::U32)],
            Typ::int(IntKind::U64),
        );

        assert!(!same_typ(&func_type, &Typ::int(IntKind::U64)));
        assert!(same_typ(
            &func_type,
            &Typ::function(
                vec![Typ::int(IntKind::U32)],
                Typ::int(IntKind::U64)
            )
        ));

        assert!(!same_typ(
            &func_type,
            &Typ::function(
                vec![Typ::int(IntKind::U32), Typ::int(IntKind::U32)],
                Typ::int(IntKind::U64)
            )
        ));
    }
}

