using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace JinSongPoc
{
    // Render-time aliases only. Native manufacturer IDs, table getters and predicates stay literal.
    internal static class OrganizationText
    {
        private static LoadedMod assets;
        private static string locale;
        private static Regex pattern;
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>(StringComparer.Ordinal);

        private static void Load()
        {
            string current = LocHelper.GetCurrentLocaleCode();
            if (assets == Mod.Assets && locale == current) return;
            assets = Mod.Assets; locale = current; pattern = null; Names.Clear();
            Dictionary<string, string> entries;
            if (assets == null || !assets.Localization.TryGetValue(current, out entries)) return;
            foreach (var language in assets.Localization)
            foreach (var entry in language.Value)
            {
                const string prefix = "customer_source_corpo_name_";
                if (!entry.Key.StartsWith(prefix, StringComparison.Ordinal) || String.IsNullOrEmpty(entry.Value)) continue;
                string target;
                if (entries.TryGetValue("organization_corpo_name_" + entry.Key.Substring(prefix.Length), out target))
                    Names[entry.Value] = target;
            }
            string[] sources = { "帕斯泰尔", "卡迈克尔", "达夫雷", "pastel", "carmicheal", "Davre" };
            string[] ids = { "pastel", "carmicheal", "davre", "pastel", "carmicheal", "davre" };
            for (int i = 0; i < sources.Length; i++)
            {
                string target;
                if (entries.TryGetValue("organization_corpo_name_" + ids[i], out target)) Names[sources[i]] = target;
            }
            var keys = new List<string>(Names.Keys);
            keys.Sort((a, b) => b.Length.CompareTo(a.Length));
            var expressions = new List<string>();
            foreach (string key in keys)
                expressions.Add(key[0] < 128 ? "(?<![A-Za-z])" + Regex.Escape(key) + "(?![A-Za-z])" : Regex.Escape(key));
            if (expressions.Count > 0) pattern = new Regex(String.Join("|", expressions.ToArray()));
        }

        internal static string Display(string value)
        {
            if (!Mod.Active || Mod.Assets == null || String.IsNullOrEmpty(value)) return value;
            Load();
            return pattern == null ? value : pattern.Replace(value, match => Names[match.Value]);
        }

        internal static string Manufacturer(string key)
        {
            string native = LocHelper.GetLocalizedClientName(key);
            return Mod.Active ? Mod.CurrentText("organization_" + key) ?? native : native;
        }

        [HarmonyPatch(typeof(GameItem), "DisplayManufacturer")]
        private static class ManufacturerLabel
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo native = AccessTools.Method(typeof(LocHelper), "GetLocalizedClientName");
                MethodInfo display = AccessTools.Method(typeof(OrganizationText), "Manufacturer");
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.Calls(native)) instruction.operand = display;
                    yield return instruction;
                }
            }
        }

        [HarmonyPatch(typeof(LocHelper), "Get", new Type[] { typeof(string), typeof(string), typeof(object[]) })]
        private static class GuideLookup
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(string __0, string __1, ref string __result)
            {
                if (__0 == "UI" && IsGuide(__1)) __result = Display(__result);
            }
        }

        internal static bool IsGuide(string key)
        { return key != null && (key.StartsWith("book_", StringComparison.Ordinal) || key.StartsWith("guide_", StringComparison.Ordinal)); }

    }
}
