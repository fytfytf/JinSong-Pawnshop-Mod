using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace JinSongPoc
{
    internal static class CityEndingPresentation
    {
        private static Image bound;
        private static Sprite original, applied, execution, victory;
        private static GameOverUIManager victoryView;
        private static bool oldDarkScreen, oldContinueButton;

        private static Image FindPicture(GameOverUIManager view)
        {
            if (view == null || view.uiPanel == null) return null;
            Transform art = view.uiPanel.transform.Find("Art");
            return art == null ? null : art.GetComponent<Image>();
        }

        private static Sprite GetArt(string key, ref Sprite sprite)
        {
            if (sprite != null) return sprite;
            Texture2D texture;
            if (Mod.Assets == null || !Mod.Assets.Textures.TryGetValue("jin_city_" + key, out texture) || texture == null) return null;
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
            sprite.name = "jinsong_poc.city_" + key;
            return sprite;
        }

        private static void ApplyImage(Image image, Sprite sprite)
        {
            RestoreImage();
            bound = image;
            original = image.overrideSprite == image.sprite ? null : image.overrideSprite;
            applied = sprite;
            image.overrideSprite = sprite;
        }

        internal static void ShowVictory(string title, string body)
        {
            GameOverUIManager view = GameOverUIManager.Instance;
            Image image = FindPicture(view);
            if (image == null || view.uiPanel.activeSelf || view.darkScreen == null || view.continuePlayButton == null
                || view.reason == null || view.description == null || view.rectTransform == null) return;
            Transform button = view.uiPanel.transform.Find("LayoutGroup/Button");
            if (button == null || !button.gameObject.activeSelf) return;
            Sprite art = GetArt("song_victory", ref victory);
            if (art == null) return;
            ApplyImage(image, art);
            oldDarkScreen = view.darkScreen.activeSelf;
            oldContinueButton = view.continuePlayButton.activeSelf;
            victoryView = view;
            view.continuePlayButton.SetActive(false);
            view.reason.text = title;
            view.description.text = body;
            view.darkScreen.SetActive(true);
            view.uiPanel.SetActive(true);
            view.rectTransform.SetAsLastSibling();
        }

        private static void CloseVictory()
        {
            GameOverUIManager view = victoryView;
            victoryView = null;
            if (view == null) return;
            view.uiPanel.SetActive(false);
            view.darkScreen.SetActive(oldDarkScreen);
            view.continuePlayButton.SetActive(oldContinueButton);
            RestoreImage();
        }

        [HarmonyPatch(typeof(GameOverUIManager), "OnMenuButtonClick")]
        private static class ContinueVictory
        {
            private static bool Prefix(GameOverUIManager __instance)
            {
                if (victoryView == null || victoryView != __instance) return true;
                CloseVictory();
                return false;
            }
        }

        [HarmonyPatch(typeof(GameOverUIManager), "DisplayUI", new[] { typeof(string), typeof(string), typeof(bool) })]
        private static class Display
        {
            private static void Prefix() { CloseVictory(); }

            private static void Postfix(GameOverUIManager __instance)
            {
                RestoreImage();
                if (!Mod.Active || !CityStory.ShowingExecution) return;
                Image image = FindPicture(__instance);
                if (image == null) return;
                Sprite art = GetArt("execution", ref execution);
                if (art != null) ApplyImage(image, art);
            }
        }

        private static void RestoreImage()
        {
            if (bound != null && bound.overrideSprite == applied) bound.overrideSprite = original;
            bound = null;
            original = null;
            applied = null;
        }

        internal static void Dispose()
        {
            CloseVictory();
            RestoreImage();
            if (execution != null) Object.Destroy(execution);
            if (victory != null) Object.Destroy(victory);
            execution = null;
            victory = null;
        }
    }
}
