using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JinSongPoc
{
    internal static class SceneActionArt
    {
        private sealed class View { internal Sprite Native, Applied; }
        private static readonly Dictionary<SpriteRenderer, View> Views = new Dictionary<SpriteRenderer, View>();
        private static readonly Dictionary<Sprite, Sprite> Sprites = new Dictionary<Sprite, Sprite>();
        private sealed class WalkerView { internal SpriteRenderer Figure; internal bool NativeEnabled, Static, FacingRight; internal int Variant; internal float LastX; }
        private static readonly Dictionary<SpriteRenderer, WalkerView> Walkers = new Dictionary<SpriteRenderer, WalkerView>();
        private static readonly string[] StaticKeys = { "jin_market_porter", "jin_market_merchant", "jin_market_woman", "jin_market_elder" };
        private static readonly Sprite[] StaticFigures = new Sprite[4];
        private static readonly Sprite[] StallSprites = new Sprite[2];
        private static readonly SpriteRenderer[] Stalls = new SpriteRenderer[2];
        private static int nextStaticVariant;
        private static SpriteRenderer[] renderers;
        private static float next;
        private static float menuNext;

        // Reuse approved paintings only at named native backdrop renderers. Never enables a node.
        internal static void RefreshMenu()
        {
            if (!Mod.Active || Mod.Assets == null || SceneManager.GetActiveScene().buildIndex != 0
                || Time.unscaledTime < menuNext) return;
            menuNext = Time.unscaledTime + 2f;
            foreach (SpriteRenderer renderer in UnityEngine.Object.FindObjectsOfType<SpriteRenderer>(true))
            {
                if (renderer == null || renderer.gameObject.scene.buildIndex != 0
                    || !renderer.gameObject.activeInHierarchy || renderer.name != "MenuBackground") continue;
                ApplyRemainder(renderer);
            }
        }

        private static bool Foreign(Sprite sprite)
        {
            if (ModLoader.Mods == null) return false;
            foreach (LoadedMod mod in ModLoader.Mods)
                if (mod.Textures != null && mod.Textures.ContainsValue(sprite.texture)) return true;
            return false;
        }

        private static bool ApplyRemainder(SpriteRenderer renderer)
        {
            Sprite stock = renderer.sprite;
            View state;
            Views.TryGetValue(renderer, out state);
            if (state != null && stock == state.Applied) return true;
            if (stock == null || stock.packed || !renderer.gameObject.activeInHierarchy || Foreign(stock)) return false;
            string textureKey = null;
            bool counter = renderer.name == "pc_alert" && stock.name.StartsWith("pc_count-Sheet_", StringComparison.Ordinal);
            if (renderer.name == "store_back" && stock.name == "store_background") textureKey = "jin_concept_shop";
            else if (renderer.name == "MenuBackground" && stock.name == "MenuMockupExportedNoText") textureKey = "jin_main_menu";
            if (!counter && textureKey == null) return false;
            Sprite art;
            if (!Sprites.TryGetValue(stock, out art))
            {
                int width = Mathf.RoundToInt(stock.rect.width), height = Mathf.RoundToInt(stock.rect.height);
                Color32[] pixels;
                if (counter)
                {
                    if (width != 32 || height != 32) return false;
                    pixels = InteriorArt.ReadSpritePixels(stock, width, height);
                    if (pixels == null) return false;
                    for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        Color32 native = pixels[index];
                        if (native.a == 0) continue;
                        int high = Math.Max(native.r, Math.Max(native.g, native.b));
                        int low = Math.Min(native.r, Math.Min(native.g, native.b));
                        // State lamps and all per-frame shapes remain recognizable.
                        if (high - low > 45) continue;
                        float value = (native.r * 3 + native.g * 6 + native.b) / 2550f;
                        Color32 paper = Color32.Lerp(new Color32(48, 31, 20, native.a), new Color32(231, 214, 173, native.a), value);
                        if (x < 4 || x >= width - 4 || y < 4 || y >= height - 4)
                            paper = Color32.Lerp(new Color32(62, 37, 20, native.a), new Color32(163, 114, 62, native.a), value);
                        paper.a = native.a;
                        pixels[index] = paper;
                    }
                }
                else
                {
                    Texture2D painting;
                    if (!Mod.Assets.Textures.TryGetValue(textureKey, out painting) || painting == null) return false;
                    Color32[] source = painting.GetPixels32();
                    pixels = new Color32[width * height];
                    for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                    {
                        int sx = Math.Min(painting.width - 1, x * painting.width / width);
                        int sy = Math.Min(painting.height - 1, y * painting.height / height);
                        pixels[y * width + x] = source[sy * painting.width + sx];
                    }
                    Texture2D street;
                    if (renderer.name == "store_back" && stock.name == "store_background"
                        && width == 960 && height == 540
                        && Mod.Assets.Textures.TryGetValue("jin_street_horizontal", out street) && street != null)
                    {
                        // Register the outdoor view to the existing window aperture, rather
                        // than shrinking a whole shop painting into the background canvas.
                        // The extra bottom rows fill the native ledge's tapered edge gaps.
                        Color32[] road = street.GetPixels32();
                        for (int row = 26; row < 227; row++) for (int x = 338; x < 623; x++)
                        {
                            int sx = (x - 338) * street.width / 285;
                            int sy = street.height - 1 - Math.Min(street.height - 1, (row - 26) * street.height / 181);
                            pixels[(height - 1 - row) * width + x] = road[sy * street.width + sx];
                        }
                    }
                }
                Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.filterMode = stock.texture.filterMode;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels32(pixels);
                texture.Apply(false, true);
                art = Sprite.Create(texture, new Rect(0, 0, width, height), stock.pivot / stock.rect.size,
                    stock.pixelsPerUnit, 0, SpriteMeshType.FullRect, stock.border);
                art.name = "jinsong_poc.scene.remainder." + stock.name;
                Sprites.Add(stock, art);
                RemainderTextures.Add(texture);
            }
            if (state == null) Views.Add(renderer, state = new View());
            state.Native = stock;
            state.Applied = art;
            renderer.sprite = art;
            return true;
        }

        private static readonly List<Texture2D> RemainderTextures = new List<Texture2D>();

        private static bool StreetWalker(SpriteRenderer renderer, Sprite stock)
        {
            if (renderer.name == "Walker1" && stock.name.StartsWith("walker1_", StringComparison.Ordinal)) return true;
            Transform parent = renderer.transform.parent;
            if (parent == null || parent.name != "walkerMidTransform" && parent.name != "walkerFarTransform") return false;
            string[] prefixes = { "midTrechCoat_", "midGasmask_", "midPuncho_", "midBackpack_", "midBandana_",
                "farTrechCoat_", "farBandana_", "farPuncho_", "farBackpack_", "silhouette-animations-tenth-size_" };
            foreach (string prefix in prefixes) if (stock.name.StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }

        private static bool ApplyStreetWalker(SpriteRenderer renderer, Sprite stock)
        {
            if (!StreetWalker(renderer, stock) || Foreign(stock)) return false;
            if (ApplyStaticWalker(renderer, stock)) return true;
            Texture2D sheet;
            if (!Mod.Assets.Textures.TryGetValue("jin_scene_walker1", out sheet) || sheet == null) return false;
            Sprite art;
            if (!Sprites.TryGetValue(stock, out art))
            {
                int frame;
                string suffix = stock.name.Substring(stock.name.LastIndexOf('_') + 1);
                if (!Int32.TryParse(suffix, out frame)) return false;
                int sw = sheet.width / 8, sh = sheet.height;
                int width = Mathf.RoundToInt(stock.rect.width), height = Mathf.RoundToInt(stock.rect.height);
                float scale = Math.Min((float)width / sw, (float)height / sh);
                int targetWidth = Math.Max(1, (int)(sw * scale)), targetHeight = Math.Max(1, (int)(sh * scale));
                int left = (width - targetWidth) / 2;
                Color32[] source = sheet.GetPixels32(), pixels = new Color32[width * height];
                for (int y = 0; y < targetHeight; y++) for (int x = 0; x < targetWidth; x++)
                    pixels[y * width + left + x] = source[Math.Min(sh - 1, y * sh / targetHeight) * sheet.width
                        + frame % 8 * sw + Math.Min(sw - 1, x * sw / targetWidth)];
                Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels32(pixels); texture.Apply(false, true);
                RemainderTextures.Add(texture);
                art = Sprite.Create(texture, new Rect(0, 0, width, height), stock.pivot / stock.rect.size,
                    stock.pixelsPerUnit, 0, SpriteMeshType.FullRect, stock.border);
                art.name = "jinsong_poc.street." + stock.name;
                Sprites.Add(stock, art);
            }
            View state;
            if (!Views.TryGetValue(renderer, out state)) Views.Add(renderer, state = new View());
            state.Native = stock; state.Applied = art; renderer.sprite = art;
            PlaceStreetFigure(renderer, art);
            return true;
        }

        private static bool PrepareStaticFigures()
        {
            if (StaticFigures[0] != null) return true;
            Texture2D[] textures = new Texture2D[StaticKeys.Length];
            for (int i = 0; i < textures.Length; i++)
                if (!Mod.Assets.Textures.TryGetValue(StaticKeys[i], out textures[i]) || textures[i] == null) return false;
            for (int i = 0; i < textures.Length; i++)
            {
                StaticFigures[i] = Sprite.Create(textures[i], new Rect(0, 0, textures[i].width, textures[i].height),
                    new Vector2(.5f, 0), 100f, 0, SpriteMeshType.FullRect);
                StaticFigures[i].name = "jinsong_poc.street.static." + StaticKeys[i];
            }
            return true;
        }

        private static bool ApplyStaticWalker(SpriteRenderer native, Sprite stock)
        {
            if (!PrepareStaticFigures()) return false;
            WalkerView view;
            if (!Walkers.TryGetValue(native, out view))
            {
                GameObject go = new GameObject("JinSong.StreetFigure");
                go.transform.SetParent(native.transform, false);
                view = new WalkerView { Figure = go.AddComponent<SpriteRenderer>(), NativeEnabled = native.enabled,
                    Static = true, Variant = nextStaticVariant++ % StaticFigures.Length,
                    FacingRight = !native.flipX, LastX = native.transform.position.x };
                Walkers.Add(native, view);
            }
            view.Static = true;
            Sprite art = StaticFigures[view.Variant];
            View state;
            if (!Views.TryGetValue(native, out state)) Views.Add(native, state = new View());
            state.Native = stock; state.Applied = art; native.sprite = art;
            PlaceStaticFigure(native, view, art);
            return true;
        }

        private static void PlaceStaticFigure(SpriteRenderer native, WalkerView view, Sprite art)
        {
            bool middle = native.transform.parent != null && native.transform.parent.name == "walkerMidTransform";
            bool far = native.transform.parent != null && native.transform.parent.name == "walkerFarTransform";
            float height = far ? .32f : middle ? .62f : .75f;
            float foot = far ? 1.42f : middle ? .70f : .62f;
            Vector3 scale = native.transform.lossyScale;
            float imageHeight = Math.Max(.001f, art.bounds.size.y);
            view.Figure.transform.localScale = new Vector3(height / imageHeight / Math.Max(.001f, Math.Abs(scale.x)),
                height / imageHeight / Math.Max(.001f, Math.Abs(scale.y)), 1f);
            view.Figure.transform.localPosition = new Vector3(0, (foot - native.transform.position.y) / Math.Max(.001f, scale.y), 0);
            view.Figure.sprite = art;
            float delta = native.transform.position.x - view.LastX;
            if (Math.Abs(delta) > .00001f) view.FacingRight = delta > 0;
            view.LastX = native.transform.position.x;
            bool sourceFacesRight = view.Variant == 0 || view.Variant == 2;
            view.Figure.flipX = (sourceFacesRight != view.FacingRight) ^ (scale.x < 0);
            view.Figure.flipY = native.flipY;
            view.Figure.color = native.color;
            view.Figure.sortingLayerID = native.sortingLayerID;
            // Keep every passer below stalls (2) and the native wall/window mask (3).
            view.Figure.sortingOrder = Math.Min(1, native.sortingOrder);
            native.enabled = false;
        }

        private static void RefreshStalls()
        {
            if (Stalls[0] != null && Stalls[1] != null) return;
            GameObject room = GameObject.Find("--Physical Store--");
            if (room == null) return;
            string[] keys = { "jin_market_left", "jin_market_right" };
            for (int i = 0; i < keys.Length; i++)
            {
                Texture2D texture;
                if (!Mod.Assets.Textures.TryGetValue(keys[i], out texture) || texture == null) return;
                if (StallSprites[i] == null)
                {
                    StallSprites[i] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        new Vector2(i == 0 ? 0 : 1, 0), 100f, 0, SpriteMeshType.FullRect);
                    StallSprites[i].name = "jinsong_poc.street.occluder." + (i == 0 ? "left" : "right");
                }
                if (Stalls[i] != null) continue;
                GameObject go = new GameObject("JinSong.MarketOccluder." + (i == 0 ? "Left" : "Right"));
                go.transform.SetParent(room.transform, false);
                go.transform.position = new Vector3(i == 0 ? -1.4485f : 1.4585f, .6119f, 0);
                go.transform.localScale = Vector3.one * (1.5928f / StallSprites[i].bounds.size.y);
                SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = StallSprites[i]; renderer.sortingOrder = 2;
                Stalls[i] = renderer;
            }
        }

        private static void PlaceStreetFigure(SpriteRenderer native, Sprite art)
        {
            WalkerView view;
            if (!Walkers.TryGetValue(native, out view))
            {
                GameObject go = new GameObject("JinSong.StreetFigure");
                go.transform.SetParent(native.transform, false);
                view = new WalkerView { Figure = go.AddComponent<SpriteRenderer>(), NativeEnabled = native.enabled };
                Walkers.Add(native, view);
            }
            bool middle = native.transform.parent != null && native.transform.parent.name == "walkerMidTransform";
            bool far = native.transform.parent != null && native.transform.parent.name == "walkerFarTransform";
            float offset = middle ? .5f : far ? .18f : 0f;
            float scale = middle ? .65f : far ? .8f : 1f;
            view.Figure.transform.localPosition = new Vector3(0, -offset / Math.Max(.01f, Math.Abs(native.transform.lossyScale.y)), 0);
            view.Figure.transform.localScale = Vector3.one * scale;
            view.Figure.sprite = art;
            view.Figure.flipX = native.flipX; view.Figure.flipY = native.flipY;
            view.Figure.color = native.color;
            view.Figure.sortingLayerID = native.sortingLayerID;
            view.Figure.sortingOrder = native.sortingOrder;
            native.enabled = false;
        }

        internal static void Refresh()
        {
            if (!Mod.Active || Mod.Assets == null || SceneManager.GetActiveScene().buildIndex != 1) return;
            RefreshStalls();
            if (renderers == null || Time.unscaledTime >= next)
            {
                renderers = UnityEngine.Object.FindObjectsOfType<SpriteRenderer>(true);
                next = Time.unscaledTime + 2f;
                var stale = new List<SpriteRenderer>();
                foreach (SpriteRenderer renderer in Views.Keys) if (renderer == null) stale.Add(renderer);
                foreach (SpriteRenderer renderer in stale) Views.Remove(renderer);
                stale.Clear();
                foreach (SpriteRenderer renderer in Walkers.Keys) if (renderer == null) stale.Add(renderer);
                foreach (SpriteRenderer renderer in stale) Walkers.Remove(renderer);
            }
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null || renderer.gameObject.scene.buildIndex != 1) continue;
                Sprite stock = renderer.sprite;
                if (stock == null || stock.packed) continue;
                if (ApplyRemainder(renderer)) continue;
                View state;
                if (Views.TryGetValue(renderer, out state) && stock == state.Applied)
                {
                    WalkerView walker;
                    if (Walkers.TryGetValue(renderer, out walker))
                    {
                        if (walker.Static) PlaceStaticFigure(renderer, walker, stock);
                        else PlaceStreetFigure(renderer, stock);
                    }
                    continue;
                }
                if (ApplyStreetWalker(renderer, stock)) continue;
                string key;
                bool sheet = false;
                if (stock.name.StartsWith("walker1_", StringComparison.Ordinal)) { key = "jin_scene_walker1"; sheet = true; }
                else if (stock.name.StartsWith("shot_animation_body_", StringComparison.Ordinal)) { key = "jin_scene_shot_animation_body"; sheet = true; }
                else if (stock.name == "one_hand" || stock.name == "shotgun_shadow" || stock.name == "shotgun_shadow_only" || stock.name == "body")
                    key = "jin_scene_" + stock.name;
                else continue;
                Texture2D texture;
                if (!Mod.Assets.Textures.TryGetValue(key, out texture) || texture == null) continue;
                bool foreign = false;
                if (ModLoader.Mods != null)
                    foreach (LoadedMod mod in ModLoader.Mods)
                        if (mod.Textures != null && mod.Textures.ContainsValue(stock.texture)) { foreign = true; break; }
                if (foreign) continue;
                Sprite art;
                if (!Sprites.TryGetValue(stock, out art))
                {
                    Rect rect = sheet ? stock.rect : new Rect(0, 0, texture.width, texture.height);
                    if (rect.width != stock.rect.width || rect.height != stock.rect.height
                        || rect.xMax > texture.width || rect.yMax > texture.height) continue;
                    art = Sprite.Create(texture, rect, stock.pivot / stock.rect.size,
                        stock.pixelsPerUnit, 0, SpriteMeshType.FullRect, stock.border);
                    art.name = "jinsong_poc.scene." + stock.name;
                    Sprites.Add(stock, art);
                }
                if (state == null) Views.Add(renderer, state = new View());
                state.Native = stock;
                state.Applied = art;
                renderer.sprite = art;
            }
        }

        internal static void Restore()
        {
            for (int i = 0; i < Stalls.Length; i++)
            {
                if (Stalls[i] != null) UnityEngine.Object.Destroy(Stalls[i].gameObject);
                if (StallSprites[i] != null) UnityEngine.Object.Destroy(StallSprites[i]);
                Stalls[i] = null; StallSprites[i] = null;
            }
            foreach (var pair in Walkers)
            {
                if (pair.Key != null && !pair.Key.enabled) pair.Key.enabled = pair.Value.NativeEnabled;
                if (pair.Value.Figure != null) UnityEngine.Object.Destroy(pair.Value.Figure.gameObject);
            }
            Walkers.Clear();
            foreach (var pair in Views)
                if (pair.Key != null && pair.Key.sprite == pair.Value.Applied) pair.Key.sprite = pair.Value.Native;
            Views.Clear();
            foreach (Sprite art in StaticFigures) if (art != null) UnityEngine.Object.Destroy(art);
            Array.Clear(StaticFigures, 0, StaticFigures.Length);
            nextStaticVariant = 0;
            foreach (Sprite art in Sprites.Values) if (art != null) UnityEngine.Object.Destroy(art);
            Sprites.Clear();
            foreach (Texture2D texture in RemainderTextures) if (texture != null) UnityEngine.Object.Destroy(texture);
            RemainderTextures.Clear();
            renderers = null;
            next = 0;
            menuNext = 0;
        }
    }
}
