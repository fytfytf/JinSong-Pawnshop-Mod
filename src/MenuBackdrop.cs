using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JinSongPoc
{
    internal static class MenuBackdrop
    {
        private const float MenuColumn = 0.18f;
        private static SpriteRenderer background;
        private static Sprite original, applied;
        private static Vector3 originalScale;
        private static MenuPosition buttons, logo;
        private static readonly Dictionary<string, string> IconKeys = new Dictionary<string, string>
        {
            { "Button", "new" },
            { "Button (1)", "load" },
            { "Button (2)", "options" },
            { "Button (4)", "credits" },
            { "ButtonMod", "mods" },
            { "Button (3)", "exit" }
        };
        private static readonly List<MenuIcon> Icons = new List<MenuIcon>();

        private struct MenuIcon
        {
            internal GameObject Object;
            internal Sprite Sprite;
        }

        private struct MenuPosition
        {
            internal RectTransform Rect;
            private float min, max, x;

            internal void Move(RectTransform rect)
            {
                Rect = rect;
                min = rect.anchorMin.x;
                max = rect.anchorMax.x;
                x = rect.anchoredPosition.x;
                rect.anchorMin = new Vector2(MenuColumn, rect.anchorMin.y);
                rect.anchorMax = new Vector2(MenuColumn, rect.anchorMax.y);
                rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
            }

            internal void Restore()
            {
                if (Rect != null && Rect.anchorMin.x == MenuColumn && Rect.anchorMax.x == MenuColumn
                    && Rect.anchoredPosition.x == 0f)
                {
                    Rect.anchorMin = new Vector2(min, Rect.anchorMin.y);
                    Rect.anchorMax = new Vector2(max, Rect.anchorMax.y);
                    Rect.anchoredPosition = new Vector2(x, Rect.anchoredPosition.y);
                }
                Rect = null;
            }
        }

        internal static void Apply(LoadedMod assets)
        {
            // The loader populates textures synchronously before its audio coroutine
            // raises OnModAssetsLoaded. The menu can already use those textures.
            if (assets == null && ModLoader.Mods != null)
                assets = ModLoader.Mods.Find(mod => mod.IsEnabled && mod.Manifest.ID == Mod.Id);
            if (assets == null || assets.Textures == null) return;
            if (background == null)
            {
                Restore();
                Texture2D art;
                if (!assets.Textures.TryGetValue("jin_main_menu", out art) || art == null) return;
                GameObject board = GameObject.Find("MenuBackgroundNew");
                GameObject canvas = GameObject.Find("Canvas");
                SpriteRenderer renderer = board == null ? null : board.GetComponent<SpriteRenderer>();
                if (renderer == null || renderer.sprite == null || renderer.sprite.name != "bazaar" || canvas == null) return;
                RectTransform buttonColumn = canvas.transform.Find("LayoutGroup") as RectTransform;
                RectTransform title = canvas.transform.Find("Logo") as RectTransform;
                if (buttonColumn == null || title == null) return;

                original = renderer.sprite;
                originalScale = renderer.transform.localScale;
                art.filterMode = FilterMode.Bilinear;
                applied = Sprite.Create(art, new Rect(0, 0, art.width, art.height), new Vector2(.5f, .5f),
                    art.height / original.bounds.size.y, 0, SpriteMeshType.FullRect);
                applied.name = "jinsong_poc.main_menu";
                background = renderer;
                renderer.sprite = applied;
                buttons.Move(buttonColumn);
                logo.Move(title);
                AddIcons(assets, buttonColumn);
            }
            MenuBrand.Refresh(assets, logo.Rect);
            Camera camera = Camera.main;
            if (background.sprite != applied || camera == null || !camera.orthographic) return;
            Vector3 size = Vector3.Scale(applied.bounds.size, originalScale);
            float height = 2f * camera.orthographicSize;
            float fill = Mathf.Max(1f, Mathf.Max(height / size.y, height * camera.aspect / size.x));
            background.transform.localScale = originalScale * fill;
        }

        private static void AddIcons(LoadedMod assets, RectTransform column)
        {
            foreach (var entry in IconKeys)
            {
                Transform root = column.Find(entry.Key);
                Texture2D art;
                if (root == null || root.GetComponent<Button>() == null
                    || !assets.Textures.TryGetValue("jin_menu_icon_" + entry.Value, out art) || art == null) continue;

                Sprite sprite = Sprite.Create(art, new Rect(0, 0, art.width, art.height), new Vector2(.5f, .5f),
                    100f, 0, SpriteMeshType.FullRect);
                sprite.name = "jinsong_poc.menu_icon." + entry.Value;
                var obj = new GameObject("JinSong.MenuIcon", typeof(RectTransform), typeof(Image));
                RectTransform rect = (RectTransform)obj.transform;
                rect.SetParent(root, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0f, .5f);
                rect.anchoredPosition = new Vector2(48f, 0f);
                rect.sizeDelta = new Vector2(40f, 40f);
                Image image = obj.GetComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;
                image.preserveAspect = true;
                Icons.Add(new MenuIcon { Object = obj, Sprite = sprite });
            }
        }

        internal static void Restore()
        {
            MenuBrand.Restore();
            if (background != null && background.sprite == applied)
            {
                background.sprite = original;
                background.transform.localScale = originalScale;
            }
            buttons.Restore();
            logo.Restore();
            foreach (MenuIcon icon in Icons)
            {
                if (icon.Object != null) UnityEngine.Object.Destroy(icon.Object);
                if (icon.Sprite != null) UnityEngine.Object.Destroy(icon.Sprite);
            }
            Icons.Clear();
            if (applied != null) UnityEngine.Object.Destroy(applied);
            background = null;
            original = applied = null;
        }
    }
}
