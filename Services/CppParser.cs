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

        public ParseResult Parse()
        {
            _pos = 0;
            _errors.Clear();
            _eofReported = false;

            var result = new ParseResult { Errors = _errors, Declarations = new List<EnumDeclarationNode>() };

            if (_tokens.Count == 0)
            {
                return result;
            }

            while (!IsAtEnd())
            {
                var current = Peek();
                if (current.Type == TokenType.Keyword &&
                    (current.Value == "enum" || current.Value == "class"))
                {
                    var decl = ParseEnumDeclaration();
                    if (decl != null)
                        result.Declarations.Add(decl);
                }
                else
                {
                    AddError(current, "Ожидалось объявление перечисления (enum)");
                    while (!IsAtEnd() &&
                           !(Peek().Type == TokenType.Keyword &&
                             (Peek().Value == "enum" || Peek().Value == "class")))
                    {
                        Advance();
                    }
                }
            }

            return result;
        }

        private EnumDeclarationNode ParseEnumDeclaration()
        {
            var node = new EnumDeclarationNode();

            var enumToken = Peek();
            if (MatchKeyword("enum", new[] { "class", "Identifier", "{" }, "ключевое слово 'enum'"))
            {
                node.Modifiers.Add("enum");
                node.Line = enumToken.Line;
                node.Column = enumToken.StartColumn;
            }

            var classToken = Peek();
            if (MatchKeyword("class", new[] { "Identifier", "{" }, "ключевое слово 'class'"))
            {
                node.Modifiers.Add("class");
                if (node.Line == 0) { node.Line = classToken.Line; node.Column = classToken.StartColumn; }
            }

            var idToken = Peek();
            if (MatchIdentifier(new[] { "{" }, "имя перечисления"))
            {
                node.Name = idToken.Value;
                if (node.Line == 0) { node.Line = idToken.Line; node.Column = idToken.StartColumn; }
            }

            MatchPunctuation("{", new[] { "Identifier", "}", ";" }, "открывающая фигурная скобка '{'");

            node.Elements.AddRange(ParseEnumList());

            MatchPunctuation("}", new[] { ";" }, "закрывающая фигурная скобка '}'");
            MatchPunctuation(";", new[] { "enum" }, "точка с запятой ';'");

            return node;
        }

        private List<EnumElementNode> ParseEnumList()
        {
            var elements = new List<EnumElementNode>();

            if (IsAtEnd()) return elements;
            if (Peek().Value == "}") return elements;

            var token = Peek();
            if (MatchIdentifier(new[] { ",", "}" }, "элемент перечисления"))
            {
                elements.Add(new EnumElementNode { Name = token.Value, Line = token.Line, Column = token.StartColumn });
            }

            while (!IsAtEnd() && Peek().Value != "}")
            {
                MatchPunctuation(",", new[] { "Identifier", "}" }, "запятая ','");

                if (!IsAtEnd() && Peek().Value == "}")
                    break;

                token = Peek();
                if (MatchIdentifier(new[] { ",", "}" }, "элемент перечисления"))
                {
                    elements.Add(new EnumElementNode { Name = token.Value, Line = token.Line, Column = token.StartColumn });
                }
            }

            return elements;
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
                        AddError("EOF", $"строка {last.Line}, столбец {last.EndColumn + 1}",
                                 $"Неожиданный конец файла. Ожидалось: {description}",
                                 last.Line, last.EndColumn + 1);
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
                return _tokens.Count > 0
                    ? _tokens.Last()
                    : new Token(TokenType.Error, "EOF", 1, 1, 1);
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
