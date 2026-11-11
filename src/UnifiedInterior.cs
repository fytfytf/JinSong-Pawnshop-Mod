using System;
using UnityEngine;

namespace JinSongPoc
{
    internal static class UnifiedInterior
    {
        private const int Width = 960;
        private const int Height = 540;
        private const string ArtKey = "jin_unified_shop";

        // Pixel landmarks in the shop scene and the approved 1672×941 painting.
        // Rows are measured down from the top in both sets of coordinates.
        private static readonly int[] SceneColumns = { 0, 60, 200, 338, 623, 960 };
        private static readonly int[] ArtColumns = { 0, 105, 315, 556, 1132, 1672 };
        private static readonly int[] SceneRows = { 0, 26, 157, 190, 256, 267, 347, 490, 540 };
        private static readonly int[] ArtRows = { 0, 77, 320, 385, 471, 475, 608, 815, 941 };
        private static readonly int[] ConceptColumns = { 0, 153, 272, 570, 1111, 1449, 1672 };
        private static readonly int[] ConceptSceneColumns = { 0, 75, 200, 338, 623, 760, 960 };
        private static readonly int[] ConceptRows = { 0, 66, 343, 400, 431, 476, 501, 627, 941 };
        private static readonly int[] ConceptSceneRows = { 0, 26, 157, 190, 207, 256, 267, 347, 540 };

        private static readonly string[] PaintedObjects = {
            "store_mid", "EmporiumWall", "Desk", "front_counter", "shutterFrame", "shutterFrameTop"
        };
        private static readonly string[] StockSprites = {
            "store_mid", "wall", "desk", "front_counter", "window", "window_top"
        };
        private static readonly int[] SortingOrders = { -4, 3, 5, 7, 8, 12 };
        private static readonly string[] CoveredObjects = {
            "store_front_wall", "store_front_floor", "store_mid_stairs", "store_mid_rail",
            "store_back_stairs", "store_front_lamp"
        };

        private sealed class Binding
        {
            internal SpriteRenderer Renderer;
            internal Sprite Original, Replacement;
        }

        private static Binding[] bindings;
        private static SpriteRenderer[] hidden;
        internal static bool IsConcept { get; private set; }
        internal static bool IsHotspot { get; private set; }
        internal static bool HasLeftShowcase { get; private set; }
        internal static bool HasCounterLayout { get; private set; }
        internal static bool HasOpenTradeLayout { get; private set; }
        internal static bool HasQueuePaper { get; private set; }

        internal static bool Replaces(string objectName)
        {
            return Array.IndexOf(PaintedObjects, objectName) >= 0 || Array.IndexOf(CoveredObjects, objectName) >= 0;
        }

        internal static bool Apply(LoadedMod assets)
        {
            if (bindings != null && bindings[0].Renderer != null)
            {
                Binding wall = bindings[1];
                Sprite current = wall.Renderer.sprite;
                if (current != wall.Replacement && Matches(current, 1))
                {
                    wall.Original = current;
                    wall.Renderer.sprite = wall.Replacement;
                }
                return true;
            }

            Restore();
            Texture2D art;
            bool hotspot = assets.Textures.TryGetValue(ShopHotspots.ArtKey, out art) && art != null;
            bool concept = hotspot || assets.Textures.TryGetValue("jin_concept_shop", out art) && art != null
                && assets.Textures.ContainsKey("jin_concept_fixtures") && assets.Textures.ContainsKey("jin_concept_banner");
            if (!concept && (!assets.Textures.TryGetValue(ArtKey, out art) || art == null)) return false;
            Texture2D niche = null;
            bool leftShowcase = hotspot && assets.Textures.TryGetValue("jin_left_showcase", out niche) && niche != null;
            Texture2D counter = null, fixtures = null, plaque = null;
            bool counterLayout = hotspot && assets.Textures.TryGetValue("jin_clear_counter", out counter) && counter != null
                && assets.Textures.TryGetValue("jin_concept_fixtures", out fixtures) && fixtures != null
                && assets.Textures.TryGetValue("jin_ledger_plaque", out plaque) && plaque != null;
            Texture2D openTrade = null, queuePaper = null;
            bool openTradeLayout = counterLayout && assets.Textures.TryGetValue("jin_open_trade", out openTrade) && openTrade != null;
            bool hasQueuePaper = hotspot && assets.Textures.TryGetValue("jin_queue_paper", out queuePaper) && queuePaper != null;
            Binding[] pending = new Binding[PaintedObjects.Length];
            for (int i = 0; i < pending.Length; i++)
            {
                SpriteRenderer renderer = FindRenderer(PaintedObjects[i]);
                if (renderer == null || renderer.sortingOrder != SortingOrders[i] || renderer.drawMode != SpriteDrawMode.Simple
                    || !Matches(renderer.sprite, i)) return false;
                pending[i] = new Binding { Renderer = renderer, Original = renderer.sprite };
            }
            try
            {
                Color32[] painting = Register(art, concept, hotspot);
                if (leftShowcase) BlendPatch(painting, Register(niche, true, true), new RectInt(265, 105, 208, 231));
                if (counterLayout)
                {
                    BlendPatch(painting, Register(counter, true, true), new RectInt(702, 494, 258, 108));
                    if (openTradeLayout)
                    {
                        Color32[] cleared = Register(openTrade, true, true);
                        BlendPatch(painting, cleared, new RectInt(473, 415, 98, 118));
                        BlendPatch(painting, cleared, new RectInt(402, 521, 285, 80));
                        BlendPatch(painting, cleared, new RectInt(986, 464, 168, 120));
                        BlendPatch(painting, cleared, new RectInt(1063, 395, 104, 141));
                        Stamp(painting, fixtures, new Rect(0.51f, 0.607f, 0.472f, 0.257f), ShopHotspots.SideAbacus);
                        Stamp(painting, fixtures, new Rect(0.073f, 0.06f, 0.335f, 0.38f), ShopHotspots.SideScale);
                    }
                    Texture2D cashbook;
                    if (assets.Textures.TryGetValue("jin_cash_ledger", out cashbook) && cashbook != null)
                        Stamp(painting, cashbook, new Rect(0, 0, 1, 1), ShopHotspots.LedgerArt);
                    else Stamp(painting, fixtures, new Rect(0.023f, 0.575f, 0.46f, 0.33f), ShopHotspots.LedgerArt);
                    Stamp(painting, plaque, new Rect(0, 0, 1, 1), ShopHotspots.CalendarArt);
                }
                if (hasQueuePaper)
                    Stamp(painting, queuePaper, new Rect(.279f, .050f, .429f, .898f), ShopHotspots.QueuePaper);
                bool horizontalStreet = assets.Textures.ContainsKey("jin_street_horizontal");
                for (int i = 0; i < pending.Length; i++)
                    pending[i].Replacement = MakeSprite(pending[i].Original, PaintedObjects[i], LayerPixels(painting, pending[i].Original, i, concept, horizontalStreet));
            }
            catch (Exception e)
            {
                foreach (Binding binding in pending) DestroySprite(binding.Replacement);
                Mod.Log.Warning("Unified shop kept previous art: " + e.Message);
                return false;
            }

            bindings = pending;
            IsConcept = concept;
            IsHotspot = hotspot;
            HasLeftShowcase = leftShowcase;
            HasCounterLayout = counterLayout;
            HasOpenTradeLayout = openTradeLayout;
            HasQueuePaper = hasQueuePaper;
            hidden = new SpriteRenderer[CoveredObjects.Length];
            for (int i = 0; i < CoveredObjects.Length; i++)
            {
                SpriteRenderer renderer = FindRenderer(CoveredObjects[i]);
                if (renderer == null || !renderer.enabled) continue;
                hidden[i] = renderer;
                renderer.enabled = false;
            }
            foreach (Binding binding in bindings) binding.Renderer.sprite = binding.Replacement;
            Mod.Log.Log("Unified shop bound: registered room painting, street aperture, trade ledge and shutter masks.");
            return true;
        }

        private static SpriteRenderer FindRenderer(string name)
        {
            GameObject obj = GameObject.Find(name);
            return obj == null || obj.scene.buildIndex != 1 ? null : obj.GetComponent<SpriteRenderer>();
        }

        private static bool Matches(Sprite sprite, int index)
        {
            return sprite != null && !sprite.packed && sprite.rect.width == Width && sprite.rect.height == Height
                && sprite.pixelsPerUnit == 100f && sprite.pivot == new Vector2(Width / 2f, Height / 2f)
                && (sprite.name == StockSprites[index] || (index == 1 && sprite.name == "wall2"));
        }

        private static Color32[] Register(Texture2D art, bool concept, bool hotspot)
        {
            Color32[] source = art.GetPixels32();
            Color32[] result = new Color32[Width * Height];
            int[] columns = hotspot ? MapAxis(ShopHotspots.SceneColumns, ShopHotspots.Columns, art.width)
                : concept ? MapAxis(ConceptSceneColumns, ConceptColumns, art.width)
                : MapAxis(SceneColumns, ArtColumns, art.width);
            int[] rows = hotspot ? MapAxis(ShopHotspots.SceneRows, ShopHotspots.Rows, art.height)
                : concept ? MapAxis(ConceptSceneRows, ConceptRows, art.height)
                : MapAxis(SceneRows, ArtRows, art.height);
            for (int y = 0; y < Height; y++)
            {
                int sourceRow = (art.height - 1 - rows[Height - 1 - y]) * art.width;
                for (int x = 0; x < Width; x++)
                {
                    Color32 pixel = source[sourceRow + columns[x]];
                    pixel.a = 255;
                    result[y * Width + x] = pixel;
                }
            }
            return result;
        }

        private static int[] MapAxis(int[] scene, int[] art, int size)
        {
            int[] mapped = new int[scene[scene.Length - 1]];
            float scale = (float)size / art[art.Length - 1];
            for (int i = 1; i < scene.Length; i++)
                for (int p = scene[i - 1]; p < scene[i]; p++)
                {
                    float t = (p + 0.5f - scene[i - 1]) / (scene[i] - scene[i - 1]);
                    mapped[p] = Mathf.Min(size - 1, (int)(Mathf.Lerp(art[i - 1], art[i], t) * scale));
                }
            return mapped;
        }

        private static void BlendPatch(Color32[] painting, Color32[] overlay, RectInt region)
        {
            // Both paintings are registered to the same shop coordinates. Feather only the patch edges.
            RectInt patch = ShopHotspots.ToScene(region);
            for (int row = patch.yMin; row < patch.yMax; row++)
                for (int x = patch.xMin; x < patch.xMax; x++)
                {
                    int fromSide = Math.Min(x - patch.xMin, patch.xMax - 1 - x);
                    int fromTop = Math.Min(row - patch.yMin, patch.yMax - 1 - row);
                    int pixel = (Height - 1 - row) * Width + x;
                    painting[pixel] = Color32.Lerp(painting[pixel], overlay[pixel], Math.Min(fromSide, fromTop) / 3f);
                }
        }

        private static void Stamp(Color32[] painting, Texture2D art, Rect source, RectInt target)
        {
            Color32[] pixels = art.GetPixels32();
            for (int y = 0; y < target.height; y++)
                for (int x = 0; x < target.width; x++)
                {
                    float u = (x + 0.5f) / target.width, v = 1f - (y + 0.5f) / target.height;
                    int sx = (int)((source.x + u * source.width) * art.width);
                    int sy = (int)((source.y + v * source.height) * art.height);
                    Color32 ink = pixels[sy * art.width + sx];
                    int pixel = (Height - 1 - target.y - y) * Width + target.x + x;
                    Color32 result = Color32.Lerp(painting[pixel], ink, ink.a / 255f);
                    result.a = 255;
                    painting[pixel] = result;
                }
        }

        private static Color32[] LayerPixels(Color32[] painting, Sprite original, int layer, bool concept, bool horizontalStreet)
        {
            if (layer == 0)
            {
                if (!horizontalStreet) return painting;
                // The original unified painting also baked the old receding street into
                // this base layer. Let the independent new outdoor layer show through.
                Color32[] room = (Color32[])painting.Clone();
                for (int row = 26; row < 227; row++) for (int x = 338; x < 623; x++)
                    if (InStreetOpening(x, row, concept)) room[(Height - 1 - row) * Width + x] = new Color32();
                return room;
            }
            if (layer == 2 && concept) return new Color32[painting.Length];
            Color32[] stock = layer == 1 ? null : InteriorArt.ReadSpritePixels(original, Width, Height);
            if (layer != 1 && stock == null) throw new InvalidOperationException("Cannot read " + original.name);
            Color32[] result = new Color32[painting.Length];
            for (int y = 0; y < Height; y++)
            {
                int row = Height - 1 - y;
                for (int x = 0; x < Width; x++)
                {
                    int pixel = y * Width + x;
                    if (layer == 2)
                    {
                        // The stock desk bakes in the button mount and right-hand readouts.
                        if (KeepDeskHardware(x, row)) result[pixel] = TintHousing(stock[pixel]);
                    }
                    else if (!InStreetOpening(x, row, concept) && (stock == null || stock[pixel].a != 0))
                    {
                        // The foreground and shutter travel masks reveal the same registered colors.
                        result[pixel] = painting[pixel];
                    }
                }
            }
            return result;
        }

        private static bool InStreetOpening(int x, int row, bool concept)
        {
            if (x < 338 || x >= 623 || row < 26) return false;
            int ledge = concept ? 207 : 190;
            if (row < ledge) return true;
            // The outer ledge has splayed sides, leaving a little more street visible at either edge.
            int taper = Mathf.Max(0, 13 - (row - ledge));
            return x < 338 + taper || x >= 623 - taper;
        }

        private static bool KeepDeskHardware(int x, int row)
        {
            int inset = Math.Max(0, (335 - row) / 2);
            return (row >= 325 && row < 363 && x >= 380 + inset && x < 525 - inset)
                || (x >= 801 && row >= 335 && row < 394);
        }

        private static Color32 TintHousing(Color32 pixel)
        {
            int min = Math.Min(pixel.r, Math.Min(pixel.g, pixel.b));
            int max = Math.Max(pixel.r, Math.Max(pixel.g, pixel.b));
            // Ink, colored indicators and display marks are part of the baked control artwork.
            if (pixel.a == 0 || min < 62 || max - min > 26) return pixel;
            int light = (pixel.r + pixel.g + pixel.b) / 3;
            return new Color32((byte)Math.Min(255, light + 15), (byte)(light * 4 / 5),
                (byte)(light * 5 / 9), pixel.a);
        }

        private static Sprite MakeSprite(Sprite original, string objectName, Color32[] pixels)
        {
            Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            try
            {
                texture.name = "jinsong_poc.unified." + objectName;
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, Width, Height), new Vector2(0.5f, 0.5f),
                    original.pixelsPerUnit, 0, SpriteMeshType.FullRect, original.border);
                if (sprite == null) throw new InvalidOperationException("Cannot build " + objectName);
                sprite.name = texture.name;
                return sprite;
            }
            catch
            {
                UnityEngine.Object.Destroy(texture);
                throw;
            }
        }

        internal static void Restore()
        {
            if (bindings == null) return;
            foreach (Binding binding in bindings)
            {
                if (binding.Renderer != null && binding.Renderer.sprite == binding.Replacement)
                    binding.Renderer.sprite = binding.Original;
                DestroySprite(binding.Replacement);
            }
            foreach (SpriteRenderer renderer in hidden)
                if (renderer != null) renderer.enabled = true;
            bindings = null;
            hidden = null;
            IsConcept = false;
            IsHotspot = false;
            HasLeftShowcase = false;
            HasCounterLayout = false;
            HasOpenTradeLayout = false;
            HasQueuePaper = false;
        }

        private static void DestroySprite(Sprite sprite)
        {
            if (sprite == null) return;
            UnityEngine.Object.Destroy(sprite.texture);
            UnityEngine.Object.Destroy(sprite);
        }
    }
}
