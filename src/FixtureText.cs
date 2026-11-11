using HarmonyLib;
using TMPro;
using UnityEngine.SceneManagement;

namespace JinSongPoc
{
    internal static class FixtureText
    {
        private const string WaterTipStock = "A water testing strip is a cheap and easy way to test the water's purity.";
        private static TMP_Text waterTip;
        private static string waterTipApplied;

        internal static void RefreshGuideTip()
        {
            MultiPageUIManager pages = MultiPageUIManager.Instance;
            if (!ChineseShop || pages == null || pages.books == null || pages.books.Count <= 1)
            {
                RestoreGuideTip();
                return;
            }
            UnityEngine.GameObject book = pages.books[1];
            if (book == null || book.name != "BookWater") return;
            UnityEngine.Transform node = book.transform.Find("Page2/Section2/Text (TMP) (4)");
            TMP_Text text = node == null ? null : node.GetComponent<TMP_Text>();
            if (text == null) return;
            if (waterTip != text) RestoreGuideTip();
            if (text.text != WaterTipStock && !(waterTip == text && text.text == waterTipApplied)) return;
            string replacement = Mod.Text("fixture_ui_book_water_s2_body2");
            if (replacement == null) return;
            waterTip = text;
            waterTipApplied = replacement;
            if (text.text != replacement) text.text = replacement;
        }

        internal static void RestoreGuideTip()
        {
            if (waterTip != null && waterTip.text == waterTipApplied) waterTip.text = WaterTipStock;
            waterTip = null;
            waterTipApplied = null;
        }

        private static bool ChineseShop
        {
            get { return Mod.Chinese && SceneManager.GetActiveScene().buildIndex == 1; }
        }

        internal static string LocalizedValue(string table, string key, params object[] args)
        {
            if (!ChineseShop || key == null) return null;
            string prefix;
            switch (table)
            {
                case "UI": prefix = "fixture_ui_"; break;
                case "Interactable": prefix = "fixture_interactable_"; break;
                case "Item": prefix = "guide_"; break;
                case "Mechanic": prefix = "ending_"; break;
                default: return null;
            }
            return Mod.Text(prefix + key, args);
        }

        [HarmonyPatch(typeof(StoreNotebook), "GetTooltip")]
        private static class NotebookTooltip
        {
            private static void Postfix(ref string __result)
            {
                if (ChineseShop) __result = Mod.Text("fixture_notebook") ?? __result;
            }
        }

        [HarmonyPatch(typeof(GameItem), "GetDisplayName")]
        private static class FixtureItemNames
        {
            private static void Postfix(GameItem __instance, bool __0, ref string __result)
            {
                EmporiumEntry entry = EmporiumEntry.Instance;
                if (entry == null) return;
                string key;
                if (__instance == entry.trashcan) key = "fixture_waste";
                else if (__instance == entry.cassettePlayer) key = "fixture_tunes";
                else if (entry.docInvElement != null && __instance == entry.docInvElement.childItem) key = "fixture_archive";
                else return;
                if (ChineseShop && !(__0 && GeneralHelper.HasCustomName(__instance))) __result = Mod.Text(key) ?? __result;
            }
        }
    }
}
