using NUnit.Framework;
using System;

namespace Mass.Compiler.Tests
{
    public class ParserTests
    {
        private Lexer lexer;
        private Parser parser;

        [OneTimeSetUp]
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
                "import abc;",
                "import { functionFromABC, functionFromABC2 } from abc;",
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