#![allow(dead_code)]

use std::collections::HashMap;

use util::P;
use ast::{Ident, Typespec, TypespecKind, Decl, DeclKind, StmtBlock};
use ty::{TyId, Ty, IntKind};

mod ty;

pub struct DefId {
    index: usize,
}

struct BuiltinTypes {
    ty_u8: TyId,
    ty_u16: TyId,
    ty_u32: TyId,
    ty_u64: TyId,

    ty_s8: TyId,
    ty_s16: TyId,
    ty_s32: TyId,
    ty_s64: TyId,

    ty_void: TyId,
}

#[derive(Clone, Debug)]
enum SymbolKind {
    Ty,
    Function,
}

struct Symbol {
    kind: SymbolKind,

    name: Ident,
    ty: TyId,
}

impl Symbol {
    fn ty(name: Ident, ty: TyId) -> Self {
        Self {
            kind: SymbolKind::Ty,
            name,
            ty,
        }
    }

    fn function(name: Ident, ty: TyId) -> Self {
        Self {
            kind: SymbolKind::Function,
            name,
            ty,
        }
    }
}

struct TyResolver<'a> {
    parser_context: &'a parser::Context,
    ty_context: ty::Context,

    builtin_types: BuiltinTypes,
    symbols: HashMap<Ident, Symbol>,
}

impl<'a> TyResolver<'a> {
    fn new(parser_context: &'a mut parser::Context) -> Self {
        let mut ty_context = ty::Context::new();
        let (symbols, builtin_types) =
            Self::create_types_map(&mut ty_context, parser_context);

        Self {
            parser_context,

            ty_context,
            builtin_types,
            symbols,
        }
    }

    fn create_types_map(
        ty_context: &mut ty::Context,
        parser_context: &mut parser::Context,
    ) -> (HashMap<Ident, Symbol>, BuiltinTypes) {
        let mut symbols = HashMap::new();

        let mut add_ty = |n, t| {
            let n = parser_context.add_ident(n);
            let ty = ty_context.add_type(t);
            let sym = Symbol::ty(n, ty);
            symbols.insert(n, sym);

            ty
        };

        let ty_u8 = add_ty("u8", Ty::int(IntKind::U8));
        let ty_u16 = add_ty("u16", Ty::int(IntKind::U16));
        let ty_u32 = add_ty("u32", Ty::int(IntKind::U32));
        let ty_u64 = add_ty("u64", Ty::int(IntKind::U64));

        let ty_s8 = add_ty("s8", Ty::int(IntKind::S8));
        let ty_s16 = add_ty("s16", Ty::int(IntKind::S16));
        let ty_s32 = add_ty("s32", Ty::int(IntKind::S32));
        let ty_s64 = add_ty("s64", Ty::int(IntKind::S64));

        let ty_void = add_ty("void", Ty::void());

        let builtin_types = BuiltinTypes {
            ty_u8,
            ty_u16,
            ty_u32,
            ty_u64,

            ty_s8,
            ty_s16,
            ty_s32,
            ty_s64,

            ty_void,
        };

        (symbols, builtin_types)
    }

    fn add_symbol(&mut self, symbol: Symbol) {
        if let None = self.symbols.get(&symbol.name) {
            self.symbols.insert(symbol.name, symbol);
        } else {
            panic!("Type already exists");
        }
    }

    fn resolve_typespec(&mut self, typespec: &P<Typespec>) -> Option<TyId> {
        match typespec.kind() {
            TypespecKind::Name(ident) => {
                if let Some(sym) = self.symbols.get(ident) {
                    if !matches!(sym.kind, SymbolKind::Ty) {
                        panic!(
                            "'{}' needs to be a type",
                            self.parser_context.ident(sym.name)
                        );
                    }

                    Some(sym.ty)
                } else {
                    None
                }
            }

            TypespecKind::Ptr(base) => {
                let base = self.resolve_typespec(base)?;
                let ptr = self.ty_context.add_type(Ty::ptr(base));
                Some(ptr)
            }
        }
    }

    fn resolve_decl(&mut self, decl: &P<Decl>) -> Option<TyId> {
        match decl.kind() {
            DeclKind::Function {
                params,
                return_type,
                body: _,
            } => {
                let mut ty_params = Vec::with_capacity(params.len());

                for param in params {
                    ty_params.push(self.resolve_typespec(param.ty())?);
                }

                let return_ty = if let Some(return_type) = return_type {
                    self.resolve_typespec(return_type)?
                } else {
                    self.void()
                };

                let ty = self
                    .ty_context
                    .add_type(Ty::function(ty_params, return_ty));
                let sym = Symbol::function(decl.name(), ty);
                self.add_symbol(sym);

                Some(ty)
            }
        }
    }

    fn resolve_decl_body(&mut self, decl: &P<Decl>) {
        match decl.kind() {
            DeclKind::Function {
                params: _,
                return_type: _,
                body,
            } => {
                if let Some(sym) = self.symbols.get(&decl.name()) {
                    assert!(matches!(sym.kind, SymbolKind::Function));

                    // let scope = Scope::new();

                    for _stmt in body.stmts() {
                        // resolve_stmt();
                    }

                    todo!();
                }
            }
        }
    }

    fn symbol_ty(&self, name: Ident) -> Option<TyId> {
        if let Some(sym) = self.symbols.get(&name) {
            Some(sym.ty)
        } else {
            None
        }
    }

    fn debug_print_symbols(&self) {
        for sym in self.symbols.values() {
            let name = self.parser_context.ident(sym.name);
            let kind = match sym.kind {
                SymbolKind::Ty => "type",
                SymbolKind::Function => "function",
            };

            println!("{} ({}) -> {:?}", name, kind, sym.ty);
        }
    }
}

impl<'a> TyResolver<'a> {
    fn u8(&self) -> TyId {
        self.builtin_types.ty_u8
    }

    fn u16(&self) -> TyId {
        self.builtin_types.ty_u16
    }

    fn u32(&self) -> TyId {
        self.builtin_types.ty_u32
    }

    fn u64(&self) -> TyId {
        self.builtin_types.ty_u64
    }

    fn s8(&self) -> TyId {
        self.builtin_types.ty_s8
    }

    fn s16(&self) -> TyId {
        self.builtin_types.ty_s16
    }

    fn s32(&self) -> TyId {
        self.builtin_types.ty_s32
    }

    fn s64(&self) -> TyId {
        self.builtin_types.ty_s64
    }

    fn void(&self) -> TyId {
        self.builtin_types.ty_void
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_lang() {
        let mut parser_context = parser::Context::new();
        let source = r#"
            func add(a: u32, b: u32) -> s32 {
                ret a + b;
            }

            func main(argc: s32, argv: s8**) -> s32 {
                var res: u32 = add(123, 321);

                return 0;
            }
        "#;
        let decls = parser::parse(&mut parser_context, source);

        let mut res = TyResolver::new(&mut parser_context);

        for decl in &decls {
            let ty = res.resolve_decl(decl).expect("Failed to resolve decl");
            assert_eq!(ty, res.symbol_ty(decl.name()).unwrap());
        }

        for decl in &decls {
            res.resolve_decl_body(decl);
        }

        panic!();
    }

    #[test]
    fn test_decl() {
        let mut parser_context = parser::Context::new();
        let u32_ident = parser_context.add_ident("u32");
        let main_ident = parser_context.add_ident("main");

        let mut res = TyResolver::new(&mut parser_context);

        let decl = Decl::function(
            main_ident,
            vec![],
            Some(Typespec::name(u32_ident)),
            StmtBlock::new(),
        );

        let ty = res.resolve_decl(&decl).expect("Failed to resolve func");
        assert_eq!(ty, res.symbol_ty(main_ident).unwrap());
    }

    #[test]
    fn test_typespec() {
        let mut parser_context = parser::Context::new();
        let u32_ident = parser_context.add_ident("u32");

        let mut res = TyResolver::new(&mut parser_context);

        let typespec = Typespec::name(u32_ident);
        let ty = res
            .resolve_typespec(&typespec)
            .expect("Failed to resolve typespec");
        assert_eq!(ty, res.u32());

        let typespec = Typespec::ptr(Typespec::name(u32_ident));
        let ty = res
            .resolve_typespec(&typespec)
            .expect("Failed to resolve typespec");
        assert_eq!(ty, res.ty_context.add_type(Ty::ptr(res.u32())));
        assert_ne!(ty, res.u32());
    }
}
