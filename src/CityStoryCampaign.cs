using System;
using System.Collections.Generic;
using HarmonyLib;

namespace JinSongPoc
{
    internal static partial class CityStory
    {
        private const string SiegeDay = "siege_day", End = "ending", EndDay = "ending_day";
        private const string NewsPrefix = "jinsong_city_news_";
        private const int ArmsBribe = 40, GrainBribe = 30;
        private static bool showingExecution;
        internal static bool ShowingExecution { get { return showingExecution; } }

        private sealed class Paper
        {
            internal readonly string Key, Title, Body;
            internal Paper(string key, string title, string body) { Key = key; Title = title; Body = body; }
        }

        private static readonly Paper Receipt = new Paper("receipt", "Armory receipt", "Shi Wu signed for the blades removed from the western racks.");
        private static readonly Paper Ledger = new Paper("ledger", "Old warehouse ledger leaf", "The missing sacks were crossed off against a false loss in transit.");
        private static readonly Paper Pledge = new Paper("pledge", "Field commander's pledge", "Men who stand down and shelter the townspeople will keep their lives.");
        private static readonly Paper Roster = new Paper("roster", "Watch rotation", "The relief watch is to take the side gate at the third drum.");
        private static readonly Paper Dispatch = new Paper("dispatch", "Outer camp dispatch", "Du Jiu is to bring word from the rendezvous beyond the east market.");
        private static readonly Paper Warrant = new Paper("warrant", "Warrant of execution", "The shopkeeper is condemned on the evidence entered in the garrison case.");
        private static readonly Paper SongPaper = new Paper("song_award", "Order to reopen the market", "The Song command orders the market reopened and records the help of this shop.");
        private static readonly Paper JinPaper = new Paper("jin_award", "Citation of the Circuit Inspectorate", "The Inspectorate commends the shop for bringing the supply and infiltration case to a close.");
        private static readonly Paper Permit = new Paper("permit", "Warehouse passage permit", "Wanyan Su allows the bearer to move grain under seal during the curfew.");
        private static readonly Paper[] Papers = { Receipt, Ledger, Pledge, Roster, Dispatch, Warrant, SongPaper, JinPaper, Permit };
        private static readonly string[] Cases = { "grain", "arms", "inside", "outside" };

        private sealed class Option
        {
            internal readonly string Key;
            internal readonly Func<string> Take;
            internal Option(string key, Func<string> take) { Key = key; Take = take; }
        }

        private static bool Has(string key) { return Read(key) > 0; }
        private static int Day { get { return StoreStation.GetDayCounter(); } }
        private static bool Done(string visit) { return Has("visit_" + visit); }
        private static bool Siege { get { return Has(SiegeDay) && !Has(End); } }
        private static Option Pick(string key, Func<string> action) { return new Option(key, action); }

        private static bool AddOnce(string fact, string score, int delta)
        {
            if (Has(fact)) return false;
            Write(fact, 1);
            Write(score, Read(score, score == JinPoints ? 6 : 0) + delta);
            return true;
        }

        private static void Close(string scene) { Write("visit_" + scene, Day); }

        private static StoreClient Visit(string scene, string name, string portrait, string line, params Option[] options)
        {
            StoreClient client = CreateClient(scene, StoreClient.ClientIntent.DIALOGUE, name, portrait);
            client.OnArrival = () => Write(LastVisitDay, Day);
            Dialogue choice = client.mainDialogue.SetText(client.displayName, Text(line));
            foreach (Option option in options)
            {
                Option selected = option;
                Dialogue next = new Dialogue().SetText("PLAYER", Text(selected.Key));
                Dialogue response = next.NextDialogue();
                response.SetEndAction(() => Dismiss(client));
                choice.AddChoice(DirectoryMaster.Item("$", true), "no choice", "", next, () => {
                    if (!AtWindow(client) || Done(scene)) return;
                    string reply = selected.Take();
                    if (reply != null) Close(scene);
                    response.SetText(client.displayName, Text(reply ?? "retry"));
                }, null);
            }
            return FinishClient(client);
        }

        private static StoreClient Purchase(string scene, string name, string portrait, string line,
            int budget, Func<GameItem, bool> eligible, Action<bool> settle)
        {
            StoreClient client = CreateClient(scene, StoreClient.ClientIntent.BUY, name, portrait);
            var trade = new WindowTrade { OpeningValue = budget };
            client.willAcceptItem = eligible;
            client.SetBudget(budget);
            client.OverrideBudget(budget);
            client.mainDialogue.SetText(client.displayName, Text(line, budget));
            client.OnArrival = () => { trade.Arrived = true; Write(LastVisitDay, Day); };
            client.onItemBought = (item, price) => {
                if (trade.Arrived && eligible(item)) trade.Record(price);
            };
            client.OnDismiss = () => {
                if (!trade.Arrived || Done(scene)) return;
                Close(scene);
                settle(trade.Passed);
            };
            return FinishClient(client);
        }

        private static bool Food(GameItem item) { return item.IsGameItemType("FOOD"); }
        private static bool Medicine(GameItem item) { return item.IsGameItemType("MEDICAL") && !item.IsGameItemType("NARCOTIC"); }

        private static bool Give(Paper paper)
        {
            if (Has("paper_" + paper.Key)) return true;
            GameItem note = PreBuiltItemHelper.CreateHandnote(paper.Body, paper.Title, "", 1);
            if (!PlayerStore.Instance.TryAddDirectSellingItemToTable(note, true, false, true, 0)) return false;
            Write("paper_" + paper.Key, 1);
            return true;
        }

        private static GameItem OwnedPaper(Paper paper)
        {
            if (EmporiumEntry.Instance == null) return null;
            return EmporiumEntry.Instance.GetAllOwnedItems(true, true).Find(item => item.identifier == "handnote"
                && item.GetTagReadonly("HANDNOTE_TITLE").GetString() == paper.Title);
        }

        private static bool HandOver(Paper paper)
        {
            GameItem item = OwnedPaper(paper);
            if (item == null) return false;
            PlayerStore.Instance.RemoveItemFromStoreInventory(new List<GameItem> { item });
            return OwnedPaper(paper) == null;
        }

        internal static string PaperText(string value, bool body)
        {
            foreach (Paper paper in Papers)
                if (value == (body ? paper.Body : paper.Title))
                    return Mod.CurrentText("city_paper_" + paper.Key + (body ? "_body" : "_title"));
            return null;
        }

        private static StoreClient ReceiptVisit()
        {
            return Visit("receipt", "arms_seller", "sec", "receipt_line",
                Pick("pay_bribe", () => {
                    PlayerStore store = PlayerStore.Instance;
                    if (store.playerCash < ArmsBribe || !Give(Receipt)) return null;
                    store.ModCash(-ArmsBribe, true);
                    Write("bribed_shi", 1);
                    return "receipt_paid";
                }), Pick("pass", () => "receipt_kept"));
        }

        private static StoreClient GrainKeeper()
        {
            return Visit("guo", "grain_seller", "admin", "guo_line",
                Pick("protect_family", () => { Write("guo_agreed", 1); return "guo_agrees"; }),
                Pick("pay_guo", () => {
                    PlayerStore store = PlayerStore.Instance;
                    if (store.playerCash < GrainBribe) return null;
                    store.ModCash(-GrainBribe, true);
                    Write("guo_agreed", 1);
                    return "guo_paid";
                }),
                Pick("pass", () => "guo_leaves"));
        }

        private static StoreClient LedgerVisit()
        {
            return Visit("ledger", "xu_name", "shadyMerchant", "ledger_line",
                Pick("accept_ledger", () => Give(Ledger) ? "ledger_accept_reply" : null),
                Pick("pass", () => "ledger_decline_reply"));
        }

        private static StoreClient ZhouPledge()
        {
            return Visit("pledge", "zhou_name", "merchant", Has("han_fed") ? "pledge_line" : "pledge_line_cold",
                Pick("take_pledge", () => Give(Pledge) ? "pledge_given" : null),
                Pick("give_zhou_ledger", () => {
                    if (!Give(Pledge)) return null;
                    if (!HandOver(Ledger)) return "pledge_given";
                    Write("ledger_route", 1);
                    return "ledger_to_zhou";
                }), Pick("pass", () => "no_pledge"));
        }

        private static StoreClient HanAnswer()
        {
            return Visit("han_answer", "han_name", "officer_lun", "han_answer_line",
                Pick("give_pledge", () => {
                    if (!Has("han_fed") || OwnedPaper(Pledge) == null) return "han_declines";
                    if (!Give(Roster) || !HandOver(Pledge)) return null;
                    AddOnce("han_joined", SongPoints, 3);
                    return "han_joins";
                }),
                Pick("take_watch_roster", () => Give(Roster) ? "han_roster_only" : null),
                Pick("pass", () => "han_declines"));
        }

        private static StoreClient ScoutReturn()
        {
            return Visit("scout_return", "wei_name", "sec", Has("scout_aided") ? "scout_recovered" : "scout_lost",
                Pick("take_dispatch", () => {
                    if (!Give(Dispatch)) return null;
                    if (Has("scout_aided")) AddOnce("scout_rejoined", SongPoints, 3);
                    return "dispatch_given";
                }),
                Pick("pass", () => {
                    if (Has("scout_aided")) AddOnce("scout_rejoined", SongPoints, 3);
                    return "scout_leaves";
                }));
        }

        private static StoreClient Wanyan()
        {
            return Visit("wanyan", "wanyan_name", "admin", "wanyan_line",
                Pick("show_ledger_wanyan", () => {
                    if (OwnedPaper(Ledger) == null) return "wanyan_unproved";
                    if (!Give(Permit) || !HandOver(Ledger)) return null;
                    Write("ledger_route", 2);
                    AddOnce("wanyan_refused", JinPoints, -2);
                    return "wanyan_refuses";
                }), Pick("pass", () => "wanyan_orders"));
        }

        private static StoreClient RivalGrain()
        {
            StoreClient client = CreateClient("rival_grain", StoreClient.ClientIntent.DIALOGUE, "han_name", "officer_lun");
            client.OnArrival = () => Write(LastVisitDay, Day);
            Dialogue choice = client.mainDialogue.SetText(client.displayName, Text("rival_han"))
                .NextDialogue().SetText(Text("zhou_name"), Text("rival_zhou"));
            RivalChoice(client, choice, "grain_to_han", 1);
            RivalChoice(client, choice, "grain_to_zhou", 2);
            RivalChoice(client, choice, "pass", 3);
            return FinishClient(client);
        }

        private static void RivalChoice(StoreClient client, Dialogue choice, string key, int buyer)
        {
            Dialogue response = new Dialogue().SetText("PLAYER", Text(key));
            response.NextDialogue().SetText(Text(buyer == 2 ? "zhou_name" : "han_name"), Text("rival_reply_" + buyer))
                .SetEndAction(() => Dismiss(client));
            choice.AddChoice(DirectoryMaster.Item("$", true), "no choice", "", response, () => {
                if (!AtWindow(client) || Done("rival_grain")) return;
                Write("rival_buyer", buyer);
                Close("rival_grain");
            }, null);
        }

        private static bool OriginalStock(Lot lot, GameItem item)
        {
            int count = Read(lot.Key + "_stock_count");
            for (int i = 0; i < count; i++)
                if (Read(lot.Key + "_stock_" + i, -1) == item.uniqueId) return true;
            return false;
        }

        private static StoreClient Requisition(Lot lot)
        {
            bool grain = lot.Key == "grain";
            string scene = grain ? "requisition" : "audit";
            string name = grain ? "wanyan_name" : "arms_seller";
            string portrait = grain ? "admin" : "sec";
            if (grain && Has("wanyan_refused"))
                return Visit(scene, name, portrait, "requisition_refused", Pick("ack", () => "requisition_refused_reply"));
            int budget = Math.Max(80, Read(lot.Key + "_quote"));
            return Purchase(scene, name, portrait, grain ? "requisition_line" : "audit_line", budget,
                item => OriginalStock(lot, item), passed => {
                    if (passed && Has(lot.Key + "_sale"))
                        AddOnce(lot.Key + "_recovered", JinPoints, 2);
                });
        }

        private static bool CanCommission()
        {
            return CityRules.CanCommission(Read("reports"), StoreReputation.GetSecReputation(),
                OwnedPaper(Ledger) != null || OwnedPaper(Receipt) != null || Has("commission"));
        }

        private static StoreClient Commission(string scene)
        {
            return Visit(scene, "pucha_name", "sec", "commission_line",
                Pick("join_inquiry", () => { Write("commission", 1); return "commission_yes"; }),
                Pick("pass", () => "commission_no"));
        }

        private static void Prove(string file, bool witness)
        {
            AddOnce("proof_" + file, JinPoints, 2);
            if (witness) AddOnce("witness_" + file, JinPoints, 2);
        }

        private static bool Verified(string file) { return Has("proof_" + file) && Has("witness_" + file); }

        private static void BreakSupport(string fact, int amount)
        {
            if (Has(fact)) AddOnce(fact + "_broken", SongPoints, -amount);
        }

        private static string GiveEvidence(string file, Paper paper, bool witness)
        {
            if (!HandOver(paper)) return "evidence_missing";
            Prove(file, witness);
            if (paper == Ledger) Write("ledger_route", 3);
            if (Done("sweep")) BreakVerifiedSupport();
            return witness ? "evidence_verified" : "evidence_proof_only";
        }

        private static StoreClient Inquiry(string scene)
        {
            var options = new List<Option>();
            if (Has("commission"))
            {
                if (!Verified("grain") && OwnedPaper(Ledger) != null)
                    options.Add(Pick("give_supply_ledger", () => GiveEvidence("grain", Ledger, Has("guo_agreed"))));
                if (!Verified("arms") && OwnedPaper(Receipt) != null)
                    options.Add(Pick("give_supply_receipt", () => GiveEvidence("arms", Receipt, Has("bribed_shi"))));
                if (!Verified("inside") && OwnedPaper(Roster) != null)
                    options.Add(Pick("give_roster", () => GiveEvidence("inside", Roster, Done("han_answer"))));
                if (!Verified("outside") && OwnedPaper(Dispatch) != null)
                    options.Add(Pick("give_dispatch", () => GiveEvidence("outside", Dispatch, Done("scout_return"))));
                if (Offense() && CountVerified() >= 2)
                    options.Add(Pick("account_for_goods", () => {
                        foreach (string file in Cases)
                            if (Involvement(file) && Verified(file)) Write("cleared_" + file, 1);
                        return "account_accepted";
                    }));
            }
            options.Add(Pick("pass", () => "inquiry_wait"));
            return Visit(scene, "pucha_name", "sec", "inquiry_line", options.ToArray());
        }

        private static bool HasNewEvidence()
        {
            return (!Has("proof_grain") && OwnedPaper(Ledger) != null)
                || (!Has("proof_arms") && OwnedPaper(Receipt) != null)
                || (!Has("proof_inside") && OwnedPaper(Roster) != null)
                || (!Has("proof_outside") && OwnedPaper(Dispatch) != null);
        }

        private static bool Involvement(string file)
        {
            if (file == "grain" || file == "arms") return Has(file + "_sale") || Has(file + "_delivery");
            if (file == "inside") return Has("han_joined") || Has("alternative_joined");
            return Has("scout_aided") || Has("siege_delivery");
        }

        private static bool Offense()
        {
            foreach (string file in Cases)
                if (Verified(file) && Involvement(file) && !Has("cleared_" + file)) return true;
            return false;
        }

        private static int CountVerified()
        {
            return (Verified("grain") || Verified("arms") ? 1 : 0) + (Verified("inside") ? 1 : 0) + (Verified("outside") ? 1 : 0);
        }

        private static void BreakVerifiedSupport()
        {
            if (Verified("inside")) { BreakSupport("han_joined", 3); BreakSupport("alternative_joined", 2); }
            if (Verified("outside")) BreakSupport("scout_rejoined", 3);
        }

        private static void InspectRecoveredStock(Lot lot, bool witness)
        {
            if (!Involvement(lot.Key)) return;
            if (Has(lot.Key + "_recovered") || witness) Prove(lot.Key, true);
        }

        private static StoreClient Sweep()
        {
            return Visit("sweep", "pucha_name", "sec", Has("commission") ? "sweep_cooperate" : "sweep_inspect",
                Pick("ack", () => {
                    InspectRecoveredStock(Lots[0], Has("guo_agreed"));
                    InspectRecoveredStock(Lots[1], Has("bribed_shi"));
                    BreakVerifiedSupport();
                    return "sweep_done";
                }));
        }

        private static CityRules.Ending Candidate()
        {
            return CityRules.Resolve(Siege && Done("sweep"), Read(SongPoints), Read(JinPoints, 6),
                (Has("han_joined") && !Has("han_joined_broken")) || (Has("alternative_joined") && !Has("alternative_joined_broken")),
                (Has("scout_rejoined") && !Has("scout_rejoined_broken")) || (Read("rival_buyer") == 2 && Has("siege_delivery") && !Verified("outside")),
                Offense(), Has("commission"), CountVerified());
        }

        private static string EndingKey(CityRules.Ending ending)
        {
            return ending == CityRules.Ending.Song ? "song" : ending == CityRules.Ending.Commendation ? "jin" : "execution";
        }

        internal static CityRules.Ending MorningEmblem()
        {
            if (!Mod.Active || StoreStation.Instance == null || PlayerStore.Instance == null || !Has(End) || Day <= Read(EndDay))
                return CityRules.Ending.None;
            return (CityRules.Ending)Read(End);
        }

        private static StoreClient EndVisit(CityRules.Ending ending)
        {
            string key = EndingKey(ending);
            StoreClient client = CreateClient("end_" + key, StoreClient.ClientIntent.DIALOGUE,
                ending == CityRules.Ending.Song ? "zhou_name" : "pucha_name", ending == CityRules.Ending.Song ? "merchant" : "sec");
            client.OnArrival = () => Write(LastVisitDay, Day);
            client.mainDialogue.SetText(client.displayName, Text("end_" + key)).SetEndAction(() => {
                if (!AtWindow(client)) return;
                if (Candidate() != ending) { Dismiss(client); return; }
                if (ending == CityRules.Ending.Execution && !Give(Warrant)) { Dismiss(client); return; }
                Write(End, (int)ending);
                Write(EndDay, Day);
                if (ending != CityRules.Ending.Execution)
                {
                    Dismiss(client);
                    if (ending == CityRules.Ending.Song)
                        CityEndingPresentation.ShowVictory(Text("song_victory_title"), Text("song_victory_body"));
                    return;
                }
                showingExecution = true;
                try { PlayerStore.Instance.ExecuteGameOver("execution"); }
                finally { showingExecution = false; }
            });
            return FinishClient(client);
        }

        private static StoreClient PreparationVisit()
        {
            if (!Done("receipt") && Has("arms_sale")) return ReceiptVisit();
            if (!Done("guo") && Has("grain_sale")) return GrainKeeper();
            if (!Done("han_food"))
                return Purchase("han_food", "han_name", "officer_lun", "han_food_line", 180, Food,
                    passed => Write("han_fed", passed ? 1 : 0));
            if (!Done("ledger")) return LedgerVisit();
            if (!Done("scout_aid"))
                return Purchase("scout_aid", "du_name", "courrier", "scout_aid_line", 150, Medicine,
                    passed => Write("scout_aided", passed ? 1 : 0));
            if (!Done("pledge")) return ZhouPledge();
            if (!Done("han_answer")) return HanAnswer();
            if (!Done("scout_return")) return ScoutReturn();
            if (!Done("wanyan")) return Wanyan();
            if (!Done("alternative") && !Has("han_joined") && Has("scout_rejoined"))
                return Visit("alternative", "wei_name", "sec", "alternative_line",
                    Pick("relay", () => { AddOnce("alternative_joined", SongPoints, 2); return "alternative_joined"; }),
                    Pick("pass", () => "alternative_left"));
            return !Done("commission") && CanCommission() ? Commission("commission") : null;
        }

        private static void QueueSiege(StoreClientManager manager, int day, int last)
        {
            if (Has(End))
            {
                bool song = Read(End) == (int)CityRules.Ending.Song;
                Paper paper = song ? SongPaper : JinPaper;
                string receipt = song ? "award_given" : "award_jin_given";
                if (day > Read(EndDay) && Read(End) != (int)CityRules.Ending.Execution && !Has("paper_" + paper.Key))
                    manager.AddClient(Visit("award", song ? "zhou_name" : "pucha_name", song ? "merchant" : "sec", song ? "award_song" : "award_jin",
                        Pick("ack", () => Give(paper) ? receipt : null)));
                return;
            }
            if (day - last < 2) return;
            bool involved = Has(IntroDay);
            StoreClient visit = null;
            if (involved && !Done("rival_grain")) visit = RivalGrain();
            else if (involved && !Done("rival_buy") && (Read("rival_buyer") == 1 || Read("rival_buyer") == 2))
            {
                bool zhou = Read("rival_buyer") == 2;
                visit = Purchase("rival_buy", zhou ? "zhou_name" : "han_name", zhou ? "merchant" : "officer_lun",
                    "rival_buy_line", 315, Food, passed => {
                        if (passed) AddOnce("siege_delivery", SongPoints, 2);
                    });
            }
            else if (involved && !Done("requisition")) visit = Requisition(Lots[0]);
            else if (!Done("siege_medicine"))
                visit = Purchase("siege_medicine", "du_name", "courrier", "siege_medicine_line", 225,
                    Medicine, passed => Write("siege_medicine_filled", passed ? 1 : 0));
            else if (involved && !Done("audit")) visit = Requisition(Lots[1]);
            else if (involved)
            {
                CityRules.Ending ending = Candidate();
                bool canExplain = ending == CityRules.Ending.Execution && Has("commission") && CountVerified() >= 2 && !Done("final_account");
                if (!Done("commission") && CanCommission()) visit = Commission("commission");
                else if (canExplain) visit = Inquiry("final_account");
                else if (ending != CityRules.Ending.None) visit = EndVisit(ending);
                else if (!Done("sweep") && (!Has("commission") || Done("inquiry_3"))) visit = Sweep();
                else if (Has("commission") && !Done("inquiry_1")) visit = Inquiry("inquiry_1");
                else if (Has("commission") && !Done("inquiry_2")) visit = Inquiry("inquiry_2");
                else if (Has("commission") && !Done("inquiry_3")) visit = Inquiry("inquiry_3");
                else if (!Has("commission") && !Done("commission_reopen") && CanCommission() && HasNewEvidence())
                    visit = Commission("commission_reopen");
                else if (Has("commission") && (HasNewEvidence() || (Offense() && CountVerified() >= 2)))
                    visit = Inquiry("inquiry_followup_" + day);
            }
            if (visit != null) manager.AddClient(visit);
        }

        internal static void OnMorning()
        {
            if (!Mod.Active || PlayerStore.Instance == null || PlayerStore.Instance.endlessMode || StoreStation.Instance == null) return;
            int day = Day;
            if (day == CityRules.SiegeStartDay - 1 && !Has("notice_day") && !Has(SiegeDay) && !Has(End)
                && Publish("notice")) Write("notice_day", day);
            if (day >= CityRules.SiegeStartDay && !Has(End))
            {
                if (!Has(SiegeDay))
                {
                    Write(SiegeDay, CityRules.SiegeStartDay);
                    Write("siege_news", -1);
                }
                if (Read("siege_news") < 0 && Publish("siege"))
                    Write("siege_news", 1);
            }
            if (Has(End) && !Has("ending_news") && day > Read(EndDay)
                && Publish(EndingKey((CityRules.Ending)Read(End)))) Write("ending_news", 1);
        }

        private static bool Publish(string key)
        {
            StoreEventManager manager = StoreStation.Instance.storeEventManager;
            if (manager == null) return false;
            string id = NewsPrefix + key;
            if (!manager.GetActiveEvents().Exists(e => e.identifier == id))
                manager.ActivateEvent(new StoreEvent {
                    identifier = id, displayName = Text("news_" + key + "_title"), newsName = Text("news_" + key + "_title"),
                    newsDescription = Text("news_" + key + "_body"), duration = 1, importance = 4,
                    eventArea = StoreEvent.EventArea.ALL, eventType = StoreEvent.EventType.COSMETIC
                }, false, false);
            return true;
        }

        internal static string LocalizedNews(StoreEvent news, bool title)
        {
            return news.identifier != null && news.identifier.StartsWith(NewsPrefix, StringComparison.Ordinal)
                ? Mod.CurrentText("city_news_" + news.identifier.Substring(NewsPrefix.Length) + (title ? "_title" : "_body")) : null;
        }

        [HarmonyPatch(typeof(PlayerStore), "GrantWantedCaptureReward", new[] { typeof(StoreClient), typeof(bool) })]
        private static class ReportCredit
        {
            private static void Postfix(PlayerStore __instance, StoreClient __0, bool __1)
            {
                if (!Mod.Active || __instance != PlayerStore.Instance || __instance.endlessMode || __1
                    || __0 == null || string.IsNullOrEmpty(__0.wantedPosterId)) return;
                AddOnce("reported_" + __0.wantedPosterId, "reports", 1);
            }
        }

        [HarmonyPatch(typeof(GameItem), "GetNegociatedValue")]
        private static class SiegePricing
        {
            private static void Postfix(GameItem __instance, ref long __result)
            {
                PlayerStore store = PlayerStore.Instance;
                if (!Mod.Active || store == null || !store.isClientArrived || store.currentClientInstance == null) return;
                StoreClient client = store.currentClientInstance.GetClientBlueprint();
                if (client == null || (client.identifier != Prefix + "rival_buy" && client.identifier != Prefix + "requisition"
                    && client.identifier != Prefix + "siege_medicine")
                    || !Siege || !__instance.IsRootedInBackInv() || !client.IsClientBuyingThisItem(__instance)) return;
                if (Food(__instance)) __result = CityRules.Price(__result, 175);
                else if (Medicine(__instance)) __result = CityRules.Price(__result, 150);
            }
        }

        [HarmonyPatch(typeof(GameOverUIManager), "DisplayUI", new[] { typeof(string), typeof(string), typeof(bool) })]
        private static class ExecutionText
        {
            private static void Prefix(ref string __0, ref string __1)
            {
                if (!showingExecution) return;
                __0 = Text("execution_title");
                __1 = Text("execution_body");
            }
        }
    }
}
