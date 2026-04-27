using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler_1.Services
{
    public class CppTokenizer
    {
        private readonly string _source;

        private static readonly HashSet<string> _keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "enum",
            "class"
        };

        private static readonly HashSet<char> _punctuationChars = new HashSet<char>
        {
            '{', '}', ',', ';'
        };

        public CppTokenizer(string source)
        {
            _source = source ?? string.Empty;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            int pos = 0;
            int line = 1;
            int column = 1;

            while (pos < _source.Length)
            {
                char c = _source[pos];

                if (c == '\r')
                {
                    pos++;
                    continue;
                }

                if (c == '\n')
                {
                    line++;
                    column = 1;
                    pos++;
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    int startLine = line;
                    int startColumn = column;
                    var whitespace = new StringBuilder();

                    while (pos < _source.Length)
                    {
                        c = _source[pos];

                        if (c == '\r' || c == '\n' || !char.IsWhiteSpace(c))
                            break;

                        whitespace.Append(c);
                        pos++;
                        column++;
                    }

                    if (tokens.Count > 0)
                    {
                        string lastValue = tokens[tokens.Count - 1].Value;
                        if (lastValue == "enum" || lastValue == "class")
                        {
                            tokens.Add(new Token(TokenType.Whitespace, whitespace.ToString(), startLine, startColumn, column - 1));
                        }
                    }

                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    int startLine = line;
                    int startColumn = column;
                    var lexeme = new StringBuilder();
                    bool invalid = false;

                    while (pos < _source.Length)
                    {
                        c = _source[pos];

                        if (c == '\r' || c == '\n' || char.IsWhiteSpace(c) || _punctuationChars.Contains(c))
                            break;

                        if (!char.IsLetterOrDigit(c) && c != '_')
                            invalid = true;

                        lexeme.Append(c);
                        pos++;
                        column++;
                    }

                    string value = lexeme.ToString();

                    if (invalid)
                    {
                        tokens.Add(new Token(TokenType.Error, value, startLine, startColumn, column - 1));
                    }
                    else if (_keywords.Contains(value))
                    {
                        tokens.Add(new Token(TokenType.Keyword, value, startLine, startColumn, column - 1));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Identifier, value, startLine, startColumn, column - 1));
                    }

                    continue;
                }

                if (_punctuationChars.Contains(c))
                {
                    tokens.Add(new Token(TokenType.Punctuation, c.ToString(), line, column, column));
                    pos++;
                    column++;
                    continue;
                }

                {
                    int startLine = line;
                    int startColumn = column;
                    var errorText = new StringBuilder();

                    while (pos < _source.Length)
                    {
                        c = _source[pos];

                        if (c == '\r' || c == '\n' || char.IsWhiteSpace(c) || _punctuationChars.Contains(c))
                            break;

                        errorText.Append(c);
                        pos++;
                        column++;
                    }

                    tokens.Add(new Token(TokenType.Error, errorText.ToString(), startLine, startColumn, column - 1));
                }
            }

            return tokens;
        }
    }
}