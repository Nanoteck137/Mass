using NUnit.Framework;
using System;

namespace Mass.Compiler.Tests
{
    public class ParserTests
    {
        private Lexer lexer;
        private Parser parser;

        [SetUp]
        public void Setup()
        {
            lexer = new Lexer("Parser Test", "");
            parser = new Parser(lexer);
        }

        [Test]
        public void ParsingTest()
        {
            string[] code = new string[]
            {
                "var a: s32 = 123;",
                "func test(a: s32[3], b: s32*) { }",
                "func test(a: s32, b: f32) { printf(\"Hello World\"); }",
                "struct Test { a: T; b: s32; }",
            };

            foreach (string c in code)
            {
                lexer.Reset(c);
                try
                {
                    Decl decl = parser.ParseDecl();
                }
                catch (FatalErrorException e)
                {
                    Assert.Fail(e.Message);
                }
            }
        }
    }
}