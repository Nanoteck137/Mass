#![allow(dead_code)]

use std::collections::HashMap;

use util::P;
use ast::{Ident, Typespec, TypespecKind};

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

#[derive(Clone)]
enum TypKind {
    Int(IntKind),
    Ptr(P<Typ>),

    Function {
        params: Vec<P<Typ>>,
        return_type: P<Typ>,
    },
}

#[derive(Clone)]
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

enum SymbolKind {
    Type(P<Typ>),
    Function,
}

enum SymbolState {
    Unresolved,
    Resolving,
    Resolved,
}

struct Symbol {
    name: Ident,
    kind: SymbolKind,
    state: SymbolState,
}

impl Symbol {
    fn typ(name: Ident, typ: P<Typ>) -> P<Symbol> {
        P::new(Box::new(Symbol {
            name,
            kind: SymbolKind::Type(typ),
            state: SymbolState::Unresolved,
        }))
    }
}

impl Symbol {
    fn kind(&self) -> &SymbolKind {
        &self.kind
    }
}

struct Context {
    parser_context: parser::Context,
    symbol_table: HashMap<ast::Ident, P<Symbol>>,
}

impl Context {
    fn new(parser_context: parser::Context) -> Self {
        Self {
            parser_context,
            symbol_table: HashMap::new(),
        }
    }

    fn add_type(&mut self, name: &str, typ: P<Typ>) {
        let name = self.parser_context.add_ident(name);

        let mut symbol = Symbol::typ(name, typ);
        symbol.state = SymbolState::Resolved;
        self.symbol_table.insert(name, symbol);
    }

    fn get_ident(&self, ident: ast::Ident) -> &String {
        self.parser_context.get_ident(ident)
    }

    fn resolve_ident(&self, ident: Ident) -> &P<Symbol> {
        self.symbol_table.get(&ident).unwrap()
    }
}

fn resolve_typespec(context: &Context, typespec: &P<Typespec>) -> P<Typ> {
    match typespec.kind() {
        TypespecKind::Name(ident) => {
            let symbol = context.resolve_ident(*ident);
            if let SymbolKind::Type(typ) = symbol.kind() {
                typ.clone()
            } else {
                panic!("{} is not a type", context.get_ident(*ident));
            }
        }

        TypespecKind::Ptr(base) => {
            let base = resolve_typespec(context, base);
            Typ::ptr(base)
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn resolve_typespec_test() {
        let mut parser_context = parser::Context::new();
        let ident_u32 = parser_context.add_ident("u32");

        let mut context = Context::new(parser_context);
        context.add_type("u32", Typ::int(IntKind::U32));

        let typespec = Typespec::name(ident_u32);
        let typ = resolve_typespec(&context, &typespec);
        assert!(same_typ(&typ, &Typ::int(IntKind::U32)));

        let typespec = Typespec::ptr(Typespec::name(ident_u32));
        let ptr_typ = resolve_typespec(&context, &typespec);
        assert!(same_typ(&ptr_typ, &Typ::ptr(Typ::int(IntKind::U32))));
        assert!(!same_typ(&ptr_typ, &typ));
    }

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
