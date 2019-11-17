using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Mass.Compiler.Tests
{
    [TestFixture]
    public class LexerTests
    {
        private Lexer lexer;

        private List<TokenType> tokensNeededTesting;

        [OneTimeSetUp]
        public void Setup()
        {
            Console.WriteLine("DEBUG: Setup");
            lexer = new Lexer("Lexer Tests", "");

            tokensNeededTesting = new List<TokenType>();
            foreach (TokenType type in Enum.GetValues(typeof(TokenType)))
            {
                if (type != TokenType.UNKNOWN && type != TokenType.EOF)
                    tokensNeededTesting.Add(type);
            }
        }

        public void ExpectToken(TokenType type)
        {
            Assert.AreEqual(type, lexer.CurrentToken);
        }

        public void ExpectInteger(ulong number)
        {
            Assert.AreEqual(number, lexer.CurrentInteger);
        }

        public void TestTokens(string text, TokenType[] expectedTokens)
        {
            lexer.Reset(text);

            int index = 0;
            while (!lexer.MatchToken(TokenType.EOF))
            {
                ExpectToken(expectedTokens[index]);

                tokensNeededTesting.Remove(lexer.CurrentToken);

                index++;
                lexer.NextToken();
            }
        }

        [Test, Order(1)]
        public void TestSingleTokens()
        {
            string text = "# : ; , ( ) [ ] { } + - * / % = & | ! .";
            TokenType[] expectedTokens = new TokenType[]
            {
                TokenType.HASHTAG,

                TokenType.COLON,
                TokenType.SEMICOLON,

                TokenType.COMMA,

                TokenType.OPEN_PAREN,
                TokenType.CLOSE_PAREN,

                TokenType.OPEN_BRACKET,
                TokenType.CLOSE_BRACKET,

                TokenType.OPEN_BRACE,
                TokenType.CLOSE_BRACE,

                TokenType.PLUS,
                TokenType.MINUS,
                TokenType.MULTIPLY,
                TokenType.DIVIDE,
                TokenType.MODULO,

                TokenType.EQUAL,
                TokenType.AND,
                TokenType.OR,
                TokenType.NOT,
                TokenType.DOT,
            };

            TestTokens(text, expectedTokens);
        }

        [Test, Order(1)]
        public void TestKeywords()
        {
            string text = "var const func struct if for while do ret continue break else as test";
            TokenType[] expectedTokens = new TokenType[]
            {
                TokenType.KEYWORD_VAR,
                TokenType.KEYWORD_CONST,
                TokenType.KEYWORD_FUNC,
                TokenType.KEYWORD_STRUCT,

                TokenType.KEYWORD_IF,
                TokenType.KEYWORD_FOR,
                TokenType.KEYWORD_WHILE,
                TokenType.KEYWORD_DO,
                TokenType.KEYWORD_RET,
                TokenType.KEYWORD_CONTINUE,
                TokenType.KEYWORD_BREAK,

                TokenType.KEYWORD_ELSE,
                TokenType.KEYWORD_AS,
                TokenType.IDENTIFIER,
            };

            TestTokens(text, expectedTokens);
        }

        [Test, Order(1)]
        public void TestMultipleTokens()
        {
            string text = "++ -- += -= *= /= %= == != > < >= <= && || -> .. ...";
            TokenType[] expectedTokens = new TokenType[]
            {
                TokenType.INC,
                TokenType.DEC,

                TokenType.PLUS_EQUALS,
                TokenType.MINUS_EQUALS,
                TokenType.MULTIPLY_EQUALS,
                TokenType.DIVIDE_EQUALS,
                TokenType.MODULO_EQUALS,

                TokenType.EQUAL2,
                TokenType.NOT_EQUAL,

                TokenType.GREATER_THEN,
                TokenType.LESS_THEN,

                TokenType.GREATER_EQUALS,
                TokenType.LESS_EQUALS,

                TokenType.AND2,
                TokenType.OR2,

                TokenType.ARROW,

                TokenType.DOT2,
                TokenType.DOT3,
            };

            TestTokens(text, expectedTokens);
        }

        [Test, Order(1)]
        public void TestIntegerToken()
        {
            tokensNeededTesting.Remove(TokenType.INTEGER);

            lexer.Reset("123");
            ExpectToken(TokenType.INTEGER);
            ExpectInteger(123);

            lexer.Reset("0x123");
            ExpectToken(TokenType.INTEGER);
            ExpectInteger(0x123);

            lexer.Reset("0b110011");
            ExpectToken(TokenType.INTEGER);
            ExpectInteger(0b110011);
        }

        [Test, Order(1)]
        public void TestFloatToken()
        {
            // TODO:
            tokensNeededTesting.Remove(TokenType.FLOAT);
        }

        [Test, Order(1)]
        public void TestStringToken()
        {
            // TODO:
            tokensNeededTesting.Remove(TokenType.STRING);
        }

        [Test]
        public void SeeIfAllTokensHasBeenTested()
        {
            Console.WriteLine("-------- Token Missed --------");
            foreach (TokenType type in tokensNeededTesting)
            {
                Console.WriteLine($"  - {type}");
            }
            Console.WriteLine("------------------------------");

            Assert.IsTrue(tokensNeededTesting.Count == 0);
        }
    }
}