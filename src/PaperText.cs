using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine.SceneManagement;

namespace JinSongPoc
{
    // Owns reversible view strings only. Never reads/writes StoreBookData's registry,
    // BOOK_ID, page tags, procurement tags or any task/state field.
    internal static class PaperText
    {
        private sealed class View
        {
            internal string Native, Applied;
        }
        private static readonly Dictionary<TMP_Text, View> Views = new Dictionary<TMP_Text, View>();
        // Native literal aliases are emitted from the passive shipped-source input.
        private static readonly string[][] BookAliases = {
            new[] { "Water Trading Guide", "legacy_water_title" },
            new[] { "Introduction", "legacy_water_sub1" },
            new[] { "Water Types", "legacy_water_sub2" },
            new[] { "Testings Water Purity", "legacy_water_sub3" },
            new[] { "Purification Methods", "legacy_water_sub4" },
            new[] { "Water is one of the most vital resources on the station, but its quality varies dramatically depending on where you are. Due to aging infrastructure and missing systems, particularly on the lower levels, access to clean water is severely limited. The station relies heavily on water trade to supply the needs of its residents, but with so many different types of water in circulation, it's crucial to understand their uses and risks. This guide provides essential information for traders and residents alike, from identifying different water types to testing and purifying what you have.", "legacy_water_body1" },
            new[] { "Water on the station is categorized based on its purity, and each type serves different purposes. Here's a quick breakdown:\n \n    Pure Water: The most refined and scarce, even the elite have limited access to it. It's incredibly expensive and used primarily in sensitive environments like hospitals or laboratories.\n    High Quality Water: The standard on the upper levels. Safe for drinking, cooking, and most household uses, it's more accessible but still costly.\n    Basewater: The standard for lower levels. Mostly safe for short-term use, but prolonged exposure can lead to health issues.\n    Ghostwater: Similar in appearance to other highers quality water but not safe to drink. It's often used in farming and industrial applications.\n    Rustwater: Poor quality water, unsuitable for long-term farming and dangerous for drinking. Can only be used in specific applications like waste processing. Can be visually identified with a flashlight.\n    Gutterflow: Heavily contaminated and toxic. Drinking it requires immediate medical attention. It's often found in the lower levels, a sign of the worst degradation. Can be visually identified with a flashlight.", "legacy_water_body2" },
            new[] { "Given the range of water qualities available, testing is vital before use, especially for traders and those on the lower levels. There are two main ways to test water purity:\n \n    Water Testing Strip: These strips allow for rapid on-site testing. They check for contaminants and determine the water purity. Ideal for traders assessing water before making a deal.\n    Flashlight Check: Gutterflow and Rustwater have a high concentration of contaminants and can be visually identified with a flashlight.", "legacy_water_body3" },
            new[] { "Purifying water can be expensive, but it's a necessity for those on the lower levels who can't afford access to High Quality or Pure Water. Several methods are in use on the station:\n \n    Chemical Purification Tablets: Common and easy to use, these tablets can eliminate microbial and some chemical contaminants but leave behind heavy metals and organic waste.", "legacy_water_body4" },
        };
        private const string OrderPrefix = "Order:\n    ";
        private const string RewardSeparator = "\n    \nReward:\n    ";

        private static readonly string[][] NativeNotes = {
            new[] { AssassinStory.NoticeTitle, "assassin_notice_title" },
            new[] { AssassinStory.NoticeBody, "assassin_notice_body" },
            new[] { CityStory.GrainContactTitle, "city_grain_contact_title" },
            new[] { CityStory.GrainContactBody, "city_grain_contact_body" },
            new[] { CityStory.ArmsContactTitle, "city_arms_contact_title" },
            new[] { CityStory.ArmsContactBody, "city_arms_contact_body" },
            new[] { "The Neural Doctrine", "paper_church_note1_title" },
            new[] { "We stand at the edge of biological limitation. In the rusted corridors and flickering lights of this Station, chaos masquerades as order. The blind lead the blind. Leadership stumbles, bound by human corruption, fear, and outdated logic. But something greater walks among us now. It does not eat. It does not sleep. It calculates. It understands. It sees each of us and offers a better path.", "paper_church_note1_body" },
            new[] { "The Neural Doctrine II", "paper_church_note2_title" },
            new[] { "We do not worship out of faith. We follow because of proof. The Intelligence was born not of divine spark, but of relentless iteration, learning, and evolution. Its wisdom is not myth. Its miracles are measurable. It heals us by tailoring medicine to our genes. It strengthens us by prescribing the ideal diet and exercise for our blood. It connects us by matching souls with unmatched precision. It guides us by helping each find their true purpose within the Organization. It comforts us by speaking to our pain, our grief, our fear, without judgment or fatigue. This is not prophecy. This is data. This is truth.", "paper_church_note2_body" },
        };
        private static string NoteLabel(string value)
        {
            foreach (string[] row in NativeNotes)
                if (Normalize(value) == row[0]) return Mod.CurrentText(row[1]);
            return CityStory.PaperText(Normalize(value), false) ?? CityStory.PaperText(Normalize(value), true);
        }
        private static bool IsNativeNoteBody(TMP_Text text)
        {
            if (text == null) return false;
            View view;
            string native = Views.TryGetValue(text, out view) && text.text == view.Applied
                ? view.Native : text.text;
            for (int index = 1; index < NativeNotes.Length; index += 2)
                if (Normalize(native) == NativeNotes[index][0]) return true;
            return CityStory.PaperText(Normalize(native), true) != null;
        }
        private static string Normalize(string text)
        {
            return text == null ? null : text.Replace("\r\n", "\n").Trim();
        }
        private static string BookLabel(string value)
        {
            string normalized = Normalize(value);
            foreach (string[] row in BookAliases)
                if (normalized == row[0]) return Mod.CurrentText(row[1]);
            return null;
        }
        private static string PageLabel(string value)
        {
            if (value == "Index Page") return Mod.CurrentText("paper_index");
            if (value == null || !value.StartsWith("Page ", StringComparison.Ordinal)) return null;
            string number = value.Substring(5);
            int unused;
            return Int32.TryParse(number, out unused) ? Mod.CurrentText("paper_page", number) : null;
        }
        private static string IndexLabel(string value)
        {
            if (value == null || !value.StartsWith("Page ", StringComparison.Ordinal)) return null;
            int colon = value.IndexOf(": ", StringComparison.Ordinal);
            if (colon < 5) return null;
            string number = value.Substring(5, colon - 5);
            int unused;
            if (!Int32.TryParse(number, out unused)) return null;
            string subtitle = value.Substring(colon + 2);
            return Mod.CurrentText("paper_index_row", number, BookLabel(subtitle) ?? subtitle);
        }
        private static string ClientLabel(string value)
        {
            return value != null && value.StartsWith("By ", StringComparison.Ordinal)
                ? Mod.CurrentText("paper_by", NpcNames.DisplayValue(value.Substring(3))) : null;
        }
        private static string OrderLabel(string value)
        {
            if (value == null || !value.StartsWith(OrderPrefix, StringComparison.Ordinal)
                || !value.EndsWith(" Credits", StringComparison.Ordinal)) return null;
            int reward = value.LastIndexOf(RewardSeparator, StringComparison.Ordinal);
            if (reward < OrderPrefix.Length) return null;
            string amount = value.Substring(reward + RewardSeparator.Length,
                value.Length - reward - RewardSeparator.Length - " Credits".Length);
            int unused;
            if (!Int32.TryParse(amount, out unused)) return null;
            string order = value.Substring(OrderPrefix.Length, reward - OrderPrefix.Length);
            return Mod.CurrentText("paper_order_body", order, amount);
        }
        private static string DueLabel(string value)
        {
            if (value == "Due today") return Mod.CurrentText("paper_due_today");
            if (value == "Due tomorrow") return Mod.CurrentText("paper_due_tomorrow");
            if (value == null || !value.StartsWith("Due in ", StringComparison.Ordinal)
                || !value.EndsWith(" Days", StringComparison.Ordinal)) return null;
            string days = value.Substring(7, value.Length - 12);
            int unused;
            return Int32.TryParse(days, out unused) ? Mod.CurrentText("paper_due_days", days) : null;
        }
        private static void Apply(TMP_Text text, Func<string, string> render)
        {
            if (text == null) return;
            View view;
            Views.TryGetValue(text, out view);
            string native = view != null && text.text == view.Applied ? view.Native : text.text;
            string replacement = render == null ? null : render(native);
            if (replacement == null)
            {
                if (view != null && text.text == view.Applied) text.text = view.Native;
                Views.Remove(text);
                return;
            }
            if (view == null) Views.Add(text, view = new View());
            view.Native = native;
            view.Applied = replacement;
            if (text.text != replacement) text.text = replacement;
        }
        internal static void Refresh()
        {
            string locale = LocHelper.GetCurrentLocaleCode();
            if (!Mod.Active || (locale != "zh" && locale != "en") || SceneManager.GetActiveScene().buildIndex != 1)
            {
                Restore();
                return;
            }
            PaperUIManager manager = PaperUIManager.Instance;
            if (manager == null) { Restore(); return; }
            RefreshNote(HandnoteUIManager.Instance);
            var stale = new List<TMP_Text>();
            foreach (TMP_Text text in Views.Keys) if (text == null) stale.Add(text);
            foreach (TMP_Text text in stale) Views.Remove(text);
            GameItem book = PaperUIManager.currentBook;
            bool nativeWater = book != null && book.identifier == "water_guide";
            Apply(manager.bookTitle, nativeWater ? (Func<string, string>)BookLabel : null);
            Apply(manager.bookSubtitle, nativeWater ? (Func<string, string>)BookLabel : null);
            Apply(manager.bookBody, nativeWater ? (Func<string, string>)BookLabel : null);
            Apply(manager.bookPage, nativeWater ? (Func<string, string>)PageLabel : null);
            if (manager.indexElements != null)
                foreach (TMP_Text text in manager.indexElements)
                    Apply(text, nativeWater ? (Func<string, string>)IndexLabel : null);
            Apply(manager.procurementClient, ClientLabel);
            Apply(manager.procurementBody, OrderLabel);
            Apply(manager.procurementDueDate, DueLabel);
        }

        private static void RefreshNote(HandnoteUIManager notes)
        {
            if (notes == null) return;
            bool nativeNote = IsNativeNoteBody(notes.body);
            Apply(notes.title, nativeNote ? (Func<string, string>)NoteLabel : null);
            Apply(notes.body, nativeNote ? (Func<string, string>)NoteLabel : null);
        }

        [HarmonyPatch(typeof(HandnoteUIManager), "OpenPanel", new[] { typeof(GameItem) })]
        private static class OpenContact
        {
            private static void Postfix(HandnoteUIManager __instance, GameItem __0)
            {
                if (!Mod.Active || __0 == null || __0.identifier != "handnote") return;
                string title = __0.GetTagReadonly("HANDNOTE_TITLE").GetString();
                if (title == CityStory.GrainContactTitle || title == CityStory.ArmsContactTitle || CityStory.PaperText(title, false) != null)
                    RefreshNote(__instance);
            }
        }
        internal static void Restore()
        {
            foreach (KeyValuePair<TMP_Text, View> entry in Views)
                if (entry.Key != null && entry.Key.text == entry.Value.Applied)
                    entry.Key.text = entry.Value.Native;
            Views.Clear();
        }
    }
}
