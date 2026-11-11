using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JinSongPoc
{
    internal static class ConceptFixtures
    {
        private const int Width = 960, Height = 540;
        private static readonly Vector3 ShowcaseOffset = new Vector3(6.57f, 0f, 0f);
        private static readonly Vector3 AlcoveOffset = new Vector3(-1.08f, 0f, 0f);
        private static readonly List<Fixture> Fixtures = new List<Fixture>();
        private static readonly List<SpriteRenderer> Hidden = new List<SpriteRenderer>();
        private static readonly List<Sprite> Sprites = new List<Sprite>();
        private static readonly List<TextSlot> Labels = new List<TextSlot>();
        private static Font ledgerFont, queueFont;
        private static bool ledgerFontChecked, queueFontChecked;
        private static readonly Dictionary<Transform, Vector3> Moved = new Dictionary<Transform, Vector3>();
        private static readonly Dictionary<SpriteRenderer, Color> SceneColors = new Dictionary<SpriteRenderer, Color>();
        private static readonly Dictionary<Image, Color> GridColors = new Dictionary<Image, Color>();
        private static readonly WindowMove ShowcaseWindow = new WindowMove(ShowcaseOffset);
        private static readonly WindowMove AlcoveWindow = new WindowMove(AlcoveOffset);
        private static readonly WindowMove DocumentWindow = new WindowMove(new Vector3(0.51f, 1.92f, 0f), 0.72f);
        private static readonly WindowMove WasteWindow = new WindowMove(new Vector3(-0.63f, -0.03f, 0f), 0.78f);
        private static Transform sceneRoot;
        private static Sprite frameSprite;
        private static RectTransform stockFrame;
        private static UtilityArt archiveArt, wasteArt, tunesArt;
        private static TMP_Text queueLabel;
        private static readonly Dictionary<Image, Sprite> ItemArt = new Dictionary<Image, Sprite>();
        private static readonly Dictionary<Image, float> ItemHitThresholds = new Dictionary<Image, float>();
        private static readonly Dictionary<SpriteRenderer, bool> BakedArt = new Dictionary<SpriteRenderer, bool>();
        private static readonly Dictionary<Interactable, bool> OutlineFlags = new Dictionary<Interactable, bool>();
        private static readonly Dictionary<EPOOutline.Outlinable, bool> NativeOutlines = new Dictionary<EPOOutline.Outlinable, bool>();
        private static readonly HotspotPin[] Pins = new HotspotPin[ShopHotspots.ZoneCount];
        private static readonly HotspotPin CassettePin = new HotspotPin();
        private static UtilityArt blankArt;
        private static GameObject memo;
        private static GameObject hiddenClientele;
        private static SpriteRenderer liftedBanner;
        private static SpriteRenderer cityEmblem;
        private static CityRules.Ending cityEmblemKind;
        private static GameObject alcoveClose;

        private sealed class Fixture
        {
            internal SpriteRenderer Renderer;
            internal Sprite Original, Idle, Closed, Open, Applied;
            internal bool AccentNativeFrames;
            internal Color BaseColor;
            internal BoxCollider2D[] Hitboxes;
            internal Vector2[] Offsets, Sizes;
            internal bool[] Enabled;

            internal void Refresh()
            {
                if (Renderer == null || Renderer.sprite == Applied || Renderer.sprite == null) return;
                Original = Renderer.sprite;
                Applied = Open != null && Original.name == "poster_down" ? Open : Closed;
                Renderer.sprite = Applied;
                if (AccentNativeFrames)
                    Renderer.color = Original == Idle ? BaseColor : BaseColor * new Color(1.20f, 1.13f, 1.02f, 1f);
            }
        }

        private sealed class TextSlot
        {
            internal TMP_Text Text;
            internal RectInt Target;
            internal Vector2 OriginalAnchor, OriginalSize;
            internal float OriginalFont;
            internal float FontRatio;
            internal Color OriginalColor;
            internal bool OriginalAutoSize, OriginalOverride;
            internal TextAlignmentOptions OriginalAlignment;
            internal bool Ledger, Counter, AmountOnly;
            internal FontStyles OriginalStyle;
            internal bool OriginalEnabled;
            internal Text InkText;
            internal TextMeshProUGUI Currency;
        }

        private sealed class UtilityArt
        {
            private readonly Color32[] source;
            private readonly int stride;
            private readonly RectInt area;
            private readonly Dictionary<Sprite, Sprite> variants = new Dictionary<Sprite, Sprite>();

            internal UtilityArt(Color32[] source, int stride, RectInt tile, bool blank = false)
            {
                this.source = source;
                this.stride = stride;
                area = blank ? tile : Trim(source, stride, tile);
            }

            internal Sprite Resolve(Sprite stock)
            {
                if (area.width == 0) return null;
                Sprite sprite;
                if (variants.TryGetValue(stock, out sprite)) return sprite;
                int width = Mathf.CeilToInt(stock.rect.width), height = Mathf.CeilToInt(stock.rect.height);
                Color32[] pixels = new Color32[width * height];
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        pixels[y * width + x] = source[(area.y + y * area.height / height) * stride + area.x + x * area.width / width];
                sprite = CreateSprite("jinsong_poc.concept." + stock.name, pixels, width, height,
                    stock.pivot / new Vector2(stock.rect.width, stock.rect.height), Vector4.zero, stock.pixelsPerUnit);
                variants.Add(stock, sprite);
                return sprite;
            }
        }

        private sealed class HotspotPin
        {
            private RectTransform target;
            private Vector2 original, applied;
            private Vector3 originalScale, appliedScale;
            private Vector3 referenceScale;
            private Rect size;

            internal void Apply(PixelWindow window, RectTransform content, Rect bounds, RectInt destination, bool uniform,
                RectTransform pixelReference = null)
            {
                RectTransform rect = window == null ? null : window.rectTransform;
                if (rect == null || content == null || bounds.width <= 0 || bounds.height <= 0
                    || rect == target && rect.anchoredPosition == applied && rect.localScale == appliedScale && size == bounds
                        && (pixelReference == null || pixelReference.lossyScale == referenceScale)) return;
                Restore();
                RectTransform parent = rect.parent as RectTransform;
                Vector2 min, max;
                if (parent == null || !Project(parent, World(destination.xMin, destination.yMax), out min)
                    || !Project(parent, World(destination.xMax, destination.yMin), out max)) return;
                Vector2 a = parent.InverseTransformPoint(content.TransformPoint(bounds.min));
                Vector2 b = parent.InverseTransformPoint(content.TransformPoint(bounds.max));
                Vector2 span = b - a;
                if (span.x <= 0f || span.y <= 0f) return;
                Vector2 factor = (max - min) / span;
                if (uniform) factor = Vector2.one * Mathf.Min(factor.x, factor.y);
                if (pixelReference != null)
                {
                    Vector3 current = content.lossyScale;
                    referenceScale = pixelReference.lossyScale;
                    if (Math.Abs(current.x) < .0001f || Math.Abs(current.y) < .0001f) return;
                    // All native GameGridInventory cells use the same local pixel pitch.
                    // Match the stock inventory's pixel scale instead of filling the mural.
                    factor = new Vector2(Math.Abs(referenceScale.x / current.x), Math.Abs(referenceScale.y / current.y));
                }
                target = rect;
                size = bounds;
                original = rect.anchoredPosition;
                originalScale = rect.localScale;
                appliedScale = Vector3.Scale(originalScale, new Vector3(factor.x, factor.y, 1f));
                rect.localScale = appliedScale;
                Vector2 center = parent.InverseTransformPoint(content.TransformPoint(bounds.center));
                applied = original + (min + max) * 0.5f - center;
                rect.anchoredPosition = applied;
            }

            internal void Restore()
            {
                if (target == null) return;
                if (target.anchoredPosition == applied) target.anchoredPosition = original;
                if (target.localScale == appliedScale) target.localScale = originalScale;
                target = null;
            }
        }

        private sealed class WindowMove
        {
            private readonly Vector3 offset;
            private readonly float scale;
            private RectTransform target;
            private Vector2 original, applied;
            private Vector3 originalScale, appliedScale;

            internal WindowMove(Vector3 offset, float scale = 1f)
            {
                this.offset = offset;
                this.scale = scale;
            }

            internal void Apply(PixelWindow window)
            {
                RectTransform rect = window == null ? null : window.rectTransform;
                if (rect == null || rect == target && rect.anchoredPosition == applied && rect.localScale == appliedScale) return;
                Restore();
                RectTransform parent = rect.parent as RectTransform;
                if (parent == null) return;
                Vector2 start, end;
                if (!Project(parent, Vector3.zero, out start) || !Project(parent, offset, out end)) return;
                target = rect;
                original = rect.anchoredPosition;
                originalScale = rect.localScale;
                applied = original + end - start;
                appliedScale = originalScale * scale;
                rect.anchoredPosition = applied;
                rect.localScale = appliedScale;
            }

            internal void Restore()
            {
                if (target != null)
                {
                    if (target.anchoredPosition == applied) target.anchoredPosition = original;
                    if (target.localScale == appliedScale) target.localScale = originalScale;
                }
                target = null;
            }
        }

        internal static bool Replaces(string objectName)
        {
            foreach (SpriteRenderer renderer in BakedArt.Keys)
                if (renderer != null && renderer.name == objectName) return true;
            return Fixtures.Exists(f => f.Renderer != null && f.Renderer.name == objectName)
                || Hidden.Exists(r => r != null && r.name == objectName);
        }

        internal static void Apply(LoadedMod assets)
        {
            if (sceneRoot == null)
            {
                Restore();
                GameObject root = GameObject.Find("--Physical Store--");
                GameObject controls = GameObject.Find("--Interactable");
                if (root == null || controls == null) return;
                if (UnifiedInterior.IsHotspot) BindHotspots(assets, root.transform, controls.transform);
                else Bind(assets, root.transform, controls.transform);
                Transform clientele = controls.transform.Find("ClienteleDisplay");
                if (clientele != null && clientele.gameObject.activeSelf)
                {
                    hiddenClientele = clientele.gameObject;
                    hiddenClientele.SetActive(false);
                }
            }
            RefreshSprites();
            foreach (TextSlot label in Labels) PlaceLabel(label);
            ApplyWindows(EmporiumEntry.Instance);
            ShopMenuSkin.Apply(assets);
        }

        internal static void RefreshSprites()
        {
            foreach (Fixture fixture in Fixtures) fixture.Refresh();
            foreach (SpriteRenderer renderer in BakedArt.Keys)
                if (renderer != null) renderer.enabled = false;
            foreach (EPOOutline.Outlinable outline in NativeOutlines.Keys)
                if (outline != null && outline.enabled) outline.enabled = false;
            if (liftedBanner != null)
            {
                StorePoster poster = StorePoster.Instance;
                liftedBanner.enabled = poster != null && poster.isPulled;
            }
        }

        internal static void RefreshCityEmblem()
        {
            if (sceneRoot == null) return;
            CityRules.Ending ending = CityStory.MorningEmblem();
            if (cityEmblem != null)
            {
                if (cityEmblemKind == ending) return;
                UnityEngine.Object.Destroy(cityEmblem.gameObject);
                cityEmblem = null;
            }
            if (ending != CityRules.Ending.Song && ending != CityRules.Ending.Commendation) return;
            bool song = ending == CityRules.Ending.Song;
            Sprite sprite = song ? CreateSongFlag() : CreateCommendationFlag();
            if (sprite == null) return;
            cityEmblemKind = ending;
            var emblem = new GameObject("JinSong.CityEndingEmblem");
            emblem.transform.SetParent(sceneRoot, false);
            // The near flag stands in front of the stall props and behind the window wall.
            emblem.transform.position = song ? World(543, 84) : World(358, 112) + Vector3.back * .02f;
            cityEmblem = emblem.AddComponent<SpriteRenderer>();
            cityEmblem.sprite = sprite;
            cityEmblem.sortingOrder = song ? 1 : 2;
        }

        private static Sprite CreateCommendationFlag()
        {
            Texture2D texture;
            if (Mod.Assets == null || !Mod.Assets.Textures.TryGetValue("jin_city_commendation_flag", out texture) || texture == null)
                return null;
            return CreateSprite("jinsong_poc.city_commendation_flag", texture.GetPixels32(), texture.width, texture.height,
                new Vector2(.5f, .5f), Vector4.zero);
        }

        private static Sprite CreateSongFlag()
        {
            const int width = 24, height = 36, scale = 2, inkY = 7;
            string[] lettering = {
                "00000100000", "00000010000", "01111111110", "01000000010",
                "00000100000", "00111111100", "00001110000", "00010101000",
                "00100100100", "01000100010", "00000100000"
            };
            var pixels = new Color32[width * height];
            int inkX = (width - lettering[0].Length * scale) / 2;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (y < 3 || (y < 9 && Math.Abs(x - width / 2) < 9 - y)) continue;
                    int edge = Math.Min(Math.Min(x, width - 1 - x), Math.Min(y, height - 1 - y));
                    int tone = (x * 17 + y * 7) % 8;
                    Color32 color = new Color32((byte)(122 + tone), 42, 31, 255);
                    if (edge == 1 || y >= height - 2) color = new Color32(153, 109, 56, 255);
                    int col = x - inkX, row = height - 1 - y - inkY;
                    if (col >= 0 && col < lettering[0].Length * scale && row >= 0 && row < lettering.Length * scale
                        && lettering[row / scale][col / scale] == '1') color = new Color32(232, 193, 117, 255);
                    pixels[y * width + x] = color;
                }
            return CreateSprite("jinsong_poc.city_emblem", pixels, width, height, new Vector2(.5f, .5f), Vector4.zero);
        }

        private static void BindHotspots(LoadedMod assets, Transform room, Transform controls)
        {
            ShopHotspots.Bind(room);
            BindHotspot(controls.Find("Door"), 1);
            memo = new GameObject("JinSong.Notebook");
            memo.transform.SetParent(controls, false);
            memo.layer = controls.Find("Calendar").gameObject.layer;
            memo.AddComponent<EPOOutline.Outlinable>();
            memo.AddComponent<StoreNotebook>();
            memo.AddComponent<BoxCollider2D>();
            Interactable memoTarget = memo.AddComponent<Interactable>();
            memoTarget.isAvailable = true;
            // Native controllable targets enter RTS selection instead of invoking IClickable.
            memoTarget.isControlable = false;
            BindHotspot(memo.transform, 2);
            BindHotspot(controls.Find("feedback_box"), 4);
            BindHotspot(controls.Find("Bell"), 5);
            BindHotspot(controls.Find("ShutterButton"), 6);
            BindHotspot(controls.Find("SecButton"), 7);
            BindHotspot(controls.Find("NegociationTablet"), 9);
            BindHotspot(controls.Find("Calendar"), 10);
            BindHotspot(controls.Find("CreditDisplay"), 11);
            BindHotspot(controls.Find("_MagnifyingWindowCollapsed"), 12);
            BindHotspot(controls.Find("wall_phone"), 13);
            BindHotspot(controls.Find("Recorder"), 14);
            BindHotspot(controls.Find("transferButton"), ShopHotspots.StockButtonZone);
            BindHotspot(controls.Find("dismissButton"), 16);
            BindHotspot(room.Find("NutruifruitPoster"), 17);
            BindHotspot(controls.Find("SleepingBag"), 20);

            foreach (string name in new[] { "CalendarTop", "CalendarGlow", "CreditDisplayGlow", "Calendar_Wire",
                "Table Wires", "connecting wire", "phoneGlow", "ClientDisplay", "ClientDisplayTop", "ClientDisplayGlow", "ClienteleDisplayGlow",
                "DeskAnimation", "DeskAnimation (1)", "DeskAnimation (2)", "DeskAnimation (3)", "Door Lock",
                "showcase", "HiddenSpace", "wires" })
                Mask(controls.Find(name) ?? room.Find(name));
            Mask(controls.Find("SecButton/Light"));
            StoreCalendar calendar = controls.Find("Calendar").GetComponent<StoreCalendar>();
            StoreCreditDisplay cash = controls.Find("CreditDisplay").GetComponent<StoreCreditDisplay>();
            if (UnifiedInterior.HasCounterLayout)
            {
                RememberLabel(calendar.dayTMP, ShopHotspots.ToScene(new RectInt(292, 49, 198, 43)), 0.43f, true);
                RememberLabel(cash.creditDisplayText, ShopHotspots.LedgerInk, .94f, ledger: true, amountOnly: true);
            }
            else
            {
                RememberLabel(calendar.dayTMP, ShopHotspots.ToScene(new RectInt(742, 524, 93, 44)), 0.17f, true);
                RememberLabel(cash.creditDisplayText, ShopHotspots.ToScene(new RectInt(847, 524, 76, 45)), 0.216f, true);
            }
            Transform counter = controls.Find("ClientDisplay");
            queueLabel = counter.GetComponent<StoreClientCounter>().clientCounterTMP;
            if (UnifiedInterior.HasQueuePaper)
            {
                RememberLabel(queueLabel, ShopHotspots.QueueNumber, .88f, counter: true);
                SetHitboxes(counter, new RectInt());
            }
            else RememberLabel(queueLabel, ShopHotspots.ToScene(new RectInt(471, 249, 32, 40)), 0.27f);
            // All fixture items stay on their native slot and only their scene-facing image is suppressed.
            blankArt = new UtilityArt(new Color32[1], 1, new RectInt(0, 0, 1, 1), blank: true);
            Texture2D utilities;
            if (assets.Textures.TryGetValue("jin_concept_utilities", out utilities))
            {
                tunesArt = new UtilityArt(utilities.GetPixels32(), utilities.width, new RectInt(0, 0, utilities.width / 2, utilities.height / 2));
                Mask(room.Find("Radio"));
            }
            Texture2D art = assets.Textures[ShopHotspots.ArtKey];
            liftedBanner = MakeAlcove(room, art);
            for (int i = 0; i < Pins.Length; i++) Pins[i] = new HotspotPin();
            sceneRoot = room;
            EmporiumEntry entry = EmporiumEntry.Instance;
            if (entry != null && entry.frontInvinvElement != null) entry.frontInvinvElement.Validate();
            if (entry != null && entry.backInvinvElement != null) entry.backInvinvElement.Validate();
            Mod.Log.Log("Approved hotspot board bound with native interactions.");
        }

        private static SpriteRenderer MakeAlcove(Transform room, Texture2D art)
        {
            // Native PullUp/PullDown remains the sole owner of the compartment state.
            // Draw a recessed timber cavity in the space baked into the room's cloth sign.
            RectInt bounds = ShopHotspots.Zone(17);
            GameObject obj = new GameObject("JinSong.OpenAlcove");
            obj.transform.SetParent(room, false);
            obj.transform.position = World(bounds.x + bounds.width / 2f, bounds.y + bounds.height / 2f);
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            Color32[] pixels = new Color32[bounds.width * bounds.height];
            for (int y = 0; y < bounds.height; y++)
                for (int x = 0; x < bounds.width; x++)
                {
                    int edge = Math.Min(Math.Min(x, bounds.width - 1 - x), Math.Min(y, bounds.height - 1 - y));
                    int grain = (x * 11 + y * 3) % 7;
                    pixels[y * bounds.width + x] = edge < 4 ? new Color32(91, 58, 32, 255)
                        : edge < 7 ? new Color32(41, 25, 16, 255)
                        : new Color32((byte)(28 + grain), (byte)(20 + grain / 2), (byte)(15 + grain / 3), 255);
                    // The upper roll stays outside the item grid and remains a native close target.
                    if (y >= bounds.height - 13 && edge >= 2)
                        pixels[y * bounds.width + x] = new Color32(151, 116, 72, 255);
                }
            renderer.sprite = CreateSprite("jinsong_poc.concept.open_alcove", pixels, bounds.width, bounds.height,
                new Vector2(.5f, .5f), Vector4.zero);
            renderer.sortingOrder = 12;
            renderer.color = Color.white;
            renderer.enabled = false;
            return renderer;
        }

        private static void Mask(Transform obj)
        {
            SpriteRenderer renderer = obj == null ? null : obj.GetComponent<SpriteRenderer>();
            if (renderer == null || BakedArt.ContainsKey(renderer)) return;
            BakedArt.Add(renderer, renderer.enabled);
            renderer.enabled = false;
        }

        private static void BindHotspot(Transform obj, int number)
        {
            if (obj == null) return;
            Mask(obj);
            Interactable target = obj.GetComponent<Interactable>();
            if (target == null) return;
            ShopHotspots.Track(number, target);
            OutlineFlags.Add(target, target.doNotOutline);
            target.doNotOutline = true;
            EPOOutline.Outlinable outline = obj.GetComponent<EPOOutline.Outlinable>();
            if (outline != null)
            {
                NativeOutlines.Add(outline, outline.enabled);
                outline.enabled = false;
            }
            SetHitboxes(obj, ShopHotspots.Zone(number));
        }

        private static void SetHitboxes(Transform obj, RectInt hit)
        {
            Fixture hitbox = new Fixture { Hitboxes = obj.GetComponents<BoxCollider2D>() };
            PlaceHitboxes(hitbox, obj, hit);
            Fixtures.Add(hitbox);
        }

        private static void Bind(LoadedMod assets, Transform room, Transform controls)
        {
            Texture2D atlas = assets.Textures["jin_concept_fixtures"];
            Texture2D banner = assets.Textures["jin_concept_banner"];
            Color32[] props = atlas.GetPixels32();
            Color32[] cloth = banner.GetPixels32();
            int halfWidth = atlas.width / 2, halfHeight = atlas.height / 2;
            RectInt ledger = new RectInt(416, 292, 134, 48);
            RectInt datePage = new RectInt(416, 292, 67, 48);
            RectInt moneyPage = new RectInt(483, 292, 67, 48);
            Attach(controls, "Calendar", atlas, props, new RectInt(0, halfHeight, halfWidth, atlas.height - halfHeight), ledger, datePage);
            Attach(controls, "CreditDisplay", atlas, props, new RectInt(0, halfHeight, halfWidth, atlas.height - halfHeight), ledger, moneyPage);
            Attach(controls, "NegociationTablet", atlas, props, new RectInt(halfWidth, halfHeight, halfWidth, atlas.height - halfHeight),
                new RectInt(211, 297, 119, 42));
            Attach(controls, "_MagnifyingWindowCollapsed", atlas, props, new RectInt(0, 0, halfWidth, halfHeight),
                new RectInt(646, 270, 56, 60));
            Attach(controls, "wall_phone", atlas, props, new RectInt(halfWidth, 0, halfWidth, halfHeight),
                new RectInt(730, 296, 90, 43));
            Attach(room, "NutruifruitPoster", banner, cloth, new RectInt(0, 0, banner.width, banner.height),
                new RectInt(674, 39, 77, 121), folded: true);
            Texture2D paper = assets.Textures["jin_letter_sheet"];
            Attach(controls, "ClientDisplay", paper, paper.GetPixels32(), new RectInt(0, 0, paper.width, paper.height),
                new RectInt(211, 83, 72, 62));
            Texture2D utilities;
            if (assets.Textures.TryGetValue("jin_concept_utilities", out utilities)) BindUtilities(utilities, controls, room);

            Transform date = controls.Find("Calendar"), cash = controls.Find("CreditDisplay");
            StoreCalendar calendar = date == null ? null : date.GetComponent<StoreCalendar>();
            StoreCreditDisplay credit = cash == null ? null : cash.GetComponent<StoreCreditDisplay>();
            if (calendar != null) RememberLabel(calendar.dayTMP, new RectInt(423, 303, 54, 26));
            if (credit != null) RememberLabel(credit.creditDisplayText, new RectInt(492, 303, 50, 26));
            Transform counter = controls.Find("ClientDisplay");
            StoreClientCounter clientCounter = counter == null ? null : counter.GetComponent<StoreClientCounter>();
            if (clientCounter != null)
            {
                queueLabel = clientCounter.clientCounterTMP;
                RememberLabel(queueLabel, new RectInt(215, 98, 64, 32));
            }
            foreach (string name in new[] { "CalendarTop", "CalendarGlow", "CreditDisplayGlow", "Calendar_Wire",
                "Table Wires", "connecting wire", "phoneGlow", "ClientDisplayTop", "ClientDisplayGlow", "ClienteleDisplayGlow",
                "DeskAnimation", "DeskAnimation (1)", "DeskAnimation (2)", "DeskAnimation (3)" })
                Hide(room, controls, name);

            Move(room.Find("showcase"), ShowcaseOffset);
            Move(room.Find("HiddenSpace"), AlcoveOffset);
            Texture2D controlArt, serviceArt;
            if (assets.Textures.TryGetValue("jin_fit_controls", out controlArt)) BindControls(controlArt, controls, room);
            if (assets.Textures.TryGetValue("jin_fit_services", out serviceArt)) BindServices(serviceArt, controls);
            Tint(room.Find("showcase"), new Color(0.58f, 0.75f, 0.88f));
            foreach (string name in new[] { "Calendar", "CreditDisplay", "ClientDisplay", "wall_phone", "_MagnifyingWindowCollapsed" })
                Tint(controls.Find(name), new Color(0.91f, 0.84f, 0.75f));
            frameSprite = MakeFrame(assets.Textures["jin_concept_shop"]);
            sceneRoot = room;
            EmporiumEntry entry = EmporiumEntry.Instance;
            if (entry != null && entry.invElement != null) entry.invElement.Validate();
            if (entry != null && entry.showcaseElement != null) entry.showcaseElement.Validate();
            Mod.Log.Log("Concept fixtures bound: counter tools, native showcase, native inventory frame.");
        }

        private static void BindControls(Texture2D atlas, Transform controls, Transform room)
        {
            Color32[] pixels = atlas.GetPixels32();
            AttachTile(controls, "Bell", atlas, pixels, 3, 2, 0, new RectInt(302, 253, 30, 23), accentNativeFrames: true);
            AttachTile(controls, "ShutterButton", atlas, pixels, 3, 2, 1, new RectInt(632, 145, 26, 34), accentNativeFrames: true);
            AttachTile(controls, "transferButton", atlas, pixels, 3, 2, 2, new RectInt(353, 343, 27, 18));
            AttachTile(controls, "dismissButton", atlas, pixels, 3, 2, 3, new RectInt(385, 343, 27, 18));
            // Move the button body so its native status lamp follows the new emergency plaque.
            Move(controls.Find("SecButton"), new Vector3(1.80f, 1.30f, 0f));
            AttachTile(controls, "SecButton", atlas, pixels, 3, 2, 4, new RectInt(632, 206, 29, 30));
            AttachTile(room, "Door Lock", atlas, pixels, 3, 2, 5, new RectInt(43, 216, 12, 22));
        }

        private static void BindServices(Texture2D atlas, Transform controls)
        {
            Color32[] pixels = atlas.GetPixels32();
            AttachTile(controls, "Recorder", atlas, pixels, 2, 2, 0, new RectInt(827, 306, 49, 29), accentNativeFrames: true);
            AttachTile(controls, "feedback_box", atlas, pixels, 2, 2, 2, new RectInt(113, 250, 49, 53));
            // The alert and its live count are children of PC and move with the holder.
            Move(controls.Find("PC"), new Vector3(-5.76f, -1.58f, 0f));
            AttachTile(controls, "PC", atlas, pixels, 2, 2, 3, new RectInt(214, 161, 85, 59));
        }

        private static void AttachTile(Transform parent, string name, Texture2D atlas, Color32[] pixels,
            int columns, int rows, int index, RectInt target, bool accentNativeFrames = false)
        {
            int column = index % columns, row = rows - 1 - index / columns;
            int x = column * atlas.width / columns, y = row * atlas.height / rows;
            RectInt tile = new RectInt(x, y, (column + 1) * atlas.width / columns - x,
                (row + 1) * atlas.height / rows - y);
            Attach(parent, name, atlas, pixels, tile, target, accentNativeFrames: accentNativeFrames);
        }

        private static void BindUtilities(Texture2D image, Transform controls, Transform room)
        {
            Color32[] pixels = image.GetPixels32();
            int w = image.width / 2, h = image.height / 2;
            archiveArt = new UtilityArt(pixels, image.width, new RectInt(0, h, w, image.height - h));
            wasteArt = new UtilityArt(pixels, image.width, new RectInt(w, h, image.width - w, image.height - h));
            tunesArt = new UtilityArt(pixels, image.width, new RectInt(0, 0, w, h));
            Attach(controls, "SleepingBag", image, pixels, new RectInt(w, 0, image.width - w, h),
                new RectInt(817, 430, 133, 103));
            Hide(room, controls, "Radio");
        }

        private static void ReplaceItem(GameItem item, UtilityArt art)
        {
            GameItemElement view = item as GameItemElement;
            if (view == null || art == null || view.background == null) return;
            Image image = view.background.image;
            if (image == null || image.sprite == null) return;
            Sprite replacement = art.Resolve(image.sprite);
            if (replacement == null || image.overrideSprite == replacement) return;
            if (!ItemArt.ContainsKey(image)) ItemArt.Add(image, image.overrideSprite == image.sprite ? null : image.overrideSprite);
            image.overrideSprite = replacement;
        }

        private static void Attach(Transform parent, string name, Texture2D image, Color32[] source, RectInt tile,
            RectInt target, RectInt hit = default(RectInt), bool folded = false, bool accentNativeFrames = false)
        {
            Transform obj = parent.Find(name);
            SpriteRenderer renderer = obj == null ? null : obj.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null) return;
            RectInt bounds = Trim(source, image.width, tile);
            if (bounds.width == 0) return;
            if (hit.width == 0) hit = target;
            Fixture fixture = new Fixture { Renderer = renderer, Original = renderer.sprite, Idle = renderer.sprite,
                AccentNativeFrames = accentNativeFrames, BaseColor = renderer.color };
            fixture.Closed = PaintFixture(renderer, source, image.width, bounds, target, hit);
            if (folded)
            {
                // Leave the native alcove reachable while the fabric is rolled up.
                RectInt roll = new RectInt(target.x, target.y, target.width, 15);
                fixture.Open = PaintFixture(renderer, source, image.width, bounds, roll, roll);
            }
            fixture.Hitboxes = renderer.GetComponents<BoxCollider2D>();
            PlaceHitboxes(fixture, obj, hit);
            Fixtures.Add(fixture);
        }

        private static void PlaceHitboxes(Fixture fixture, Transform obj, RectInt hit)
        {
            fixture.Offsets = new Vector2[fixture.Hitboxes.Length];
            fixture.Sizes = new Vector2[fixture.Hitboxes.Length];
            fixture.Enabled = new bool[fixture.Hitboxes.Length];
            for (int i = 0; i < fixture.Hitboxes.Length; i++)
            {
                BoxCollider2D box = fixture.Hitboxes[i];
                fixture.Offsets[i] = box.offset;
                fixture.Sizes[i] = box.size;
                fixture.Enabled[i] = box.enabled;
                if (i > 0 || hit.width == 0)
                {
                    box.enabled = false;
                    continue;
                }
                Vector3 topLeft = obj.InverseTransformPoint(World(hit.xMin, hit.yMin));
                Vector3 bottomRight = obj.InverseTransformPoint(World(hit.xMax, hit.yMax));
                box.offset = (topLeft + bottomRight) * 0.5f;
                box.size = new Vector2(Mathf.Abs(bottomRight.x - topLeft.x), Mathf.Abs(bottomRight.y - topLeft.y));
            }
        }

        private static RectInt Trim(Color32[] pixels, int width, RectInt area)
        {
            int left = area.xMax, right = area.xMin, bottom = area.yMax, top = area.yMin;
            for (int y = area.yMin; y < area.yMax; y++)
                for (int x = area.xMin; x < area.xMax; x++)
                    if (pixels[y * width + x].a > 32)
                    {
                        left = Math.Min(left, x);
                        right = Math.Max(right, x + 1);
                        bottom = Math.Min(bottom, y);
                        top = Math.Max(top, y + 1);
                    }
            return right <= left ? new RectInt() : new RectInt(left, bottom, right - left, top - bottom);
        }

        private static Sprite PaintFixture(SpriteRenderer renderer, Color32[] source, int stride,
            RectInt area, RectInt target, RectInt clip)
        {
            // A shared coordinate grid keeps art and click targets registered to the shop scene.
            Color32[] pixels = new Color32[Width * Height];
            for (int y = clip.yMin; y < clip.yMax; y++)
                for (int x = clip.xMin; x < clip.xMax; x++)
                {
                    int sx = area.x + (x - target.x) * area.width / target.width;
                    int sy = area.yMax - 1 - (y - target.y) * area.height / target.height;
                    pixels[(Height - 1 - y) * Width + x] = source[sy * stride + sx];
                }
            Vector3 position = renderer.transform.position;
            Vector2 pivot = new Vector2(0.5f + position.x * 100f / Width, 0.5f + position.y * 100f / Height);
            return CreateSprite("jinsong_poc.concept." + renderer.name, pixels, Width, Height, pivot, Vector4.zero);
        }

        private static Sprite MakeFrame(Texture2D art)
        {
            Color32[] source = art.GetPixels32();
            Color32[] tile = new Color32[24 * 24];
            int x = art.width * 3 / 5, y = art.height / 5;
            for (int row = 0; row < 24; row++)
                Array.Copy(source, (y + row) * art.width + x, tile, row * 24, 24);
            return CreateSprite("jinsong_poc.concept.stock_frame", tile, 24, 24, new Vector2(0.5f, 0.5f),
                new Vector4(8, 8, 8, 8));
        }

        private static Sprite CreateSprite(string name, Color32[] pixels, int width, int height, Vector2 pivot, Vector4 border,
            float ppu = 100f)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), pivot, ppu, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            Sprites.Add(sprite);
            return sprite;
        }

        private static void Hide(Transform room, Transform controls, string name)
        {
            Transform obj = controls.Find(name) ?? room.Find(name);
            SpriteRenderer renderer = obj == null ? null : obj.GetComponent<SpriteRenderer>();
            if (renderer == null || !renderer.enabled) return;
            renderer.enabled = false;
            Hidden.Add(renderer);
        }

        private static void Move(Transform obj, Vector3 offset)
        {
            if (obj == null) return;
            Moved.Add(obj, obj.localPosition);
            obj.position += offset;
        }

        private static void Tint(Transform obj, Color multiplier)
        {
            SpriteRenderer renderer = obj == null ? null : obj.GetComponent<SpriteRenderer>();
            if (renderer == null) return;
            SceneColors.Add(renderer, renderer.color);
            renderer.color *= multiplier;
        }

        private static void RememberLabel(TMP_Text text, RectInt target, float fontRatio = 0.30f,
            bool ledger = false, bool counter = false, bool amountOnly = false)
        {
            if (text == null) return;
            Labels.Add(new TextSlot { Text = text, Target = target, OriginalAnchor = text.rectTransform.anchoredPosition,
                OriginalSize = text.rectTransform.sizeDelta, OriginalFont = text.fontSize, FontRatio = fontRatio, OriginalColor = text.color,
                OriginalAlignment = text.alignment, OriginalAutoSize = text.enableAutoSizing,
                OriginalOverride = text.overrideColorTags, Ledger = ledger, Counter = counter, AmountOnly = amountOnly,
                OriginalEnabled = text.enabled,
                OriginalStyle = text.fontStyle });
        }

        private static Font FindInkFont(string kind, params string[] names)
        {
            string[] installed = Font.GetOSInstalledFontNames();
            foreach (string name in names)
            {
                if (Array.IndexOf(installed, name) < 0) continue;
                Font font = Font.CreateDynamicFontFromOSFont(name, 32);
                if (font == null) continue;
                font.name = "jinsong_poc." + kind + "." + name;
                return font;
            }
            return null;
        }

        private static void StyleInk(TextSlot label)
        {
            if (label.Counter && !queueFontChecked)
            {
                queueFont = FindInkFont("queue", "STKaiti", "KaiTi", "Kaiti SC", "Georgia");
                queueFontChecked = true;
            }
            if (label.Ledger && !ledgerFontChecked)
            {
                ledgerFont = FindInkFont("ledger", "SimSun", "NSimSun", "STSong", "Songti SC", "Noto Serif CJK SC");
                ledgerFontChecked = true;
            }
            label.Text.fontStyle = label.Counter ? FontStyles.Bold : FontStyles.Normal;
            Font font = label.Counter ? queueFont : ledgerFont;
            string value = label.Text.text;
            string currency = label.Ledger ? GeneralHelper.CreditIcon(false) : null;
            bool hasCurrency = currency != null && value != null && value.EndsWith(currency, StringComparison.Ordinal);
            if (hasCurrency) value = value.Substring(0, value.Length - currency.Length).TrimEnd();
            if (label.AmountOnly)
            {
                hasCurrency = false;
                if (PlayerStore.Instance != null)
                    value = PlayerStore.Instance.playerCash.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (font == null)
                    font = label.InkText == null ? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") : label.InkText.font;
            }
            bool useInk = font != null && !String.IsNullOrEmpty(value);
            if (useInk)
                foreach (char character in value)
                    if (!Char.IsWhiteSpace(character) && !font.HasCharacter(character)) { useInk = false; break; }
            if (useInk && label.InkText == null)
            {
                GameObject ink = new GameObject(label.Counter ? "JinSong.QueueNumber" : "JinSong.LedgerType", typeof(RectTransform));
                RectTransform rect = (RectTransform)ink.transform;
                rect.SetParent(label.Text.rectTransform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                label.InkText = ink.AddComponent<Text>();
                label.InkText.font = font;
                if (label.AmountOnly) label.InkText.fontStyle = FontStyle.Bold;
                label.InkText.raycastTarget = false;
                label.InkText.supportRichText = false;
                label.InkText.alignment = TextAnchor.MiddleCenter;
                label.InkText.horizontalOverflow = HorizontalWrapMode.Overflow;
                label.InkText.verticalOverflow = VerticalWrapMode.Overflow;
            }
            if (hasCurrency && useInk && label.Currency == null)
            {
                GameObject symbol = new GameObject("JinSong.LedgerCurrency", typeof(RectTransform));
                symbol.transform.SetParent(label.Text.rectTransform, false);
                label.Currency = symbol.AddComponent<TextMeshProUGUI>();
                label.Currency.font = label.Text.font;
                label.Currency.spriteAsset = label.Text.spriteAsset;
                label.Currency.raycastTarget = false;
                label.Currency.alignment = TextAlignmentOptions.Center;
                label.Currency.text = currency;
            }
            float gap = 0f, symbolWidth = 0f;
            if (label.InkText != null)
            {
                label.InkText.enabled = useInk && label.OriginalEnabled;
                label.InkText.fontSize = Mathf.Max(1, Mathf.RoundToInt(label.Text.fontSize));
                label.InkText.color = label.Text.color;
                label.InkText.text = value;
                MeasureLedgerCurrency(label, hasCurrency && useInk, out gap, out symbolWidth);
                if (useInk && (label.Counter || UnifiedInterior.HasCounterLayout))
                {
                    float available = label.Text.rectTransform.rect.width;
                    float needed = label.InkText.preferredWidth + gap + symbolWidth;
                    if (needed > available)
                    {
                        label.Text.fontSize *= available / needed;
                        label.InkText.fontSize = Mathf.Max(1, Mathf.FloorToInt(label.Text.fontSize));
                        MeasureLedgerCurrency(label, hasCurrency, out gap, out symbolWidth);
                    }
                }
                label.InkText.rectTransform.offsetMax = new Vector2(-gap - symbolWidth, 0f);
            }
            if (label.Currency != null)
            {
                label.Currency.enabled = useInk && hasCurrency && label.OriginalEnabled;
                label.Currency.color = label.Text.color;
                label.Currency.rectTransform.anchoredPosition = new Vector2((label.InkText.preferredWidth + gap) * .5f,
                    -label.Currency.fontSize * .11f);
                label.Currency.rectTransform.sizeDelta = new Vector2(symbolWidth + 2f, label.Text.rectTransform.rect.height);
            }
            if (!useInk && (label.Counter || hasCurrency))
            {
                float needed = label.Text.GetPreferredValues(label.Text.text).x;
                if (needed > label.Text.rectTransform.rect.width)
                    label.Text.fontSize *= label.Text.rectTransform.rect.width / needed;
            }
            label.Text.enabled = !useInk && label.OriginalEnabled;
        }

        private static void MeasureLedgerCurrency(TextSlot label, bool shown, out float gap, out float width)
        {
            gap = width = 0f;
            if (!shown || label.Currency == null) return;
            label.Currency.fontSize = label.InkText.fontSize;
            gap = label.Currency.fontSize * .14f;
            width = label.Currency.GetPreferredValues(label.Currency.text).x;
        }

        private static void PlaceLabel(TextSlot label)
        {
            if (label.Text == null) return;
            RectTransform rect = label.Text.rectTransform;
            RectTransform parent = rect.parent as RectTransform;
            Vector3 bottomLeft = World(label.Target.xMin, label.Target.yMax);
            Vector3 topRight = World(label.Target.xMax, label.Target.yMin);
            Vector3 paperBottom, paperTop;
            if (label.AmountOnly && LocationArt.TryWorkshopBalanceArea(out paperBottom, out paperTop))
            {
                bottomLeft = paperBottom;
                topRight = paperTop;
            }
            Vector2 min, max;
            if (parent == null || !Project(parent, bottomLeft, out min) || !Project(parent, topRight, out max)) return;
            rect.position = parent.TransformPoint((min + max) * 0.5f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, max.x - min.x);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, max.y - min.y);
            label.Text.enableAutoSizing = false;
            label.Text.fontSize = (max.y - min.y) * label.FontRatio;
            label.Text.alignment = TextAlignmentOptions.Center;
            label.Text.color = label.Counter ? new Color32(177, 33, 25, 255)
                : label.Ledger ? new Color32(48, 29, 15, 255) : new Color32(63, 37, 23, 255);
            label.Text.overrideColorTags = true;
            if (label.Ledger || label.Counter) StyleInk(label);
        }

        private static Vector3 World(float x, float row)
        {
            return new Vector3((x - Width / 2f) / 100f, (Height / 2f - row) / 100f, 0f);
        }

        private static bool Project(RectTransform parent, Vector3 world, out Vector2 local)
        {
            local = Vector2.zero;
            Camera camera = Camera.main;
            Canvas canvas = parent.GetComponentInParent<Canvas>();
            if (camera == null || canvas == null) return false;
            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera ?? camera;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, camera.WorldToScreenPoint(world), uiCamera, out local);
        }

        internal static void BeforeValidate()
        {
            foreach (HotspotPin pin in Pins)
                if (pin != null) pin.Restore();
            CassettePin.Restore();
            ShowcaseWindow.Restore();
            AlcoveWindow.Restore();
            DocumentWindow.Restore();
            WasteWindow.Restore();
        }

        internal static void ApplyWindows(EmporiumEntry entry)
        {
            if (sceneRoot == null || entry == null) return;
            if (UnifiedInterior.IsHotspot)
            {
                ApplyHotspotWindows(entry);
                return;
            }
            ShowcaseWindow.Apply(entry.showcaseWindow);
            AlcoveWindow.Apply(entry.hiddenWindow);
            DocumentWindow.Apply(entry.docInvWindow);
            WasteWindow.Apply(entry.trashInvWindow);
            ReplaceItem(entry.docInvElement == null ? null : entry.docInvElement.childItem, archiveArt);
            ReplaceItem(entry.trashcan, wasteArt);
            ReplaceItem(entry.cassettePlayer, tunesArt);
            if (entry.invWindow == null || entry.invElement == null || frameSprite == null) return;
            RectTransform parent = entry.invWindow.rectTransform;
            if (stockFrame == null || stockFrame.parent != parent)
            {
                if (stockFrame != null) UnityEngine.Object.Destroy(stockFrame.gameObject);
                GameObject go = new GameObject("JinSong.StockWoodFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                stockFrame = (RectTransform)go.transform;
                stockFrame.SetParent(parent, false);
                stockFrame.SetAsFirstSibling();
                Image frame = go.GetComponent<Image>();
                frame.raycastTarget = false;
                frame.sprite = frameSprite;
                frame.type = Image.Type.Sliced;
                frame.fillCenter = false;
            }
            RectTransform grid = entry.invElement.rectTransform;
            float width = entry.invElement.widthPixels, height = entry.invElement.heightPixels;
            stockFrame.position = grid.TransformPoint(new Vector3(width / 2, -height / 2, 0f));
            stockFrame.sizeDelta = new Vector2(width + 12, height + 12);
        }

        internal static PixelWindow HotspotWindow(int number)
        {
            EmporiumEntry entry = EmporiumEntry.Instance;
            if (entry == null) return null;
            switch (number)
            {
                case 8: return entry.frontInvWindow;
                case 17: return entry.hiddenWindow;
                case 18: return entry.docInvWindow;
                case 19: return entry.showcaseWindow;
                case 21: return entry.trashInvWindow;
                case 22: return entry.faucetWindow;
                case 23: return entry.drainInvWindow;
                case 24: return entry.backInvWindow;
                default: return null;
            }
        }

        private static void ApplyHotspotWindows(EmporiumEntry entry)
        {
            GameGridInventory grid = entry.invElement;
            if (grid != null)
                Pins[0].Apply(entry.invWindow, grid.rectTransform, GridBounds(grid),
                    ShopHotspots.ToScene(new RectInt(485, 673, 723, 248)), true);
            MapUIManager map = MapUIManager.Instance;
            if (map != null && map.Current is DrJacksonLocation)
            {
                // Jackson reuses the trade windows with his own board and counter positions.
                Pins[7].Restore();
                Pins[23].Restore();
            }
            else
            {
                PinGrid(8, entry.frontInvWindow, entry.frontInvinvElement);
                PinGrid(24, entry.backInvWindow, entry.backInvinvElement);
            }
            if (entry.hiddenElement != null)
            {
                RectInt cavity = ShopHotspots.Zone(17);
                RectInt storage = new RectInt(cavity.x + 8, cavity.y + 18, cavity.width - 16, cavity.height - 28);
                Pins[16].Apply(entry.hiddenWindow, entry.hiddenElement.rectTransform, GridBounds(entry.hiddenElement), storage, true,
                    grid == null ? null : grid.rectTransform);
                EnsureAlcoveClose(entry, cavity);
            }
            PinGrid(19, entry.showcaseWindow, entry.showcaseElement);
            PinItem(18, entry.docInvWindow, entry.docInvElement == null ? null : entry.docInvElement.childItem);
            PinItem(21, entry.trashInvWindow, entry.trashcan);
            PinItem(22, entry.faucetWindow, entry.faucet);
            PinItem(23, entry.drainInvWindow, entry.drain);
            GameItemElement player = entry.cassettePlayer as GameItemElement;
            if (tunesArt != null && player != null && player.background != null && player.background.image != null)
            {
                RectTransform area = player.background.image.rectTransform;
                CassettePin.Apply(entry.cassettePlayerWindow, area, area.rect,
                    ShopHotspots.ToScene(new RectInt(1550, 544, 105, 55)), false);
                ReplaceItem(player, tunesArt);
            }
        }

        private static void EnsureAlcoveClose(EmporiumEntry entry, RectInt cavity)
        {
            if (entry.hiddenWindow == null || entry.hiddenWindow.rectTransform == null) return;
            RectTransform host = entry.hiddenWindow.rectTransform;
            if (alcoveClose == null)
            {
                alcoveClose = new GameObject("JinSong.AlcoveClose", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                alcoveClose.transform.SetParent(host, false);
                Image image = alcoveClose.GetComponent<Image>();
                image.color = new Color32(145, 104, 59, 255);
                Button close = alcoveClose.GetComponent<Button>();
                close.targetGraphic = image;
                close.navigation = new Navigation { mode = Navigation.Mode.None };
                close.onClick.AddListener(delegate { if (StorePoster.Instance != null && StorePoster.Instance.isPulled) StorePoster.Instance.OnClick(); });
                GameObject label = new GameObject("Caption", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                label.transform.SetParent(alcoveClose.transform, false);
                RectTransform r = (RectTransform)label.transform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
                Text text = label.GetComponent<Text>();
                text.font = ledgerFont;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color32(244, 225, 178, 255);
                text.raycastTarget = false;
                text.resizeTextForBestFit = true; text.resizeTextMinSize = 5; text.resizeTextMaxSize = 14;
            }
            RectTransform rect = (RectTransform)alcoveClose.transform;
            Vector2 a, b;
            if (Project(host, World(cavity.x + 4, cavity.y + 3), out a)
                && Project(host, World(cavity.xMax - 4, cavity.y + 15), out b))
            {
                rect.anchorMin = rect.anchorMax = host.pivot;
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = (a + b) * .5f;
                rect.sizeDelta = new Vector2(Math.Abs(b.x - a.x), Math.Abs(b.y - a.y));
            }
            alcoveClose.transform.SetAsLastSibling();
            Text caption = alcoveClose.GetComponentInChildren<Text>();
            caption.font = ledgerFont;
            caption.text = Mod.Chinese ? "合上帘画" : "Close panel";
        }

        private static void PinGrid(int number, PixelWindow window, GameGridInventory grid)
        {
            if (grid != null) Pins[number - 1].Apply(window, grid.rectTransform, GridBounds(grid), ShopHotspots.Zone(number), true);
        }

        private static Rect GridBounds(GameGridInventory grid)
        {
            return new Rect(0f, -grid.heightPixels, grid.widthPixels, grid.heightPixels);
        }

        private static void PinItem(int number, PixelWindow window, GameItem item)
        {
            GameItemElement view = item as GameItemElement;
            if (view == null || view.background == null || view.background.image == null) return;
            RectTransform area = view.background.image.rectTransform;
            Pins[number - 1].Apply(window, area, area.rect, ShopHotspots.Zone(number), false);
            ReplaceItem(item, blankArt);
            if (blankArt != null && (number == 18 || number == 21))
            {
                // The archive and waste basket are painted into the room. Their
                // transparent native item views still need to receive clicks.
                Image image = view.background.image;
                if (!ItemHitThresholds.ContainsKey(image)) ItemHitThresholds.Add(image, image.alphaHitTestMinimumThreshold);
                image.alphaHitTestMinimumThreshold = 0f;
            }
        }

        internal static void TintStockGrid(GameGridInventory grid, Image background, Image lines)
        {
            EmporiumEntry entry = EmporiumEntry.Instance;
            if (sceneRoot == null || entry == null || background == null || lines == null) return;
            bool trade = UnifiedInterior.IsHotspot && (grid == entry.frontInvinvElement || grid == entry.backInvinvElement);
            if (!trade && grid != entry.invElement && grid != entry.showcaseElement) return;
            bool niche = UnifiedInterior.HasLeftShowcase && grid == entry.showcaseElement;
            if (!GridColors.ContainsKey(background)) GridColors.Add(background, background.color);
            if (!GridColors.ContainsKey(lines)) GridColors.Add(lines, lines.color);
            background.color = new Color(1f, 0.83f, 0.63f, trade ? 0.17f : niche ? 0.14f : GridColors[background].a);
            lines.color = new Color(1f, 0.91f, 0.74f, trade || niche ? 0.38f : GridColors[lines].a);
        }

        internal static void ShowQueue(StoreClientCounter counter)
        {
            if (queueLabel == null || counter.clientCounterTMP != queueLabel) return;
            if (UnifiedInterior.HasQueuePaper)
            {
                if (String.IsNullOrEmpty(queueLabel.text)) queueLabel.text = "0";
                return;
            }
            if (UnifiedInterior.IsHotspot || !Mod.Chinese) return;
            int number;
            if (Int32.TryParse(queueLabel.text, out number)) queueLabel.text = "候客 " + number;
        }

        internal static void Restore()
        {
            BeforeValidate();
            ShopMenuSkin.Restore();
            ShopHotspots.Restore();
            Array.Clear(Pins, 0, Pins.Length);
            foreach (KeyValuePair<SpriteRenderer, bool> mask in BakedArt)
                if (mask.Key != null) mask.Key.enabled = mask.Value;
            BakedArt.Clear();
            foreach (KeyValuePair<Interactable, bool> flag in OutlineFlags)
                if (flag.Key != null) flag.Key.doNotOutline = flag.Value;
            OutlineFlags.Clear();
            foreach (KeyValuePair<EPOOutline.Outlinable, bool> outline in NativeOutlines)
                if (outline.Key != null) outline.Key.enabled = outline.Value;
            NativeOutlines.Clear();
            if (memo != null) UnityEngine.Object.Destroy(memo);
            if (liftedBanner != null) UnityEngine.Object.Destroy(liftedBanner.gameObject);
            if (cityEmblem != null) UnityEngine.Object.Destroy(cityEmblem.gameObject);
            cityEmblem = null;
            if (alcoveClose != null) UnityEngine.Object.Destroy(alcoveClose);
            alcoveClose = null;
            memo = null;
            if (hiddenClientele != null) hiddenClientele.SetActive(true);
            hiddenClientele = null;
            liftedBanner = null;
            blankArt = null;
            foreach (Fixture fixture in Fixtures)
            {
                if (fixture.Renderer != null && fixture.Renderer.sprite == fixture.Applied)
                {
                    fixture.Renderer.sprite = fixture.Original;
                    if (fixture.AccentNativeFrames) fixture.Renderer.color = fixture.BaseColor;
                }
                for (int i = 0; i < fixture.Hitboxes.Length; i++)
                {
                    if (fixture.Hitboxes[i] == null) continue;
                    fixture.Hitboxes[i].offset = fixture.Offsets[i];
                    fixture.Hitboxes[i].size = fixture.Sizes[i];
                    fixture.Hitboxes[i].enabled = fixture.Enabled[i];
                }
            }
            Fixtures.Clear();
            foreach (SpriteRenderer renderer in Hidden)
                if (renderer != null) renderer.enabled = true;
            Hidden.Clear();
            foreach (TextSlot label in Labels)
            {
                if (label.Text == null) continue;
                label.Text.rectTransform.anchoredPosition = label.OriginalAnchor;
                label.Text.rectTransform.sizeDelta = label.OriginalSize;
                label.Text.fontSize = label.OriginalFont;
                label.Text.color = label.OriginalColor;
                label.Text.alignment = label.OriginalAlignment;
                label.Text.enableAutoSizing = label.OriginalAutoSize;
                label.Text.overrideColorTags = label.OriginalOverride;
                if (!label.Ledger && !label.Counter) continue;
                if (label.InkText != null) UnityEngine.Object.Destroy(label.InkText.gameObject);
                if (label.Currency != null) UnityEngine.Object.Destroy(label.Currency.gameObject);
                label.Text.enabled = label.OriginalEnabled;
                label.Text.fontStyle = label.OriginalStyle;
            }
            Labels.Clear();
            if (ledgerFont != null) UnityEngine.Object.Destroy(ledgerFont);
            if (queueFont != null) UnityEngine.Object.Destroy(queueFont);
            ledgerFont = queueFont = null;
            ledgerFontChecked = queueFontChecked = false;
            queueLabel = null;
            foreach (KeyValuePair<Image, Sprite> art in ItemArt)
                if (art.Key != null && Sprites.Contains(art.Key.overrideSprite))
                    art.Key.overrideSprite = art.Value;
            ItemArt.Clear();
            foreach (var entry in ItemHitThresholds)
                if (entry.Key != null && entry.Key.alphaHitTestMinimumThreshold == 0f)
                    entry.Key.alphaHitTestMinimumThreshold = entry.Value;
            ItemHitThresholds.Clear();
            archiveArt = wasteArt = tunesArt = null;
            foreach (KeyValuePair<Transform, Vector3> move in Moved)
                if (move.Key != null) move.Key.localPosition = move.Value;
            Moved.Clear();
            foreach (KeyValuePair<SpriteRenderer, Color> tint in SceneColors)
                if (tint.Key != null) tint.Key.color = tint.Value;
            SceneColors.Clear();
            foreach (KeyValuePair<Image, Color> tint in GridColors)
                if (tint.Key != null) tint.Key.color = tint.Value;
            GridColors.Clear();
            if (stockFrame != null) UnityEngine.Object.Destroy(stockFrame.gameObject);
            stockFrame = null;
            foreach (Sprite sprite in Sprites)
            {
                UnityEngine.Object.Destroy(sprite.texture);
                UnityEngine.Object.Destroy(sprite);
            }
            Sprites.Clear();
            frameSprite = null;
            sceneRoot = null;
        }
    }

    [HarmonyPatch(typeof(EmporiumEntry), "Validate")]
    internal static class ConceptWindowPlacement
    {
        private static void Prefix()
        {
            if (Mod.Active && UnifiedInterior.IsConcept) ConceptFixtures.BeforeValidate();
        }

        private static void Postfix(EmporiumEntry __instance)
        {
            if (Mod.Active && UnifiedInterior.IsConcept && SceneManager.GetActiveScene().buildIndex == 1)
                ConceptFixtures.ApplyWindows(__instance);
        }
    }

    [HarmonyPatch(typeof(GameGridInventory), "Validate")]
    internal static class ConceptStockPalette
    {
        private static void Postfix(GameGridInventory __instance, TreeNodeRender ___background, TreeNodeRender ___insideBorder)
        {
            if (Mod.Active && UnifiedInterior.IsConcept && ___background != null && ___insideBorder != null
                && !ShopMenuSkin.PaintGrid(__instance, ___background.image, ___insideBorder.image))
                ConceptFixtures.TintStockGrid(__instance, ___background.image, ___insideBorder.image);
        }
    }

    [HarmonyPatch(typeof(TreeNodeRender), "SetParent", typeof(TreeNodeRender), typeof(bool))]
    internal static class ConceptItemPlacement
    {
        private static void Postfix(TreeNodeRender __instance, TreeNodeRender __0, bool __1)
        {
            if (!Mod.Active || !UnifiedInterior.IsHotspot || __1 || __0 == null) return;
            GameItemElement item = __instance.element as GameItemElement;
            PixelElement inventory = item == null ? null : item.parentInventory as PixelElement;
            // The drag's temporary parent preserves screen scale. A placed item inherits its new grid's scale.
            if (inventory != null && item.handler == __instance && inventory.inner == __0)
                __instance.rectTransform.localScale = Vector3.one;
        }
    }

    [HarmonyPatch(typeof(GameItemElement), "ToggleFakeBackground", typeof(bool), typeof(Vector4))]
    internal static class ConceptDragPreview
    {
        private static void Postfix(GameItemElement __instance, bool __0)
        {
            if (!Mod.Active || !UnifiedInterior.IsHotspot || __0) return;
            // The preview rejoins the item after a drop or cancellation. Clear the previous window's scale.
            __instance.fakeMiddle.rectTransform.localScale = Vector3.one;
        }
    }

    [HarmonyPatch(typeof(StoreClientCounter), "Update")]
    internal static class ConceptQueueLabel
    {
        private static void Postfix(StoreClientCounter __instance)
        {
            if (Mod.Active && UnifiedInterior.IsConcept) ConceptFixtures.ShowQueue(__instance);
        }
    }
}
