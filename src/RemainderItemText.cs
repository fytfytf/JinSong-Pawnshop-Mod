using System;
using System.Collections.Generic;

namespace JinSongPoc
{
    // Exact display-value aliases only. Never hooks a native table getter used by constructors or saves.
    internal static class RemainderItemText
    {
        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        private static string locale;
        private static LoadedMod assets;
        internal static void Clear() { Aliases.Clear(); locale = null; assets = null; }

        private static void Load(string current)
        {
            Clear();
            locale = current;
            assets = Mod.Assets;
            Dictionary<string, string> entries;
            if (!assets.Localization.TryGetValue(current, out entries)) return;
            var ambiguous = new HashSet<string>(StringComparer.Ordinal);
            const string sourcePrefix = "remainder_source_Item_";
            // Saved/native display fields may still contain the language used at creation.
            // Only source captures from our own tables participate; target is the current locale.
            foreach (var language in assets.Localization)
            foreach (var pair in language.Value)
            {
                if (!pair.Key.StartsWith(sourcePrefix, StringComparison.Ordinal) || String.IsNullOrEmpty(pair.Value)) continue;
                string key = "remainder_Item_" + pair.Key.Substring(sourcePrefix.Length);
                string field = key.EndsWith("_name", StringComparison.Ordinal) ? "_name"
                    : key.EndsWith("_desc", StringComparison.Ordinal) ? "_desc"
                    : key.EndsWith("_flavor", StringComparison.Ordinal) ? "_flavor" : null;
                if (field == null) continue;
                string identity = field + "\u001f" + pair.Value;
                string replacement;
                if (!entries.TryGetValue(key, out replacement) || ambiguous.Contains(identity)) continue;
                string prior;
                if (Aliases.TryGetValue(identity, out prior) && prior != replacement)
                { Aliases.Remove(identity); ambiguous.Add(identity); }
                else Aliases[identity] = replacement;
            }
        }

        internal static string Get(GameItem item, string suffix)
        {
            if (!Mod.Active || Mod.Assets == null || item == null || item.useSpriteFromMod) return null;
            string current = LocHelper.GetCurrentLocaleCode();
            if (assets != Mod.Assets || locale != current) Load(current);
            string native = suffix == "_name" ? item.name : suffix == "_desc" ? item.shortDescription : item.flavorText;
            string value;
            return !String.IsNullOrEmpty(native) && Aliases.TryGetValue(suffix + "\u001f" + native, out value) ? value : null;
        }
    }
}
