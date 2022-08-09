#![allow(dead_code)]

use util::P;
use ast::Ident;
use ast::{Typespec, TypespecKind};
use ast::{Decl, DeclKind};
use ast::{StmtBlock};

use typ::{Typ, TypIndex, IntKind};

mod typ;

enum SymbolKind {
    Type,
    Function { decl: usize },
}

#[derive(Copy, Clone, PartialEq, Debug)]
enum SymbolState {
    Unresolved,
    Resolving,
    Resolved { typ: TypIndex },
}

struct Symbol {
    name: Ident,
    kind: SymbolKind,
    state: SymbolState,
}

impl Symbol {
    fn typ(name: Ident) -> P<Symbol> {
        P::new(Box::new(Symbol {
            name,
            kind: SymbolKind::Type,
            state: SymbolState::Unresolved,
        }))
    }

    fn function(name: Ident, decl: usize) -> P<Symbol> {
        P::new(Box::new(Symbol {
            name,
            kind: SymbolKind::Function { decl },
            state: SymbolState::Unresolved,
        }))
    }
}

impl Symbol {
    fn kind(&self) -> &SymbolKind {
        &self.kind
    }
}

#[derive(Copy, Clone, PartialEq, Debug)]
enum SymbolTablePtrKind {
    Global,
    Local,
}

#[derive(Copy, Clone, PartialEq, Debug)]
struct SymbolTablePtr {
    kind: SymbolTablePtrKind,
    index: usize,
}

struct SymbolTable {
    global: Vec<P<Symbol>>,
    local: Vec<P<Symbol>>,
}

impl SymbolTable {
    fn new() -> Self {
        Self {
            global: Vec::new(),
            local: Vec::new(),
        }
    }

    fn add_global_symbol(&mut self, symbol: P<Symbol>) -> SymbolTablePtr {
        let index = self.global.len();
        self.global.push(symbol);

        SymbolTablePtr {
            kind: SymbolTablePtrKind::Global,
            index,
        }
    }

    fn symbol_from_name(&self, name: Ident) -> Option<SymbolTablePtr> {
        for (index, symbol) in self.local.iter().rev().enumerate() {
            if symbol.name == name {
                return Some(SymbolTablePtr {
                    kind: SymbolTablePtrKind::Local,
                    index,
                });
            }
        }

        for (index, symbol) in self.global.iter().enumerate() {
            if symbol.name == name {
                return Some(SymbolTablePtr {
                    kind: SymbolTablePtrKind::Global,
                    index,
                });
            }
        }

        None
    }

    fn symbol_from_ptr(&self, ptr: SymbolTablePtr) -> &P<Symbol> {
        match ptr.kind {
            SymbolTablePtrKind::Global => &self.global[ptr.index],
            SymbolTablePtrKind::Local => &self.local[ptr.index],
        }
    }

    fn symbol_from_ptr_mut(&mut self, ptr: SymbolTablePtr) -> &mut P<Symbol> {
        match ptr.kind {
            SymbolTablePtrKind::Global => &mut self.global[ptr.index],
            SymbolTablePtrKind::Local => &mut self.local[ptr.index],
        }
    }
}

struct Context {
    parser_context: parser::Context,
    type_context: typ::Context,

    decls: Vec<P<Decl>>,
    symbol_table: SymbolTable,

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

            decls: Vec::new(),
            symbol_table: SymbolTable::new(),

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

    fn create_normal_types(&mut self) {
        let ident = self.parser_context.add_ident("u8");
        self.add_global_symbol_type(ident, self.u8());

        let ident = self.parser_context.add_ident("u16");
        self.add_global_symbol_type(ident, self.u16());

        let ident = self.parser_context.add_ident("u32");
        self.add_global_symbol_type(ident, self.u32());

        let ident = self.parser_context.add_ident("u64");
        self.add_global_symbol_type(ident, self.u64());
    }

    fn add_ident(&mut self, s: &str) -> Ident {
        self.parser_context.add_ident(s)
    }

    fn get_ident(&self, ident: Ident) -> &String {
        self.parser_context.get_ident(ident)
    }

    fn add_global_symbol_type(&mut self, name: Ident, typ: TypIndex) {
        let mut symbol = Symbol::typ(name);
        symbol.state = SymbolState::Resolved { typ };

        self.symbol_table.add_global_symbol(symbol);
    }

    fn add_decl(&mut self, decl: P<Decl>) {
        let name = decl.name();

        let decl_index = self.decls.len();
        self.decls.push(decl);

        let symbol = match self.decls[decl_index].kind() {
            DeclKind::Function { .. } => Symbol::function(name, decl_index),
        };

        self.symbol_table.add_global_symbol(symbol);
    }

    fn get_symbol(&self, name: Ident) -> Option<SymbolTablePtr> {
        self.symbol_table.symbol_from_name(name)
    }

    fn resolve_symbol(&mut self, symbol: SymbolTablePtr) {
        let symbol = self.symbol_table.symbol_from_ptr_mut(symbol);

        match symbol.kind {}

        todo!();
    }

    fn resolve_ident(&mut self, ident: Ident) -> &P<Symbol> {
        let symbol = self.get_symbol(ident);
        if let Some(ptr) = symbol {
            let symbol_state = self.symbol_table.symbol_from_ptr(ptr).state;

            if symbol_state == SymbolState::Unresolved {
                self.resolve_symbol(ptr);
            } else if symbol_state == SymbolState::Resolving {
                panic!("Cyclic");
            }

            return self.symbol_table.symbol_from_ptr(ptr);
        } else {
            panic!("No symbol with name: {}", self.get_ident(ident));
        }
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
            if let SymbolKind::Type = symbol.kind() {
                if let SymbolState::Resolved { typ } = symbol.state {
                    typ
                } else {
                    unreachable!();
                }
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
    fn resolve_func_decl() {
        let mut parser_context = parser::Context::new();

        let mut context = Context::new(parser_context);
        context.create_normal_types();

        let body = StmtBlock::new();

        let func_decl =
            Decl::function(context.add_ident("main"), vec![], None, body);
        context.add_decl(func_decl);

        let name = context.add_ident("main");
        let symbol = context.resolve_ident(name);
    }

    #[test]
    fn resolve_typespec_test() {
        let mut parser_context = parser::Context::new();
        let ident_u32 = parser_context.add_ident("u32");

        let mut context = Context::new(parser_context);
        context.create_normal_types();

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
