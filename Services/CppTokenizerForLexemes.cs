using System;
using System.Collections.Generic;

namespace Compiler_1.Services
{
    public class CppTokenizerForLexemes
    {
        private readonly string _source;

        private enum State
        {
            Normal,
            InIdentifier,
            InWhitespace,
            InErrorSequence
        }

        private static readonly HashSet<string> _keywords = new HashSet<string>
        {
            "enum",
            "class"
        };

        private static readonly HashSet<char> _punctuationChars = new HashSet<char>
        {
            '{', '}', ',', ';'
        };

        private static bool IsValidIdentifierChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        public CppTokenizerForLexemes(string source)
        {
            _source = source ?? string.Empty;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            int pos = 0;
            int line = 1;
            int column = 1;
            State state = State.Normal;
            string currentLexeme = string.Empty;
            int startLine = line;
            int startColumn = column;

            while (pos < _source.Length)
            {
                char c = _source[pos];

                switch (state)
                {
                    case State.Normal:
                        if (c == '\n')
                        {
                            line++;
                            column = 1;
                            pos++;
                            continue;
                        }

                        if (char.IsWhiteSpace(c) && c != '\n')
                        {
                            state = State.InWhitespace;
                            if (tokens.Count > 0 && (tokens[tokens.Count - 1].Value == "enum" || tokens[tokens.Count - 1].Value == "class"))
                                currentLexeme = c.ToString();
                            startLine = line;
                            startColumn = column;
                            pos++;
                            column++;
                            continue;
                        }

                        if (char.IsLetter(c) || c == '_')
                        {
                            state = State.InIdentifier;
                            currentLexeme = c.ToString();
                            startLine = line;
                            startColumn = column;
                            pos++;
                            column++;
                            continue;
                        }

                        if (_punctuationChars.Contains(c))
                        {
                            tokens.Add(new Token(TokenType.Punctuation, c.ToString(), line, column, column));
                            pos++;
                            column++;
                            continue;
                        }

                        state = State.InErrorSequence;
                        currentLexeme = c.ToString();
                        startLine = line;
                        startColumn = column;
                        pos++;
                        column++;
                        break;

                    case State.InIdentifier:
                        if (IsValidIdentifierChar(c))
                        {
                            currentLexeme += c;
                            pos++;
                            column++;
                        }
                        else
                        {
                            TokenType type = _keywords.Contains(currentLexeme) ? TokenType.Keyword : TokenType.Identifier;
                            tokens.Add(new Token(type, currentLexeme, startLine, startColumn, column - 1));
                            state = State.Normal;
                        }
                        break;

                    case State.InWhitespace:
                        if (char.IsWhiteSpace(c) && c != '\n')
                        {
                            pos++;
                            column++;
                        }
                        else
                        {
                            if (tokens.Count > 0 && (tokens[tokens.Count - 1].Value == "enum" || tokens[tokens.Count - 1].Value == "class"))
                                tokens.Add(new Token(TokenType.Whitespace, currentLexeme, startLine, startColumn, column - 1));
                            state = State.Normal;
                        }
                        break;

                    case State.InErrorSequence:
                        if (!char.IsLetter(c) && c != '_' && !_punctuationChars.Contains(c) && !char.IsWhiteSpace(c))
                        {
                            currentLexeme += c;
                            pos++;
                            column++;
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.Error, currentLexeme, startLine, startColumn, column - 1));
                            state = State.Normal;
                        }
                        break;
                }
            }

            if (state == State.InIdentifier)
            {
                TokenType type = _keywords.Contains(currentLexeme) ? TokenType.Keyword : TokenType.Identifier;
                tokens.Add(new Token(type, currentLexeme, startLine, startColumn, column - 1));
            }
            else if (state == State.InWhitespace)
            {
                if (tokens.Count > 0 && (tokens[tokens.Count - 1].Value == "enum" || tokens[tokens.Count - 1].Value == "class"))
                    tokens.Add(new Token(TokenType.Whitespace, currentLexeme, startLine, startColumn, column - 1));
            }
            else if (state == State.InErrorSequence)
            {
                tokens.Add(new Token(TokenType.Error, currentLexeme, startLine, startColumn, column - 1));
            }

            return tokens;
        }
    }
}