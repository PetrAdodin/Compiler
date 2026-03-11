namespace Compiler_1.Services
{
    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }

        public Token(TokenType type, string value, int line, int startColumn, int endColumn)
        {
            Type = type;
            Value = value;
            Line = line;
            StartColumn = startColumn;
            EndColumn = endColumn;
        }

        public Token(TokenType type, string value, int line, int startColumn)
            : this(type, value, line, startColumn, startColumn + value.Length - 1)
        {
        }
    }
}