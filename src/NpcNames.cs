using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TMPro;

namespace JinSongPoc
{
    internal static class NpcNames
    {
        private const string Prefix = "customer_";
        private static Dictionary<string, string> names;
        private static string namesLocale;
        private static readonly Dictionary<string, string> SourceKeys = new Dictionary<string, string>(StringComparer.Ordinal);
        private static TMP_Text cachedSpeaker;
        private static string cachedNative, cachedApplied;

        internal static void Clear()
        {
            if (cachedSpeaker != null && cachedSpeaker.text == cachedApplied) cachedSpeaker.text = cachedNative;
            cachedSpeaker = null; cachedNative = cachedApplied = null;
            names = null;
            namesLocale = null;
            SourceKeys.Clear();
        }

        private static Dictionary<string, string> ReadNames()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            Dictionary<string, string> localized;
            if (!Mod.Assets.Localization.TryGetValue(LocHelper.GetCurrentLocaleCode(), out localized)) return result;
            foreach (var entry in localized)
            {
                if (!entry.Key.StartsWith(Prefix + "name_", StringComparison.Ordinal)
                    && !entry.Key.StartsWith(Prefix + "faction_name_", StringComparison.Ordinal)
                    && !entry.Key.StartsWith(Prefix + "location_", StringComparison.Ordinal)) continue;
                AddName(result, entry.Key.Substring(Prefix.Length), entry.Value);
            }
            AddName(result, "name_james_bailey", Mod.CurrentText("wanted1_name"));
            AddName(result, "name_the_butcher", Mod.CurrentText("wanted2_name"));
            return result;
        }

        private static void AddName(Dictionary<string, string> result, string key, string replacement)
        {
            if (String.IsNullOrEmpty(replacement)) return;
            string original = LocHelper.GetLocalizedClientName(key);
            if (!String.IsNullOrEmpty(original) && original != key) result[original] = replacement;
        }

        private static string ForDisplay(string original)
        {
            if (String.IsNullOrEmpty(original) || !Mod.Active || Mod.Assets == null) return original;
            string current = LocHelper.GetCurrentLocaleCode();
            if (names == null || namesLocale != current) { names = ReadNames(); namesLocale = current; }
            string replacement;
            if (names.TryGetValue(original, out replacement)) return replacement;
            string key = SourceKey(original);
            return key == null ? original : Mod.CurrentText(Prefix + key) ?? LocHelper.GetLocalizedClientName(key);
        }

        private static string SourceKey(string value)
        {
            if (value == null || Mod.Assets == null) return null;
            if (SourceKeys.Count == 0)
            {
                foreach (var language in Mod.Assets.Localization)
                    foreach (var pair in language.Value)
                        if (pair.Key.StartsWith("customer_source_", StringComparison.Ordinal)
                            && !String.IsNullOrEmpty(pair.Value) && pair.Value.IndexOf('{') < 0 && !SourceKeys.ContainsKey(pair.Value))
                            SourceKeys.Add(pair.Value, pair.Key.Substring("customer_source_".Length));
                SourceKeys["革命军士兵"] = "name_revolutionary_fighter";
                SourceKeys["革命軍戰士"] = "name_revolutionary_fighter";
            }
            string key;
            return SourceKeys.TryGetValue(value, out key) ? key : null;
        }
        internal static string DisplayValue(string value) { return ForDisplay(value); }
        // Refresh the visible cached label as well as the normal IL display calls.
        // A cached dialogue line can be restored without executing its original name getter.
        internal static void RefreshSpeakerLabels()
        {
            DialogUIManager ui = DialogUIManager.Instance;
            if (ui == null || ui.clientNameTMP == null || !ui.clientNameTMP.isActiveAndEnabled) return;
            TMP_Text label = ui.clientNameTMP;
            string replacement = ForDisplay(label.text);
            if (replacement != label.text)
            {
                cachedSpeaker = label; cachedNative = label.text; cachedApplied = replacement;
                label.text = replacement;
            }
        }
        internal static string NativeValue(string value)
        {
            string key = SourceKey(value);
            return key == null ? value : LocHelper.GetLocalizedClientName(key);
        }

        private static string DisplayClient(StoreClient client)
        {
            return ForDisplay(client.displayName);
        }

        private static string DisplayRole(string key)
        {
            return ForDisplay(LocHelper.GetLocalizedClientName(key));
        }

        internal static string DisplayContact(StorePhoneClient contact)
        {
            string original = contact.GetLocalizedDisplayName();
            return Mod.CurrentText(Prefix + contact.locID) ?? original;
        }

        [HarmonyPatch]
        private static class FactionLabels
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(ReputationElement), "Init", new Type[] { typeof(StoreReputation) });
                yield return AccessTools.Method(typeof(ReputationUIManager), "NotifyReputation", new Type[] { typeof(string), typeof(int) });
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo field = AccessTools.Field(typeof(StoreReputation), "factionDisplay");
                MethodInfo getter = AccessTools.Method(typeof(StoreReputation), "GetFactionNameByID");
                MethodInfo display = AccessTools.Method(typeof(NpcNames), "ForDisplay");
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                    if (instruction.LoadsField(field) || instruction.Calls(getter))
                        yield return new CodeInstruction(OpCodes.Call, display);
                }
            }
        }

        [HarmonyPatch]
        private static class SpeakerLabels
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(DialogUIManager), "DisplayClientText", new Type[] { typeof(Dialogue) });
                yield return AccessTools.Method(typeof(DialogUIManager), "DisplaySecText", new Type[] { typeof(Dialogue) });
                yield return AccessTools.Method(typeof(DialogUIManager), "PlayClientSingleText", new Type[] { typeof(string) });
                yield return AccessTools.Method(typeof(NegociationUIManager), "RefreshClientHeader", new Type[] { typeof(StoreClient) });
                yield return AccessTools.Method(typeof(DialoguePhoneUIManager), "DisplayClientText", new Type[] { typeof(Dialogue) });
                yield return AccessTools.Method(typeof(DialoguePhoneUIManager), "PlayClientSingleText", new Type[] { typeof(string) });
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo stockName = AccessTools.Field(typeof(StoreClient), "displayName");
                MethodInfo localizedName = AccessTools.Method(typeof(LocHelper), "GetLocalizedClientName");
                MethodInfo displayName = AccessTools.Method(typeof(NpcNames), "DisplayClient");
                MethodInfo displayRole = AccessTools.Method(typeof(NpcNames), "DisplayRole");
                MethodInfo stockContact = AccessTools.Method(typeof(StorePhoneClient), "GetLocalizedDisplayName");
                MethodInfo displayContact = AccessTools.Method(typeof(NpcNames), "DisplayContact");
                // Keep the native name on the customer blueprint, including aliases and disguise state.
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.LoadsField(stockName))
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = displayName;
                    }
                    else if (instruction.Calls(localizedName)) instruction.operand = displayRole;
                    else if (instruction.Calls(stockContact))
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = displayContact;
                    }
                    yield return instruction;
                }
            }
        }
    }
}
