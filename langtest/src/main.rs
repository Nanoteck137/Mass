fn main() {
    let input = std::fs::read_to_string("test.ma")
        .expect("Failed to read 'test.ma'");
    let decls = parser::parse(&input);
}
