using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

/*
 * TODO:
 */

enum TokenType
{
    UNKNOWN,

    KEYWORD_VAR,
    KEYWORD_CONST,
    KEYWORD_FUNC,
    KEYWORD_EXTERNAL,
    KEYWORD_RET,
    KEYWORD_STRUCT,

    IDENTIFIER,
    STRING,
    INTEGER,

    PLUS,
    MINUS,
    ASTERISK,
    FORWORD_SLASH,

    OPEN_PAREN,
    CLOSE_PAREN,

    OPEN_BRACKET,
    CLOSE_BRACKET,

    OPEN_BRACE,
    CLOSE_BRACE,

    COLON,
    SEMICOLON,

    EQUAL,
    EQUAL2,

    ARROW,

    DOT,
    DOT2,
    DOT3,
    SEMIDOT,

    EOF,
}

class SourceSpan
{
    public int FromLineNumber { get; set; }
    public int FromColumnNumber { get; set; }

    public int ToLineNumber { get; set; }
    public int ToColumnNumber { get; set; }

    public SourceSpan(int lineNumber, int columnNumber)
    {
        this.FromLineNumber = lineNumber;
        this.FromColumnNumber = columnNumber;

        this.ToLineNumber = lineNumber;
        this.ToColumnNumber = columnNumber;
    }

    public SourceSpan(int fromLineNumber, int fromLineColumn, int toLineNumber, int toColumnNumber)
    {
        this.FromLineNumber = fromLineNumber;
        this.FromColumnNumber = fromLineColumn;

        this.ToLineNumber = toLineNumber;
        this.ToColumnNumber = toColumnNumber;
    }

    public SourceSpan Clone()
    {
        return new SourceSpan(this.FromLineNumber, this.FromColumnNumber, this.ToLineNumber, this.ToColumnNumber);
    }

    public override string ToString()
    {
        return string.Format("({0}:{1}, {2}:{3})", this.FromLineNumber, this.FromColumnNumber, this.ToLineNumber, this.ToColumnNumber);
    }
}

class Lexer
{
    public string FileName { get; private set; }

    private string text;
    private int ptr;

    private StringBuilder builder;

    public TokenType CurrentToken { get; private set; }
    public SourceSpan CurrentTokenSpan { get; private set; }

    public string CurrentIdentifier { get; private set; }
    public string CurrentString { get; private set; }
    public ulong CurrentNumber { get; private set; }

    public Lexer(string fileName, string text)
    {
        this.FileName = fileName;

        this.builder = new StringBuilder();

        Reset(text);
    }

    public void Reset(string text)
    {
        this.text = text.Replace("\r\n", "\n").Replace("\r", "");
        this.ptr = 0;
        this.CurrentTokenSpan = new SourceSpan(1, 1, 1, 1);

        CurrentToken = TokenType.UNKNOWN;
        CurrentIdentifier = "";
        CurrentString = "";
        CurrentNumber = 0;
    }

    public void Fatal(string message)
    {
        Console.WriteLine("{0}: fatal error: {1}", FileName, message);
        Debugger.Break();
        Environment.Exit(-1);
    }

    public void Error(string message, SourceSpan span)
    {
        //TODO: Add Error recovery
        Console.WriteLine("{0}({1}:{2}, {3}:{4}): error: {5}", FileName, span.FromLineNumber, span.FromColumnNumber, span.ToLineNumber, span.ToColumnNumber, message);
    }

    public void Warning(string message, SourceSpan span)
    {
        Console.WriteLine("{0}({1}:{2}, {3}:{4}): warning: {5}", FileName, span.FromLineNumber, span.FromColumnNumber, span.ToLineNumber, span.ToColumnNumber, message);
    }

    public void ExpectToken(TokenType type, bool skip = true)
    {
        if (CurrentToken != type)
        {
            Error(string.Format("Unexpected token '{0}' expected '{1}'", CurrentToken.ToString(), type.ToString()), CurrentTokenSpan);
            Debug.Assert(false);
        }
        else
        {
            if (skip)
                NextToken();
        }
    }

    private void RemoveComments()
    {
        if (text[ptr] == '/' && text[ptr + 1] == '/')
        {
            while (ptr < text.Length && text[ptr] == '/' && text[ptr + 1] == '/')
            {
                while (ptr < text.Length && text[ptr] != '\n')
                {
                    CurrentTokenSpan.FromColumnNumber++;
                    ptr++;
                }

                CurrentTokenSpan.FromColumnNumber++;
                ptr++;

                CurrentTokenSpan.FromLineNumber++;
                CurrentTokenSpan.FromColumnNumber = 1;

                RemoveWhitespace();
            }
        }
    }

    private void RemoveWhitespace()
    {
        while (ptr < text.Length && char.IsWhiteSpace(text[ptr]))
        {
            if (text[ptr] == '\n')
            {
                CurrentTokenSpan.FromLineNumber++;
                CurrentTokenSpan.FromColumnNumber = 1;
            }
            else
            {
                CurrentTokenSpan.FromColumnNumber++;
            }

            ptr++;

            if (ptr >= text.Length)
            {
                CurrentToken = TokenType.EOF;
                return;
            }
        }
    }

    public void NextToken()
    {
        CurrentToken = TokenType.UNKNOWN;
        CurrentIdentifier = "";
        CurrentString = "";
        CurrentNumber = 0;

        CurrentTokenSpan.FromColumnNumber = CurrentTokenSpan.ToColumnNumber;

        if (ptr >= text.Length)
        {
            CurrentToken = TokenType.EOF;
            return;
        }

        RemoveWhitespace();
        RemoveComments();
        RemoveWhitespace();

        if (ptr >= text.Length)
        {
            CurrentToken = TokenType.EOF;
            return;
        }

        CurrentTokenSpan.ToColumnNumber = CurrentTokenSpan.FromColumnNumber + 1;
        CurrentTokenSpan.ToLineNumber = CurrentTokenSpan.FromLineNumber;

        char current = text[ptr++];

        switch (current)
        {
            case '+':
                CurrentToken = TokenType.PLUS;
                break;
            case '-':
                if (text[ptr] == '>')
                {
                    CurrentToken = TokenType.ARROW;

                    CurrentTokenSpan.ToColumnNumber++;
                    ptr++;
                }
                else
                {
                    CurrentToken = TokenType.MINUS;
                }
                break;
            case '*':
                CurrentToken = TokenType.ASTERISK;
                break;
            case '/':
                CurrentToken = TokenType.FORWORD_SLASH;
                break;

            case '(':
                CurrentToken = TokenType.OPEN_PAREN;
                break;
            case ')':
                CurrentToken = TokenType.CLOSE_PAREN;
                break;

            case '[':
                CurrentToken = TokenType.OPEN_BRACKET;
                break;
            case ']':
                CurrentToken = TokenType.CLOSE_BRACKET;
                break;

            case '{':
                CurrentToken = TokenType.OPEN_BRACE;
                break;
            case '}':
                CurrentToken = TokenType.CLOSE_BRACE;
                break;

            case ':':
                CurrentToken = TokenType.COLON;
                break;
            case ';':
                CurrentToken = TokenType.SEMICOLON;
                break;

            case '=':
                if (text[ptr] == '=')
                {
                    CurrentToken = TokenType.EQUAL2;

                    CurrentTokenSpan.ToColumnNumber++;
                    ptr++;
                }
                else
                {
                    CurrentToken = TokenType.EQUAL;
                }
                break;
            case '.':
                if (text[ptr] == '.')
                {
                    CurrentTokenSpan.ToColumnNumber++;
                    ptr++;

                    if (text[ptr] == '.')
                    {
                        CurrentToken = TokenType.DOT3;

                        CurrentTokenSpan.ToColumnNumber++;
                        ptr++;
                    }
                    else
                    {
                        CurrentToken = TokenType.DOT2;
                    }
                }
                else
                {
                    CurrentToken = TokenType.DOT;
                }
                break;
            case ',':
                CurrentToken = TokenType.SEMIDOT;
                break;

            case '"':
            {
                while (text[ptr] != '"')
                {
                    builder.Append(text[ptr]);

                    CurrentTokenSpan.ToColumnNumber++;
                    ptr++;

                    if (ptr >= text.Length)
                    {
                        Error("String never ends", new SourceSpan(CurrentTokenSpan.FromLineNumber,
                                                                  CurrentTokenSpan.FromLineNumber,
                                                                  CurrentTokenSpan.FromLineNumber,
                                                                  CurrentTokenSpan.FromLineNumber + 1));
                        //TODO: Add a fatal method to terminate the lexer
                        Debug.Assert(false);
                    }
                }

                CurrentTokenSpan.ToColumnNumber++;
                ptr++;

                CurrentString = builder.ToString();
                CurrentString = Regex.Unescape(CurrentString);
                CurrentToken = TokenType.STRING;

                builder.Clear();

                break;
            }

            default:
                if (char.IsLetter(current) || current == '_')
                {
                    builder.Append(current);

                    while (ptr < text.Length && (char.IsLetterOrDigit(text[ptr]) || text[ptr] == '_'))
                    {
                        builder.Append(text[ptr]);

                        CurrentTokenSpan.ToColumnNumber++;
                        ptr++;
                    }

                    CurrentIdentifier = builder.ToString();

                    if (CurrentIdentifier.Equals("var", StringComparison.Ordinal))
                    {
                        CurrentToken = TokenType.KEYWORD_VAR;
                    }
                    else if (CurrentIdentifier.Equals("const", StringComparison.Ordinal))
                    {
                        CurrentToken = TokenType.KEYWORD_CONST;
                    }
                    else if (CurrentIdentifier.Equals("func", StringComparison.Ordinal))
                    {
                        CurrentToken = TokenType.KEYWORD_FUNC;
                    }
                    else if (CurrentIdentifier.Equals("external", StringComparison.Ordinal))
                    {
                        CurrentToken = TokenType.KEYWORD_EXTERNAL;
                    }
                    else if (CurrentIdentifier.Equals("ret", StringComparison.Ordinal))
                    {
                        CurrentToken = TokenType.KEYWORD_RET;
                    }
                    else if (CurrentIdentifier.Equals("struct", StringComparison.Ordinal))
                    {
                        CurrentToken = TokenType.KEYWORD_STRUCT;
                    }
                    else
                    {
                        CurrentToken = TokenType.IDENTIFIER;
                    }

                    builder.Clear();
                }
                else if (char.IsDigit(current))
                {
                    CurrentNumber = (ulong)(current - '0');

                    while (ptr < text.Length && char.IsDigit(text[ptr]))
                    {
                        CurrentNumber *= 10;
                        CurrentNumber += (ulong)(text[ptr] - '0');

                        CurrentTokenSpan.ToColumnNumber++;
                        ptr++;
                    }

                    CurrentToken = TokenType.INTEGER;
                }
                break;
        }
    }

    public static void Test()
    {
        Lexer lexer = new Lexer("test", "");
        lexer.Reset("struct Hello {}");
        lexer.NextToken();

        void PrintAllTokens()
        {
            while (lexer.CurrentToken != TokenType.EOF)
            {
                if (lexer.CurrentToken == TokenType.UNKNOWN)
                    Debug.Assert(false);
                lexer.Warning(string.Format("{0}", lexer.CurrentToken.ToString()), lexer.CurrentTokenSpan);

                lexer.NextToken();
            }

            Console.WriteLine();
        }

        PrintAllTokens();
    }
}
