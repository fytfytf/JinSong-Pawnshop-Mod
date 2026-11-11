using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JinSongPoc
{
    // Presentation overrides retain native identities. New story content uses its own IDs.
    public sealed class Mod : IMod
    {
        public const string Id = "jinsong_poc";
        internal static bool Active;
        internal static LoadedMod Assets;
        internal static ModLog Log;
        private GameObject host;

        public void Init(ModManifest manifest) { Log = new ModLog(manifest); }
        public void OnEnable()
        {
            Active = true;
            ModHook.OnModAssetsLoaded += AssetsLoaded;
            ModHook.OnGameLoadedLate += GameLoaded;
            ModHook.OnGenerateCustomerVeryLate += AssassinStory.Queue;
            ModHook.OnGenerateCustomerVeryLate += CityStory.Queue;
            ModHook.OnWakingUpLate += AssassinStory.PublishNews;
            ModHook.OnWakingUpEarly += CityStory.OnMorning;
            SceneManager.sceneLoaded += SceneLoaded;
            host = new GameObject("JinSongPoc.Runtime");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<PresentationDriver>();
            Log.Log("POC enabled; independent story content registered.");
        }
        public void OnDisable()
        {
            Active = false;
            ModHook.OnModAssetsLoaded -= AssetsLoaded;
            ModHook.OnGameLoadedLate -= GameLoaded;
            ModHook.OnGenerateCustomerVeryLate -= AssassinStory.Queue;
            ModHook.OnGenerateCustomerVeryLate -= CityStory.Queue;
            ModHook.OnWakingUpLate -= AssassinStory.PublishNews;
            ModHook.OnWakingUpEarly -= CityStory.OnMorning;
            SceneManager.sceneLoaded -= SceneLoaded;
            ShopAudio.Restore();
            DirectShopAudio.Restore();
            RemainderItemText.Clear();
            NewsPresentation.Clear();
            SerializedLabels.Restore();
            ReportedUiSkin.Restore();
            CommonUiArt.Restore();
            SceneActionArt.Restore();
            CityEndingPresentation.Dispose();
            MapCards.Dispose();
            LocationArt.Restore();
            FixtureText.RestoreGuideTip();
            PaperText.Restore();
            MenuBackdrop.Restore();
            Visuals.Restore();
            Correspondence.Restore();
            InteriorArt.RestoreAndDispose();
            if (host != null) UnityEngine.Object.Destroy(host);
            Visuals.Dispose();
            ShopGoods.Dispose();
            NpcNames.Clear();
            Assets = null;
        }
        private static void AssetsLoaded(LoadedMod mod)
        {
            if (mod.Manifest.ID != Id) return;
            Assets = mod;
            Visuals.Load(mod);
            GameLoaded();
        }
        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single && scene.buildIndex != 0) MenuBackdrop.Restore();
            Visuals.Apply();
            if (scene.buildIndex == 0) CommonUiArt.Refresh(true);
            LocationArt.Apply();
            ShopMenuSkin.RefreshGuideImages();
            ShopAudio.Apply();
        }
        private static void GameLoaded()
        {
            ShopAudio.Apply();
            if (SceneManager.GetActiveScene().buildIndex == 1) DialoguePresentation.BindCurrent();
            Visuals.Apply();
            LocationArt.Apply();
            CommonUiArt.Refresh(true);
        }
        internal static bool Chinese
        {
            get { return Active && Assets != null && LocHelper.GetCurrentLocaleCode() == "zh"; }
        }
        internal static string Text(string key, params object[] args)
        {
            Dictionary<string, string> chinese;
            if (Assets == null || !Assets.Localization.TryGetValue("zh", out chinese) || !chinese.ContainsKey(key)) return null;
            return ModHelper.GetLocalized(Id, key, args);
        }
        internal static string CurrentText(string key, params object[] args)
        {
            if (!Active || Assets == null) return null;
            string locale = LocHelper.GetCurrentLocaleCode();
            Dictionary<string, string> entries;
            if (!Assets.Localization.TryGetValue(locale, out entries) || !entries.ContainsKey(key)) return null;
            return ModHelper.GetLocalized(Id, key, args);
        }
        internal static bool Target(StoreClient c)
        {
            return c != null && c.identifier == "securityOfficer" && !c.isWanted
                && String.IsNullOrEmpty(c.wantedPosterId);
        }
        internal static string WantedKey(StoreClient c)
        {
            if (c == null || !c.isWanted) return null;
            foreach (string key in new string[] { "wanted1", "wanted2" })
                if (c.wantedPosterId == key && c.spriteName == key
                    && (c.identifier == key || c.identifier == key + "_return")) return key;
            return null;
        }
    }

    public sealed class PresentationDriver : MonoBehaviour
    {
        private float next;
        private void Start()
        {
            if (!Mod.Active || SceneManager.GetActiveScene().buildIndex != 0) return;
            MenuBackdrop.Apply(Mod.Assets);
            CommonUiArt.Refresh(true);
        }

        private void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 0.25f;
            if (Mod.Active)
            {
                Visuals.Apply(); MapCards.Refresh(); LocationArt.Apply(); SerializedLabels.Refresh(); FixtureText.RefreshGuideTip(); ShopMenuSkin.RefreshGuideImages(); PaperText.Refresh(); CommonUiArt.Refresh(); ReportedUiSkin.Refresh(); NpcNames.RefreshSpeakerLabels(); SceneActionArt.RefreshMenu();
                if (SceneManager.GetActiveScene().buildIndex == 1) ConceptFixtures.RefreshCityEmblem();
            }
        }
        private void LateUpdate()
        {
            if (Mod.Active && SceneManager.GetActiveScene().buildIndex == 1)
            {
                InteriorArt.ApplyAnimated(Mod.Assets);
                if (UnifiedInterior.IsConcept) ConceptFixtures.RefreshSprites();
                if (UnifiedInterior.IsHotspot)
                {
                    ConceptFixtures.ApplyWindows(EmporiumEntry.Instance);
                    ShopHotspots.UpdateHover();
                }
                SceneActionArt.Refresh();
            }
        }
    }

    internal static class Visuals
    {
        private static Sprite official, street;
        private static readonly Dictionary<string, Sprite> wanted = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        private static readonly List<Texture2D> portraitTextures = new List<Texture2D>();
        private static readonly List<PosterView> posters = new List<PosterView>();
        private static Texture2D officialTexture, streetTexture;
        private static SpriteRenderer clientRenderer, backgroundRenderer;
        private static Sprite clientOriginal, clientApplied, backgroundOriginal;
        private static StoreClient visualClient;

        internal static void Load(LoadedMod assets)
        {
            Texture2D source;
            if (assets.Textures.TryGetValue("jin_official", out source))
                official = MakeSprite(source, 288, 211, "official", out officialTexture);
            else Mod.Log.Warning("Missing jin_official; original portrait retained.");
            if (assets.Textures.TryGetValue("jin_street", out source))
                street = MakeSprite(source, 960, 540, "street", out streetTexture, frameStreet: true);
            else Mod.Log.Warning("Missing jin_street; original background retained.");
            foreach (string key in new string[] { "wanted1", "wanted2" })
            {
                Texture2D texture;
                if (assets.Textures.TryGetValue(key == "wanted1" ? "song_agent" : "song_militia", out source))
                {
                    wanted[key] = MakeSprite(source, 288, 211, key, out texture);
                    portraitTextures.Add(texture);
                }
                if (assets.Textures.TryGetValue(key + "_portrait", out source))
                {
                    icons[key] = MakeSprite(source, 340, 320, key + ".portrait", out texture, grayscale: true);
                    portraitTextures.Add(texture);
                }
            }
            OrdinaryPortraits.Load(assets, portraitTextures);
            RosterPortraits.Load(assets, portraitTextures);
            foreach (string key in RosterPortraits.WantedKeys)
            {
                if (!assets.Textures.TryGetValue("jin_npc_" + key, out source)) continue;
                Texture2D texture;
                wanted[key] = MakeSprite(source, 288, 211, key, out texture, figureScale: .90f);
                portraitTextures.Add(texture);
                icons[key] = MakeSprite(source, 340, 320, key + ".portrait", out texture, grayscale: true,
                    sourceArea: new Rect(.20f, .30f, .60f, .70f));
                portraitTextures.Add(texture);
            }
            Mod.Log.Log("AI assets ready. Runtime samples: NPC 288x211, wanted portraits 340x320, background 960x540.");
        }

        internal static Sprite MakeSprite(Texture2D source, int width, int height, string name, out Texture2D texture,
            bool frameStreet = false, bool grayscale = false, float figureScale = 1f, Rect? sourceArea = null)
        {
            Color32[] input = source.GetPixels32();
            Color32[] output = new Color32[width * height];
            Rect area = sourceArea ?? new Rect(0f, 0f, 1f, 1f);
            for (int y = 0; y < height; y++)
            {
                double v = (y + 0.5) / height / figureScale;
                if (v >= 1.0) break;
                // The visible opening occupies x=394..568, y=24..172 from the top of store_mid.
                // Its street view spans x=600..1456, y=207..935 in the 1672x941 source art.
                // Unity texture rows run from bottom to top.
                if (frameStreet) v = ReframeAxis(v, 368.0 / 540, 516.0 / 540, 6.0 / 941, 734.0 / 941);
                int sy = Math.Min(source.height - 1, (int)((area.y + v * area.height) * source.height));
                for (int x = 0; x < width; x++)
                {
                    double u = (x + 0.5) / width;
                    if (figureScale != 1f) u = (u - 0.5) / figureScale + 0.5;
                    if (u < 0.0 || u >= 1.0) continue;
                    if (frameStreet) u = ReframeAxis(u, 394.0 / 960, 568.0 / 960, 600.0 / 1672, 1456.0 / 1672);
                    int sx = Math.Min(source.width - 1, (int)((area.x + u * area.width) * source.width));
                    Color32 pixel = input[sy * source.width + sx];
                    if (grayscale)
                    {
                        byte gray = (byte)((299 * pixel.r + 587 * pixel.g + 114 * pixel.b + 500) / 1000);
                        pixel = new Color32(gray, gray, gray, pixel.a);
                    }
                    output[y * width + x] = pixel;
                }
            }
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "jinsong_poc." + name;
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(output);
            texture.Apply(false, false);
            UnityEngine.Object.DontDestroyOnLoad(texture);
            Sprite result = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            result.name = "jinsong_poc." + name;
            UnityEngine.Object.DontDestroyOnLoad(result);
            return result;
        }
        private static double ReframeAxis(double point, double windowStart, double windowEnd, double artStart, double artEnd)
        {
            // Extend each edge of the framing to the remaining source instead of repeating border pixels.
            if (point < windowStart) return artStart * point / windowStart;
            if (point > windowEnd) return artEnd + (1 - artEnd) * (point - windowEnd) / (1 - windowEnd);
            return artStart + (artEnd - artStart) * (point - windowStart) / (windowEnd - windowStart);
        }
        internal static void Apply()
        {
            if (!Mod.Active) return;
            int scene = SceneManager.GetActiveScene().buildIndex;
            if (scene == 0) MenuBackdrop.Apply(Mod.Assets);
            if (scene != 1) return;
            ApplyPortrait();
            ApplyPosters();
            if (InteriorArt.Apply(Mod.Assets) || street == null) return;
            if (backgroundRenderer == null)
            {
                GameObject go = GameObject.Find("store_mid");
                if (go == null || go.scene.buildIndex != 1) return;
                SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
                if (renderer == null || renderer.sprite == null || renderer.sprite.name != "store_mid") return;
                backgroundRenderer = renderer;
                backgroundOriginal = renderer.sprite;
                renderer.sprite = street;
                Mod.Log.Log("Background bound: store_mid (visible scenery layer); transform, material, sorting and interactions retained.");
            }
            // If another mod replaces this layer later, do not overwrite its choice every frame.
        }
        internal static void ApplyPortrait()
        {
            if (!Mod.Active || SceneManager.GetActiveScene().buildIndex != 1) return;
            PlayerStore store = PlayerStore.Instance;
            StoreClientInstance instance = store == null ? null : store.currentClientInstance;
            StoreClient c = instance == null ? null : instance.GetClientBlueprint();
            StoreClientMono mono = StoreClientMono.Instance;
            if (mono == null || mono.image == null) return;
            SpriteRenderer renderer = mono.image;
            // DismissCurrentClient clears the blueprint before the renderer finishes the exit animation.
            if (instance == null && clientApplied != null && renderer == clientRenderer
                && renderer.sprite == clientApplied && renderer.enabled && renderer.gameObject.activeInHierarchy) return;
            Sprite replacement;
            string wantedKey = Mod.WantedKey(c) ?? RosterPortraits.WantedKey(c);
            if (wantedKey != null) wanted.TryGetValue(wantedKey, out replacement);
            else if (Mod.Target(c) || (c != null && c.identifier == AssassinStory.NoticeClient)) replacement = official;
            else replacement = OrdinaryPortraits.Get(c) ?? RosterPortraits.Get(c, official);
            if (replacement == null)
            {
                RestoreClient();
                return;
            }
            if (visualClient != c || clientRenderer != renderer)
            {
                RestoreClient();
                visualClient = c;
                clientRenderer = renderer;
                clientOriginal = renderer.sprite;
            }
            else if (renderer.sprite != clientOriginal && renderer.sprite != clientApplied
                && !String.IsNullOrEmpty(c.spriteName) && SpriteDict.Instance != null)
            {
                Sprite stock;
                if (SpriteDict.Instance.spriteDictionary.TryGetValue(c.spriteName, out stock) && renderer.sprite == stock)
                    clientOriginal = stock;
            }
            if (renderer.sprite == clientOriginal || renderer.sprite == clientApplied)
            {
                renderer.sprite = replacement;
                clientApplied = replacement;
            }
        }
        private static void RestoreClient()
        {
            if (clientRenderer != null && clientRenderer.sprite == clientApplied) clientRenderer.sprite = clientOriginal;
            clientRenderer = null;
            clientOriginal = null;
            clientApplied = null;
            visualClient = null;
        }
        private sealed class PosterView
        {
            internal WantedElement Element;
            internal GameObject Cover;
            internal TMPro.TMP_Text Name;
            internal string OriginalNameKey, OriginalNameValue, AppliedName;
        }
        private static void ApplyPosters()
        {
            foreach (WantedElement element in Resources.FindObjectsOfTypeAll<WantedElement>())
            {
                if (element.gameObject.scene.buildIndex != 1 || !element.isActiveAndEnabled) continue;
                Sprite icon;
                if (!icons.TryGetValue(element.identifier, out icon)) continue;
                PosterView view = posters.Find(delegate(PosterView p) { return p.Element == element; });
                if (view == null)
                {
                    Transform name = element.transform.Find("Name");
                    if (name == null) continue;
                    TMPro.TMP_Text text = name.GetComponent<TMPro.TMP_Text>();
                    if (text == null) continue;
                    GameObject cover = MakePosterCover(element, icon.texture);
                    if (cover == null) continue;
                    string originalName = element.identifier == "wanted1" ? "name_james_bailey"
                        : element.identifier == "wanted2" ? "name_the_butcher" : null;
                    view = new PosterView { Element = element, Cover = cover, Name = text, OriginalNameKey = originalName, OriginalNameValue = text.text };
                    posters.Add(view);
                }
                if (view.Name == null) continue;
                if (view.OriginalNameKey == null)
                {
                    string native = view.Name.text == view.AppliedName ? view.OriginalNameValue : view.Name.text;
                    string label = NpcNames.DisplayValue(native);
                    view.OriginalNameValue = native;
                    view.AppliedName = label;
                    view.Name.text = label;
                    continue;
                }
                string translated = Mod.CurrentText(element.identifier + "_name");
                if (translated != null)
                {
                    view.AppliedName = translated;
                    view.Name.text = translated;
                }
                else if (view.Name.text == view.AppliedName)
                    view.Name.text = LocHelper.GetLocalizedClientName(view.OriginalNameKey);
            }
            posters.RemoveAll(delegate(PosterView p) { return p.Element == null; });
        }
        private static GameObject MakePosterCover(WantedElement element, Texture2D texture)
        {
            Transform grid = element.transform.parent;
            if (grid == null || grid.name != "Grid" || grid.parent == null || grid.parent.name != "_WantedPanel") return null;
            Transform panel = grid.parent;
            UnityEngine.UI.Image sheet = panel.GetComponent<UnityEngine.UI.Image>();
            if (sheet == null || sheet.overrideSprite == null || sheet.overrideSprite.name != "wanted"
                || sheet.overrideSprite.rect.width != 666 || sheet.overrideSprite.rect.height != 930) return null;

            // The visible portrait is baked into the sheet. The stock "Portait" child is inactive.
            // Position non-interactive overlays relative to the paper; do not touch its original image.
            int number = element.identifier[6] - '1';
            GameObject cover = new GameObject("JinSong.Poster." + element.identifier, typeof(RectTransform));
            RectTransform root = cover.GetComponent<RectTransform>();
            root.SetParent(panel, false);
            root.SetSiblingIndex(0); // Beneath Grid's name/reward/cross and the paper controls.
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            int x = 62 + number % 3 * 194, y = 148 + number / 3 * 224;
            // Two rectangles leave the pre-existing reward box exposed in the upper right.
            MakePosterRegion(root, texture, x, y, 94, 30, 0);
            MakePosterRegion(root, texture, x, y + 30, 150, 136, 30);
            return cover;
        }
        private static void MakePosterRegion(RectTransform root, Texture2D texture, int x, int y, int w, int h, int portraitY)
        {
            GameObject backing = new GameObject("Backing", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            RectTransform rect = backing.GetComponent<RectTransform>();
            rect.SetParent(root, false);
            rect.anchorMin = new Vector2(x / 666f, 1f - (y + h) / 930f);
            rect.anchorMax = new Vector2((x + w) / 666f, 1f - y / 930f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            UnityEngine.UI.Image fill = backing.GetComponent<UnityEngine.UI.Image>();
            fill.color = new Color32(136, 148, 138, 255);
            fill.raycastTarget = false;

            GameObject art = new GameObject("AI portrait", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            RectTransform artRect = art.GetComponent<RectTransform>();
            artRect.SetParent(rect, false);
            artRect.anchorMin = Vector2.zero; artRect.anchorMax = Vector2.one;
            artRect.offsetMin = artRect.offsetMax = Vector2.zero;
            UnityEngine.UI.RawImage image = art.GetComponent<UnityEngine.UI.RawImage>();
            image.texture = texture;
            // Both pieces share the same center crop so the portrait stays aligned around the reward box.
            float portraitWidth = (150f / 166f) * texture.height / texture.width;
            image.uvRect = new Rect((1 - portraitWidth) * 0.5f, (166f - portraitY - h) / 166f,
                portraitWidth * w / 150f, h / 166f);
            image.raycastTarget = false;
        }
        internal static void Restore()
        {
            RestoreClient();
            if (backgroundRenderer != null && backgroundRenderer.sprite == street)
                backgroundRenderer.sprite = backgroundOriginal;
            backgroundRenderer = null;
            backgroundOriginal = null;
            foreach (PosterView view in posters)
            {
                if (view.Cover != null) UnityEngine.Object.Destroy(view.Cover);
                if (view.Name != null && view.OriginalNameKey != null && view.Name.text == view.AppliedName)
                    view.Name.text = LocHelper.GetLocalizedClientName(view.OriginalNameKey);
                else if (view.Name != null && view.OriginalNameKey == null && view.Name.text == view.AppliedName)
                    view.Name.text = NpcNames.NativeValue(view.OriginalNameValue);
            }
            posters.Clear();
        }
        internal static void Dispose()
        {
            if (official != null) UnityEngine.Object.Destroy(official);
            if (street != null) UnityEngine.Object.Destroy(street);
            if (officialTexture != null) UnityEngine.Object.Destroy(officialTexture);
            if (streetTexture != null) UnityEngine.Object.Destroy(streetTexture);
            official = street = null;
            officialTexture = streetTexture = null;
            foreach (Sprite sprite in wanted.Values) UnityEngine.Object.Destroy(sprite);
            foreach (Sprite sprite in icons.Values) UnityEngine.Object.Destroy(sprite);
            OrdinaryPortraits.Dispose();
            RosterPortraits.Dispose();
            foreach (Texture2D texture in portraitTextures) UnityEngine.Object.Destroy(texture);
            wanted.Clear(); icons.Clear(); portraitTextures.Clear();
        }
    }

    internal sealed class LineBinding
    {
        internal string Key;
        internal object[] Args;
    }
    internal sealed class LineSnapshot
    {
        internal Dialogue Node;
        internal string OriginalPlain, AppliedPlain;
        internal RichTextBuilder OriginalRich, AppliedRich;
        internal bool Applying;
        internal void Restore()
        {
            if (Node != null && (Applying || (ReferenceEquals(Node.text, AppliedRich) && Node.plainText == AppliedPlain)))
            {
                Node.plainText = OriginalPlain;
                Node.text = OriginalRich;
            }
        }
    }
    internal sealed class DisplaySnapshot
    {
        internal readonly List<LineSnapshot> Lines = new List<LineSnapshot>();
        internal StoreClient Client;
        internal string Name, AppliedName;
        internal void Restore()
        {
            foreach (LineSnapshot line in Lines) line.Restore();
            if (Client != null && Client.displayName == AppliedName) Client.displayName = Name;
        }
    }

    internal static class DialoguePresentation
    {
        private static readonly string[] Wanted1Keys = new string[] {
            "dialog_wanted1_1", "dialog_wanted1_2", "dialog_wanted1_3",
            "dialog_wanted1_return_1", "dialog_wanted1_return_2", "dialog_wanted1_return_3",
            "dialog_wanted1_arrest_client", "dialog_wanted1_arrest_sec_1", "dialog_wanted1_arrest_sec_2",
            "dialog_wanted1_plea_1", "dialog_wanted1_plea_2", "dialog_wanted1_plea_3" };
        private static readonly string[] Wanted2Keys = new string[] {
            "dialog_wanted2_1", "dialog_wanted2_2", "dialog_wanted2_3", "dialog_wanted2_return_1",
            "dialog_wanted2_arrest_client", "dialog_wanted2_arrest_sec",
            "dialog_wanted2_warning_1", "dialog_wanted2_warning_2", "dialog_wanted2_warning_3" };
        // Sidecar metadata never enters StoreClient/Dialogue serialization.
        private static readonly ConditionalWeakTable<Dialogue, LineBinding> Bindings = new ConditionalWeakTable<Dialogue, LineBinding>();
        private static void Bind(Dialogue node, string key, params object[] args)
        {
            if (node == null) return;
            Bindings.Remove(node);
            Bindings.Add(node, new LineBinding { Key = key, Args = args });
        }
        internal static void BindIntro(StoreClient client)
        {
            if (Mod.Target(client)) Bind(client.mainDialogue, "fine_intro");
        }
        internal static void BindCurrent()
        {
            StoreClientInstance instance = PlayerStore.Instance.currentClientInstance;
            if (instance == null || !Mod.Target(instance.GetClientBlueprint())) return;
            BindIntro(instance.GetClientBlueprint());
            BindFine(instance.GetClientBlueprint());
        }
        internal static void BindFine(StoreClient client)
        {
            if (!Mod.Active || !Mod.Target(client) || client.mainDialogue == null) return;
            Dialogue root = client.mainDialogue;
            Dialogue crime = root.nextDialogue;
            Dialogue amount = crime == null ? null : crime.nextDialogue;
            Dialogue threat = amount == null ? null : amount.nextDialogue;
            if (threat == null || threat.choices == null || threat.choices.Count < 1 || threat.choices.Count > 2) return;
            int fine = PlayerStore.Instance.secData.GetFineValue();
            bool hasPay = false, hasRefuse = false;
            // Validate the whole graph before adding bindings. Newer builds keep both choices visible
            // and evaluate their original enabled conditions at render/click time.
            foreach (var entry in threat.choices)
            {
                Dialogue choice = entry.Item2;
                if (choice == null || choice.nextDialogue == null) return;
                if (choice.plainText == LocHelper.GetLocalizedDialogue("dialog_sec_fine_choice_pay", fine))
                {
                    if (hasPay || choice.nextDialogue.plainText != LocHelper.GetLocalizedDialogue("dialog_sec_fine_pay_1")
                        || choice.nextDialogue.nextDialogue != null) return;
                    hasPay = true;
                }
                else if (choice.plainText == LocHelper.GetLocalizedDialogue("dialog_sec_fine_choice_refuse"))
                {
                    Dialogue last = choice.nextDialogue.nextDialogue;
                    if (hasRefuse || choice.nextDialogue.plainText != LocHelper.GetLocalizedDialogue("dialog_sec_fine_refuse_1")
                        || last == null || last.plainText != LocHelper.GetLocalizedDialogue("dialog_sec_fine_refuse_2")
                        || last.nextDialogue != null) return;
                    hasRefuse = true;
                }
                else return;
            }
            if (threat.choices.Count == 2 && !(hasPay && hasRefuse)) return;
            Bind(root, "fine_intro");
            Bind(crime, "fine_crime", PlayerStore.Instance.secData.GetBiggestCrime());
            Bind(amount, "fine_amount", fine);
            Bind(threat, "fine_threat");
            foreach (var entry in threat.choices)
            {
                Dialogue choice = entry.Item2;
                bool pay = choice.plainText == LocHelper.GetLocalizedDialogue("dialog_sec_fine_choice_pay", fine);
                Bind(choice, pay ? "fine_choice_pay" : "fine_choice_refuse", fine);
                Bind(choice.nextDialogue, pay ? "fine_pay_1" : "fine_refuse_1");
                if (!pay) Bind(choice.nextDialogue.nextDialogue, "fine_refuse_2");
            }
            Mod.Log.Log("Bound original fine dialogue view; choices=" + threat.choices.Count + "; graph/callbacks unchanged.");
        }
        private static void Add(DisplaySnapshot state, Dialogue node)
        {
            LineBinding binding;
            if (node == null || !Bindings.TryGetValue(node, out binding)) return;
            string translated = Mod.Text(binding.Key, binding.Args);
            if (translated == null) return; // Missing translation keeps the original line.
            LineSnapshot line = new LineSnapshot { Node = node, OriginalPlain = node.plainText, OriginalRich = node.text };
            state.Lines.Add(line);
            // Only these two text fields are changed, and only for this synchronous render call.
            line.Applying = true;
            node.SetText(node.title, translated);
            line.AppliedPlain = node.plainText;
            line.AppliedRich = node.text;
            line.Applying = false;
        }
        internal static void Begin(DisplaySnapshot state, Dialogue node, bool choices)
        {
            if (!Mod.Chinese || SceneManager.GetActiveScene().buildIndex != 1) return;
            StoreClientInstance instance = PlayerStore.Instance.currentClientInstance;
            if (instance == null) return;
            StoreClient client = instance.GetClientBlueprint();
            string wantedKey = Mod.WantedKey(client);
            if (!Mod.Target(client) && wantedKey == null) return;
            if (wantedKey != null && node != null)
            {
                foreach (string key in wantedKey == "wanted1" ? Wanted1Keys : Wanted2Keys)
                    if (node.plainText == LocHelper.GetLocalizedDialogue(key)) { Bind(node, key); break; }
            }
            if (choices)
            {
                if (node != null && node.choices != null)
                    foreach (var choice in node.choices) Add(state, choice.Item2);
            }
            else
            {
                Add(state, node);
                if (state.Lines.Count != 0)
                {
                    if (wantedKey != null && node.title != "CLIENT") return;
                    state.Client = client;
                    state.Name = state.Client.displayName;
                    state.AppliedName = Mod.Text(wantedKey == null ? "official_name" : wantedKey + "_name");
                    if (state.AppliedName != null) state.Client.displayName = state.AppliedName;
                }
            }
        }
    }

    [HarmonyPatch(typeof(LocHelper), "Get", new Type[] { typeof(string), typeof(string), typeof(object[]) })]
    internal static class LocalizedNpcDialogue
    {
        private static void Postfix(string __0, string __1, object[] __2, ref string __result)
        {
            if (String.IsNullOrEmpty(__1) || !Mod.Active) return;
            __result = FixtureText.LocalizedValue(__0, __1, __2) ?? __result;
            if (__0 == "UI")
            {
                string trade = Mod.CurrentText("trade_UI_" + __1, __2);
                if (trade != null) { __result = trade; return; }
            }
            // ClientName and Item stay on their existing display-only adapters;
            // never localize native constructor/save names through this table hook.
            if (__0 != "ClientName" && __0 != "Item")
            {
                string remainder = Mod.CurrentText("remainder_" + __0 + "_" + __1, __2);
                if (remainder != null) { __result = remainder; return; }
            }
            if (!Mod.Chinese) return;
            if (__0 == "Dialogue") __result = Mod.Text("npc_" + __1, __2) ?? __result;
            else if (__0 == "Mechanic" || __0 == "UI" || __0 == "NewGame" || __0 == "NetworkUpgrade")
                __result = Mod.Text("supplement_" + __0 + "_" + __1, __2) ?? __result;
        }
    }

    [HarmonyPatch(typeof(StoreClientListSecurity), "CreateSecurityOfficer")]
    internal static class BindOfficer
    {
        private static void Postfix(StoreClient __result) { DialoguePresentation.BindIntro(__result); }
    }
    [HarmonyPatch(typeof(StoreClientListSecurity), "GraftFineDialogue")]
    internal static class BindFine
    {
        private static void Postfix(StoreClient __0) { DialoguePresentation.BindFine(__0); }
    }
    [HarmonyPatch(typeof(PlayerStore), "GetNextClient")]
    internal static class OnClient
    {
        private static void Postfix() { DialoguePresentation.BindCurrent(); Visuals.ApplyPortrait(); }
    }
    [HarmonyPatch(typeof(PlayerStore), "SetupWaitingClient")]
    internal static class OnWaitingClient
    {
        private static void Postfix() { Visuals.ApplyPortrait(); }
    }
    [HarmonyPatch(typeof(StoreUIManager), "HideClient")]
    internal static class OnClientHidden
    {
        private static void Postfix() { Visuals.ApplyPortrait(); }
    }
    [HarmonyPatch(typeof(MapUIManager), "ActivateClientOutsideStore")]
    internal static class OnOutsideClient
    {
        private static void Postfix() { LocationArt.Apply(); Visuals.ApplyPortrait(); }
    }
    [HarmonyPatch(typeof(DialogUIManager), "DisplayClientText")]
    internal static class ClientText
    {
        private static void Prefix(Dialogue __0, out DisplaySnapshot __state)
        { __state = new DisplaySnapshot(); DialoguePresentation.Begin(__state, __0, false); }
        private static Exception Finalizer(Exception __exception, DisplaySnapshot __state)
        { if (__state != null) __state.Restore(); return __exception; }
    }
    [HarmonyPatch(typeof(DialogUIManager), "DisplaySecText")]
    internal static class SecText
    {
        private static void Prefix(Dialogue __0, out DisplaySnapshot __state)
        { __state = new DisplaySnapshot(); DialoguePresentation.Begin(__state, __0, false); }
        private static Exception Finalizer(Exception __exception, DisplaySnapshot __state)
        { if (__state != null) __state.Restore(); return __exception; }
    }
    [HarmonyPatch(typeof(DialogUIManager), "DisplayerPlayerText")]
    internal static class PlayerText
    {
        private static void Prefix(Dialogue __0, out DisplaySnapshot __state)
        { __state = new DisplaySnapshot(); DialoguePresentation.Begin(__state, __0, false); }
        private static Exception Finalizer(Exception __exception, DisplaySnapshot __state)
        { if (__state != null) __state.Restore(); return __exception; }
    }
    [HarmonyPatch(typeof(DialogUIManager), "DisplayChoice")]
    internal static class ChoiceText
    {
        private static void Prefix(DialogUIManager __instance, out DisplaySnapshot __state)
        { __state = new DisplaySnapshot(); DialoguePresentation.Begin(__state, __instance.currentDialog, true); }
        private static Exception Finalizer(Exception __exception, DisplaySnapshot __state)
        { if (__state != null) __state.Restore(); return __exception; }
    }
}
