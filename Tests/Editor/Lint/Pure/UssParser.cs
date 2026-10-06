using System.Collections.Generic;
using System.Text;

namespace FutureDigitalPlus.UIKit.Lint
{
    /// <summary>One <c>name: value</c> declaration of a USS rule.</summary>
    public sealed class UssDeclaration
    {
        /// <summary>Property or custom property name, as written (<c>--fdp-color-bg</c>).</summary>
        public string Name;

        /// <summary>Value text without the trailing semicolon, trimmed.</summary>
        public string Value;

        /// <summary>1 based line of the declaration.</summary>
        public int Line;
    }

    /// <summary>One USS rule (<c>selector { declarations }</c>) or one at-rule (<c>@import ...;</c> or <c>@media ... { }</c>).</summary>
    public sealed class UssRule
    {
        /// <summary>The selector text (or the whole at-rule head), trimmed.</summary>
        public string Selector;

        /// <summary>1 based line where the rule starts.</summary>
        public int Line;

        /// <summary>True when <see cref="Selector"/> starts with <c>@</c>.</summary>
        public bool IsAtRule;

        /// <summary>The declarations of the rule body. Empty for at-rules.</summary>
        public List<UssDeclaration> Declarations = new List<UssDeclaration>();
    }

    /// <summary>
    /// A small, forgiving USS reader for lint rules: comments removed, rules and declarations split, line numbers kept.
    /// It is not a validator; it only has to be right about the structure of the sheets of this package.
    /// </summary>
    public static class UssParser
    {
        /// <summary>Replaces every <c>/* ... */</c> comment by spaces (newlines stay, so line numbers are unchanged). Quoted strings are respected.</summary>
        public static string StripComments(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var sb = new StringBuilder(text.Length);
            char quote = '\0';

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (quote != '\0')
                {
                    sb.Append(c);
                    if (c == '\\' && i + 1 < text.Length) sb.Append(text[++i]);
                    else if (c == quote) quote = '\0';
                    continue;
                }

                if (c == '"' || c == '\'')
                {
                    quote = c;
                    sb.Append(c);
                }
                else if (c == '/' && i + 1 < text.Length && text[i + 1] == '*')
                {
                    int end = text.IndexOf("*/", i + 2, System.StringComparison.Ordinal);
                    int stop = end < 0 ? text.Length : end + 2;
                    for (int j = i; j < stop; j++) sb.Append(text[j] == '\n' ? '\n' : ' ');
                    i = stop - 1;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>Parses a sheet into rules. Nested blocks (in unsupported at-rules such as <c>@media</c>) are skipped as one at-rule.</summary>
        public static List<UssRule> Parse(string text)
        {
            var rules = new List<UssRule>();
            string src = StripComments(text);
            int line = 1;
            int i = 0;

            while (i < src.Length)
            {
                // Skip whitespace between rules.
                while (i < src.Length && char.IsWhiteSpace(src[i]))
                {
                    if (src[i] == '\n') line++;
                    i++;
                }

                if (i >= src.Length) break;

                int headStart = i;
                int headLine = line;
                char quote = '\0';
                int paren = 0;

                // Read the head up to '{' or ';' (outside strings and parentheses).
                while (i < src.Length)
                {
                    char c = src[i];
                    if (quote != '\0')
                    {
                        if (c == '\\') i++;
                        else if (c == quote) quote = '\0';
                    }
                    else if (c == '"' || c == '\'') quote = c;
                    else if (c == '(') paren++;
                    else if (c == ')') paren = paren > 0 ? paren - 1 : 0;
                    else if (paren == 0 && (c == '{' || c == ';' || c == '}')) break;

                    if (c == '\n') line++;
                    i++;
                }

                string head = src.Substring(headStart, i - headStart).Trim();
                var rule = new UssRule { Selector = head, Line = headLine, IsAtRule = head.StartsWith("@") };

                if (i >= src.Length)
                {
                    if (head.Length > 0) rules.Add(rule);
                    break;
                }

                char stop = src[i];

                if (stop == '{')
                {
                    int bodyStart = ++i;
                    int bodyLine = line;
                    int depth = 1;
                    quote = '\0';

                    while (i < src.Length && depth > 0)
                    {
                        char c = src[i];
                        if (quote != '\0')
                        {
                            if (c == '\\') i++;
                            else if (c == quote) quote = '\0';
                        }
                        else if (c == '"' || c == '\'') quote = c;
                        else if (c == '{') depth++;
                        else if (c == '}') depth--;

                        if (depth > 0)
                        {
                            if (c == '\n') line++;
                            i++;
                        }
                    }

                    if (!rule.IsAtRule) ParseDeclarations(src.Substring(bodyStart, i - bodyStart), bodyLine, rule.Declarations);
                    if (i < src.Length) i++; // the closing brace
                }
                else
                {
                    i++; // ';' or a stray '}'
                }

                if (head.Length > 0) rules.Add(rule);
            }

            return rules;
        }

        private static void ParseDeclarations(string body, int firstLine, List<UssDeclaration> into)
        {
            int line = firstLine;
            int start = 0;
            int startLine = line;
            int paren = 0;
            char quote = '\0';

            for (int i = 0; i <= body.Length; i++)
            {
                bool end = i == body.Length;
                char c = end ? ';' : body[i];

                if (!end)
                {
                    if (quote != '\0')
                    {
                        if (c == '\\') i++;
                        else if (c == quote) quote = '\0';
                    }
                    else if (c == '"' || c == '\'') quote = c;
                    else if (c == '(') paren++;
                    else if (c == ')') paren = paren > 0 ? paren - 1 : 0;
                }

                if (!end && quote == '\0' && c == '\n') line++;

                if (end || (quote == '\0' && paren == 0 && c == ';'))
                {
                    string piece = body.Substring(start, i - start);
                    int colon = piece.IndexOf(':');
                    if (colon > 0)
                    {
                        string name = piece.Substring(0, colon).Trim();
                        string value = piece.Substring(colon + 1).Trim();
                        if (name.Length > 0)
                        {
                            // The declaration's line is the line of its first non-space character.
                            int lead = 0;
                            while (lead < piece.Length && char.IsWhiteSpace(piece[lead]))
                            {
                                if (piece[lead] == '\n') startLine++;
                                lead++;
                            }

                            into.Add(new UssDeclaration { Name = name, Value = value, Line = startLine });
                        }
                    }

                    start = i + 1;
                    startLine = line;
                }
            }
        }
    }
}
