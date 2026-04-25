using System;
using System.Collections.Generic;
using System.Linq;

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

            if (!HasPlausibleDeclarationAhead(_position))
            {
                AddSyntaxError(
                    GetCurrentOrSyntheticToken(),
                    "enum",
                    "ожидалось объявление перечисление 'enum'");
                return _errors;
            }

            ParseEnumKeyword();
            ParseClassKeywordOrName();
            ParseEnumName();
            ParseOpeningBrace();
            ParseEnumeratorList();
            ParseClosingBrace();
            ParseSemicolon();

            return _errors;
        }

        private void ParseEnumKeyword()
        {
            SkipIgnoredTokens();

            if (IsKeyword("enum"))
            {
                Advance();
                return;
            }

            AddSyntaxError(
                GetCurrentOrSyntheticToken(),
                "enum",
                "ожидалось объявление перечисление 'enum'");

            // Пытаемся восстановиться, пропуская до class или до потенциального имени перечисления
            if (!RecoverAfterMissingEnum())
            {
                _position = _tokens.Count; // дальше парсить бессмысленно
            }
        }

        private void ParseClassKeywordOrName()
        {
            SkipIgnoredTokens();

            if (IsKeyword("class"))
            {
                Advance();
                return;
            }

            AddSyntaxError(
                GetCurrentOrSyntheticToken(),
                "class",
                "ожидалось ключевое слово 'class'");

            RecoverBeforeName();
        }

        private void ParseEnumName()
        {
            SkipIgnoredTokens();

            if (IsIdentifier())
            {
                Advance();
                return;
            }

            AddSyntaxError(
                GetCurrentOrSyntheticToken(),
                GetCurrentValueOr("<identifier>"),
                "ожидался идентификатор");

            SkipUntilNameOrBrace();

            if (IsIdentifier())
                Advance();
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

            while (!IsEnd && !IsPunctuation("{"))
                Advance();

            if (IsPunctuation("{"))
                Advance();
        }

        private void ParseEnumeratorList()
        {
            SkipIgnoredTokens();

            if (IsPunctuation("}"))
                return;

            bool parsedAnyEnumerator = false;

            // Основной цикл: собираем перечислители через запятую, пока не упрёмся в } или конец
            while (!IsEnd)
            {
                SkipIgnoredTokens();

                if (IsPunctuation("}"))
                    break;

                if (!IsIdentifier())
                {
                    if (!IsEnd && !IsPunctuation(";") && !IsPunctuation("}"))
                    {
                        AddSyntaxError(
                            GetCurrentOrSyntheticToken(),
                            GetCurrentValueOr("<enumerator>"),
                            "ожидался элемент перечисления");
                    }

                    Advance();
                    continue;
                }

                parsedAnyEnumerator = true;
                Advance();
                SkipIgnoredTokens();

                if (IsPunctuation(","))
                {
                    Advance();
                    continue;
                }

                if (IsIdentifier())
                {
                    AddSyntaxError(
                        GetCurrentOrSyntheticToken(),
                        Current.Value,
                        "ожидалась ',' между элементами перечисления");
                    continue;
                }

                if (IsPunctuation("}"))
                    break;

                if (IsPunctuation(";"))
                    break;

                if (!IsEnd && parsedAnyEnumerator)
                {
                    AddSyntaxError(
                        GetCurrentOrSyntheticToken(),
                        GetCurrentValueOr("<enumerator>"),
                        "ожидалась ',' или '}'");
                    Advance();
                }
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

            while (!IsEnd && !IsPunctuation("}"))
                Advance();

            if (IsPunctuation("}"))
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
        }

        // Восстановление после пропущенного enum: ищем class или имя, за которым {/;
        private bool RecoverAfterMissingEnum()
        {
            while (!IsEnd)
            {
                SkipIgnoredTokens();

                if (IsKeyword("class"))
                    return true;

                if (IsIdentifier() && (IsTerminatorAhead() || IsBraceAhead()))
                    return true;

                if (IsIdentifier() && NextLooksLikeGarbage())
                {
                    Advance();
                    continue;
                }

                if (Current.Type == TokenType.Punctuation)
                {
                    return false;
                }

                Advance();
            }

            return false;
        }

        // Восстановление, когда пропущен class: ищем class или допустимое имя перечисления
        private void RecoverBeforeName()
        {
            while (!IsEnd)
            {
                SkipIgnoredTokens();

                if (IsKeyword("class"))
                {
                    Advance();
                    return;
                }

                if (IsIdentifier() && (IsBraceAhead() || IsTerminatorAhead()))
                    return;

                if (IsIdentifier() && NextLooksLikeGarbage())
                {
                    Advance();
                    continue;
                }

                if (Current.Type == TokenType.Punctuation)
                    return;

                Advance();
            }
        }

        private void SkipUntilNameOrBrace()
        {
            while (!IsEnd)
            {
                SkipIgnoredTokens();

                if (IsIdentifier() || IsPunctuation("{") || IsPunctuation("}") || IsPunctuation(";"))
                    return;

                Advance();
            }
        }

        // Быстрая эвристика: есть ли впереди хотя бы одно ключевое слово или идентификатор
        private bool HasPlausibleDeclarationAhead(int startIndex)
        {
            for (int i = startIndex; i < _tokens.Count; i++)
            {
                var token = _tokens[i];

                if (token.Type == TokenType.Identifier)
                    return true;

                if (token.Type == TokenType.Keyword && (token.Value == "class" || token.Value == "enum"))
                    return true;
            }

            return false;
        }

        // Проверяем, не похож ли следующий токен на мусор (помогает при восстановлении)
        private bool NextLooksLikeGarbage()
        {
            if (IsEnd)
                return false;

            if (IsKeyword("class"))
                return false;

            if (IsIdentifier() && IsBraceAhead())
                return false;

            if (IsIdentifier() && IsTerminatorAhead())
                return false;

            if (IsIdentifier() && NextIsIdentifierOrError())
                return true;

            if (Current.Type == TokenType.Error)
                return true;

            return false;
        }

        private bool NextIsIdentifierOrError()
        {
            if (Peek(1) == null)
                return false;

            return Peek(1).Type == TokenType.Identifier || Peek(1).Type == TokenType.Error;
        }

        private bool IsBraceAhead()
        {
            var next = Peek(1);
            return next != null && next.Type == TokenType.Punctuation && next.Value == "{";
        }

        private bool IsTerminatorAhead()
        {
            var next = Peek(1);
            return next == null || (next.Type == TokenType.Punctuation && (next.Value == ";" || next.Value == "}"));
        }

        private Token Current => !IsEnd ? _tokens[_position] : null;

        private bool IsEnd => _position >= _tokens.Count;

        private void Advance()
        {
            if (!IsEnd)
                _position++;
        }

        private Token Peek(int offset)
        {
            int index = _position + offset;
            if (index < 0 || index >= _tokens.Count)
                return null;
            return _tokens[index];
        }

        private void SkipIgnoredTokens()
        {
            while (!IsEnd && (Current.Type == TokenType.Error || Current.Type == TokenType.Whitespace))
                Advance();
        }

        private bool IsKeyword(string value)
        {
            return !IsEnd && Current.Type == TokenType.Keyword && string.Equals(Current.Value, value, StringComparison.Ordinal);
        }

        private bool IsIdentifier()
        {
            return !IsEnd && Current.Type == TokenType.Identifier;
        }

        private bool IsPunctuation(string value)
        {
            return !IsEnd && Current.Type == TokenType.Punctuation && Current.Value == value;
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