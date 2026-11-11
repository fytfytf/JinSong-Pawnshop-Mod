using System;
using System.Collections.Generic;
using UnityEngine;

namespace JinSongPoc
{
    internal static class OrdinaryPortraits
    {
        private struct Appearance
        {
            internal string FirstStock, FirstArt, SecondStock, SecondArt;

            internal string Resolve(string stock)
            {
                if (stock.StartsWith(FirstStock, StringComparison.Ordinal)) return FirstArt;
                if (SecondStock != null && stock.StartsWith(SecondStock, StringComparison.Ordinal)) return SecondArt;
                return null;
            }
        }

        private static readonly Dictionary<string, Appearance> Groups = BuildGroups();
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        internal static void Load(LoadedMod assets, List<Texture2D> textures)
        {
            HashSet<string> keys = new HashSet<string>();
            foreach (Appearance appearance in Groups.Values)
            {
                keys.Add(appearance.FirstArt);
                if (appearance.SecondArt != null) keys.Add(appearance.SecondArt);
            }
            foreach (string key in keys)
            {
                Texture2D source, texture;
                if (!assets.Textures.TryGetValue("jin_customer_" + key, out source)) continue;
                Sprites[key] = Visuals.MakeSprite(source, 288, 211, "customer." + key, out texture, figureScale: 0.90f);
                textures.Add(texture);
            }
        }

        internal static Sprite Get(StoreClient client)
        {
            if (client == null || client.isWanted || !String.IsNullOrEmpty(client.wantedPosterId)
                || String.IsNullOrEmpty(client.spriteName) || String.IsNullOrEmpty(client.identifier)) return null;
            Appearance appearance;
            if (!Groups.TryGetValue(client.identifier, out appearance)) return null;
            return GetArt(appearance.Resolve(client.spriteName));
        }

        internal static Sprite GetArt(string key)
        {
            Sprite sprite;
            return key != null && Sprites.TryGetValue(key, out sprite) ? sprite : null;
        }

        internal static void Dispose()
        {
            foreach (Sprite sprite in Sprites.Values) UnityEngine.Object.Destroy(sprite);
            Sprites.Clear();
        }

        private static Dictionary<string, Appearance> BuildGroups()
        {
            // Use the native role and appearance together because stock sprite pools are shared by unrelated clients.
            Dictionary<string, Appearance> groups = new Dictionary<string, Appearance>(StringComparer.Ordinal);
            Add(groups, "maleLower", "townsman", "femaleLower", "townswoman",
                "thirstySpacer", "scavWaterShopper", "waterSpace2", "spacerFoodI",
                "hungrySpacer", "expeditionSupplier", "spacerMedical", "spacerHouseholdItem",
                "youngSpacerHousehold", "citizenHousehold", "upperMaid", "sickLowers",
                "spacerFoodVariety", "spacerAmenityVariety", "lower_level_flexi_buyer", "cluelessTouristBuyer",
                "touristJunkSouvenirBuyer", "foodBankBuyer", "lower_level_rare_merchant");
            Add(groups, "maleScav", "scavenger", "femaleScav", "scavenger_woman",
                "scavGeneral", "waterScavenger", "scrap_byer", "scavWaterMinor",
                "scavHaul", "peatMinorClient", "conspiracyMinor", "spacerFood",
                "junker", "junker2", "scavStamp", "machine_scrapper",
                "scavCrate");
            Add(groups, "maleEng", "repairman", "femaleEng", "repairwoman",
                "lowerRepairman", "maintenanceWorker", "stationEngineer", "stationEngineerMinor",
                "annoyedEngineer");
            Add(groups, "maleChef", "cook", "femaleChef", "cook_woman",
                "spacerChef", "privateChef");
            Add(groups, "maleSci", "physician", "femaleSci", "healer",
                "lowLevelDoctor", "clinicNurse", "nurseMinor", "nurseMinor2",
                "lowerLevelChemist", "rareLowerLevelChemist");
            Add(groups, "maleFarmer", "farmer", "femaleFarmer", "farmer_woman",
                "farmer_standard", "farmerMinor");
            Add(groups, "merchant", "merchant", null, null,
                "merchant", "waterSeller");
            Add(groups, "maleLower", "merchant", "femaleLower", "merchant",
                AssassinStory.FoodClient, AssassinStory.LootClient, AssassinStory.WeaponClient, AssassinStory.MedicineClient);
            Add(groups, "maleUpper", "visitor", "femaleUpper", "visitor_woman",
                "waterStockpiler", "hospitalAdmin", "upperLevelVisitorMinor", "upperLowerVisitor",
                "upperLowerVisitorLuxury", "brokeUpperLevel", "lotteryWinnerMinor");
            Add(groups, "scavElder", "elder", null, null,
                "lowerLevelElder", "OldScav1");
            Add(groups, "maleScav", "miner", null, null,
                "miner", "minerMinor");
            Add(groups, "maleScav", "townsman", "femaleScav", "household",
                "familyMother", "sickChildCaretaker");
            Add(groups, "femaleSci", "repairwoman", null, null,
                "lowerInventor");
            return groups;
        }

        private static void Add(Dictionary<string, Appearance> groups, string firstStock, string firstArt,
            string secondStock, string secondArt, params string[] roles)
        {
            Appearance appearance = new Appearance { FirstStock = firstStock, FirstArt = firstArt,
                SecondStock = secondStock, SecondArt = secondArt };
            foreach (string role in roles) groups.Add(role, appearance);
        }
    }
}
