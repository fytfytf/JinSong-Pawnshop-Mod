using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

namespace JinSongUiSurvey
{
    public sealed class ProbeMod : IMod
    {
        public void Init(ModManifest manifest) { }
        public void OnEnable()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey") < 0) return;
            GameObject host = new GameObject("JinSong temporary memory-only UI survey");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<Survey>();
        }
        public void OnDisable() { }
    }
    [HarmonyPatch(typeof(PlayerStore), "InitialSave")]
    internal static class NoInitialSave { private static bool Prefix() { return false; } }
    [HarmonyPatch(typeof(PlayerStore), "SaveGame")]
    internal static class NoSave { private static bool Prefix() { return false; } }
    [HarmonyPatch(typeof(PlayerStore), "WriteSlotFile")]
    internal static class NoSlotWrite { private static bool Prefix() { return false; } }
    [HarmonyPatch(typeof(PlayerStore), "PreLoadStore")]
    internal static class NoPreload
    {
        internal static int Blocked;
        private static bool Prefix(ref PlayerStore __result) { Blocked++; __result = null; return false; }
    }
    [HarmonyPatch(typeof(PlayerStore), "LoadGame")]
    internal static class NoLoad
    { internal static int Blocked; private static bool Prefix() { Blocked++; return false; } }
    [HarmonyPatch(typeof(EmporiumEntry), "Update")]
    internal static class QuietEntry { private static bool Prefix() { return false; } }

    public sealed class Survey : MonoBehaviour
    {
        private int failures, checks;
        private LoadedMod mod;
        private Locale originalLocale;
        private RichText details;
        private readonly List<string> runtimeErrors = new List<string>();
        private readonly List<StoreEvent> events = new List<StoreEvent>();
        private bool focused;
        private bool menuLanguage;
        private int originalLanguageIndex;
        private bool chromeOnly;
        private bool finishOnly;
        private bool reportedIssues;
        private bool alcoveOnly;
        private bool streetOnly;
        private bool polishOnly;
        private bool gridOnly;
        private bool marketOnly;
        private bool companyOnly;
        private bool archiveServicesOnly;
        private bool fixtureClicksOnly;
        private bool workshopOnly;
        private bool networkOnly;
        private bool assassinOnly;
        private bool workshopCompatibility;
        private GameItemElement alcoveItem;
        private void OnLog(string message, string stack, LogType type)
        {
            if ((type == LogType.Error || type == LogType.Exception) && runtimeErrors.Count < 40)
                runtimeErrors.Add(message + " | " + stack);
        }
        private void Check(bool valid, string message)
        {
            checks++;
            if (!valid) throw new InvalidOperationException(message);
            Debug.Log("[JINSONG_SURVEY] PASS " + message);
        }
        private bool Try(string phase, Action action)
        {
            try { action(); return true; }
            catch (Exception error) { failures++; Debug.Log("[JINSONG_SURVEY] FAILURE " + phase + " " + error); return false; }
        }
        private void LateUpdate()
        {
            if (details != null && OverlayHandler.current != null)
                OverlayHandler.current.ShowTooltip(Vector2.zero, details, 52);
        }
        private static string Plain(string text) { return Regex.Replace(text ?? "", "<[^>]+>|\\s+", ""); }
        private static int AssassinValue(string key)
        {
            return ModHelper.GetModDataInt("jinsong_poc", "assassin." + key, -1);
        }

        private static void SetAssassinTestPrice(GameItem item, int price)
        {
            // Native stolen-item modifiers can scale the unit value before cash settlement.
            long lower = 0;
            long upper = Math.Max(item.unitValue, price) * 10;
            while (lower <= upper)
            {
                long value = lower + (upper - lower) / 2;
                item.SetValue(value);
                long quoted = item.GetNegociatedValue();
                if (quoted == price) return;
                if (quoted < price) lower = value + 1;
                else upper = value - 1;
            }
            throw new InvalidOperationException("No test unit value could produce " + price + " for " + item.identifier);
        }

        private StoreClient ShowAssassinClient(string suffix)
        {
            PlayerStore store = PlayerStore.Instance;
            var queue = store.storeClientManager.clientStack;
            string id = "jinsong_assassin_" + suffix;
            int index = queue.FindIndex(c => c.identifier == id);
            Check(index >= 0 && queue.FindAll(c => c.identifier == id).Count == 1, "one native queue entry for " + suffix);
            StoreClient client = queue[index];
            queue.RemoveAt(index);
            queue.Insert(0, client);
            store.GetNextClient();
            store.isClientArrived = true;
            StoreUIManager.Instance.OnNextClientArrived();
            Check(store.currentClientInstance.GetClientBlueprint() == client, "native window received " + suffix);
            return client;
        }

        private void CheckAssassin(bool passes)
        {
            const string modId = "jinsong_poc";
            PlayerStore store = PlayerStore.Instance;
            StoreStation station = StoreStation.Instance;
            var manager = store.storeClientManager;
            var front = EmporiumEntry.Instance.frontInvinvElement;
            foreach (string key in new[] { "food_day", "food_passed", "notice_day", "notice_attempt_day", "loot_passed" })
                ModHelper.RemoveModData(modId, "assassin." + key);
            int firstDay = passes ? 17 : 27;
            station.dayCounter = firstDay;
            manager.GenerateClient();
            StoreClient foodClient = ShowAssassinClient("food");
            Check(foodClient.GetBudget() == 1000 && !foodClient.isWanted, "independent buyer with fixed 1000 budget");
            var food = DirectoryMaster.Item("nutrifruit", true);
            int foodPrice = passes ? 660 : 659;
            food.SetValue(food.unitValue + foodPrice - food.GetNegociatedValue());
            Check(food.GetNegociatedValue() == foodPrice && foodClient.IsClientBuyingThisItem(food), "qualified food priced at " + foodPrice);
            int cash = store.playerCash;
            store.SellItem(food);
            Check(store.playerCash - cash == foodPrice && AssassinValue("food_passed") == -1, "native food sale pays cash and waits for departure");
            store.DismissCurrentClient(false, false, true);
            Check(AssassinValue("food_day") == firstDay && AssassinValue("food_passed") == (passes ? 1 : 0), "food result committed on departure at " + foodPrice);
            manager.GenerateClient();
            Check(!manager.clientStack.Exists(c => c.identifier == "jinsong_assassin_notice"), "no guard on the day of food departure");

            station.dayCounter = firstDay + 1;
            manager.GenerateClient();
            var originalWanted = new Dictionary<string, int>(store.wantedStatus);
            StoreClient guard = ShowAssassinClient("notice");
            var notes = front.childItems.FindAll(item => item.identifier == "handnote" && item.GetTagReadonly("HANDNOTE_TITLE").GetString() == "Wanted Notice from the Jin Garrison");
            Check(notes.Count == (passes ? 1 : 2) && !guard.isWanted && AssassinValue("notice_day") == firstDay + 1, "guard delivers one readable document on the following day");
            guard.OnArrival();
            Check(front.childItems.FindAll(item => item.identifier == "handnote").Count == notes.Count, "repeat arrival callback cannot duplicate notice");
            Check(store.wantedStatus.Count == originalWanted.Count && !new List<string>(originalWanted.Keys).Exists(k => store.wantedStatus[k] != originalWanted[k]), "original wanted statuses unchanged by guard and notice");
            store.DismissCurrentClient(false, false, true);

            station.dayCounter = firstDay + 2;
            manager.GenerateClient();
            ShowAssassinClient("loot");
            var goods = front.childItems.FindAll(item => item.identifier == "system_module_efficiancy" || item.identifier == "system_module_quality" || item.identifier == "system_module_performance");
            Check(goods.Count == (passes ? 3 : 1), "food outcome controls the number of garrison goods on the counter");
            long total = 0;
            foreach (GameItem item in goods) total += item.GetNegociatedValue();
            Check(total > 0, "native garrison lot has a positive opening price");
            int payment = (int)((total * 66 + 99) / 100) - (passes ? 0 : 1);
            GameItem purchased = goods[0];
            SetAssassinTestPrice(purchased, payment);
            Check(purchased.GetNegociatedValue() == payment, "one native purchase priced at the test boundary");
            store.playerCash = 5000;
            store.BuyItem(purchased);
            Check(5000 - store.playerCash == payment && AssassinValue("loot_passed") == -1, "native loot purchase debits cash and waits for departure");
            store.DismissCurrentClient(false, false, true);
            Check(AssassinValue("loot_passed") == (passes ? 1 : 0), "loot result uses 66 percent of the locked opening price on departure");
            manager.GenerateClient();
            Check(!manager.clientStack.Exists(c => c.identifier.StartsWith("jinsong_assassin_")), "completed batch does not requeue any of the three visits");
            if (passes) HandnoteUIManager.Instance.OpenPanel(notes[0]);
        }

        private static void ResetAssassinEnding()
        {
            foreach (string key in new[] { "loot_day", "weapon_day", "weapon_passed", "outcome_day", "outcome", "news_published" })
                ModHelper.RemoveModData("jinsong_poc", "assassin." + key);
        }

        private void PayAssassinBuyer(StoreClient client, string itemId, int price)
        {
            PlayerStore store = PlayerStore.Instance;
            GameItem item = DirectoryMaster.Item(itemId, true);
            SetAssassinTestPrice(item, price);
            Check(client.IsClientBuyingThisItem(item), "native buyer accepts " + itemId);
            int cash = store.playerCash;
            store.SellItem(item);
            Check(store.playerCash - cash == price, "native buyer actually pays " + price);
        }

        private void CheckAssassinEnding(bool killed, bool escaped, bool fromFullRun, bool report = false)
        {
            PlayerStore store = PlayerStore.Instance;
            StoreStation station = StoreStation.Instance;
            StoreClientManager manager = store.storeClientManager;
            if (!fromFullRun)
            {
                ResetAssassinEnding();
                station.dayCounter += 4;
                ModHelper.SetModDataInt("jinsong_poc", "assassin.loot_passed", 0);
            }
            else
            {
                station.dayCounter += 2;
                manager.GenerateClient();
                Check(!manager.clientStack.Exists(c => c.identifier == "jinsong_assassin_weapon"), "weapons visit waits three days after loot departure");
                station.dayCounter++;
            }
            manager.GenerateClient();
            StoreClient weapon = ShowAssassinClient("weapon");
            Check(weapon.GetBudget() == 1000, "weapons visit has the agreed budget");
            string locale = LocHelper.GetCurrentLocaleCode();
            string prefix = "assassin_";
            Check(weapon.mainDialogue.plainText == mod.Localization[locale][prefix + (fromFullRun ? "weapon_informant" : "weapon_alone")], "loot result changes the informant dialogue");
            DialogUIManager dialog = DialogUIManager.Instance;
            Dialogue decision = weapon.mainDialogue.nextDialogue;
            Check(dialog.StartDialogue(decision, true) && dialog.IsChoice(), "native dialogue starts with the two weapon choices");
            dialog.DisplayChoice();
            Check(dialog.dialogChoiceBubbles[0].TMP.text == mod.Localization[locale][prefix + "weapon_continue"]
               && dialog.dialogChoiceBubbles[1].TMP.text == mod.Localization[locale][prefix + "weapon_report"], "trade and report choices visibly rendered");
            dialog.SelectChoice(report ? 1 : 0);
            string ending;
            if (report)
            {
                Check(AssassinValue("outcome") == 5 && AssassinValue("weapon_day") == -1, "native report choice records arrest before the attack");
                Dialogue response = decision.choices[1].Item2.nextDialogue;
                dialog.DisplayClientText(response);
                response.ExecuteEndAction();
                Check(store.currentClientInstance == null, "report response ends the window visit");
                ending = "reported";
            }
            else
            {
                PayAssassinBuyer(weapon, "combat_knife", killed ? 660 : 659);
                Check(AssassinValue("weapon_passed") == -1, "weapon trade waits for dismissal");
                store.DismissCurrentClient(false, false, true);
                Check(AssassinValue("weapon_passed") == (killed ? 1 : 0), "weapon threshold decides target fate at departure");
                manager.GenerateClient();
                Check(!manager.clientStack.Exists(c => c.identifier == "jinsong_assassin_medicine"), "wounded visitor waits until next day");
                station.dayCounter++;
                manager.GenerateClient();
                StoreClient medicine = ShowAssassinClient("medicine");
                Check(medicine.GetBudget() == 500 && medicine.mainDialogue.plainText == mod.Localization[locale][prefix + (killed ? "medicine_killed" : "medicine_survived")], "wounded visitor recounts the target's fate");
                PayAssassinBuyer(medicine, "bandage_item", escaped ? 330 : 329);
                Check(AssassinValue("outcome") == -1, "medicine trade waits for dismissal");
                store.DismissCurrentClient(false, false, true);
                Check(AssassinValue("outcome") == (killed ? (escaped ? 1 : 2) : (escaped ? 3 : 4)), "medicine threshold decides escape or capture at departure");
                ending = (killed ? "kill_" : "survive_") + (escaped ? "escape" : "caught");
            }
            string newsId = "jinsong_assassin_news_" + ending;
            StoreEventManager newspaper = station.storeEventManager;
            manager.GenerateClient();
            Check(!newspaper.GetActiveEvents().Exists(e => e.identifier == newsId), "ending is held for next-day news");
            station.dayCounter++;
            manager.GenerateClient();
            manager.GenerateClient();
            List<StoreEvent> articles = newspaper.GetActiveEvents().FindAll(e => e.identifier == newsId);
            Check(articles.Count == 1 && AssassinValue("news_published") == 1, "one native next-day newspaper event for " + ending);
            Check(!manager.clientStack.Exists(c => c.identifier.StartsWith("jinsong_assassin_")), "completed story has no queued return");
            NewsUIManager news = NewsUIManager.Instance;
            news.uiGameObject.SetActive(true);
            news.HideAllElement();
            news.SelectLayout(articles);
            NewsUIElement row = null;
            foreach (GameObject layout in new[] { news.layout1, news.layout2, news.layout3, news.layout4 })
                if (layout.activeInHierarchy) { row = layout.GetComponentInChildren<NewsUIElement>(true); break; }
            Check(row != null, "native newspaper layout selected an article row");
            row.PopulateUI(articles[0]);
            row.gameObject.SetActive(true);
            string articleKey = prefix + "news_" + ending;
            Check(Plain(row.title.text) == Plain(mod.Localization[locale][articleKey + "_title"])
                && Plain(row.textContent.text).Contains(Plain(mod.Localization[locale][articleKey + "_body"])), "rendered newspaper matches " + ending);
            news.CloseUI();
        }

        private void CheckWorkshopCompatibility()
        {
            Check(LocHelper.GetLocalizedUI("inte_credit_count") == "查看账上现银", "native fixture lookup retains the shop wording");
            Check(LocHelper.GetLocalizedUI("book_ribwich_section6_body").Contains("大丰食坊"), "guide company wording still resolves through the game");

            string owner = mod.HarmonyInstance.Id;
            int chemistry = 0, detailsTargets = 0, dialogue = 0;
            bool legacyTablePatched = false;
            foreach (var target in Harmony.GetAllPatchedMethods())
            {
                if (target.DeclaringType == null) continue;
                var patch = Harmony.GetPatchInfo(target);
                if (patch == null || !new List<string>(patch.Owners).Contains(owner)) continue;
                string type = target.DeclaringType.FullName;
                if ((type.StartsWith("MachineHeatingPlate+") && target.Name == "<HeatingPlate>b__4")
                    || (type.StartsWith("MachineChemFinisher+") && target.Name == "<ChemFinisher>b__3")) chemistry++;
                if (type.StartsWith("ItemDetails.", StringComparison.Ordinal)) detailsTargets++;
                if ((type == "DialogUIManager" || type == "DialoguePhoneUIManager") && target.Name == "SetClientBubbleColor") dialogue++;
                legacyTablePatched |= type == "UnityEngine.Localization.Tables.StringTableEntry";
            }
            Check(!legacyTablePatched, "no Harmony patch on Unity localization table entries");
            Check(chemistry == 2, "both native chemical live-status closures are patched");
            Check(dialogue == 2, "shop and phone name bubble targets are active");
            var players = UnityEngine.Object.FindObjectsOfType<EmporiumAudioPlayer>(true);
            Check(Array.Exists(players, player => player.audioButton1 != null
                && player.audioButton1.clip == mod.Sounds["jin_effect_button1"]), "public audio field uses the approved click");
            foreach (StoreSoundPlayer player in UnityEngine.Object.FindObjectsOfType<StoreSoundPlayer>(true))
                Check(player.musicAudioSource == null || player.musicAudioSource.clip == null
                    || !mod.Sounds.ContainsValue(player.musicAudioSource.clip), "background music keeps its native clip");

            LoadedMod detailsMod = ModLoader.Mods.Find(m => m.Manifest.ID == "item_details");
            bool enabled = detailsMod != null && detailsMod.IsEnabled;
            Check(enabled ? detailsTargets == 5 : detailsTargets == 0, enabled ? "all five Item Details targets bound" : "main mod loads independently with Item Details disabled");
            if (!enabled) return;
            GameItemElement item = (GameItemElement)DirectoryMaster.Item("water_purifier", true);
            GameItem jar = DirectoryMaster.Item("med_bottle_small_pink", true);
            Check(GraphUtils.TryAcceptAll(item, jar, -1) == 0, "native purifier accepts its original jar");
            WaterHelper.FillWithBaseWater(jar);
            string tooltip = item.GetTooltipAdvanced().nakedText;
            Check(tooltip.Contains("青瓷净水器") && tooltip.Contains("青瓷接水壶") && tooltip.Contains("150"), "actual Item Details tooltip shows themed container and live water");
        }

        private IEnumerator Start()
        {
            Application.logMessageReceived += OnLog;
            Application.runInBackground = true;
            focused = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-focus") >= 0;
            menuLanguage = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-menu-language") >= 0;
            chromeOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-chrome-only") >= 0;
            finishOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-finish-only") >= 0;
            alcoveOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-alcove-only") >= 0;
            streetOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-street-only") >= 0;
            gridOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-grid-only") >= 0;
            polishOnly = gridOnly || Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-polish-only") >= 0;
            networkOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-network") >= 0;
            assassinOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-assassin") >= 0;
            workshopCompatibility = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-workshop-compat") >= 0;
            workshopOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-workshop") >= 0;
            fixtureClicksOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-fixture-clicks") >= 0;
            archiveServicesOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-archive-services") >= 0;
            companyOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-company-only") >= 0;
            marketOnly = Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-market-only") >= 0;
            reportedIssues = alcoveOnly || Array.IndexOf(Environment.GetCommandLineArgs(), "--jinsong-ui-survey-reported") >= 0;
            yield return new WaitForSecondsRealtime(6f);
            originalLocale = LocalizationSettings.SelectedLocale;
            if (!Try("ready", () => {
                mod = ModLoader.Mods.Find(m => m.Manifest.ID == "jinsong_poc");
                if (workshopCompatibility)
                {
                    Check(mod != null && mod.IsEnabled && mod.Manifest.ModVersion == "0.1.2-alpha262", "installed compatibility candidate active");
                    return;
                }
                if (assassinOnly)
                {
                    Check(mod != null && mod.IsEnabled && mod.Manifest.ModVersion == "0.1.2-alpha260", "installed Assassin alpha260 enabled");
                    return;
                }
                Check(mod != null && mod.IsEnabled && (networkOnly ? (mod.Manifest.ModVersion == "0.1.2-alpha256" || mod.Manifest.ModVersion == "0.1.2-alpha258") : workshopOnly ? (mod.Manifest.ModVersion == "0.1.2-alpha255" || mod.Manifest.ModVersion == "0.1.2-alpha256") : fixtureClicksOnly ? (mod.Manifest.ModVersion == "0.1.2-alpha254" || mod.Manifest.ModVersion == "0.1.2-alpha255") : archiveServicesOnly ? (mod.Manifest.ModVersion == "0.1.2-alpha252" || mod.Manifest.ModVersion == "0.1.2-alpha253") : companyOnly ? mod.Manifest.ModVersion == "0.1.2-alpha252" : marketOnly ? mod.Manifest.ModVersion == "0.1.2-alpha251" : polishOnly ? mod.Manifest.ModVersion == "0.1.2-alpha250" : streetOnly ? mod.Manifest.ModVersion == "0.1.2-alpha249" : mod.Manifest.ModVersion == "0.1.2-alpha245" || reportedIssues), "installed presentation mod enabled " + (mod == null ? "null" : mod.Manifest.ModVersion));
                Debug.Log("[JINSONG_SURVEY] RESOLUTION " + Screen.width + "x" + Screen.height + " original_locale=" + LocHelper.GetCurrentLocaleCode());
            })) { Finish(); yield break; }
            if (!focused && !alcoveOnly && !streetOnly && !polishOnly && !marketOnly && !companyOnly && !archiveServicesOnly && !fixtureClicksOnly && !workshopOnly && !networkOnly && !assassinOnly && !workshopCompatibility) yield return Capture("01-menu");
            if (polishOnly && !gridOnly)
            {
                Try("open trait selection", () => NewGamePerksUI.Open());
                foreach (bool positive in new[] { true, false })
                {
                    Try("show actual trait description", () => {
                        var perks = (List<StartingPerk>)AccessTools.Field(typeof(NewGamePerksUI), "sorted").GetValue(null);
                        StartingPerk perk = perks.Find(p => positive ? p.cost > 0 : p.cost < 0);
                        Check(perk != null, "native " + (positive ? "positive" : "negative") + " trait exists");
                        AccessTools.Method(typeof(NewGamePerksUI), "ShowTooltip").Invoke(null, new object[] { perk, null });
                    });
                    yield return new WaitForSecondsRealtime(.4f);
                    Try("trait description contrast", CheckTraitContrast);
                    yield return Capture(positive ? "40-positive-trait" : "41-negative-trait");
                }
                Try("leave trait selection", () => NewGamePerksUI.CloseWithoutReturning());
            }
            if (reportedIssues && !alcoveOnly)
            {
                Try("open profession selection", () => NewGameSpecializationUI.Open());
                yield return Capture("20-profession");
                Try("profession chrome", DumpReportedUi);
                Try("profession paper style", () => CheckReportedPanel(GameObject.Find("Canvas/custom_window_new_game_specialization"), "new_game_bg"));
                Try("close profession selection", () => NewGameSpecializationUI.Hide());
            }
            if (finishOnly)
            {
                Try("Chinese menu brand", () => CheckBrand("金宋当铺", "收售百货"));
                Try("native English selection", () => {
                    MainMenuUIController menu = MainMenuUIController.Instance;
                    originalLanguageIndex = menu.localeDropdown.value;
                    int english = menu.localeDropdown.options.FindIndex(o => o.text == "English");
                    Check(english >= 0, "native English option present");
                    menu.OnLocaleChanged(english);
                });
                yield return new WaitForSecondsRealtime(2f);
                Try("English menu brand", () => CheckBrand("JinSong Pawnshop", "BUY & SELL GOODS"));
                yield return Capture("10-brand-en");
                Try("restore Chinese selection", () => MainMenuUIController.Instance.OnLocaleChanged(originalLanguageIndex));
                yield return new WaitForSecondsRealtime(2f);
            }
            if (chromeOnly) Try("menu hit masks", CheckHitMasks);
            if (menuLanguage)
            {
                Try("main-menu language selection", () => {
                    MainMenuUIController menu = MainMenuUIController.Instance;
                    originalLanguageIndex = menu.localeDropdown.value;
                    int english = menu.localeDropdown.options.FindIndex(o => o.text == "English");
                    Check(english >= 0, "native main-menu English option exists");
                    menu.OnLocaleChanged(english);
                });
                yield return new WaitForSecondsRealtime(3f);
            }
            NewGameData.Instance.isLoadFromMainMenu = false;
            NewGameData.Instance.saveSlot = -1;
            NewGameData.Instance.skipIntro = true;
            NewGameData.Instance.disableTutorialHint = true;
            Debug.Log("[JINSONG_SURVEY] MEMORY saveSlot=-1; preload/load/save/write guards active");
            SceneManager.LoadScene(1);
            yield return new WaitForSecondsRealtime(6f);
            if (!Try("memory shop", () => Check(PlayerStore.Instance != null && NoLoad.Blocked == 0 && NewGameData.Instance.saveSlot == -1, "fresh memory shop, no LoadGame calls")))
            { Finish(); yield break; }
            Try("close starting hints", () => { TutorialUIManager.Instance.CloseAllTutorial(); StoreUIManager.Instance.controlHintOverlay.SetActive(false); });
            if (workshopCompatibility)
            {
                Try("public audio and scoped Harmony targets", CheckWorkshopCompatibility);
                NewsUIManager news = NewsUIManager.Instance;
                news.uiGameObject.SetActive(true);
                yield return new WaitForSecondsRealtime(1f);
                Try("native serialized newspaper labels", () => {
                    string title = LocHelper.GetCurrentLocaleCode() == "zh" ? "坊间日报" : "City Daily";
                    string subtitle = LocHelper.GetCurrentLocaleCode() == "zh" ? "每日消息" : "Daily News";
                    var labels = new List<TMP_Text>(news.uiGameObject.GetComponentsInChildren<TMP_Text>(true));
                    if (!labels.Exists(t => t.text == title) || !labels.Exists(t => t.text == subtitle)) DumpWorkshopLabels(news.uiGameObject);
                    Check(labels.Exists(t => t.text == title) && labels.Exists(t => t.text == subtitle), "serialized newspaper keeps the themed masthead and subtitle");
                    news.CloseUI();
                });
                Try("open native serialized guide", () => {
                    MultiPageUIManager.Instance.OpenUI(1);
                    AccessTools.Method(typeof(MultiPageUIManager), "ShowPage").Invoke(MultiPageUIManager.Instance, new object[] { 1 });
                });
                yield return new WaitForSecondsRealtime(1f);
                Try("native serialized guide labels", () => {
                    var labels = new List<TMP_Text>(MultiPageUIManager.Instance.books[1].GetComponentsInChildren<TMP_Text>(true));
                    Check(labels.Exists(t => t.isActiveAndEnabled && t.text.Contains("城中的水可能含有多种类型的污染物")), "native water guide renders the authored label");
                    MultiPageUIManager.Instance.CloseUI();
                });
                Try("compatibility memory guards", () => Check(NoLoad.Blocked == 0 && NewGameData.Instance.saveSlot == -1, "compatibility checks used a guarded temporary shop"));
                Finish(); yield break;
            }
            if (assassinOnly)
            {
                Try("66 percent success and notice", () => CheckAssassin(true));
                yield return new WaitForSecondsRealtime(.4f);
                Try("read independent document", () => {
                    var note = HandnoteUIManager.Instance;
                    Check(note.title.text == (LocHelper.GetCurrentLocaleCode() == "zh" ? "金军军营通缉单" : "Wanted Notice from the Jin Garrison"), "native handnote reader shows garrison notice");
                    Check(note.body.text.Contains(LocHelper.GetCurrentLocaleCode() == "zh" ? "宋地口音" : "Song accent"), "native handnote reader shows body");
                    note.ClosePanel();
                });
                Try("full story with escape", () => CheckAssassinEnding(true, true, true));
                Try("target killed and assassin caught", () => CheckAssassinEnding(true, false, false));
                Try("target survives and assassin escapes", () => CheckAssassinEnding(false, true, false));
                Try("target survives and assassin caught", () => CheckAssassinEnding(false, false, false));
                Try("report before the attack", () => CheckAssassinEnding(false, false, false, true));
                Try("assassin memory guards", () => Check(NoLoad.Blocked == 0 && NewGameData.Instance.saveSlot == -1, "assassin check used guarded memory shop"));
                Finish(); yield break;
            }
            if (networkOnly)
            {
                Try("open native network panel", () => WildNetworkUI.Open(true));
                yield return new WaitForSecondsRealtime(.5f);
                WildNetworkFrame frame = UnityEngine.Object.FindObjectOfType<WildNetworkFrame>();
                int cash = PlayerStore.Instance.playerCash;
                Try("native network upgrade selection", () => {
                    Check(WildNetworkUI.IsOpen && frame != null, "native contacts panel opened");
                    var slots = frame.gridContent.GetComponentsInChildren<WildNetworkSlot>();
                    Check(slots.Length > 0, "native upgrade cards populated");
                    slots[0].OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
                    Check(!String.IsNullOrEmpty(frame.infoTitle.text) && !String.IsNullOrEmpty(frame.infoDescription.text), "original click shows upgrade details");
                    if (mod.Manifest.ModVersion == "0.1.2-alpha258")
                    {
                        var images = frame.GetComponentsInChildren<UnityEngine.UI.Image>();
                        Check(Array.Exists(images, image => image.sprite == frame.windowFrame && image.overrideSprite != null
                            && image.overrideSprite.name == "jinsong_poc.network.window_frame"), "network window uses wood and paper panel");
                        Check(Array.Exists(images, image => image.sprite == frame.cardPlate && image.overrideSprite != null
                            && image.overrideSprite.name == "jinsong_poc.network.card_plate"), "native upgrade cards use paper material");
                        Check(Array.Exists(frame.GetComponentsInChildren<TMP_Text>(), text => text.text == "乔望川的人脉簿"), "panel title uses established character name");
                        Check(frame.infoTitle.text == "红魔鬼酒坊" && frame.infoDescription.text.Contains("乔望川"), "native selected detail uses new names");
                        int cost = PlayerStore.Instance.networkUpgrade[slots[0].id].GetCost();
                        Check(frame.usePrice.text.Contains(cost.ToString()) && frame.fundsValue.text == cash.ToString(), "native price and funds still shown");
                        Check(Array.Exists(images, image => image.sprite == frame.priceTagUnavailable), "native unaffordable card state still present");
                    }
                });
                yield return new WaitForSecondsRealtime(.4f);
                yield return Capture("73-contacts");
                Try("network close and memory state", () => {
                    WildNetworkUI.Close();
                    Check(!WildNetworkUI.IsOpen && PlayerStore.Instance.playerCash == cash, "native close restores shop without changing funds");
                    Check(NoLoad.Blocked == 0 && NewGameData.Instance.saveSlot == -1 && runtimeErrors.Count == 0, "network check uses only guarded memory shop");
                });
                Finish(); yield break;
            }
            if (workshopOnly)
            {
                MapUIManager map = MapUIManager.Instance;
                EmporiumEntry entry = EmporiumEntry.Instance;
                GameGridInventory front = entry.frontInvinvElement;
                Vector2 shopPosition = entry.frontInvWindow.rectTransform.anchoredPosition;
                Vector3 shopScale = entry.frontInvWindow.rectTransform.localScale;
                Try("native workshop visit", () => map.Travel(new DrJacksonLocation()));
                yield return new WaitForSecondsRealtime(2f);
                bool aligned = Try("workshop stock and drop target", () => {
                    Check(map.Current is DrJacksonLocation && map.upgradeStoreScene.activeInHierarchy, "native travel opens Jackson workshop");
                    Check(GridCells(front) == new Vector2(14, 21) && front.childItems.Count > 0, "native board stock populated in the 14 by 21 grid");
                    Vector2 bottomLeft = FixtureScreenPoint(front.rectTransform, new Vector2(0, -front.heightPixels));
                    Vector2 topRight = FixtureScreenPoint(front.rectTransform, new Vector2(front.widthPixels, 0));
                    Debug.Log("[JINSONG_SURVEY] WORKSHOP stock=" + front.childItems.Count + " board=" + bottomLeft + " to " + topRight);
                    Check(bottomLeft.x >= Screen.width * .727f && topRight.x <= Screen.width * .985f
                        && bottomLeft.y >= Screen.height * .26f && topRight.y <= Screen.height * .93f,
                        "entire stock grid fits inside the right-hand board");
                    var item = front.childItems[0] as GameItemElement;
                    RectTransform itemRect = item.background.image.rectTransform;
                    Vector2 itemPoint = FixtureScreenPoint(itemRect, itemRect.rect.center);
                    Check(RenderHandler.RaycastElement<GameItemElement>(itemPoint, null) == item,
                        "stock on the board responds at its rendered position");
                    Vector2 emptyPoint = FixtureScreenPoint(front.rectTransform, new Vector2(front.widthPixels - 8, -front.heightPixels + 8));
                    Check(RenderHandler.RaycastFindParentElement<GameInventory>(emptyPoint, null) == front,
                        "board corner reaches the original inventory drop target");
                    entry.Validate(true);
                });
                if (aligned)
                {
                    yield return new WaitForEndOfFrame();
                    Try("workshop revalidation", () => Check(FixtureScreenPoint(front.rectTransform, Vector2.zero).x >= Screen.width * .727f,
                        "native layout refresh keeps stock on the board"));
                    yield return Capture("72-jackson-board");
                }
                if (map.Current is DrJacksonLocation) map.ReturnNow();
                yield return new WaitForSecondsRealtime(2f);
                Try("return from workshop", () => {
                    Check(map.Current == null, "native return restores the pawnshop");
                    Check(Vector2.Distance(entry.frontInvWindow.rectTransform.anchoredPosition, shopPosition) < 1f
                        && Vector3.Distance(entry.frontInvWindow.rectTransform.localScale, shopScale) < .01f,
                        "the pawnshop counter returns to its themed placement");
                    Check(NoLoad.Blocked == 0 && NewGameData.Instance.saveSlot == -1 && runtimeErrors.Count == 0,
                        "workshop visit stayed in memory without runtime errors");
                });
                Finish(); yield break;
            }
            if (fixtureClicksOnly)
            {
                EmporiumEntry entry = EmporiumEntry.Instance;
                GameItemElement displayed = null;
                GameGridInventory showcase = entry.showcaseElement;
                Try("display case placement", () => {
                    Vector2 position = FixtureScreenPoint(showcase.rectTransform,
                        new Vector2(showcase.widthPixels / 2f, -showcase.heightPixels / 2f));
                    GameInventory destination = RenderHandler.RaycastFindParentElement<GameInventory>(position, null);
                    LogFixtureHits("showcase", position);
                    Check(destination == showcase, "display case center reaches native inventory drop target");
                    displayed = DirectoryMaster.Item("caffeine_pill", true) as GameItemElement;
                    Check(displayed != null && displayed.identifier == "caffeine_pill", "real sample item ready");
                    Check(GraphUtils.TryAcceptAll(destination, displayed, 1) == 0 && showcase.childItems.Contains(displayed), "display case accepts sample through native storage");
                });
                yield return new WaitForEndOfFrame();
                Try("display case retrieval", () => {
                    if (displayed == null || !showcase.childItems.Contains(displayed)) return;
                    var image = displayed.background.image;
                    RectTransform rect = image.rectTransform;
                    Vector2 position = FixtureScreenPoint(rect, rect.rect.center);
                    LogFixtureHits("showcase item", position);
                    Debug.Log("[JINSONG_SURVEY] SHOWCASE ITEM active=" + image.isActiveAndEnabled + " raycast=" + image.raycastTarget
                        + " threshold=" + image.alphaHitTestMinimumThreshold + " rect=" + rect.rect);
                    GameItemElement hit = RenderHandler.RaycastElement<GameItemElement>(position, null);
                    Check(hit == displayed, "displayed item can be picked up from its rendered position");
                    Check(GraphUtils.TryAcceptAll(entry.invElement, hit, 1) == 0 && !showcase.childItems.Contains(displayed), "sample returns to main inventory");
                });
                Try("waste basket input", () => {
                    GameItemElement waste = entry.trashcan as GameItemElement;
                    RectTransform rect = waste.background.image.rectTransform;
                    Vector2 position = FixtureScreenPoint(rect, rect.rect.center);
                    GameItemElement hit = RenderHandler.RaycastElement<GameItemElement>(position, null);
                    LogFixtureHits("waste", position);
                    Debug.Log("[JINSONG_SURVEY] WASTE threshold=" + waste.background.image.alphaHitTestMinimumThreshold);
                    Check(hit == waste, "painted waste basket reaches native item click target");
                    ItemMouseDoubleClickHandler.current.OpenContentAction(hit, position);
                    Check(waste.contentWindow.IsVisible(), "native double-click action opens waste basket");
                    GameItem sample = DirectoryMaster.Item("caffeine_pill", true);
                    var grids = GraphUtils.FindChildrenType<GameGridInventory>(waste.contentWindow, null, null);
                    Check(grids.Count == 1 && GraphUtils.TryAcceptAll(grids[0], sample, 1) == 0, "waste basket accepts sample through native storage");
                    Check(GraphUtils.TryAcceptAll(entry.invElement, sample, 1) == 0 && !grids[0].childItems.Contains(sample), "sample can be retrieved before overnight disposal");
                    waste.contentWindow.Hide();
                });
                Try("fixture check memory guards", () => Check(NoLoad.Blocked == 0 && NewGameData.Instance.saveSlot == -1 && runtimeErrors.Count == 0,
                    "fixture check uses guarded memory shop without runtime errors"));
                Finish(); yield break;
            }
            if (archiveServicesOnly)
            {
                GameItemElement archive = EmporiumEntry.Instance.docInvElement.childItem as GameItemElement;
                Vector2 archivePoint = Vector2.zero;
                Try("archive raycast", () => {
                    Check(archive != null && archive.contentWindow != null, "native archive object and content exist");
                    var image = archive.background.image;
                    Canvas canvas = image.GetComponentInParent<Canvas>();
                    Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                    archivePoint = RectTransformUtility.WorldToScreenPoint(camera, image.rectTransform.TransformPoint(image.rectTransform.rect.center));
                    Debug.Log("[JINSONG_SURVEY] ARCHIVE id=" + archive.identifier + " screen=" + archivePoint
                        + " threshold=" + image.alphaHitTestMinimumThreshold + " valid=" + image.IsRaycastLocationValid(archivePoint, camera));
                    Check(image.IsRaycastLocationValid(archivePoint, camera), "transparent archive art retains native click area");
                    Check(RenderHandler.RaycastElement<GameItemElement>(archivePoint, null) == archive, "painted archive location resolves to native archive item");
                });
                Try("native archive open", () => ItemMouseDoubleClickHandler.current.OpenContentAction(archive, archivePoint));
                yield return new WaitForSecondsRealtime(.5f);
                Try("archive content visible", () => Check(archive.contentWindow.IsVisible() && archive.contentWindow.handler.gameObject.activeInHierarchy,
                    "original archive open action displays its original content window"));
                yield return Capture("70-archive-open");
                if (mod.Manifest.ModVersion == "0.1.2-alpha253") Try("archive native storage roundtrip", () => {
                    var grids = GraphUtils.FindChildrenType<GameGridInventory>(archive.contentWindow, null, null);
                    Check(grids.Count == 1 && grids[0].childItems.Count > 0, "archive retains original storage and contents");
                    GameGridInventory storage = grids[0];
                    GameItem document = storage.childItems[0];
                    int count = storage.childItems.Count;
                    string id = document.identifier, name = document.name;
                    Check(GraphUtils.TryAcceptAll(EmporiumEntry.Instance.invElement, document, 1) == 0, "native archive document can be retrieved");
                    Check(GraphUtils.TryAcceptAll(storage, document, 1) == 0, "same native document can be stored again");
                    Check(storage.childItems.Count == count && document.identifier == id && document.name == name, "archive roundtrip preserves contents and document identity");
                });
                Try("archive close", () => archive.contentWindow.Hide());
                var serviceNames = new Dictionary<StoreService, string>();
                var serviceLocks = new Dictionary<StoreService, bool>();
                var serviceCosts = new Dictionary<StoreService, int>();
                Try("prepare isolated service list", () => {
                    foreach (StoreService service in PlayerStore.Instance.storeService)
                    {
                        serviceNames[service] = service.displayName;
                        serviceCosts[service] = service.cost;
                        serviceLocks[service] = service.unlocked;
                        service.unlocked = true;
                        Debug.Log("[JINSONG_SURVEY] SERVICE id=" + service.id + " name=" + service.displayName + " cost=" + service.cost
                            + " mandatory=" + service.isMandatory + " cooldown=" + service.currentCooldown);
                    }
                    EndOfDayUIManager.Instance.OpenUI();
                });
                yield return new WaitForSecondsRealtime(.5f);
                if (mod.Manifest.ModVersion == "0.1.2-alpha253") Try("service display and predicates", () => {
                    string labels = "";
                    foreach (TMP_Text label in EndOfDayUIManager.Instance.UI.GetComponentsInChildren<TMP_Text>(true)) labels += label.text + "\n";
                    Check(labels.Contains("供能机枢费") && labels.Contains("失窃保契") && labels.Contains("铺面洒扫"), "native service rows display current-theme names");
                    foreach (var entry in serviceNames) Check(entry.Key.displayName == entry.Value, "cached service name not mutated " + entry.Key.id);
                    foreach (var entry in serviceCosts) Check(entry.Key.cost == entry.Value, "native service charge retained " + entry.Key.id);
                    StoreService insurance = PlayerStore.Instance.storeService.Find(service => service.id == "INSURANCE_SERVICE");
                    TabletCheckbox insuranceBox = null;
                    foreach (TMP_Text label in EndOfDayUIManager.Instance.UI.GetComponentsInChildren<TMP_Text>(true))
                        if (label.text == "失窃保契") insuranceBox = label.transform.parent.GetComponentInChildren<TabletCheckbox>();
                    Check(insurance != null && insuranceBox != null && insuranceBox.button.interactable, "original insurance checkbox remains available");
                    bool usingBefore = insurance.isUsing;
                    int cashBefore = PlayerStore.Instance.playerCash;
                    insuranceBox.button.onClick.Invoke();
                    Check(insurance.isUsing != usingBefore && insurance.cost == serviceCosts[insurance], "native checkbox toggles insurance without changing its fee");
                    insuranceBox.button.onClick.Invoke();
                    Check(insurance.isUsing == usingBefore && PlayerStore.Instance.playerCash == cashBefore, "native selection restores and no cash is charged before settlement");
                    Check(LocHelper.GetLocalizedUI("ui_info_service_insurance").Contains("失窃保契"), "insurance tooltip agrees with service label");
                    Check(LocHelper.GetLocalizedMechanic("mech_nightlog_power_on").Contains("供能"), "morning settlement uses matching supply wording");
                });
                yield return Capture("71-nightly-services");
                Try("restore temporary service state", () => {
                    EndOfDayUIManager.Instance.CloseUI(false);
                    foreach (var entry in serviceLocks) entry.Key.unlocked = entry.Value;
                    Check(NoLoad.Blocked == 0 && NewGameData.Instance.saveSlot == -1 && runtimeErrors.Count == 0, "archive/services run stays memory-only without errors");
                });
                Finish(); yield break;
            }
            if (companyOnly)
            {
                Try("company display and native identity", () => {
                    var item = (GameItemElement)DirectoryMaster.Item("energy_credit", true);
                    string id = item.identifier, name = item.name, description = item.shortDescription;
                    int count = 0;
                    foreach (var entry in mod.Localization[LocHelper.GetCurrentLocaleCode()])
                    {
                        const string prefix = "organization_corpo_name_";
                        if (!entry.Key.StartsWith(prefix, StringComparison.Ordinal)) continue;
                        string company = entry.Key.Substring(prefix.Length);
                        var builder = new RichTextBuilder(40);
                        item.DisplayManufacturer(builder, company);
                        Check(builder.Build(-1).nakedText.Contains(entry.Value), "native manufacturer renderer uses alias " + company);
                        string original = LocHelper.GetLocalizedClientName("corpo_name_" + company);
                        Check(original != entry.Value, "native company getter stays original " + company);
                        count++;
                    }
                    Check(count == 21, "all twenty-one approved manufacturer aliases rendered");
                    Check(item.identifier == id && item.name == name && item.shortDescription == description,
                        "rendering does not mutate native item fields");
                    string real = mod.Localization[LocHelper.GetCurrentLocaleCode()]["organization_corpo_name_eminence"];
                    string fake = mod.Localization[LocHelper.GetCurrentLocaleCode()]["organization_corpo_name_eninence"];
                    Check(real != fake && real.Contains("卓越") && fake.Contains("桌越"), "genuine and counterfeit spelling difference retained");
                    Check(LocHelper.GetLocalizedUI("book_ribwich_section6_body").Contains("大丰食坊"), "synchronous guide company name matches item");
                    var table = LocalizationSettings.StringDatabase.GetTable("UI");
                    Check(table.GetEntry("book_ribwich_section6_title").GetLocalizedString(null, null, null).Contains("大丰食坊"),
                        "serialized guide label uses the same company alias");
                    details = item.GetTooltipAdvanced();
                    Check(details.nakedText.Contains("卓越机枢行") && !details.nakedText.Contains("卓越电力公司"),
                        "actual energy-credit item-details tooltip shows the new company");
                    Debug.Log("[JINSONG_SURVEY] COMPANY tooltip=" + details.nakedText.Replace("\n", " / "));
                });
                yield return Capture("60-company-tooltip");
                details = null;
                if (OverlayHandler.current != null) OverlayHandler.current.HideTooltip();
                DumpAudio("shop-entry");
                yield return new WaitForSecondsRealtime(6f);
                DumpAudio("shop-after-six-seconds");
                Try("company/audio narrow run state", () => Check(NoLoad.Blocked == 0 && NewGameData.Instance.saveSlot == -1 && runtimeErrors.Count == 0,
                    "company/audio run uses guarded memory shop without runtime errors"));
                Finish(); yield break;
            }
            if (marketOnly)
            {
                Try("open market window", () => StoreShutterButton.Instance.OpenShutter());
                yield return new WaitForSecondsRealtime(2f);
                SpriteRenderer mover = null;
                Sprite pose = null;
                Vector3 start = Vector3.zero;
                Try("approved static market composition", () => {
                    Check(mod.Textures.ContainsKey("jin_market_left") && mod.Textures.ContainsKey("jin_market_right"), "approved stall pair loaded");
                    var variants = new HashSet<string>();
                    int count = 0;
                    foreach (SpriteRenderer figure in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>(true))
                    {
                        if (figure.name != "JinSong.StreetFigure") continue;
                        Check(figure.sprite != null && figure.sprite.name.StartsWith("jinsong_poc.street.static.", StringComparison.Ordinal), "fixed-pose figure replaces gait frames");
                        Check(!figure.transform.parent.GetComponent<SpriteRenderer>().enabled, "original walking silhouette suppressed");
                        Check(figure.sortingOrder < 2, "passer below foreground stalls");
                        variants.Add(figure.sprite.name); count++;
                        Transform parent = figure.transform.parent.parent;
                        if (mover == null && figure.gameObject.activeInHierarchy && parent != null && parent.name == "walkerMidTransform") mover = figure;
                    }
                    Check(count >= 23 && variants.Count == 4, "native passer pools use all four approved stills");
                    foreach (string side in new[] { "Left", "Right" })
                    {
                        GameObject stall = GameObject.Find("JinSong.MarketOccluder." + side);
                        Check(stall != null && stall.GetComponent<SpriteRenderer>().sortingOrder == 2, "foreground occluder active " + side);
                        Check(stall.GetComponents<Component>().Length == 2, "stall contains only transform and renderer, no interaction " + side);
                    }
                    Check(GameObject.Find("EmporiumWall").GetComponent<SpriteRenderer>().sortingOrder > 2,
                        "native window mask clips stalls and passers");
                    Check(mover != null, "native moving middle-distance passer found");
                    pose = mover.sprite; start = mover.transform.position;
                    Debug.Log("[JINSONG_SURVEY] MARKET start=" + start + " pose=" + pose.name);
                });
                yield return Capture("50-market-entry");
                yield return new WaitForSecondsRealtime(2.5f);
                Try("fixed-pose translation", () => {
                    Check(mover != null && mover.sprite == pose, "same sprite retained across native animation ticks");
                    Check(Math.Abs(mover.transform.position.x - start.x) > .1f, "original native object translates passer horizontally");
                    Check(Math.Abs(mover.transform.position.y - start.y) < .01f, "static passer footline remains level");
                    Debug.Log("[JINSONG_SURVEY] MARKET middle=" + mover.transform.position + " pose=" + mover.sprite.name);
                });
                yield return Capture("51-market-crossing");
                yield return new WaitForSecondsRealtime(5f);
                Try("natural side departure", () => {
                    Check(mover != null && mover.sprite == pose, "no gait frame returns at side departure");
                    Debug.Log("[JINSONG_SURVEY] MARKET end=" + mover.transform.position + " active=" + mover.gameObject.activeInHierarchy);
                    Check(!mover.gameObject.activeInHierarchy || Math.Abs(mover.transform.position.x) > 1.42f,
                        "native passer leaves visible opening at a side");
                    Check(NoLoad.Blocked == 0 && NewGameData.Instance.saveSlot == -1 && runtimeErrors.Count == 0,
                        "market-only run uses memory without runtime errors");
                });
                yield return Capture("52-market-departure");
                Finish(); yield break;
            }
            if (polishOnly)
            {
                string balance = null;
                if (!gridOnly)
                {
                Try("native cash-ledger click", () => {
                    StoreCreditDisplay ledger = UnityEngine.Object.FindObjectOfType<StoreCreditDisplay>(true);
                    balance = ledger.creditDisplayText.text;
                    Check(mod.Textures.ContainsKey("jin_cash_ledger"), "new cash-ledger cover loaded");
                    ledger.OnClick();
                    Check(AdvCalendarUIManager.Instance.UIPanel.activeInHierarchy
                        && AdvCalendarUIManager.Instance.statusSection.activeInHierarchy, "original balance click opens original account status page");
                });
                yield return Capture("42-open-account-book");
                Try("account display preserved", () => {
                    Check(UnityEngine.Object.FindObjectOfType<StoreCreditDisplay>(true).creditDisplayText.text == balance,
                        "native cash balance display unchanged by opening account page");
                    AdvCalendarUIManager.Instance.ToggleUI();
                });
                }
                Try("prepare native hidden item", () => {
                    StorePoster.Instance.PullDown(true);
                    StorePoster.Instance.OnClick();
                    alcoveItem = DirectoryMaster.Item("caffeine_pill", true) as GameItemElement;
                    Check(alcoveItem != null && alcoveItem.identifier == "caffeine_pill", "real native grid-size test item");
                    Check(GraphUtils.TryAcceptAll(EmporiumEntry.Instance.invElement, alcoveItem, 1) == 0, "item first placed in normal stock grid");
                });
                yield return new WaitForSecondsRealtime(.4f);
                Vector2 normalItem = Vector2.zero;
                Try("normal item size", () => { normalItem = ScreenSize(alcoveItem.background.image.rectTransform); });
                Try("move into hidden grid", () => Check(GraphUtils.TryAcceptAll(EmporiumEntry.Instance.hiddenElement, alcoveItem, 1) == 0,
                    "native hidden inventory accepts item without changing capacity"));
                yield return new WaitForSecondsRealtime(.4f);
                Try("normal hidden cells", () => {
                    GameGridInventory normal = EmporiumEntry.Instance.invElement, hidden = EmporiumEntry.Instance.hiddenElement;
                    Vector2 normalCells = GridCells(normal), hiddenCells = GridCells(hidden);
                    Check(hiddenCells == new Vector2(2, 2), "hidden inventory remains native 2x2");
                    Vector2 a = ScreenCell(normal), b = ScreenCell(hidden);
                    Check(a.x > 1f && a.y > 1f && Math.Abs(a.x - b.x) < .6f && Math.Abs(a.y - b.y) < .6f,
                        "hidden cells have same nonzero screen dimensions as normal stock cells");
                    Vector2 inside = ScreenSize(alcoveItem.background.image.rectTransform);
                    Check(Vector2.Distance(normalItem, inside) < .8f, "same item keeps its normal displayed size after transfer");
                    Debug.Log("[JINSONG_SURVEY] GRID_SIZE normal_cell=" + a + " hidden_cell=" + b + " item_normal=" + normalItem + " item_hidden=" + inside);
                });
                yield return Capture("43-normal-hidden-grid-and-ledger");
                Try("rescaled close handle", () => {
                    GameObject close = GameObject.Find("JinSong.AlcoveClose");
                    Check(close != null, "close handle remains visible after grid rescale");
                    Canvas canvas = close.GetComponentInParent<Canvas>();
                    var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                    pointer.position = RectTransformUtility.WorldToScreenPoint(canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, close.transform.position);
                    var hits = new List<UnityEngine.EventSystems.RaycastResult>();
                    UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
                    Check(hits.Count > 0 && (hits[0].gameObject == close || hits[0].gameObject.transform.IsChildOf(close.transform)), "close handle still receives UI raycast");
                    UnityEngine.EventSystems.ExecuteEvents.Execute(close, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                    Check(!StorePoster.Instance.isPulled, "UI close invokes original mural restore");
                    Check(NoLoad.Blocked == 0 && NewGameData.Instance.saveSlot == -1 && runtimeErrors.Count == 0,
                        "polish-only run uses temporary memory without runtime errors");
                });
                Finish(); yield break;
            }
            if (streetOnly)
            {
                Try("open street window", () => StoreShutterButton.Instance.OpenShutter());
                yield return new WaitForSecondsRealtime(2f);
                SpriteRenderer walker = null;
                Vector3 first = Vector3.zero;
                Try("horizontal street binding", () => {
                    Texture2D street;
                    Check(mod.Textures.TryGetValue("jin_street_horizontal", out street) && street != null, "horizontal street asset loaded");
                });
                Try("native background and pedestrian", () => {
                    Texture2D street = mod.Textures["jin_street_horizontal"];
                    Check(street.width == 285 && street.height == 181, "new outdoor painting fits native window aperture");
                    SpriteRenderer background = GameObject.Find("store_back").GetComponent<SpriteRenderer>();
                    Check(background.sprite.name == "jinsong_poc.scene.remainder.store_background"
                        && background.sprite.rect.size == new Vector2(960, 540), "native background canvas and ownership preserved");
                    Texture2D room = GameObject.Find("store_mid").GetComponent<SpriteRenderer>().sprite.texture;
                    Check(TexturePixel(room, 480, 409).a < .01f, "baked vertical street removed from room aperture");
                    Check(TexturePixel(room, 100, 300).a > .99f, "shop wall outside aperture remains opaque");
                    foreach (SpriteRenderer figure in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>())
                    {
                        if (figure.name != "JinSong.StreetFigure" || figure.transform.parent == null
                            || figure.transform.parent.parent == null || figure.transform.parent.parent.name != "walkerMidTransform") continue;
                        walker = figure; first = figure.transform.position; break;
                    }
                    Check(walker != null && walker.enabled, "native middle-distance pedestrian visible over the road");
                    Debug.Log("[JINSONG_SURVEY] STREET_MOTION start=" + first);
                });
                yield return Capture("30-horizontal-street-a");
                yield return new WaitForSecondsRealtime(1.5f);
                yield return Capture("31-horizontal-street-b");
                Try("horizontal walking", () => {
                    Check(walker != null && Math.Abs(walker.transform.position.x - first.x) > .05f,
                        "native pedestrian moves horizontally across new street");
                    Check(walker != null && Math.Abs(walker.transform.position.y - first.y) < .15f,
                        "native pedestrian remains on a level walking band");
                    Debug.Log("[JINSONG_SURVEY] STREET_MOTION end=" + walker.transform.position);
                    Check(NoLoad.Blocked == 0 && NewGameData.Instance.saveSlot == -1 && runtimeErrors.Count == 0,
                        "street-only run uses memory without runtime errors");
                });
                Finish(); yield break;
            }
            if (!focused && !alcoveOnly) yield return Capture("02-shop");
            if (reportedIssues)
            {
                if (!alcoveOnly)
                {
                Try("open native shutter", () => StoreShutterButton.Instance.OpenShutter());
                yield return new WaitForSecondsRealtime(2f);
                Try("scene evidence", DumpReportedScene);
                Try("walker routes", CheckStreetRoutes);
                Try("counter backplates", () => {
                    foreach (PixelWindow window in new[] { EmporiumEntry.Instance.frontInvWindow, EmporiumEntry.Instance.backInvWindow })
                    {
                        TreeNodeRender background = AccessTools.Field(typeof(PixelWindow), "background").GetValue(window) as TreeNodeRender;
                        Check(background != null && background.image.color.a == 0, "trade tray has no redundant opaque board");
                    }
                });
                }
                Try("poster up", () => {
                    StorePoster.Instance.PullDown(true);
                    StorePoster.Instance.OnClick();
                    Check(StorePoster.Instance.isPulled, "native poster opens");
                    alcoveItem = DirectoryMaster.Item("caffeine_pill", true) as GameItemElement;
                    Check(alcoveItem != null && alcoveItem.identifier == "caffeine_pill"
                        && alcoveItem.spriteAtlasPath == "Items/items_medical" && alcoveItem.spritePath == "cafeine_pill"
                        && alcoveItem.unitCount == 1, "real native pill fixture rather than unknown dummy");
                    Check(GraphUtils.TryAcceptAll(EmporiumEntry.Instance.hiddenElement, alcoveItem, 1) == 0,
                        "native hidden inventory accepts test object");
                });
                yield return Capture("21-alcove-open");
                if (!alcoveOnly) Try("alcove evidence", DumpReportedUi);
                Try("stored item", () => Check(alcoveItem != null && alcoveItem.parentInventory == EmporiumEntry.Instance.hiddenElement
                    && EmporiumEntry.Instance.hiddenElement.childItems.Contains(alcoveItem)
                    && alcoveItem.background.image.isActiveAndEnabled, "stored native item visible inside compartment"));
                Try("poster close click", () => {
                    Check(GameObject.Find("JinSong.OpenAlcove").GetComponent<SpriteRenderer>().enabled, "open compartment has visible recess");
                    GameObject close = GameObject.Find("JinSong.AlcoveClose");
                    Check(close != null, "reachable close handle exists outside storage grid");
                    Canvas canvas = close.GetComponentInParent<Canvas>();
                    Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                    var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                    pointer.position = RectTransformUtility.WorldToScreenPoint(camera, close.transform.position);
                    var hits = new List<UnityEngine.EventSystems.RaycastResult>();
                    UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
                    Check(hits.Count > 0 && (hits[0].gameObject == close || hits[0].gameObject.transform.IsChildOf(close.transform)), "native UI raycast reaches close handle");
                    UnityEngine.EventSystems.ExecuteEvents.Execute(close, pointer, UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                    Check(!StorePoster.Instance.isPulled && !EmporiumEntry.Instance.hiddenWindow.rectTransform.gameObject.activeSelf,
                        "UI click invokes native poster close and hides storage");
                });
                yield return Capture("22-alcove-closed");
                Try("reopen stored object", () => {
                    StorePoster.Instance.OnClick();
                    Check(StorePoster.Instance.isPulled && alcoveItem.parentInventory == EmporiumEntry.Instance.hiddenElement,
                        "native reopen retains hidden inventory object");
                    Check(GraphUtils.TryAcceptAll(EmporiumEntry.Instance.invElement, alcoveItem, 1) == 0
                        && alcoveItem.parentInventory == EmporiumEntry.Instance.invElement
                        && !EmporiumEntry.Instance.hiddenElement.childItems.Contains(alcoveItem), "native inventory transfer takes stored item back");
                    StorePoster.Instance.OnClick();
                    Check(!StorePoster.Instance.isPulled, "second native close restores sign");
                });
                if (alcoveOnly)
                {
                    Try("alcove-only guards", () => Check(NoLoad.Blocked == 0 && NewGameData.Instance.saveSlot == -1 && runtimeErrors.Count == 0,
                        "alcove-only run uses memory without runtime errors"));
                    Finish(); yield break;
                }
                Try("revolution customer", () => {
                    PlayerStore.Instance.storeClientManager.AddNextClient(StoreClientListRev.CreateRevolutionaryBuyer());
                    PlayerStore.Instance.GetNextClient();
                    DialogUIManager.Instance.PlayClientSingleText("掌柜，我来置办些补给。");
                    StoreClient client = PlayerStore.Instance.currentClientInstance.GetClientBlueprint();
                    Debug.Log("[JINSONG_SURVEY] NAME native=" + client.displayName + " shown=" + DialogUIManager.Instance.clientNameTMP.text);
                    Check(DialogUIManager.Instance.clientNameTMP.text == "义军兵士", "revolution customer display uses approved role");
                    NegociationUIManager.Instance.OpenUI();
                });
                yield return Capture("23-negotiation");
                Try("trade chrome", DumpReportedUi);
                Try("trade paper style", () => CheckReportedPanel(GameObject.Find("CanvasPaperUI/NegotiationTablet"), "tablet"));
                Try("cached role label", () => DialogUIManager.Instance.clientNameTMP.text = "革命军士兵");
                yield return new WaitForSecondsRealtime(.5f);
                Try("cached role recovery", () => {
                    Check(DialogUIManager.Instance.clientNameTMP.text == "义军兵士", "cached speaker role normalized without changing blueprint");
                    Check(PlayerStore.Instance.currentClientInstance.GetClientBlueprint().displayName == "革命军战士", "native customer identity unchanged");
                });
                Try("close trade", () => NegociationUIManager.Instance.CloseUI());
                Try("open paid services", () => EndOfDayUIManager.Instance.OpenUI());
                yield return Capture("24-services");
                Try("service chrome", DumpReportedUi);
                Try("service paper style", () => CheckReportedPanel(GameObject.Find("CanvasPaperUI/EndOfDayTablet"), "tablet"));
                Try("close paid services", () => EndOfDayUIManager.Instance.CloseUI(false));
                Try("open morning report", () => StartOfDayUIManager.Instance.ShowMorningReport());
                yield return Capture("25-morning");
                Try("morning chrome", DumpReportedUi);
                Try("morning paper style", () => CheckReportedPanel(AdvCalendarUIManager.Instance.UIPanel, "calendar_stripped"));
                Try("close morning", () => StartOfDayUIManager.Instance.HideUI());
                Try("reported guards", () => Check(NoLoad.Blocked == 0 && NewGameData.Instance.saveSlot == -1 && runtimeErrors.Count == 0, "reported issue run uses memory only without runtime errors"));
                Finish(); yield break;
            }
            if (finishOnly)
            {
                Try("open native water guide", () => MultiPageUIManager.Instance.OpenUI(1));
                yield return Capture("11-guide-dividers");
                Try("guide decorations", CheckDividers);
                Try("close guide", () => MultiPageUIManager.Instance.CloseUI());
                SceneManager.LoadScene(0);
                yield return new WaitForSecondsRealtime(4f);
                Try("actual disable restores menu", () => mod.Instance.OnDisable());
                yield return new WaitForSecondsRealtime(.5f);
                Try("new brand restore", () => {
                    GameObject logo = GameObject.Find("Canvas").transform.Find("Logo").gameObject;
                    Check(logo.GetComponent<UnityEngine.UI.Image>().enabled, "native logo restored on actual disable");
                    Check(logo.transform.Find("JinSong.MenuBrand") == null, "owned brand removed on actual disable");
                    bool discord = false;
                    foreach (RectTransform node in GameObject.Find("Canvas").GetComponentsInChildren<RectTransform>(true))
                        if (node.name == "DiscordButton") discord |= node.gameObject.activeInHierarchy;
                    Check(discord, "original community entry restored on actual disable");
                    Check(runtimeErrors.Count == 0, "no captured Unity runtime errors");
                    Check(NoLoad.Blocked == 0, "no LoadGame calls");
                });
                Finish(); yield break;
            }
            if (chromeOnly)
            {
                Try("notebook", () => NotebookUIManager.Instance.OpenUI());
                yield return Capture("05-notebook");
                Try("notebook chrome and hit masks", () => {
                    bool covered = false;
                    foreach (UnityEngine.UI.Image image in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Image>())
                        if (image.name == "_NotebookPanel") covered |= image.overrideSprite != null && image.overrideSprite.name == "jinsong_poc.common.uibackOrange1L";
                    Check(covered, "actual orange notebook panel uses paper/wood override");
                    CheckHitMasks();
                });
                Try("no runtime errors", () => Check(runtimeErrors.Count == 0, "no captured Unity runtime errors"));
                Finish(); yield break;
            }

            Try("news fixtures", () => {
                events.Add(EventListCosmetic.RoutineSpacewalkTraining());
                events.Add(EventListCosmetic.TechDemoDay());
                events.Add(EventListNormal.CruiseShipDocked());
                events.Add(EventListSpecial.LotteryDraw());
                string[] stems = { "event_cosmetic_spacewalk", "event_cosmetic_tech_demo", "event_normal_cruise_ship", "event_lottery_draw" };
                for (int i = 0; i < events.Count; i++)
                {
                    // Simulate native cached text from the language used at event creation.
                    events[i].newsName = mod.Localization["en"]["news_source_" + stems[i] + "_news_v0"];
                    events[i].newsDescription = String.Format(mod.Localization["en"]["news_source_" + stems[i] + "_desc_v0"], "000723");
                    events[i].isDurationVisible = true;
                    events[i].duration = 2;
                }
            });
            if (!menuLanguage)
            {
                if (LocHelper.GetCurrentLocaleCode() != "zh") yield return LocHelper.SetLocale("zh");
                yield return new WaitForSecondsRealtime(1f);
                Try("Chinese actual news window", () => ShowNews("zh"));
                yield return Capture("03-news-zh");
                yield return LocHelper.SetLocale("en");
            }
            yield return new WaitForSecondsRealtime(1f);
            Try("English actual news window", () => ShowNews("en"));
            yield return Capture("04-news-en");
            Try("close news", () => NewsUIManager.Instance.CloseUI());
            if (!menuLanguage) yield return LocHelper.SetLocale(originalLocale.Identifier.Code);
            yield return new WaitForSecondsRealtime(1f);

            if (!focused)
            {
                Try("notebook", () => NotebookUIManager.Instance.OpenUI());
                yield return Capture("05-notebook");
                Try("close notebook", () => NotebookUIManager.Instance.CloseUI());
            }
            Try("water guide", () => MultiPageUIManager.Instance.OpenUI(1));
            yield return Capture("06-water-guide");
            if (menuLanguage) Try("serialized English water-guide alias", () => {
                bool city = false;
                foreach (TMP_Text text in UnityEngine.Object.FindObjectsOfType<TMP_Text>())
                    if (text.isActiveAndEnabled && text.text.Contains("Water sold in the city")) city = true;
                Check(city, "actual English guide uses authored city wording");
            });
            Try("close guide", () => MultiPageUIManager.Instance.CloseUI());
            if (menuLanguage)
            {
                SceneManager.LoadScene(0);
                yield return new WaitForSecondsRealtime(4f);
                Try("restore native language setting", () => MainMenuUIController.Instance.OnLocaleChanged(originalLanguageIndex));
                yield return new WaitForSecondsRealtime(3f);
                Try("restored original language", () => Check(LocHelper.GetCurrentLocaleCode() == originalLocale.Identifier.Code, "original language restored through native menu"));
                Finish(); yield break;
            }
            if (focused) { Finish(); yield break; }
            Try("item details", () => {
                GameItemElement item = (GameItemElement)DirectoryMaster.Item("water_purifier", true);
                string name = item.GetDisplayName();
                details = item.GetTooltipAdvanced();
                Debug.Log("[JINSONG_SURVEY] DETAILS " + details.nakedText.Replace("\n", " / "));
                Check(!String.IsNullOrEmpty(name) && details.nakedText.Contains(name), "current item-details title matches native item display");
                foreach (LoadedMod loaded in ModLoader.Mods)
                    if (loaded.Manifest.ID == "item_details") Debug.Log("[JINSONG_SURVEY] ITEM_DETAILS " + loaded.Manifest.ModVersion + " enabled=" + loaded.IsEnabled);
            });
            yield return Capture("07-item-details");
            details = null;
            Try("close tooltip", () => { if (OverlayHandler.current != null) OverlayHandler.current.HideTooltip(); });
            Try("trade header", () => {
                if (PlayerStore.Instance.currentClientInstance == null)
                {
                    PlayerStore.Instance.storeClientManager.AddNextClient(StoreClientList.CreateScavGeneral());
                    PlayerStore.Instance.GetNextClient();
                }
                NegociationUIManager.Instance.OpenUI();
            });
            yield return Capture("08-trade-window");
            Try("close trade", () => NegociationUIManager.Instance.CloseUI());
            Try("escape settings", () => UnityEngine.Object.FindObjectOfType<EscapeUIManager>(true).OpenEscapeMenu());
            yield return Capture("09-escape");
            Try("final guards", () => Check(NoLoad.Blocked == 0 && NewGameData.Instance.saveSlot == -1, "no saved shop loaded during survey"));
            Finish();
        }
        private static void DumpWorkshopLabels(GameObject root)
        {
            foreach (LocalizeStringEvent item in root.GetComponentsInChildren<LocalizeStringEvent>(true))
            {
                var targets = new List<string>();
                for (int i = 0; i < item.OnUpdateString.GetPersistentEventCount(); i++)
                {
                    var target = item.OnUpdateString.GetPersistentTarget(i);
                    if (target != null) targets.Add(target.GetType().Name + "/" + target.name + "/" + item.OnUpdateString.GetPersistentMethodName(i));
                }
                Debug.Log("[JINSONG_SURVEY] LOCALIZER " + root.name + "/" + item.name + " active=" + item.isActiveAndEnabled
                    + " table=" + item.StringReference.TableReference + " key=" + item.StringReference.TableEntryReference
                    + " id=" + item.StringReference.TableEntryReference.KeyId + " component=" + item.GetType().FullName
                    + " targets=" + String.Join(",", targets.ToArray()));
            }
            foreach (TMP_Text item in root.GetComponentsInChildren<TMP_Text>(true))
                Debug.Log("[JINSONG_SURVEY] LABEL " + root.name + "/" + item.name + " active=" + item.isActiveAndEnabled
                    + " text=" + (item.text ?? "").Replace("\n", " "));
        }

        private void ShowNews(string locale)
        {
            Check(LocHelper.GetCurrentLocaleCode() == locale, "selected runtime locale " + locale);
            NewsUIManager manager = NewsUIManager.Instance;
            manager.uiGameObject.SetActive(true);
            manager.HideAllElement();
            manager.SelectLayout(events);
            NewsUIElement[] elements = null;
            foreach (GameObject layout in new[] { manager.layout1, manager.layout2, manager.layout3, manager.layout4 })
                if (layout != null && layout.activeInHierarchy) { elements = layout.GetComponentsInChildren<NewsUIElement>(true); break; }
            Check(elements != null && elements.Length >= events.Count, "actual news layout contains required rows");
            string[] stems = { "event_cosmetic_spacewalk", "event_cosmetic_tech_demo", "event_normal_cruise_ship", "event_lottery_draw" };
            for (int i = 0; i < events.Count; i++)
            {
                string name = events[i].newsName, body = events[i].newsDescription;
                events[i].newsDescription += "\n[extra: 17 items]";
                body = events[i].newsDescription;
                elements[i].PopulateUI(events[i]);
                elements[i].gameObject.SetActive(true);
                Check(elements[i].title.gameObject.activeInHierarchy && elements[i].textContent.gameObject.activeInHierarchy,
                    "actual news row is visible " + stems[i]);
                string expectedName = mod.Localization[locale]["news_text_" + stems[i] + "_news"];
                string expectedBody = String.Format(mod.Localization[locale]["news_text_" + stems[i] + "_desc"], "000723");
                Check(Plain(elements[i].title.text) == Plain(expectedName), "actual cached news title " + locale + " " + stems[i]);
                Check(Plain(elements[i].textContent.text).Contains(Plain(expectedBody)), "actual cached news body " + locale + " " + stems[i]);
                Check(elements[i].textContent.text.Contains("[extra: 17 items]"), "native appended content retained");
                Check(events[i].newsName == name && events[i].newsDescription == body && events[i].duration == 2, "event fields unchanged by news display");
                events[i].newsDescription = body.Substring(0, body.Length - "\n[extra: 17 items]".Length);
            }
        }
        private void CheckHitMasks()
        {
            int checkedMasks = 0;
            foreach (UnityEngine.UI.Image image in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Image>())
            {
                Sprite sprite = image.overrideSprite;
                if (image.alphaHitTestMinimumThreshold <= 0 || sprite == null || !sprite.name.StartsWith("jinsong_poc.common.", StringComparison.Ordinal)) continue;
                Check(sprite.texture.isReadable, "native alpha hit mask remains readable " + image.name);
                checkedMasks++;
            }
            Debug.Log("[JINSONG_SURVEY] READABLE_MASKS " + checkedMasks);
        }
        private static Color TexturePixel(Texture2D source, int x, int y)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Texture2D pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(source, temporary); RenderTexture.active = temporary;
                pixel.ReadPixels(new Rect(x, y, 1, 1), 0, 0); pixel.Apply();
                return pixel.GetPixel(0, 0);
            }
            finally
            {
                RenderTexture.active = previous; RenderTexture.ReleaseTemporary(temporary);
                UnityEngine.Object.Destroy(pixel);
            }
        }
        private static Vector2 ScreenSize(RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector3[] corners = new Vector3[4]; rect.GetWorldCorners(corners);
            Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 b = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            return new Vector2(Math.Abs(b.x - a.x), Math.Abs(b.y - a.y));
        }
        private static Vector2 GridCells(GameGridInventory grid)
        {
            // Native Validate computes these dimensions as shape size * gridSize.
            // This avoids both the newer-language GridShape type and optional render caches.
            return new Vector2(grid.widthPixels, grid.heightPixels) / UISettings.current.gridSize;
        }
        private static Vector2 ScreenCell(GameGridInventory grid)
        {
            RectTransform rect = grid.rectTransform;
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            // Grid roots are zero-sized transform containers; measure a native cell span.
            Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(Vector3.zero));
            Vector2 b = RectTransformUtility.WorldToScreenPoint(camera,
                rect.TransformPoint(new Vector3(UISettings.current.gridSize, -UISettings.current.gridSize, 0)));
            return new Vector2(Math.Abs(b.x - a.x), Math.Abs(b.y - a.y));
        }
        private static float Light(Color value)
        {
            Func<float, float> linear = c => c <= .04045f ? c / 12.92f : (float)Math.Pow((c + .055f) / 1.055f, 2.4);
            return .2126f * linear(value.r) + .7152f * linear(value.g) + .0722f * linear(value.b);
        }
        private void CheckTraitContrast()
        {
            NewGamePerkTooltip tooltip = UnityEngine.Object.FindObjectOfType<NewGamePerkTooltip>();
            Check(tooltip != null && tooltip.IsShown, "native trait tooltip shown");
            UnityEngine.UI.Image fill = tooltip.transform.Find("Fill").GetComponent<UnityEngine.UI.Image>();
            Check(fill.color.a > .95f && Light(fill.color) > .55f, "trait tooltip uses opaque light paper");
            int labels = 0;
            foreach (TMP_Text text in tooltip.GetComponentsInChildren<TMP_Text>())
            {
                if (!text.isActiveAndEnabled || String.IsNullOrWhiteSpace(text.text)) continue;
                float a = Light(fill.color), b = Light(text.color);
                Check((Math.Max(a, b) + .05f) / (Math.Min(a, b) + .05f) >= 4.5f, "readable trait label contrast " + text.name);
                labels++;
            }
            Check(labels >= 3, "trait name cost and effect body populated");
        }
        private static string UiPath(Transform node)
        {
            string path = node.name;
            while (node.parent != null) { node = node.parent; path = node.name + "/" + path; }
            return path;
        }
        private void DumpReportedUi()
        {
            foreach (UnityEngine.UI.Image image in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Image>())
                if (image.isActiveAndEnabled)
                    Debug.Log("[JINSONG_SURVEY] ISSUE_UI " + UiPath(image.transform) + " sprite=" + (image.sprite == null ? "null" : image.sprite.name)
                        + " override=" + (image.overrideSprite == null ? "null" : image.overrideSprite.name) + " color=" + image.color + " raycast=" + image.raycastTarget);
        }
        private void DumpReportedScene()
        {
            foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>(true))
                if (renderer.gameObject.scene.buildIndex == 1)
                    Debug.Log("[JINSONG_SURVEY] ISSUE_SCENE " + UiPath(renderer.transform) + " sprite=" + (renderer.sprite == null ? "null" : renderer.sprite.name)
                        + " enabled=" + renderer.enabled + " active=" + renderer.gameObject.activeInHierarchy + " bounds=" + renderer.bounds + " color=" + renderer.color);
        }
        private void CheckReportedPanel(GameObject root, string nativeSprite)
        {
            Check(root != null && root.activeInHierarchy, "reported panel visible " + nativeSprite);
            int themed = 0;
            foreach (UnityEngine.UI.Image image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (image.sprite != null && image.sprite.name == nativeSprite)
                {
                    Check(image.overrideSprite != null && image.overrideSprite.name == "jinsong_poc.reported." + nativeSprite,
                        "actual native panel art replaced " + nativeSprite);
                    Check(image.overrideSprite.rect.size == image.sprite.rect.size, "native panel canvas preserved " + nativeSprite);
                    themed++;
                }
            Check(themed > 0, "affected panel located " + nativeSprite);
        }
        private void CheckStreetRoutes()
        {
            int walkers = 0;
            foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>(true))
            {
                Transform parent = renderer.transform.parent;
                if (renderer.gameObject.scene.buildIndex != 1 || (renderer.name != "Walker1"
                    && (parent == null || parent.name != "walkerMidTransform" && parent.name != "walkerFarTransform"))) continue;
                Check(renderer.sprite != null && renderer.sprite.name.StartsWith("jinsong_poc.street.", StringComparison.Ordinal),
                    "native street walker uses new townsman art " + renderer.name);
                walkers++;
            }
            Check(walkers >= 23, "near middle and far walker pools covered");
        }
        private void CheckBrand(string expectedTitle, string expectedCaption)
        {
            Transform logo = GameObject.Find("Canvas").transform.Find("Logo");
            Transform root = logo.Find("JinSong.MenuBrand");
            Check(root != null && root.gameObject.activeInHierarchy, "owned wood sign visible");
            Check(!logo.GetComponent<UnityEngine.UI.Image>().enabled, "original logo and baked2241 are hidden");
            Check(root.Find("Title").GetComponent<UnityEngine.UI.Text>().text == expectedTitle, "localized brand title");
            Check(root.Find("TradeCaption").GetComponent<UnityEngine.UI.Text>().text == expectedCaption, "localized trade caption without invented year");
            int hidden = 0;
            foreach (RectTransform node in GameObject.Find("Canvas").GetComponentsInChildren<RectTransform>(true))
                if (node.name == "DiscordButton" || node.name == "DiscordNote" || node.name == "Discord")
                { Check(!node.gameObject.activeInHierarchy, "community node has no active click/focus area " + node.name); hidden++; }
            Check(hidden > 0, "known community entry found and hidden");
        }
        private void CheckDividers()
        {
            int ink = 0, blank = 0;
            GameObject water = MultiPageUIManager.Instance.books[1];
            foreach (UnityEngine.UI.Image image in water.GetComponentsInChildren<UnityEngine.UI.Image>())
            {
                Sprite sprite = image.overrideSprite;
                if (sprite == null || !sprite.name.StartsWith("jinsong_poc.glyph.jin_divider_ink", StringComparison.Ordinal)) continue;
                ink++;
                if (sprite.name.EndsWith(".blank", StringComparison.Ordinal)) blank++;
            }
            Check(ink > 0, "actual guide uses dedicated divider owner");
            Check(blank > 0, "actual warning area has a transparent decorative divider");
            Debug.Log("[JINSONG_SURVEY] DIVIDERS total=" + ink + " blank_warning_or_no_room=" + blank);
        }
        private IEnumerator Capture(string phase)
        {
            yield return new WaitForSecondsRealtime(.8f);
            yield return new WaitForEndOfFrame();
            Try("capture " + phase, () => {
                Texture2D shot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                shot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                shot.Apply();
                string file = "survey-" + phase + ".png";
                ModHelper.WriteModDataFileBytes("jinsong_poc_probe", file, ImageConversion.EncodeToPNG(shot));
                UnityEngine.Object.Destroy(shot);
                Debug.Log("[JINSONG_SURVEY] CAPTURED " + file);
                foreach (TMP_Text text in UnityEngine.Object.FindObjectsOfType<TMP_Text>())
                    if (text.isActiveAndEnabled && !String.IsNullOrWhiteSpace(text.text))
                        Debug.Log("[JINSONG_SURVEY] TEXT " + phase + " " + text.name + " | " + text.text.Replace("\n", " / "));
                if (phase == "01-menu" || phase == "05-notebook")
                    foreach (UnityEngine.UI.Image image in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Image>())
                        if (image.isActiveAndEnabled && image.sprite != null)
                            Debug.Log("[JINSONG_SURVEY] CHROME " + phase + " " + image.name + " sprite=" + image.sprite.name
                                + " override=" + (image.overrideSprite == null ? "null" : image.overrideSprite.name) + " tint=" + image.color);
            });
        }
        private static Vector2 FixtureScreenPoint(RectTransform rect, Vector2 point)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera ?? Camera.main;
            return RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(point));
        }

        private static void LogFixtureHits(string name, Vector2 point)
        {
            var pointer = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
            pointer.position = point;
            var hits = new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer, hits);
            string objects = "";
            foreach (var hit in hits) objects += " " + hit.gameObject.name;
            Debug.Log("[JINSONG_SURVEY] FIXTURE " + name + " screen=" + point + " hits=" + objects);
        }

        private void DumpAudio(string phase)
        {
            foreach (AudioSource source in UnityEngine.Object.FindObjectsOfType<AudioSource>(true))
            {
                if (!source.isPlaying && !source.loop) continue;
                AudioClip clip = source.clip;
                bool ours = clip != null && mod.Sounds.ContainsValue(clip);
                Debug.Log("[JINSONG_SURVEY] AUDIO " + phase + " object=" + source.gameObject.name
                    + " clip=" + (clip == null ? "null" : clip.name) + " playing=" + source.isPlaying
                    + " loop=" + source.loop + " volume=" + source.volume + " mute=" + source.mute
                    + " mod_clip=" + ours + " length=" + (clip == null ? 0 : clip.length));
            }
        }

        private void Finish()
        {
            if (originalLocale != null) LocalizationSettings.SelectedLocale = originalLocale;
            Application.logMessageReceived -= OnLog;
            foreach (string error in runtimeErrors) Debug.Log("[JINSONG_SURVEY] RUNTIME_ERROR " + error);
            Debug.Log("[JINSONG_SURVEY] RESULT {\"outcome\":\"" + (failures == 0 ? "passed" : "failed")
                + "\",\"checks\":" + checks + ",\"phase_failures\":" + failures + ",\"runtime_errors\":" + runtimeErrors.Count
                + ",\"load_attempts\":" + NoLoad.Blocked + ",\"preload_blocked\":" + NoPreload.Blocked + ",\"save_slot\":-1}");
            Application.Quit();
        }
    }
}
