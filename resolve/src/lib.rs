#![allow(dead_code)]

use std::collections::HashMap;

use util::P;
use ast::{Ident, Typespec, TypespecKind};

use typ::{Typ, TypIndex, IntKind};

mod typ;

enum SymbolKind {
    Type(TypIndex),
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
    fn typ(name: Ident, typ: TypIndex) -> P<Symbol> {
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
    type_context: typ::Context,
    symbol_table: HashMap<ast::Ident, P<Symbol>>,

    type_u8: TypIndex,
    type_u16: TypIndex,
    type_u32: TypIndex,
    type_u64: TypIndex,

    type_s8: TypIndex,
    type_s16: TypIndex,
    type_s32: TypIndex,
    type_s64: TypIndex,
}

impl Context {
    fn new(parser_context: parser::Context) -> Self {
        let mut type_context = typ::Context::new();
        let type_u8 = type_context.add_type(Typ::int(IntKind::U8));
        let type_u16 = type_context.add_type(Typ::int(IntKind::U16));
        let type_u32 = type_context.add_type(Typ::int(IntKind::U32));
        let type_u64 = type_context.add_type(Typ::int(IntKind::U64));

        let type_s8 = type_context.add_type(Typ::int(IntKind::S8));
        let type_s16 = type_context.add_type(Typ::int(IntKind::S16));
        let type_s32 = type_context.add_type(Typ::int(IntKind::S32));
        let type_s64 = type_context.add_type(Typ::int(IntKind::S64));

        Self {
            parser_context,
            type_context,
            symbol_table: HashMap::new(),

            type_u8,
            type_u16,
            type_u32,
            type_u64,

            type_s8,
            type_s16,
            type_s32,
            type_s64,
        }
    }

    fn get_ident(&self, ident: Ident) -> &String {
        self.parser_context.get_ident(ident)
    }

    fn add_type_symbol(&mut self, name: Ident, typ: TypIndex) {
        let mut symbol = Symbol::typ(name, typ);
        symbol.state = SymbolState::Resolved;

        self.symbol_table.insert(name, symbol);
    }

    fn resolve_ident(&self, ident: Ident) -> &P<Symbol> {
        self.symbol_table.get(&ident).unwrap()
    }

    fn add_type(&mut self, typ: P<Typ>) -> TypIndex {
        self.type_context.add_type(typ)
    }

    fn u8(&self) -> TypIndex {
        self.type_u8
    }

    fn u16(&self) -> TypIndex {
        self.type_u16
    }

    fn u32(&self) -> TypIndex {
        self.type_u32
    }

    fn u64(&self) -> TypIndex {
        self.type_u64
    }
}

fn resolve_typespec(
    context: &mut Context,
    typespec: &P<Typespec>,
) -> TypIndex {
    match typespec.kind() {
        TypespecKind::Name(ident) => {
            let symbol = context.resolve_ident(*ident);
            if let SymbolKind::Type(typ) = symbol.kind() {
                *typ
            } else {
                panic!("{} is not a type", context.get_ident(*ident));
            }
        }

        TypespecKind::Ptr(base) => {
            let base = resolve_typespec(context, base);
            context.type_context.add_type(Typ::ptr(base))
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
        context.add_type_symbol(ident_u32, context.u32());

        let typespec = Typespec::name(ident_u32);
        let typ = resolve_typespec(&mut context, &typespec);
        assert_eq!(typ, context.u32());

        let typespec = Typespec::ptr(Typespec::name(ident_u32));
        let typ = resolve_typespec(&mut context, &typespec);
        assert_eq!(typ, context.add_type(Typ::ptr(context.u32())));
        assert_ne!(typ, context.u32());
    }

    #[test]
    fn type_test() {
        let parser_context = parser::Context::new();
        let mut context = Context::new(parser_context);

        let ptr = context.add_type(Typ::ptr(context.u32()));

        assert!(ptr == context.add_type(Typ::ptr(context.u32())));
        assert!(ptr != context.add_type(Typ::ptr(context.u16())));
        assert!(ptr != context.add_type(Typ::ptr(ptr)));

        let func_type = context
            .add_type(Typ::function(vec![context.u32()], context.u64()));
        assert_eq!(
            func_type,
            context
                .add_type(Typ::function(vec![context.u32()], context.u64()))
        );
        assert_ne!(
            func_type,
            context
                .add_type(Typ::function(vec![context.u32()], context.u32()))
        );
        assert_ne!(
            func_type,
            context
                .add_type(Typ::function(vec![context.u64()], context.u64()))
        );
        assert_ne!(
            func_type,
            context.add_type(Typ::function(
                vec![context.u64(), context.u64()],
                context.u64()
            ))
        );
    }
}
