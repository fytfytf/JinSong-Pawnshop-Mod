using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace JinSongPoc
{
    internal static class CraftingText
    {
        // Exact shipped methods only. A changed call shape leaves that method original.
        private static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions, int count, int defaults, string helper = "Localize")
        {
            List<CodeInstruction> body = new List<CodeInstruction>(instructions);
            MethodInfo original = AccessTools.Method(typeof(LocHelper), "GetLocalizedMechanic");
            MethodInfo fallback = AccessTools.Method(typeof(LocHelper), "GetLocalizedMechanicOrDefault");
            int actual = 0, actualDefaults = 0;
            foreach (CodeInstruction instruction in body)
            {
                if (instruction.Calls(original)) actual++;
                if (instruction.Calls(fallback)) actualDefaults++;
            }
            if (actual != count || actualDefaults != defaults)
            {
                UnityEngine.Debug.LogWarning("[JinSong] Crafting text call shape changed; original method retained.");
                return body;
            }
            foreach (CodeInstruction instruction in body)
            {
                if (instruction.Calls(original)) instruction.operand = AccessTools.Method(typeof(CraftingText), helper);
                else if (instruction.Calls(fallback)) instruction.operand = AccessTools.Method(typeof(CraftingText), "LocalizeOrDefault");
            }
            return body;
        }

        private static string Localize(string key, object[] args)
        { return (Mod.Chinese ? Mod.Text(key, args) : null) ?? LocHelper.GetLocalizedMechanic(key, args); }

        private static string LocalizeOrDefault(string key, string fallback, object[] args)
        { return (Mod.Chinese ? Mod.Text(key, args) : null) ?? LocHelper.GetLocalizedMechanicOrDefault(key, fallback, args); }

        [HarmonyPatch(typeof(MachineFurnace), "CreateMachineInventoryWindow", new Type[] { typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(int) })]
        private static class FurnacePanel
        { private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return Redirect(instructions, 3, 0); } }

        [HarmonyPatch(typeof(MachinePrinter), "CreateMachineInventoryWindow", new Type[] { typeof(int), typeof(int), typeof(bool), typeof(bool), typeof(int) })]
        private static class PrinterPanel
        { private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return Redirect(instructions, 4, 0); } }

        [HarmonyPatch(typeof(MachineBottlePrinter), "CreateMachineInventoryWindow", new Type[] { typeof(bool), typeof(bool), typeof(int) })]
        private static class BottlePanel
        { private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return Redirect(instructions, 4, 1); } }

        [HarmonyPatch(typeof(MachineBottlePrinter), "CreateTooltip", new Type[] { typeof(RichTextBuilder), typeof(GameItem) })]
        private static class BottleTooltip
        { private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return Redirect(instructions, 5, 0); } }

        // Moisture-only aliases: shared water-condition keys remain globally native.
        private static string MoistureLocalize(string key, object[] args)
        {
            string alias = null;
            switch (key)
            {
                case "mech_moisture_farm_label_module": alias = "moisture_panel_module"; break;
                case "mech_moisture_farm_label_container": alias = "moisture_panel_container"; break;
                case "mech_moisture_farm_tooltip": alias = "moisture_tooltip_output"; break;
                case "mech_moisture_farm_tooltip_running": alias = "moisture_tooltip_running"; break;
                case "mech_condition_gutterflow": alias = "moisture_quality_gutterflow"; break;
                case "mech_condition_rustwater": alias = "moisture_quality_rustwater"; break;
                case "mech_condition_ghostwater": alias = "moisture_quality_ghostwater"; break;
                case "mech_condition_basewater": alias = "moisture_quality_basewater"; break;
                case "mech_condition_high_quality_water": alias = "moisture_quality_high_quality_water"; break;
                case "mech_condition_pure_water": alias = "moisture_quality_pure_water"; break;
            }
            return (Mod.Chinese && alias != null ? Mod.Text(alias, args) : null) ?? LocHelper.GetLocalizedMechanic(key, args);
        }

        [HarmonyPatch(typeof(MachineMoistureFarm), "CreateMachineInventoryWindow", new Type[] { typeof(bool), typeof(bool), typeof(int) })]
        private static class MoisturePanel
        { private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return Redirect(instructions, 2, 0, "MoistureLocalize"); } }

        [HarmonyPatch(typeof(MachineMoistureFarm), "CreateTooltip", new Type[] { typeof(RichTextBuilder), typeof(GameItem) })]
        private static class MoistureTooltip
        { private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return Redirect(instructions, 2, 0, "MoistureLocalize"); } }

        [HarmonyPatch(typeof(MachineMoistureFarm), "GetOutputQualityDisplay", new Type[] { typeof(GameItem) })]
        private static class MoistureQuality
        { private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return Redirect(instructions, 6, 0, "MoistureLocalize"); } }

        // Four original panel reads only. Native growing/status/overlay paths stay original.
        private static string HydroponicLocalize(string key, object[] args)
        {
            string alias = null;
            switch (key)
            {
                case "mech_hydroponic_label_module": alias = "hydroponic_panel_module"; break;
                case "mech_hydroponic_label_water": alias = "hydroponic_panel_water"; break;
                case "mech_hydroponic_label_seed": alias = "hydroponic_panel_seed"; break;
                case "mech_hydroponic_label_output": alias = "hydroponic_panel_output"; break;
            }
            return (Mod.Chinese && alias != null ? Mod.Text(alias, args) : null) ?? LocHelper.GetLocalizedMechanic(key, args);
        }

        [HarmonyPatch(typeof(MachineHydroponic), "CreateMachineInventoryWindow", new Type[] { typeof(bool), typeof(bool) })]
        private static class HydroponicPanel
        { private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) { return Redirect(instructions, 4, 0, "HydroponicLocalize"); } }

        private static readonly Dictionary<string, string> RequirementRecipes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "printer_chip_box_cutter", "printer_chip6" }, { "printer_chip_kitchen_knife", "printer_chip6" },
            { "printer_chip_hatchet", "printer_chip6" }, { "printer_chip_kitchen_cleaver", "printer_chip6" },
            { "printer_chip_22_mag", "printer_chip3" }, { "printer_chip_22_mag_ext", "printer_chip3" },
            { "printer_chip_22_mag_high", "printer_chip3" }, { "printer_chip_pistol_double", "printer_chip_pistol" }
        };

        private static string RequirementName(string identifier, GameItem source)
        {
            // Preserve the original native cache lookup and its first-read behavior.
            // Only the returned text at this one requirements UI site is themed.
            string original = GeneralHelper.GetDisplayNameFromID(identifier);
            string expectedSprite;
            if (Mod.Chinese && source != null && !source.useSpriteFromMod
                && !String.IsNullOrEmpty(source.identifier)
                && source.spriteAtlasPath == "Items/items_tool"
                && source.IsTag("PRINTER_CHIP_TAG")
                && RequirementRecipes.TryGetValue(source.identifier, out expectedSprite) && source.spritePath == expectedSprite
                && (expectedSprite != "printer_chip6" || source.IsTag("PRINTER_MELEE_CHIP_TAG"))
                && (identifier == "scrap_metal" || identifier == "printer_plastic"))
                return Mod.Text("goods_" + identifier + "_name") ?? original;
            return original;
        }

        [HarmonyPatch(typeof(MachineProgressHelper), "AddAdditionalItemRequirementsTooltip",
            new Type[] { typeof(RichTextBuilder), typeof(GameItem) })]
        private static class PrinterRequirements
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var body = new List<CodeInstruction>(instructions);
                MethodInfo original = AccessTools.Method(typeof(GeneralHelper), "GetDisplayNameFromID", new Type[] { typeof(string) });
                int count = 0;
                foreach (CodeInstruction instruction in body) if (instruction.Calls(original)) count++;
                if (count != 1)
                {
                    UnityEngine.Debug.LogWarning("[JinSong] Printer requirement name call shape changed; original retained.");
                    return body;
                }
                MethodInfo display = AccessTools.Method(typeof(CraftingText), "RequirementName");
                var changed = new List<CodeInstruction>();
                foreach (CodeInstruction instruction in body)
                {
                    if (instruction.Calls(original))
                    {
                        changed.Add(new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(instruction));
                        instruction.operand = display;
                    }
                    changed.Add(instruction);
                }
                return changed;
            }
        }

        internal static string NoteText(GameItem item, string suffix)
        {
            if (item.useSpriteFromMod || item.identifier != "postit" || item.spriteAtlasPath != "Items/items_tool"
                || item.spritePath != "postit" || !item.IsTag("IMPORTANT_TAG")) return null;
            string prefix = FurnaceNotes.Contains(item.name) ? "furnace_note" : PrinterNotes.Contains(item.name) ? "printer_note" : MoistureNotes.Contains(item.name) ? "moisture_note" : HydroponicNotes.Contains(item.name) ? "hydroponic_note" : null;
            return prefix == null ? null : Mod.CurrentText(prefix + suffix);
        }

        private static readonly HashSet<string> FurnaceNotes = new HashSet<string>(StringComparer.Ordinal)
        {
            "使用说明：熔炉", "How To: The Furnace", "Instructions - Four", "Anleitung: Der Schmelzofen",
            "使い方：溶鉱炉", "Instrukcja: piec", "Как работать с горном", "Cómo se usa la fragua", "item_furnace_instruction_name"
        };
        private static readonly HashSet<string> PrinterNotes = new HashSet<string>(StringComparer.Ordinal)
        {
            "3D打印机说明", "3D Printer Instruction", "Instructions - Imprimante 3D", "Anleitung: 3D-Drucker",
            "3Dプリンターの説明書", "Instrukcja użycia drukarki 3D", "Инструкция к 3D-принтеру", "Manual de la impresora 3D", "item_3d_printer_instruction_name"
        };
        private static readonly HashSet<string> HydroponicNotes = new HashSet<string>(StringComparer.Ordinal)
        {
            "水培系统说明",
            "Hydroponic Instruction",
            "Instructions - Hydroponie",
            "Anleitung: Hydrokultursystem",
            "水耕システムの説明書",
            "Instrukcja użycia hydroponiki",
            "Инструкция к гидропонной системе",
            "Guía del sistema de hidroponía",
            "item_hydroponic_instruction_name"
        };
        private static readonly HashSet<string> MoistureNotes = new HashSet<string>(StringComparer.Ordinal)
        {
            "使用说明：水分收集器",
            "How To: The Moisture Farm",
            "Instructions - Récupérateur hydrique",
            "Anleitung: Die Feuchtigkeitsfarm",
            "使い方：モイスチャーファーム",
            "Instrukcja: farma wilgoci",
            "Как работать с конденсатором влаги",
            "Cómo se usa el extractor de humedad",
            "item_moisture_farm_instruction_name"
        };
    }
}
