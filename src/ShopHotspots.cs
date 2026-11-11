using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace JinSongPoc
{
    internal static class ShopHotspots
    {
        internal const string ArtKey = "jin_hotspot_shop";
        internal const int StockButtonZone = 15;
        // Source landmarks measured in the approved 1671 × 941 painting.
        internal static readonly int[] Columns = { 0, 569, 1109, 1671 };
        internal static readonly int[] Rows = { 0, 66, 454, 484, 606, 941 };
        internal static readonly int[] SceneColumns = { 0, 338, 623, 960 };
        internal static readonly int[] SceneRows = { 0, 26, 207, 256, 347, 540 };
        private static readonly RectInt[] Bounds = {
            new RectInt(22,110,130,545), new RectInt(304,107,160,214), new RectInt(466,152,42,158),
            new RectInt(259,429,76,74), new RectInt(398,468,76,63), new RectInt(1125,236,42,62),
            new RectInt(1128,313,42,46), new RectInt(577,451,510,37), new RectInt(413,533,265,60),
            new RectInt(715,498,123,97), new RectInt(839,498,112,97), new RectInt(996,490,142,86),
            new RectInt(1191,523,219,76), new RectInt(1417,531,120,68), new RectInt(761,617,77,43),
            new RectInt(849,617,77,43), new RectInt(1216,83,202,278), new RectInt(1549,46,114,112),
            new RectInt(1467,222,199,150), new RectInt(1466,399,203,111), new RectInt(117,685,123,171),
            new RectInt(241,626,67,166), new RectInt(241,838,76,40), new RectInt(566,490,146,40)
        };
        private static readonly Interactable[] Targets = new Interactable[Bounds.Length];
        private static readonly RectInt[] SceneBounds = Array.ConvertAll(Bounds, ToScene);
        private static readonly RectInt MemoPapers = ToScene(new RectInt(271, 315, 207, 111));
        private static readonly RectInt ShowcaseNiche = ToScene(new RectInt(298, 137, 145, 147));
        private static readonly RectInt WallCalendar = ToScene(new RectInt(268, 39, 246, 64));
        private static readonly RectInt WallLedger = new RectInt(195, 190, 106, 40);
        private static readonly RectInt PlayerTrade = ToScene(new RectInt(678, 506, 315, 51));
        private static readonly RectInt FullTrade = ToScene(new RectInt(568, 489, 548, 86));
        internal static readonly RectInt SideAbacus = ToScene(new RectInt(386, 548, 170, 46));
        internal static readonly RectInt SideScale = ToScene(new RectInt(1121, 527, 69, 72));
        internal static readonly RectInt QueuePaper = ToScene(new RectInt(522, 154, 36, 64));
        internal static readonly RectInt QueueNumber = ToScene(new RectInt(525, 168, 30, 43));
        internal static RectInt CalendarArt { get { return WallCalendar; } }
        internal static RectInt LedgerArt { get { return WallLedger; } }
        internal static RectInt LedgerInk
        {
            get
            {
                return new RectInt(WallLedger.x + 8, WallLedger.y + 8, WallLedger.width - 16, WallLedger.height - 11);
            }
        }
        private static readonly Dictionary<Image, HighlightAppearance> DropTargets = new Dictionary<Image, HighlightAppearance>();
        private static readonly Color32[] DropColors = {
            new Color32(235, 109, 93, 225), new Color32(149, 220, 131, 225), new Color32(131, 207, 241, 220),
            new Color32(241, 237, 224, 220), new Color32(41, 34, 26, 225), new Color32(225, 186, 121, 225)
        };
        private static GameObject root;
        private static Sprite edge, dropOutline;
        private static SpriteRenderer[] strokes;
        internal static int ZoneCount { get { return Bounds.Length; } }

        private struct HighlightAppearance
        {
            internal Sprite Override;
            internal Image.Type Type;
            internal bool FillCenter;
            internal Color Color;
        }

        internal static RectInt Zone(int number)
        {
            if (UnifiedInterior.HasOpenTradeLayout)
            {
                if (number == 9) return SideAbacus;
                if (number == 12) return SideScale;
                if (number == 24) return FullTrade;
            }
            if (UnifiedInterior.HasCounterLayout)
            {
                if (number == 10) return WallCalendar;
                if (number == 11) return new RectInt(WallLedger.x, WallLedger.y + 5, WallLedger.width, WallLedger.height - 5);
                if (number == 24) return PlayerTrade;
            }
            if (UnifiedInterior.HasLeftShowcase)
            {
                if (number == 2) return MemoPapers;
                if (number == 19) return ShowcaseNiche;
            }
            return SceneBounds[number - 1];
        }

        internal static RectInt ToScene(RectInt region)
        {
            int x = Map(region.xMin, Columns, SceneColumns), y = Map(region.yMin, Rows, SceneRows);
            return new RectInt(x, y, Map(region.xMax, Columns, SceneColumns) - x, Map(region.yMax, Rows, SceneRows) - y);
        }

        private static int Map(int value, int[] source, int[] target)
        {
            for (int i = 1; i < source.Length; i++)
                if (value <= source[i])
                    return Mathf.RoundToInt(Mathf.Lerp(target[i - 1], target[i], Mathf.InverseLerp(source[i - 1], source[i], value)));
            return target[target.Length - 1];
        }

        internal static void Bind(Transform parent)
        {
            root = new GameObject("JinSong.HotspotOutline");
            root.transform.SetParent(parent, false);
            Texture2D pixel = new Texture2D(5, 5, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[25];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            pixel.SetPixels32(pixels);
            pixel.Apply(false, false);
            Vector2 center = new Vector2(0.5f, 0.5f);
            edge = Sprite.Create(pixel, new Rect(0, 0, 5, 5), center, 5f);
            dropOutline = Sprite.Create(pixel, new Rect(0, 0, 5, 5), center, 100f, 0,
                SpriteMeshType.FullRect, Vector4.one * 2f);
            dropOutline.name = "jinsong_poc.hotspot.drop_outline";
            strokes = new SpriteRenderer[8];
            for (int i = 0; i < strokes.Length; i++)
            {
                GameObject side = new GameObject("Edge " + i);
                side.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = side.AddComponent<SpriteRenderer>();
                renderer.sprite = edge;
                renderer.sortingOrder = 31 + i / 4;
                renderer.color = i < 4 ? new Color(1f, 0.69f, 0.27f, 0.20f) : new Color(1f, 0.86f, 0.54f, 0.70f);
                strokes[i] = renderer;
            }
            root.SetActive(false);
        }

        internal static void Track(int number, Interactable target) { Targets[number - 1] = target; }

        private static bool IsPawnshopStockButton(StoreTransferButton button)
        {
            MapUIManager map = MapUIManager.Instance;
            Interactable target = Targets[StockButtonZone - 1];
            return Mod.Active && UnifiedInterior.IsHotspot && map != null && map.Current == null
                && target != null && target.gameObject == button.gameObject;
        }

        [HarmonyPatch(typeof(StoreTransferButton), "OnClick")]
        private static class ReturnCounterStock
        {
            private static bool Prefix(StoreTransferButton __instance)
            {
                if (!IsPawnshopStockButton(__instance)) return true;
                EmporiumEntry entry = EmporiumEntry.Instance;
                if (entry == null || entry.backInvinvElement == null || entry.invElement == null) return false;

                if (__instance.audioSource != null) __instance.audioSource.Play();
                entry.TransferOwnedItemBackToInv();
                TutorialUIManager tutorial = TutorialUIManager.Instance;
                if (tutorial != null) tutorial.OnTransferButtonClicked();
                return false;
            }
        }

        [HarmonyPatch(typeof(StoreTransferButton), "GetTooltip")]
        private static class ReturnCounterStockTooltip
        {
            private static void Postfix(StoreTransferButton __instance, ref string __result)
            {
                if (IsPawnshopStockButton(__instance))
                    __result = Mod.CurrentText("fixture_counter_to_stock") ?? __result;
            }
        }

        private static void FrameDropTarget(GameSlotInventory slot, Image image, RenderHandler.BackgroundColor color)
        {
            int index = (int)color;
            EmporiumEntry entry = EmporiumEntry.Instance;
            if (dropOutline == null || image == null || entry == null || index < 0 || index >= DropColors.Length) return;
            // The fixture's artwork is painted into the room beneath the native slot.
            if (slot != entry.docInvElement && slot != entry.trashInvElement
                && slot != entry.faucetElement && slot != entry.drainInvElement) return;
            if (!DropTargets.ContainsKey(image))
                DropTargets.Add(image, new HighlightAppearance { Override = image.overrideSprite == image.sprite ? null : image.overrideSprite,
                    Type = image.type, FillCenter = image.fillCenter, Color = image.color });
            image.overrideSprite = dropOutline;
            image.type = Image.Type.Sliced;
            image.fillCenter = false;
            image.color = DropColors[index];
        }

        [HarmonyPatch(typeof(GameSlotInventory), "HighlightSlot")]
        private static class FixedSlotHighlight
        {
            private static void Postfix(GameSlotInventory __instance, RenderHandler.BackgroundColor __1,
                List<TreeNodeRender> __result, TreeNodeRender ___highlight)
            {
                if (Mod.Active && UnifiedInterior.IsHotspot && __result != null && __result.Count > 0 && ___highlight != null)
                    FrameDropTarget(__instance, ___highlight.image, __1);
            }
        }

        internal static void UpdateHover()
        {
            if (root == null) return;
            Camera camera = Camera.main;
            Vector3 mouse = Input.mousePosition;
            StoreUIManager ui = StoreUIManager.Instance;
            if (camera == null || ui == null || !camera.pixelRect.Contains(mouse) || !Application.isFocused
                || Interactable.isPaused || StoreUIManager.IsAnyTyping() || ui.isShootingMode)
            {
                root.SetActive(false);
                return;
            }
            Vector3 world = camera.ScreenToWorldPoint(mouse);
            Vector2 scene = new Vector2(480f + world.x * 100f, 270f - world.y * 100f);
            int zone = HitAt(scene, ui.hoveringStoreUI);
            Show(zone);
        }

        private static int HitAt(Vector2 point, bool overUi)
        {
            for (int i = 1; i <= Bounds.Length; i++)
            {
                RectInt rect = Zone(i);
                if (point.x < rect.xMin || point.x >= rect.xMax || point.y < rect.yMin || point.y >= rect.yMax) continue;
                Interactable target = Targets[i - 1];
                if (target != null)
                    return !overUi && target.isActiveAndEnabled && target.isAvailable ? i : 0;
                PixelWindow window = ConceptFixtures.HotspotWindow(i);
                return window != null && window.IsVisible() && window.rectTransform.gameObject.activeInHierarchy ? i : 0;
            }
            return 0;
        }

        private static void Show(int number)
        {
            root.SetActive(number > 0);
            if (number == 0) return;
            RectInt rect = Zone(number);
            for (int layer = 0; layer < 2; layer++)
            {
                float t = layer == 0 ? 0.040f : 0.015f;
                float x = (rect.x + rect.width * 0.5f - 480) / 100f;
                float y = (270 - rect.y - rect.height * 0.5f) / 100f;
                float w = rect.width / 100f, h = rect.height / 100f;
                int start = layer * 4;
                Line(strokes[start], x, y + h / 2, w, t);
                Line(strokes[start + 1], x, y - h / 2, w, t);
                Line(strokes[start + 2], x - w / 2, y, t, h);
                Line(strokes[start + 3], x + w / 2, y, t, h);
            }
        }

        private static void Line(SpriteRenderer renderer, float x, float y, float width, float height)
        {
            renderer.transform.localPosition = new Vector3(x, y, 0);
            renderer.transform.localScale = new Vector3(width, height, 1);
        }

        internal static void Restore()
        {
            Array.Clear(Targets, 0, Targets.Length);
            foreach (KeyValuePair<Image, HighlightAppearance> saved in DropTargets)
            {
                Image image = saved.Key;
                if (image == null || image.overrideSprite != dropOutline) continue;
                image.overrideSprite = saved.Value.Override;
                image.type = saved.Value.Type;
                image.fillCenter = saved.Value.FillCenter;
                image.color = saved.Value.Color;
            }
            DropTargets.Clear();
            if (dropOutline != null) UnityEngine.Object.Destroy(dropOutline);
            if (root != null) UnityEngine.Object.Destroy(root);
            if (edge != null)
            {
                UnityEngine.Object.Destroy(edge.texture);
                UnityEngine.Object.Destroy(edge);
            }
            root = null;
            edge = null;
            dropOutline = null;
            strokes = null;
        }
    }
}
