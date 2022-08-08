fn main() {
    let mut parser_context = parser::ParserContext::new();

    let input =
        std::fs::read_to_string("test.ma").expect("Failed to read 'test.ma'");
    let decls = parser::parse(&mut parser_context, &input);
}
