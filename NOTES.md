# Mass a Programming Language

## Structure:

### Pipeline
1. Lexer
2. Parser
3. Symbol Resolver
4. Code Generator

## Syntax

### Example
```php
var hello = 123;
var test = "Wooh";

func add(a: int32, b: int32) -> int32 {
    var result = a + b;
    printf("%d + %d = %d\n", a, b, result);
    ret a + b;
}
```
### Keywords
* var 
* func
* ret

### Literals
* NAME = <b>[a-zA-Z_][a-zA-Z0-9_]*</b>
* INT = 0 | [1-9][0-9]* | 0[xX][0-9a-fA-F]+ | 0[0-7]+ | 0[bB][0-1]+
* FLOAT = [0-9]*[.]?[0-9]*([eE][+-]?[0-9]+)?
* CHAR = '\'' . '\''
* STR = '"' [^"]* '"'

### Def
* type = 

* type_list = type (',' type)*

* func_prototype = NAME '(' type_list ')' ('->' type)?

* func_decl = func_prototype stmt_block

* var_decl = NAME ('=' expr)?

* decl = 'func' func_decl
       | 'var' var_decl ';'

### Expr
* operand_expr = INT
               | STR
               | NAME
               | '(' expr ')'
* base_expr = operand_expr ('(' expr_list ')')*
* mul_expr = base_expr ([*/] base_expr)*
* add_expr = mul_expr ([+-] mul_expr)*
* expr = add_expr

## Code Resolver
* Order Independent Decls (Later)
* Symbol Table (name => decl)
* Resolve Stmts
    * Symbol Scopes

Hur ska scopes fungera?
Lista av Symbols?

Hur ska man resolvea stmts?
Foreach varje stmt? Kanske
Primärt behöver resolva DeclStmts

* Symbol
    1. Decl
    2. State (Unresolved, Resolving, Resolved)

* Algorithm:
    1. For a symbol
        1. If Unresolved then set state to resolving, and start resolving
        2. If resolved, return resolved data
        3. If resolving, loop, error
