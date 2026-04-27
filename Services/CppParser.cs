using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler_1.Services
{
    public class CppParser
    {
        private readonly List<Token> _tokens;
        private readonly List<SyntaxError> _errors = new List<SyntaxError>();
        private int _position;

        public CppParser(List<Token> tokens)
        {
            _tokens = tokens ?? new List<Token>();
        }

        public List<SyntaxError> Parse()
        {
            _errors.Clear();
            _position = 0;

            while (true)
            {
                SkipIgnoredTokens();

                if (IsEnd)
                    break;

                if (!HasPlausibleDeclarationAhead(_position))
                {
                    AddSyntaxError(
                        GetCurrentOrSyntheticToken(),
                        "enum",
                        "ожидалось объявление перечисление 'enum'");
                    break;
                }

                int before = _position;
                ParseDeclaration();
                SkipIgnoredTokens();

                if (_position == before)
                    Advance();
            }

            return _errors;
        }

        private void ParseDeclaration()
        {
            SkipIgnoredTokens();

            if (IsEnd)
                return;

            if (IsEnumKeyword(Current))
            {
                Advance();
            }
            else if (Current.Type == TokenType.Error && IsEnumLike(Current.Value))
            {
                AddSyntaxError(Current, "enum", "ожидалось объявление перечисление 'enum'");
                Advance();
            }
            else if (Current.Type == TokenType.Error)
            {
                AddSyntaxError(Current, "enum", "ожидалось объявление перечисление 'enum'");
                Advance();
            }
            else
            {
                AddSyntaxError(GetCurrentOrSyntheticToken(), "enum", "ожидалось объявление перечисление 'enum'");
            }

            ParseClassKeywordOrName();
            ParseEnumName();
            ParseOpeningBrace();
            ParseEnumeratorList();
            ParseClosingBrace();
            ParseSemicolon();
        }

        private void ParseClassKeywordOrName()
        {
            SkipIgnoredTokens();

            if (IsClassKeyword(Current))
            {
                Advance();
                return;
            }

            if (Current != null && Current.Type == TokenType.Error)
            {
                AddSyntaxError(Current, "class", "ожидалось ключевое слово 'class'");
                Advance();
                return;
            }

            AddSyntaxError(
                GetCurrentOrSyntheticToken(),
                "class",
                "ожидалось ключевое слово 'class'");
        }

        private void ParseEnumName()
        {
            SkipIgnoredTokens();

            if (IsIdentifier())
            {
                Advance();
                return;
            }

            if (Current != null && Current.Type == TokenType.Error && IsIdentifierLike(Current.Value))
            {
                AddSyntaxError(Current, Current.Value, "ожидался идентификатор");
                Advance();
                return;
            }

            AddSyntaxError(
                GetCurrentOrSyntheticToken(),
                GetCurrentValueOr("<identifier>"),
                "ожидался идентификатор");
        }

        private void ParseOpeningBrace()
        {
            SkipIgnoredTokens();

            if (IsPunctuation("{"))
            {
                Advance();
                return;
            }

            AddSyntaxError(
                GetCurrentOrSyntheticToken(),
                GetCurrentValueOr("{"),
                "ожидалась '{'");

            if (Current != null && (Current.Type == TokenType.Error || (Current.Type == TokenType.Punctuation && Current.Value != "}" && Current.Value != ";")))
                Advance();
        }

        private void ParseEnumeratorList()
        {
            SkipIgnoredTokens();

            if (IsEnd || IsPunctuation("}") || IsPunctuation(";") || IsClosingParenError(Current))
                return;

            while (!IsEnd)
            {
                SkipIgnoredTokens();

                if (IsEnd || IsPunctuation("}") || IsPunctuation(";") || IsClosingParenError(Current))
                    return;

                if (IsIdentifier())
                {
                    Advance();
                }
                else if (Current != null && Current.Type == TokenType.Error)
                {
                    if (IsIdentifierLike(Current.Value))
                    {
                        AddSyntaxError(Current, Current.Value, "ожидался элемент перечисления");
                        Advance();
                    }
                    else
                    {
                        AddSyntaxError(Current, GetCurrentValueOr("<enumerator>"), "ожидался элемент перечисления");
                        Advance();
                    }
                }
                else
                {
                    AddSyntaxError(
                        GetCurrentOrSyntheticToken(),
                        GetCurrentValueOr("<enumerator>"),
                        "ожидался элемент перечисления");

                    if (Current != null && Current.Type == TokenType.Punctuation && Current.Value == ",")
                        Advance();
                    else
                        break;
                }

                SkipIgnoredTokens();

                if (IsPunctuation(","))
                {
                    Advance();
                    continue;
                }

                if (IsIdentifier())
                {
                    AddSyntaxError(
                        Current,
                        Current.Value,
                        "ожидалась ',' между элементами перечисления");
                    continue;
                }

                if (Current != null && Current.Type == TokenType.Error && IsIdentifierLike(Current.Value))
                {
                    AddSyntaxError(
                        Current,
                        Current.Value,
                        "ожидалась ',' между элементами перечисления");
                    Advance();
                    continue;
                }

                if (IsPunctuation("}") || IsPunctuation(";") || IsClosingParenError(Current))
                    return;

                if (Current != null && Current.Type == TokenType.Error)
                {
                    AddSyntaxError(
                        Current,
                        Current.Value,
                        "ожидалась ',' между элементами перечисления");
                    Advance();
                    continue;
                }

                break;
            }
        }

        private void ParseClosingBrace()
        {
            SkipIgnoredTokens();

            if (IsPunctuation("}"))
            {
                Advance();
                return;
            }

            AddSyntaxError(
                GetCurrentOrSyntheticToken(),
                GetCurrentValueOr("}"),
                "ожидалась '}'");

            if (Current != null && (Current.Type == TokenType.Error || (Current.Type == TokenType.Punctuation && Current.Value != ";")))
                Advance();
        }

        private void ParseSemicolon()
        {
            SkipIgnoredTokens();

            if (IsPunctuation(";"))
            {
                Advance();
                return;
            }

            AddSyntaxError(
                GetCurrentOrSyntheticToken(),
                GetCurrentValueOr(";"),
                "ожидалась ';'");

            if (Current != null && Current.Type == TokenType.Error)
                Advance();
        }

        private bool HasPlausibleDeclarationAhead(int startIndex)
        {
            for (int i = startIndex; i < _tokens.Count; i++)
            {
                Token token = _tokens[i];

                if (token.Type == TokenType.Keyword && (token.Value == "enum" || token.Value == "class"))
                    return true;

                if (token.Type == TokenType.Identifier)
                    return true;

                if (token.Type == TokenType.Error && IsIdentifierLike(token.Value))
                    return true;
            }

            return false;
        }

        private static string NormalizeWord(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length);

            foreach (char ch in value)
            {
                if (char.IsLetterOrDigit(ch) || ch == '_')
                    builder.Append(char.ToLowerInvariant(ch));
            }

            return builder.ToString();
        }

        private static bool IsIdentifierLike(string value)
        {
            return NormalizeWord(value).Length > 0;
        }

        private static bool IsEnumLike(string value)
        {
            return string.Equals(NormalizeWord(value), "enum", StringComparison.Ordinal);
        }

        private static bool IsClassLike(string value)
        {
            return string.Equals(NormalizeWord(value), "class", StringComparison.Ordinal);
        }

        private static bool IsClosingParenError(Token token)
        {
            return token != null &&
                   token.Type == TokenType.Error &&
                   token.Value == ")";
        }

        private Token Current => !IsEnd ? _tokens[_position] : null;

        private bool IsEnd => _position >= _tokens.Count;

        private void Advance()
        {
            if (!IsEnd)
                _position++;
        }

        private void SkipIgnoredTokens()
        {
            while (!IsEnd && Current.Type == TokenType.Whitespace)
                Advance();
        }

        private bool IsEnumKeyword(Token token)
        {
            return token != null &&
                   token.Type == TokenType.Keyword &&
                   string.Equals(token.Value, "enum", StringComparison.Ordinal);
        }

        private bool IsClassKeyword(Token token)
        {
            return token != null &&
                   token.Type == TokenType.Keyword &&
                   string.Equals(token.Value, "class", StringComparison.Ordinal);
        }

        private bool IsIdentifier()
        {
            return !IsEnd && Current.Type == TokenType.Identifier;
        }

        private bool IsPunctuation(string value)
        {
            return !IsEnd &&
                   Current.Type == TokenType.Punctuation &&
                   Current.Value == value;
        }

        private string GetCurrentValueOr(string fallback)
        {
            return !IsEnd ? Current.Value : fallback;
        }

        private Token GetCurrentOrSyntheticToken()
        {
            if (!IsEnd)
                return Current;

            if (_tokens.Count > 0)
                return _tokens[_tokens.Count - 1];

            return new Token(TokenType.Error, string.Empty, 1, 1, 1);
        }

        private void AddSyntaxError(Token token, string fragment, string description)
        {
            if (token == null)
            {
                _errors.Add(new SyntaxError
                {
                    Fragment = fragment,
                    Description = description,
                    Line = 1,
                    StartColumn = 1,
                    EndColumn = 1
                });
                return;
            }

            _errors.Add(new SyntaxError
            {
                Fragment = fragment,
                Description = description,
                Line = token.Line,
                StartColumn = token.StartColumn,
                EndColumn = token.EndColumn
            });
        }
    }
}