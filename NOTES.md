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
var hello: s32 = 123;
var test: u8* = "Wooh";

#external
func printf(format: u8*, ...) -> s32;

func add(a: s32, b: s32) -> s32 {
    var result: s32 = a + b;
    printf("%d + %d = %d\n", a, b, result);
    ret result;
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
* base_type = NAME
* type = base_type ('[' expr? ']' | '*')*

* type_list = type (',' type)*

* func_prototype = NAME '(' type_list ')' ('->' type)?

* func_decl = func_prototype stmt_block

* var_decl = NAME ':' type ('=' expr)?

* const_decl = NAME ':' type '=' expr

* struct_item_list = NAME ':' type ';'
* struct_decl = NAME '{' struct_item_list* '}'

* decl = 'func' func_decl
       | 'var' var_decl ';'
	   | 'const' const_decl ';'
	   | 'struct' struct_decl

### Statements

stmt_block = '{' stmt* '}'

stmt = 'return' expr ';'
	 | 'break' ';'
	 | 'continue' ';'
	 | 'if' '(' expr ')' stmt_block ('else' 'if' '(' expr ')' stmt_block)* ('else' stmt_block)?
	 | stmt_block
	 | expr  (INC | DEC | assign_op expr)?

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

* Symbol
    1. Decl
    2. State (Unresolved, Resolving, Resolved)

* Algorithm:
    1. For a symbol
        1. If Unresolved then set state to resolving, and start resolving
        2. If resolved, return resolved data
        3. If resolving, loop, error