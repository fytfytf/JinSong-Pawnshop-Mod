using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JinSongPoc
{
    internal static class InteriorArt
    {
        private const int Width = 960;
        private const int Height = 540;
        private sealed class Layer
        {
            internal readonly string ObjectName, SpriteName, ArtKey;
            internal readonly string[] AlternateSprites;
            internal readonly float ShadeStrength, Mix;
            internal readonly bool FitVisible, Sequence, KeepStructure;
            internal readonly bool PreserveDisplay, UsePaintingAlpha;
            internal readonly RectInt PaintingArea, DisplayArea;
            internal readonly Rect SourceCrop, SourceDisplay;
            internal SpriteRenderer Renderer;
            internal Sprite Original, Applied;
            internal readonly Dictionary<Sprite, Sprite> Composites = new Dictionary<Sprite, Sprite>();

            internal Layer(string objectName, string spriteName, string artKey, float shadeStrength,
                bool fitVisible = false, float mix = 1f, bool sequence = false, bool keepStructure = false,
                string[] alternateSprites = null)
            {
                ObjectName = objectName;
                SpriteName = spriteName;
                ArtKey = artKey;
                ShadeStrength = shadeStrength;
                FitVisible = fitVisible;
                Mix = mix;
                Sequence = sequence;
                KeepStructure = keepStructure;
                AlternateSprites = alternateSprites;
            }

            internal Layer(string objectName, string spriteName, string artKey, RectInt paintingArea,
                RectInt displayArea = default(RectInt), Rect sourceCrop = default(Rect),
                Rect sourceDisplay = default(Rect), bool preserveDisplay = true, bool usePaintingAlpha = false)
                : this(objectName, spriteName, artKey, 0f)
            {
                PaintingArea = paintingArea;
                DisplayArea = displayArea;
                SourceCrop = sourceCrop;
                SourceDisplay = sourceDisplay;
                PreserveDisplay = preserveDisplay;
                UsePaintingAlpha = usePaintingAlpha;
            }

            internal bool Matches(string name)
            {
                if (!Sequence) return name == SpriteName
                    || (AlternateSprites != null && Array.IndexOf(AlternateSprites, name) >= 0);
                int frame;
                return name.StartsWith(SpriteName, StringComparison.Ordinal)
                    && int.TryParse(name.Substring(SpriteName.Length), out frame) && frame >= 0;
            }
        }

        private static Layer Showcase(string spriteName, RectInt area)
        {
            // All three showcase sizes keep the same frame margins. Match the art opening
            // to the stock pane so the top row of items stays inside the frame.
            RectInt pane = new RectInt(area.x + 13, area.y + 18, area.width - 28, area.height - 31);
            return new Layer("showcase", spriteName, "jin_showcase_frame", area, pane,
                new Rect(0f, 0f, 1f, 1f), new Rect(0.095f, 0.165f, 0.81f, 0.666f), preserveDisplay: false);
        }

        private static readonly Layer[] Layers = {
            new Layer("EmporiumWall", "wall", "jin_shop_wall", 0.30f, alternateSprites: new[] { "wall2" }),
            new Layer("store_front_wall", "store_front_wall_lit", "jin_window_timber", 0f, keepStructure: true),
            new Layer("shutterFrame", "window", "jin_window_timber", 0f, keepStructure: true),
            new Layer("shutterFrameTop", "window_top", "jin_window_timber", 0f, keepStructure: true),
            new Layer("Desk", "desk", "jin_counter_wood", 0f, keepStructure: true),
            new Layer("front_counter", "front_counter", "jin_counter_wood", 0f, keepStructure: true),
            new Layer("NutruifruitPoster", "poster_no_shadow", "jin_pawn_notice", 0f, fitVisible: true,
                alternateSprites: new[] { "poster", "poster_down" }),

            // Fit the ring height separately so the narrow screen-edge door does not stretch it.
            new Layer("Door", "door", "jin_entry_door", new RectInt(0, 1, 60, 539),
                new RectInt(0, 290, 60, 25), new Rect(0f, 0f, 1f, 1f), new Rect(0f, 0.36f, 0f, 0.134f),
                preserveDisplay: false),
            new Layer("Door Lock", "door lock", "jin_brass_control", 0.9f, mix: 0.78f),
            new Layer("ShutterDoor", "shutter_door", "jin_bamboo_shutter", 0.10f, fitVisible: true),
            new Layer("ShutterGlassDoor", "glass_door", "jin_lacquer_display", 0.85f, mix: 0.50f),

            // Whole fixture paintings use the original scene's bottom-up pixel coordinates.
            new Layer("Calendar", "calendar", "jin_ledger_plaque", new RectInt(177, 437, 125, 84),
                new RectInt(186, 446, 104, 65)),
            new Layer("CalendarTop", "calendar_top-Sheet_", "jin_brass_control", 0.65f, mix: 0.64f, sequence: true),
            new Layer("CreditDisplay", "credit_display", "jin_lacquer_display", 0.90f, fitVisible: true, mix: 0.69f),
            new Layer("ClientDisplay", "client_display", "jin_lacquer_display", 0.92f, fitVisible: true, mix: 0.69f),
            new Layer("ClientDisplayTop", "client_counter-Sheet_", "jin_brass_control", 0.72f, mix: 0.64f, sequence: true),
            new Layer("ClienteleDisplay", "clientele-Sheet_", "jin_brass_control", 0.85f, mix: 0.54f, sequence: true),
            new Layer("PC", "marketplace_pc", "jin_lacquer_display", 0.90f, fitVisible: true, mix: 0.72f),
            new Layer("pc_notif", "marketplace_pc_notif", "jin_brass_control", 0.76f, mix: 0.64f),
            new Layer("NegociationTablet", "negociation_tablet", "jin_negotiation_stand", new RectInt(649, 252, 130, 111),
                new RectInt(675, 302, 80, 55), new Rect(0.082f, 0.07f, 0.836f, 0.88f),
                new Rect(0.128f, 0.45f, 0.739f, 0.463f)),
            new Layer("_MagnifyingWindow", "magnifying_screen", "jin_lacquer_display", 0.86f, mix: 0.64f),
            new Layer("_MagnifyingWindowCollapsed", "magnifying_screen", "jin_lacquer_display", 0.86f, mix: 0.64f),
            Showcase("display_case", new RectInt(115, 310, 143, 108)),
            Showcase("display_case_medium", new RectInt(117, 294, 158, 127)),
            Showcase("display_case_large", new RectInt(115, 280, 174, 140)),

            new Layer("wall_phone", "phone", "jin_letter_box", new RectInt(241, 233, 85, 39), usePaintingAlpha: true),
            new Layer("Radio", "radio", "jin_signal_box", new RectInt(883, 213, 77, 61)),
            new Layer("Recorder", "tape anim-Sheet_", "jin_lacquer_display", 0.95f, mix: 0.67f, sequence: true),
            new Layer("feedback_box", "reputation_box", "jin_lacquer_display", 0.85f, fitVisible: true, mix: 0.76f),
            new Layer("VendingMachine", "vending", "jin_ceramic_dispenser", 0.45f, fitVisible: true, mix: 0.78f, sequence: true),
            new Layer("VendingFountain", "fountain", "jin_ceramic_dispenser", 0.45f, fitVisible: true, mix: 0.78f, sequence: true),
            new Layer("SleepingBag", "sleeping_bag", "jin_quilt", 0.28f, fitVisible: true),

            new Layer("Bell", "bell_button_sheet_", "jin_brass_control", 0.95f, mix: 0.63f, sequence: true),
            new Layer("SecButton", "button_sec", "jin_brass_control", 0.95f, mix: 0.58f),
            new Layer("dismissButton", "button_dismiss", "jin_brass_control", 0.95f, mix: 0.58f),
            new Layer("transferButton", "button_transfer", "jin_brass_control", 0.95f, mix: 0.58f),
            new Layer("ShutterButton", "shutter_button_sheet_", "jin_brass_control", 0.95f, mix: 0.63f, sequence: true),

            new Layer("Table Wires", "wires_table", "jin_cord_weave", 0.78f),
            new Layer("wires", "wires", "jin_cord_weave", 0.78f, alternateSprites: new[] { "wires2" }),
            new Layer("Calendar_Wire", "calendar wire", "jin_cord_weave", 0.78f),
            new Layer("connecting wire", "connectimg wire", "jin_cord_weave", 0.78f),
            new Layer("store_front_floor", "store_front_floor", "jin_stone_flags", 0.90f),
            new Layer("store_mid_stairs", "store_mid_stairs", "jin_stone_flags", 0.94f),
            new Layer("store_mid_rail", "store_mid_rail", "jin_window_timber", 0.82f),
            new Layer("store_back_stairs", "store_background_rail", "jin_stone_flags", 0.90f),
            new Layer("store_front_lamp", "store_front_lamp", "jin_brass_control", 0.55f, mix: 0.65f)
        };

        private static readonly Layer ClosedShutter = new Layer("Blocker", "shutter_door_cropped_2x",
            "jin_bamboo_shutter", 0.10f, fitVisible: true);
        private static Image closedShutterImage;
        // Lower-right cupboard face of the original 960x540 desk, in Unity bottom-up coordinates.
        private static readonly RectInt DeskCupboard = new RectInt(670, 46, 113, 146);
        private static readonly RectInt PaintedDesk = new RectInt(200, 192, 760, 81);
        private const int FloorHeight = 92;
        private const int DeskRimTop = 205;
        private const float PaintedRimFraction = 0.20f;
        private static readonly Dictionary<Texture2D, Color32[]> ArtPixels = new Dictionary<Texture2D, Color32[]>();

        internal static bool Apply(LoadedMod assets)
        {
            if (assets == null) return false;
            bool unified = UnifiedInterior.Apply(assets);
            if (UnifiedInterior.IsConcept) ConceptFixtures.Apply(assets);
            foreach (Layer layer in Layers)
            {
                if (unified && UnifiedInterior.Replaces(layer.ObjectName)) continue;
                if (ConceptFixtures.Replaces(layer.ObjectName)) continue;
                if (layer.Renderer == null)
                {
                    GameObject go = GameObject.Find(layer.ObjectName);
                    if (go == null || go.scene.buildIndex != 1) continue;
                    layer.Renderer = go.GetComponent<SpriteRenderer>();
                }
                ApplyLayer(layer, assets);
            }
            ApplyClosedShutter(assets);
            return unified;
        }

        internal static void ApplyAnimated(LoadedMod assets)
        {
            if (assets == null) return;
            foreach (Layer layer in Layers)
                if (layer.Sequence && layer.Renderer != null) ApplyLayer(layer, assets);
        }

        private static void ApplyLayer(Layer layer, LoadedMod assets)
        {
            SpriteRenderer renderer = layer.Renderer;
            if (renderer == null || renderer.drawMode != SpriteDrawMode.Simple) return;
            Sprite original = renderer.sprite;
            Sprite replacement = GetReplacement(layer, original, assets);
            if (replacement == null) return;
            layer.Original = original;
            layer.Applied = replacement;
            renderer.sprite = replacement;
        }

        private static void ApplyClosedShutter(LoadedMod assets)
        {
            // The stock canvas blocker also draws the closed shutter. Its mask and layout belong to the game.
            if (closedShutterImage == null)
            {
                StoreShutterButton shutter = StoreShutterButton.Instance;
                if (shutter == null || shutter.blockerGameObject == null || shutter.blockerGameObject.scene.buildIndex != 1) return;
                closedShutterImage = shutter.blockerGameObject.GetComponent<Image>();
            }
            if (closedShutterImage == null || closedShutterImage.overrideSprite != closedShutterImage.sprite) return;
            Sprite original = closedShutterImage.sprite;
            Sprite replacement = GetReplacement(ClosedShutter, original, assets);
            if (replacement == null) return;
            ClosedShutter.Original = original;
            ClosedShutter.Applied = replacement;
            closedShutterImage.sprite = replacement;
        }

        private static Sprite GetReplacement(Layer layer, Sprite original, LoadedMod assets)
        {
            if (original == null || !layer.Matches(original.name)) return null;
            Texture2D art;
            if (!assets.Textures.TryGetValue(layer.ArtKey, out art) || art == null) return null;
            Sprite replacement;
            if (!layer.Composites.TryGetValue(original, out replacement))
            {
                replacement = TryBuild(layer, original, art, assets);
                layer.Composites.Add(original, replacement);
            }
            return replacement;
        }

        private static Sprite TryBuild(Layer layer, Sprite original, Texture2D art, LoadedMod assets)
        {
            Texture2D output = null;
            Sprite sprite = null;
            try
            {
                Rect originalRect = original.rect;
                int width = Mathf.CeilToInt(originalRect.width);
                int height = Mathf.CeilToInt(originalRect.height);
                if (original.packed || width < 1 || height < 1 || width > Width || height > Height) return null;
                Color32[] pixels = ReadSpritePixels(original, width, height);
                if (pixels == null) return null;
                RectInt visible = VisibleBounds(pixels, width, height);
                if (visible.width == 0 || visible.height == 0) return null;
                // Scene-sized layers must keep their original openings even if a readback fails.
                if (width == Width && height == Height && Array.TrueForAll(pixels, p => p.a != 0)) return null;

                bool paintedFixture = layer.PaintingArea.width > 0;
                if (paintedFixture && (width != Width || height != Height)) return null;
                Color32[] material = GetArtPixels(art);
                RectInt area = paintedFixture ? layer.PaintingArea
                    : layer.FitVisible ? visible : new RectInt(0, 0, width, height);
                int bottom = paintedFixture ? area.yMin : visible.yMin;
                int top = paintedFixture ? area.yMax : visible.yMax;
                int left = paintedFixture ? area.xMin : visible.xMin;
                int right = paintedFixture ? area.xMax : visible.xMax;
                bool ceramic = layer.ArtKey == "jin_ceramic_dispenser";
                bool desk = layer.ObjectName == "Desk" && width == Width && height == Height;
                bool shopWall = layer.ObjectName == "EmporiumWall" && width == Width && height == Height;
                Texture2D floorArt = null;
                if (shopWall) assets.Textures.TryGetValue("jin_indoor_floor", out floorArt);
                Color32[] floorPaint = floorArt != null ? GetArtPixels(floorArt) : null;
                Texture2D cupboardArt;
                assets.Textures.TryGetValue("jin_counter_joinery", out cupboardArt);
                Color32[] cupboard = desk && cupboardArt != null ? GetArtPixels(cupboardArt) : null;
                Texture2D deskPainting;
                assets.Textures.TryGetValue("jin_desk_painting", out deskPainting);
                Color32[] deskPaint = desk && deskPainting != null ? GetArtPixels(deskPainting) : null;
                bool keepStructure = layer.KeepStructure || layer.Mix < 1f;
                for (int y = bottom; y < top; y++)
                {
                    int floorRow = floorPaint != null && y < FloorHeight
                        ? ArtCoordinate(y, 0, FloorHeight, floorArt.height, 0f) : -1;
                    int sy = paintedFixture && layer.SourceDisplay.height > 0
                        ? FitDisplayAxis(y, area.yMin, layer.DisplayArea.yMin, layer.DisplayArea.yMax, area.yMax,
                            layer.SourceCrop.yMin, layer.SourceDisplay.yMin, layer.SourceDisplay.yMax, layer.SourceCrop.yMax, art.height)
                        : ArtCoordinate(y, area.y, area.height, art.height, ceramic ? 0.04f : 0f);
                    int paintedRow = deskPaint != null && y >= PaintedDesk.yMin && y < PaintedDesk.yMax
                        ? DeskPaintingRow(y, deskPainting.height) : -1;
                    for (int x = left; x < right; x++)
                    {
                        int index = y * width + x;
                        Color32 old = pixels[index];
                        if (old.a == 0 && !layer.UsePaintingAlpha) continue;
                        // The wall sprite also contains the floor and under-counter shadows. Their
                        // original palette has red above green, unlike the adjacent blue-green wall.
                        if (shopWall && old.r > old.g && (y < FloorHeight || (x >= 220 && y < 193)))
                        {
                            if (floorRow >= 0 && old.r >= 47)
                            {
                                int fx = ArtCoordinate(x, 0, Width, floorArt.width, 0f);
                                Color32 stone = floorPaint[floorRow * floorArt.width + fx];
                                // Let the painting supply the foreground joints, shading it near the wall.
                                float floorShade = Mathf.Lerp(1f, old.r / 79f, Mathf.InverseLerp(30f, 66f, y));
                                pixels[index] = new Color32(Shade(old.r, stone.r, floorShade, 1f),
                                    Shade(old.g, stone.g, floorShade, 1f), Shade(old.b, stone.b, floorShade, 1f), old.a);
                            }
                            continue;
                        }
                        if (paintedFixture)
                        {
                            // Some housings have a baked readout. The rest of the fixture takes only painted RGB.
                            if (layer.PreserveDisplay && layer.DisplayArea.Contains(new Vector2Int(x, y))) continue;
                            int fx = layer.SourceDisplay.width > 0
                                ? FitDisplayAxis(x, area.xMin, layer.DisplayArea.xMin, layer.DisplayArea.xMax, area.xMax,
                                    layer.SourceCrop.xMin, layer.SourceDisplay.xMin, layer.SourceDisplay.xMax, layer.SourceCrop.xMax, art.width)
                                : ArtCoordinate(x, area.x, area.width, art.width, 0f);
                            Color32 face = material[sy * art.width + fx];
                            pixels[index] = new Color32(face.r, face.g, face.b, layer.UsePaintingAlpha ? face.a : old.a);
                            continue;
                        }
                        if (paintedRow >= 0 && PaintsDeskAt(x, y))
                        {
                            int px = ArtCoordinate(x, PaintedDesk.x, PaintedDesk.width, deskPainting.width, 0f);
                            Color32 painted = deskPaint[paintedRow * deskPainting.width + px];
                            pixels[index] = new Color32(painted.r, painted.g, painted.b, old.a);
                            continue;
                        }
                        float light = Luminance(old);
                        // Retain bright readout panels and dark icon ink on controls and display housings.
                        if (layer.Mix < 1f && old.a >= 128 && (light > 173.4f || light < 38.25f)) continue;
                        // The cyan marks on the lower-right equipment are drawn into the desk sprite.
                        if (desk && x >= 785 && y < DeskCupboard.yMax && old.g > old.r + 24 && old.b > old.r + 20) continue;

                        if (cupboard != null && x >= DeskCupboard.xMin && x < DeskCupboard.xMax
                            && y >= DeskCupboard.yMin && y < DeskCupboard.yMax)
                        {
                            int cx = ArtCoordinate(x, DeskCupboard.x, DeskCupboard.width, cupboardArt.width, 0.25f);
                            int cy = ArtCoordinate(y, DeskCupboard.y, DeskCupboard.height, cupboardArt.height, 0f);
                            // This face replaces a vent. Keep its silhouette only, so the vent and reflections cannot ghost through the joinery.
                            Color32 face = cupboard[cy * cupboardArt.width + cx];
                            pixels[index] = new Color32(face.r, face.g, face.b, old.a);
                            continue;
                        }
                        int sx = ArtCoordinate(x, area.x, area.width, art.width, ceramic ? 0.025f : 0f);
                        Color32 sample = material[sy * art.width + sx];
                        if (keepStructure)
                        {
                            // Use the original shape's value contrast and the new material's color and fine detail.
                            float bias = desk ? (light < 128f ? -6f : 3f) : 0f;
                            pixels[index] = StructuredPixel(old, sample, light, layer.Mix, ceramic ? 0.38f : 0.18f, bias);
                            continue;
                        }
                        float shade = 1f + (light / 255f - 0.48f) * layer.ShadeStrength;
                        pixels[index] = new Color32(Shade(old.r, sample.r, shade, layer.Mix),
                            Shade(old.g, sample.g, shade, layer.Mix), Shade(old.b, sample.b, shade, layer.Mix), old.a);
                    }
                }

                output = new Texture2D(width, height, TextureFormat.RGBA32, false);
                output.name = "jinsong_poc.interior." + layer.ObjectName;
                output.filterMode = FilterMode.Point;
                output.wrapMode = TextureWrapMode.Clamp;
                output.SetPixels32(pixels);
                output.Apply(false, false);
                Vector2 pivot = new Vector2(original.pivot.x / originalRect.width, original.pivot.y / originalRect.height);
                // Keep fractional scene geometry when an animation sheet divides into non-integer cells.
                Rect spriteRect = new Rect(0, 0, originalRect.width, originalRect.height);
                sprite = Sprite.Create(output, spriteRect, pivot, original.pixelsPerUnit, 0, SpriteMeshType.FullRect, original.border);
                if (sprite == null) throw new InvalidOperationException("Sprite creation returned null.");
                sprite.name = output.name;
                UnityEngine.Object.DontDestroyOnLoad(output);
                UnityEngine.Object.DontDestroyOnLoad(sprite);
                return sprite;
            }
            catch (Exception e)
            {
                if (sprite != null) UnityEngine.Object.Destroy(sprite);
                if (output != null) UnityEngine.Object.Destroy(output);
                Mod.Log.Warning("Interior art kept original for " + layer.ObjectName + ": " + e.Message);
                return null;
            }
        }

        private static bool PaintsDeskAt(int x, int y)
        {
            if (x < PaintedDesk.xMin || x >= PaintedDesk.xMax) return false;
            if (y < DeskRimTop && x >= 801) return false; // Angled instrument panel at the right return.
            int sceneRow = Height - 1 - y;
            if (sceneRow < 325) return true;
            int inset = Math.Max(0, (335 - sceneRow) / 2);
            return x < 380 + inset || x >= 525 - inset; // Leave the original raised button mount.
        }

        private static int DeskPaintingRow(int y, int artHeight)
        {
            // Register the single painting's bevel to the original desk edge. No tiling or inherited lighting.
            float v = y < DeskRimTop
                ? PaintedRimFraction * (y + 0.5f - PaintedDesk.yMin) / (DeskRimTop - PaintedDesk.yMin)
                : PaintedRimFraction + (1f - PaintedRimFraction) * (y + 0.5f - DeskRimTop) / (PaintedDesk.yMax - DeskRimTop);
            return Mathf.Clamp((int)(v * artHeight), 0, artHeight - 1);
        }

        private static Color32[] GetArtPixels(Texture2D texture)
        {
            Color32[] material;
            if (!ArtPixels.TryGetValue(texture, out material))
            {
                material = texture.GetPixels32();
                ArtPixels.Add(texture, material);
            }
            return material;
        }

        private static float Luminance(Color32 pixel)
        {
            return (299f * pixel.r + 587f * pixel.g + 114f * pixel.b) / 1000f;
        }

        private static Color32 StructuredPixel(Color32 original, Color32 sample, float sourceLight, float mix,
            float materialLightShare, float bias)
        {
            float materialLight = Luminance(sample);
            float value = Mathf.Lerp(sourceLight, materialLight, materialLightShare) + bias;
            return new Color32(StructuredChannel(original.r, sample.r, materialLight, value, mix),
                StructuredChannel(original.g, sample.g, materialLight, value, mix),
                StructuredChannel(original.b, sample.b, materialLight, value, mix), original.a);
        }

        private static byte StructuredChannel(byte original, byte sample, float materialLight, float value, float mix)
        {
            float tinted = value + (sample - materialLight) * 0.65f;
            return (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(original, tinted, mix)), 0, 255);
        }

        private static int ArtCoordinate(int pixel, int start, int size, int artSize, float inset)
        {
            float t = (pixel - start + 0.5f) / size;
            return Mathf.Clamp((int)((inset + t * (1f - 2f * inset)) * artSize), 0, artSize - 1);
        }

        // Align the painted frame to the stock display opening.
        private static int FitDisplayAxis(int pixel, int start, int displayStart, int displayEnd, int end,
            float cropStart, float sourceStart, float sourceEnd, float cropEnd, int artSize)
        {
            float center = pixel + 0.5f;
            float uv;
            if (center < displayStart)
                uv = Mathf.Lerp(cropStart, sourceStart, Mathf.InverseLerp(start, displayStart, center));
            else if (center < displayEnd)
                uv = Mathf.Lerp(sourceStart, sourceEnd, Mathf.InverseLerp(displayStart, displayEnd, center));
            else
                uv = Mathf.Lerp(sourceEnd, cropEnd, Mathf.InverseLerp(displayEnd, end, center));
            return Mathf.Clamp((int)(uv * artSize), 0, artSize - 1);
        }

        private static byte Shade(byte original, byte channel, float shade, float mix)
        {
            return (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(original, channel * shade, mix)), 0, 255);
        }

        private static RectInt VisibleBounds(Color32[] pixels, int width, int height)
        {
            int left = width, right = -1, bottom = height, top = -1;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (pixels[y * width + x].a != 0)
                    {
                        left = Math.Min(left, x);
                        right = Math.Max(right, x);
                        bottom = Math.Min(bottom, y);
                        top = Math.Max(top, y);
                    }
            return right < left ? new RectInt() : new RectInt(left, bottom, right - left + 1, top - bottom + 1);
        }

        internal static Color32[] ReadSpritePixels(Sprite sprite, int width, int height)
        {
            Texture2D original = sprite.texture;
            Rect region = sprite.textureRect;
            int w = Mathf.CeilToInt(region.width), h = Mathf.CeilToInt(region.height);
            int x = Mathf.RoundToInt(region.x), y0 = Mathf.RoundToInt(region.y);
            int offsetX = Mathf.RoundToInt(sprite.textureRectOffset.x), offsetY = Mathf.RoundToInt(sprite.textureRectOffset.y);
            if (w < 1 || h < 1 || x < 0 || y0 < 0 || x + w > original.width
                || y0 + h > original.height || offsetX < 0 || offsetY < 0 || offsetX + w > width || offsetY + h > height)
                return null;
            Color32[] part = ReadTextureRegion(original, new Rect(x, y0, w, h), w, h);
            if (w == width && h == height) return part;
            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < h; y++)
                Array.Copy(part, y * w, pixels, (y + offsetY) * width + offsetX, w);
            return pixels;
        }

        private static Color32[] ReadTextureRegion(Texture2D original, Rect region, int w, int h)
        {
            if (original.isReadable && w == original.width && h == original.height) return original.GetPixels32();
            RenderTexture previous = RenderTexture.active;
            RenderTexture target = RenderTexture.GetTemporary(original.width, original.height, 0, RenderTextureFormat.ARGB32);
            Texture2D copy = null;
            try
            {
                Graphics.Blit(original, target);
                RenderTexture.active = target;
                copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
                copy.ReadPixels(region, 0, 0, false);
                return copy.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (copy != null) UnityEngine.Object.Destroy(copy);
            }
        }

        internal static void RestoreAndDispose()
        {
            ConceptFixtures.Restore();
            UnifiedInterior.Restore();
            foreach (Layer layer in Layers)
            {
                if (layer.Renderer != null && layer.Applied != null && layer.Renderer.sprite == layer.Applied)
                    layer.Renderer.sprite = layer.Original;
                ReleaseLayer(layer);
            }
            if (closedShutterImage != null && ClosedShutter.Applied != null && closedShutterImage.sprite == ClosedShutter.Applied)
                closedShutterImage.sprite = ClosedShutter.Original;
            closedShutterImage = null;
            ReleaseLayer(ClosedShutter);
            ArtPixels.Clear();
        }

        private static void ReleaseLayer(Layer layer)
        {
            layer.Renderer = null;
            layer.Applied = layer.Original = null;
            foreach (Sprite sprite in layer.Composites.Values)
            {
                if (sprite == null) continue;
                UnityEngine.Object.Destroy(sprite.texture);
                UnityEngine.Object.Destroy(sprite);
            }
            layer.Composites.Clear();
        }
    }
}
