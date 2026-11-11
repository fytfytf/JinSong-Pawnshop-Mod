using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JinSongPoc
{
    internal static class ShopMenuSkin
    {
        private static readonly Dictionary<Image, ImageState> Images = new Dictionary<Image, ImageState>();
        private static readonly Dictionary<TMP_Text, TextState> Texts = new Dictionary<TMP_Text, TextState>();
        private static readonly Dictionary<Sprite, Sprite> Buttons = new Dictionary<Sprite, Sprite>();
        private static readonly Dictionary<Sprite, Sprite> GridCells = new Dictionary<Sprite, Sprite>();
        private static readonly Dictionary<Sprite, Sprite> GridFrames = new Dictionary<Sprite, Sprite>();
        private static readonly Dictionary<Sprite, Sprite> GlyphSprites = new Dictionary<Sprite, Sprite>();
        private static readonly Dictionary<Sprite, Sprite> BlankDividerSprites = new Dictionary<Sprite, Sprite>();
        private static readonly Dictionary<Image, Sprite[]> GlyphImages = new Dictionary<Image, Sprite[]>();
        private sealed class DividerPlacement
        {
            internal Vector2 Native, Applied;
            internal int Signature = Int32.MinValue;
            internal bool Blank;
        }
        private static readonly Dictionary<Image, DividerPlacement> DividerPlacements = new Dictionary<Image, DividerPlacement>();
        private static readonly Dictionary<string, Texture2D> DividerTextures = new Dictionary<string, Texture2D>();
        private static readonly PixelWindow[] Windows = new PixelWindow[3];
        private static readonly HashSet<PixelWindow> NativeWindows = new HashSet<PixelWindow>();
        private static readonly Color Ink = new Color32(54, 32, 21, 255);
        private static readonly Color Brass = new Color32(163, 115, 64, 255);
        private static readonly HashSet<string> TuneFaces = new HashSet<string> { "previous", "play", "pause", "next", "down", "up" };
        private static Sprite wood, paper, copper, grid;
        private static Color32[] woodGrain;
        private static string locale;

        private struct ImageState
        {
            internal Sprite Override;
            internal Image.Type Type;
            internal Color Color;
        }

        private struct TextState
        {
            internal string Stock, Applied;
            internal Color Color;
        }

        internal static void Apply(LoadedMod assets)
        {
            EmporiumEntry entry = EmporiumEntry.Instance;
            if (entry == null) return;
            if (wood == null)
            {
                Texture2D atlas;
                if (!assets.Textures.TryGetValue("jin_submenu_materials", out atlas)) return;
                int half = atlas.width / 2, middle = atlas.height / 2;
                wood = Tile(atlas, "wood", 0, middle, half, atlas.height - middle);
                paper = Tile(atlas, "paper", half, middle, atlas.width - half, atlas.height - middle);
                copper = Tile(atlas, "copper", 0, 0, half, middle);
                grid = Tile(atlas, "grid", half, 0, atlas.width - half, middle);
                woodGrain = atlas.GetPixels32();
                if (entry.invElement != null) entry.invElement.Validate();
            }
            string current = LocHelper.GetCurrentLocaleCode();
            bool localeChanged = current != locale;
            locale = current;
            GameItemElement documents = entry.docInvElement == null ? null : entry.docInvElement.childItem as GameItemElement;
            UpdateWindow(0, entry.trashcan == null ? null : entry.trashcan.contentWindow, localeChanged);
            UpdateWindow(1, documents == null ? null : documents.contentWindow, localeChanged);
            UpdateWindow(2, entry.cassettePlayer == null ? null : entry.cassettePlayer.contentWindow, localeChanged);
        }

        private static void UpdateWindow(int index, PixelWindow window, bool localeChanged)
        {
            if (window == Windows[index] && !localeChanged) return;
            Windows[index] = window;
            if (window == null) return;
            window.Validate();
            if (index == 2)
                foreach (Image image in window.rectTransform.GetComponentsInChildren<Image>(true))
                    if (image.sprite != null && TuneFaces.Contains(image.sprite.name.Split('_')[0])) Button(image);
        }

        private static Sprite Tile(Texture2D atlas, string name, int x, int y, int width, int height)
        {
            Sprite sprite = Sprite.Create(atlas, new Rect(x + 2, y + 2, width - 4, height - 4),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.name = "jinsong_poc.submenu." + name;
            return sprite;
        }

        private static bool Owns(RectTransform rect, bool tunesOnly = false)
        {
            if (wood == null || rect == null) return false;
            for (int i = tunesOnly ? 2 : 0; i < Windows.Length; i++)
            {
                PixelWindow window = Windows[i];
                if (window != null && window.rectTransform != null && rect.IsChildOf(window.rectTransform)) return true;
            }
            if (!tunesOnly)
                foreach (PixelWindow window in NativeWindows)
                    if (window != null && window.rectTransform != null && rect.IsChildOf(window.rectTransform)) return true;
            return false;
        }

        private static void Paint(Image image, Color color, Sprite art = null, bool preserveType = false)
        {
            if (image == null) return;
            if (!Images.ContainsKey(image))
                Images.Add(image, new ImageState { Color = image.color, Type = image.type,
                    Override = image.overrideSprite != image.sprite ? image.overrideSprite : null });
            if (art != null)
            {
                image.overrideSprite = art;
                if (!preserveType) image.type = Image.Type.Simple;
            }
            image.color = color;
        }

        private static void Paint(TreeNodeRender node, Color color, Sprite art = null)
        {
            if (node != null) Paint(node.image, color, art);
        }

        private static void Caption(TreeNodeRender node, Color color, string replacement = null, string stock = null)
        {
            if (node == null || node.text == null) return;
            TMP_Text text = node.text;
            TextState state;
            if (!Texts.TryGetValue(text, out state)) state.Color = text.color;
            if (stock != null) state.Stock = stock;
            state.Applied = replacement;
            Texts[text] = state;
            if (stock != null) text.text = replacement ?? stock;
            text.color = color;
        }

        internal static bool PaintGrid(GameGridInventory inventory, Image background, Image lines)
        {
            if (!IsThemedGrid(inventory)) return false;
            PaintGridImage(background, false);
            PaintGridImage(lines, true);
            return true;
        }

        // Cosmetic rows only. One original shared Sprite keeps one shared artwork.
        private static readonly string[][] StartingGlyphs = {
            new[] { "saving", "saving", "jin_trait_saving" },
            new[] { "bad_lease", "bad_lease", "jin_trait_bad_lease" },
            new[] { "convict", "convict", "jin_trait_convict" },
            new[] { "model_citizen", "model_citizen", "jin_trait_model_citizen" },
            new[] { "well_connected", "connected", "jin_trait_well_connected" },
            new[] { "diplomatic", "diplomatic", "jin_trait_diplomatic" },
            new[] { "intimidating", "intimidating", "jin_trait_intimidating" },
            new[] { "secret_admirer", "secret_admirer", "jin_trait_secret_admirer" },
            new[] { "blackmail", "blackmail", "jin_trait_blackmail" },
            new[] { "blabbermouth", "blabbermouth", "jin_trait_blabbermouth" },
            new[] { "colorful_character", "colorful", "jin_trait_colorful_character" },
            new[] { "side_street", "side_street", "jin_trait_side_street" },
            new[] { "prime_real_estate", "prime_real_estate", "jin_trait_prime_real_estate" },
            new[] { "sycophant", "sycophant", "jin_trait_sycophant" },
            new[] { "nicotine_addiction", "additionc_nicotine", "jin_trait_nicotine_addiction" },
            new[] { "narcotic_addiction", "addiction_narcotic", "jin_trait_narcotic_addiction" },
            new[] { "alcohol_addiction", "additionc_alcohol", "jin_trait_alcohol_addiction" },
            new[] { "gambling_addiction", "additionc_gambling", "jin_trait_gambling_addiction" },
            new[] { "sentinel", "sentinel", "jin_trait_sentinel" },
            new[] { "short_sleeper", "short_sleeper", "jin_trait_short_sleeper" },
            new[] { "degenerative_osteoarthritis", "degenerative_osteoarthritis", "jin_trait_degenerative_osteoarthritis" },
            new[] { "experienced_chemist", "noun-beaker-7821176-FFFFFF", "jin_trait_experienced_chemist" },
            new[] { "speedy", "stopwatch", "jin_trait_speedy" },
            new[] { "proficient_scavenger", "scav", "jin_trait_proficient_scavenger" },
            new[] { "careful_scavenger", "scav", "jin_trait_proficient_scavenger" },
            new[] { "handy", "handy", "jin_trait_handy" },
            new[] { "tinkerer", "tinkerer", "jin_trait_tinkerer" },
            new[] { "expert_negotiator", "expert_negotiator", "jin_trait_expert_negotiator" },
            new[] { "awful_negotiator", "awful_negotiator", "jin_trait_awful_negotiator" },
            new[] { "alert", "alert", "jin_trait_alert" },
            new[] { "sawyer_crew", "sawyer_crew", "jin_trait_sawyer_crew" },
            new[] { "revolution_sympathizer", "rev", "jin_trait_revolution_sympathizer" },
            new[] { "wild_favorite", "wild_favorite", "jin_trait_wild_favorite" },
            new[] { "wild_bad_side", "wild_favorite", "jin_trait_wild_favorite" },
            new[] { "known_snitch", "snitch", "jin_trait_known_snitch" },
            new[] { "public_enemy", "public_enemy", "jin_trait_public_enemy" },
            new[] { "loving_family", "loving_family", "jin_trait_loving_family" },
            new[] { "welfare_program", "welfare_program", "jin_trait_welfare_program" },
            new[] { "banned_commissary", "banned_commissary", "jin_trait_banned_commissary" },
            new[] { "vending", "vending", "jin_trait_vending" },
            new[] { "working_pipes", "working_pipes", "jin_trait_working_pipes" },
            new[] { "tapehead", "cassette", "jin_trait_tapehead" },
            new[] { "hoarder", "hoarder", "jin_trait_hoarder" },
            new[] { "makeshift_reactor", "makeshift_reactor", "jin_trait_makeshift_reactor" },
            new[] { "unsubscribed", "noun-flyer-7880108-FFFFFF", "jin_trait_unsubscribed" },
            new[] { "minimalist", "minimalist", "jin_trait_minimalist" },
            new[] { "agoraphobia", "agoraphobia", "jin_trait_agoraphobia" },
            new[] { "expedition_organizer", "noun-flag-7646741-FFFFFF", "jin_trait_expedition_organizer" },
            new[] { "illiterate", "illiterate", "jin_trait_illiterate" },
            new[] { "ascetic", "ascetic", "jin_trait_ascetic" },
            new[] { "maimed", "maimed", "jin_trait_maimed" },
            new[] { "aug_menace", "aug_menace", "jin_trait_aug_menace" },
            new[] { "lucky", "lucky", "jin_trait_lucky" },
            new[] { "unlucky", "lucky", "jin_trait_lucky" },
        };

        private static readonly string[][] GuideGlyphs = {
            new[] { "notebook_manual_section", "jin_divider_ink" },
            new[] { "notebook_manual_section_2x", "jin_divider_ink_2x" },
            new[] { "top_bun_deluxe", "jin_ref_top_bun_deluxe" },
            new[] { "wrap_open_flipped", "jin_ref_wrap_open_flipped" },
            new[] { "ribwich_logo_incorrect3", "jin_ref_ribwich_logo_incorrect3" },
            new[] { "pickles_extra", "jin_ref_pickles_extra" },
            new[] { "ribwich_logo_incorrect1", "jin_ref_ribwich_logo_incorrect1" },
            new[] { "davre_logo_incorrect2", "jin_ref_davre_logo_incorrect2" },
            new[] { "bottom_bun", "jin_ref_bottom_bun" },
            new[] { "pickles", "jin_ref_pickles" },
            new[] { "davre_logo_incorrect1", "jin_ref_davre_logo_incorrect1" },
            new[] { "top_bun_normal", "jin_ref_top_bun_normal" },
            new[] { "ribwich_logo_incorrect2", "jin_ref_ribwich_logo_incorrect2" },
            new[] { "bar_code", "jin_ref_bar_code" },
            new[] { "ribwich_logo", "jin_ref_ribwich_logo" },
            new[] { "facility", "jin_ref_facility" },
            new[] { "double_meat", "jin_ref_double_meat" },
            new[] { "davre_logo_incorrect7", "jin_ref_davre_logo_incorrect7" },
            new[] { "davre_logo_correct", "jin_ref_davre_logo_correct" },
            new[] { "warp_normal", "jin_ref_warp_normal" },
            new[] { "davre_logo_incorrect6", "jin_ref_davre_logo_incorrect6" },
            new[] { "davre_logo_incorrect3", "jin_ref_davre_logo_incorrect3" },
            new[] { "davre_logo_incorrect4", "jin_ref_davre_logo_incorrect4" },
            new[] { "ribs", "jin_ref_ribs" },
            new[] { "davre_logo_incorrect5", "jin_ref_davre_logo_incorrect5" },
            new[] { "guide_ribwich_logo", "jin_ref_guide_ribwich_logo" },
            new[] { "guide_ribwich_logo_incorrect", "jin_ref_guide_ribwich_logo_incorrect" },
            new[] { "guide_davre_logo", "jin_ref_guide_davre_logo" },
            new[] { "guide_davre_logo_incorrect", "jin_ref_guide_davre_logo_incorrect" },
            new[] { "seal_zhong", "jin_ref_seal_zhong" },
            new[] { "condition_reinforced", "jin_ref_condition_reinforced" },
            new[] { "condition_wear", "jin_ref_condition_wear" },
            new[] { "condition_screws", "jin_ref_condition_screws" },
            new[] { "condition_heavy_wear", "jin_ref_condition_heavy_wear" },
            new[] { "seal_ll", "jin_ref_seal_ll" },
            new[] { "cartridge", "jin_ref_cartridge" },
            new[] { "seal_ignition", "jin_ref_seal_ignition" },
            new[] { "seal_republic", "jin_ref_seal_republic" },
            new[] { "3gold_hand_2wear", "jin_ref_3gold_hand_2wear" },
            new[] { "3gold_hand_3damage", "jin_ref_3gold_hand_3damage" },
            new[] { "3gold_interface_emg", "jin_ref_3gold_interface_emg" },
            new[] { "3gold_addon_1armor", "jin_ref_3gold_addon_1armor" },
            new[] { "3gold_interface_symbol_ni", "jin_ref_3gold_interface_symbol_ni" },
            new[] { "3gold_interface_symbol_emgp", "jin_ref_3gold_interface_symbol_emgp" },
            new[] { "3gold_interface_ni", "jin_ref_3gold_interface_ni" },
            new[] { "3gold_interface_emgp", "jin_ref_3gold_interface_emgp" },
            new[] { "3gold_interface_symbol_emg", "jin_ref_3gold_interface_symbol_emg" },
            new[] { "3gold_hand_1normal", "jin_ref_3gold_hand_1normal" },
            new[] { "2carbon_int_full_emg", "jin_ref_2carbon_int_full_emg" },
            new[] { "2carbon_hand_1normal", "jin_ref_2carbon_hand_1normal" },
            new[] { "2carbon_int_symboll_emgp", "jin_ref_2carbon_int_symboll_emgp" },
            new[] { "2carbon_hand_2wear", "jin_ref_2carbon_hand_2wear" },
            new[] { "2carbon_addon_1armor", "jin_ref_2carbon_addon_1armor" },
            new[] { "2carbon_int_full_ni", "jin_ref_2carbon_int_full_ni" },
            new[] { "2carbon_int_full_emgp", "jin_ref_2carbon_int_full_emgp" },
            new[] { "2carbon_int_symboll_ni", "jin_ref_2carbon_int_symboll_ni" },
            new[] { "2carbon_int_symboll_emg", "jin_ref_2carbon_int_symboll_emg" },
            new[] { "2carbon_hand_3damage", "jin_ref_2carbon_hand_3damage" },
            new[] { "1steel_int_full_emgp", "jin_ref_1steel_int_full_emgp" },
            new[] { "1steel_hand_2wear", "jin_ref_1steel_hand_2wear" },
            new[] { "1steel_hand_3damage", "jin_ref_1steel_hand_3damage" },
            new[] { "1steel_int_syml_emg", "jin_ref_1steel_int_syml_emg" },
            new[] { "1steel_int_syml_ni", "jin_ref_1steel_int_syml_ni" },
            new[] { "1steel_hand_1normal", "jin_ref_1steel_hand_1normal" },
            new[] { "1steel_int_full_emg", "jin_ref_1steel_int_full_emg" },
            new[] { "1steel_int_full_ni", "jin_ref_1steel_int_full_ni" },
            new[] { "1steel_int_syml_emgp", "jin_ref_1steel_int_syml_emgp" },
            new[] { "1steel_addon_1armor", "jin_ref_1steel_addon_1armor" },
            new[] { "berry_2under2", "jin_ref_berry_2under2" },
            new[] { "berry_3over", "jin_ref_berry_3over" },
            new[] { "berry_1normal", "jin_ref_berry_1normal" },
            new[] { "berry_2under1", "jin_ref_berry_2under1" },
            new[] { "berry_4mold", "jin_ref_berry_4mold" },
            new[] { "berry_5small", "jin_ref_berry_5small" },
            new[] { "wine_berry", "jin_goods_wine_berry" },
            new[] { "yeast", "jin_goods_wine_yeast" },
            new[] { "water_ration", "jin_goods_water_ration" },
            new[] { "dream_dust", "jin_goods_dream_dust" },
            new[] { "dream_cap", "jin_goods_dream_cap" },
            new[] { "chem_finisher_b", "jin_goods_chem_finisher_b" },
            new[] { "heating_plate_b", "jin_goods_heating_plate_b" },
            new[] { "coffee_bean", "jin_goods_coffee_bean" },
            new[] { "med_bottle_violet", "jin_goods_med_bottle_violet" },
            new[] { "chemical2", "jin_goods_restricted_chemical" },
            new[] { "evaporator_b", "jin_goods_evaporator_b" },
            new[] { "blender_b", "jin_goods_blender_b" },
            new[] { "chem_module", "jin_goods_chem_module" },
            new[] { "chem_scanner", "jin_goods_chem_scanner" },
            new[] { "chemical1", "jin_goods_chemical1" },
            new[] { "plant_analyser", "jin_goods_plant_analyser" },
            new[] { "nutrient_tablet", "jin_goods_nutrient_tablet" },
            new[] { "aquascan", "jin_goods_aquascan" },
            new[] { "hydroponic", "jin_goods_hydroponic" },
            new[] { "nutrifruit", "jin_goods_nutrifruit" },
            new[] { "nutrifruit_seed", "jin_goods_nutrifruit_seed" },
            new[] { "battery100", "jin_goods_battery100" },
            new[] { "strip_new", "jin_goods_strip_new" },
            new[] { "water_test_container", "jin_goods_water_test_container" },
            new[] { "water_filter", "jin_goods_water_filter" },
            new[] { "water_tablet", "jin_goods_water_tablet" },
            new[] { "water_tablet_container", "jin_goods_water_tablet_container" },
            new[] { "guide_logo", "jin_guide_trade_mark" },
            new[] { "wine_tutorial", "jin_guide_wine_tutorial" },
            new[] { "fillerweed_p", "jin_guide_fillerweed_plant" },
        };

        internal static void RefreshGuideImages()
        {
            if (!Mod.Active || Mod.Assets == null) return;
            MultiPageUIManager pages = MultiPageUIManager.Instance;
            string[] nativeBooks = { "BookFuel", "BookWater", "BookFarm", "BookChemistry",
                "BookRibwich", "BookIngot", "BookHand", "BookWine" };
            if (pages != null && pages.books != null)
            foreach (GameObject book in pages.books)
            {
                if (book == null || book.scene.buildIndex != 1 || Array.IndexOf(nativeBooks, book.name) < 0) continue;
                ApplyReferenceImages(book);
            }
            // Native OnInit still chooses every appraisal state. The exact same
            // Sprite-name table themes the small/large view and the guide samples.
            InspectionUIManager inspection = InspectionUIManager.Instance;
            if (inspection != null && inspection.gameObject.scene.buildIndex == 1)
            {
                Component[] views = { inspection.insBerryUISmall, inspection.insBerryUILarge,
                    inspection.appHandUISmall, inspection.appHandUILarge,
                    inspection.fuelUISmall, inspection.fuelUILarge,
                    inspection.ribwichUISmall, inspection.ribwichUILarge };
                foreach (Component view in views)
                    if (view != null) ApplyReferenceImages(view.gameObject);
            }
        }

        private static void ApplyReferenceImages(GameObject root)
        {
                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                {
                    Sprite stock = image.sprite;
                    if (stock == null) continue;
                    foreach (string[] row in GuideGlyphs)
                        if (stock.name == row[0])
                        {
                            if (row[1] == "jin_divider_ink" || row[1] == "jin_divider_ink_2x")
                                ApplyDivider(image, row[1], root);
                            else ApplyGlyph(image, row[1]);
                            break;
                        }
                }
        }

        private static void ApplyGlyph(Image image, string artKey, bool blankDivider = false)
        {
            if (!Mod.Active || Mod.Assets == null || image == null) return;
            Sprite stock = image.sprite;
            if (stock == null || stock.packed) return;
            Sprite current = image.overrideSprite;
            if (current != stock && !GlyphSprites.ContainsValue(current) && !BlankDividerSprites.ContainsValue(current)) return;
            Texture2D art;
            if (artKey == "jin_divider_ink" || artKey == "jin_divider_ink_2x") art = Divider(artKey, blankDivider);
            else Mod.Assets.Textures.TryGetValue(artKey, out art);
            if (art == null && (artKey == "jin_trait_default" || artKey == "jin_trait_unknown"))
                art = TraitFallback(artKey);
            if (art == null
                || art.width != stock.rect.width || art.height != stock.rect.height) return;
            if (ModLoader.Mods != null)
                foreach (LoadedMod mod in ModLoader.Mods)
                    if (mod != Mod.Assets && mod.Textures != null && mod.Textures.ContainsValue(stock.texture)) return;
            Sprite replacement;
            Dictionary<Sprite, Sprite> cache = blankDivider ? BlankDividerSprites : GlyphSprites;
            if (!cache.TryGetValue(stock, out replacement))
            {
                Vector2 pivot = new Vector2(stock.pivot.x / stock.rect.width, stock.pivot.y / stock.rect.height);
                replacement = Sprite.Create(art, new Rect(0,0,art.width,art.height), pivot,
                    stock.pixelsPerUnit, 0, SpriteMeshType.FullRect, stock.border);
                replacement.name = "jinsong_poc.glyph." + artKey + (blankDivider ? ".blank" : "");
                cache.Add(stock, replacement);
            }
            var stale = new List<Image>();
            foreach (Image prior in GlyphImages.Keys) if (prior == null) stale.Add(prior);
            foreach (Image prior in stale) GlyphImages.Remove(prior);
            Sprite[] state;
            if (!GlyphImages.TryGetValue(image, out state))
            {
                state = new[] { current == stock ? null : current, replacement };
                GlyphImages.Add(image, state);
            }
            state[1] = replacement;
            image.overrideSprite = replacement;
        }

        // Small line graphics use code-native ink strokes; no generated illustration or source-pixel scan.
        private static Texture2D Divider(string key, bool blank = false)
        {
            Texture2D texture;
            string cacheKey = key + (blank ? ".blank" : "");
            if (DividerTextures.TryGetValue(cacheKey, out texture)) return texture;
            int scale = key.EndsWith("_2x", StringComparison.Ordinal) ? 2 : 1;
            int width = 213 * scale, height = 14 * scale;
            Color32[] pixels = new Color32[width * height];
            if (!blank) for (int x = width * 55 / 100; x < width * 95 / 100; x++)
            {
                int wave = (x / (17 * scale)) % 3 == 1 ? scale : 0;
                for (int y = 0; y < scale; y++)
                    pixels[(scale + wave + y) * width + x] =
                        new Color32(65, 44, 31, (byte)(x % (11 * scale) < scale ? 112 : 190));
            }
            if (!blank) for (int x = width * 3 / 4 - scale; x < width * 3 / 4 + scale; x++)
                for (int y = scale; y < 3 * scale; y++)
                    pixels[y * width + x] = new Color32(143, 100, 52, 215);
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "jinsong_poc." + cacheKey;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            DividerTextures.Add(cacheKey, texture);
            return texture;
        }

        private static Rect TextInImage(TMP_Text text, RectTransform image)
        {
            Bounds bounds = text.textBounds;
            Vector3 min = bounds.min, max = bounds.max;
            Vector2 low = new Vector2(Single.PositiveInfinity, Single.PositiveInfinity);
            Vector2 high = new Vector2(Single.NegativeInfinity, Single.NegativeInfinity);
            foreach (Vector3 point in new[] { new Vector3(min.x, min.y), new Vector3(min.x, max.y),
                new Vector3(max.x, min.y), new Vector3(max.x, max.y) })
            {
                Vector2 local = image.InverseTransformPoint(text.transform.TransformPoint(point));
                low = Vector2.Min(low, local); high = Vector2.Max(high, local);
            }
            return Rect.MinMaxRect(low.x, low.y, high.x, high.y);
        }

        private static bool WarningText(TMP_Text text)
        {
            string value = (text.text ?? "").Trim();
            return value.IndexOf("WARNING", StringComparison.OrdinalIgnoreCase) >= 0
                || value.Contains("警告") || value.Contains("立即就医");
        }

        private static void ApplyDivider(Image image, string artKey, GameObject book)
        {
            Sprite stock = image.sprite, current = image.overrideSprite;
            if (stock == null || stock.packed || (current != stock && !GlyphSprites.ContainsValue(current)
                && !BlankDividerSprites.ContainsValue(current))) return;
            if (ModLoader.Mods != null)
                foreach (LoadedMod mod in ModLoader.Mods)
                    if (mod != Mod.Assets && mod.Textures != null && mod.Textures.ContainsValue(stock.texture)) return;
            if (!image.gameObject.activeInHierarchy) { ApplyGlyph(image, artKey); return; }
            DividerPlacement state;
            if (!DividerPlacements.TryGetValue(image, out state))
                DividerPlacements.Add(image, state = new DividerPlacement { Native = image.rectTransform.anchoredPosition });
            Vector2 native = image.rectTransform.anchoredPosition == state.Applied ? state.Native : image.rectTransform.anchoredPosition;
            Transform page = image.transform.parent;
            for (Transform parent = image.transform.parent; parent != null && parent != book.transform; parent = parent.parent)
                if (parent.name.StartsWith("Page", StringComparison.Ordinal)) { page = parent; break; }
            TMP_Text[] texts = page.GetComponentsInChildren<TMP_Text>();
            int signature = LocHelper.GetCurrentLocaleCode().GetHashCode() ^ native.GetHashCode() ^ image.rectTransform.rect.size.GetHashCode();
            foreach (TMP_Text text in texts)
                signature = unchecked(signature * 31 + (text.text ?? "").GetHashCode() + text.fontSize.GetHashCode() + text.rectTransform.anchoredPosition.GetHashCode());
            if (signature == state.Signature) { ApplyGlyph(image, artKey, state.Blank); return; }
            state.Native = native;
            image.rectTransform.anchoredPosition = native;
            state.Blank = false;
            Rect rect = image.rectTransform.rect;
            var bounds = new List<Rect>();
            foreach (TMP_Text text in texts)
            {
                if (String.IsNullOrWhiteSpace(text.text) || text.font == null) continue;
                text.ForceMeshUpdate();
                Rect glyphs = TextInImage(text, image.rectTransform);
                if (glyphs.width <= 0 || glyphs.height <= 0) continue;
                bounds.Add(glyphs);
                if (WarningText(text) && glyphs.Overlaps(rect)) state.Blank = true;
            }
            Rect line = Rect.MinMaxRect(rect.xMin + rect.width * .55f, rect.yMin + rect.height / 14f,
                rect.xMin + rect.width * .95f, rect.yMin + rect.height * 3f / 14f);
            float shift = 0f;
            for (int pass = 0; pass < 4 && !state.Blank; pass++)
            {
                bool moved = false;
                foreach (Rect glyphs in bounds)
                    if (line.Overlaps(glyphs))
                    {
                        float delta = glyphs.yMin - 2f - line.yMax;
                        shift += delta; line.y += delta; moved = true;
                    }
                if (!moved) break;
            }
            if (Mathf.Abs(shift) > rect.height || bounds.Exists(b => b.Overlaps(line))) state.Blank = true;
            state.Applied = native + (state.Blank ? Vector2.zero : new Vector2(0, shift));
            image.rectTransform.anchoredPosition = state.Applied;
            state.Signature = signature;
            ApplyGlyph(image, artKey, state.Blank);
        }

        private static Texture2D TraitFallback(string key)
        {
            Texture2D texture;
            if (DividerTextures.TryGetValue(key, out texture)) return texture;
            Color32[] pixels = new Color32[256];
            for (int y = 2; y < 14; y++) for (int x = 2; x < 14; x++)
                pixels[y * 16 + x] = new Color32(214, 193, 148, 255);
            Color32 ink = new Color32(65, 44, 31, 255);
            if (key == "jin_trait_unknown")
            {
                int[] points = { 167, 168, 169, 182, 186, 154, 137, 120, 104, 72 };
                foreach (int point in points) pixels[point] = ink;
            }
            else
                for (int x = 5; x < 11; x++) pixels[8 * 16 + x] = ink;
            texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.name = "jinsong_poc." + key;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            DividerTextures.Add(key, texture);
            return texture;
        }

        // Original SetIcon still chooses the tier and applies all native colors/availability.
        [HarmonyPatch(typeof(PerkElement), "SetIcon")]
        private static class ReputationFaces
        {
            private static void Postfix(PerkElement __instance)
            {
                if (!Mod.Active || __instance == null || __instance.image == null) return;
                ReputationUIManager manager = ReputationUIManager.Instance;
                if (manager == null) return;
                Sprite[] native = { manager.spriteStar, manager.spriteHappy, manager.spriteSmile,
                    manager.spriteSad, manager.spriteAngry, manager.spriteSkull };
                int index = Array.IndexOf(native, __instance.image.sprite);
                if (index < 0)
                {
                    Sprite stock = __instance.image.sprite;
                    if (stock != null && (stock.name == "unknown" || stock.name == "default"))
                        ApplyGlyph(__instance.image, "jin_trait_" + stock.name);
                    return;
                }
                string[] keys = { "star", "happy", "smile", "sad", "angry", "skull" };
                ApplyGlyph(__instance.image, "jin_reputation_" + keys[index]);
            }
        }

        [HarmonyPatch(typeof(StartingPerkElement), "Start")]
        private static class StartingTraitFaces
        {
            private static void Postfix(StartingPerkElement __instance)
            {
                if (!Mod.Active || __instance == null || __instance.perk == null || __instance.icon == null) return;
                StartingPerk perk = __instance.perk;
                // Keep explicit custom icons and raw/provider-driven MOD traits in native priority order.
                if (!String.IsNullOrEmpty(perk.iconKey) || !String.IsNullOrEmpty(perk.rawName)
                    || !String.IsNullOrEmpty(perk.rawDescription) || perk.nameProvider != null || perk.descriptionProvider != null) return;
                Sprite stock = __instance.icon.sprite;
                if (stock == null) return;
                foreach (string[] row in StartingGlyphs)
                    if (perk.id == row[0] && stock.name == row[1])
                    {
                        ApplyGlyph(__instance.icon, row[2]);
                        return;
                    }
                if (stock.name == "unknown" || stock.name == "default")
                    ApplyGlyph(__instance.icon, "jin_trait_" + stock.name);
            }
        }

        private static bool IsThemedGrid(GameGridInventory inventory)
        {
            EmporiumEntry entry = EmporiumEntry.Instance;
            return woodGrain != null && ((entry != null && (inventory == entry.invElement
                || UnifiedInterior.IsHotspot && inventory == entry.hiddenElement)) || Owns(inventory.rectTransform));
        }

        private static void PaintGridImage(Image image, bool frame)
        {
            if (image == null || image.sprite == null) return;
            Sprite stock = image.sprite;
            Dictionary<Sprite, Sprite> variants = frame ? GridFrames : GridCells;
            Sprite art;
            if (!variants.TryGetValue(stock, out art))
            {
                art = BuildGridSprite(stock, frame);
                variants.Add(stock, art);
            }
            if (art != null) Paint(image, new Color(1f, 1f, 1f, image.color.a), art, preserveType: true);
        }

        private static Sprite BuildGridSprite(Sprite stock, bool frame)
        {
            int width = Mathf.CeilToInt(stock.rect.width), height = Mathf.CeilToInt(stock.rect.height);
            Color32[] pixels = InteriorArt.ReadSpritePixels(stock, width, height);
            if (pixels == null) return null;
            int darkest = 255, lightest = 0;
            foreach (Color32 pixel in pixels)
            {
                if (pixel.a == 0) continue;
                int light = Luma(pixel);
                darkest = Math.Min(darkest, light);
                lightest = Math.Max(lightest, light);
            }
            Color32 shadow = frame ? new Color32(61, 41, 28, 255) : new Color32(42, 30, 24, 255);
            Color32 lightWood = frame ? new Color32(126, 90, 57, 255) : new Color32(86, 64, 46, 255);
            Sprite material = frame ? wood : grid;
            Rect sample = material.rect;
            int stride = material.texture.width;
            for (int y = 0; y < height; y++)
            {
                int row = ((int)sample.y + y % (int)sample.height) * stride;
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    Color32 original = pixels[i];
                    if (original.a == 0) continue;
                    float value = lightest > darkest ? Mathf.InverseLerp(darkest, lightest, Luma(original)) : 0.45f;
                    int grain = Luma(woodGrain[row + (int)sample.x + x % (int)sample.width]);
                    Color32 color = Color32.Lerp(shadow, lightWood, Mathf.Clamp01(value * 0.90f + grain / 500f));
                    color.a = original.a;
                    pixels[i] = color;
                }
            }
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            Sprite art = Sprite.Create(texture, new Rect(0, 0, width, height), stock.pivot / stock.rect.size,
                stock.pixelsPerUnit, 0, SpriteMeshType.FullRect, stock.border);
            art.name = "jinsong_poc.grid." + (frame ? "frame." : "cell.") + stock.name;
            return art;
        }

        private static int Luma(Color32 color) { return (color.r * 3 + color.g * 6 + color.b) / 10; }

        private static void Button(Image image)
        {
            if (image == null || image.sprite == null) return;
            Sprite stock = image.sprite;
            Sprite art;
            if (!Buttons.TryGetValue(stock, out art))
            {
                int width = Mathf.CeilToInt(stock.rect.width), height = Mathf.CeilToInt(stock.rect.height);
                Color32[] pixels = InteriorArt.ReadSpritePixels(stock, width, height);
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 pixel = pixels[i];
                    bool glyph = pixel.r - pixel.g > 20 && pixel.r - pixel.b > 20;
                    if (glyph)
                    {
                        pixels[i] = new Color32(239, 198, 135, pixel.a);
                    }
                    else
                    {
                        pixels[i] = new Color32((byte)(pixel.r * 0.57f), (byte)(pixel.g * 0.41f),
                            (byte)(pixel.b * 0.27f), pixel.a);
                    }
                }
                Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                art = Sprite.Create(texture, new Rect(0, 0, width, height), stock.pivot / stock.rect.size,
                    stock.pixelsPerUnit, 0, SpriteMeshType.FullRect, stock.border);
                art.name = "jinsong_poc.submenu." + stock.name;
                Buttons.Add(stock, art);
            }
            Paint(image, image.color, art);
        }

        internal static void Restore()
        {
            foreach (var pair in DividerPlacements)
                if (pair.Key != null && pair.Key.rectTransform.anchoredPosition == pair.Value.Applied)
                    pair.Key.rectTransform.anchoredPosition = pair.Value.Native;
            DividerPlacements.Clear();
            foreach (var pair in GlyphImages)
                if (pair.Key != null && pair.Key.overrideSprite == pair.Value[1]) pair.Key.overrideSprite = pair.Value[0];
            GlyphImages.Clear();
            foreach (Sprite glyph in GlyphSprites.Values) if (glyph != null) UnityEngine.Object.Destroy(glyph);
            GlyphSprites.Clear();
            foreach (Sprite glyph in BlankDividerSprites.Values) if (glyph != null) UnityEngine.Object.Destroy(glyph);
            BlankDividerSprites.Clear();
            foreach (Texture2D texture in DividerTextures.Values)
                if (texture != null) UnityEngine.Object.Destroy(texture);
            DividerTextures.Clear();
            foreach (KeyValuePair<Image, ImageState> saved in Images)
            {
                if (saved.Key == null) continue;
                saved.Key.overrideSprite = saved.Value.Override;
                saved.Key.type = saved.Value.Type;
                saved.Key.color = saved.Value.Color;
            }
            foreach (KeyValuePair<TMP_Text, TextState> saved in Texts)
            {
                if (saved.Key == null) continue;
                if (saved.Value.Applied != null && saved.Key.text == saved.Value.Applied)
                    saved.Key.text = saved.Value.Stock;
                saved.Key.color = saved.Value.Color;
            }
            foreach (Sprite art in Buttons.Values)
            {
                UnityEngine.Object.Destroy(art.texture);
                UnityEngine.Object.Destroy(art);
            }
            DisposeGridSprites(GridCells);
            DisposeGridSprites(GridFrames);
            foreach (Sprite art in new Sprite[] { wood, paper, copper, grid })
                if (art != null) UnityEngine.Object.Destroy(art);
            Images.Clear();
            Texts.Clear();
            Buttons.Clear();
            Array.Clear(Windows, 0, Windows.Length);
            NativeWindows.Clear();
            wood = paper = copper = grid = null;
            woodGrain = null;
            locale = null;
        }

        private static void DisposeGridSprites(Dictionary<Sprite, Sprite> variants)
        {
            foreach (Sprite art in variants.Values)
            {
                if (art == null) continue;
                UnityEngine.Object.Destroy(art.texture);
                UnityEngine.Object.Destroy(art);
            }
            variants.Clear();
        }

        [HarmonyPatch(typeof(PixelWindow), "Validate")]
        private static class WindowChrome
        {
            private static void Postfix(PixelWindow __instance, TreeNodeRender ___background, TreeNodeRender ___outsideBorder,
                TreeNodeRender ___topDragBorder, TreeNodeRender ___topCloseButton, TreeNodeRender ___topTitleBackground,
                TreeNodeRender ___titleText, TreeNodeRender ___machineryHeader, TreeNodeRender ___machineryRule,
                TreeNodeRender ___machineryRuleBacking, TreeNodeRender ___machineryPlate, TreeNodeRender ___machineryIntake)
            {
                if (!Mod.Active || wood == null) return;
                EmporiumEntry entry = EmporiumEntry.Instance;
                // These item trays sit directly on the room's painted counter. Keep the
                // native grid/hit area, but do not put a second wooden board over that art.
                if (UnifiedInterior.IsHotspot && entry != null
                    && (__instance == entry.frontInvWindow || __instance == entry.backInvWindow || __instance == entry.hiddenWindow))
                {
                    Paint(___background, Color.clear);
                    if (___outsideBorder != null) Paint(___outsideBorder, Color.clear);
                    return;
                }
                int kind = Array.IndexOf(Windows, __instance);
                if (kind < 0)
                {
                    if (__instance.rectTransform == null || __instance.rectTransform.gameObject.scene.buildIndex != 1
                        || ___background == null || ___background.image == null) return;
                    Sprite native = ___background.image.sprite;
                    Sprite active = ___background.image.overrideSprite;
                    if (native == null || (active != native && active != null
                        && !active.name.StartsWith("jinsong_poc.", StringComparison.Ordinal))) return;
                    if (ModLoader.Mods != null)
                        foreach (LoadedMod mod in ModLoader.Mods)
                            if (mod != Mod.Assets && mod.Textures != null && mod.Textures.ContainsValue(native.texture)) return;
                    NativeWindows.Add(__instance);
                }
                Paint(___background, Color.white, wood);
                if (___outsideBorder != null) PaintGridImage(___outsideBorder.image, true);
                if (___machineryPlate != null && ___machineryPlate.image.enabled) PaintGridImage(___machineryPlate.image, true);
                if (___machineryIntake != null && ___machineryIntake.image.enabled) PaintGridImage(___machineryIntake.image, true);
                Paint(___topDragBorder, Color.white, copper);
                Paint(___machineryHeader, Color.white, copper);
                Paint(___machineryRule, Brass);
                Paint(___machineryRuleBacking, Ink);
                Paint(___topTitleBackground, Color.white, paper);
                if (___topCloseButton != null) Button(___topCloseButton.image);
                Caption(___titleText, ___topTitleBackground != null && ___topTitleBackground.image != null
                    && ___topTitleBackground.image.isActiveAndEnabled ? Ink : (Color)new Color32(237, 200, 143, 255),
                    stock: __instance.titleString);
            }
        }

        [HarmonyPatch(typeof(MachineryPanel), "Validate")]
        private static class TunePanels
        {
            private static void Postfix(TreeNodeRender ___plate)
            {
                if (Mod.Active && ___plate != null && Owns(___plate.rectTransform))
                    Paint(___plate, Color.white, grid);
            }
        }

        [HarmonyPatch(typeof(MachineryIntake), "Validate")]
        private static class TuneGridIntakes
        {
            private static void Postfix(MachineryIntake __instance, TreeNodeRender ___plate)
            {
                if (!Mod.Active || ___plate == null || !Owns(___plate.rectTransform)) return;
                GameGridInventory storage = __instance.content as GameGridInventory;
                if (storage == null) return;
                PaintGridImage(___plate.image, true);
                // The machinery layout refreshes widgets but skips GameGridInventory.
                storage.Validate();
            }
        }

        [HarmonyPatch(typeof(GameGridInventory), "ValidateBackground")]
        private static class DynamicGrid
        {
            private static void Postfix(GameGridInventory __instance, TreeNodeRender ___dynamicBackgroundBase,
                List<TreeNodeRender> ___dynamicBackgrounds, List<TreeNodeRender> ___dynamicR0,
                List<TreeNodeRender> ___dynamicR1, List<TreeNodeRender> ___dynamicR2, List<TreeNodeRender> ___dynamicR3)
            {
                if (!Mod.Active || !IsThemedGrid(__instance) || ___dynamicBackgroundBase == null
                    || !___dynamicBackgroundBase.gameObject.activeSelf) return;
                PaintDynamic(___dynamicBackgrounds, false);
                PaintDynamic(___dynamicR0, true);
                PaintDynamic(___dynamicR1, true);
                PaintDynamic(___dynamicR2, true);
                PaintDynamic(___dynamicR3, true);
            }

            private static void PaintDynamic(List<TreeNodeRender> nodes, bool frame)
            {
                foreach (TreeNodeRender node in nodes)
                    if (node != null && node.gameObject.activeSelf) PaintGridImage(node.image, frame);
            }
        }

        [HarmonyPatch(typeof(MachineryLabel), "Validate")]
        private static class TuneLabels
        {
            private static void Postfix(TreeNodeRender ___plate, TreeNodeRender ___textNode, string ___text)
            {
                if (!Mod.Active || ___plate == null || !Owns(___plate.rectTransform)) return;
                Paint(___plate, Color.white, paper);
                string caption = Mod.Chinese && ___text == LocHelper.GetLocalizedMechanic("mech_cassette_player_label_cassette")
                    ? Mod.Text("fixture_tune_label") : null;
                Caption(___textNode, Ink, caption, ___text);
            }
        }

        [HarmonyPatch(typeof(MachineryButton), "ApplyState")]
        private static class TuneButtons
        {
            private static void Postfix(TreeNodeRender ___plate)
            {
                if (Mod.Active && ___plate != null && Owns(___plate.rectTransform)) Button(___plate.image);
            }
        }
    }
}
