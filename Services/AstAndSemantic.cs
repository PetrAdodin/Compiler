using System.Collections.Generic;
using System.Linq;

namespace Compiler_1.Services
{
    public abstract class AstNode
    {
        public abstract string Print(string indent = "", bool isLast = true);
    }

    public class EnumDeclarationNode : AstNode
    {
        public string Name { get; set; }
        public List<string> Modifiers { get; set; } = new List<string>();
        public List<EnumElementNode> Elements { get; set; } = new List<EnumElementNode>();
        public int Line { get; set; }
        public int Column { get; set; }

        public override string Print(string indent = "", bool isLast = true)
        {
            var sb = new System.Text.StringBuilder();
            string marker = isLast ? "└── " : "├── ";
            sb.AppendLine($"{indent}{marker}EnumDeclarationNode");
            indent += isLast ? "    " : "│   ";

            if (!string.IsNullOrEmpty(Name))
                sb.AppendLine($"{indent}├── name: \"{Name}\"");

            string mods = string.Join(", ", Modifiers.Select(m => $"\"{m}\""));
            sb.AppendLine($"{indent}├── modifiers: [{mods}]");

            if (Elements.Count == 0)
            {
                sb.AppendLine($"{indent}└── elements: []");
            }
            else
            {
                sb.AppendLine($"{indent}└── elements:");
                for (int i = 0; i < Elements.Count; i++)
                {
                    sb.Append(Elements[i].Print(indent + "    ", i == Elements.Count - 1));
                }
            }
            return sb.ToString();
        }
    }

    public class EnumElementNode : AstNode
    {
        public string Name { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public override string Print(string indent = "", bool isLast = true)
        {
            string marker = isLast ? "└── " : "├── ";
            return $"{indent}{marker}EnumElementNode (name: \"{Name}\")\n";
        }
    }

    public class ParseResult
    {
        public List<SyntaxError> Errors { get; set; } = new List<SyntaxError>();
        public List<EnumDeclarationNode> Declarations { get; set; } = new List<EnumDeclarationNode>();
    }

    public class SemanticError
    {
        public string Description { get; set; }
        public string Location { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public class SemanticAnalyzer
    {
        public List<SemanticError> Analyze(List<EnumDeclarationNode> declarations)
        {
            var errors = new List<SemanticError>();
            var declaredEnums = new Dictionary<string, (int Line, int Column)>();
            var nodesToRemove = new List<EnumDeclarationNode>();

            foreach (var decl in declarations)
            {
                if (string.IsNullOrEmpty(decl.Name)) continue;

                // Проверка уникальности имени
                if (!declaredEnums.TryAdd(decl.Name, (decl.Line, decl.Column)))
                {
                    var original = declaredEnums[decl.Name];
                    errors.Add(new SemanticError
                    {
                        Description = $"Ошибка: идентификатор \"{decl.Name}\" уже объявлен ранее (строка {original.Line})",
                        Location = $"строка {decl.Line}, символ {decl.Column}",
                        Line = decl.Line,
                        Column = decl.Column
                    });

                    // Убираю дубликат
                    nodesToRemove.Add(decl);
                    continue;
                }

                var declaredElements = new Dictionary<string, (int Line, int Column)>();
                var elementsToRemove = new List<EnumElementNode>();

                foreach (var elem in decl.Elements)
                {
                    if (string.IsNullOrEmpty(elem.Name)) continue;

                    // Проверка уникальности элементов внутри enum
                    if (!declaredElements.TryAdd(elem.Name, (elem.Line, elem.Column)))
                    {
                        var original = declaredElements[elem.Name];
                        errors.Add(new SemanticError
                        {
                            Description = $"Ошибка: идентификатор \"{elem.Name}\" уже объявлен ранее (строка {original.Line})",
                            Location = $"строка {elem.Line}, символ {elem.Column}",
                            Line = elem.Line,
                            Column = elem.Column
                        });
                        elementsToRemove.Add(elem);
                    }
                }

                foreach (var e in elementsToRemove)
                    decl.Elements.Remove(e);
            }

            foreach (var n in nodesToRemove)
                declarations.Remove(n);

            return errors;
        }
    }
}
