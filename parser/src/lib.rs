#[macro_use]
extern crate pest_derive;

use pest::Parser;

#[derive(Parser)]
#[grammar = "grammar.pest"]
struct LangParser;

pub fn parse(input: &str) {
    let file = LangParser::parse(Rule::file, input)
        .expect("Failed to parse")
        .next()
        .unwrap();

    for root in file.into_inner() {
        println!("Root: {:#?}", root);
    }
}
