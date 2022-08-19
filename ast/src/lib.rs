use util::P;

#[derive(Clone, Debug)]
pub struct Span {
    start: usize,
    end: usize,
}

impl Span {
    pub fn new(start: usize, end: usize) -> Self {
        Self { start, end }
    }

    pub fn start(&self) -> usize {
        self.start
    }

    pub fn end(&self) -> usize {
        self.end
    }
}

#[derive(Copy, Clone, Hash, Eq, PartialEq, Debug)]
pub struct Ident(usize);

impl Ident {
    pub fn new(index: usize) -> Self {
        Self(index)
    }

    pub fn index(&self) -> usize {
        self.0
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
    span: Span,
}

impl Expr {
    pub fn integer(span: Span, value: u64) -> P<Expr> {
        P::new(Box::new(Expr {
            kind: ExprKind::Integer(value),
            span,
        }))
    }

    pub fn ident(span: Span, ident: Ident) -> P<Expr> {
        P::new(Box::new(Expr {
            kind: ExprKind::Ident(ident),
            span,
        }))
    }

    pub fn string(span: Span, string: String) -> P<Expr> {
        P::new(Box::new(Expr {
            kind: ExprKind::String(string),
            span,
        }))
    }

    pub fn binary(
        span: Span,
        op: BinaryOp,
        left: P<Expr>,
        right: P<Expr>,
    ) -> P<Expr> {
        P::new(Box::new(Expr {
            kind: ExprKind::Binary { op, left, right },
            span,
        }))
    }

    pub fn call(span: Span, expr: P<Expr>, args: Vec<P<Expr>>) -> P<Expr> {
        P::new(Box::new(Expr {
            kind: ExprKind::Call { expr, args },
            span,
        }))
    }

    pub fn index(span: Span, expr: P<Expr>, index: P<Expr>) -> P<Expr> {
        P::new(Box::new(Expr {
            kind: ExprKind::Index { expr, index },
            span,
        }))
    }
}

impl Expr {
    pub fn kind(&self) -> &ExprKind {
        &self.kind
    }

    pub fn span(&self) -> &Span {
        &self.span
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
    span: Span,
}

impl Stmt {
    pub fn var(
        span: Span,
        name: Ident,
        typ: P<Typespec>,
        expr: Option<P<Expr>>,
    ) -> P<Stmt> {
        P::new(Box::new(Stmt {
            kind: StmtKind::Var { name, typ, expr },
            span,
        }))
    }

    pub fn ret(span: Span, expr: P<Expr>) -> P<Stmt> {
        P::new(Box::new(Stmt {
            kind: StmtKind::Ret(expr),
            span,
        }))
    }

    pub fn expr(span: Span, expr: P<Expr>) -> P<Stmt> {
        P::new(Box::new(Stmt {
            kind: StmtKind::Expr(expr),
            span,
        }))
    }
}

impl Stmt {
    pub fn kind(&self) -> &StmtKind {
        &self.kind
    }

    pub fn span(&self) -> &Span {
        &self.span
    }
}

pub struct StmtBlock {
    stmts: Vec<P<Stmt>>,
    span: Span,
}

impl StmtBlock {
    pub fn new(span: Span) -> Self {
        Self {
            stmts: Vec::new(),
            span,
        }
    }

    pub fn add_stmt(&mut self, stmt: P<Stmt>) {
        self.stmts.push(stmt);
    }

    pub fn stmts(&self) -> &[P<Stmt>] {
        &self.stmts
    }

    pub fn span(&self) -> &Span {
        &self.span
    }
}

pub enum TypespecKind {
    Name(Ident),
    Ptr(P<Typespec>),
}

pub struct Typespec {
    kind: TypespecKind,
    span: Span,
}

impl Typespec {
    pub fn name(span: Span, name: Ident) -> P<Typespec> {
        P::new(Box::new(Typespec {
            kind: TypespecKind::Name(name),
            span,
        }))
    }

    pub fn ptr(span: Span, base: P<Typespec>) -> P<Typespec> {
        P::new(Box::new(Typespec {
            kind: TypespecKind::Ptr(base),
            span,
        }))
    }
}

impl Typespec {
    pub fn kind(&self) -> &TypespecKind {
        &self.kind
    }

    pub fn span(&self) -> &Span {
        &self.span
    }
}

pub struct FunctionParam {
    name: Ident,
    ty: P<Typespec>,
    span: Span,
}

impl FunctionParam {
    pub fn new(span: Span, name: Ident, ty: P<Typespec>) -> Self {
        Self { name, ty, span }
    }

    pub fn name(&self) -> Ident {
        self.name
    }

    pub fn ty(&self) -> &P<Typespec> {
        &self.ty
    }

    pub fn span(&self) -> &Span {
        &self.span
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
    span: Span,
}

impl Decl {
    pub fn function(
        span: Span,
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
            span,
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

    pub fn span(&self) -> &Span {
        &self.span
    }
}
