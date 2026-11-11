using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace JinSongPoc
{
    // Read-only UI/tooltip sites. The original cycle still writes its own card name/progress.
    internal static class AccessCraftingText
    {
        private static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions, int expected)
        {
            var body = new List<CodeInstruction>(instructions);
            MethodInfo original = AccessTools.Method(typeof(LocHelper), "GetLocalizedMechanic",
                new Type[] { typeof(string), typeof(object[]) });
            int count = 0;
            foreach (CodeInstruction instruction in body) if (instruction.Calls(original)) count++;
            if (count != expected)
            {
                UnityEngine.Debug.LogWarning("[JinSong] Access crafting text call shape changed; original retained.");
                return body;
            }
            MethodInfo display = AccessTools.Method(typeof(AccessCraftingText), "Localize");
            foreach (CodeInstruction instruction in body) if (instruction.Calls(original)) instruction.operand = display;
            return body;
        }

        private static string Localize(string key, object[] args)
        { return (Mod.Chinese ? Mod.Text("access_" + key, args) : null) ?? LocHelper.GetLocalizedMechanic(key, args); }

        [HarmonyPatch(typeof(MachineDesequencer), "CreateMachineInventoryWindow",
            new Type[] { typeof(bool), typeof(bool), typeof(int) })]
        private static class TallyPanel
        { private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return Redirect(instructions, 4); } }

        [HarmonyPatch(typeof(MachineDesequencer), "CreateMachineDesequencerTooltip",
            new Type[] { typeof(RichTextBuilder), typeof(GameItem) })]
        private static class TallyTooltip
        { private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return Redirect(instructions, 3); } }

        [HarmonyPatch(typeof(MachineBoxDispenser), "CreateMachineInventoryWindow",
            new Type[] { typeof(bool), typeof(bool), typeof(int) })]
        private static class BoxPanel
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var body = new List<CodeInstruction>(instructions);
                int dispense = 0, output = 0, control = 0;
                foreach (CodeInstruction instruction in body)
                    {
                        string value = instruction.operand as string;
                        if (value == "Dispense") dispense++;
                        else if (value == "Output") output++;
                        else if (value == "Control") control++;
                    }
                if (dispense != 1 || output != 1 || control != 1)
                {
                    UnityEngine.Debug.LogWarning("[JinSong] Box crafting literal shape changed; original retained.");
                    return body;
                }
                foreach (CodeInstruction instruction in body)
                {
                    string value = instruction.operand as string;
                    string getter = value == "Dispense" ? "Dispense" : value == "Output" ? "Output" : value == "Control" ? "Control" : null;
                    if (getter == null) continue;
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(AccessCraftingText), getter);
                }
                return body;
            }
        }

        private static string Dispense() { return Mod.Chinese ? Mod.Text("access_box_dispense") : "Dispense"; }
        private static string Output() { return Mod.Chinese ? Mod.Text("access_box_output") : "Output"; }
        private static string Control() { return Mod.Chinese ? Mod.Text("access_box_control") : "Control"; }

        private static readonly HashSet<string> Notes = new HashSet<string>(StringComparer.Ordinal)
        {
            "使用说明：解序器", "How To: The Desequencer", "Instructions - Déséquenceur", "Anleitung: Der Desequenzierer",
            "使い方：デシーケンサー", "Instrukcja: desekwencer", "Как работать с десеквенсором", "Cómo usar el desecuenciador",
            "item_desequencer_instruction_name"
        };

        internal static string NoteText(GameItem item, string suffix)
        {
            if (item.useSpriteFromMod || item.identifier != "postit" || item.spriteAtlasPath != "Items/items_tool"
                || item.spritePath != "postit" || !item.IsTag("IMPORTANT_TAG") || !item.IsGameItemType("DOCUMENT")
                || !Notes.Contains(item.name)) return null;
            return Mod.CurrentText("access_note" + suffix);
        }
    }
}
