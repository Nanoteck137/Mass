use super::Context;
use util::P;
use ast::{Decl, DeclKind};
use ast::{Typespec, TypespecKind};
use ast::{StmtBlock, Stmt, StmtKind};
use ast::{Expr, ExprKind, BinaryOp};

const INDENT: &str = "  ";

pub struct Debug {
    indent: usize,
}

impl Debug {
    pub fn new() -> Self {
        Self { indent: 0 }
    }

    fn indent(&mut self) {
        self.indent += 1;
    }

    fn unindent(&mut self) {
        self.indent -= 1;
    }

    fn newline(&self) {
        println!();
        for _ in 0..self.indent {
            print!("{}", INDENT);
        }
    }

    pub fn typespec(
        &mut self,
        parser_context: &Context,
        typespec: &P<Typespec>,
    ) {
        match typespec.kind() {
            TypespecKind::Name(ident) => {
                print!("{}", parser_context.get_ident(*ident))
            }

            TypespecKind::Ptr(base) => {
                print!("(ptr ");
                self.typespec(parser_context, base);
                print!(")");
            }
        }
    }

    pub fn expr(&mut self, parser_context: &Context, expr: &P<Expr>) {
        match expr.kind() {
            ExprKind::Integer(val) => {
                print!("{}", val);
            }

            ExprKind::Ident(ident) => {
                print!("{}", parser_context.get_ident(*ident));
            }

            ExprKind::String(str) => {
                print!("\"{}\"", str);
            }

            ExprKind::Binary { op, left, right } => {
                let op = match op {
                    BinaryOp::Multiply => "mul",
                    BinaryOp::Divide => "div",

                    BinaryOp::Add => "add",
                    BinaryOp::Minus => "sub",

                    BinaryOp::LessThan => "le",
                    BinaryOp::LessThanEqual => "leq",
                    BinaryOp::GreaterThan => "ge",
                    BinaryOp::GreaterThanEqual => "geq",

                    BinaryOp::Equal => "eq",
                    BinaryOp::NotEqual => "neq",

                    BinaryOp::And => "and",
                    BinaryOp::Or => "or",
                };

                print!("({} ", op);
                self.expr(parser_context, left);
                print!(" ");
                self.expr(parser_context, right);
                print!(")");
            }

            ExprKind::Call { expr, args } => {
                print!("(call ");
                self.expr(parser_context, expr);
                for arg in args {
                    print!(" ");
                    self.expr(parser_context, arg);
                }
                print!(")");
            }

            ExprKind::Index { expr, index } => {
                print!("(index ");
                self.expr(parser_context, expr);
                print!(" ");
                self.expr(parser_context, index);
                print!(")");
            }
        }
    }

    pub fn stmt(&mut self, parser_context: &Context, stmt: &P<Stmt>) {
        match stmt.kind() {
            StmtKind::Var { name, typ, expr } => {
                print!("(var {} ", parser_context.get_ident(*name));
                self.typespec(parser_context, typ);

                if let Some(expr) = expr {
                    print!(" ");
                    self.expr(parser_context, expr);
                }

                print!(")");
            }

            StmtKind::Ret(expr) => {
                print!("(ret ");
                self.expr(parser_context, expr);
                print!(")");
            }

            StmtKind::Expr(expr) => {
                print!("(expr ");
                self.expr(parser_context, expr);
                print!(")");
            }
        }
    }

    pub fn stmt_block(
        &mut self,
        parser_context: &Context,
        stmt_block: &StmtBlock,
    ) {
        print!("(block ");
        self.indent();
        for stmt in stmt_block.stmts() {
            self.newline();
            self.stmt(parser_context, stmt);
        }
        self.unindent();
        print!(")");
    }

    pub fn decl(&mut self, parser_context: &Context, decl: &P<Decl>) {
        match decl.kind() {
            DeclKind::Function {
                params,
                return_type,
                body,
            } => {
                print!("(function {} ", parser_context.get_ident(decl.name()));
                print!("(");
                for param in params {
                    print!(" {} ", parser_context.get_ident(param.name()));
                    self.typespec(parser_context, param.typ());
                }
                print!(" ) ");

                if let Some(return_type) = return_type {
                    self.typespec(parser_context, return_type);
                } else {
                    print!("void");
                }

                self.indent();
                self.newline();
                self.stmt_block(parser_context, body);
                self.unindent();

                print!(")");
            }
        }
    }
}
