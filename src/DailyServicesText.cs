using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace JinSongPoc
{
    // The native services retain their cached names, prices, cooldowns and payment callbacks.
    internal static class DailyServicesText
    {
        private static readonly Dictionary<string, string> Keys = new Dictionary<string, string> {
            { "POWER_SERVICE", "power" }, { "JANITORIAL_SERVICE", "janitorial" },
            { "INSURANCE_SERVICE", "insurance" }, { "CARTEL_PROTECTION", "cartel" },
            { "SEC_INFORMANT", "inspection" }, { "BUY_REFERAL", "buy_referral" },
            { "SELL_REFERAL", "sell_referral" }, { "CRIMINEL_NETWORK", "criminal_network" },
            { "WILD_FIXER", "fixer" }, { "CURRENT_CONVERTER_SERVICE", "converter" },
            { "DEBRIS_REMOVAL_SERVICE", "debris" }, { "BUTCHER_HUNTING_SERVICE", "butcher" },
            { "HYPERTECH_SMUGGLER_SERVICE", "smuggler" }
        };

        private static string Name(StoreService service)
        {
            if (service == null) return null;
            string key;
            if (!Mod.Active || service.id == null || !Keys.TryGetValue(service.id, out key)) return service.displayName;
            // Preserve the meaningful distinction between native fixer upgrade levels.
            if (key == "fixer")
            {
                if (service.displayName == LocHelper.GetLocalizedNetworkUpgrade("network_WILD_FIXER3_title")
                    || service.displayName == "王尔德的善后人 III" || service.displayName == "Wild's Fixers III") key = "fixer3";
                else if (service.displayName == LocHelper.GetLocalizedNetworkUpgrade("network_WILD_FIXER2_title")
                    || service.displayName == "王尔德的善后人 II" || service.displayName == "Wild's Fixers II") key = "fixer2";
            }
            return Mod.CurrentText("daily_service_" + key) ?? service.displayName;
        }

        [HarmonyPatch(typeof(EndOfDayUIManager), "BuildServiceRow")]
        private static class NativeRows
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo native = AccessTools.Field(typeof(StoreService), "displayName");
                MethodInfo display = AccessTools.Method(typeof(DailyServicesText), "Name");
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.LoadsField(native)) { instruction.opcode = OpCodes.Call; instruction.operand = display; }
                    yield return instruction;
                }
            }
        }
    }
}
