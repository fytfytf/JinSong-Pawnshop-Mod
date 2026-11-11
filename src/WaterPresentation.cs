using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace JinSongPoc
{
    // Only redirect reads inside the native strip window. Feature getters and
    // shared condition/static names also serve business rules and stay original.
    internal static class WaterPresentation
    {
        private static readonly string[] Fields = {
            "PURE_WATER_DISPLAY", "HIGH_QUALITY_WATER_DISPLAY", "BASE_WATER_DISPLAY",
            "GHOSTWATER_DISPLAY", "RUST_WATER_DISPLAY", "GUTTERFLOW_WATER_DISPLAY" };
        private static readonly string[] Keys = {
            "mech_condition_pure_water", "mech_condition_high_quality_water", "mech_condition_basewater",
            "mech_condition_ghostwater", "mech_condition_rustwater", "mech_condition_gutterflow" };

        private static bool NativeStrip(GameItem strip)
        {
            return Mod.Active && strip != null && strip.identifier == "water_test_strip"
                && strip.spriteAtlasPath == "Items/items_amenities" && strip.IsTag("IS_SPENT");
        }
        private static string UiName(string original, string key, GameItem strip)
        {
            if (!NativeStrip(strip)) return original;
            string current = LocHelper.GetLocalizedMechanic(key);
            return String.IsNullOrEmpty(current) ? original : current;
        }
        private static string ConditionName(ItemCondition condition, GameItem strip)
        {
            string original = condition.display;
            if (!NativeStrip(strip)) return original;
            List<ItemCondition> conditions = ItemConditionList.conditionWaterPurity;
            if (conditions == null || conditions.Count != Keys.Length) return original;
            for (int i = 0; i < Keys.Length; i++)
                if (ReferenceEquals(conditions[i], condition)) return UiName(original, Keys[i], strip);
            return original;
        }
        private static int StaticIndex(CodeInstruction instruction)
        {
            FieldInfo field = instruction.operand as FieldInfo;
            if (field == null || field.DeclaringType != typeof(WaterFeatureHelper)
                || !instruction.LoadsField(field)) return -1;
            return Array.IndexOf(Fields, field.Name);
        }
        private static bool ConditionRead(CodeInstruction instruction)
        {
            FieldInfo field = instruction.operand as FieldInfo;
            return field != null && field.DeclaringType == typeof(ItemCondition)
                && field.Name == "display" && instruction.LoadsField(field);
        }

        [HarmonyPatch(typeof(WaterStripUIManager), "Open", new Type[] { typeof(GameItem) })]
        private static class StripWindow
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> code = new List<CodeInstruction>(instructions);
                int[] counts = new int[Keys.Length]; int conditionReads = 0, dynamicReads = 0;
                MethodInfo dynamicName = AccessTools.Method(typeof(WaterFeatureHelper), "GetWaterDisplayName", new Type[] { typeof(int) });
                foreach (CodeInstruction instruction in code)
                {
                    int index = StaticIndex(instruction);
                    if (index >= 0) counts[index]++;
                    if (ConditionRead(instruction)) conditionReads++;
                    if (instruction.Calls(dynamicName)) dynamicReads++;
                }
                bool supported = conditionReads == 1 && dynamicReads == 7;
                foreach (int count in counts) supported = supported && count == 1;
                if (!supported)
                {
                    Debug.LogWarning("[jinsong_poc] Unknown water-strip UI layout; preserving original method.");
                    foreach (CodeInstruction instruction in code) yield return instruction;
                    yield break;
                }
                MethodInfo name = AccessTools.Method(typeof(WaterPresentation), "UiName");
                MethodInfo condition = AccessTools.Method(typeof(WaterPresentation), "ConditionName");
                foreach (CodeInstruction instruction in code)
                {
                    int index = StaticIndex(instruction);
                    if (ConditionRead(instruction))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(instruction);
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = condition;
                    }
                    yield return instruction;
                    if (index >= 0)
                    {
                        yield return new CodeInstruction(OpCodes.Ldstr, Keys[index]);
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Call, name);
                    }
                }
            }
        }
    }
}
