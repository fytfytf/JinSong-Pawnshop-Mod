using System;
using System.Collections.Generic;

namespace JinSongPoc
{
    internal static class AssassinStory
    {
        internal const string FoodClient = "jinsong_assassin_food";
        internal const string NoticeClient = "jinsong_assassin_notice";
        internal const string LootClient = "jinsong_assassin_loot";
        internal const string WeaponClient = "jinsong_assassin_weapon";
        internal const string MedicineClient = "jinsong_assassin_medicine";
        internal const string NoticeTitle = "Wanted Notice from the Jin Garrison";
        internal const string NoticeBody = "A man posing as a travelling merchant is wanted in connection with an attack on a Jin supply wagon. He wears a dark travelling coat and speaks with a Song accent. Shopkeepers who see him or find goods bearing the garrison seal should inform the gate guard.";

        private const int FirstDay = 17;
        private const int FoodBudget = 1000;
        private const int WeaponBudget = 1000;
        private const int MedicineBudget = 500;
        private const string FoodDay = "assassin.food_day";
        private const string FoodPassed = "assassin.food_passed";
        private const string NoticeDay = "assassin.notice_day";
        private const string NoticeAttemptDay = "assassin.notice_attempt_day";
        private const string LootPassed = "assassin.loot_passed";
        private const string LootDay = "assassin.loot_day";
        private const string WeaponDay = "assassin.weapon_day";
        private const string WeaponPassed = "assassin.weapon_passed";
        private const string OutcomeDay = "assassin.outcome_day";
        private const string OutcomeKey = "assassin.outcome";
        private const string NewsPublished = "assassin.news_published";
        private const string NewsPrefix = "jinsong_assassin_news_";

        private enum Ending { None, KillEscape, KillCaught, SurviveEscape, SurviveCaught, Reported }

        private sealed class Trade
        {
            internal bool Arrived;
            internal long OpeningValue;
            internal long Paid;

            internal void Record(int price)
            {
                if (price > 0) Paid += price;
            }

            internal bool Passed { get { return OpeningValue > 0 && Paid * 100 >= OpeningValue * 66; } }
        }

        internal static void Queue(StoreClientManager manager)
        {
            PlayerStore store = PlayerStore.Instance;
            if (!Mod.Active || store == null || store.endlessMode) return;
            if (Read(OutcomeDay) != 0)
            {
                PublishNews();
                return;
            }
            int day = StoreStation.GetDayCounter();
            int foodDay = Read(FoodDay);
            int noticeDay = Read(NoticeDay);
            int lootPassed = Read(LootPassed, -1);
            int lootDay = Read(LootDay);
            int weaponDay = Read(WeaponDay);
            if (foodDay == 0 && day >= FirstDay && !AlreadyPresent(store, manager, FoodClient))
                manager.AddClient(CreateFoodBuyer());
            else if (foodDay > 0 && noticeDay == 0 && day > foodDay && day > Read(NoticeAttemptDay)
                     && !AlreadyPresent(store, manager, NoticeClient))
                manager.AddNextClient(CreateNoticeGuard());
            else if (lootPassed < 0 && noticeDay > 0 && day > noticeDay && !AlreadyPresent(store, manager, LootClient))
                manager.AddClient(CreateLootSeller(Read(FoodPassed) == 1));
            // Alpha259 recorded the result without a date. Its completed visits qualify at the next generation.
            else if (lootPassed >= 0 && weaponDay == 0 && (lootDay == 0 || day >= lootDay + 3)
                     && !AlreadyPresent(store, manager, WeaponClient))
                manager.AddClient(CreateWeaponBuyer(lootPassed == 1));
            else if (weaponDay > 0 && day > weaponDay && !AlreadyPresent(store, manager, MedicineClient))
                manager.AddNextClient(CreateMedicineBuyer(Read(WeaponPassed) == 1));
        }

        private static bool AlreadyPresent(PlayerStore store, StoreClientManager manager, string id)
        {
            StoreClient active = store.currentClientInstance == null ? null : store.currentClientInstance.GetClientBlueprint();
            return (active != null && active.identifier == id) || manager.clientStack.Exists(client => client.identifier == id);
        }

        private static int Read(string key, int fallback = 0) { return ModHelper.GetModDataInt(Mod.Id, key, fallback); }
        private static void Write(string key, int value) { ModHelper.SetModDataInt(Mod.Id, key, value); }

        private static string Line(string key, string fallback, params object[] args)
        {
            return Mod.CurrentText("assassin_" + key, args) ?? String.Format(fallback, args);
        }

        private static StoreClient CreateClient(string id, StoreClient.ClientIntent intent, bool guard = false)
        {
            var client = new StoreClient {
                identifier = id,
                clientIntent = intent,
                displayName = guard ? Line("guard", "Jin Garrison Guard") : Line("merchant", "Travelling Merchant"),
                spriteName = guard ? SpriteDict.GetRandomSecSpriteName() : SpriteDict.GetRandomLowerLevelSpriteName(),
                dismissable = true,
                nonShootable = true
            };
            if (guard) client.bubbleColor = "sec";
            else StoreClientFactionSetup.InitLowerLevelCitizen(client);
            ModHelper.SetClientSource(client, Mod.Id);
            return client;
        }

        private static StoreClient CreateFoodBuyer()
        {
            StoreClient client = CreateBuyer(FoodClient, FoodBudget, "FOOD", null, passed => {
                if (Read(FoodDay) != 0) return;
                Write(FoodPassed, passed ? 1 : 0);
                Write(FoodDay, StoreStation.GetDayCounter());
            });
            client.mainDialogue.SetText(client.displayName, Line("food_1", "Shopkeeper, do you have any food that will keep a few days? I came through the gate this morning."))
                .NextDialogue().SetText(client.displayName, Line("food_2", "I can spend {0}. Wrap up what you have. I may be staying here a while.", FoodBudget));
            client.AddBasicDialog();
            client.CompleteClientCreation(false);
            return client;
        }

        private static StoreClient CreateBuyer(string id, int budget, string category, string excluded, Action<bool> settle)
        {
            StoreClient client = CreateClient(id, StoreClient.ClientIntent.BUY);
            var trade = new Trade { OpeningValue = budget };
            client.clientBuyingTagList.Add(category);
            if (excluded != null) client.clientBlackTagList.Add(excluded);
            client.SetBudget(budget);
            client.OverrideBudget(budget);
            client.OnArrival = () => trade.Arrived = true;
            client.onItemBought = (item, price) => {
                if (item.IsGameItemType(category) && (excluded == null || !item.IsGameItemType(excluded)))
                    trade.Record(price);
            };
            client.OnDismiss = () => { if (trade.Arrived) settle(trade.Passed); };
            return client;
        }

        private static StoreClient CreateNoticeGuard()
        {
            StoreClient client = CreateClient(NoticeClient, StoreClient.ClientIntent.DIALOGUE, true);
            client.mainDialogue.SetText(client.displayName, Line("notice_1", "A garrison supply wagon was hit last night. We are handing this notice to every shop on the street."));
            Dialogue delivery = client.mainDialogue.NextDialogue();
            client.OnArrival = () => {
                int day = StoreStation.GetDayCounter();
                if (Read(NoticeDay) != 0 || Read(NoticeAttemptDay) == day) return;
                Write(NoticeAttemptDay, day);
                GameItem note = PreBuiltItemHelper.CreateHandnote(NoticeBody, NoticeTitle, "", 1);
                PlayerStore store = PlayerStore.Instance;
                bool delivered = store.TryAddDirectSellingItemToTable(note, true, false, true, 0);
                if (delivered) Write(NoticeDay, day);
                delivery.SetText(client.displayName, delivered
                    ? Line("notice_2", "Read this. If the man comes in, or you see the garrison seal on any goods, tell the gate guard.")
                    : Line("notice_full", "There is no room on your counter. Clear a space for the notice and I will come back tomorrow."));
            };
            delivery.SetEndAction(() => DismissAfterDialogue(client));
            client.AddBasicDialog();
            client.CompleteClientCreation(false);
            return client;
        }

        private static StoreClient CreateLootSeller(bool provisioned)
        {
            StoreClient client = CreateClient(LootClient, StoreClient.ClientIntent.SELL);
            var trade = new Trade();
            var stock = new HashSet<GameItem>();
            string[] lot = provisioned
                ? new[] { "system_module_efficiancy", "system_module_quality", "system_module_performance" }
                : new[] { "system_module_efficiancy" };
            client.mainDialogue.SetText(client.displayName, provisioned
                ? Line("loot_full", "You remember me. The food you sold me kept me off the streets long enough to learn their route. I got away with these fittings.")
                : Line("loot_small", "You remember me. I had to move before I knew their route. I barely got away with this fitting."));
            Dialogue offer = client.mainDialogue.NextDialogue().SetText(client.displayName,
                Line("loot_reason", "Yes, that is the Jin garrison seal. Their quartermaster burned our crossing. My brother was there."));
            Dialogue priceLine = offer.NextDialogue();
            client.OnArrival = () => {
                if (trade.Arrived) return;
                trade.Arrived = true;
                PlayerStore store = PlayerStore.Instance;
                foreach (string id in lot)
                {
                    GameItem item = DirectoryMaster.Item(id, true);
                    if (!store.TryAddDirectSellingItemToTable(item, false, true, false, 0)) continue;
                    stock.Add(item);
                    trade.OpeningValue += Math.Max(0, item.GetNegociatedValue());
                }
                priceLine.SetText(client.displayName, trade.OpeningValue > 0
                    ? Line("loot_price", "The lot on your counter comes to {0}. I need the money to find someone who knows the quartermaster's movements.", trade.OpeningValue)
                    : Line("loot_no_room", "There is no room on the counter. I will take the goods with me."));
            };
            client.onItemSold = (item, price) => { if (stock.Remove(item)) trade.Record(price); };
            client.OnDismiss = () => {
                if (!trade.Arrived || Read(LootPassed, -1) >= 0) return;
                Write(LootDay, StoreStation.GetDayCounter());
                Write(LootPassed, trade.Passed ? 1 : 0);
            };
            client.AddBasicDialog();
            client.CompleteClientCreation(false);
            return client;
        }

        private static StoreClient CreateWeaponBuyer(bool foundInformant)
        {
            StoreClient client = CreateBuyer(WeaponClient, WeaponBudget, "WEAPON", null, passed => {
                if (Read(WeaponDay) != 0 || Read(OutcomeDay) != 0) return;
                Write(WeaponPassed, passed ? 1 : 0);
                Write(WeaponDay, StoreStation.GetDayCounter());
            });
            Dialogue decision = client.mainDialogue.SetText(client.displayName, foundInformant
                ? Line("weapon_informant", "The money bought me a word from a stable hand. I know where the quartermaster will pass.")
                : Line("weapon_alone", "I came up short. No one would tell me his route. I'll have to follow him myself."))
                .NextDialogue().SetText(client.displayName,
                    Line("weapon_request", "I'm going after the man who burned the crossing. I have {0} for weapons. Will you sell to me?", WeaponBudget));
            Dialogue continueTrade = new Dialogue().SetText("PLAYER", Line("weapon_continue", "[Continue trading]"));
            Dialogue report = new Dialogue().SetText("PLAYER", Line("weapon_report", "[Report him to the Jin garrison]"));
            report.NextDialogue().SetText(client.displayName,
                Line("weapon_report_response", "So the soldiers at the door are here for me. Tell them my name is Shen Li."))
                .SetEndAction(() => DismissAfterDialogue(client));
            decision.AddChoice(DirectoryMaster.Item("$", true), "no choice", "", continueTrade, null, null);
            decision.AddChoice(DirectoryMaster.Item("$", true), "no choice", "", report, () => {
                if (IsAtWindow(client)) Finish(Ending.Reported);
            }, null);
            client.AddBasicDialog();
            client.CompleteClientCreation(false);
            return client;
        }

        private static bool IsAtWindow(StoreClient client)
        {
            PlayerStore store = PlayerStore.Instance;
            return store != null && store.currentClientInstance != null && store.currentClientInstance.GetClientBlueprint() == client;
        }

        private static void DismissAfterDialogue(StoreClient client)
        {
            // Let the active conversation finish processing its own end action.
            if (IsAtWindow(client)) PlayerStore.Instance.DismissCurrentClient(false, true, false);
        }

        private static StoreClient CreateMedicineBuyer(bool killed)
        {
            StoreClient client = CreateBuyer(MedicineClient, MedicineBudget, "MEDICAL", "NARCOTIC", passed => {
                if (killed) Finish(passed ? Ending.KillEscape : Ending.KillCaught);
                else Finish(passed ? Ending.SurviveEscape : Ending.SurviveCaught);
            });
            client.mainDialogue.SetText(client.displayName, killed
                ? Line("medicine_killed", "The quartermaster is dead. One of his guards got me in the side. They are closing the gates.")
                : Line("medicine_survived", "The quartermaster got away. His guards struck me before I could reach him. They are searching the streets."))
                .NextDialogue().SetText(client.displayName,
                    Line("medicine_request", "I've {0} left for dressings and medicine. I have to stop the bleeding before I try the gate.", MedicineBudget));
            client.AddBasicDialog();
            client.CompleteClientCreation(false);
            return client;
        }

        private static void Finish(Ending ending)
        {
            if (Read(OutcomeDay) != 0) return;
            Write(OutcomeKey, (int)ending);
            Write(OutcomeDay, StoreStation.GetDayCounter());
        }

        private static string NewsKey(Ending ending)
        {
            switch (ending)
            {
                case Ending.KillEscape: return "kill_escape";
                case Ending.KillCaught: return "kill_caught";
                case Ending.SurviveEscape: return "survive_escape";
                case Ending.SurviveCaught: return "survive_caught";
                case Ending.Reported: return "reported";
                default: return null;
            }
        }

        internal static void PublishNews()
        {
            if (!Mod.Active || PlayerStore.Instance == null || StoreStation.Instance == null) return;
            int outcomeDay = Read(OutcomeDay);
            if (outcomeDay == 0 || StoreStation.GetDayCounter() <= outcomeDay || Read(NewsPublished) != 0) return;
            string key = NewsKey((Ending)Read(OutcomeKey));
            StoreEventManager manager = StoreStation.Instance.storeEventManager;
            if (key == null || manager == null) return;
            string id = NewsPrefix + key;
            if (!manager.GetActiveEvents().Exists(e => e.identifier == id))
            {
                string title = Line("news_" + key + "_title", "Garrison dispatch");
                manager.ActivateEvent(new StoreEvent {
                    identifier = id,
                    newsName = title,
                    displayName = title,
                    newsDescription = Line("news_" + key + "_body", "The garrison has issued a dispatch concerning the attack."),
                    duration = 1,
                    importance = 4,
                    eventArea = StoreEvent.EventArea.ALL,
                    eventType = StoreEvent.EventType.COSMETIC
                }, false, false);
            }
            Write(NewsPublished, 1);
        }

        internal static string LocalizedNews(StoreEvent news, bool title)
        {
            return news.identifier != null && news.identifier.StartsWith(NewsPrefix, StringComparison.Ordinal)
                ? Mod.CurrentText("assassin_news_" + news.identifier.Substring(NewsPrefix.Length) + (title ? "_title" : "_body"))
                : null;
        }
    }
}
