namespace Compiler_1.Services
{
    public class SyntaxError
    {
        public string Fragment { get; set; }
        public string Description { get; set; }
        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }

        public string Location
        {
            get => $"строка {Line}, {StartColumn}-{EndColumn}";
            set { /* игнор */ }
        }
    }
}