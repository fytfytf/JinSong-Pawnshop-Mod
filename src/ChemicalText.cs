using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace JinSongPoc
{
    // Scoped chemical presentation on the pinned shipped assembly; exact method targets.
    // Every mechanic lookup uses a private CSV namespace, including shared keys.
    // Recipe names, numbers, callbacks, roles, tags and saved text are untouched.
    internal static class ChemicalText
    {
        private static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions,
            int count, int defaults, string scope)
        {
            List<CodeInstruction> body = new List<CodeInstruction>(instructions);
            MethodInfo mechanic = AccessTools.Method(typeof(LocHelper), "GetLocalizedMechanic",
                new Type[] { typeof(string), typeof(object[]) });
            MethodInfo fallback = AccessTools.Method(typeof(LocHelper), "GetLocalizedMechanicOrDefault",
                new Type[] { typeof(string), typeof(string), typeof(object[]) });
            if (mechanic == null || fallback == null)
            {
                UnityEngine.Debug.LogWarning("[JinSong] Chemical text signatures unavailable; original retained: " + scope);
                return body;
            }
            int actual = 0, actualDefaults = 0;
            foreach (CodeInstruction instruction in body)
            {
                if (instruction.Calls(mechanic)) actual++;
                if (instruction.Calls(fallback)) actualDefaults++;
            }
            if (actual != count || actualDefaults != defaults)
            {
                UnityEngine.Debug.LogWarning("[JinSong] Chemical text call shape changed; original retained: " + scope
                    + " actual=" + actual + "/" + actualDefaults + " expected=" + count + "/" + defaults);
                return body;
            }
            MethodInfo localize = AccessTools.Method(typeof(ChemicalText), "Localize");
            MethodInfo localizeDefault = AccessTools.Method(typeof(ChemicalText), "LocalizeOrDefault");
            foreach (CodeInstruction instruction in body)
            {
                if (instruction.Calls(mechanic)) instruction.operand = localize;
                else if (instruction.Calls(fallback)) instruction.operand = localizeDefault;
            }
            // Operand replacement preserves original stack, labels and exception blocks.
            return body;
        }

        private static string Localize(string key, object[] args)
        {
            return (Mod.Chinese ? Mod.Text("chemical_" + key, args) : null)
                ?? LocHelper.GetLocalizedMechanic(key, args);
        }

        private static string LocalizeOrDefault(string key, string fallback, object[] args)
        {
            return (Mod.Chinese ? Mod.Text("chemical_" + key, args) : null)
                ?? LocHelper.GetLocalizedMechanicOrDefault(key, fallback, args);
        }

        private static IEnumerable<MethodBase> ExactNested(string typeName, string methodName,
            Type[] parameters)
        {
            MethodBase method = null;
            try { method = ModReflect.Target(typeName, methodName, parameters); }
            catch (ModReflectAccessException) { }
            if (method == null)
                UnityEngine.Debug.LogWarning("[JinSong] Chemical text closure changed; original retained: " + typeName + "." + methodName);
            else
                yield return method;
        }

        [HarmonyPatch(typeof(MachineHeatingPlate), "CreateMachineInventoryWindow",
            new Type[] { typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(int) })]
        private static class HeatingPanel
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            { return Redirect(instructions, 8, 0, "HeatingPanel"); }
        }

        // +/- controls and target label are created in the factory, not the panel helper.
        [HarmonyPatch(typeof(MachineHeatingPlate), "HeatingPlate", new Type[] { })]
        private static class HeatingControls
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            { return Redirect(instructions, 3, 0, "HeatingControls"); }
        }

        [HarmonyPatch(typeof(MachineHeatingPlate), "CreateTooltip", new Type[] { typeof(RichTextBuilder), typeof(GameItem) })]
        private static class HeatingTooltip
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            { return Redirect(instructions, 4, 0, "HeatingTooltip"); }
        }

        [HarmonyPatch]
        private static class HeatingLiveStatus
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return ExactNested("MachineHeatingPlate+<>c__DisplayClass5_0", "<HeatingPlate>b__4",
                    new Type[] { typeof(GameItem), typeof(GameInventory), typeof(SlotMarker) });
            }
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            { return Redirect(instructions, 1, 0, "HeatingLiveStatus"); }
        }

        [HarmonyPatch(typeof(MachineBlender), "CreateMachineInventoryWindow",
            new Type[] { typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(int) })]
        private static class BlenderPanel
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            { return Redirect(instructions, 5, 0, "BlenderPanel"); }
        }

        [HarmonyPatch(typeof(MachineBlender), "CreateTooltip", new Type[] { typeof(RichTextBuilder), typeof(GameItem) })]
        private static class BlenderTooltip
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            { return Redirect(instructions, 2, 0, "BlenderTooltip"); }
        }

        [HarmonyPatch(typeof(MachineEvaporator), "CreateMachineInventoryWindow",
            new Type[] { typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(int) })]
        private static class EvaporatorPanel
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            { return Redirect(instructions, 3, 0, "EvaporatorPanel"); }
        }

        [HarmonyPatch(typeof(MachineChemFinisher), "CreateMachineInventoryWindow",
            new Type[] { typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(int) })]
        private static class FinisherPanel
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            { return Redirect(instructions, 8, 0, "FinisherPanel"); }
        }

        [HarmonyPatch(typeof(MachineChemFinisher), "CreateTooltip", new Type[] { typeof(RichTextBuilder), typeof(GameItem) })]
        private static class FinisherTooltip
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            { return Redirect(instructions, 2, 0, "FinisherTooltip"); }
        }

        [HarmonyPatch]
        private static class FinisherLiveStatus
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return ExactNested("MachineChemFinisher+<>c__DisplayClass1_0", "<ChemFinisher>b__3",
                    new Type[] { typeof(GameItem), typeof(GameInventory), typeof(SlotMarker) });
            }
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            { return Redirect(instructions, 9, 0, "FinisherLiveStatus"); }
        }

        [HarmonyPatch(typeof(ChemFinisherUIManager), "OpenUI", new Type[] { typeof(GameItem) })]
        private static class RecipeWindowDynamicText
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            { return Redirect(instructions, 2, 0, "RecipeWindowDynamicText"); }
        }

        // Optional explicit integration point for ShopGoods.Text; intentionally not
        // wired here. Existing display-name custom-name guard must still run first.
        internal static string NoteText(GameItem item, string suffix)
        {
            if (!Mod.Active || item == null || item.useSpriteFromMod || item.identifier != "postit"
                || item.spriteAtlasPath != "Items/items_tool" || item.spritePath != "postit"
                || !item.IsTag("IMPORTANT_TAG") || !EvaporatorNotes.Contains(item.name)) return null;
            if (suffix != "_name" && suffix != "_flavor") return null;
            return Mod.CurrentText("chemical_evaporator_note" + suffix);
        }

        private static readonly HashSet<string> EvaporatorNotes = new HashSet<string>(StringComparer.Ordinal)
        {
            "蒸发器说明", "Evaporator Instruction", "Instructions - Évaporateur", "Anleitung: Verdampfer",
            "エバポレーターの説明書", "Instrukcja użycia parownika", "Инструкция к испарителю", "Cómo usar el vaporizador",
            "item_evaporator_instruction_name"
        };
    }
}
