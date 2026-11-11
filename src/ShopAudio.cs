using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace JinSongPoc
{
    // Only declared interface/shop sources change. The native audio directory stays intact.
    internal static class ShopAudio
    {
        private sealed class Snapshot
        {
            internal AudioClip Original, Applied;
        }
        private static readonly Dictionary<AudioSource, Snapshot> Changed = new Dictionary<AudioSource, Snapshot>();
        private sealed class Binding
        {
            internal string Id;
            internal Action Apply;
        }
        private static readonly Binding[] Fields = {
            Bind<EmporiumAudioPlayer>(source => source.audioButton1, "effect_button1"),
            Bind<EmporiumAudioPlayer>(source => source.audioButton2, "effect_button2"),
            Bind<EmporiumAudioPlayer>(source => source.audioButton3, "effect_button3"),
            Bind<EmporiumAudioPlayer>(source => source.audioButton4, "effect_button4"),
            Bind<RecorderUIManager>(source => source.openAudio, "effect_button3"),
            Bind<RecorderUIManager>(source => source.closeAudio, "effect_button4"),
            Bind<PaperUIManager>(source => source.audioSourcePickup, "effect_paper_pickup"),
            Bind<PaperUIManager>(source => source.audioSourceDrop, "effect_paper_drop"),
            Bind<NewsUIManager>(source => source.audioSourcePickup, "effect_paper_pickup"),
            Bind<NewsUIManager>(source => source.audioSourceDrop, "effect_paper_drop"),
            Bind<LogUIManager>(source => source.openAudio, "effect_paper_pickup"),
            Bind<LogUIManager>(source => source.closeAudio, "effect_paper_drop"),
            Bind<NotebookUIManager>(source => source.audioSourcePickup, "effect_book_pickup"),
            Bind<NotebookUIManager>(source => source.audioSourceDrop, "effect_book_drop"),
            Bind<StoreDismissButton>(source => source.audioSource, "effect_light_switch"),
            Bind<StoreSecButton>(source => source.audioSource, "effect_light_switch"),
            Bind<StoreShutterButton>(source => source.audioSource, "effect_light_switch"),
            Bind<StoreTransferButton>(source => source.audioSource, "effect_light_switch"),
            Bind<EscapeUIManager>(source => source.audioSource, "effect_light_switch"),
            Bind<EmporiumAudioPlayer>(source => source.audioBeatVictory, "shop_coin"),
            Bind<EmporiumAudioPlayer>(source => source.audioRegister, "shop_register"),
            Bind<EmporiumAudioPlayer>(source => source.audioStep1, "shop_step1"),
            Bind<EmporiumAudioPlayer>(source => source.audioStep2, "shop_step2"),
            Bind<EmporiumAudioPlayer>(source => source.audioClickClick, "shop_box"),
            Bind<EmporiumAudioPlayer>(source => source.audioNegativeTwoBeep, "shop_negative"),
            Bind<EmporiumAudioPlayer>(source => source.audioExposeScam, "shop_fail"),
            Bind<EmporiumAudioPlayer>(source => source.audioFail, "shop_fail"),
            Bind<EmporiumAudioPlayer>(source => source.audioLabeler, "shop_label"),
            Bind<EmporiumAudioPlayer>(source => source.audioBedroll, "shop_cloth"),
            Bind<EmporiumAudioPlayer>(source => source.audioLampClick, "shop_lamp_click"),
            Bind<EmporiumAudioPlayer>(source => source.audioLampSnap, "shop_lamp_snap"),
            Bind<EmporiumAudioPlayer>(source => source.audioWriting, "shop_writing"),
            Bind<EmporiumAudioPlayer>(source => source.flip1, "shop_page"),
            Bind<EmporiumAudioPlayer>(source => source.alarm2, "shop_alarm"),
            Bind<EmporiumAudioPlayer>(source => source.airlock_ext_open, "shop_door_open"),
            Bind<EmporiumAudioPlayer>(source => source.airlock_ext_close, "shop_door_close"),
            Bind<EmporiumAudioPlayer>(source => source.cross, "shop_cross"),
            Bind<EmporiumAudioPlayer>(source => source.dumpWater, "shop_water"),
            Bind<EmporiumAudioPlayer>(source => source.wirecutter, "shop_wire"),
            Bind<EmporiumAudioPlayer>(source => source.ratchet, "shop_ratchet"),
            Bind<EmporiumAudioPlayer>(source => source.ratchet2, "shop_ratchet2"),
            Bind<EmporiumAudioPlayer>(source => source.welder, "shop_welder"),
        };

        private static Binding Bind<T>(Func<T, AudioSource> source, string id) where T : MonoBehaviour
        {
            return new Binding { Id = id, Apply = () => {
                foreach (T instance in UnityEngine.Object.FindObjectsOfType<T>(true)) ApplySource(source(instance), id);
            } };
        }

        private static bool Protected(AudioSource source)
        {
            if (source == null || source.loop) return true;
            if (source.gameObject.name == "MenuThemeSource" || source.gameObject.name == "AudioSourceMusic") return true;
            foreach (StoreSoundPlayer player in UnityEngine.Object.FindObjectsOfType<StoreSoundPlayer>(true))
                if (player.musicAudioSource == source || player.gameOverAudioSource == source) return true;
            return false;
        }

        internal static void ApplySource(AudioSource source, string identifier)
        {
            if (!Mod.Active || Mod.Assets == null || Protected(source)) return;
            bool declared = false;
            foreach (Binding field in Fields) if (field.Id == identifier) { declared = true; break; }
            AudioClip clip;
            if (!declared || !Mod.Assets.Sounds.TryGetValue("jin_" + identifier, out clip) || clip == null) return;
            Snapshot previous;
            if (Changed.TryGetValue(source, out previous) && source.clip == previous.Applied) return;
            Changed[source] = new Snapshot { Original = source.clip, Applied = clip };
            source.clip = clip;
        }

        internal static void Apply()
        {
            if (!Mod.Active || Mod.Assets == null) return;
            var stale = new List<AudioSource>();
            foreach (AudioSource source in Changed.Keys) if (source == null) stale.Add(source);
            foreach (AudioSource source in stale) Changed.Remove(source);
            foreach (Binding binding in Fields) binding.Apply();
        }

        internal static void Restore()
        {
            foreach (KeyValuePair<AudioSource, Snapshot> row in Changed)
            {
                AudioSource source = row.Key;
                if (source == null || source.clip != row.Value.Applied) continue;
                if (source.isPlaying) source.Stop();
                source.clip = row.Value.Original;
            }
            Changed.Clear();
        }
    }

    [HarmonyPatch]
    internal static class BindShopUiSound
    {
        private static MethodBase TargetMethod() { return AccessTools.Method(typeof(StoreUISounds), "Use"); }
        private static void Postfix(AudioSource __1, string __2) { ShopAudio.ApplySource(__1, __2); }
    }
}
