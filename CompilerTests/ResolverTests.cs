using NUnit.Framework;
using System;

namespace Mass.Compiler.Tests
{
    public class ResolverTests
    {
        private Lexer lexer;
        private Parser parser;
        private Resolver resolver;

        [OneTimeSetUp]
        public void Setup()
        {
            lexer = new Lexer("Resolver Test", "");
            parser = new Parser(lexer);
            resolver = new Resolver();
        }

        [Test]
        public void ResolverTest()
        {
            string[] code = new string[]
            {
                "var a: s32 = 123;",
                "struct T { i: s32; }",
                "func testFunc(a: s32) { }",
                "func test() -> s32 { testFunc(123); }"
            };

            foreach (string c in code)
            {
                try
                {
                    lexer.Reset(c);

                    Decl decl = parser.ParseDecl();
                    resolver.AddSymbol(decl);
                }
                catch (FatalErrorException e)
                {
                    Assert.Fail(e.Message);
                }
            }

            try
            {
                resolver.ResolveSymbols();
                resolver.FinalizeSymbols();
            }
            catch (FatalErrorException e)
            {
                Assert.Fail(e.Message);
            }
        }
    }
}