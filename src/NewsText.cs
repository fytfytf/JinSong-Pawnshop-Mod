using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace JinSongPoc
{
    // Pure display matching: never owns an event, its dates, callbacks or persisted fields.
    internal sealed class NewsText
    {
        private sealed class Alias
        {
            internal string Source, Key;
            internal Regex Pattern;
            internal int ArgumentCount;
        }
        private readonly List<Alias> aliases = new List<Alias>();
        private readonly Dictionary<string, Alias> bySource = new Dictionary<string, Alias>(StringComparer.Ordinal);
        private readonly bool keepTail;

        internal NewsText(bool keepTail) { this.keepTail = keepTail; }

        internal void Add(string source, string key)
        {
            if (String.IsNullOrEmpty(source)) return;
            Alias prior;
            if (bySource.TryGetValue(source, out prior))
            {
                if (prior.Key != key) prior.Key = null;
                return;
            }
            var alias = new Alias { Source = source, Key = key };
            var expression = new StringBuilder(@"\A");
            int position = 0;
            var seen = new HashSet<int>();
            foreach (Match token in Regex.Matches(source, @"\{([0-9]+)\}"))
            {
                int argument;
                if (!Int32.TryParse(token.Groups[1].Value, out argument) || argument > 15) return;
                expression.Append(Regex.Escape(source.Substring(position, token.Index - position)));
                expression.Append(seen.Add(argument) ? "(?<arg" + argument + ">.*?)" : @"\k<arg" + argument + ">");
                alias.ArgumentCount = Math.Max(alias.ArgumentCount, argument + 1);
                position = token.Index + token.Length;
            }
            expression.Append(Regex.Escape(source.Substring(position)));
            expression.Append(keepTail && source.Length >= 20 ? "(?<tail>.*)" : "");
            expression.Append(@"\z");
            alias.Pattern = new Regex(expression.ToString(), RegexOptions.Singleline | RegexOptions.CultureInvariant);
            aliases.Add(alias);
            bySource.Add(source, alias);
            aliases.Sort((a, b) => b.Source.Length.CompareTo(a.Source.Length));
        }

        internal string Translate(string original, Func<string, object[], string> localize)
        {
            if (String.IsNullOrEmpty(original)) return original;
            foreach (Alias alias in aliases)
            {
                if (alias.Key == null) continue;
                Match match = alias.Pattern.Match(original);
                if (!match.Success) continue;
                var arguments = new object[alias.ArgumentCount];
                for (int i = 0; i < arguments.Length; i++) arguments[i] = match.Groups["arg" + i].Value;
                string replacement;
                try { replacement = localize(alias.Key, arguments); }
                catch (FormatException) { return original; }
                return replacement == null ? original : replacement + match.Groups["tail"].Value;
            }
            return original;
        }
    }
}
