use pest::prec_climber::{Assoc, PrecClimber};
use pest::iterators::{Pair, Pairs};
use pest::Parser;

use util::P;
use ast::{Ident, Typespec, Decl, StmtBlock, FunctionParam, Stmt, Expr, BinaryOp};

mod debug;

#[macro_use]
extern crate pest_derive;

#[macro_use]
extern crate lazy_static;

#[derive(Parser)]
#[grammar = "grammar.pest"]
struct LangParser;

lazy_static! {
    static ref PREC_CLIMBER: PrecClimber<Rule> = {
        use pest::prec_climber::Operator;
        use Assoc::*;
        use Rule::*;

        PrecClimber::new(vec![
            Operator::new(op_or, Left),
            Operator::new(op_and, Left),
            Operator::new(op_equal, Left) | Operator::new(op_notequal, Left),
            Operator::new(op_lessthan, Left) |
                Operator::new(op_lessthanequal, Left) |
                Operator::new(op_greaterthan, Left) |
                Operator::new(op_greaterthanequal, Left),
            Operator::new(op_add, Left) | Operator::new(op_minus, Left),
            Operator::new(op_multiply, Left) | Operator::new(op_divide, Left),
        ])
    };
}

#[derive(Debug)]
pub struct Context {
    ident_table: Vec<String>,
}

impl Context {
    pub fn new() -> Self {
        Self {
            ident_table: Vec::new(),
        }
    }

    pub fn add_ident(&mut self, ident: &str) -> Ident {
        let mut has_ident = None;
        for index in 0..self.ident_table.len() {
            if self.ident_table[index] == ident {
                has_ident = Some(index);
            }
        }

        return if let Some(index) = has_ident {
            Ident::new(index)
        } else {
            let index = self.ident_table.len();
            self.ident_table.push(ident.to_string());
            Ident::new(index)
        };
    }

    pub fn ident(&self, ident: Ident) -> &String {
        &self.ident_table[ident.index()]
    }
}

fn process_base_typespec(
    parser_context: &mut Context,
    base_typespec: Pair<Rule>,
) -> P<Typespec> {
    let base = base_typespec.into_inner().next().unwrap();
    match base.as_rule() {
        Rule::ident => {
            let ident = base.as_str();
            let ident = parser_context.add_ident(ident);
            Typespec::name(ident)
        }

        _ => unimplemented!("{:?}", base.as_rule()),
    }
}

fn process_typespec(
    parser_context: &mut Context,
    typespec: Pair<Rule>,
) -> P<Typespec> {
    let mut inner = typespec.into_inner();
    let base_typespec = inner.next().unwrap();
    let base_typespec = process_base_typespec(parser_context, base_typespec);

    let mut result = base_typespec;
    if let Some(after) = inner.next() {
        match after.as_rule() {
            Rule::pointer => {
                for _ in 0..after.as_str().len() {
                    result = Typespec::ptr(result);
                }
            }

            _ => unimplemented!("{:?}", after.as_rule()),
        }
    }

    result
}

fn create_expr_ast(
    parser_context: &mut Context,
    expr: Pairs<Rule>,
) -> P<Expr> {
    PREC_CLIMBER.climb(
        expr,
        |pair: Pair<Rule>| match pair.as_rule() {
            Rule::bin_op => process_expr(parser_context, pair),

            Rule::integer => {
                let s = pair.as_str();
                Expr::integer(s.parse::<u64>().unwrap())
            }

            Rule::ident => {
                let ident = pair.as_str();
                let ident = parser_context.add_ident(ident);
                Expr::ident(ident)
            }

            Rule::string => {
                let s = pair.as_str();
                let s = &s[1..s.len() - 1];
                Expr::string(String::from(s))
            }

            Rule::expr => process_expr(parser_context, pair),

            Rule::base => {
                let mut inner = pair.into_inner();
                let pair = inner.next().unwrap();

                let expr = process_expr(parser_context, pair);

                if let Some(next) = inner.next() {
                    match next.as_rule() {
                        Rule::func_call => {
                            let mut args = next.into_inner();
                            let args = args.next().unwrap().into_inner();

                            let mut res = Vec::new();
                            for arg in args {
                                let expr = process_expr(parser_context, arg);
                                res.push(expr);
                            }

                            Expr::call(expr, res)
                        }

                        Rule::array_index => {
                            let index = process_expr(
                                parser_context,
                                next.into_inner().next().unwrap(),
                            );

                            Expr::index(expr, index)
                        }

                        _ => unimplemented!("{:?}", next.as_rule()),
                    }
                } else {
                    expr
                }
            }

            _ => unimplemented!("{:?}", pair.as_rule()),
        },
        |left: P<Expr>, op: Pair<Rule>, right: P<Expr>| {
            let op = match op.as_rule() {
                Rule::op_multiply => BinaryOp::Multiply,
                Rule::op_divide => BinaryOp::Divide,

                Rule::op_add => BinaryOp::Add,
                Rule::op_minus => BinaryOp::Minus,

                Rule::op_lessthan => BinaryOp::LessThan,
                Rule::op_lessthanequal => BinaryOp::LessThanEqual,
                Rule::op_greaterthan => BinaryOp::GreaterThan,
                Rule::op_greaterthanequal => BinaryOp::GreaterThanEqual,

                Rule::op_equal => BinaryOp::Equal,
                Rule::op_notequal => BinaryOp::NotEqual,

                Rule::op_and => BinaryOp::And,
                Rule::op_or => BinaryOp::Or,

                _ => unimplemented!("{:?}", op.as_rule()),
            };

            Expr::binary(op, left, right)
        },
    )
}

fn process_expr(parser_context: &mut Context, expr: Pair<Rule>) -> P<Expr> {
    create_expr_ast(parser_context, expr.into_inner())
}

fn process_stmt(parser_context: &mut Context, stmt: Pair<Rule>) -> P<Stmt> {
    let stmt = stmt.into_inner().next().unwrap();

    match stmt.as_rule() {
        Rule::stmt_var => {
            let mut inner = stmt.into_inner();

            let name = inner.next().unwrap().as_str();
            let name = parser_context.add_ident(name);

            let typespec = inner.next().unwrap();
            let typespec = process_typespec(parser_context, typespec);

            let expr = if let Some(expr) = inner.next() {
                let expr = process_expr(parser_context, expr);
                Some(expr)
            } else {
                None
            };

            Stmt::var(name, typespec, expr)
        }

        Rule::stmt_ret => {
            let expr = stmt.into_inner().next().unwrap();
            let expr = process_expr(parser_context, expr);

            Stmt::ret(expr)
        }

        Rule::stmt_expr => {
            let expr = stmt.into_inner().next().unwrap();
            let expr = process_expr(parser_context, expr);

            Stmt::expr(expr)
        }

        _ => unimplemented!("{:?}", stmt.as_rule()),
    }
}

fn process_stmt_list(
    parser_context: &mut Context,
    stmt_list: Pair<Rule>,
) -> StmtBlock {
    let mut result = StmtBlock::new();

    for stmt in stmt_list.into_inner() {
        let stmt = process_stmt(parser_context, stmt);
        result.add_stmt(stmt);
    }

    result
}

fn process_block(
    parser_context: &mut Context,
    block: Pair<Rule>,
) -> StmtBlock {
    process_stmt_list(parser_context, block.into_inner().next().unwrap())
}

fn process_decl_func(
    parser_context: &mut Context,
    func: Pair<Rule>,
) -> P<Decl> {
    let mut inner = func.into_inner();

    let name = inner.next().unwrap().as_str();

    let func_param_list = inner.next().unwrap();
    let mut params = Vec::new();

    for func_param in func_param_list.into_inner() {
        let mut inner = func_param.into_inner();

        let name = inner.next().unwrap().as_str();
        let typespec = inner.next().unwrap();
        let typespec = process_typespec(parser_context, typespec);

        let name = parser_context.add_ident(name);
        params.push(FunctionParam::new(name, typespec));
    }

    let return_type = inner.next().unwrap();

    let return_type = if let Some(typespec) = return_type.into_inner().next() {
        let typespec = process_typespec(parser_context, typespec);
        Some(typespec)
    } else {
        None
    };

    let body = inner.next().unwrap();
    let body = process_block(parser_context, body);

    let name = parser_context.add_ident(name);
    Decl::function(name, params, return_type, body)
}

fn process_decl(parser_context: &mut Context, decl: Pair<Rule>) -> P<Decl> {
    let decl = decl.into_inner().next().unwrap();

    match decl.as_rule() {
        Rule::func_decl => process_decl_func(parser_context, decl),

        _ => unimplemented!("{:?}", decl.as_rule()),
    }
}

pub fn parse(parser_context: &mut Context, input: &str) -> Vec<P<Decl>> {
    let file = LangParser::parse(Rule::file, input)
        .expect("Failed to parse")
        .next()
        .unwrap();

    let mut decls = Vec::new();
    for pair in file.into_inner() {
        if pair.as_rule() == Rule::decl {
            let decl = process_decl(parser_context, pair);
            decls.push(decl);
        }
    }

    let mut debug = debug::Debug::new();
    for decl in &decls {
        debug.decl(parser_context, decl);
        println!();
    }

    decls
}
