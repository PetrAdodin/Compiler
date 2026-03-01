using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler_1.Services
{
    public enum TokenType
    {
        Keyword,
        Identifier,
        Number,
        StringLiteral, // 'enum...' не ключевое слово
        CharLiteral,   // Для символов
        Punctuation,
        EndOfLine
    }
}
