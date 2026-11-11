using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JinSongPoc
{
    internal static class MapCards
    {
        private sealed class CardState
        {
            internal TransitCardView Card;
            internal Image Picture;
            internal GameObject Original;
            internal bool OriginalActive;
            internal TMP_Text Header;
            internal string AppliedTitle, TextKey;
        }
        private static readonly List<CardState> Cards = new List<CardState>();
        private static readonly Dictionary<string, Sprite> Pictures = new Dictionary<string, Sprite>();
        private static string locale;

        private static string Key(AfterhourLocation location)
        {
            if (location == null) return null;
            Type type = location.GetType();
            if (type == typeof(DumpingGroundLocation)) return "dumping_ground";
            if (type == typeof(CommissaryLocation)) return "commissary";
            if (type == typeof(DrJacksonLocation)) return "dr_jackson";
            return null;
        }

        private static Sprite Art(string key)
        {
            Sprite result;
            if (Pictures.TryGetValue(key, out result)) return result;
            Texture2D texture;
            if (Mod.Assets == null || !Mod.Assets.Textures.TryGetValue("jin_map_" + key, out texture) || texture == null) return null;
            result = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
            result.name = "jinsong_poc.map." + key;
            UnityEngine.Object.DontDestroyOnLoad(result);
            Pictures.Add(key, result);
            return result;
        }

        private static bool Foreign(Image picture)
        {
            if (picture == null || picture.overrideSprite == null || ModLoader.Mods == null) return false;
            Texture2D texture = picture.overrideSprite.texture;
            foreach (LoadedMod mod in ModLoader.Mods)
                if (mod != Mod.Assets && mod.Textures != null && mod.Textures.ContainsValue(texture)) return true;
            return false;
        }

        private static void Bind(TransitCardView card)
        {
            if (!Mod.Active || Mod.Assets == null || card == null || Cards.Exists(previous => previous.Card == card)) return;
            string key = Key(card.location);
            if (key == null) return;
            Transform plate = card.transform.Find("Image");
            if (plate == null) return;
            if (plate.Find("JinSongMapPicture") != null) return;
            Transform original = plate.Find("Picture") ?? plate.Find("Placeholder");
            Image originalImage = original == null ? null : original.GetComponent<Image>();
            CardState row = new CardState { Card = card,
                Header = card.transform.Find("Header") == null ? null : card.transform.Find("Header").GetComponent<TMP_Text>(),
                TextKey = key == "dr_jackson" ? "customer_name_dr._jackson" : "map_location_" + key };
            Sprite art = Foreign(originalImage) ? null : Art(key);
            if (art != null)
            {
                GameObject host = new GameObject("JinSongMapPicture", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform rect = host.GetComponent<RectTransform>();
                rect.SetParent(plate, false);
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
                row.Picture = host.GetComponent<Image>();
                row.Picture.sprite = art;
                row.Picture.raycastTarget = false;
                if (original != null)
                {
                    row.Original = original.gameObject;
                    row.OriginalActive = row.Original.activeSelf;
                    row.Original.SetActive(false);
                }
            }
            Cards.Add(row);
            Title(row, true);
        }

        private static void Title(CardState row, bool first)
        {
            if (row.Card == null || row.Header == null || row.Card.location == null) return;
            string stock = row.Card.location.GetTitle();
            if ((first && row.Header.text != stock) || (!first && row.Header.text != row.AppliedTitle)) return;
            row.AppliedTitle = (Mod.Chinese ? Mod.Text(row.TextKey) : null) ?? stock;
            row.Header.text = row.AppliedTitle;
        }

        internal static void Refresh()
        {
            if (!Mod.Active || Mod.Assets == null) return;
            Cards.RemoveAll(row => row.Card == null);
            GameObject root = TransitUI.Root;
            if (root != null)
                foreach (TransitCardView card in root.GetComponentsInChildren<TransitCardView>(true)) Bind(card);
            string current = LocHelper.GetCurrentLocaleCode();
            if (locale == current) return;
            locale = current;
            foreach (CardState row in Cards) Title(row, false);
        }

        internal static void Dispose()
        {
            foreach (CardState row in Cards)
            {
                bool owned = row.Picture == null || Pictures.ContainsValue(row.Picture.overrideSprite);
                if (row.Picture != null && owned)
                {
                    row.Picture.enabled = false;
                    UnityEngine.Object.Destroy(row.Picture.gameObject);
                }
                if (owned && row.Original != null && !row.Original.activeSelf) row.Original.SetActive(row.OriginalActive);
                if (row.Card != null && row.Header != null && row.Header.text == row.AppliedTitle)
                    row.Header.text = row.Card.location.GetTitle();
            }
            Cards.Clear();
            foreach (Sprite sprite in Pictures.Values) if (sprite != null) UnityEngine.Object.Destroy(sprite);
            Pictures.Clear(); locale = null;
        }

        [HarmonyPatch(typeof(TransitUI), "BuildCard")]
        private static class CardBuilt
        { private static void Postfix(TransitCardView __result) { Bind(__result); } }
    }
}
