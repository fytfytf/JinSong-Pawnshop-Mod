using System;
using System.Collections.Generic;
using UnityEngine;

namespace JinSongPoc
{
    internal static class RosterPortraits
    {
        internal static readonly string[] WantedKeys = {
            "wanted3", "wanted4", "wanted5", "wanted6", "wanted7", "wanted8", "wanted9"
        };

        private static readonly Dictionary<string, string> Named = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "mentor", "mentor" }, { "sawyer", "sawyer" }, { "joe", "joe" }, { "landlord", "landlord" },
            { "billy", "billy" }, { "officer_lun", "officer_lun" }, { "inventor", "inventor" },
            { "cartel_handler", "cartel_handler" }, { "courrier", "courrier" }, { "admin", "clerk" },
            { "shadyMerchant", "shady_merchant" }, { "blackmailer", "blackmailer" }, { "greytide", "greytide" },
            { "george", "george" }, { "grandpa_cheng", "grandpa_cheng" }, { "nellis", "nellis" },
            { "spy", "spy" }, { "waffle_enjoyer", "foodie" }, { "case_enjoyer", "collector" },
            { "eleanor1", "eleanor" }, { "eleanor2", "eleanor_plain" }, { "eleanor3", "eleanor_sad" },
            { "eleanor_scav", "eleanor_scav" }, { "eleanor_rev", "eleanor_rev" },
            { "serpico", "serpico" }, { "serpico_scav", "serpico_scav" }, { "serpico_rev", "serpico_rev" },
            { "lou_sec", "guard_f" }, { "sec", "official" }, { "merchant", "merchant" }, { "scavElder", "elder" },
            { "retired_farmer", "elder" }, { "retired_chemist", "physician" }, { "retired_aquafarmer", "farmer" },
            { "wanted_bait1", "townsman" }, { "cul1", "visitor" }, { "sha3", "broker_m" },
            { "trailer_rev1", "rebel_m" }, { "trailer_rev2", "rebel_f" },
            { "trailer_scav1", "scavenger" }, { "trailer_scav2", "scavenger_woman" },
            { "trailer_scav3", "townsman" }, { "trailer_scav4", "scavenger" }, { "trailer_scav5", "scavenger_woman" },
            { "augLLMale1", "clockwork_lower_m1" }, { "augLLMale2", "clockwork_lower_m2" },
            { "augLLMale3", "clockwork_lower_m3" },
            { "augLLFemale1", "clockwork_lower_f1" }, { "augLLFemale2", "clockwork_lower_f2" },
            { "augLLFemale3", "clockwork_lower_f3" },
            { "augBMMale1", "clockwork_broker_m1" }, { "augBMMale2", "clockwork_broker_m2" },
            { "augBMMale3", "clockwork_broker_m3" },
            { "augBMFemale1", "clockwork_broker_f1" }, { "augBMFemale2", "clockwork_broker_f2" },
            { "augBMFemale3", "clockwork_broker_f3" },
            { "augULMale1", "clockwork_upper_m1" }, { "augULMale2", "clockwork_upper_m2" },
            { "augULMale3", "clockwork_upper_m3" },
            { "augULFemale1", "clockwork_upper_f1" }, { "augULFemale2", "clockwork_upper_f2" },
            { "augULFemale3", "clockwork_upper_f3" },
        };

        // These are the keys passed to SpriteDict, including deliberately mismatched disguises.
        private static readonly Dictionary<string, string> Pools = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "maleLower", "townsman" }, { "femaleLower", "townswoman" },
            { "maleUpper", "visitor" }, { "femaleUpper", "visitor_woman" },
            { "maleScav", "scavenger" }, { "femaleScav", "scavenger_woman" },
            { "maleEng", "repairman" }, { "femaleEng", "repairwoman" },
            { "maleChef", "cook" }, { "femaleChef", "cook_woman" },
            { "maleSci", "physician" }, { "femaleSci", "healer" },
            { "maleMed", "physician" }, { "femaleMed", "healer" },
            { "maleFarmer", "farmer" }, { "femaleFarmer", "farmer_woman" },
            { "maleBM", "broker_m" }, { "femaleBM", "broker_f" },
            { "maleRev", "rebel_m" }, { "femaleRev", "rebel_f" },
            { "maleMerc", "merc_m" }, { "femaleMerc", "merc_f" },
            { "maleSec", "official" }, { "femaleSec", "guard_f" },
            { "maleSecCorpo", "merc_m" }, { "femaleSecCorpo", "merc_f" },
            { "maleSecOff", "merc_m" }, { "femaleSecOff", "merc_f" },
            { "sec", "official" }, { "miner", "miner" },
        };

        private static readonly Dictionary<string, Sprite> Portraits = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        internal static void Load(LoadedMod assets, List<Texture2D> textures)
        {
            HashSet<string> keys = new HashSet<string>(Named.Values, StringComparer.Ordinal);
            keys.UnionWith(Pools.Values);
            keys.Add("foreign_courier");
            foreach (string key in keys)
            {
                Texture2D source, texture;
                if (!assets.Textures.TryGetValue("jin_npc_" + key, out source)) continue;
                // The mentor artwork is framed as a close-up. Keep it centered and bottom-aligned.
                float figureScale = key == "mentor" ? .70f : .90f;
                Portraits[key] = Visuals.MakeSprite(source, 288, 211, "npc." + key, out texture, figureScale: figureScale);
                textures.Add(texture);
            }
        }

        internal static string WantedKey(StoreClient client)
        {
            return client != null && Array.IndexOf(WantedKeys, client.spriteName) >= 0 ? client.spriteName : null;
        }

        internal static Sprite Get(StoreClient client, Sprite official)
        {
            if (client == null || client.isWanted || !String.IsNullOrEmpty(client.wantedPosterId)
                || String.IsNullOrEmpty(client.spriteName)) return null;
            string stock = client.spriteName;
            string key;
            if (stock == "maleBM3" && (client.identifier == "courrierChem" || client.identifier == "courrierChemFree"))
                key = "foreign_courier";
            else if (!Named.TryGetValue(stock, out key))
            {
                int end = stock.Length;
                while (end > 0 && stock[end - 1] >= '0' && stock[end - 1] <= '9') end--;
                if (end == stock.Length || !Pools.TryGetValue(stock.Substring(0, end), out key)) return null;
            }
            if (key == "official") return official;
            Sprite sprite;
            return Portraits.TryGetValue(key, out sprite) ? sprite : OrdinaryPortraits.GetArt(key);
        }

        internal static void Dispose()
        {
            foreach (Sprite sprite in Portraits.Values) UnityEngine.Object.Destroy(sprite);
            Portraits.Clear();
        }
    }
}
