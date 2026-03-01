using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler_1.Services
{
    public class CppTokenizer
    {

        private readonly string _source;
        private enum State
        {
            Normal,
            InIdentifier,
            InNumber,
            InString,
            InChar,
            InLineComment,
            InBlockComment,
            InPreprocessor
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
                char next = (pos + 1 < _source.Length) ? _source[pos + 1] : '\0';

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

                        if (char.IsWhiteSpace(c))
                        {
                            column++;
                            pos++;
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

                        if (char.IsDigit(c))
                        {
                            state = State.InNumber;
                            currentLexeme = c.ToString();
                            startLine = line;
                            startColumn = column;
                            pos++;
                            column++;
                            continue;
                        }


                        if (c == '"')
                        {
                            state = State.InString;
                            currentLexeme = "";
                            startLine = line;
                            startColumn = column;
                            pos++;
                            column++;
                            continue;
                        }

                        if (c == '\'')
                        {
                            state = State.InChar;
                            currentLexeme = "";
                            startLine = line;
                            startColumn = column;
                            pos++;
                            column++;
                            continue;
                        }

                        if (c == '/' && next == '/')
                        {
                            state = State.InLineComment;
                            pos += 2;
                            column += 2;
                            continue;
                        }

                        if (c == '/' && next == '*')
                        {
                            state = State.InBlockComment;
                            pos += 2;
                            column += 2;
                            continue;
                        }

                        if (c == '#' && (pos == 0 || _source[pos - 1] == '\n'))
                        {
                            state = State.InPreprocessor;
                            pos++;
                            column++;
                            continue;
                        }

                        // Если никуда не попали, значит пуктуация ( ) { } ; ,
                        tokens.Add(new Token(TokenType.Punctuation, c.ToString(), line, column));
                        pos++;
                        column++;
                        break;

                    case State.InIdentifier:

                        if (char.IsLetterOrDigit(c) || c == '_')
                        {
                            currentLexeme += c;
                            pos++;
                            column++;
                        }

                        else
                        {
                            AddIdentifierOrKeywordToken(tokens, currentLexeme, startLine, startColumn);
                            state = State.Normal;
                        }
                        break;

                    case State.InNumber:

                        if (char.IsDigit(c))
                        {
                            currentLexeme += c;
                            pos++;
                            column++;
                        }

                        else
                        {
                            tokens.Add(new Token(TokenType.Number, currentLexeme, startLine, startColumn));
                            state = State.Normal;
                        }
                        break;

                    case State.InString:

                        if (c == '"' && (currentLexeme.Length == 0 || currentLexeme[^1] != '\\'))
                        {
                            tokens.Add(new Token(TokenType.StringLiteral, currentLexeme, startLine, startColumn));
                            pos++;
                            column++;
                            state = State.Normal;
                        }
                        else
                        {
                            // Проверяю на escape последовательности
                            if (c == '\\' && next != '\0')
                            {
                                currentLexeme += c;
                                pos++;
                                column++;
                                c = _source[c];
                                pos++;
                                column++;
                            }
                            else
                            {
                                currentLexeme += c;
                                pos++;
                                column++;
                            }
                        }
                        break;

                    case State.InChar:

                        if (c == '\'' && (currentLexeme.Length == 0 || currentLexeme[^1] != '\\'))
                        {
                            tokens.Add(new Token(TokenType.CharLiteral, currentLexeme, startLine, startColumn));
                            pos++;
                            column++;
                            state = State.Normal;
                        }
                        else
                        {
                            if (c == '\\' && next != '\0')
                            {
                                currentLexeme += c;
                                pos++;
                                column++;
                                c = _source[pos];
                                currentLexeme += c;
                                pos++;
                                column++;
                            }
                            else
                            {
                                currentLexeme += c;
                                pos++;
                                column++;
                            }
                        }
                        break;
                    case State.InLineComment:
                        if (c == '\n')
                        {
                            line++;
                            column = 1;
                            pos++;
                            state = State.Normal;
                        }
                        else
                        {
                            pos++;
                            column++;
                        }
                        break;

                    case State.InBlockComment:
                        if (c == '*' && next == '/')
                        {
                            pos += 2;
                            column += 2;
                            state = State.Normal;
                        }
                        else
                        {
                            if (c == '\n')
                            {
                                line++;
                                column = 1;
                            }
                            else
                            {
                                column++;
                            }
                            pos++;
                        }
                        break;

                    case State.InPreprocessor:
                        if (c == '\n')
                        {
                            line++;
                            column = 1;
                            pos++;
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
            // Завершение последнего токена
            if (state == State.InIdentifier)
                AddIdentifierOrKeywordToken(tokens, currentLexeme, startLine, startColumn);

            else if (state == State.InNumber)
                tokens.Add(new Token(TokenType.Number, currentLexeme, startLine, startColumn));

            return tokens;
        }

        private void AddIdentifierOrKeywordToken(List<Token> tokens, string lexeme, int line, int column)
        {
            if (lexeme == "enum" || lexeme == "class")
                tokens.Add(new Token(TokenType.Keyword, lexeme, line, column));
            else
                tokens.Add(new Token(TokenType.Identifier, lexeme, line, column));
        }
    }
}
