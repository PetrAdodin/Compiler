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
            var results = new List<RegexInfo>();
            if (string.IsNullOrEmpty(text)) return results;

            string pattern = @"(?=(\S{12,})(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[/#?!@_$/%\^&*\-|]))";
            Regex regex = new Regex(pattern, RegexOptions.Compiled);
            MatchCollection matches = regex.Matches(text);

            foreach (Match match in matches)
            {
                if (match.Groups[1].Success)
                {
                    string value = match.Groups[1].Value;
                    int startIndex = match.Groups[1].Index;
                    int length = value.Length;
                    (int line, int column) = GetLineAndColumn(text, startIndex);

                    results.Add(new RegexInfo
                    {
                        Value = value,
                        Line = line,
                        StartColumn = column,
                        EndColumn = column + length - 1,   // исправлено: ранее был абсолютный индекс
                        Location = $"строка {line}, {column}"
                    });
                }
            }
            return results;
        }

        public static List<RegexInfo> SearchUsername(string text)
        {
            var results = new List<RegexInfo>();
            if (string.IsNullOrEmpty(text)) return results;

            const int StateStart = 0;
            const int StateInName = 1;
            const int StateAccept = 2;

            int state = StateStart;
            int length = 0;
            int startIndex = -1;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                bool isValidChar = IsValidUsernameChar(c);

                switch (state)
                {
                    case StateStart:
                        if (isValidChar)
                        {
                            state = StateInName;
                            startIndex = i;
                            length = 1;
                        }
                        break;

                    case StateInName:
                        if (isValidChar)
                        {
                            length++;
                            if (length >= 8 && length <= 16)
                            {
                                state = StateAccept;
                            }
                            else if (length > 16)
                            {
                                state = StateStart;
                                startIndex = -1;

                                if (isValidChar)
                                {
                                    state = StateInName;
                                    startIndex = i;
                                    length = 1;
                                }
                            }
 
                        }
                        else
                        {
                            if (length >= 8 && length <= 16 && startIndex != -1)
                            {
                                int endIndex = i - 1;
                                int finalLength = endIndex - startIndex + 1;
                                string username = text.Substring(startIndex, finalLength);
                                (int line, int column) = GetLineAndColumn(text, startIndex);

                                results.Add(new RegexInfo
                                {
                                    Value = username,
                                    Line = line,
                                    StartColumn = column,
                                    EndColumn = column + finalLength - 1,
                                    Location = $"строка {line}, {column}"
                                });
                            }

                            state = StateStart;
                            startIndex = -1;
                        }
                        break;

                    case StateAccept:
                        if (isValidChar)
                        {
                            length++;
                            if (length > 16)
                            {
                                int endIndex = i - 1;
                                int finalLength = endIndex - startIndex + 1;
                                string username = text.Substring(startIndex, finalLength);
                                (int line, int column) = GetLineAndColumn(text, startIndex);

                                results.Add(new RegexInfo
                                {
                                    Value = username,
                                    Line = line,
                                    StartColumn = column,
                                    EndColumn = column + finalLength - 1,
                                    Location = $"строка {line}, {column}"
                                });

                                state = StateInName;
                                startIndex = i;
                                length = 1;
                            }
                        }
                        else
                        {
                            if (length >= 8 && length <= 16 && startIndex != -1)
                            {
                                int endIndex = i - 1;
                                int finalLength = endIndex - startIndex + 1;
                                string username = text.Substring(startIndex, finalLength);
                                (int line, int column) = GetLineAndColumn(text, startIndex);

                                results.Add(new RegexInfo
                                {
                                    Value = username,
                                    Line = line,
                                    StartColumn = column,
                                    EndColumn = column + finalLength - 1,
                                    Location = $"строка {line}, {column}"
                                });
                            }

                            state = StateStart;
                            startIndex = -1;
                        }
                        break;
                }
            }

            if ((state == StateInName || state == StateAccept) && startIndex != -1 && length >= 8 && length <= 16)
            {
                int endIndex = text.Length - 1;
                int finalLength = endIndex - startIndex + 1;
                string username = text.Substring(startIndex, finalLength);
                (int line, int column) = GetLineAndColumn(text, startIndex);

                results.Add(new RegexInfo
                {
                    Value = username,
                    Line = line,
                    StartColumn = column,
                    EndColumn = column + finalLength - 1,
                    Location = $"строка {line}, {column}"
                });
            }

            return results;
        }

        private static bool IsValidUsernameChar(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                   (c >= '0' && c <= '9') ||
                   c == '-' || c == '_';
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
                    StartColumn = column,
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