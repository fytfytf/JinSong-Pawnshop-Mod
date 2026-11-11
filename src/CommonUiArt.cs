using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace JinSongPoc
{
    // Only declared native UI chrome. Never recolors content, charts, portraits or custom MOD sprites.
    internal static class CommonUiArt
    {
        private sealed class State
        {
            internal Sprite NativeOverride, Base, Applied;
            internal Color NativeColor, AppliedColor;
            internal bool ColorChanged;
        }
        private static readonly Dictionary<Image, State> Images = new Dictionary<Image, State>();
        private static readonly Dictionary<Sprite, Sprite> Sprites = new Dictionary<Sprite, Sprite>();
        private static readonly HashSet<string> Names = new HashSet<string> {
            "card_plate", "card_header_plate", "card_outline_normal", "card_outline_selected", "card_outline_unavailable",
            "station", "connector", "frame2", "option_line_single", "dialogue_bubble_science", "bubble_sci_p",
            "dialogue_bubble2_pink_pointed", "tutorial_bubble_unpointed_x2", "tutorial_bubble_x2",
            "panel", "InputFieldBackground", "Background", "background", "background_fill",
            "button_generic_x2", "button_load", "button_load_hl", "button_main_menu_h", "button_main_menu_n", "button_main_menu_p",
            "checkbox", "checkbox_arrow", "checkbox_hl", "checkbox_mark", "Checkmark",
            "close_button_h", "close_button_n", "close_button_p", "delete_save_button", "delete_save_button_hl",
            "dropdownElement", "dropdown_2x", "dropdown_box", "dropdown_button", "dropdown_option", "dropdown_panel", "dropdown_template_bg",
            "gen_button_2x_h", "gen_button_2x_n", "gen_button_2x_p", "glass_button_0", "glass_button_1", "glass_button_2",
            "scroll", "scroll_bar_bot_x2", "scroll_bar_top_x2", "scroll_thumb", "scroll_track", "vertical_scroll_bar", "vertical_scroll_bar_handle_2x",
            "slider_bar_2x", "slider_bar_filled_2x", "slider_gauge", "slider_handle_h", "slider_handle_n", "slider_thumb", "slider_thumb_hl", "slider_track",
            "slot_border_hover", "slot_border_normal", "slot_border_purchased", "tab_active", "tab_hover", "tab_normal",
            "appraisal_panel", "aqua_scan_button", "blue_button_2x", "blue_close_button", "button", "button_back_blue_m",
            "button_bargain-Sheet_0", "button_bargain-Sheet_1", "button_bargain-Sheet_2", "button_confront", "button_confront_2x", "button_contact",
            "button_glass", "button_integoration", "button_rewind_disabled", "button_rewind_highlight", "button_rewind_normal", "button_rewind_pressed",
            "button_unavaible", "button_zoom_0", "button_zoom_1", "button_zoom_2", "chem_scan_button", "dropdown_arrow",
            "general_button_2x", "general_button_2x_c", "general_button_2x_d", "general_button_2x_hl", "marketplace_scrollbar_back",
            "nego_button", "nego_button_Disabled", "nego_button_Hover", "nego_button_Pressed", "nego_buttonx2-1Normal", "nego_buttonx2-2Hover", "nego_buttonx2-3Pressed", "nego_buttonx2-4Disabled",
            "network_button_disabled", "network_button_hover", "network_button_normal", "network_button_pressed", "nightly_tab_reversed",
            "paper_buttons1", "paper_buttons2", "paper_buttons3", "paper_buttons4", "paper_buttons_close1", "paper_buttons_close2", "paper_buttons_close3", "paper_buttons_close4",
            "paper_buttons_l_1", "paper_buttons_l_2", "paper_buttons_l_3", "paper_buttons_l_4", "paper_buttons_screen_0", "paper_buttons_screen_1", "paper_buttons_screen_2", "paper_buttons_screen_3",
            "scrollbar_handle", "subpanel_x2", "top_button", "border-export", "borderOld", "border_black", "border_white", "border_tan",
            "border_bottom_resizable_button", "border_bottom_resizable_button_down", "border_outside_top_rounded", "border_outside_top_rounded_crop", "border_outside_top_rounded_new",
            "border_right_scrollbar", "border_right_scrollbar_button", "border_top_button_x", "border_top_button_x_inverted", "border_top_button_x_select",
            "button_disabled", "button_hover", "button_normal", "button_pressed", "button_up", "push_button_frame", "slider_background", "slider_button", "slider_thumb_hover", "slider_thumb_normal",
            "tab_plate", "text_background", "text_background_in",
            // R05: additional exact native UI chrome from the saved resource catalog.
            // Item frames, device internals, scene backgrounds and playtest logos are excluded.
            "indicator_frame", "bevel_frame", "progress_frame", "frame_hl", "slot_frame", "frame",
            "frame_2_square", "window_frame", "frame_2", "frame_notched", "reputation_background",
            "checkbox_checked", "checkbox_orange", "checkbox_checked_orange",
            "border_green", "border_red", "border_blue",
            "button_up_orange", "button_up_green", "button_up_purple", "button_up_blue", "button_up_red",
            "button_disabled_orange", "button_disabled_green", "button_disabled_purple", "button_disabled_blue", "button_disabled_red",
            "notebook_manual", "uibackOrange1L"
        };
        private static bool painting;
        private static float next;

        private static bool Decorative(string name)
        {
            return name == "notebook_manual" || name == "uibackOrange1L"
                || name.StartsWith("button_main_menu_", StringComparison.Ordinal)
                || name == "button_load" || name == "button_load_hl";
        }

        private static void RestoreColor(Image image, State state)
        {
            if (state.ColorChanged && image.color == state.AppliedColor)
            {
                painting = true;
                try { image.color = state.NativeColor; }
                finally { painting = false; }
            }
            state.ColorChanged = false;
        }

        private static void TintDecoration(Image image, State state)
        {
            if (state.Base == null || !Decorative(state.Base.name)) { RestoreColor(image, state); return; }
            Color native = state.ColorChanged && image.color == state.AppliedColor ? state.NativeColor : image.color;
            float value = native.r * .3f + native.g * .6f + native.b * .1f;
            Color tint = new Color(value, value * .97f, value * .90f, native.a);
            state.NativeColor = native;
            state.AppliedColor = tint;
            state.ColorChanged = true;
            painting = true;
            try { if (image.color != tint) image.color = tint; }
            finally { painting = false; }
        }

        private static bool Foreign(Sprite sprite)
        {
            if (ModLoader.Mods == null) return false;
            foreach (LoadedMod mod in ModLoader.Mods)
                if (mod.Textures != null && mod.Textures.ContainsValue(sprite.texture)) return true;
            return false;
        }

        private static Sprite Make(Sprite stock)
        {
            Sprite art;
            if (Sprites.TryGetValue(stock, out art)) return art;
            int width = Mathf.CeilToInt(stock.rect.width), height = Mathf.CeilToInt(stock.rect.height);
            Color32[] pixels = InteriorArt.ReadSpritePixels(stock, width, height);
            if (pixels == null) return null;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                Color32 native = pixels[index];
                if (native.a == 0) continue;
                int high = Math.Max(native.r, Math.Max(native.g, native.b));
                int low = Math.Min(native.r, Math.Min(native.g, native.b));
                // Preserve native warning, status and selected-state chroma verbatim.
                if (high - low > 45 && !Decorative(stock.name)) continue;
                float value = (native.r * 3 + native.g * 6 + native.b) / 2550f;
                Color32 color = Color32.Lerp(new Color32(43, 30, 23, 255), new Color32(232, 216, 178, 255), value);
                if (stock.name.StartsWith("notebook_manual_section", StringComparison.Ordinal))
                    color = new Color32(62, 41, 27, 255);
                int grain = (x * 13 + y * 7) % 5 - 2;
                color.r = (byte)Mathf.Clamp(color.r + grain, 0, 255);
                color.g = (byte)Mathf.Clamp(color.g + grain, 0, 255);
                color.b = (byte)Mathf.Clamp(color.b + grain, 0, 255);
                color.a = native.a;
                pixels[index] = color;
            }
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = stock.texture.filterMode;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            // Native alphaHitTestMinimumThreshold reads this texture on the CPU during raycasts.
            // The same sprite can be shared by images with different hit-test thresholds.
            texture.Apply(false, false);
            art = Sprite.Create(texture, new Rect(0, 0, width, height), stock.pivot / stock.rect.size,
                stock.pixelsPerUnit, 0, SpriteMeshType.FullRect, stock.border);
            art.name = "jinsong_poc.common." + stock.name;
            Sprites.Add(stock, art);
            return art;
        }

        private static void Apply(Image image, bool nativeBubble = false)
        {
            if (painting || ReportedUiSkin.Painting || !Mod.Active || image == null) return;
            if (ReportedUiSkin.OwnsSprite(image)) return;
            int scene = image.gameObject.scene.buildIndex;
            if (scene != 0 && (scene != 1 || Mod.Assets == null)) return;
            State state;
            Images.TryGetValue(image, out state);
            Sprite source = image.overrideSprite;
            if (state != null && source == state.Applied)
            {
                if (state.Base == image.sprite) { TintDecoration(image, state); return; }
                source = image.sprite;
            }
            if (source == null || source.packed || (!nativeBubble && !Names.Contains(source.name)) || Foreign(source))
            {
                if (state != null)
                {
                    RestoreColor(image, state);
                    if (image.overrideSprite == state.Applied)
                    {
                        painting = true;
                        try { image.overrideSprite = state.Base == image.sprite ? state.NativeOverride : null; }
                        finally { painting = false; }
                    }
                    Images.Remove(image);
                }
                return;
            }
            Sprite art = Make(source);
            if (art == null) return;
            if (state == null) Images.Add(image, state = new State());
            state.NativeOverride = source == image.sprite ? null : source;
            state.Base = image.sprite;
            state.Applied = art;
            painting = true;
            try { image.overrideSprite = art; }
            finally { painting = false; }
            TintDecoration(image, state);
        }

        // Hand the original override to a more specific skin before it takes ownership.
        internal static Sprite NativeSprite(Image image)
        {
            Sprite current = image.overrideSprite;
            State state;
            if (Images.TryGetValue(image, out state) && current == state.Applied)
                return (state.Base == image.sprite ? state.NativeOverride : null) ?? image.sprite;
            return current;
        }

        internal static Sprite Release(Image image)
        {
            Sprite current = NativeSprite(image);
            State state;
            if (Images.TryGetValue(image, out state))
            {
                RestoreColor(image, state);
                Images.Remove(image);
            }
            return current == image.sprite ? null : current;
        }

        internal static void Refresh(bool force = false)
        {
            if (!Mod.Active || (Mod.Assets == null && UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex != 0)
                || (!force && Time.unscaledTime < next)) return;
            next = Time.unscaledTime + 2f;
            // Initial serialized and newly instantiated UI only; native setter hooks handle state transitions.
            foreach (Image image in UnityEngine.Object.FindObjectsOfType<Image>(true)) Apply(image);
            var stale = new List<Image>();
            foreach (Image image in Images.Keys) if (image == null) stale.Add(image);
            foreach (Image image in stale) Images.Remove(image);
        }

        internal static void Restore()
        {
            painting = true;
            try
            {
                foreach (var pair in Images)
                    if (pair.Key != null)
                    {
                        if (pair.Key.overrideSprite == pair.Value.Applied) pair.Key.overrideSprite = pair.Value.NativeOverride;
                        if (pair.Value.ColorChanged && pair.Key.color == pair.Value.AppliedColor) pair.Key.color = pair.Value.NativeColor;
                    }
            }
            finally { painting = false; }
            Images.Clear();
            foreach (Sprite art in Sprites.Values)
                if (art != null) { UnityEngine.Object.Destroy(art.texture); UnityEngine.Object.Destroy(art); }
            Sprites.Clear();
            next = 0;
        }

        [HarmonyPatch]
        private static class NativeNameBubbles
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(DialogUIManager), "SetClientBubbleColor");
                yield return AccessTools.Method(typeof(DialogUIManager), "SetColor", new[] { typeof(string) });
                yield return AccessTools.Method(typeof(DialoguePhoneUIManager), "SetClientBubbleColor");
            }
            private static void Postfix(object __instance)
            {
                DialogUIManager shop = __instance as DialogUIManager;
                if (shop != null)
                {
                    ReportedUiSkin.RefreshSpeech();
                    if (shop.secNameBubble != null) Apply(shop.secNameBubble.GetComponent<Image>(), true);
                }
                DialoguePhoneUIManager phone = __instance as DialoguePhoneUIManager;
                if (phone != null) Apply(phone.clientNameBubbleImage, true);
            }
        }

        [HarmonyPatch(typeof(ReputationUIManager), "NotifyReputation", new[] { typeof(string), typeof(int) })]
        private static class NativeNotificationPlate
        {
            private static void Postfix(GameObject ___reputationNotifier)
            { if (___reputationNotifier != null) Apply(___reputationNotifier.GetComponent<Image>(), true); }
        }

        [HarmonyPatch]
        private static class NativeUiStates
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.PropertySetter(typeof(Image), "sprite");
                yield return AccessTools.PropertySetter(typeof(Image), "overrideSprite");
            }
            private static void Postfix(Image __instance)
            {
                if (!painting && !ReportedUiSkin.Painting && !ReportedUiSkin.RefreshChangedImage(__instance))
                    Apply(__instance);
            }
        }

        [HarmonyPatch(typeof(Graphic), "OnEnable")]
        private static class SerializedImageEnabled
        {
            private static void Postfix(Graphic __instance)
            {
                Image image = __instance as Image;
                if (image != null) Apply(image);
            }
        }

        [HarmonyPatch(typeof(Graphic), "set_color")]
        private static class NativeDecorationColors
        {
            private static void Postfix(Graphic __instance)
            {
                Image image = __instance as Image;
                if (image != null) Apply(image);
            }
        }
    }
}
