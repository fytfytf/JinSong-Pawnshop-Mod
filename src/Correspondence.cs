using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JinSongPoc
{
    internal static class Correspondence
    {
        private static readonly Color Ink = new Color32(72, 43, 28, 255);
        private static readonly Color Paper = new Color32(225, 198, 145, 255);
        private static readonly Color Seal = new Color32(130, 56, 37, 255);
        private static readonly KeyValuePair<string, long>[] Cards = {
            new KeyValuePair<string, long>("joe_card", StorePhoneClient.WILD_CLIENT_NUMBER),
            new KeyValuePair<string, long>("card_brewer", StorePhoneClient.RED_IMP_REP_NUMBER),
            new KeyValuePair<string, long>("card_mining", StorePhoneClient.MINING_NUMBER),
            new KeyValuePair<string, long>("card_pharma", StorePhoneClient.PHARMA_NUMBER),
            new KeyValuePair<string, long>("card_chem", StorePhoneClient.CHEM_NUMBER),
            new KeyValuePair<string, long>("card_rev", StorePhoneClient.REV_MERCHANT_NUMBER)
        };

        private sealed class Delivery
        {
            internal StorePhoneClient Contact;
            internal Dialogue First, Decision;
            internal int Choice;
            internal string Subject;
        }

        private static GameObject panel, page, rows, confirmation;
        private static Image background;
        private static Sprite original, sheet;
        private static Texture2D sheetTexture;
        private static TMP_Text message, subject;
        private static Button send, bulk, cancel;
        private static TMP_Text sendLabel;
        private static readonly List<GameObject> Hidden = new List<GameObject>();
        private static long recipient;
        private static string regularSubject, bulkSubject;
        private static bool sending;

        internal static bool IsLedger(PhoneUIManager manager)
        {
            return Mod.Active && panel != null && manager.phonePanel == panel;
        }

        internal static bool Open(PhoneUIManager manager)
        {
            if (!Mod.Active || Mod.Assets == null || PlayerStore.Instance == null) return false;
            GameObject current = manager.phonePanel;
            if (current == null || current.name != "_Phone" || current.scene.buildIndex != 1) return false;
            if (panel != current)
            {
                Restore();
                Image image = current.GetComponent<Image>();
                Texture2D art;
                if (image == null || image.sprite == null || image.sprite.name != "phone"
                    || image.sprite.rect.width != 281 || image.sprite.rect.height != 316
                    || image.overrideSprite != image.sprite
                    || !Mod.Assets.Textures.TryGetValue("jin_letter_sheet", out art) || art == null) return false;

                sheet = Visuals.MakeSprite(art, 281, 316, "letter_ledger", out sheetTexture);
                if (sheet == null) return false;
                panel = current;
                background = image;
                original = image.sprite;
                image.sprite = sheet;
                foreach (Transform child in panel.transform)
                {
                    if (child.name == "CloseButton" || !child.gameObject.activeSelf) continue;
                    Hidden.Add(child.gameObject);
                    child.gameObject.SetActive(false);
                }

                page = Place("JinSong.AddressLedger", panel.transform, 0, 0, 562, 632);
                Text("Title", page.transform, 0, 248, 470, 53, 34, Ink).text = Label("contacts", "Correspondence");
                rows = Place("Recipients", page.transform, 0, 0, 562, 632);
                message = Text("Message", page.transform, 0, -229, 470, 53, 24, Seal);
                confirmation = Place("ConfirmLetter", page.transform, 0, 0, 562, 632);
                subject = Text("Subject", confirmation.transform, 0, 78, 476, 154, 22, Ink);
                send = Control("Send", confirmation.transform, 0, -30, 388, 46, Seal, Color.white,
                    Label("send", "Send"), () => Send(0));
                sendLabel = send.GetComponentInChildren<TMP_Text>();
                bulk = Control("Bulk", confirmation.transform, 0, -89, 388, 46, Seal, Color.white,
                    Label("bulk", "Place bulk order"), () => Send(1));
                cancel = Control("Cancel", confirmation.transform, 0, -148, 388, 46, Paper, Ink,
                    Label("cancel", "Cancel"), Cancel);
            }

            // The stock root keeps its close button, drag behaviour and Escape handling.
            panel.SetActive(true);
            RegisterExisting();
            Cancel();
            message.text = "";
            Refresh();
            return true;
        }

        internal static void Close()
        {
            Cancel();
            if (panel != null) panel.SetActive(false);
        }

        private static void Cancel()
        {
            recipient = 0;
            regularSubject = bulkSubject = null;
            if (confirmation != null) confirmation.SetActive(false);
            if (rows != null) rows.SetActive(true);
        }

        internal static void Register(GameItem item)
        {
            if (!Mod.Active || Mod.Assets == null || item == null || PlayerStore.Instance == null
                || UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex != 1
                || !GeneralHelper.IsItemOwned(item)) return;
            foreach (KeyValuePair<string, long> card in Cards)
            {
                if (item.identifier != card.Key) continue;
                if (Record(card.Value) && !sending && rows != null && rows.activeInHierarchy) Refresh();
                return;
            }
        }

        private static string ContactKey(long number) { return "letter-contact-" + number; }

        private static bool Record(long number)
        {
            string key = ContactKey(number);
            if (ModHelper.GetModDataBool(Mod.Id, key, false)) return false;
            ModHelper.SetModDataBool(Mod.Id, key, true);
            return true;
        }

        private static void RegisterExisting()
        {
            EmporiumEntry entry = EmporiumEntry.Instance;
            if (entry == null) return;
            foreach (KeyValuePair<string, long> card in Cards)
                if (entry.HaveItem(card.Key, true)) Record(card.Value);
        }

        private static bool Known(long number, StorePhoneClient contact)
        {
            return contact.dialedBefore || ModHelper.GetModDataBool(Mod.Id, ContactKey(number), false)
                || contact.phoneState == StorePhoneClient.PhoneState.Introduction
                || contact.phoneState == StorePhoneClient.PhoneState.Regular
                || contact.phoneState == StorePhoneClient.PhoneState.Cooldown;
        }

        private static void Refresh()
        {
            if (rows == null || PlayerStore.Instance == null) return;
            foreach (Transform child in rows.transform)
            {
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
            Text("Subtitle", rows.transform, 0, 197, 470, 44, 22, Ink).text =
                Label("help", "Select a recipient to write to.");
            int y = 139;
            foreach (KeyValuePair<string, long> card in Cards)
            {
                StorePhoneClient contact;
                long number = card.Value;
                if (!PlayerStore.Instance.PhoneClientDict.TryGetValue(number, out contact) || !Known(number, contact)) continue;
                string name = NpcNames.DisplayContact(contact);
                Control("Recipient_" + number, rows.transform, 0, y, 456, 45,
                    Paper, Ink, name, () => Confirm(number, contact, name));
                y -= 54;
            }
            if (y == 139)
                Text("Empty", rows.transform, 0, 28, 470, 74, 24, Ink).text =
                    Label("empty", "New contacts are recorded when you receive their cards.");
        }

        private static Delivery GetDelivery(long number, int choice)
        {
            StorePhoneClient contact;
            if (PlayerStore.Instance == null
                || !PlayerStore.Instance.PhoneClientDict.TryGetValue(number, out contact) || !Known(number, contact)
                || contact.phoneState == StorePhoneClient.PhoneState.Cooldown || !PhoneUIManager.WillAnswerCall(number))
                return null;
            Dialogue first = contact.GetCallDialog();
            if (first == null) return null;
            Dialogue decision = first;
            for (int n = 0; decision != null && n < 32; n++)
            {
                if (decision.choices != null && decision.choices.Count > 0)
                {
                    if (choice >= decision.choices.Count || !decision.IsChoiceEnabledAt(choice)) return null;
                    var option = decision.choices[choice];
                    // Only dispatch an actual service. A plain dismissal is never a delivery.
                    if (option.Item2 == null || option.Item3 == null) return null;
                    return new Delivery { Contact = contact, First = first, Decision = decision, Choice = choice,
                        Subject = option.Item2.plainText };
                }
                decision = decision.nextDialogue;
            }
            if (choice == 0 && decision == null && contact.phoneState == StorePhoneClient.PhoneState.Introduction)
                return new Delivery { Contact = contact, First = first, Subject = Label("introduce", "Open correspondence") };
            return null;
        }

        private static void Confirm(long number, StorePhoneClient contact, string name)
        {
            if (sending) return;
            Delivery regularDelivery = GetDelivery(number, 0);
            bool canOfferBulk = number != StorePhoneClient.REV_MERCHANT_NUMBER
                && contact.phoneClientType == StorePhoneClient.PhoneClientType.Supplier
                && contact.phoneState == StorePhoneClient.PhoneState.Regular;
            Delivery bulkDelivery = canOfferBulk ? GetDelivery(number, 1) : null;
            recipient = number;
            regularSubject = regularDelivery != null ? regularDelivery.Subject : null;
            bulkSubject = bulkDelivery != null ? bulkDelivery.Subject : null;
            rows.SetActive(false);
            message.text = "";
            subject.text = name + "\n" + (regularSubject ?? Label("unavailable", "Currently unavailable"));
            if (bulkDelivery != null) subject.text += "\n" + bulkSubject;
            sendLabel.text = canOfferBulk ? Label("regular", "Place standard order") : Label("send", "Send");
            send.interactable = regularDelivery != null;
            bulk.gameObject.SetActive(bulkDelivery != null);
            ((RectTransform)cancel.transform).anchoredPosition = new Vector2(0, bulkDelivery != null ? -148 : -89);
            confirmation.SetActive(true);
        }

        private static void Send(int choice)
        {
            if (sending || recipient == 0) return;
            long number = recipient;
            string expected = choice == 0 ? regularSubject : bulkSubject;
            if (expected == null) return;
            Cancel();
            Delivery delivery = GetDelivery(number, choice);
            if (delivery == null || delivery.Subject != expected)
            {
                message.text = Label("unavailable", "Currently unavailable");
                Refresh();
                return;
            }

            PhoneUIManager manager = PhoneUIManager.Instance;
            if (manager == null || !IsLedger(manager)) return;
            StorePhoneClient previous = manager.currentPhoneClient;
            sending = true;
            try
            {
                // Use the game's own service callbacks and persist only stock contact progress.
                manager.currentPhoneClient = delivery.Contact;
                delivery.Contact.dialedBefore = true;
                Record(number);
                RunDelivery(delivery);
                message.text = Label("sent", "Letter sent");
            }
            catch (Exception e)
            {
                Mod.Log.Warning("Letter service interrupted: " + e.Message);
                message.text = Label("failed", "Delivery interrupted");
            }
            finally
            {
                manager.currentPhoneClient = previous;
                sending = false;
                Refresh();
            }
        }

        private static void RunDelivery(Delivery delivery)
        {
            Dialogue current = delivery.First;
            HashSet<Dialogue> visited = new HashSet<Dialogue>();
            while (current != null)
            {
                if (visited.Count >= 32 || !visited.Add(current))
                    throw new InvalidOperationException("Unexpected correspondence loop.");
                if (current == delivery.Decision)
                {
                    if (!current.IsChoiceEnabledAt(delivery.Choice))
                        throw new InvalidOperationException("The service is no longer available.");
                    var option = current.choices[delivery.Choice];
                    option.Item3();
                    current = option.Item2;
                    continue;
                }
                if (current.choices != null && current.choices.Count > 0)
                    throw new InvalidOperationException("Unexpected secondary service decision.");
                current.ExecuteEndAction();
                current = current.nextDialogue;
                if (current != null && current.startAction != null) current.startAction();
            }
        }

        private static GameObject Place(string name, Transform parent, int x, int y, int width, int height)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            return go;
        }

        private static TMP_Text Text(string name, Transform parent, int x, int y, int width, int height, float size, Color color)
        {
            TMP_Text text = Place(name, parent, x, y, width, height).AddComponent<TextMeshProUGUI>();
            FontLoader fonts = FontLoader.Instance;
            TMP_FontAsset font = fonts != null ? (Mod.Chinese ? fonts.simplifiedChineseFont : fonts.westernFont) : null;
            if (font != null) text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        private static Button Control(string name, Transform parent, int x, int y, int width, int height,
            Color backgroundColor, Color textColor, string label, UnityEngine.Events.UnityAction click)
        {
            GameObject go = Place(name, parent, x, y, width, height);
            Image image = go.AddComponent<Image>();
            image.color = backgroundColor;
            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color32(239, 223, 191, 255);
            colors.pressedColor = new Color32(195, 169, 130, 255);
            button.colors = colors;
            Text("Label", go.transform, 0, 0, width - 16, height, 24, textColor).text = label;
            button.onClick.AddListener(click);
            return button;
        }

        internal static string Label(string key, string fallback)
        {
            return Mod.Chinese ? Mod.Text("letter_" + key) ?? fallback : fallback;
        }

        internal static void Restore()
        {
            Close();
            if (background != null && background.sprite == sheet) background.sprite = original;
            foreach (GameObject go in Hidden)
                if (go != null) go.SetActive(true);
            Hidden.Clear();
            if (page != null) UnityEngine.Object.Destroy(page);
            if (sheet != null) UnityEngine.Object.Destroy(sheet);
            if (sheetTexture != null) UnityEngine.Object.Destroy(sheetTexture);
            panel = page = rows = confirmation = null;
            background = null;
            original = sheet = null;
            sheetTexture = null;
            subject = message = sendLabel = null;
            send = bulk = cancel = null;
            sending = false;
        }
    }

    [HarmonyPatch(typeof(PhoneUIManager), "OpenUI")]
    internal static class OpenLetterLedger
    {
        private static bool Prefix(PhoneUIManager __instance) { return !Correspondence.Open(__instance); }
    }

    [HarmonyPatch(typeof(PhoneUIManager), "CloseUI")]
    internal static class CloseLetterLedger
    {
        private static bool Prefix(PhoneUIManager __instance)
        {
            if (!Correspondence.IsLedger(__instance)) return true;
            Correspondence.Close();
            return false;
        }
    }

    [HarmonyPatch(typeof(PhoneUIManager), "Update")]
    internal static class LedgerInput
    {
        private static bool Prefix(PhoneUIManager __instance)
        {
            return !Correspondence.IsLedger(__instance) || !__instance.phonePanel.activeSelf;
        }
    }

    [HarmonyPatch(typeof(GameItem), "AddParent")]
    internal static class RegisterReceivedCard
    {
        private static void Postfix(GameItem __instance, bool __result)
        {
            if (__result) Correspondence.Register(__instance);
        }
    }

    [HarmonyPatch(typeof(GeneralHelper), "SetItemOwned")]
    internal static class RegisterOwnedCard
    {
        private static void Postfix(GameItem __0, bool __1)
        {
            if (__1 && __0 != null && __0.parentInventory != null) Correspondence.Register(__0);
        }
    }

    [HarmonyPatch(typeof(TooltipContainer), "UpdateTooltip")]
    internal static class LetterBoxTooltip
    {
        private static void Prefix(TooltipContainer __instance, out string __state)
        {
            __state = null;
            if (!Mod.Active || Mod.Assets == null || __instance.gameObject.scene.buildIndex != 1
                || __instance.gameObject.name != "wall_phone" || String.IsNullOrEmpty(__instance.content)) return;
            __state = __instance.content;
            __instance.content = Correspondence.Label("tooltip", "Open letter box");
        }

        private static void Postfix(TooltipContainer __instance, string __state)
        {
            if (__state != null) __instance.content = __state;
        }
    }
}
