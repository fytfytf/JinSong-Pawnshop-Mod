using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JinSongPoc
{
    // Native menu-only presentation. No game/store identity, links, dates or save fields are changed.
    internal static class MenuBrand
    {
        private sealed class NavState { internal Navigation Native, Applied; }
        private static readonly Dictionary<GameObject, bool> Hidden = new Dictionary<GameObject, bool>();
        private static readonly Dictionary<Selectable, NavState> NavigationChanges = new Dictionary<Selectable, NavState>();
        private static GameObject root;
        private static Image nativeLogo;
        private static bool nativeEnabled;
        private static Sprite wood;
        private static Font chineseFont, englishFont;
        private static Text title, subtitle;

        internal static void Refresh(LoadedMod assets, RectTransform logo)
        {
            if (!Mod.Active || assets == null || logo == null || logo.gameObject.scene.buildIndex != 0) return;
            if (root == null)
            {
                Image image = logo.GetComponent<Image>();
                Texture2D texture;
                if (image == null || image.sprite == null || image.sprite.name != "Logo"
                    || !assets.Textures.TryGetValue("jin_counter_wood", out texture) || texture == null) return;
                if (ModLoader.Mods != null)
                    foreach (LoadedMod mod in ModLoader.Mods)
                        if (mod != assets && mod.Textures != null && mod.Textures.ContainsValue(image.sprite.texture)) return;
                chineseFont = FontFor("KaiTi", "STKaiti", "SimSun", "NSimSun");
                englishFont = FontFor("Georgia", "Times New Roman", "Arial");
                if (chineseFont == null || englishFont == null) return;
                nativeLogo = image;
                nativeEnabled = image.enabled;
                root = new GameObject("JinSong.MenuBrand", typeof(RectTransform));
                root.transform.SetParent(logo, false);
                RectTransform frame = (RectTransform)root.transform;
                Place(frame, new Vector2(.025f, .20f), new Vector2(.975f, .80f));
                wood = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
                wood.name = "jinsong_poc.brand.wood";
                Panel("Timber", frame, Vector2.zero, Vector2.one, new Color(.28f, .22f, .18f, 1f), wood);
                Color brass = new Color32(169, 124, 65, 255);
                Panel("Top", frame, new Vector2(0, .965f), Vector2.one, brass, null);
                Panel("Bottom", frame, Vector2.zero, new Vector2(1, .035f), brass, null);
                Panel("Left", frame, Vector2.zero, new Vector2(.018f, 1), brass, null);
                Panel("Right", frame, new Vector2(.982f, 0), Vector2.one, brass, null);
                title = Label("Title", frame, new Vector2(.07f, .30f), new Vector2(.93f, .90f), chineseFont, 52,
                    new Color32(236, 204, 135, 255));
                subtitle = Label("TradeCaption", frame, new Vector2(.10f, .09f), new Vector2(.83f, .30f), chineseFont, 17,
                    new Color32(210, 180, 121, 255));
                Panel("CinnabarSeal", frame, new Vector2(.86f, .09f), new Vector2(.96f, .26f), new Color32(139, 40, 28, 255), null);
                Text seal = Label("SealGlyph", frame, new Vector2(.86f, .09f), new Vector2(.96f, .26f), chineseFont, 20,
                    new Color32(239, 208, 160, 255));
                seal.text = "当";
                nativeLogo.enabled = false;
            }
            bool chinese = LocHelper.GetCurrentLocaleCode() == "zh";
            title.font = chinese ? chineseFont : englishFont;
            title.text = chinese ? "金宋当铺" : "JinSong Pawnshop";
            subtitle.font = chinese ? chineseFont : englishFont;
            subtitle.text = chinese ? "收售百货" : "BUY & SELL GOODS";
            Canvas canvas = logo.GetComponentInParent<Canvas>();
            if (canvas != null) HideCommunityEntry(canvas);
        }

        private static Font FontFor(params string[] candidates)
        {
            string[] installed = Font.GetOSInstalledFontNames();
            foreach (string candidate in candidates)
                if (Array.IndexOf(installed, candidate) >= 0)
                {
                    Font font = Font.CreateDynamicFontFromOSFont(candidate, 64);
                    if (font != null) return font;
                }
            return null;
        }
        private static void Place(RectTransform rect, Vector2 min, Vector2 max)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static void Panel(string name, RectTransform parent, Vector2 min, Vector2 max, Color color, Sprite sprite)
        {
            GameObject node = new GameObject(name, typeof(RectTransform), typeof(Image));
            node.transform.SetParent(parent, false);
            Place((RectTransform)node.transform, min, max);
            Image image = node.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
        }
        private static Text Label(string name, RectTransform parent, Vector2 min, Vector2 max, Font font, int size, Color color)
        {
            GameObject node = new GameObject(name, typeof(RectTransform), typeof(Text));
            node.transform.SetParent(parent, false);
            Place((RectTransform)node.transform, min, max);
            Text text = node.GetComponent<Text>();
            text.font = font; text.fontSize = size; text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 9; text.resizeTextMaxSize = size;
            text.supportRichText = false; text.raycastTarget = false;
            return text;
        }
        private static bool HiddenTarget(Selectable target)
        {
            if (target == null) return false;
            foreach (var entry in Hidden)
                if (entry.Key != null && target.transform.IsChildOf(entry.Key.transform)) return true;
            return false;
        }
        private static void HideCommunityEntry(Canvas canvas)
        {
            foreach (RectTransform node in canvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (node.name != "DiscordButton" && node.name != "DiscordNote" && node.name != "Discord") continue;
                if (!Hidden.ContainsKey(node.gameObject)) Hidden.Add(node.gameObject, node.gameObject.activeSelf);
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
                    && EventSystem.current.currentSelectedGameObject.transform.IsChildOf(node))
                    EventSystem.current.SetSelectedGameObject(null);
                node.gameObject.SetActive(false);
            }
            foreach (Selectable control in canvas.GetComponentsInChildren<Selectable>(true))
            {
                Navigation navigation = control.navigation;
                bool affected = HiddenTarget(navigation.selectOnUp) || HiddenTarget(navigation.selectOnDown)
                    || HiddenTarget(navigation.selectOnLeft) || HiddenTarget(navigation.selectOnRight);
                if (!affected) continue;
                NavState state;
                if (!NavigationChanges.TryGetValue(control, out state))
                    NavigationChanges.Add(control, state = new NavState { Native = navigation });
                if (HiddenTarget(navigation.selectOnUp)) navigation.selectOnUp = null;
                if (HiddenTarget(navigation.selectOnDown)) navigation.selectOnDown = null;
                if (HiddenTarget(navigation.selectOnLeft)) navigation.selectOnLeft = null;
                if (HiddenTarget(navigation.selectOnRight)) navigation.selectOnRight = null;
                state.Applied = navigation;
                control.navigation = navigation;
            }
        }
        internal static void Restore()
        {
            foreach (var pair in NavigationChanges)
                if (pair.Key != null && pair.Key.navigation.Equals(pair.Value.Applied)) pair.Key.navigation = pair.Value.Native;
            NavigationChanges.Clear();
            foreach (var pair in Hidden) if (pair.Key != null && !pair.Key.activeSelf) pair.Key.SetActive(pair.Value);
            Hidden.Clear();
            if (nativeLogo != null && !nativeLogo.enabled) nativeLogo.enabled = nativeEnabled;
            if (root != null) UnityEngine.Object.Destroy(root);
            if (wood != null) UnityEngine.Object.Destroy(wood);
            if (chineseFont != null) UnityEngine.Object.Destroy(chineseFont);
            if (englishFont != null) UnityEngine.Object.Destroy(englishFont);
            root = null; nativeLogo = null; title = subtitle = null;
            wood = null; chineseFont = englishFont = null;
        }
    }
}
