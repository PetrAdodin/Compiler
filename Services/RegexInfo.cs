namespace Compiler_1.Services
{
    public class RegexInfo
    {
        public string Value { get; set; }
        public int Line { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
        public string Location { get; set; }
    }
}