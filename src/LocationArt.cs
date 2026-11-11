using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JinSongPoc
{
    // Only the three native backdrop renderers below public map scene roots.
    internal static class LocationArt
    {
        private sealed class Binding
        {
            internal SpriteRenderer Renderer;
            internal Sprite Original, Applied;
        }
        private static readonly List<Binding> Bound = new List<Binding>();

        internal static void Apply()
        {
            if (!Mod.Active || Mod.Assets == null || MapUIManager.Instance == null) return;
            for (int n = Bound.Count - 1; n >= 0; n--)
                if (Bound[n].Renderer == null)
                {
                    if (Bound[n].Applied != null) Object.Destroy(Bound[n].Applied);
                    Bound.RemoveAt(n);
                }
            MapUIManager map = MapUIManager.Instance;
            Bind(map.commissaryScene, "Commissary", "commissary", "commissary");
            Bind(map.upgradeStoreScene, "inventor_scene", "drjackson2", "dr_jackson");
            Bind(map.scavengingScene, "DumpingGround", "dumping_ground", "dumping_ground");
        }

        private static void Bind(GameObject root, string childName, string nativeName, string key)
        {
            if (root == null || root.scene.buildIndex != 1) return;
            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.gameObject.name != childName || renderer.drawMode != SpriteDrawMode.Simple
                    || Bound.Exists(row => row.Renderer == renderer)) continue;
                Sprite original = renderer.sprite;
                Texture2D art;
                if (original == null || original.name != nativeName || original.packed
                    || !Mod.Assets.Textures.TryGetValue("jin_location_" + key, out art) || art == null
                    || original.rect.width != art.width || original.rect.height != art.height) continue;
                bool foreign = false;
                if (ModLoader.Mods != null)
                    foreach (LoadedMod mod in ModLoader.Mods)
                        if (mod != Mod.Assets && mod.Textures != null && mod.Textures.ContainsValue(original.texture)) foreign = true;
                if (foreign) continue;
                Vector2 pivot = new Vector2(original.pivot.x / original.rect.width, original.pivot.y / original.rect.height);
                Sprite applied = Sprite.Create(art, new Rect(0,0,art.width,art.height), pivot,
                    original.pixelsPerUnit, 0, SpriteMeshType.FullRect, original.border);
                applied.name = "jinsong_poc.location." + key;
                Bound.Add(new Binding { Renderer = renderer, Original = original, Applied = applied });
                renderer.sprite = applied;
            }
        }

        internal static void Restore()
        {
            foreach (Binding row in Bound)
            {
                if (row.Renderer != null && row.Renderer.sprite == row.Applied) row.Renderer.sprite = row.Original;
                if (row.Applied != null) Object.Destroy(row.Applied);
            }
            Bound.Clear();
        }

        [HarmonyPatch(typeof(DrJacksonLocation), "GetLeaveWarning")]
        private static class LeavingWorkshop
        {
            private static void Postfix(ref string __result)
            {
                if (Mod.Chinese && SceneManager.GetActiveScene().buildIndex == 1)
                    __result = Mod.Text("location_jackson_leave_warning") ?? __result;
            }
        }
    }
}
