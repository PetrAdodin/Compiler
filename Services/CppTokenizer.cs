using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Compiler_1.Services
{
    public class CppTokenizer
    {
        private readonly string _source;

        private enum State
        {
            Normal,
            InIdentifier,
            InWhitespace,
            InError
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

        public CppTokenizer(string source)
        {
            _source = source;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            int pos = 0;
            int line = 1;
            int column = 1;
            State state = State.Normal;
            string currentLexeme = "";
            int startLine = line, startColumn = column;

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

                        tokens.Add(new Token(TokenType.Error, c.ToString(), line, column, column));
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
                            TokenType type;

                            if (_keywords.Contains(currentLexeme))
                            {
                                type = TokenType.Keyword;
                            }
                            else
                            {
                                type = TokenType.Identifier;
                            }

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
                            if (tokens[tokens.Count - 1].Value == "enum" || tokens[tokens.Count - 1].Value == "class")
                                tokens.Add(new Token(TokenType.Whitespace, currentLexeme, startLine, startColumn, column - 1));
                            state = State.Normal;
                        }
                        break;

                    case State.InError:
                        // В состоянии ошибки пропускаем символы до конца строки
                        // или до появления допустимого символа
                        if (c == '\n')
                        {
                            line++;
                            column = 1;
                            pos++;
                            state = State.Normal;
                        }
                        else if (char.IsWhiteSpace(c) && c != '\n')
                        {
                            state = State.Normal;
                        }
                        else if (char.IsLetter(c) || c == '_' || _punctuationChars.Contains(c))
                        {
                            state = State.Normal;
                        }
                        else
                        {
                            pos++;
                            column++;
                        }
                        break;
                }
            }

            // Добавление последнего токена
            if (state == State.InIdentifier)
            {
                TokenType type;
                if (_keywords.Contains(currentLexeme))
                {
                    type = TokenType.Keyword;
                }
                else
                {
                    type = TokenType.Identifier;
                }
                tokens.Add(new Token(type, currentLexeme, startLine, startColumn, column - 1));
            }
            else if (state == State.InWhitespace)
            {
                if (tokens.Count > 0 && (tokens[tokens.Count - 1].Value == "enum" || tokens[tokens.Count - 1].Value == "class"))
                    tokens.Add(new Token(TokenType.Whitespace, currentLexeme, startLine, startColumn, column - 1));
            }

            return tokens;
        }
    }
}