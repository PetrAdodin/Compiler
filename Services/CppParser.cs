using System.Collections.Generic;
using System.Linq;

namespace Compiler_1.Services
{
    public class SyntaxError
    {
        public string Fragment { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public int Line { get; set; }
        public int StartColumn { get; set; }
    }

    public class CppParser
    {
        private readonly List<Token> _tokens;
        private int _pos;
        private readonly List<SyntaxError> _errors;
        private bool _eofReported = false;

        public CppParser(List<Token> tokens)
        {
            _tokens = tokens.Where(t => t.Type != TokenType.Whitespace).ToList();
            _pos = 0;
            _errors = new List<SyntaxError>();
        }

        public List<SyntaxError> Parse()
        {
            _pos = 0;
            _errors.Clear();
            _eofReported = false;

            ParseEnumDeclaration();

            return _errors;
        }

        private void ParseEnumDeclaration()
        {
            MatchKeyword("enum", new[] { "class", "Identifier", "{" }, "ключевое слово 'enum'");
            MatchKeyword("class", new[] { "Identifier", "{" }, "ключевое слово 'class'");
            MatchIdentifier(new[] { "{" }, "имя перечисления");
            MatchPunctuation("{", new[] { "Identifier", "}", ";" }, "открывающая фигурная скобка '{'");

            ParseEnumList();

            MatchPunctuation("}", new[] { ";" }, "закрывающая фигурная скобка '}'");
            MatchPunctuation(";", new[] { "EOF" }, "точка с запятой ';'");

            while (!IsAtEnd())
            {
                var current = Peek();
                AddError(current, $"Ожидался конец файла, найден лишний символ");
                Advance();
            }
        }

        private void ParseEnumList()
        {
            if (IsAtEnd()) return;
            if (Peek().Value == "}") return;

            MatchIdentifier(new[] { ",", "}" }, "элемент перечисления");

            while (!IsAtEnd() && Peek().Value != "}")
            {
                MatchPunctuation(",", new[] { "Identifier", "}" }, "запятая ','");

                if (!IsAtEnd() && Peek().Value == "}")
                    break;

                MatchIdentifier(new[] { ",", "}" }, "элемент перечисления");
            }
        }

        private bool MatchKeyword(string value, string[] syncValues, string description)
        {
            return Match(TokenType.Keyword, value, syncValues, description);
        }

        private bool MatchIdentifier(string[] syncValues, string description)
        {
            return Match(TokenType.Identifier, null, syncValues, description);
        }

        private bool MatchPunctuation(string value, string[] syncValues, string description)
        {
            return Match(TokenType.Punctuation, value, syncValues, description);
        }

        private bool Match(TokenType expectedType, string expectedValue, string[] syncValues, string description)
        {
            if (IsAtEnd())
            {
                if (!_eofReported)
                {
                    if (_tokens.Count > 0)
                    {
                        var last = _tokens.Last();
                        AddError("EOF", $"строка {last.Line}, столбец {last.EndColumn + 1}", $"Неожиданный конец файла. Ожидалось: {description}", last.Line, last.EndColumn + 1);
                    }
                    else
                    {
                        AddError("EOF", "строка 1, столбец 1", $"Пустой файл. Ожидалось: {description}", 1, 1);
                    }
                    _eofReported = true;
                }
                return false;
            }

            var current = Peek();
            bool isMatch = false;

            if (expectedType == TokenType.Identifier)
                isMatch = current.Type == TokenType.Identifier;
            else
                isMatch = current.Type == expectedType && current.Value == expectedValue;

            if (isMatch)
            {
                Advance();
                return true;
            }

            AddError(current, $"Ожидалось: {description}");

            while (!IsAtEnd())
            {
                current = Peek();

                if (expectedType == TokenType.Identifier && current.Type == TokenType.Identifier)
                {
                    Advance();
                    return true;
                }
                if (current.Type == expectedType && current.Value == expectedValue)
                {
                    Advance();
                    return true;
                }

                bool isSync = false;
                if (current.Type == TokenType.Identifier && syncValues.Contains("Identifier"))
                    isSync = true;
                else if (syncValues.Contains(current.Value))
                    isSync = true;

                if (isSync)
                {
                    return false;
                }

                Advance();
            }

            _eofReported = true;

            return false;
        }

        private Token Peek()
        {
            if (IsAtEnd())
            {
                return _tokens.Count > 0 ? _tokens.Last() : new Token(TokenType.Error, "EOF", 1, 1, 1);
            }
            return _tokens[_pos];
        }

        private void Advance()
        {
            if (!IsAtEnd()) _pos++;
        }

        private bool IsAtEnd()
        {
            return _pos >= _tokens.Count;
        }

        private void AddError(Token token, string description)
        {
            _errors.Add(new SyntaxError
            {
                Fragment = token.Type == TokenType.Error ? $"'{token.Value}'" : token.Value,
                Location = $"строка {token.Line}, {token.StartColumn}-{token.EndColumn}",
                Description = description,
                Line = token.Line,
                StartColumn = token.StartColumn
            });
        }

        private void AddError(string fragment, string location, string description, int line, int col)
        {
            _errors.Add(new SyntaxError
            {
                Fragment = fragment,
                Location = location,
                Description = description,
                Line = line,
                StartColumn = col
            });
        }
    }
}
