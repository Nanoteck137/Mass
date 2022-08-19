use util::P;

#[derive(Copy, Clone, Hash, Eq, PartialEq, Debug)]
pub struct Ident {
    index: usize,
}

impl Ident {
    pub fn new(index: usize) -> Self {
        Self { index }
    }

    pub fn index(&self) -> usize {
        self.index
    }
}

#[derive(Copy, Clone, PartialEq, Debug)]
pub enum BinaryOp {
    Multiply,
    Divide,

    Add,
    Minus,

    LessThan,
    LessThanEqual,
    GreaterThan,
    GreaterThanEqual,

    Equal,
    NotEqual,

    And,
    Or,
}

pub enum ExprKind {
    Integer(u64),
    Ident(Ident),
    String(String),

    Binary {
        op: BinaryOp,
        left: P<Expr>,
        right: P<Expr>,
    },

    Call {
        expr: P<Expr>,
        args: Vec<P<Expr>>,
    },

    Index {
        expr: P<Expr>,
        index: P<Expr>,
    },
}

pub struct Expr {
    kind: ExprKind,
}

impl Expr {
    pub fn integer(value: u64) -> P<Expr> {
        P::new(Box::new(Expr {
            kind: ExprKind::Integer(value),
        }))
    }

    pub fn ident(ident: Ident) -> P<Expr> {
        P::new(Box::new(Expr {
            kind: ExprKind::Ident(ident),
        }))
    }

    pub fn string(string: String) -> P<Expr> {
        P::new(Box::new(Expr {
            kind: ExprKind::String(string),
        }))
    }

    pub fn binary(op: BinaryOp, left: P<Expr>, right: P<Expr>) -> P<Expr> {
        P::new(Box::new(Expr {
            kind: ExprKind::Binary { op, left, right },
        }))
    }

    pub fn call(expr: P<Expr>, args: Vec<P<Expr>>) -> P<Expr> {
        P::new(Box::new(Expr {
            kind: ExprKind::Call { expr, args },
        }))
    }

    pub fn index(expr: P<Expr>, index: P<Expr>) -> P<Expr> {
        P::new(Box::new(Expr {
            kind: ExprKind::Index { expr, index },
        }))
    }
}

impl Expr {
    pub fn kind(&self) -> &ExprKind {
        &self.kind
    }
}

pub enum StmtKind {
    Var {
        name: Ident,
        typ: P<Typespec>,
        expr: Option<P<Expr>>,
    },

    Ret(P<Expr>),

    Expr(P<Expr>),
}

pub struct Stmt {
    kind: StmtKind,
}

impl Stmt {
    pub fn var(
        name: Ident,
        typ: P<Typespec>,
        expr: Option<P<Expr>>,
    ) -> P<Stmt> {
        P::new(Box::new(Stmt {
            kind: StmtKind::Var { name, typ, expr },
        }))
    }

    pub fn ret(expr: P<Expr>) -> P<Stmt> {
        P::new(Box::new(Stmt {
            kind: StmtKind::Ret(expr),
        }))
    }

    pub fn expr(expr: P<Expr>) -> P<Stmt> {
        P::new(Box::new(Stmt {
            kind: StmtKind::Expr(expr),
        }))
    }
}

impl Stmt {
    pub fn kind(&self) -> &StmtKind {
        &self.kind
    }
}

pub struct StmtBlock {
    stmts: Vec<P<Stmt>>,
}

impl StmtBlock {
    pub fn new() -> Self {
        Self { stmts: Vec::new() }
    }

    pub fn add_stmt(&mut self, stmt: P<Stmt>) {
        self.stmts.push(stmt);
    }

    pub fn stmts(&self) -> &[P<Stmt>] {
        &self.stmts
    }
}

pub enum TypespecKind {
    Name(Ident),
    Ptr(P<Typespec>),
}

pub struct Typespec {
    kind: TypespecKind,
}

impl Typespec {
    pub fn name(name: Ident) -> P<Typespec> {
        P::new(Box::new(Typespec {
            kind: TypespecKind::Name(name),
        }))
    }

    pub fn ptr(base: P<Typespec>) -> P<Typespec> {
        P::new(Box::new(Typespec {
            kind: TypespecKind::Ptr(base),
        }))
    }
}

impl Typespec {
    pub fn kind(&self) -> &TypespecKind {
        &self.kind
    }
}

pub struct FunctionParam {
    name: Ident,
    ty: P<Typespec>,
}

impl FunctionParam {
    pub fn new(name: Ident, ty: P<Typespec>) -> Self {
        Self { name, ty }
    }

    pub fn name(&self) -> Ident {
        self.name
    }

    pub fn ty(&self) -> &P<Typespec> {
        &self.ty
    }
}

pub enum DeclKind {
    Function {
        params: Vec<FunctionParam>,
        return_type: Option<P<Typespec>>,
        body: StmtBlock,
    },
}

pub struct Decl {
    name: Ident,
    kind: DeclKind,
}

impl Decl {
    pub fn function(
        name: Ident,
        params: Vec<FunctionParam>,
        return_type: Option<P<Typespec>>,
        body: StmtBlock,
    ) -> P<Decl> {
        P::new(Box::new(Decl {
            name,
            kind: DeclKind::Function {
                params,
                return_type,
                body,
            },
        }))
    }
}

impl Decl {
    pub fn name(&self) -> Ident {
        self.name
    }

    pub fn kind(&self) -> &DeclKind {
        &self.kind
    }
}
