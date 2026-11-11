using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JinSongPoc
{
    // Scope is the four user-reported native panels. No data, callback, price,
    // selection, content layout, or inventory geometry is replaced here.
    internal static class ReportedUiSkin
    {
        private sealed class ImageState
        {
            internal Color Native, Applied;
            internal Sprite Base, Override, AppliedSprite;
            internal WildNetworkFrame Network;
        }
        private sealed class TextState { internal Color Native, Applied; }
        private static readonly Dictionary<Image, ImageState> Images = new Dictionary<Image, ImageState>();
        private static readonly Dictionary<TMP_Text, TextState> Texts = new Dictionary<TMP_Text, TextState>();
        private static readonly Dictionary<string, Sprite> Art = new Dictionary<string, Sprite>();
        private static readonly Color Ink = new Color32(62, 39, 24, 255);
        private static readonly Color Paper = new Color32(235, 219, 180, 255);
        private static readonly Color Wood = new Color32(81, 48, 29, 255);
        private static readonly Color Gold = new Color32(176, 127, 65, 255);
        private static readonly Color NetworkCard = new Color32(225, 204, 163, 255);
        private static readonly Color NetworkInactive = new Color32(105, 74, 49, 255);
        private static readonly Color NetworkRecommended = new Color32(126, 77, 36, 255);
        private static bool busy;
        internal static bool Painting { get { return busy; } }

        internal static bool OwnsSprite(Image image)
        {
            ImageState state;
            return Images.TryGetValue(image, out state) && (state.Network != null
                || (state.AppliedSprite != null && image.overrideSprite == state.AppliedSprite));
        }

        internal static bool RefreshChangedImage(Image image)
        {
            ImageState state;
            if (!Mod.Active || Mod.Assets == null || image == null || !Images.TryGetValue(image, out state)
                || state.Network == null || !state.Network.gameObject.activeInHierarchy) return false;
            busy = true;
            try { SkinNetworkImage(state.Network, image); }
            finally { busy = false; }
            return true;
        }

        private static Sprite NativeSprite(Image image, ImageState state)
        {
            Sprite current = image.overrideSprite;
            if (state != null && current == state.AppliedSprite)
                return (state.Base == image.sprite ? state.Override : null) ?? image.sprite;
            return CommonUiArt.NativeSprite(image);
        }

        private static void ApplySprite(Image image, ImageState state, Sprite art)
        {
            if (image.overrideSprite != state.AppliedSprite || state.Base != image.sprite)
            {
                Color previous = image.color;
                state.Override = image.overrideSprite == state.AppliedSprite ? null : CommonUiArt.Release(image);
                if (previous != image.color) state.Native = image.color;
            }
            state.Base = image.sprite;
            state.AppliedSprite = art;
            if (image.overrideSprite != art) image.overrideSprite = art;
        }

        private static bool Foreign(Sprite sprite)
        {
            if (sprite == null || ModLoader.Mods == null) return false;
            foreach (LoadedMod mod in ModLoader.Mods)
                if (mod != Mod.Assets && mod.Textures != null && mod.Textures.ContainsValue(sprite.texture)) return true;
            return false;
        }

        private static Color Alpha(Color color, float alpha) { color.a = alpha; return color; }
        private static bool Near(Color value, float r, float g, float b)
        { return Math.Abs(value.r - r) < .04f && Math.Abs(value.g - g) < .04f && Math.Abs(value.b - b) < .04f; }

        private static Color Palette(Color native, bool text, string name)
        {
            if (native.a == 0) return native;
            // Keep warning, availability, gain/loss and selected-state signals.
            if (native.g > native.r * 1.25f && native.g > native.b * 1.12f
                || native.r > native.g * 1.45f && native.r > native.b * 1.45f)
                return native;
            if (text)
            {
                if (name == "HeaderTitle" || name == "HeaderText" || name == "SectionHeader" || name == "ContinueLabel")
                    return Alpha(Paper, native.a);
                return Alpha(Ink, native.a);
            }
            if (Near(native, .161f, .133f, .204f) || Near(native, .161f, .137f, .204f))
                return Alpha(Wood, native.a);
            if (Near(native, .255f, .271f, .329f)) return Alpha(Wood, native.a);
            if (Near(native, .541f, .749f, .733f)) return Alpha(Gold, native.a);
            if (Near(native, .820f, .784f, .647f)) return Alpha(Paper, native.a);
            if (Math.Abs(native.r - native.g) < .07f && Math.Abs(native.g - native.b) < .07f && native.r < .65f)
                return Alpha(Ink, native.a);
            return native;
        }

        private static Sprite Panel(Sprite stock, bool profession)
        {
            string key = stock.GetInstanceID() + (profession ? ".profession" : ".ledger");
            Sprite result;
            if (Art.TryGetValue(key, out result)) return result;
            int width = Mathf.RoundToInt(stock.rect.width), height = Mathf.RoundToInt(stock.rect.height);
            bool ledger = stock.name == "calendar_stripped";
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int edge = Math.Min(Math.Min(x, width - 1 - x), Math.Min(y, height - 1 - y));
                int grain = (x * 17 + y * 3) % 7 - 3;
                Color32 color = edge < 2 ? new Color32(47, 29, 18, 255)
                    : edge < 4 ? new Color32(176, 127, 65, 255)
                    : edge < 13 ? new Color32((byte)(87 + grain), (byte)(52 + grain), (byte)(31 + grain), 255)
                    : new Color32((byte)(235 + grain), (byte)(219 + grain), (byte)(180 + grain), 255);
                if (profession && y > height - 65 && edge >= 4)
                    color = new Color32((byte)(87 + grain), (byte)(52 + grain), (byte)(31 + grain), 255);
                if (ledger)
                {
                    // A single open account-book page: binding stays in the outer margin,
                    // leaving the native calendar, graphs and account rows unobstructed.
                    bool cover = edge < 9 || x < 26;
                    color = cover ? new Color32((byte)(83 + grain), (byte)(39 + grain), (byte)(31 + grain), 255)
                        : new Color32((byte)(239 + grain), (byte)(224 + grain), (byte)(188 + grain), 255);
                    if (edge == 3 || edge == 4) color = new Color32(167, 116, 62, 255);
                    if (x >= 9 && x <= 20 && y > 14 && y < height - 14 && y % 30 < 3)
                        color = new Color32(192, 151, 92, 255);
                    if (y >= 10 && y <= 13 && x >= 28 && x < width - 12)
                        color = new Color32(191, 161, 115, 255);
                }
                pixels[y * width + x] = color;
            }
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "jinsong_poc.reported.panel";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels); texture.Apply(false, false);
            result = Sprite.Create(texture, new Rect(0, 0, width, height), stock.pivot / stock.rect.size,
                stock.pixelsPerUnit, 0, SpriteMeshType.FullRect, stock.border);
            result.name = "jinsong_poc.reported." + stock.name;
            Art.Add(key, result);
            return result;
        }

        private static void Skin(Transform root, bool profession)
        {
            if (root == null || !root.gameObject.activeInHierarchy) return;
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image == null || Foreign(image.sprite) || Foreign(image.overrideSprite)) continue;
                ImageState state;
                if (!Images.TryGetValue(image, out state))
                {
                    state = new ImageState { Native = image.color };
                    Images.Add(image, state);
                }
                else if (image.color != state.Applied) state.Native = image.color;
                Color color = Palette(state.Native, false, image.name);
                Sprite stock = image.sprite;
                if (stock != null && (stock.name == "tablet" || stock.name == "calendar_stripped" || stock.name == "new_game_bg"))
                {
                    ApplySprite(image, state, Panel(stock, profession));
                    color = Color.white;
                }
                else if (stock != null && (stock.name == "slot_normal" || stock.name == "slot_selected"
                    || stock.name.StartsWith("arrow_", StringComparison.Ordinal)))
                {
                    // Native alpha and selected/disabled states still govern these controls.
                    color = Alpha(stock.name == "slot_selected" ? (Color)new Color32(240, 197, 104, 255) : Gold, state.Native.a);
                }
                else if (image.name == "Screen") color = Alpha(Paper, state.Native.a);
                if (image.GetComponentInParent<NewGamePerkTooltip>() != null)
                {
                    if (image.name == "Fill") color = Alpha(Paper, 1f);
                    else if (image.name == "Outer") color = Alpha(Wood, state.Native.a);
                    else if (image.name == "Border" || image.name == "HeaderRule" || image.name == "LockRule")
                        color = Alpha(Gold, state.Native.a);
                }
                state.Applied = color;
                if (image.color != color) image.color = color;
            }
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                TextState state;
                if (!Texts.TryGetValue(text, out state))
                    Texts.Add(text, state = new TextState { Native = text.color });
                else if (text.color != state.Applied) state.Native = text.color;
                Color color = Palette(state.Native, true, text.name);
                // White label text belongs on the dark native header/button faces.
                Transform parent = text.transform.parent;
                if (parent != null && (parent.name == "HeaderPlate" || parent.name == "Button" || parent.name == "ContinueButton"
                    || parent.name == "CalendarButton" || parent.name == "NightlyButton" || parent.name == "StatusButton"))
                    color = Alpha(Paper, state.Native.a);
                if (profession && text.name == "HeaderTitle") color = Alpha(Paper, state.Native.a);
                if (text.GetComponentInParent<NewGamePerkTooltip>() != null) color = Alpha(Ink, state.Native.a);
                state.Applied = color;
                if (text.color != color) text.color = color;
            }
        }

        internal static void Refresh()
        {
            if (busy || !Mod.Active || Mod.Assets == null) return;
            busy = true;
            try
            {
                foreach (TabletFrame frame in UnityEngine.Object.FindObjectsOfType<TabletFrame>())
                    if (frame is NegotiationTabletFrame || frame is EndOfDayTabletFrame) Skin(frame.transform, false);
                foreach (NewGamePanelFrame frame in UnityEngine.Object.FindObjectsOfType<NewGamePanelFrame>())
                    Skin(frame.transform, true);
                foreach (WildNetworkFrame frame in UnityEngine.Object.FindObjectsOfType<WildNetworkFrame>())
                    SkinNetwork(frame);
                AdvCalendarUIManager calendar = AdvCalendarUIManager.Instance;
                if (calendar != null && calendar.UIPanel != null) Skin(calendar.UIPanel.transform, false);
                SkinSpeech();
            }
            finally { busy = false; }
        }

        private static void SkinNetwork(WildNetworkFrame frame)
        {
            foreach (Image image in frame.GetComponentsInChildren<Image>(true))
                SkinNetworkImage(frame, image);
            foreach (TMP_Text text in frame.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == frame.infoMessage) continue;
                bool light = text.name == "Title" && text != frame.infoTitle || text.name == "title" || text.name == "label"
                    || text == frame.useLabel || text == frame.usePrice;
                if (text.name == "cost" && text.transform.parent != null)
                {
                    Image price = text.transform.parent.GetComponent<Image>();
                    light = price != null && price.sprite == frame.priceTagUnavailable;
                }
                SpeechText(text, light ? Paper : Ink);
            }
        }

        private static void SkinNetworkImage(WildNetworkFrame frame, Image image)
        {
            ImageState state;
            Images.TryGetValue(image, out state);
            Sprite stock = NativeSprite(image, state);
            if (Foreign(image.sprite) || Foreign(stock)) return;
            Sprite art = null;
            Color? tint = null;
            if (stock != null)
            {
                if (stock == frame.windowFrame) art = NetworkFace(stock, Paper, true);
                else if (stock == frame.headerBar)
                    art = NetworkFace(stock, Wood, true);
                else if (stock == frame.cardHeaderPlate)
                    art = NetworkFace(stock, image.transform.parent.name == "card_recommended" ? NetworkRecommended : Wood, true);
                else if (stock == frame.tabNormal)
                {
                    Button tab = image.GetComponent<Button>();
                    bool selected = tab != null && !tab.interactable;
                    art = NetworkFace(stock, selected ? NetworkRecommended : Wood, true);
                }
                else if (stock == frame.tabActive || stock == frame.tabHover)
                    art = NetworkFace(stock, NetworkInactive, true);
                else if (stock == frame.cardPlate) art = NetworkFace(stock, NetworkCard, true);
                else if (stock == frame.priceTagNormal) art = NetworkFace(stock, Paper);
                else if (stock == frame.slotFrame || stock == frame.slotInset || stock == frame.slotBorderNormal
                    || stock == frame.connector || stock == frame.scrollThumb || stock == frame.frame2Square)
                    art = NetworkFace(stock, Gold);
                else if (stock == frame.scrollTrack) art = NetworkFace(stock, NetworkCard);
            }
            else if (image.name == "icon_bg" || image.name == "UseButton") tint = Wood;
            else if (image.name == "StatFunds" || image.name == "StatFavor") tint = NetworkCard;
            else if (image.name == "SeparatorH" || image.name == "SeparatorV" || image.name == "Divider") tint = Gold;

            if (art == null && !tint.HasValue)
            {
                // A native state can replace the normal price or slot frame with a warning or selection sprite.
                if (state != null)
                {
                    if (state.AppliedSprite != null && image.overrideSprite == state.AppliedSprite)
                        image.overrideSprite = stock == image.sprite ? null : stock;
                    if (image.color == state.Applied) image.color = state.Native;
                    state.Base = image.sprite;
                    state.AppliedSprite = null;
                }
                return;
            }
            if (state == null)
            {
                Images.Add(image, state = new ImageState { Native = image.color, Network = frame });
            }
            else if (image.color != state.Applied) state.Native = image.color;
            if (art != null) ApplySprite(image, state, art);
            state.Applied = art != null ? Alpha(Color.white, state.Native.a)
                : Alpha(tint.Value, image.name == "StatFunds" || image.name == "StatFavor" ? .9f : state.Native.a);
            if (image.color != state.Applied) image.color = state.Applied;
        }

        private static Sprite NetworkFace(Sprite stock, Color32 material, bool solid = false)
        {
            string key = "network." + stock.GetInstanceID() + "." + material.r + "." + solid;
            Sprite art;
            if (Art.TryGetValue(key, out art)) return art;
            int width = Mathf.RoundToInt(stock.rect.width), height = Mathf.RoundToInt(stock.rect.height);
            Color32[] pixels = InteriorArt.ReadSpritePixels(stock, width, height);
            if (pixels == null) return null;
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 native = pixels[i];
                int shade = (native.r + native.g + native.b - 384) / (solid ? 35 : 12) + i % 3 - 1;
                pixels[i] = new Color32((byte)Mathf.Clamp(material.r + shade, 0, 255),
                    (byte)Mathf.Clamp(material.g + shade, 0, 255), (byte)Mathf.Clamp(material.b + shade, 0, 255),
                    solid ? (byte)255 : native.a);
                if (stock.name == "window_frame" && (i < width || i >= pixels.Length - width || i % width == 0 || i % width == width - 1))
                    pixels[i] = Wood;
            }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            art = Sprite.Create(texture, new Rect(0, 0, width, height), stock.pivot / stock.rect.size,
                stock.pixelsPerUnit, 0, SpriteMeshType.FullRect, stock.border);
            art.name = "jinsong_poc.network." + stock.name;
            Art.Add(key, art);
            return art;
        }

        private static void SkinSpeech()
        {
            DialogUIManager ui = DialogUIManager.Instance;
            if (ui == null) return;
            if (ui.clientBubble != null)
            {
                SpeechImage(ui.clientBubble.backgroundImage, false);
                foreach (TMP_Text text in ui.clientBubble.GetComponentsInChildren<TMP_Text>(true)) SpeechText(text, Ink);
            }
            if (ui.clientNameBubbleImage != null) SpeechImage(ui.clientNameBubbleImage, true);
            if (ui.clientNameTMP != null) SpeechText(ui.clientNameTMP, Paper);
        }

        internal static void RefreshSpeech()
        {
            if (busy || !Mod.Active || Mod.Assets == null) return;
            busy = true;
            try { SkinSpeech(); }
            finally { busy = false; }
        }

        private static void SpeechText(TMP_Text text, Color color)
        {
            TextState state;
            if (!Texts.TryGetValue(text, out state)) Texts.Add(text, state = new TextState { Native = text.color });
            else if (text.color != state.Applied) state.Native = text.color;
            state.Applied = Alpha(color, state.Native.a);
            if (text.color != state.Applied) text.color = state.Applied;
        }

        private static void SpeechImage(Image image, bool title)
        {
            if (image == null || image.sprite == null || Foreign(image.sprite) || Foreign(image.overrideSprite)) return;
            ImageState state;
            if (!Images.TryGetValue(image, out state))
                Images.Add(image, state = new ImageState { Native = image.color });
            Sprite stock = NativeSprite(image, state) ?? image.sprite;
            string key = "speech." + stock.GetInstanceID() + "." + title;
            Sprite art;
            if (!Art.TryGetValue(key, out art))
            {
                int w = Mathf.RoundToInt(stock.rect.width), h = Mathf.RoundToInt(stock.rect.height);
                Color32[] native = InteriorArt.ReadSpritePixels(stock, w, h);
                if (native == null) return;
                Color32[] pixels = new Color32[native.Length];
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (native[i].a == 0) continue;
                    bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1
                        || native[i - 1].a == 0 || native[i + 1].a == 0 || native[i - w].a == 0 || native[i + w].a == 0;
                    Color32 color = edge ? (Color32)Gold : title ? (Color32)Wood : (Color32)Paper;
                    color.a = native[i].a; pixels[i] = color;
                }
                Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels32(pixels); texture.Apply(false, false);
                art = Sprite.Create(texture, new Rect(0, 0, w, h), stock.pivot / stock.rect.size,
                    stock.pixelsPerUnit, 0, SpriteMeshType.FullRect, stock.border);
                art.name = "jinsong_poc.reported.speech." + stock.name;
                Art.Add(key, art);
            }
            ApplySprite(image, state, art);
            state.Applied = Color.white;
            if (image.color != Color.white) image.color = Color.white;
        }

        internal static void Restore()
        {
            foreach (var pair in Images)
            {
                if (pair.Key == null) continue;
                if (pair.Key.color == pair.Value.Applied) pair.Key.color = pair.Value.Native;
                if (pair.Value.AppliedSprite != null && pair.Key.overrideSprite == pair.Value.AppliedSprite)
                    pair.Key.overrideSprite = pair.Value.Override;
            }
            foreach (var pair in Texts)
                if (pair.Key != null && pair.Key.color == pair.Value.Applied) pair.Key.color = pair.Value.Native;
            Images.Clear(); Texts.Clear();
            foreach (Sprite sprite in Art.Values)
                if (sprite != null) { UnityEngine.Object.Destroy(sprite.texture); UnityEngine.Object.Destroy(sprite); }
            Art.Clear();
        }

        [HarmonyPatch]
        private static class VisiblePanels
        {
            private static IEnumerable<System.Reflection.MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(NegociationUIManager), "OpenUI");
                yield return AccessTools.Method(typeof(EndOfDayUIManager), "OpenUI");
                yield return AccessTools.Method(typeof(StartOfDayUIManager), "ShowMorningReport");
                yield return AccessTools.Method(typeof(NewGameSpecializationUI), "Open");
                yield return AccessTools.Method(typeof(NewGameSpecializationUI), "ShowScenario");
                yield return AccessTools.Method(typeof(NewGamePerkTooltip), "Show");
                yield return AccessTools.Method(typeof(StoreCreditDisplay), "OnClick");
                yield return AccessTools.Method(typeof(WildNetworkUI), "RefreshAll");
                yield return AccessTools.Method(typeof(WildNetworkUI), "OnSlotClicked");
            }
            private static void Postfix() { Refresh(); }
        }

        [HarmonyPatch(typeof(WildNetworkUI), "Open")]
        private static class NetworkOpen
        {
            private static void Postfix()
            {
                // Opening again creates new cards. Release references to previous windows.
                var oldImages = new List<Image>();
                foreach (Image image in Images.Keys) if (image == null) oldImages.Add(image);
                foreach (Image image in oldImages) Images.Remove(image);
                var oldTexts = new List<TMP_Text>();
                foreach (TMP_Text text in Texts.Keys) if (text == null) oldTexts.Add(text);
                foreach (TMP_Text text in oldTexts) Texts.Remove(text);
                Refresh();
            }
        }

        [HarmonyPatch(typeof(AdvCalendarUIManager), "ToggleUI", new Type[0])]
        private static class LedgerToggled
        {
            private static void Postfix(AdvCalendarUIManager __instance)
            {
                if (busy || !Mod.Active || Mod.Assets == null || __instance.UIPanel == null) return;
                busy = true;
                try { Skin(__instance.UIPanel.transform, false); }
                finally { busy = false; }
            }
        }
    }
}
