using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Text.RegularExpressions;

namespace Compiler_1.Services
{
    public static class RegexSearchService
    {
        public static List<RegexInfo> SearchRussianVowelsExceptA(string text)
        {
            string pattern = @"[еёиоуыэюяЕЁИОУЫЭЮЯ]";
            return FindMatches(text, pattern);
        }

        public static List<RegexInfo> SearchEthereumAddresses(string text)
        {
            string pattern = @"\b0x[a-fA-F0-9]{40}\b";
            return FindMatches(text, pattern);
        }

        public static List<RegexInfo> SearchStrongPasswords(string text)
        {
            string pattern = @"(?=\S{12,})(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[/#?!@_$/%\^&*\-|])\S+";
            return FindMatches(text, pattern);
        }

        private static List<RegexInfo> FindMatches(string text, string pattern)
        {
            var results = new List<RegexInfo>();
            if (string.IsNullOrEmpty(text)) return results;

            Regex regex = new Regex(pattern, RegexOptions.Compiled);
            MatchCollection matches = regex.Matches(text);

            foreach (Match match in matches)
            {
                int startIndex = match.Index;
                int length = match.Length;
                string value = match.Value;

                (int line, int column) = GetLineAndColumn(text, startIndex);

                results.Add(new RegexInfo
                {
                    Value = value,
                    StartColumn = startIndex,
                    EndColumn = startIndex + length,
                    Line = line,
                    Location = $"строка {line}, {column}"
                });
            }
            return results;
        }

        private static (int line, int column) GetLineAndColumn(string text, int index)
        {
            int line = 1, column = 1;
            for (int i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    column = 1;
                }
                else
                {
                    column++;
                }
            }
            return (line, column);
        }
    }
}