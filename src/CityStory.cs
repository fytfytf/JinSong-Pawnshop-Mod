using System;
using System.Collections.Generic;

namespace JinSongPoc
{
    internal static partial class CityStory
    {
        internal const string GrainContactTitle = "Grain Warehouse Contact";
        internal const string GrainContactBody = "Guo Shouyi, deputy grain keeper. Ask about the paper-wrapped rations from the old warehouse.";
        internal const string ArmsContactTitle = "Armory Contact";
        internal const string ArmsContactBody = "Shi Wu, armory guard. Ask about the old garrison blades he has set aside.";

        private const string Prefix = "jinsong_city_";
        private const int FirstDay = 7;
        private const int SupplyGap = 4;
        private const string IntroDay = "intro_day";
        private const string Declined = "declined";
        private const string LastVisitDay = "last_visit_day";
        private const string SongPoints = "song_points";
        private const string JinPoints = "jin_points";
        private const string GrainContact = "grain_contact";
        private const string ArmsContact = "arms_contact";

        private sealed class Lot
        {
            internal readonly string Key, Category, Portrait;
            internal readonly string[] Items;
            internal readonly int CollectionGap;
            internal Lot(string key, string category, string portrait, int collectionGap, params string[] items)
            { Key = key; Category = category; Portrait = portrait; CollectionGap = collectionGap; Items = items; }
        }

        private static readonly Lot[] Lots = {
            new Lot("grain", "FOOD", "admin", 3, "li_eat_snackbar", "li_eat_snackbar", "li_eat_snackbar"),
            new Lot("arms", "WEAPON", "sec", 4, "combat_knife", "combat_machete")
        };

        private static int Read(string key, int fallback = 0) { return ModHelper.GetModDataInt(Mod.Id, "city." + key, fallback); }
        private static void Write(string key, int value) { ModHelper.SetModDataInt(Mod.Id, "city." + key, value); }
        private static string Text(string key, params object[] args)
        {
            return Mod.CurrentText("city_" + key, args) ?? ModHelper.GetLocalized(Mod.Id, "city_" + key, args);
        }

        private static bool StoryVisit(StoreClient client)
        {
            return client != null && client.identifier != null
                && (client.identifier.StartsWith(Prefix, StringComparison.Ordinal)
                    || client.identifier.StartsWith("jinsong_assassin_", StringComparison.Ordinal));
        }

        internal static void Queue(StoreClientManager manager)
        {
            PlayerStore store = PlayerStore.Instance;
            if (!Mod.Active || store == null || store.endlessMode) return;
            OnMorning();
            int day = StoreStation.GetDayCounter();
            int lastVisit = Read(LastVisitDay);
            StoreClient active = store.currentClientInstance == null ? null : store.currentClientInstance.GetClientBlueprint();
            if (day < FirstDay || lastVisit == day || StoryVisit(active) || manager.clientStack.Exists(StoryVisit)) return;

            if (Has(SiegeDay) || Has(End))
            {
                QueueSiege(manager, day, lastVisit);
                return;
            }
            if (Has(Declined)) return;
            int introduction = Read(IntroDay);
            if (introduction == 0 || Read(GrainContact) == 0 || Read(ArmsContact) == 0)
            {
                manager.AddClient(CreateIntroduction(introduction > 0));
                return;
            }
            int daysSinceVisit = day - Math.Max(introduction, lastVisit);
            foreach (Lot lot in Lots)
            {
                if (Read(lot.Key + "_sale", -1) < 0)
                {
                    if (daysSinceVisit >= SupplyGap) manager.AddClient(CreateSeller(lot));
                    return;
                }
            }
            foreach (Lot lot in Lots)
            {
                if (Read(lot.Key + "_delivery", -1) < 0)
                {
                    if (daysSinceVisit >= lot.CollectionGap) manager.AddClient(CreateCollector(lot, Read(lot.Key + "_quote")));
                    return;
                }
            }
            if (day - lastVisit < 2) return;
            StoreClient preparation = PreparationVisit();
            if (preparation != null) manager.AddClient(preparation);
        }

        private static StoreClient CreateClient(string id, StoreClient.ClientIntent intent, string name, string portrait)
        {
            var client = new StoreClient {
                identifier = Prefix + id,
                clientIntent = intent,
                displayName = Text(name),
                spriteName = portrait,
                dismissable = true,
                nonShootable = true
            };
            StoreClientFactionSetup.InitLowerLevelCitizen(client);
            ModHelper.SetClientSource(client, Mod.Id);
            return client;
        }

        private static StoreClient FinishClient(StoreClient client)
        {
            client.AddBasicDialog();
            client.CompleteClientCreation(false);
            return client;
        }

        private static bool AtWindow(StoreClient client)
        {
            PlayerStore store = PlayerStore.Instance;
            return Mod.Active && store != null && store.currentClientInstance != null
                && store.currentClientInstance.GetClientBlueprint() == client;
        }

        private static void Dismiss(StoreClient client)
        {
            if (AtWindow(client)) PlayerStore.Instance.DismissCurrentClient(false, true, false);
        }

        private static StoreClient CreateIntroduction(bool returning)
        {
            StoreClient client = CreateClient(returning ? "contacts_return" : "intro", StoreClient.ClientIntent.DIALOGUE, "zhou_name", "merchant");
            if (returning)
            {
                Dialogue response = client.mainDialogue.SetText(client.displayName, Text("contacts_return"));
                client.OnArrival = () => {
                    Write(LastVisitDay, StoreStation.GetDayCounter());
                    DeliverContacts(client, response);
                };
                response.SetEndAction(() => Dismiss(client));
                return FinishClient(client);
            }

            Dialogue choice = client.mainDialogue.SetText(client.displayName, Text("intro"))
                .NextDialogue().SetText(client.displayName, Text("intro_offer"));
            Dialogue accept = new Dialogue().SetText("PLAYER", Text("intro_accept"));
            Dialogue responseLine = accept.NextDialogue();
            responseLine.SetEndAction(() => Dismiss(client));
            Dialogue decline = new Dialogue().SetText("PLAYER", Text("intro_decline"));
            decline.NextDialogue().SetText(client.displayName, Text("decline_reply")).SetEndAction(() => Dismiss(client));
            client.OnArrival = () => Write(LastVisitDay, StoreStation.GetDayCounter());
            choice.AddChoice(DirectoryMaster.Item("$", true), "no choice", "", accept, () => {
                if (!AtWindow(client) || Read(Declined) != 0) return;
                if (Read(IntroDay) == 0)
                {
                    Write("version", 1);
                    Write(SongPoints, 0);
                    Write(JinPoints, 6);
                    Write(IntroDay, StoreStation.GetDayCounter());
                }
                DeliverContacts(client, responseLine);
            }, null);
            choice.AddChoice(DirectoryMaster.Item("$", true), "no choice", "", decline, () => {
                if (AtWindow(client) && Read(IntroDay) == 0) Write(Declined, 1);
            }, null);
            return FinishClient(client);
        }

        private static void DeliverContacts(StoreClient client, Dialogue response)
        {
            DeliverContact(GrainContact, GrainContactTitle, GrainContactBody);
            DeliverContact(ArmsContact, ArmsContactTitle, ArmsContactBody);
            Write(LastVisitDay, StoreStation.GetDayCounter());
            response.SetText(client.displayName, Text(Read(GrainContact) != 0 && Read(ArmsContact) != 0
                ? "contacts_delivered" : "contacts_no_room"));
        }

        private static void DeliverContact(string key, string title, string body)
        {
            if (Read(key) != 0) return;
            GameItem note = PreBuiltItemHelper.CreateHandnote(body, title, "", 1);
            if (PlayerStore.Instance.TryAddDirectSellingItemToTable(note, true, false, true, 0)) Write(key, 1);
        }

        private static StoreClient CreateSeller(Lot lot)
        {
            string resultKey = lot.Key + "_sale";
            StoreClient client = CreateClient(resultKey, StoreClient.ClientIntent.SELL, lot.Key + "_seller", lot.Portrait);
            var trade = new WindowTrade();
            var stock = new HashSet<GameItem>();
            Dialogue quote = client.mainDialogue.SetText(client.displayName, Text(lot.Key + "_seller_line")).NextDialogue();
            client.OnArrival = () => {
                if (trade.Arrived) return;
                trade.Arrived = true;
                Write(LastVisitDay, StoreStation.GetDayCounter());
                if (Read(resultKey, -1) >= 0) return;
                foreach (string id in lot.Items)
                {
                    GameItem item = DirectoryMaster.Item(id, true);
                    if (!PlayerStore.Instance.TryAddDirectSellingItemToTable(item, false, true, false, 0)) continue;
                    stock.Add(item);
                    trade.OpeningValue += Math.Max(0, item.GetNegociatedValue());
                }
                quote.SetText(client.displayName, trade.OpeningValue > 0
                    ? Text("seller_quote", trade.OpeningValue) : Text("seller_no_room"));
            };
            var acquired = new List<int>();
            client.onItemSold = (item, price) => {
                if (!stock.Remove(item)) return;
                trade.Record(price);
                if (price > 0) acquired.Add(item.GetUniqueID());
            };
            client.OnDismiss = () => {
                if (!trade.Arrived || trade.OpeningValue <= 0 || Read(resultKey, -1) >= 0) return;
                Write(LastVisitDay, StoreStation.GetDayCounter());
                Write(lot.Key + "_quote", (int)Math.Min(int.MaxValue, trade.OpeningValue));
                Write(resultKey, trade.Passed ? 1 : 0);
                if (trade.Passed)
                {
                    Write(JinPoints, Read(JinPoints, 6) - 2);
                    Write(lot.Key + "_stock_count", acquired.Count);
                    for (int i = 0; i < acquired.Count; i++) Write(lot.Key + "_stock_" + i, acquired[i]);
                }
            };
            return FinishClient(client);
        }

        private static StoreClient CreateCollector(Lot lot, int budget)
        {
            string resultKey = lot.Key + "_delivery";
            StoreClient client = CreateClient(resultKey, StoreClient.ClientIntent.BUY, "zhou_name", "merchant");
            var trade = new WindowTrade { OpeningValue = budget };
            client.clientBuyingTagList.Add(lot.Category);
            client.SetBudget(budget);
            client.OverrideBudget(budget);
            client.mainDialogue.SetText(client.displayName, Text(lot.Key + "_collect", budget));
            client.OnArrival = () => {
                trade.Arrived = true;
                Write(LastVisitDay, StoreStation.GetDayCounter());
            };
            client.onItemBought = (item, price) => {
                if (trade.Arrived && item.IsGameItemType(lot.Category)) trade.Record(price);
            };
            client.OnDismiss = () => {
                if (!trade.Arrived || Read(resultKey, -1) >= 0) return;
                Write(LastVisitDay, StoreStation.GetDayCounter());
                Write(resultKey, trade.Passed ? 1 : 0);
                if (trade.Passed) Write(SongPoints, Read(SongPoints) + 2);
            };
            return FinishClient(client);
        }
    }
}
