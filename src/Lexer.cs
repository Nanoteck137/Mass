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

    KEYWORD_ELSE,

    IDENTIFIER,
    STRING,
    INTEGER,
    FLOAT,

    HASHTAG,

    PLUS,
    INC,
    PLUS_EQUALS,

    MINUS,
    DEC,
    MINUS_EQUALS,

    MULTIPLY,
    MULTIPLY_EQUALS,

    DIVIDE,
    DIVIDE_EQUALS,

    MODULO,
    MODULO_EQUALS,

    COLON,
    SEMICOLON,

    EQUAL,
    EQUAL2,

    NOT_EQUAL,
    GREATER_THEN,
    LESS_THEN,
    GREATER_EQUALS,
    LESS_EQUALS,

    AND,
    AND2,
    OR,
    OR2,
    NOT,

    ARROW,

    DOT,
    DOT2,
    DOT3,
    COMMA,

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
    private SourceSpan currentTokenSpan;
    public SourceSpan CurrentTokenSpan
    {
        get
        {
            return currentTokenSpan.Clone();
        }
    }

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
            { "break", TokenType.KEYWORD_BREAK },

            { "else", TokenType.KEYWORD_ELSE },
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
        this.currentTokenSpan = new SourceSpan(this.FileName, 1, 1, 1, 1);

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



    public void ExpectToken(TokenType type, bool skip = true)
    {
        if (CurrentToken != type)
        {
            Log.Error($"Unexpected token '{CurrentToken.ToString()}' expected '{type.ToString()}'", currentTokenSpan);
            Debug.Assert(false);
        }
        else
        {
            if (skip)
                NextToken();
        }
    }

    public bool MatchToken(TokenType type)
    {
        return CurrentToken == type;
    }

    private void RemoveComments()
    {
        if (ptr >= text.Length)
            return;

        if (text[ptr] == '/' && text[ptr + 1] == '/')
        {
            while (ptr < text.Length && text[ptr] == '/' && text[ptr + 1] == '/')
            {
                while (ptr < text.Length && text[ptr] != '\n')
                {
                    Inc();
                }

                Inc();

                currentTokenSpan.FromLineNumber++;
                currentTokenSpan.FromColumnNumber = 1;

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
                currentTokenSpan.FromLineNumber++;
                currentTokenSpan.FromColumnNumber = 1;
            }
            else
            {
                currentTokenSpan.FromColumnNumber++;
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
        currentTokenSpan.ToColumnNumber++;
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

        currentTokenSpan.FromColumnNumber = currentTokenSpan.ToColumnNumber;

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

        currentTokenSpan.ToColumnNumber = currentTokenSpan.FromColumnNumber + 1;
        currentTokenSpan.ToLineNumber = currentTokenSpan.FromLineNumber;

        CurrentTokenStart = ptr;
        char current = text[ptr++];

        switch (current)
        {
            case '#':
                CurrentToken = TokenType.HASHTAG;
                break;

            case '+':
                if (!Case2('+', TokenType.INC, TokenType.PLUS))
                {
                    Case2('=', TokenType.PLUS_EQUALS, TokenType.PLUS);
                }

                break;
            case '-':
                if (!Case2('-', TokenType.DEC, TokenType.MINUS))
                {
                    if (!Case2('=', TokenType.MINUS_EQUALS, TokenType.MINUS))
                    {
                        Case2('>', TokenType.ARROW, TokenType.MINUS);
                    }
                }
                break;
            case '*':
                Case2('=', TokenType.MULTIPLY_EQUALS, TokenType.MULTIPLY);
                break;
            case '/':
                Case2('=', TokenType.DIVIDE_EQUALS, TokenType.DIVIDE);
                break;

            case '%':
                Case2('=', TokenType.MODULO_EQUALS, TokenType.MODULO);
                break;

            case ':':
                CurrentToken = TokenType.COLON;
                break;
            case ';':
                CurrentToken = TokenType.SEMICOLON;
                break;

            case '=':
                Case2('=', TokenType.EQUAL2, TokenType.EQUAL);
                break;

            case '!':
                Case2('=', TokenType.NOT_EQUAL, TokenType.NOT);
                break;
            case '>':
                Case2('=', TokenType.GREATER_EQUALS, TokenType.GREATER_THEN);
                break;
            case '<':
                Case2('=', TokenType.LESS_EQUALS, TokenType.LESS_THEN);
                break;
            case '&':
                Case2('&', TokenType.AND2, TokenType.AND);
                break;
            case '|':
                Case2('|', TokenType.OR2, TokenType.OR);
                break;

            case '.':
                Case3('.', '.', TokenType.DOT2, TokenType.DOT3, TokenType.DOT);
                break;
            case ',':
                CurrentToken = TokenType.COMMA;
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
                        Log.Fatal("String never ends", new SourceSpan(currentTokenSpan.FileName,
                                                                      currentTokenSpan.FromLineNumber,
                                                                      currentTokenSpan.FromLineNumber,
                                                                      currentTokenSpan.FromLineNumber,
                                                                      currentTokenSpan.FromLineNumber + 1));
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
                    currentTokenSpan.ToColumnNumber--;
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

        lexer.Reset(". .. ... + += - -> -= * *= / /= % %=");
        lexer.NextToken();

        lexer.ExpectToken(TokenType.DOT);
        lexer.ExpectToken(TokenType.DOT2);
        lexer.ExpectToken(TokenType.DOT3);
        lexer.ExpectToken(TokenType.PLUS);
        lexer.ExpectToken(TokenType.PLUS_EQUALS);
        lexer.ExpectToken(TokenType.MINUS);
        lexer.ExpectToken(TokenType.ARROW);
        lexer.ExpectToken(TokenType.MINUS_EQUALS);
        lexer.ExpectToken(TokenType.MULTIPLY);
        lexer.ExpectToken(TokenType.MULTIPLY_EQUALS);
        lexer.ExpectToken(TokenType.DIVIDE);
        lexer.ExpectToken(TokenType.DIVIDE_EQUALS);
        lexer.ExpectToken(TokenType.MODULO);
        lexer.ExpectToken(TokenType.MODULO_EQUALS);

        lexer.Reset("hello \"hellostr\" 123 0xff00cd 0b11001111 # + += - -= * *= / /= :; = == -> . .. ... , ()[]{}");

        while (lexer.CurrentToken != TokenType.EOF)
        {
            lexer.NextToken();

            Console.WriteLine("{0} {1}", lexer.CurrentToken.ToString(), lexer.currentTokenSpan.ToString());
        }

        lexer.Reset("! != > < >= <= & && | ||");
        while (lexer.CurrentToken != TokenType.EOF)
        {
            lexer.NextToken();

            Console.WriteLine("{0} {1}", lexer.CurrentToken.ToString(), lexer.currentTokenSpan.ToString());
        }
    }
}
