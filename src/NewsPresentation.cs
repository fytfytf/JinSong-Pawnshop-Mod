using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace JinSongPoc
{
    internal static class NewsPresentation
    {
        private static NewsText names, bodies;
        private static LoadedMod assets;
        private static string locale;

        internal static void Clear() { names = null; bodies = null; assets = null; locale = null; }

        private static void Load()
        {
            string current = LocHelper.GetCurrentLocaleCode();
            if (names != null && assets == Mod.Assets && locale == current) return;
            names = new NewsText(false);
            bodies = new NewsText(true);
            assets = Mod.Assets;
            locale = current;
            Dictionary<string, string> targets;
            if (assets == null || !assets.Localization.TryGetValue(current, out targets)) return;
            foreach (var language in assets.Localization)
            foreach (var entry in language.Value)
            {
                const string prefix = "news_source_";
                if (!entry.Key.StartsWith(prefix, StringComparison.Ordinal)) continue;
                int version = entry.Key.LastIndexOf("_v", StringComparison.Ordinal);
                if (version < prefix.Length) continue;
                string nativeKey = entry.Key.Substring(prefix.Length, version - prefix.Length);
                string target = "news_text_" + nativeKey;
                if (!targets.ContainsKey(target)) continue;
                if (nativeKey.EndsWith("_news", StringComparison.Ordinal)) names.Add(entry.Value, target);
                else if (nativeKey.EndsWith("_desc", StringComparison.Ordinal)) bodies.Add(entry.Value, target);
            }
        }

        private static string Name(StoreEvent value)
        {
            string original = value.newsName;
            if (!Mod.Active || Mod.Assets == null) return original;
            string assassin = AssassinStory.LocalizedNews(value, true);
            if (assassin != null) return assassin;
            Load();
            return names.Translate(original, Mod.CurrentText);
        }
        private static string Body(StoreEvent value)
        {
            string original = value.newsDescription;
            if (!Mod.Active || Mod.Assets == null) return original;
            string assassin = AssassinStory.LocalizedNews(value, false);
            if (assassin != null) return assassin;
            Load();
            return bodies.Translate(original, Mod.CurrentText);
        }

        [HarmonyPatch(typeof(NewsUIElement), "PopulateUI", new[] { typeof(StoreEvent) })]
        private static class DisplayNews
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var body = new List<CodeInstruction>(instructions);
                FieldInfo title = AccessTools.Field(typeof(StoreEvent), "newsName");
                FieldInfo description = AccessTools.Field(typeof(StoreEvent), "newsDescription");
                int titles = 0, descriptions = 0;
                foreach (CodeInstruction instruction in body)
                {
                    if (instruction.LoadsField(title)) titles++;
                    if (instruction.LoadsField(description)) descriptions++;
                }
                if (titles != 1 || descriptions != 1)
                {
                    UnityEngine.Debug.LogWarning("[JinSong] News display field shape changed; original retained.");
                    return body;
                }
                foreach (CodeInstruction instruction in body)
                {
                    MethodInfo method = instruction.LoadsField(title) ? AccessTools.Method(typeof(NewsPresentation), "Name")
                        : instruction.LoadsField(description) ? AccessTools.Method(typeof(NewsPresentation), "Body") : null;
                    if (method == null) continue;
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = method;
                }
                return body;
            }
        }
    }
}
