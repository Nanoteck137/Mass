using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
    KEYWORD_STRUCT,

    KEYWORD_IF,
    KEYWORD_FOR,
    KEYWORD_WHILE,
    KEYWORD_DO,
    KEYWORD_RET,
    KEYWORD_CONTINUE,
    KEYWORD_BREAK,

    IDENTIFIER,
    STRING,
    INTEGER,
    FLOAT,

    HASHTAG,

    PLUS,
    PLUS_EQUAL,

    MINUS,
    MINUS_EQUAL,

    ASTERISK,
    MUL_EQUAL,

    FORWORD_SLASH,
    DIV_EQUAL,

    COLON,
    SEMICOLON,

    EQUAL,
    EQUAL2,

    ARROW,

    DOT,
    DOT2,
    DOT3,
    SEMIDOT,

    OPEN_PAREN,
    CLOSE_PAREN,

    OPEN_BRACKET,
    CLOSE_BRACKET,

    OPEN_BRACE,
    CLOSE_BRACE,

    EOF,
}

enum TokenMod
{
    NONE,
    FLOAT,
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

    private readonly StringBuilder builder;
    private readonly Dictionary<string, TokenType> keywords;
    private readonly Dictionary<char, int> hexCharMap;

    public int CurrentTokenStart { get; private set; }
    public TokenType CurrentToken { get; private set; }
    public SourceSpan CurrentTokenSpan { get; private set; }

    public TokenMod TokenMod { get; private set; }
    public string CurrentIdentifier { get; private set; }
    public string CurrentString { get; private set; }
    public ulong CurrentInteger { get; private set; }
    public double CurrentFloat { get; private set; }

    public Lexer(string fileName, string text)
    {
        this.FileName = fileName;
        this.builder = new StringBuilder();

        keywords = new Dictionary<string, TokenType>
        {
            { "var", TokenType.KEYWORD_VAR },
            { "const", TokenType.KEYWORD_CONST },
            { "func", TokenType.KEYWORD_FUNC },
            { "struct", TokenType.KEYWORD_STRUCT },

            { "if", TokenType.KEYWORD_IF },
            { "for", TokenType.KEYWORD_FOR },
            { "while", TokenType.KEYWORD_WHILE },
            { "do", TokenType.KEYWORD_DO },
            { "ret", TokenType.KEYWORD_RET },
            { "continue", TokenType.KEYWORD_CONTINUE },
            { "break", TokenType.KEYWORD_BREAK }
        };

        hexCharMap = new Dictionary<char, int>()
        {
            { '0', 0 }, { '1', 1 }, { '2', 2 }, { '3', 3 },
            { '4', 4 }, { '5', 5 }, { '6', 6 }, { '7', 7 },
            { '8', 8 }, { '9', 9 },

            { 'a', 10 }, { 'b', 11 },
            { 'c', 12 }, { 'd', 13 },
            { 'e', 14 }, { 'f', 15 },

            { 'A', 10 }, { 'B', 11 },
            { 'C', 12 }, { 'D', 13 },
            { 'E', 14 }, { 'F', 15 },
        };

        Reset(text);
    }

    public void Reset(string text)
    {
        this.text = text.Replace("\r\n", "\n").Replace("\r", "");
        this.ptr = 0;
        this.CurrentTokenSpan = new SourceSpan(1, 1, 1, 1);

        ResetToken();
    }

    private void ResetToken()
    {
        CurrentTokenStart = 0;
        CurrentToken = TokenType.UNKNOWN;

        TokenMod = TokenMod.NONE;
        CurrentIdentifier = "";
        CurrentString = "";
        CurrentInteger = 0;
        CurrentFloat = 0.0;
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
                    Inc();
                }

                Inc();

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

    private void ScanHexInt()
    {
        while (ptr < text.Length && hexCharMap.ContainsKey(text[ptr]))
        {
            CurrentInteger *= 16;
            CurrentInteger += (ulong)(hexCharMap[text[ptr]]);

            Inc();
        }
    }

    private void ScanBinaryInt()
    {
        while (ptr < text.Length && (text[ptr] == '0' || text[ptr] == '1'))
        {
            CurrentInteger *= 2;
            CurrentInteger += (ulong)(text[ptr] - '0');

            Inc();
        }
    }

    private void ScanInt()
    {
        // TODO(patrik): Binary Notation needs only to accept 0 and 1 and hex needs to have ABCDEF as "numbers"
        // Format: 1: 123 2: 0x123 3: 0b101
        char format = ' ';
        if (ptr + 1 < text.Length)
            format = text[ptr + 1];

        if (format == 'x' || format == 'X')
        {
            Inc();
            Inc();

            ScanHexInt();
        }
        else if (format == 'b' || format == 'B')
        {
            Inc();
            Inc();

            ScanBinaryInt();
        }
        else
        {
            while (ptr < text.Length && char.IsDigit(text[ptr]))
            {
                CurrentInteger *= 10;
                CurrentInteger += (ulong)(text[ptr] - '0');

                Inc();
            }
        }

        CurrentToken = TokenType.INTEGER;
    }

    private void ScanFloat()
    {
        // Format: 1: 3.14 (double) 2: 3.14f (float)
        bool alreadySeenDot = false;

        while (ptr < text.Length && (char.IsDigit(text[ptr]) || text[ptr] == '.'))
        {
            if (text[ptr] == '.')
            {
                if (alreadySeenDot)
                {
                    // TODO(patrik): Replace with a proper error
                    Debug.Assert(false);
                }
                else
                {
                    alreadySeenDot = true;
                }
            }

            Inc();
        }

        bool isFloat = false;

        int end = ptr;
        if (ptr < text.Length && (text[ptr] == 'f' || text[ptr] == 'F'))
        {
            isFloat = true;
            Inc();
        }

        string str = text.Substring(CurrentTokenStart, end - CurrentTokenStart);
        double val = double.Parse(str, CultureInfo.InvariantCulture);

        CurrentToken = TokenType.FLOAT;
        CurrentFloat = val;
        if (isFloat)
        {
            TokenMod = TokenMod.FLOAT;
        }
    }

    private bool MatchChar(char c)
    {
        return ptr < text.Length && text[ptr] == c;
    }

    private void Inc()
    {
        ptr++;
        CurrentTokenSpan.ToColumnNumber++;
    }

    private bool Case2(char next, TokenType nextToken, TokenType otherToken)
    {
        if (MatchChar(next))
        {
            CurrentToken = nextToken;
            Inc();

            return true;
        }
        else
        {
            CurrentToken = otherToken;

            return false;
        }
    }

    private void Case3(char case2, char case3, TokenType case2Token, TokenType case3Token, TokenType otherToken)
    {
        if (MatchChar(case2))
        {
            Inc();

            if (MatchChar(case3))
            {
                CurrentToken = case3Token;
                Inc();
            }
            else
            {
                CurrentToken = case2Token;
            }
        }
        else
        {
            CurrentToken = otherToken;
        }
    }

    public void NextToken()
    {
        ResetToken();

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

        CurrentTokenStart = ptr;
        char current = text[ptr++];

        switch (current)
        {
            case '#':
                CurrentToken = TokenType.HASHTAG;
                break;

            case '+':
                Case2('=', TokenType.PLUS_EQUAL, TokenType.PLUS);
                break;
            case '-':
                if (!Case2('=', TokenType.MINUS_EQUAL, TokenType.MINUS))
                {
                    Case2('>', TokenType.ARROW, TokenType.MINUS);
                }
                break;
            case '*':
                Case2('=', TokenType.MUL_EQUAL, TokenType.ASTERISK);
                break;
            case '/':
                Case2('=', TokenType.DIV_EQUAL, TokenType.FORWORD_SLASH);
                break;

            case ':':
                CurrentToken = TokenType.COLON;
                break;
            case ';':
                CurrentToken = TokenType.SEMICOLON;
                break;

            case '=':
                Case2('>', TokenType.EQUAL2, TokenType.EQUAL);
                break;

            case '.':
                Case3('.', '.', TokenType.DOT2, TokenType.DOT3, TokenType.DOT);
                break;
            case ',':
                CurrentToken = TokenType.SEMIDOT;
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

            case '"':
            {
                while (text[ptr] != '"')
                {
                    builder.Append(text[ptr]);

                    Inc();

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

                Inc();

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

                        Inc();
                    }

                    CurrentIdentifier = builder.ToString();

                    if (keywords.ContainsKey(CurrentIdentifier))
                    {
                        CurrentToken = keywords[CurrentIdentifier];
                    }
                    else
                    {
                        CurrentToken = TokenType.IDENTIFIER;
                    }

                    builder.Clear();
                }
                else if (char.IsDigit(current))
                {
                    while (ptr < text.Length && char.IsDigit(text[ptr]))
                    {
                        ptr++;
                    }

                    char c = ptr < text.Length ? text[ptr] : '0';
                    CurrentTokenSpan.ToColumnNumber--;
                    ptr = CurrentTokenStart;

                    if (c == '.')
                    {
                        ScanFloat();
                    }
                    else
                    {
                        ScanInt();
                    }
                }
                break;
        }
    }

    public static void Test()
    {
        Lexer lexer = new Lexer("test", "");
        lexer.Reset("struct Hello {}");
        lexer.NextToken();

        lexer.ExpectToken(TokenType.KEYWORD_STRUCT);
        lexer.ExpectToken(TokenType.IDENTIFIER);
        lexer.ExpectToken(TokenType.OPEN_BRACE);
        lexer.ExpectToken(TokenType.CLOSE_BRACE);

        lexer.Reset("123");
        lexer.NextToken();
        Debug.Assert(lexer.CurrentToken == TokenType.INTEGER);
        Debug.Assert(lexer.CurrentInteger == 123);

        lexer.Reset("0x123af");
        lexer.NextToken();
        Debug.Assert(lexer.CurrentToken == TokenType.INTEGER);
        Debug.Assert(lexer.CurrentInteger == 0x123af);

        lexer.Reset("0b1100112");
        lexer.NextToken();
        Debug.Assert(lexer.CurrentToken == TokenType.INTEGER);
        Debug.Assert(lexer.CurrentInteger == 0b110011);

        lexer.Reset("3.14f");
        lexer.NextToken();
        Debug.Assert(lexer.CurrentToken == TokenType.FLOAT);
        Debug.Assert(lexer.CurrentFloat == 3.14);
        Debug.Assert(lexer.TokenMod == TokenMod.FLOAT);
        lexer.NextToken();
        lexer.ExpectToken(TokenType.EOF);

        lexer.Reset("3.14");
        lexer.NextToken();
        Debug.Assert(lexer.CurrentToken == TokenType.FLOAT);
        Debug.Assert(lexer.CurrentFloat == 3.14);
        Debug.Assert(lexer.TokenMod != TokenMod.FLOAT);
        lexer.NextToken();
        lexer.ExpectToken(TokenType.EOF);

        lexer.Reset(". .. ... + += - -> -= * *= / /=");
        lexer.NextToken();

        lexer.ExpectToken(TokenType.DOT);
        lexer.ExpectToken(TokenType.DOT2);
        lexer.ExpectToken(TokenType.DOT3);
        lexer.ExpectToken(TokenType.PLUS);
        lexer.ExpectToken(TokenType.PLUS_EQUAL);
        lexer.ExpectToken(TokenType.MINUS);
        lexer.ExpectToken(TokenType.ARROW);
        lexer.ExpectToken(TokenType.MINUS_EQUAL);
        lexer.ExpectToken(TokenType.ASTERISK);
        lexer.ExpectToken(TokenType.MUL_EQUAL);
        lexer.ExpectToken(TokenType.FORWORD_SLASH);
        lexer.ExpectToken(TokenType.DIV_EQUAL);

        lexer.Reset("hello \"hellostr\" 123 0xff00cd 0b11001111 # + += - -= * *= / /= :; = == -> . .. ... , ()[]{}");

        while (lexer.CurrentToken != TokenType.EOF)
        {
            lexer.NextToken();

            Console.WriteLine("{0} {1}", lexer.CurrentToken.ToString(), lexer.CurrentTokenSpan.ToString());
        }
    }
}
