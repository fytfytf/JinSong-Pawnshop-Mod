using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace JinSongPoc
{
    internal static class ShopGoods
    {
        private sealed class Goods
        {
            internal readonly string Atlas, Identifier, NativeSprite, ArtKey, TextKey;

            internal Goods(string atlas, string identifier, string nativeSprite)
                : this(atlas, identifier, nativeSprite, nativeSprite, nativeSprite) { }

            internal Goods(string atlas, string identifier, string nativeSprite, string artBase, string textBase)
            {
                Atlas = atlas;
                Identifier = identifier;
                NativeSprite = nativeSprite;
                ArtKey = ArtPrefix + artBase;
                TextKey = "goods_" + textBase;
            }
        }

        private const string WaterAtlas = "Items/water_container3";
        private const string ArtPrefix = "jin_goods_";
        private static readonly Goods[] Rules = {
            new Goods("Items/items_food", "processed_meat", "processed_meat"),
            new Goods("Items/items_food", "li_eat_snackbar", "li_eat_snackbar"),
            new Goods("Items/items_hydroponic_plant", "nutrifruit", "nutrifruit"),
            new Goods("Items/items_hydroponic_plant", "nutrifruit", "nutrifruit_s"),
            new Goods("Items/items_hydroponic_plant", "nutrifruit", "nutrifruit_l"),
            new Goods("Items/items_hydroponic_plant", "nutrifruit", "nutrifruit_xl"),
            new Goods("Items/items_food", "raw_meat", "raw_meat"),
            new Goods("Items/items_food", "small_raw_meat", "small_raw_meat"),
            new Goods("Items/items_food", "red_beer", "red_beer"),
            new Goods("Items/items_food", "empty_beer_bottle", "empty_beer_bottle"),
            new Goods("Items/items_food", "bottled_water_premium", "water_bottle_full_premium"),
            new Goods("Items/water_container3", "small_bottled_water", "small"),
            new Goods("Items/water_container3", "bottled_water", "normal"),
            new Goods("Items/water_container3", "large_bottled_water", "large"),
            new Goods("Items/items_medical", "bandage_item", "bandage_small"),
            new Goods("Items/items_medical", "hemostatic_bandage_item", "hemostatic_bandage_small"),
            new Goods("Items/items_medical", "topical_bandage_item", "topical_bandage_small"),
            new Goods("Items/items_medical", "caffeine_pill", "cafeine_pill"),
            new Goods("Items/items_tool", "screwdriver", "screwdriver_s"),
            new Goods("Items/items_tool", "wire_cutter", "wirecutter"),
            new Goods("Items/items_tool", "magnifier", "magnifier"),
            new Goods("Items/items_weapons_melee", "box_cutter", "box_cutter"),
            new Goods("Items/items_food", "processed_cheese", "cheese"),
            new Goods("Items/items_food", "cat_bar", "cat_bar"),
            new Goods("Items/items_food", "water_ration", "water_ration"),
            new Goods("Items/items_food", "galaxy_blend", "juice_pouch"),
            new Goods("Items/items_food", "nudka", "nudka"),
            new Goods("Items/items_medical", "phagimycin_pill", "pill1"),
            new Goods("Items/items_medical", "unlicensed_phagimycin_pill", "pill2"),
            new Goods("Items/items_tool", "welder", "welder_s"),
            new Goods("Items/items_tool2", "toolbox", "toolbox_old"),
            new Goods("Items/items_tool", "labeler", "labeler"),
            new Goods("Items/items_tool", "labeler_blue", "labeler_blue"),
            new Goods("Items/items_food", "water_ration_small", "water_ration_small"),
            new Goods("Items/items_tool2", "toolbox", "toolbox_new"),
            new Goods("Items/items_amenities", "paper_towel", "paper_towel"),
            new Goods("Items/items_amenities", "toilet_paper", "toilet_paper"),
            new Goods("Items/items_amenities", "shampoo", "shampoo"),
            new Goods("Items/items_amenities", "toothpaste", "toothpaste"),
            new Goods("Items/items_amenities", "water_test_container", "water_test_container"),
            new Goods("Items/items_amenities", "water_test_strip", "strip_new"),
            new Goods("Items/items_amenities", "water_test_strip", "strip_used"),
            new Goods("Items/items_amenities", "water_test_strip", "strip0"),
            new Goods("Items/items_amenities", "water_test_strip", "strip1"),
            new Goods("Items/items_amenities", "water_test_strip", "strip2"),
            new Goods("Items/items_amenities", "water_test_strip", "strip3"),
            new Goods("Items/items_amenities", "water_test_strip", "strip4"),
            new Goods("Items/items_amenities", "water_test_strip", "strip5"),
            new Goods("Items/items_amenities", "water_tablet_container", "water_tablet_container"),
            new Goods("Items/items_amenities", "water_tablet", "water_tablet"),
            new Goods("Items/items_amenities", "water_filter", "water_filter"),
            new Goods("Items/items_amenities", "water_filter_adv", "water_filter_adv"),
            new Goods("Items/items_hydroponic_plant", "hydroreed_filter", "hydroreed_filter"),
            new Goods("Items/items_amenities", "uv_filter", "uv"),
            new Goods("Items/water_container4", "water_pitcher", "pitcher"),
            new Goods("Items/items_material", "scrap_metal", "scrap_metal"),
            new Goods("Items/items_material", "junk", "scrap_metal_2"),
            new Goods("Items/items_material", "metal_ingot", "metal_ingot"),
            new Goods("Items/items_construction", "wire", "wire"),
            new Goods("Items/items_material", "common_ore", "ore1"),
            new Goods("Items/items_material", "rare_ore", "ore2"),
            new Goods("Items/items_material", "common_electronic", "electronic1"),
            new Goods("Items/items_material", "common_chemical", "chemical1"),
            new Goods("Items/items_tool2", "water_purifier", "purifier"),
            new Goods("Items/items_tool2", "water_purifier", "purifier_w"),
            new Goods("Items/items_medical", "med_bottle_small_pink", "med_bottle_small_pink"),
            new Goods("Items/items_food", "processed_milk", "items_food_pilk_alt"),
            new Goods("Items/items_food", "processed_juice", "items_food_juice"),
            new Goods("Items/items_food", "bottle_hot_sauce", "hot_sauce"),
            new Goods("Items/water_container3", "mini_bottle", "mini"),
            new Goods("Items/water_container4", "water_jug", "xl"),
            new Goods("Items/items_food", "soda_red", "items_food_soda_red"),
            new Goods("Items/items_food", "energy_drink", "energy_drink"),
            new Goods("Items/items_food", "morsel", "morsel"),
            new Goods("Items/items_food", "small_morsel", "morsel_small"),
            new Goods("Items/items_food", "fat_meat", "fat_meat"),
            new Goods("Items/items_food", "meat_scrap", "meat_scrap"),
            new Goods("Items/items_food", "cup_noodle", "cup_noodle"),
            new Goods("Items/items_food", "beis_icecream", "beis_icecream"),
            new Goods("Items/items_tool", "powerblock", "powerblock"),
            new Goods("Items/items_tool", "recharger", "recharger_base"),
            new Goods("Items/items_tool", "powerblock", "powerblock_on"),
            new Goods("Items/items_tool", "energy_credit_ext", "battery_ext80"),
            new Goods("Items/items_tool", "energy_credit_ext", "battery_ext60"),
            new Goods("Items/items_tool", "energy_credit_ext", "battery_ext40"),
            new Goods("Items/items_tool", "energy_credit_ext", "battery_ext20"),
            new Goods("Items/items_tool", "energy_credit_ext", "battery_ext10"),
            new Goods("Items/items_tool", "energy_credit_ext", "battery_ext0"),
            new Goods("Items/items_tool", "energy_credit", "battery100"),
            new Goods("Items/items_tool", "energy_credit_ext", "battery_ext100"),
            new Goods("Items/items_tool", "energy_credit", "battery80"),
            new Goods("Items/items_tool", "energy_credit", "battery60"),
            new Goods("Items/items_tool", "energy_credit", "battery40"),
            new Goods("Items/items_tool", "energy_credit", "battery20"),
            new Goods("Items/items_tool", "energy_credit", "battery10"),
            new Goods("Items/items_tool", "energy_credit", "battery0"),
            new Goods("Items/items_tool2", "energy_credit_breeder", "sbattery100"),
            new Goods("Items/items_tool2", "energy_credit_breeder", "sbattery80"),
            new Goods("Items/items_tool2", "energy_credit_breeder", "sbattery60"),
            new Goods("Items/items_tool2", "energy_credit_breeder", "sbattery40"),
            new Goods("Items/items_tool2", "energy_credit_breeder", "sbattery20"),
            new Goods("Items/items_tool2", "energy_credit_breeder", "sbattery10"),
            new Goods("Items/items_tool2", "energy_credit_breeder", "sbattery0"),
            new Goods("Items/items_tool", "energy_credit_dis", "battery_dis100"),
            new Goods("Items/items_tool2", "energy_credit_hyp", "battery_hyp"),
            new Goods("Items/items_tool2", "current_converter", "current_converter"),
            new Goods("Items/items_tool2", "current_converter", "current_converter_off"),
            new Goods("Items/items_tool2", "furnace", "furnace"),
            new Goods("Items/items_tool2", "printer", "printer_off"),
            new Goods("Items/items_tool", "bottle_printer", "bottle_printer"),
            new Goods("Items/items_material", "printer_plastic", "printer_plastic"),
            new Goods("Items/items_material", "flux_agent", "flux_agent"),
            new Goods("Items/items_tool2", "printer", "printer_on"),
            new Goods("Items/items_tool2", "heating_plate", "heating_plate"),
            new Goods("Items/items_tool2", "blender", "blender"),
            new Goods("Items/items_tool2", "evaporator", "evaporator"),
            new Goods("Items/items_tool2", "chem_finisher", "chem_finisher"),
            new Goods("Items/items_tool2", "heating_plate", "heating_plate_b"),
            new Goods("Items/items_tool2", "blender", "blender_b"),
            new Goods("Items/items_tool2", "evaporator", "evaporator_b"),
            new Goods("Items/items_tool2", "chem_finisher", "chem_finisher_b"),
            new Goods("Items/items_tool", "desequencer", "desequencer"),
            new Goods("Items/items_tool", "desequencer", "desequencer_r"),
            new Goods("Items/items_tool", "box_dispenser", "bottle_printer", "box_dispenser", "box_dispenser"),
            new Goods("Items/items_misc", "blank_keycard", "blank_card"),
            new Goods("Items/items_misc", "blank_keycard", "blank_card_wip"),
            new Goods("Items/items_misc", "sec_keycard", "sec_keycard"),
            new Goods("Items/items_tool", "crypto_module_sec", "sec_module"),
            new Goods("Items/items_module", "system_module_ruined", "module_ruined"),
            new Goods("Items/items_tool", "printer_chip_box_cutter", "printer_chip6", "printer_chip_box_cutter", "printer_chip_box_cutter"),
            new Goods("Items/items_tool", "printer_chip_kitchen_knife", "printer_chip6", "printer_chip_kitchen_knife", "printer_chip_kitchen_knife"),
            new Goods("Items/items_tool", "printer_chip_hatchet", "printer_chip6", "printer_chip_hatchet", "printer_chip_hatchet"),
            new Goods("Items/items_tool", "printer_chip_kitchen_cleaver", "printer_chip6", "printer_chip_kitchen_cleaver", "printer_chip_kitchen_cleaver"),
            new Goods("Items/items_weapons_melee", "kitchen_knife", "kitchen_knife"),
            new Goods("Items/items_weapons_melee", "hatchet", "hatchet"),
            new Goods("Items/items_weapons_melee", "kitchen_cleaver", "kitchen_cleaver"),
            new Goods("Items/items_weapons_ranged_energy", "printed_gun", "printed_gun"),
            new Goods("Items/items_tool", "printer_chip_22_mag", "printer_chip3", "printer_chip_22_mag", "printer_chip_22_mag"),
            new Goods("Items/items_tool", "printer_chip_22_mag_ext", "printer_chip3", "printer_chip_22_mag_ext", "printer_chip_22_mag_ext"),
            new Goods("Items/items_tool", "printer_chip_22_mag_high", "printer_chip3", "printer_chip_22_mag_high", "printer_chip_22_mag_high"),
            new Goods("Items/items_tool", "printer_chip_pistol_double", "printer_chip_pistol", "printer_chip_pistol_double", "printer_chip_pistol_double"),
            new Goods("Items/items_tool", "moisture_farm", "moisture_farm", "moisture_farm", "moisture_farm"),
            new Goods("Items/items_tool", "moisture_farm", "moisture_farm_w", "moisture_farm_w", "moisture_farm"),
            new Goods("Items/items_hydroponic_plant", "hydroponic", "hydroponic", "hydroponic", "hydroponic"),
            new Goods("Items/items_hydroponic_plant", "hydroponic", "hydroponic_off", "hydroponic_off", "hydroponic"),
            new Goods("Items/items_hydroponic_plant", "nutrifruit_seed", "nutrifruit_seed", "nutrifruit_seed", "nutrifruit_seed"),
            new Goods("Items/items_hydroponic_plant", "kotton_seed", "kotton_plant_seed", "kotton_seed", "kotton_seed"),
            new Goods("Items/items_hydroponic_plant", "kotton_fiber", "kotton_plant", "kotton_fiber", "kotton_fiber"),
            new Goods("Items/items_hydroponic_plant", "dream_cap_seed", "dream_cap_seed", "dream_cap_seed", "dream_cap_seed"),
            new Goods("Items/items_hydroponic_plant", "dream_cap", "dream_cap", "dream_cap", "dream_cap"),
            new Goods("Items/items_hydroponic_plant", "meal_cap", "meal_cap", "meal_cap", "meal_cap"),
            new Goods("Items/items_hydroponic_plant", "nightmare_cap", "nightmare_cap", "nightmare_cap", "nightmare_cap"),
            new Goods("Items/items_hydroponic_plant", "coffee_bean", "coffee_bean", "coffee_bean", "coffee_bean"),
            new Goods("Items/items_hydroponic_plant", "fillerweed_seed", "fillerweed_seed", "fillerweed_seed", "fillerweed_seed"),
            new Goods("Items/items_hydroponic_plant", "fillerweed", "fillerweed", "fillerweed", "fillerweed"),
            new Goods("Items/items_hydroponic_plant", "hydroreed_seed", "hydroreed_seed", "hydroreed_seed", "hydroreed_seed"),
            new Goods("Items/items_hydroponic_plant", "bloomberry_seed", "bloomberry_seed", "bloomberry_seed", "bloomberry_seed"),
            new Goods("Items/items_module", "farm_module_nutrifruit", "farm_module_l2", "farm_module_nutrifruit", "farm_module_nutrifruit"),
            new Goods("Items/items_module", "farm_module_dream_cap", "farm_module_l4", "farm_module_dream_cap", "farm_module_dream_cap"),
            new Goods("Items/items_module", "farm_module_kotton_loom", "farm_module_l1", "farm_module_kotton_loom", "farm_module_kotton_loom"),
            new Goods("Items/items_module", "farm_module_nightmare_cap", "farm_module5", "farm_module_nightmare_cap", "farm_module_nightmare_cap"),
            new Goods("Items/items_module", "farm_module_fillerweed_production", "farm_module_l5", "farm_module_fillerweed_production", "farm_module_fillerweed_production"),
            new Goods("Items/items_module", "farm_module_hydroreed_filter", "farm_module_l6", "farm_module_hydroreed_filter", "farm_module_hydroreed_filter"),
            new Goods("Items/items_module", "farm_module_smuggler", "farm_module6", "farm_module_smuggler", "farm_module_smuggler"),
            new Goods("Items/items_hydroponic_plant", "plant_analyser", "plant_analyser", "plant_analyser", "plant_analyser"),
            new Goods("Items/items_hydroponic_plant", "plant_dna_disk", "dna_disk_black_empty", "plant_dna_disk", "plant_dna_disk"),
            new Goods("Items/items_hydroponic_plant", "plant_dna_disk", "dna_disk_purple_full", "plant_dna_disk_purple_full", "plant_dna_disk"),
            new Goods("Items/items_hydroponic_plant", "plant_dna_disk", "dna_disk_blue_full", "plant_dna_disk_blue_full", "plant_dna_disk"),
            new Goods("Items/items_hydroponic_plant", "plant_dna_disk", "dna_disk_green_full", "plant_dna_disk_green_full", "plant_dna_disk"),
            new Goods("Items/items_hydroponic_plant", "plant_dna_disk", "dna_disk_red_full", "plant_dna_disk_red_full", "plant_dna_disk"),
            new Goods("Items/items_hydroponic_plant", "plant_dna_manipulator", "dna_manipulator", "plant_dna_manipulator", "plant_dna_manipulator"),
            new Goods("Items/items_hydroponic_plant", "plant_dna_manipulator", "dna_manipulator_working", "plant_dna_manipulator_working", "plant_dna_manipulator"),
            new Goods("Items/items_hydroponic_plant", "nutrifruit_seed_packet", "nutrifruit_seed_packet", "nutrifruit_seed_packet", "nutrifruit_seed_packet"),
            new Goods("Items/items_module", "dna_manipulator_module_stabilizer", "long_module1", "dna_manipulator_module_stabilizer", "dna_manipulator_module_stabilizer"),
            new Goods("Items/items_module", "dna_manipulator_module_positive_filter", "long_module1", "dna_manipulator_module_positive_filter", "dna_manipulator_module_positive_filter"),
            new Goods("Items/items_module", "dna_manipulator_module_negative_filter", "long_module1", "dna_manipulator_module_negative_filter", "dna_manipulator_module_negative_filter"),
            new Goods("Items/items_module", "dna_manipulator_module_precision", "long_module1", "dna_manipulator_module_precision", "dna_manipulator_module_precision"),
            new Goods("Items/items_winemaking", "wine_yeast", "yeast", "wine_yeast", "wine_yeast"),
            new Goods("Items/items_winemaking", "wine_superyeast", "superyeast", "wine_superyeast", "wine_superyeast"),
            new Goods("Items/items_winemaking", "wine_yeast_infinite", "yeast_gen", "wine_yeast_infinite", "wine_yeast_infinite"),
            new Goods("Items/items_winemaking", "wine_bottle", "wine_bottle", "wine_bottle", "wine_bottle"),
            new Goods("Items/items_winemaking", "wine_bottle", "wine_fermenting", "wine_bottle_wine_fermenting", "wine_bottle"),
            new Goods("Items/items_winemaking", "wine_bottle", "bottle0", "wine_bottle_bottle0", "wine_bottle"),
            new Goods("Items/items_winemaking", "wine_bottle", "bottle1", "wine_bottle_bottle1", "wine_bottle"),
            new Goods("Items/items_winemaking", "wine_bottle", "bottle2", "wine_bottle_bottle2", "wine_bottle"),
            new Goods("Items/items_winemaking", "wine_bottle", "bottle3", "wine_bottle_bottle3", "wine_bottle"),
            new Goods("Items/items_winemaking", "wine_bottle", "bottle4", "wine_bottle_bottle4", "wine_bottle"),
            new Goods("Items/items_winemaking", "wine_bottle", "bottle5", "wine_bottle_bottle5", "wine_bottle"),
            new Goods("Items/items_winemaking", "wine_rack", "wine_rack", "wine_rack", "wine_rack"),
            new Goods("Items/items_winemaking", "wine_yeast_red", "illegal_yeast", "wine_yeast_red", "wine_yeast_red"),
            new Goods("Items/items_winemaking", "wine_berry", "wine_berry", "wine_berry", "wine_berry"),
            new Goods("Items/items_aquaculture", "caviar", "caviar", "caviar", "caviar"),
            new Goods("Items/items_aquaculture", "sturgeon_roe", "roe_sturgeon", "sturgeon_roe", "sturgeon_roe"),
            new Goods("Items/items_aquaculture", "fish_scrap", "fishscrap", "fish_scrap", "fish_scrap"),
            new Goods("Items/items_medical", "tilapia_skin_graft", "bandage_fish", "tilapia_skin_graft", "tilapia_skin_graft"),
            new Goods("Items/items_medical", "serum_red", "red_serum", "serum_red", "serum_red"),
            new Goods("Items/items_medical", "serum_green", "green_serum", "serum_green", "serum_green"),
            new Goods("Items/items_husbandry", "marking_pen", "marking_pen", "marking_pen", "marking_pen"),
            new Goods("Items/items_husbandry", "mouse_trap", "mouse_trap", "mouse_trap", "mouse_trap"),
            new Goods("Items/items_husbandry", "mouse_trap", "mouse_trap_cheese", "mouse_trap_cheese", "mouse_trap"),
            new Goods("Items/items_husbandry", "mouse_trap", "mouse_trap_m", "mouse_trap_m", "mouse_trap"),
            new Goods("Items/items_husbandry", "mouse_trap", "mouse_trap_f", "mouse_trap_f", "mouse_trap"),
            new Goods("Items/items_husbandry", "animal_cage", "animal_cage", "animal_cage", "animal_cage"),
            new Goods("Items/items_husbandry", "livestock_feed_dispenser", "animal_feeder", "livestock_feed_dispenser", "livestock_feed_dispenser"),
            new Goods("Items/items_tool2", "auto_butcher", "gibber", "auto_butcher", "auto_butcher"),
            new Goods("Items/items_husbandry", "feces_small", "feces_small", "feces_small", "feces_small"),
            new Goods("Items/items_husbandry", "feces_medium", "feces", "feces_medium", "feces_medium"),
            new Goods("Items/items_husbandry", "feces_large", "feces_large", "feces_large", "feces_large"),
            new Goods("Items/items_husbandry", "vacuum_robot", "vacuum_robot", "vacuum_robot", "vacuum_robot"),
            new Goods("Items/items_aquaculture", "gutterflow_pellet", "fish_food", "gutterflow_pellet", "gutterflow_pellet"),
            new Goods("Items/items_aquaculture", "industrial_fish_feed", "fish_food2", "industrial_fish_feed", "industrial_fish_feed"),
            new Goods("Items/items_aquaculture", "ras", "ras_empty", "ras", "ras"),
            new Goods("Items/items_aquaculture", "ras", "ras", "ras_water", "ras"),
            new Goods("Items/items_aquaculture", "ras", "ras_full", "ras_full", "ras"),
            new Goods("Items/items_aquaculture", "ras_water_tank", "ras_tank", "ras_water_tank", "ras_water_tank"),
            new Goods("Items/items_tool2", "water_tank", "water_tank", "water_tank", "water_tank"),
            new Goods("Items/items_aquaculture", "tilapia_roe", "roe", "tilapia_roe", "tilapia_roe"),
            new Goods("Items/items_aquaculture", "tilapia_roe", "roe_fertilized", "tilapia_roe_fertilized", "tilapia_roe_fertilized"),
            new Goods("Items/items_aquaculture", "tilapia_fry", "fries", "tilapia_fry", "tilapia_fry"),
            new Goods("Items/items_aquaculture", "tilapia", "tilapia_small1", "tilapia_small", "tilapia"),
            new Goods("Items/items_aquaculture", "tilapia", "tilapia1", "tilapia", "tilapia"),
            new Goods("Items/items_aquaculture", "tilapia", "tilapia_large1", "tilapia_large", "tilapia"),
            new Goods("Items/items_aquaculture", "gutter_tilapia_roe", "roe3", "gutter_tilapia_roe", "gutter_tilapia_roe"),
            new Goods("Items/items_aquaculture", "gutter_tilapia_roe", "roe_fertilized3", "gutter_tilapia_roe_fertilized", "gutter_tilapia_roe_fertilized"),
            new Goods("Items/items_aquaculture", "gutter_tilapia_fry", "fries", "gutter_tilapia_fry", "gutter_tilapia_fry"),
            new Goods("Items/items_aquaculture", "gutter_tilapia", "tilapia_small3", "gutter_tilapia_small", "gutter_tilapia"),
            new Goods("Items/items_aquaculture", "gutter_tilapia", "tilapia3", "gutter_tilapia", "gutter_tilapia"),
            new Goods("Items/items_aquaculture", "gutter_tilapia", "tilapia_large3", "gutter_tilapia_large", "gutter_tilapia"),
            new Goods("Items/items_aquaculture", "graft_tilapia_roe", "roe2", "graft_tilapia_roe", "graft_tilapia_roe"),
            new Goods("Items/items_aquaculture", "eel", "eel_small1", "eel_small", "eel"),
            new Goods("Items/items_aquaculture", "eel", "eel1", "eel", "eel"),
            new Goods("Items/items_aquaculture", "eel", "eel_large1", "eel_large", "eel"),
            new Goods("Items/items_aquaculture", "electric_eel", "eel_small2", "electric_eel_small", "electric_eel"),
            new Goods("Items/items_aquaculture", "electric_eel", "eel2", "electric_eel", "electric_eel"),
            new Goods("Items/items_aquaculture", "electric_eel", "eel_large2", "electric_eel_large", "electric_eel"),
            new Goods("Items/items_aquaculture", "sturgeon", "sturgeon", "sturgeon", "sturgeon"),
            new Goods("Items/items_aquaculture", "tiny_fish_fillet", "meat_fish_tiny4", "tiny_fish_fillet", "tiny_fish_fillet"),
            new Goods("Items/items_aquaculture", "small_fish_fillet", "meat_fish_small4", "small_fish_fillet", "small_fish_fillet"),
            new Goods("Items/items_aquaculture", "fish_fillet", "meat_fish4", "fish_fillet", "fish_fillet"),
            new Goods("Items/items_aquaculture", "large_fish_fillet", "meat_fish_large4", "large_fish_fillet", "large_fish_fillet"),
            new Goods("Items/items_aquaculture", "small_eel_fillet", "meat_eel_tiny", "small_eel_fillet", "small_eel_fillet"),
            new Goods("Items/items_aquaculture", "eel_fillet", "meat_eel_small", "eel_fillet", "eel_fillet"),
            new Goods("Items/items_armor", "kotton_fabric", "kotton_fabric", "kotton_fabric", "kotton_fabric"),
            new Goods("Items/items_husbandry", "rat", "rat_s_m", "rat_s_m", "rat_s_m"),
            new Goods("Items/items_husbandry", "rat", "rat_s_f", "rat_s_f", "rat_s_f"),
            new Goods("Items/items_husbandry", "rat", "rat_m_m", "rat_m_m", "rat_m_m"),
            new Goods("Items/items_husbandry", "rat", "rat_m_f", "rat_m_f", "rat_m_f"),
            new Goods("Items/items_husbandry", "rat", "rat_l_m", "rat_l_m", "rat_l_m"),
            new Goods("Items/items_husbandry", "rat", "rat_l_f", "rat_l_f", "rat_l_f"),
            new Goods("Items/items_husbandry", "rat", "rat_xl_m", "rat_xl_m", "rat_xl_m"),
            new Goods("Items/items_husbandry", "rat", "rat_xl_f", "rat_xl_f", "rat_xl_f"),
            new Goods("Items/items_husbandry", "rat", "rat_xxl_m", "rat_xxl_m", "rat_xxl_m"),
            new Goods("Items/items_husbandry", "rat", "rat_xxl_f", "rat_xxl_f", "rat_xxl_f"),
            new Goods("Items/items_husbandry", "rat", "rat_s_m_d", "rat_s_m_d", "rat_s_m_d"),
            new Goods("Items/items_husbandry", "rat", "rat_s_f_d", "rat_s_f_d", "rat_s_f_d"),
            new Goods("Items/items_husbandry", "rat", "rat_m_m_d", "rat_m_m_d", "rat_m_m_d"),
            new Goods("Items/items_husbandry", "rat", "rat_m_f_d", "rat_m_f_d", "rat_m_f_d"),
            new Goods("Items/items_husbandry", "rat", "rat_l_m_d", "rat_l_m_d", "rat_l_m_d"),
            new Goods("Items/items_husbandry", "rat", "rat_l_f_d", "rat_l_f_d", "rat_l_f_d"),
            new Goods("Items/items_husbandry", "rat", "rat_xl_m_d", "rat_xl_m_d", "rat_xl_m_d"),
            new Goods("Items/items_husbandry", "rat", "rat_xl_f_d", "rat_xl_f_d", "rat_xl_f_d"),
            new Goods("Items/items_husbandry", "rat", "rat_xxl_m_d", "rat_xxl_m_d", "rat_xxl_m_d"),
            new Goods("Items/items_husbandry", "rat", "rat_xxl_f_d", "rat_xxl_f_d", "rat_xxl_f_d"),
            new Goods("Items/items_module", "blank_module", "module_white", "blank_module", "blank_module"),
            new Goods("Items/items_module", "system_module_performance", "module_red", "system_module_performance", "system_module_performance"),
            new Goods("Items/items_module", "system_module_efficiancy", "module_green", "system_module_efficiancy", "system_module_efficiancy"),
            new Goods("Items/items_module", "system_module_quality", "module_yellow", "system_module_quality", "system_module_quality"),
            new Goods("Items/items_module", "system_module_overclock", "module_red_2", "system_module_overclock", "system_module_overclock"),
            new Goods("Items/items_module", "system_module_eco", "module_green_2", "system_module_eco", "system_module_eco"),
            new Goods("Items/items_module", "system_module_fineness", "module_yellow_2", "system_module_fineness", "system_module_fineness"),
            new Goods("Items/items_module", "system_module_streamlining", "module_green_3", "system_module_streamlining", "system_module_streamlining"),
            new Goods("Items/items_module", "system_module_corrupt", "module_corrupt", "system_module_corrupt", "system_module_corrupt"),
            new Goods("Items/items_module", "evaporator_module_solvent_recovery", "long_module1", "evaporator_module_solvent_recovery", "evaporator_module_solvent_recovery"),
            new Goods("Items/items_module", "printer_module_metal", "long_module1", "printer_module_metal", "printer_module_metal"),
            new Goods("Items/items_module", "printer_module_cnc", "long_module3", "printer_module_cnc", "printer_module_cnc"),
            new Goods("Items/items_module", "printer_module_melee_batch", "long_module2", "printer_module_melee_batch", "printer_module_melee_batch"),
            new Goods("Items/items_module", "furnace_module_blast", "farm_module_l8", "furnace_module_blast", "furnace_module_blast"),
            new Goods("Items/items_module", "furnace_module_junk", "long_module3", "furnace_module_junk", "furnace_module_junk"),
            new Goods("Items/items_module", "alarm_module_transmitter", "farm_module_l7", "alarm_module_transmitter", "alarm_module_transmitter"),
            new Goods("Items/items_module", "chem_module", "chem_module", "chem_module", "chem_module"),
            new Goods("Items/items_module", "module_extractor", "module_extractor", "module_extractor", "module_extractor"),
            new Goods("Items/items_module", "module_extractor_advanced", "module_extractor_adv", "module_extractor_advanced", "module_extractor_advanced"),
            new Goods("Items/items_module", "module_bay_expansion_kit", "hydro_upgrade_kit", "module_bay_expansion_kit", "module_bay_expansion_kit"),
            new Goods("Items/items_module", "system_uncapped_neural_core", "module_black_5", "system_uncapped_neural_core", "system_uncapped_neural_core"),
            new Goods("Items/items_module", "system_capped_neural_core", "module_black_4", "system_capped_neural_core", "system_capped_neural_core"),
            new Goods("Items/items_module", "node_small", "node_s", "node_small", "node_small"),
            new Goods("Items/items_module", "node_medium", "node_m", "node_medium", "node_medium"),
            new Goods("Items/items_tool", "crypto_module_cmd", "cmd_module", "crypto_module_cmd", "crypto_module_cmd"),
            new Goods("Items/items_tool", "crypto_module_med", "med_module", "crypto_module_med", "crypto_module_med"),
            new Goods("Items/items_tool", "crypto_module_eng", "eng_module", "crypto_module_eng", "crypto_module_eng"),
            new Goods("Items/items_tool", "crypto_module_sup", "sup_module", "crypto_module_sup", "crypto_module_sup"),
            new Goods("Items/items_tool", "crypto_module_ser", "ser_module", "crypto_module_ser", "crypto_module_ser"),
            new Goods("Items/items_tool", "printer_chip_small_bottle", "printer_chip2", "printer_chip_small_bottle", "printer_chip_small_bottle"),
            new Goods("Items/items_tool", "printer_chip_makeshift_rds", "printer_chip2", "printer_chip_makeshift_rds", "printer_chip_makeshift_rds"),
            new Goods("Items/items_tool", "printer_chip_makeshift_toy_laser", "printer_chip2", "printer_chip_makeshift_toy_laser", "printer_chip_makeshift_toy_laser"),
            new Goods("Items/items_tool", "printer_chip_internal_springs_improved", "printer_chip2", "printer_chip_internal_springs_improved", "printer_chip_internal_springs_improved"),
            new Goods("Items/items_tool", "printer_chip_rds", "printer_chip4", "printer_chip_rds", "printer_chip_rds"),
            new Goods("Items/items_tool", "printer_chip_laser_sight", "printer_chip4", "printer_chip_laser_sight", "printer_chip_laser_sight"),
            new Goods("Items/items_tool", "printer_chip_silencer", "printer_chip4", "printer_chip_silencer", "printer_chip_silencer"),
            new Goods("Items/items_tool", "printer_chip_pistol", "printer_chip_pistol", "printer_chip_pistol", "printer_chip_pistol"),
            new Goods("Items/items_tool", "printer_chip_handmade_pistol_frame", "printer_chip_pistol", "printer_chip_handmade_pistol_frame", "printer_chip_handmade_pistol_frame"),
            new Goods("Items/items_tool", "printer_chip_heavy_handmade_pistol_frame", "printer_chip_pistol", "printer_chip_heavy_handmade_pistol_frame", "printer_chip_heavy_handmade_pistol_frame"),
            new Goods("Items/items_tool", "printer_chip_gun_part", "printer_chip_pistol", "printer_chip_gun_part", "printer_chip_gun_part"),
            new Goods("Items/items_tool", "printer_chip_combat_knife", "printer_chip6", "printer_chip_combat_knife", "printer_chip_combat_knife"),
            new Goods("Items/items_tool", "printer_chip_combat_machete", "printer_chip6", "printer_chip_combat_machete", "printer_chip_combat_machete"),
            new Goods("Items/items_tool", "printer_chip_10_mag", "printer_chip4", "printer_chip_10_mag", "printer_chip_10_mag"),
            new Goods("Items/items_tool", "printer_chip_10_mag_ext", "printer_chip4", "printer_chip_10_mag_ext", "printer_chip_10_mag_ext"),
            new Goods("Items/items_tool", "printer_chip_10_mag_high", "printer_chip4", "printer_chip_10_mag_high", "printer_chip_10_mag_high"),
            new Goods("Items/items_tool", "deal_maker", "dealmaker", "deal_maker", "deal_maker"),
            new Goods("Items/items_tool", "security_alarm", "alarm_system", "security_alarm", "security_alarm"),
            new Goods("Items/items_tool", "security_alarm", "alarm_system_r", "security_alarm_red", "security_alarm"),
            new Goods("Items/items_tool", "storage_coordinator", "storage_coordinator", "storage_coordinator", "storage_coordinator"),
            new Goods("Items/items_ship", "makeshift_reactor", "makeshift_reactor", "makeshift_reactor", "makeshift_reactor"),
            new Goods("Items/items_tool2", "mirage_projector", "projector", "mirage_projector", "mirage_projector"),
            new Goods("Items/items_ship", "fridge", "fridge", "fridge", "fridge"),
            new Goods("Items/items_furniture", "cassette_player", "cassette_player", "cassette_player", "cassette_player"),
            new Goods("Items/items_module", "vending_machine_core", "core", "vending_machine_core", "vending_machine_core"),
            new Goods("Items/items_module", "vending_fountain_core", "core", "vending_fountain_core", "vending_fountain_core"),
            new Goods("Items/items_vending", "vending_machine", "vending0", "vending_machine_0", "vending_machine"),
            new Goods("Items/items_vending", "vending_machine", "vending10", "vending_machine_10", "vending_machine"),
            new Goods("Items/items_vending", "vending_machine", "vending25", "vending_machine_25", "vending_machine"),
            new Goods("Items/items_vending", "vending_machine", "vending50", "vending_machine_50", "vending_machine"),
            new Goods("Items/items_vending", "vending_machine", "vending75", "vending_machine_75", "vending_machine"),
            new Goods("Items/items_vending", "vending_machine", "vending90", "vending_machine_90", "vending_machine"),
            new Goods("Items/items_vending", "vending_machine", "vending100", "vending_machine_100", "vending_machine"),
            new Goods("Items/items_vending", "vending_fountain", "fountain0", "vending_fountain_0", "vending_fountain"),
            new Goods("Items/items_vending", "vending_fountain", "fountain10", "vending_fountain_10", "vending_fountain"),
            new Goods("Items/items_vending", "vending_fountain", "fountain25", "vending_fountain_25", "vending_fountain"),
            new Goods("Items/items_vending", "vending_fountain", "fountain50", "vending_fountain_50", "vending_fountain"),
            new Goods("Items/items_vending", "vending_fountain", "fountain75", "vending_fountain_75", "vending_fountain"),
            new Goods("Items/items_vending", "vending_fountain", "fountain90", "vending_fountain_90", "vending_fountain"),
            new Goods("Items/items_vending", "vending_fountain", "fountain100", "vending_fountain_100", "vending_fountain"),
            new Goods("Items/water_container3", "1ml_dropper", "1ml", "1ml_dropper", "1ml_dropper"),
            new Goods("Items/water_container3", "5ml_dropper", "5ml", "5ml_dropper", "5ml_dropper"),
            new Goods("Items/water_container3", "25ml_cylinder", "25ml", "25ml_cylinder", "25ml_cylinder"),
            new Goods("Items/water_container3", "50ml_cylinder", "50ml", "50ml_cylinder", "50ml_cylinder"),
            new Goods("Items/water_container3", "100ml_cylinder", "100ml", "100ml_cylinder", "100ml_cylinder"),
            new Goods("Items/items_medical", "med_bottle_red", "med_bottle_red", "med_bottle_red", "med_bottle_red"),
            new Goods("Items/items_medical", "med_bottle_blue", "med_bottle_blue", "med_bottle_blue", "med_bottle_blue"),
            new Goods("Items/items_medical", "med_bottle_violet", "med_bottle_violet", "med_bottle_violet", "med_bottle_violet"),
            new Goods("Items/items_medical", "med_bottle_orange", "med_bottle_orange", "med_bottle_orange", "med_bottle_orange"),
            new Goods("Items/items_material", "restricted_chemical", "chemical2", "restricted_chemical", "restricted_chemical"),
            new Goods("Items/items_material", "common_medical", "med1", "common_medical", "common_medical"),
            new Goods("Items/items_material", "restricted_medical", "med2", "restricted_medical", "restricted_medical"),
            new Goods("Items/items_material", "rare_electronic", "electronic2", "rare_electronic", "rare_electronic"),
            new Goods("Items/items_material", "hypertech_electronic", "electronic3", "hypertech_electronic", "hypertech_electronic"),
            new Goods("Items/items_material", "advanced_flux_agent", "flux_agent_advanced", "advanced_flux_agent", "advanced_flux_agent"),
            new Goods("Items/items_backpack2", "fanny_pack", "fanny_pack", "fanny_pack", "fanny_pack"),
            new Goods("Items/items_backpack2", "satchel", "satchel", "satchel", "satchel"),
            new Goods("Items/items_backpack2", "kids_backpack", "junor_backpack1", "kids_backpack", "kids_backpack"),
            new Goods("Items/items_backpack2", "kids_backpack_large", "junor_backpack2", "kids_backpack_large", "kids_backpack_large"),
            new Goods("Items/items_backpack2", "backpack_small", "simple_backpack", "backpack_small", "backpack_small"),
            new Goods("Items/items_backpack2", "backpack_medium", "simple_backpack_medium", "backpack_medium", "backpack_medium"),
            new Goods("Items/items_backpack2", "backpack_medium_military", "simple_backpack_medium_military", "backpack_medium_military", "backpack_medium_military"),
            new Goods("Items/items_backpack2", "backpack_large", "simple_backpack_large", "backpack_large", "backpack_large"),
            new Goods("Items/items_backpack2", "backpack_large_military", "simple_backpack_large_military", "backpack_large_military", "backpack_large_military"),
            new Goods("Items/items_container", "med_box", "med_box", "med_box", "med_box"),
            new Goods("Items/items_container", "eng_box", "eng_box", "eng_box", "eng_box"),
            new Goods("Items/items_container", "service_box", "service_box", "service_box", "service_box"),
            new Goods("Items/items_container", "luxury_supply_crate", "eleanor_box", "luxury_supply_crate", "luxury_supply_crate"),
            new Goods("Items/items_container", "expedition_box", "expedition_box", "expedition_box", "expedition_box"),
            new Goods("Items/beer_case", "beerCase", "beer_case_0", "beer_case_0", "beer_case"),
            new Goods("Items/beer_case", "beerCase", "beer_case_1", "beer_case_1", "beer_case"),
            new Goods("Items/beer_case", "beerCase", "beer_case_2", "beer_case_2", "beer_case"),
            new Goods("Items/beer_case", "beerCase", "beer_case_3", "beer_case_3", "beer_case"),
            new Goods("Items/beer_case", "beerCase", "beer_case_4", "beer_case_4", "beer_case"),
            new Goods("Items/beer_case", "beerCase", "beer_case_5", "beer_case_5", "beer_case"),
            new Goods("Items/beer_case", "beerCase", "beer_case_6", "beer_case_6", "beer_case"),
            new Goods("Items/beer_case", "beerCase", "beer_case_7", "beer_case_7", "beer_case"),
            new Goods("Items/beer_case", "beerCase", "beer_case_8", "beer_case_8", "beer_case"),
            new Goods("Items/items_container", "sec_box", "sec_box", "sec_box", "sec_box"),
            new Goods("Items/items_container", "evidence_box", "evidence_box", "evidence_box", "evidence_box"),
            new Goods("Items/items_container", "lou_locker", "evidence_box", "lou_locker", "lou_locker"),
            new Goods("Items/items_medical", "blood_bag", "blood_bag", "blood_bag", "blood_bag"),
            new Goods("Items/items_medical", "blue_blood_bag", "blue_blood_bag", "blue_blood_bag", "blue_blood_bag"),
            new Goods("Items/items_medical", "salve", "salve", "salve", "salve"),
            new Goods("Items/items_medical", "rubbing_alcohol", "black_bottle", "rubbing_alcohol", "rubbing_alcohol"),
            new Goods("Items/items_medical", "smelling_salt", "smelling_salt", "smelling_salt", "smelling_salt"),
            new Goods("Items/items_tool", "surgery_tool", "surgery_toolkit", "surgery_tool", "surgery_tool"),
            new Goods("Items/items_medical", "birth_control_pill", "items_medical_15", "birth_control_pill", "birth_control_pill"),
            new Goods("Items/items_medical", "fanta_pill", "oxycodone_pill", "fanta_pill", "fanta_pill"),
            new Goods("Items/items_medical", "zyanide_pill", "items_medical_12", "zyanide_pill", "zyanide_pill"),
            new Goods("Items/items_medical", "zerochew", "zerochew", "zerochew", "zerochew"),
            new Goods("Items/items_medical", "black_injector", "injector_black", "black_injector", "black_injector"),
            new Goods("Items/items_medical", "pink_injector", "injector_pink", "pink_injector", "pink_injector"),
            new Goods("Items/items_medical", "pink_alt_injector", "injector_pink_alt", "pink_alt_injector", "pink_alt_injector"),
            new Goods("Items/items_medical", "large_purple_injector", "injector_purple_large", "large_purple_injector", "large_purple_injector"),
            new Goods("Items/items_medical", "pure_white_injector", "injector_pure_white_s", "pure_white_injector", "pure_white_injector"),
            new Goods("Items/items_medical", "dream_dust", "dream_dust", "dream_dust", "dream_dust"),
            new Goods("Items/items_medical", "nightmare_dust", "nightmare_dust", "nightmare_dust", "nightmare_dust"),
            new Goods("Items/items_amenities", "chem_scanner", "chem_scanner", "chem_scanner", "chem_scanner"),
            new Goods("Items/items_material", "gun_part", "gun_part", "gun_part", "gun_part"),
            new Goods("Items/items_material", "nuclear_waste", "nuclear_waste", "nuclear_waste", "nuclear_waste"),
            new Goods("Items/items_amenities", "box_tampon", "box_tampon", "box_tampon", "box_tampon"),
            new Goods("Items/items_amenities", "packet_red_cigarette", "cig_red", "packet_red_cigarette", "packet_red_cigarette"),
            new Goods("Items/items_amenities", "pack_condom", "pack_condom", "pack_condom", "pack_condom"),
            new Goods("Items/items_amenities", "skincare_cream", "skincare_cream", "skincare_cream", "skincare_cream"),
            new Goods("Items/items_amenities", "neuroactive_perfume", "perfume1", "neuroactive_perfume", "neuroactive_perfume"),
            new Goods("Items/items_misc", "self_refilling_neuroactive_perfume", "eleanor_perfume", "self_refilling_neuroactive_perfume", "self_refilling_neuroactive_perfume"),
            new Goods("Items/items_amenities", "pheromone_perfume", "perfume2", "pheromone_perfume", "pheromone_perfume"),
            new Goods("Items/items_amenities", "aquascan", "aquascan", "aquascan", "aquascan"),
            new Goods("Items/items_amenities", "metal_scanner", "metal_scanner", "metal_scanner", "metal_scanner"),
            new Goods("Items/items_amenities", "gene_scanner", "genetic_scanner", "gene_scanner", "gene_scanner"),
            new Goods("Items/items_amenities", "gene_scanner", "genetic_scanner_on", "gene_scanner_on", "gene_scanner"),
            new Goods("Items/items_amenities", "portable_water_purifier", "portable_water_purifier", "portable_water_purifier", "portable_water_purifier"),
            new Goods("Items/items_hydroponic_plant", "nutrient_tablet", "nutrient_tablet", "nutrient_tablet", "nutrient_tablet"),
            new Goods("Items/items_hydroponic_plant", "nutrient_tablet_container", "nutrient_tablet_container", "nutrient_tablet_container", "nutrient_tablet_container"),
            new Goods("Items/items_amenities", "lottery_ticket", "lottery", "lottery_ticket", "lottery_ticket"),
            new Goods("Items/items_amenities", "lottery_ticket_old", "lottery_old", "lottery_ticket_old", "lottery_ticket_old"),
            new Goods("Items/items_tool", "turbo_booster", "turbo_booster", "turbo_booster", "turbo_booster"),
            new Goods("Items/items_tool", "turbo_booster_adv", "turbo_booster_adv", "turbo_booster_adv", "turbo_booster_adv"),
            new Goods("Items/items_tool2", "turbo_booster_hypertech", "turbo_booster_hypertech", "turbo_booster_hypertech", "turbo_booster_hypertech"),
            new Goods("Items/items_tool", "mind_reprogrammer", "mind_reprogrammer", "mind_reprogrammer", "mind_reprogrammer"),
            new Goods("Items/items_tool", "xray_scanner", "spectral_analyser", "xray_scanner", "xray_scanner"),
            new Goods("Items/items_sign", "sign_water", "water_sign", "sign_water", "sign_water"),
            new Goods("Items/items_sign", "sign_food", "food_sign", "sign_food", "sign_food"),
            new Goods("Items/items_sign", "sign_medical", "medical_sign", "sign_medical", "sign_medical"),
            new Goods("Items/items_sign", "sign_household", "household_sign", "sign_household", "sign_household"),
            new Goods("Items/items_sign", "sign_material", "material_sign", "sign_material", "sign_material"),
            new Goods("Items/items_sign", "sign_ruined_machine", "material_sign", "sign_ruined_machine", "sign_ruined_machine"),
            new Goods("Items/items_sign", "sign_firearm", "gun_sign", "sign_firearm", "sign_firearm"),
            new Goods("Items/items_sign", "sign_firearm_heavy", "gun_sign2", "sign_firearm_heavy", "sign_firearm_heavy"),
            new Goods("Items/items_sign", "sign_berry", "berry_sign", "sign_berry", "sign_berry"),
            new Goods("Items/items_sign", "sign_expedition", "hiring_sign", "sign_expedition", "sign_expedition"),
            new Goods("Items/items_tool", "intake_hopper", "hopper_in", "intake_hopper", "intake_hopper"),
            new Goods("Items/items_tool", "outlet_hopper", "hopper_out", "outlet_hopper", "outlet_hopper"),
            new Goods("Items/items_armor", "armor_lining", "armor_lining", "armor_lining", "armor_lining"),
            new Goods("Items/items_construction", "nuts_metal", "nuts_metal_pile", "nuts_metal", "nuts_metal"),
            new Goods("Items/items_misc", "liquid_luck", "liquid_luck_bottle", "liquid_luck", "liquid_luck"),
            new Goods("Items/items_misc", "cmd_keycard", "command_keycard", "cmd_keycard", "cmd_keycard"),
            new Goods("Items/items_misc", "sci_keycard", "sci_card", "sci_keycard", "sci_keycard"),
            new Goods("Items/items_misc", "med_keycard", "med_card", "med_keycard", "med_keycard"),
            new Goods("Items/items_misc", "eng_keycard", "eng_card", "eng_keycard", "eng_keycard"),
            new Goods("Items/items_misc", "sup_keycard", "sup_card", "sup_keycard", "sup_keycard"),
            new Goods("Items/items_misc", "ser_keycard", "ser_card", "ser_keycard", "ser_keycard"),
            new Goods("Items/items_misc", "joe_card", "card_wild", "joe_card", "joe_card"),
            new Goods("Items/items_misc", "card_brewer", "card_brewer", "card_brewer", "card_brewer"),
            new Goods("Items/items_misc", "card_mining", "card_mining", "card_mining", "card_mining"),
            new Goods("Items/items_misc", "card_pharma", "card_pharma", "card_pharma", "card_pharma"),
            new Goods("Items/items_misc", "card_chem", "card_chem", "card_chem", "card_chem"),
            new Goods("Items/items_misc", "card_rev", "card_rev", "card_rev", "card_rev"),
            new Goods("Items/items_misc", "cigarette_guide", "guide1", "cigarette_guide", "cigarette_guide"),
            new Goods("Items/items_misc", "hand_guide", "manual5", "hand_guide", "hand_guide"),
            new Goods("Items/items_misc", "fuel_guide", "manual1", "fuel_guide", "fuel_guide"),
            new Goods("Items/items_misc", "ribwich_guide", "manual4", "ribwich_guide", "ribwich_guide"),
            new Goods("Items/items_misc", "stamp_guide", "guide3", "stamp_guide", "stamp_guide"),
            new Goods("Items/items_misc", "injector_guide", "guide5", "injector_guide", "injector_guide"),
            new Goods("Items/items_misc", "aug_id_guide", "aug_id_guide", "aug_id_guide", "aug_id_guide"),
            new Goods("Items/items_misc", "water_guide", "book1", "water_guide", "water_guide"),
            new Goods("Items/items_misc", "hydroponic_guide", "book3", "hydroponic_guide", "hydroponic_guide"),
            new Goods("Items/items_misc", "chemistry_guide", "book2", "chemistry_guide", "chemistry_guide"),
            new Goods("Items/items_misc", "ingot_book", "book4", "ingot_book", "ingot_book"),
            new Goods("Items/items_misc", "wine_book", "book5", "wine_book", "wine_book"),
            new Goods("Items/items_misc", "lottery_info", "guide0", "lottery_info", "lottery_info"),
            new Goods("Items/itematlas", "tutorial_book", "tutorial_book", "tutorial_book", "tutorial_book"),
            new Goods("Items/itematlas", "tutorial_book", "tutorial_book_full", "tutorial_book_full", "tutorial_book"),
            new Goods("Items/itematlas", "tutorial_page", "tutorial_page", "tutorial_page", "tutorial_page"),
            new Goods("Items/itematlas", "newspaper", "newspaper", "newspaper", "newspaper"),
            new Goods("Items/items_misc", "deed", "deed", "deed", "deed"),
            new Goods("Items/items_misc", "mentor_contract", "deed", "mentor_contract", "mentor_contract"),
            new Goods("Items/items_misc", "business_permit", "deed", "business_permit", "business_permit"),
            new Goods("Items/itematlas", "procurement_slip", "slip", "procurement_slip", "procurement_slip"),
            new Goods("Items/items_misc", "slip", "slip", "slip", "slip"),
            new Goods("Items/items_misc", "handnote", "handnote", "handnote", "handnote"),
            new Goods("Items/items_misc", "exchange_directive", "exchange_directive", "exchange_directive", "exchange_directive"),
            new Goods("Items/items_misc", "intel_sec", "intel_sec", "intel_sec", "intel_sec"),
            new Goods("Items/items_misc", "permit_gun_1", "permit_gun_1", "permit_gun_1", "permit_gun_1"),
            new Goods("Items/items_misc", "permit_gun_2", "permit_gun_2", "permit_gun_2", "permit_gun_2"),
            new Goods("Items/items_misc", "permit_gun_3", "permit_gun_3", "permit_gun_3", "permit_gun_3"),
            new Goods("Items/items_misc", "sec_slip", "sec_slip", "sec_slip", "sec_slip"),
            new Goods("Items/itematlas", "wanted_paper", "paper_wanted", "wanted_paper", "wanted_paper"),
            new Goods("Items/items_misc", "food_stamp", "food_stamp", "food_stamp", "food_stamp"),
            new Goods("Items/items_misc", "expedition_log", "handnote", "expedition_log", "expedition_log"),
            new Goods("Items/items_misc", "lou_old_orders", "aug_id_guide", "lou_old_orders", "lou_old_orders"),
            new Goods("Items/items_misc", "rev_codeword", "slip", "rev_codeword", "rev_codeword"),
            new Goods("Items/items_misc", "cert1", "cert1", "cert1", "cert1"),
            new Goods("Items/items_misc", "cert2", "cert2", "cert2", "cert2"),
            new Goods("Items/items_misc", "cert3", "cert3", "cert3", "cert3"),
            new Goods("Items/items_misc", "cert4", "cert4", "cert4", "cert4"),
            new Goods("Items/items_tool", "postit", "postit", "postit", "postit"),
            new Goods("Items/itematlas", "cigarette_color", "cigarette_color_guide", "cigarette_color", "cigarette_color"),
            new Goods("Items/dossier", "dossier", "dossier_0", "dossier_0", "dossier"),
            new Goods("Items/dossier", "dossier", "dossier_1", "dossier_1", "dossier"),
            new Goods("Items/dossier", "dossier", "dossier_2", "dossier_2", "dossier"),
            new Goods("Items/dossier", "dossier", "dossier_3", "dossier_3", "dossier"),
            new Goods("Items/items_misc", "scratcher", "scratch", "scratcher", "scratcher"),
            new Goods("Items/items_misc", "scratcher_stack", "scratch_stack", "scratcher_stack", "scratcher_stack"),
            new Goods("Items/items_misc", "gamblers_coin", "the_coin", "gamblers_coin", "gamblers_coin"),
            new Goods("Items/itematlas", "scav_token", "scav_medal", "scav_token", "scav_token"),
            new Goods("Items/items_misc", "flashlight", "flashlight3", "flashlight", "flashlight"),
            new Goods("Items/items_sign", "black_lamp", "black_lamp", "black_lamp", "black_lamp"),
            new Goods("Items/items_misc", "republic_transmitter", "republic_transmitter", "republic_transmitter", "republic_transmitter"),
            new Goods("Items/items_misc", "logo_checker", "republic_logo_checker", "logo_checker", "logo_checker"),
            new Goods("Items/items_module", "aug_chip", "aug_chip", "aug_chip", "aug_chip"),
            new Goods("Items/items_tool", "aug_scanner", "aug_scanner", "aug_scanner", "aug_scanner"),
            new Goods("Items/items_inspectable", "hand", "hand_steel", "hand_steel", "hand"),
            new Goods("Items/items_inspectable", "hand", "hand_carbon", "hand_carbon", "hand"),
            new Goods("Items/items_inspectable", "hand", "hand_gold", "hand_gold", "hand"),
            new Goods("Items/items_food", "ribwich", "ribwich", "ribwich", "ribwich"),
            new Goods("Items/items_ship", "plasma_fuel", "plasma_fuel_cell", "plasma_fuel", "plasma_fuel"),
            new Goods("Items/items_container", "plastic_bag", "bag", "plastic_bag", "plastic_bag"),
            new Goods("Items/items_amenities", "cassette1", "cassette5", "music_token_1", "cassette1"),
            new Goods("Items/items_amenities", "cassette2", "cassette8", "music_token_2", "cassette2"),
            new Goods("Items/items_amenities", "cassette3", "cassette6", "music_token_3", "cassette3"),
            new Goods("Items/items_amenities", "cassette4", "cassette9", "music_token_4", "cassette4"),
            new Goods("Items/items_amenities", "cassette5", "cassette10", "music_token_5", "cassette5"),
            new Goods("Items/items_amenities", "cassette6", "cassette3", "music_token_6", "cassette6"),
            new Goods("Items/items_amenities", "cassette7", "cassette4", "music_token_7", "cassette7"),
            new Goods("Items/items_amenities", "cassette1l", "cassette5", "music_token_1", "cassette1l"),
            new Goods("Items/items_amenities", "cassette2l", "cassette8", "music_token_2", "cassette2l"),
            new Goods("Items/items_amenities", "cassette3l", "cassette6", "music_token_3", "cassette3l"),
            new Goods("Items/items_amenities", "cassette4l", "cassette9", "music_token_4", "cassette4l"),
            new Goods("Items/items_amenities", "cassette5l", "cassette10", "music_token_5", "cassette5l"),
            new Goods("Items/items_amenities", "cassette6l", "cassette3", "music_token_6", "cassette6l"),
            new Goods("Items/items_amenities", "cassette7l", "cassette4", "music_token_7", "cassette7l"),
            new Goods("Items/items_amenities", "cassette1d", "cassette5", "music_token_1", "cassette1d"),
            new Goods("Items/items_amenities", "cassette2d", "cassette8", "music_token_2", "cassette2d"),
            new Goods("Items/items_amenities", "cassette3d", "cassette6", "music_token_3", "cassette3d"),
            new Goods("Items/items_amenities", "cassette4d", "cassette9", "music_token_4", "cassette4d"),
            new Goods("Items/items_amenities", "cassette5d", "cassette10", "music_token_5", "cassette5d"),
            new Goods("Items/items_amenities", "cassette6d", "cassette3", "music_token_6", "cassette6d"),
            new Goods("Items/items_amenities", "cassette7d", "cassette4", "music_token_7", "cassette7d"),
            new Goods("Items/items_amenities", "cassette8", "cassette8b", "music_token_8", "cassette8"),
            new Goods("Items/items_amenities", "cassette9", "cassette9b", "music_token_9", "cassette9"),
            new Goods("Items/items_amenities", "cassette10", "cassette10b", "music_token_10", "cassette10"),
            new Goods("Items/items_amenities", "cassette11", "cassette11b", "music_token_11", "cassette11"),
            new Goods("Items/items_amenities", "cassette12", "cassette12b", "music_token_12", "cassette12"),
            new Goods("Items/items_amenities", "cassette13", "cassette13b", "music_token_13", "cassette13"),
            new Goods("Items/items_amenities", "cassette14", "cassette14b", "music_token_14", "cassette14"),
            new Goods("Items/items_amenities", "cassette8l", "cassette8b", "music_token_8", "cassette8l"),
            new Goods("Items/items_amenities", "cassette9l", "cassette9b", "music_token_9", "cassette9l"),
            new Goods("Items/items_amenities", "cassette10l", "cassette10b", "music_token_10", "cassette10l"),
            new Goods("Items/items_amenities", "cassette11l", "cassette11b", "music_token_11", "cassette11l"),
            new Goods("Items/items_amenities", "cassette12l", "cassette12b", "music_token_12", "cassette12l"),
            new Goods("Items/items_amenities", "cassette13l", "cassette13b", "music_token_13", "cassette13l"),
            new Goods("Items/items_amenities", "cassette14l", "cassette14b", "music_token_14", "cassette14l"),
            new Goods("Items/items_amenities", "cassette8d", "cassette8b", "music_token_8", "cassette8d"),
            new Goods("Items/items_amenities", "cassette9d", "cassette9b", "music_token_9", "cassette9d"),
            new Goods("Items/items_amenities", "cassette10d", "cassette10b", "music_token_10", "cassette10d"),
            new Goods("Items/items_amenities", "cassette11d", "cassette11b", "music_token_11", "cassette11d"),
            new Goods("Items/items_amenities", "cassette12d", "cassette12b", "music_token_12", "cassette12d"),
            new Goods("Items/items_amenities", "cassette13d", "cassette13b", "music_token_13", "cassette13d"),
            new Goods("Items/items_amenities", "cassette14d", "cassette14b", "music_token_14", "cassette14d"),
            new Goods("Items/items_furniture", "trashcan", "trashcan0", "trashcan0", "trashcan"),
            new Goods("Items/items_furniture", "trashcan", "trashcan25", "trashcan25", "trashcan"),
            new Goods("Items/items_furniture", "trashcan", "trashcan50", "trashcan50", "trashcan"),
            new Goods("Items/items_furniture", "trashcan", "trashcan75", "trashcan75", "trashcan"),
            new Goods("Items/items_furniture", "trashcan", "trashcan100", "trashcan100", "trashcan"),
            new Goods("Items/items_furniture", "drain", "drain", "drain", "drain"),
            new Goods("Items/items_furniture", "faucet", "faucet", "faucet", "faucet"),
            new Goods("Items/items_ship", "debris_pile1", "debris_pile1", "debris_pile1", "debris_pile1"),
            new Goods("Items/items_ship", "debris_pile2", "debris_pile2", "debris_pile2", "debris_pile2"),
            new Goods("Items/items_ship", "debris_pile3", "debris_pile3", "debris_pile3", "debris_pile3"),
            new Goods("Items/items_ship", "makeshift_storage_bay", "storage_bay", "makeshift_storage_bay", "makeshift_storage_bay"),
            new Goods("Items/items_ship", "storage_bay", "storage_bay", "storage_bay", "storage_bay"),
            new Goods("Items/items_ship", "storage_bay_large", "storage_bay_large", "storage_bay_large", "storage_bay_large"),
            new Goods("Items/items_tool2", "storage_bay_expansion_kit", "storage_bay_expansion_kit", "storage_bay_expansion_kit", "storage_bay_expansion_kit"),
            new Goods("Items/items_tool2", "machine_bay", "machine_bay0", "machine_bay0", "machine_bay"),
            new Goods("Items/items_tool2", "machine_bay", "machine_bay1", "machine_bay1", "machine_bay"),
            new Goods("Items/items_tool2", "machine_bay", "machine_bay2", "machine_bay2", "machine_bay"),
            new Goods("Items/items_tool2", "machine_bay", "machine_bay3", "machine_bay3", "machine_bay"),
            new Goods("Items/items_tool2", "machine_bay", "machine_bay4", "machine_bay4", "machine_bay"),
            new Goods("Items/items_tool2", "machine_bay_ext", "machine_bay_ext", "machine_bay_ext", "machine_bay_ext"),
            new Goods("Items/items_ship", "storage_bay_gun", "storage_bay_gun", "storage_bay_gun", "storage_bay_gun"),
            new Goods("Items/items_ship", "storage_bay_chem", "storage_bay_chem", "storage_bay_chem", "storage_bay_chem"),
            new Goods("Items/items_ship", "smuggler_bay_mini", "smuggler_bay_mini", "smuggler_bay_mini", "smuggler_bay_mini"),
            new Goods("Items/items_ship", "smuggler_bay", "smuggler_bay", "smuggler_bay", "smuggler_bay"),
            new Goods("Items/items_ship", "smuggler_bay_mod", "smuggler_bay_large", "smuggler_bay_mod", "smuggler_bay_mod"),
            new Goods("Items/items_tool2", "hypertech_bay", "hypertech_bay", "hypertech_bay", "hypertech_bay"),
            new Goods("Items/items_weapons_melee", "combat_knife", "combat_knife_2", "combat_knife", "combat_knife"),
            new Goods("Items/items_weapons_melee", "combat_machete", "combat_machete", "combat_machete", "combat_machete"),
            new Goods("Items/items_weapons_melee", "pipe_weapon", "pipe", "pipe_weapon", "pipe_weapon"),
            new Goods("Items/items_weapons_melee", "stun_baton", "stun_baton", "stun_baton", "stun_baton"),
            new Goods("Items/items_weapons_melee", "crowbar", "crowbar", "crowbar", "crowbar"),
            new Goods("Items/items_weapons_melee", "glass_shard_shiv", "glass_shard_shiv", "glass_shard_shiv", "glass_shard_shiv"),
            new Goods("Items/items_weapons_melee", "skinning_knife", "skinning_knife", "skinning_knife", "skinning_knife"),
            new Goods("Items/items_weapons_melee", "glass_shard", "glass_shard", "glass_shard", "glass_shard"),
            new Goods("Items/items_mod", "rds", "rds_item", "rds", "rds"),
            new Goods("Items/items_mod", "rds_makeshift", "rds_makeshift_item", "rds_makeshift", "rds_makeshift"),
            new Goods("Items/items_mod", "laser", "laser_item", "laser", "laser"),
            new Goods("Items/items_mod", "laser2", "laser2_item", "laser2", "laser2"),
            new Goods("Items/items_mod", "laser_toy", "laser_toy_item", "laser_toy", "laser_toy"),
            new Goods("Items/items_mod", "bipod", "bipod_item", "bipod", "bipod"),
            new Goods("Items/items_mod", "grip", "grip_item", "grip", "grip"),
            new Goods("Items/items_mod", "stock", "stock_item", "stock", "stock"),
            new Goods("Items/items_mod", "short_stock", "short_stock_item", "short_stock", "short_stock"),
            new Goods("Items/items_mod", "silencer", "silencer_item", "silencer", "silencer"),
            new Goods("Items/items_mod", "silencer2", "silencer2_item", "silencer2", "silencer2"),
            new Goods("Items/items_mod", "silencer_makeshift", "bottle_item", "silencer_makeshift", "silencer_makeshift"),
            new Goods("Items/items_mod", "barrel", "barrel_item", "gun_barrel", "barrel"),
            new Goods("Items/items_mod", "compensator", "compensator_item", "compensator", "compensator"),
            new Goods("Items/items_mod", "internal_springs", "special_mod", "internal_springs", "internal_springs"),
            new Goods("Items/items_mod", "internal_springs_improved", "special_mod1", "internal_springs_improved", "internal_springs_improved"),
            new Goods("Items/items_mod", "trigger_group", "special_mod", "trigger_group", "trigger_group"),
            new Goods("Items/items_mod", "match_barrel", "special_mod", "match_barrel", "match_barrel"),
            new Goods("Items/items_mod", "pistol_switch", "special_mod", "pistol_switch", "pistol_switch"),
            new Goods("Items/items_mod", "ammo_display", "special_mod", "ammo_display", "ammo_display"),
            new Goods("Items/items_mod", "grip_gel", "special_mod", "grip_gel", "grip_gel"),
            new Goods("Items/items_mod", "mag_10mm", "10mm_mag_item", "mag_10mm", "mag_10mm"),
            new Goods("Items/items_mod", "mag_10mm_high", "10mm_mag_high_item", "mag_10mm_high", "mag_10mm_high"),
            new Goods("Items/items_mod", "mag_10mm_ext", "10mm_mag_ext_item", "mag_10mm_ext", "mag_10mm_ext"),
            new Goods("Items/items_mod", "mag_10mm_smg", "10mm_mag_smg_item", "mag_10mm_smg", "mag_10mm_smg"),
            new Goods("Items/items_mod", "mag_10mm_smg_high", "10mm_mag_smg_high_item", "mag_10mm_smg_high", "mag_10mm_smg_high"),
            new Goods("Items/items_mod", "mag_10mm_smg_box", "10mm_mag_smg_box_item", "mag_10mm_smg_box", "mag_10mm_smg_box"),
            new Goods("Items/items_mod", "gl", "gl_item", "underbarrel_gl", "gl"),
            new Goods("Items/items_weapons_ranged_energy", "handmade_pistol", "handmade_pistol", "weapon_handmade_pistol", "handmade_pistol"),
            new Goods("Items/items_weapons_ranged_energy", "heavy_handmade_pistol", "heavy_handmade_pistol", "weapon_heavy_handmade_pistol", "heavy_handmade_pistol"),
            new Goods("Items/items_weapons_ranged_energy", "glock_receiver", "glock_receiver", "weapon_glock_receiver", "glock_receiver"),
            new Goods("Items/items_weapons_ranged_energy", "revolver", "revolver", "weapon_revolver", "revolver"),
            new Goods("Items/items_weapons_ranged_antique", "smg", "heavy_smg", "weapon_smg", "smg"),
            new Goods("Items/items_weapons_ranged_antique", "shotgun", "shotgun", "weapon_shotgun", "shotgun"),
            new Goods("Items/items_weapons_ranged_energy", "stun_gun", "stun_gun", "weapon_stun_gun", "stun_gun"),
            new Goods("Items/items_sec", "tazer", "tazer", "weapon_tazer", "tazer"),
            new Goods("Items/items_sec", "small_pistol_ammo", "light_pistol_ammo", "ammo_small_pistol_ammo", "small_pistol_ammo"),
            new Goods("Items/items_sec", "small_pistol_ammo_p", "light_pistol_ammo_p", "ammo_small_pistol_ammo_p", "small_pistol_ammo_p"),
            new Goods("Items/items_sec", "small_pistol_ammo_nl", "light_pistol_ammo_nl", "ammo_small_pistol_ammo_nl", "small_pistol_ammo_nl"),
            new Goods("Items/items_sec", "heavy_pistol_ammo", "heavy_pistol_ammo", "ammo_heavy_pistol_ammo", "heavy_pistol_ammo"),
            new Goods("Items/items_sec", "heavy_pistol_ammo_p", "heavy_pistol_ammo_p", "ammo_heavy_pistol_ammo_p", "heavy_pistol_ammo_p"),
            new Goods("Items/items_sec", "heavy_pistol_ammo_nl", "heavy_pistol_ammo_nl", "ammo_heavy_pistol_ammo_nl", "heavy_pistol_ammo_nl"),
            new Goods("Items/items_sec", "lou_etched_round", "heavy_pistol_ammo", "ammo_heavy_pistol_ammo", "lou_etched_round"),
            new Goods("Items/items_weapons_ranged_energy", "handmade_pistol_frame", "handmade_pistol_frame", "weapon_handmade_pistol_frame", "handmade_pistol_frame"),
            new Goods("Items/items_weapons_ranged_energy", "heavy_handmade_pistol_frame", "heavy_handmade_pistol_frame", "weapon_heavy_handmade_pistol_frame", "heavy_handmade_pistol_frame"),
            new Goods("Items/itematlas", "smoke_grenade", "small_smoke", "ordnance_smoke", "smoke_grenade"),
            new Goods("Items/itematlas", "flashbang_grenade", "small_flash", "ordnance_flash", "flashbang_grenade"),
            new Goods("Items/itematlas", "c4", "c4", "ordnance_c4", "c4"),
            new Goods("Items/itematlas", "c4_set", "c4", "ordnance_c4", "c4_set"),
            new Goods("Items/itematlas", "lou_bomb", "c4", "ordnance_c4", "lou_bomb"),
            new Goods("Items/items_sec", "canister_mbs", "canister_nerve", "ordnance_canister_mbs", "canister_mbs"),
            new Goods("Items/items_ship", "storage_bay", "storage_bay1", "bay_appearance_storage_bay1", "storage_bay"),
            new Goods("Items/items_ship", "storage_bay", "storage_bay2", "bay_appearance_storage_bay2", "storage_bay"),
            new Goods("Items/items_ship", "storage_bay_large", "storage_bay_large1", "bay_appearance_storage_bay_large1", "storage_bay_large"),
            new Goods("Items/items_ship", "storage_bay_large", "storage_bay_large2", "bay_appearance_storage_bay_large2", "storage_bay_large"),
        };
        private static readonly Dictionary<string, List<Goods>> Items = BuildItems();
        private static readonly Dictionary<Sprite, Dictionary<Texture2D, Sprite>> Icons =
            new Dictionary<Sprite, Dictionary<Texture2D, Sprite>>();
        private static readonly Dictionary<Sprite, Sprite> NativeByReplacement = new Dictionary<Sprite, Sprite>();

        private static Dictionary<string, List<Goods>> BuildItems()
        {
            var items = new Dictionary<string, List<Goods>>(StringComparer.Ordinal);
            foreach (Goods rule in Rules)
            {
                List<Goods> bucket;
                if (!items.TryGetValue(rule.Identifier, out bucket))
                { bucket = new List<Goods>(); items.Add(rule.Identifier, bucket); }
                foreach (Goods previous in bucket)
                    if (previous.Atlas == rule.Atlas && previous.NativeSprite == rule.NativeSprite)
                        throw new InvalidOperationException("Duplicate goods identity: " + rule.Identifier);
                bucket.Add(rule);
            }
            return items;
        }
        private static readonly HashSet<string> ChargerParts = new HashSet<string>(StringComparer.Ordinal)
        { "recharger_top", "recharger_g", "recharger_o" };
        private sealed class WeaponLayer
        {
            internal readonly string Identifier, NativeSprite, ArtKey;
            internal WeaponLayer(string identifier, string nativeSprite, string artBase)
            { Identifier = identifier; NativeSprite = nativeSprite; ArtKey = ArtPrefix + artBase; }
        }
        private static readonly WeaponLayer[] WeaponLayers = {
            new WeaponLayer("rds", "rds_view2", "weapon_layer_rds"),
            new WeaponLayer("rds_makeshift", "rds_makeshift_view", "weapon_layer_rds_makeshift"),
            new WeaponLayer("laser", "laser_view", "weapon_layer_laser"),
            new WeaponLayer("laser2", "laser2_view", "weapon_layer_laser2"),
            new WeaponLayer("laser_toy", "laser_toy_view", "weapon_layer_laser_toy"),
            new WeaponLayer("silencer", "silencer_view", "weapon_layer_silencer"),
            new WeaponLayer("silencer2", "silencer2_view", "weapon_layer_silencer2"),
            new WeaponLayer("silencer_makeshift", "bottle_view", "weapon_layer_silencer_makeshift"),
            new WeaponLayer("barrel", "barrel_view", "weapon_layer_barrel"),
            new WeaponLayer("compensator", "compensator_view", "weapon_layer_compensator"),
            new WeaponLayer("bipod", "bipod_view", "weapon_layer_bipod"),
            new WeaponLayer("grip", "grip_view", "weapon_layer_grip"),
            new WeaponLayer("stock", "stock_view", "weapon_layer_stock"),
            new WeaponLayer("short_stock", "short_stock_view", "weapon_layer_short_stock"),
            new WeaponLayer("mag_10mm", "10mm_mag_view", "weapon_layer_mag10mm"),
            new WeaponLayer("mag_10mm_high", "10mm_mag_view", "weapon_layer_mag10mm"),
            new WeaponLayer("mag_10mm_ext", "10mm_mag_ext_view", "weapon_layer_mag10mm_ext"),
            new WeaponLayer("mag_10mm_smg", "10mm_mag_smg_view", "weapon_layer_mag10mm_smg"),
            new WeaponLayer("mag_10mm_smg_high", "10mm_mag_smg_high_view", "weapon_layer_mag10mm_smg_high"),
            new WeaponLayer("mag_10mm_smg_box", "10mm_mag_smg_box_item", "mag_10mm_smg_box"),
            new WeaponLayer("gl", "gl_view", "weapon_layer_gl"),
        };
        private static readonly HashSet<string> WeaponParents = new HashSet<string>(StringComparer.Ordinal)
        { "printed_gun", "handmade_pistol", "heavy_handmade_pistol", "glock_receiver", "smg", "shotgun", "revolver", "stun_gun", "tazer" };
        private static readonly Dictionary<Sprite, Sprite> FillMarkers = new Dictionary<Sprite, Sprite>();
        private static Assembly itemDetailsAssembly;
        private static readonly HashSet<MethodBase> ItemDetailsPatches = new HashSet<MethodBase>();
        // The vanilla note is saved as a generic post-it with its original localized title.
        private static readonly HashSet<string> PurifierNoteNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "净水器说明", "Water Purifier Instructions", "Instructions - Purificateur d'eau",
            "Anleitung: Der Wasserreiniger", "浄水器の説明書", "Instrukcja użycia uzdatniacza wody",
            "Инструкция к очистителю воды", "Manual del purificador de agua",
            "item_water_purifier_instruction_name"
        };

        private static Goods Find(GameItem item)
        {
            List<Goods> bucket;
            if (item == null || item.useSpriteFromMod || String.IsNullOrEmpty(item.spritePath)) return null;
            string identifier = item.identifier;
            DirectPresentation presentation;
            if (DirectItems.TryGetValue(item, out presentation) && identifier == presentation.NativeIdentifier)
                identifier = presentation.Identifier;
            if (String.IsNullOrEmpty(identifier) || !Items.TryGetValue(identifier, out bucket)) return null;
            foreach (Goods goods in bucket)
                if (item.spriteAtlasPath == goods.Atlas && item.spritePath == goods.NativeSprite) return goods;
            return null;
        }

        private sealed class DirectPresentation
        {
            internal readonly string NativeIdentifier, Identifier;
            internal DirectPresentation(GameItem item, string identifier)
            { NativeIdentifier = item.identifier; Identifier = identifier; }
        }
        // Factory provenance is presentation-only: never write an ID, tag or saved field.
        // Weak keys neither retain dead items nor disappear on a visual cache disposal.
        private static readonly ConditionalWeakTable<GameItem, DirectPresentation> DirectItems =
            new ConditionalWeakTable<GameItem, DirectPresentation>();

        private static void RememberDirect(GameItem item, string identifier)
        {
            if (item == null) return;
            DirectItems.Remove(item);
            DirectItems.Add(item, new DirectPresentation(item, identifier));
        }

        [HarmonyPatch(typeof(LouToyBoxHelper), "CreateOldOrders")]
        private static class DirectOldOrders
        { private static void Postfix(GameItem __result) { RememberDirect(__result, "lou_old_orders"); } }

        [HarmonyPatch(typeof(LouOperativeHelper), "CreateTransmitter")]
        private static class DirectTransmitter
        { private static void Postfix(GameItem __result) { RememberDirect(__result, "republic_transmitter"); } }

        [HarmonyPatch(typeof(IntelHelper), "CreateRevCodeword")]
        private static class DirectCodeword
        { private static void Postfix(GameItem __result) { RememberDirect(__result, "rev_codeword"); } }

        [HarmonyPatch(typeof(DebrisHelper), "CreatePile")]
        private static class DirectDebris
        {
            private static void Postfix(GameItem __result)
            {
                if (__result != null && (__result.spritePath == "debris_pile1"
                    || __result.spritePath == "debris_pile2" || __result.spritePath == "debris_pile3"))
                    RememberDirect(__result, __result.spritePath);
            }
        }

        [HarmonyPatch(typeof(StorageExpansionHelper), "CreateKit")]
        private static class DirectStorageKit
        { private static void Postfix(GameItem __result) { RememberDirect(__result, "storage_bay_expansion_kit"); } }

        [HarmonyPatch(typeof(LouBombHelper), "CreateBomb")]
        private static class DirectBomb
        { private static void Postfix(GameItem __result) { RememberDirect(__result, "lou_bomb"); } }

        private static string Text(GameItem item, string suffix)
        {
            if (!Mod.Active) return null;
            Goods goods = Find(item);
            if (goods != null)
            {
                string text = Mod.Chinese ? Mod.Text(goods.TextKey + suffix) : null;
                // The native callback owns this live progress string. Keep all of it visible.
                if (text != null && suffix == "_desc" && item.identifier == "blank_keycard"
                    && item.spritePath == "blank_card_wip" && !String.IsNullOrEmpty(item.shortDescription))
                    return text + "\n" + item.shortDescription;
                return text ?? RemainderItemText.Get(item, suffix);
            }
            if (!item.useSpriteFromMod && item.identifier == "postit" && PurifierNoteNames.Contains(item.name))
                return Mod.CurrentText("purifier_note" + suffix);
            return CraftingText.NoteText(item, suffix) ?? ChemicalText.NoteText(item, suffix) ?? AccessCraftingText.NoteText(item, suffix);
        }

        private static bool TryGetIcon(GameItem item, Sprite stock, out Texture2D art)
        {
            art = null;
            Goods goods = Find(item);
            // LiquidMarker also uses this art/size lookup for its native liquid layers.
            return Mod.Active && Mod.Assets != null && stock != null && goods != null
                && Mod.Assets.Textures.TryGetValue(goods.ArtKey, out art)
                && art != null && stock.rect.width == art.width && stock.rect.height == art.height;
        }

        private static Sprite CanonicalMain(GameItem item, Sprite current)
        {
            Goods goods = Find(item);
            if (goods == null || current == null) return null;
            Sprite native = RenderHandler.LoadFromAtlas(goods.Atlas, goods.NativeSprite);
            Sprite origin;
            return native != null && (current == native
                || (NativeByReplacement.TryGetValue(current, out origin) && origin == native)) ? native : null;
        }

        private static Sprite GetOrCreate(Sprite native, Texture2D art)
        {
            Dictionary<Texture2D, Sprite> variants;
            if (!Icons.TryGetValue(native, out variants))
            { variants = new Dictionary<Texture2D, Sprite>(); Icons.Add(native, variants); }
            Sprite replacement;
            if (!variants.TryGetValue(art, out replacement))
            {
                replacement = CreateLike(native, art);
                variants.Add(art, replacement);
                NativeByReplacement.Add(replacement, native);
            }
            return replacement;
        }

        private static Sprite CreateLike(Sprite stock, Texture2D art)
        {
            Vector2 pivot = new Vector2(stock.pivot.x / art.width, stock.pivot.y / art.height);
            art.filterMode = FilterMode.Point;
            Sprite sprite = Sprite.Create(art, new Rect(0, 0, art.width, art.height), pivot,
                stock.pixelsPerUnit, 0, SpriteMeshType.FullRect, stock.border);
            sprite.name = stock.name;
            UnityEngine.Object.DontDestroyOnLoad(sprite);
            return sprite;
        }

        private static Sprite CreateFillMarker(Sprite stock, int percent, string shade)
        {
            int width = (int)stock.rect.width;
            int height = (int)stock.rect.height;
            int bottom = height / 7;
            int fill = Mathf.Max(1, Mathf.RoundToInt(height * .62f * percent / 100f));
            Color32[] pixels = new Color32[width * height];
            bool highlight = shade == "hl";
            Color32 color = shade == "blue" ? new Color32(126, 212, 223, 255) : new Color32(255, 255, 255, 255);
            // The native liquid tint is still applied by the game's real and drag-preview layers.
            if (highlight)
                pixels[(bottom + fill - 1) * width + width - 3] = new Color32(245, 225, 178, 255);
            else
                for (int y = bottom; y < bottom + fill; y++)
                {
                    pixels[y * width + width - 4] = new Color32(157, 143, 114, 255);
                    pixels[y * width + width - 3] = color;
                    pixels[y * width + width - 2] = color;
                }
            Texture2D art = new Texture2D(width, height, TextureFormat.RGBA32, false);
            art.name = "jinsong_poc.water_marker." + stock.name;
            art.wrapMode = TextureWrapMode.Clamp;
            art.SetPixels32(pixels);
            art.Apply(false, false);
            UnityEngine.Object.DontDestroyOnLoad(art);
            return CreateLike(stock, art);
        }

        internal static void Dispose()
        {
            foreach (Dictionary<Texture2D, Sprite> variants in Icons.Values)
                foreach (Sprite sprite in variants.Values) UnityEngine.Object.Destroy(sprite);
            foreach (Sprite sprite in FillMarkers.Values)
            {
                UnityEngine.Object.Destroy(sprite.texture);
                UnityEngine.Object.Destroy(sprite);
            }
            Icons.Clear();
            NativeByReplacement.Clear();
            FillMarkers.Clear();
            UnbindItemDetails();
        }

        internal static void BindItemDetails()
        {
            if (Mod.Assets == null || Mod.Assets.HarmonyInstance == null || ModLoader.Mods == null) return;
            LoadedMod details = ModLoader.Mods.Find(mod => mod.Manifest.ID == "item_details" && mod.IsEnabled && mod.Assembly != null);
            if (details == null) return;

            if (details.Assembly != itemDetailsAssembly) UnbindItemDetails();
            itemDetailsAssembly = details.Assembly;
            Harmony harmony = Mod.Assets.HarmonyInstance;
            HarmonyMethod name = new HarmonyMethod(typeof(ShopGoods), "ItemDetailsName");
            HarmonyMethod tooltip = new HarmonyMethod(typeof(TooltipText), "Transpiler");
            PatchItemDetails(harmony, "GameRead", "ItemName", name, null, typeof(GameItem));
            PatchItemDetails(harmony, "GameRead", "ItemDisplayName", name, null, typeof(GameItem));
            PatchItemDetails(harmony, "ContainerItemSignature", "DisplayName", name, null, typeof(GameItem));
            PatchItemDetails(harmony, "TooltipPipeline", "AddLinePrefix", null, tooltip,
                typeof(string).MakeByRefType(), typeof(RenderHandler.ColorPalette).MakeByRefType(),
                typeof(bool).MakeByRefType(), typeof(RichTextBuilder));
            PatchItemDetails(harmony, "TooltipPipeline", "NoteDescription", null, tooltip, typeof(string), typeof(GameItem));
        }

        private static void PatchItemDetails(Harmony harmony, string typeName, string methodName,
            HarmonyMethod postfix, HarmonyMethod transpiler, params Type[] parameters)
        {
            MethodInfo method;
            try { method = ModReflect.Target("ItemDetails." + typeName, methodName, parameters) as MethodInfo; }
            catch (ModReflectAccessException) { return; }
            if (method == null || !method.IsStatic || method.ReturnType != (postfix == null ? typeof(bool) : typeof(string))
                || ItemDetailsPatches.Contains(method)) return;
            harmony.Patch(method, postfix: postfix, transpiler: transpiler);
            ItemDetailsPatches.Add(method);
        }

        private static void UnbindItemDetails()
        {
            Harmony harmony = Mod.Assets == null ? null : Mod.Assets.HarmonyInstance;
            if (harmony != null)
                foreach (MethodBase method in ItemDetailsPatches)
                    harmony.Unpatch(method, HarmonyPatchType.All, harmony.Id);
            ItemDetailsPatches.Clear();
            itemDetailsAssembly = null;
        }

        private static void ItemDetailsName(GameItem __0, ref string __result)
        {
            if (__0 != null && !GeneralHelper.HasCustomName(__0)) __result = Text(__0, "_name") ?? __result;
        }

        [HarmonyPatch(typeof(GameItemElement), "ResolveItemSprite")]
        private static class DisplaySprite
        {
            private static void Postfix(GameItemElement __instance, ref Sprite __result)
            {
                Sprite native = CanonicalMain(__instance, __result);
                Texture2D art;
                if (native != null && TryGetIcon(__instance, native, out art)) __result = GetOrCreate(native, art);
            }
        }

        [HarmonyPatch(typeof(GameItemElement), "ValidateSelectLocked")]
        private static class VisibleInventorySprite
        {
            private static void Postfix(GameItemElement __instance)
            {
                // Native factories set the Sprite before assigning the final identifier. Their first
                // validation consumes spriteChanged while Find must still reject the incomplete item.
                // Inventory validation runs again after identity is complete, even when that flag is false.
                if (__instance.background != null) Apply(__instance, __instance.background.image);
                if (__instance.fakeBackground != null) Apply(__instance, __instance.fakeBackground.image);
            }

            private static void Apply(GameItem item, UnityEngine.UI.Image image)
            {
                if (image == null) return;
                Sprite native = CanonicalMain(item, image.sprite);
                Texture2D art;
                if (native == null || !TryGetIcon(item, native, out art)) return;
                // Native owns inventory geometry; replacement retains size, pivot, PPU and border.
                Sprite replacement = GetOrCreate(native, art);
                if (image.sprite != replacement) image.sprite = replacement;
            }
        }

        [HarmonyPatch(typeof(GameItemElement), "UpdateInnateModSprite")]
        private static class ChargerLayerSprite
        {
            private static void Prefix(GameItemElement __instance, ref Sprite __1)
            {
                // The native charger loads layers directly from atlases, without calling the
                // contained battery's ResolveItemSprite. Replace only verified native layers.
                if (!Mod.Active || Mod.Assets == null || __1 == null || __instance.useSpriteFromMod
                    || __instance.identifier != "recharger" || __instance.spriteAtlasPath != "Items/items_tool") return;
                Texture2D art = null;
                string key = __1.name;
                if (ChargerParts.Contains(key))
                {
                    if (__1 != RenderHandler.LoadFromAtlas("Items/items_tool", key)
                        || !Mod.Assets.Textures.TryGetValue(ArtPrefix + key, out art)) return;
                }
                else
                {
                    foreach (GameItem child in __instance.FindAllChildItems())
                    {
                        if (child.spritePath != key || Find(child) == null
                            || __1 != RenderHandler.LoadFromAtlas(child.spriteAtlasPath, key)) continue;
                        Texture2D candidate;
                        if (!TryGetIcon(child, __1, out candidate)) continue;
                        // A shared native layer does not identify which owning child it depicts.
                        if (art != null && art != candidate) return;
                        art = candidate;
                    }
                }
                if (art == null || __1.rect.width != art.width || __1.rect.height != art.height) return;
                __1 = GetOrCreate(__1, art);
            }
        }

        [HarmonyPatch(typeof(GameItemElement), "UpdateModSprite")]
        private static class WeaponLayerSprite
        {
            private static void Prefix(GameItemElement __instance, ref Sprite __1)
            {
                if (!Mod.Active || Mod.Assets == null || __1 == null || !WeaponParents.Contains(__instance.identifier)
                    || Find(__instance) == null) return;
                Texture2D art = null;
                foreach (GameItem child in __instance.FindAllChildItems())
                {
                    if (Find(child) == null) continue;
                    foreach (WeaponLayer layer in WeaponLayers)
                    {
                        if (child.identifier != layer.Identifier || __1.name != layer.NativeSprite
                            || __1 != RenderHandler.LoadFromAtlas("Items/items_mod", layer.NativeSprite)) continue;
                        Texture2D candidate;
                        if (!Mod.Assets.Textures.TryGetValue(layer.ArtKey, out candidate) || candidate == null) continue;
                        // A shared native layer must have one unambiguous cosmetic owner.
                        if (art != null && art != candidate) return;
                        art = candidate;
                    }
                }
                if (art != null && __1.rect.width == art.width && __1.rect.height == art.height)
                    __1 = GetOrCreate(__1, art);
            }
        }

        [HarmonyPatch(typeof(PowerHelper), "CreatePowerSourceItemTooltip")]
        private static class PowerTooltip
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo message = AccessTools.Method(typeof(PowerTooltip), "Disposable");
                foreach (CodeInstruction instruction in instructions)
                {
                    // CIL string operands identify ldstr; avoid an unnecessary OpCode comparison.
                    if ((instruction.operand as string) == "Disposable: cannot be recharged")
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = message;
                    }
                    yield return instruction;
                }
            }

            private static string Disposable()
            {
                return Mod.Chinese ? Mod.Text("power_disposable_nocharge") : "Disposable: cannot be recharged";
            }
        }

        [HarmonyPatch(typeof(GameItemElement), "UpdateInnateModSprite")]
        private static class LiquidMarker
        {
            private static void Prefix(GameItemElement __instance, ref Sprite __1)
            {
                if (__instance.spriteAtlasPath != WaterAtlas && __instance.spriteAtlasPath != "Items/water_container4") return;
                Texture2D art;
                if (!TryGetIcon(__instance, __1, out art)) return;
                string[] parts = __1.name.Split('_');
                int percent;
                if (parts.Length != 3 || parts[0] != __instance.spritePath
                    || !Int32.TryParse(parts[2], out percent) || percent <= 0 || percent > 100) return;
                string shade = parts[1];
                if (shade != "blue" && shade != "red" && shade != "dark" && shade != "hl") return;
                Sprite replacement;
                if (!FillMarkers.TryGetValue(__1, out replacement))
                {
                    replacement = CreateFillMarker(__1, percent, shade);
                    FillMarkers.Add(__1, replacement);
                }
                __1 = replacement;
            }
        }

        [HarmonyPatch(typeof(GameItem), "GetDisplayName", new Type[] { typeof(bool) })]
        private static class DisplayName
        {
            private static void Postfix(GameItem __instance, bool __0, ref string __result)
            {
                if (__0 && GeneralHelper.HasCustomName(__instance)) return;
                __result = Text(__instance, "_name") ?? __result;
            }
        }

        [HarmonyPatch(typeof(GameItemElement), "GetTooltipBasic")]
        private static class TooltipText
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo shortDescription = AccessTools.PropertyGetter(typeof(GameItem), "shortDescription");
                MethodInfo flavorText = AccessTools.PropertyGetter(typeof(GameItem), "flavorText");
                MethodInfo displayDescription = AccessTools.Method(typeof(TooltipText), "Description");
                MethodInfo displayFlavor = AccessTools.Method(typeof(TooltipText), "Flavor");
                // Redirect the tooltip reads. The save encoder still reads the original item fields.
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.Calls(shortDescription))
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = displayDescription;
                    }
                    else if (instruction.Calls(flavorText))
                    {
                        instruction.opcode = OpCodes.Call;
                        instruction.operand = displayFlavor;
                    }
                    yield return instruction;
                }
            }

            private static string Description(GameItem item)
            {
                return OrganizationText.Display(Text(item, "_desc") ?? item.shortDescription);
            }

            private static string Flavor(GameItem item)
            {
                return OrganizationText.Display(Text(item, "_flavor") ?? item.flavorText);
            }
        }

        [HarmonyPatch(typeof(MachinePurifier), "CreateMachineInventoryWindow")]
        private static class PurifierPanelText
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo original = AccessTools.Method(typeof(LocHelper), "GetLocalizedMechanic");
                MethodInfo replacement = AccessTools.Method(typeof(PurifierPanelText), "Localize");
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.Calls(original)) instruction.operand = replacement;
                    yield return instruction;
                }
            }

            private static string Localize(string key, object[] args)
            {
                return (Mod.Chinese ? Mod.Text(key, args) : null) ?? LocHelper.GetLocalizedMechanic(key, args);
            }
        }

        [HarmonyPatch(typeof(MachineRecharger), "CreateMachineInventoryWindow")]
        private static class ChargerPanelText
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo original = AccessTools.Method(typeof(LocHelper), "GetLocalizedMechanic");
                MethodInfo replacement = AccessTools.Method(typeof(PurifierPanelText), "Localize");
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.Calls(original)) instruction.operand = replacement;
                    yield return instruction;
                }
            }
        }

        [HarmonyPatch(typeof(MachinePurifier), "CreateTooltip")]
        private static class PurifierTooltipText
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo mechanic = AccessTools.Method(typeof(LocHelper), "GetLocalizedMechanic");
                MethodInfo mechanicOrDefault = AccessTools.Method(typeof(LocHelper), "GetLocalizedMechanicOrDefault");
                MethodInfo localize = AccessTools.Method(typeof(PurifierTooltipText), "Localize");
                MethodInfo localizeOrDefault = AccessTools.Method(typeof(PurifierTooltipText), "LocalizeOrDefault");
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.Calls(mechanic) || instruction.Calls(mechanicOrDefault))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1).MoveLabelsFrom(instruction);
                        instruction.operand = instruction.Calls(mechanic) ? localize : localizeOrDefault;
                    }
                    yield return instruction;
                }
            }

            private static string Localize(string key, object[] args, GameItem item)
            {
                return (Mod.Chinese && Find(item) != null ? Mod.Text(key, args) : null)
                    ?? LocHelper.GetLocalizedMechanic(key, args);
            }

            private static string LocalizeOrDefault(string key, string fallback, object[] args, GameItem item)
            {
                return (Mod.Chinese && Find(item) != null ? Mod.Text(key, args) : null)
                    ?? LocHelper.GetLocalizedMechanicOrDefault(key, fallback, args);
            }
        }

        [HarmonyPatch(typeof(WaterHelper), "CreateWaterContainerTooltip")]
        private static class LiquidTooltipText
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo estimate = AccessTools.Method(typeof(WaterHelper), "GetEstimatedVolume");
                MethodInfo localize = AccessTools.Method(typeof(LiquidTooltipText), "Localize");
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;
                    // Keep the numeric ml line intact so Item Details can recognize and format it.
                    if (instruction.Calls(estimate) || instruction.operand as string == ", Capacity:")
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Call, localize);
                    }
                }
            }

            private static string Localize(string original, GameItem item)
            {
                if (!Mod.Chinese || Find(item) == null) return original;
                string key;
                switch (original)
                {
                    case "Empty": key = "liquid_empty"; break;
                    case "Almost Empty": key = "liquid_almost_empty"; break;
                    case "Less Than Half Full": key = "liquid_less_than_half"; break;
                    case "Half Full": key = "liquid_half"; break;
                    case "More Than Half Full": key = "liquid_more_than_half"; break;
                    case "Almost Full": key = "liquid_almost_full"; break;
                    case "Full": key = "liquid_full"; break;
                    case ", Capacity:": key = "liquid_capacity"; break;
                    default: return original;
                }
                return Mod.Text(key) ?? original;
            }
        }
    }
}
