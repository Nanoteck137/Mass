#[derive(Clone, Debug)]
pub struct P<T: ?Sized> {
    ptr: Box<T>,
}

impl<T: ?Sized> P<T> {
    pub fn new(ptr: Box<T>) -> Self {
        Self { ptr }
    }
}

impl<T: ?Sized> std::ops::Deref for P<T> {
    type Target = T;

    fn deref(&self) -> &T {
        &self.ptr
    }
}

#[derive(Copy, Clone, PartialEq, Debug)]
pub struct Ident {
    index: usize,
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
        typ: Typespec,
        expr: Option<P<Expr>>,
    },

    Expr(P<Expr>),
}

pub struct Stmt {
    kind: StmtKind,
}

impl Stmt {
    pub fn var(name: Ident, typ: Typespec, expr: Option<P<Expr>>) -> P<Stmt> {
        P::new(Box::new(Stmt {
            kind: StmtKind::Var { name, typ, expr },
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
    pub fn new(stmts: Vec<P<Stmt>>) -> Self {
        Self { stmts }
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
    fn kind(&self) -> &TypespecKind {
        &self.kind
    }
}

pub struct FunctionParam {
    name: Ident,
    typ: P<Typespec>,
}

impl FunctionParam {
    pub fn new(name: Ident, typ: P<Typespec>) -> Self {
        Self { name, typ }
    }

    pub fn name(&self) -> Ident {
        self.name
    }

    pub fn typ(&self) -> &P<Typespec> {
        &self.typ
    }
}

pub enum DeclKind {
    Function {
        name: Ident,
        params: Vec<FunctionParam>,
        return_type: Typespec,
        body: StmtBlock,
    },
}

pub struct Decl {
    kind: DeclKind,
}

impl Decl {
    pub fn function(
        name: Ident,
        params: Vec<FunctionParam>,
        return_type: Typespec,
        body: StmtBlock,
    ) -> P<Decl> {
        P::new(Box::new(Decl {
            kind: DeclKind::Function {
                name,
                params,
                return_type,
                body,
            },
        }))
    }
}

impl Decl {
    fn kind(&self) -> &DeclKind {
        &self.kind
    }
}

