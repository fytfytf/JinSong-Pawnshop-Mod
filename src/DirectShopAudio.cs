using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JinSongPoc
{
    // Reuses approved clips at the native direct-play lookup. Never changes AudioDirectory or playback rules.
    internal static class DirectShopAudio
    {
        private sealed class State { internal AudioClip Native, Applied; }
        private static readonly Dictionary<AudioSource, State> Sources = new Dictionary<AudioSource, State>();
        private static readonly Dictionary<AudioClip, AudioClip> CountdownClips = new Dictionary<AudioClip, AudioClip>();
        private static readonly HashSet<AudioClip> UnreadableCountdowns = new HashSet<AudioClip>();
        private static readonly Dictionary<string, string> Clips = new Dictionary<string, string> {
            { "battery_insert", "jin_shop_ratchet" }, { "battery_removed", "jin_shop_ratchet2" },
            { "module_insert", "jin_shop_box" }, { "module_remove", "jin_shop_ratchet2" },
            { "scanner_activate", "jin_effect_button3" }, { "blender", "jin_shop_welder" },
            { "cassette_insert", "jin_shop_box" }, { "cassette_remove", "jin_shop_cross" },
            { "click_vintage", "jin_effect_button1" }, { "interface_click", "jin_effect_button1" },
            { "PING_SONAR", "jin_effect_button3" }, { "turbo", "jin_shop_welder" },
            { "alarm_siren", "jin_shop_alarm" }, { "close_door", "jin_shop_door_close" },
            { "shotgun_cock", "jin_shop_ratchet" }, { "shotgun_pump", "jin_shop_ratchet2" }
        };

        // Keep the native countdown's complete duration and burst envelope, including silence.
        // Replacing a whole countdown with a short button click would lose the remaining warnings.
        private static AudioClip Countdown(AudioClip native)
        {
            AudioClip result;
            if (CountdownClips.TryGetValue(native, out result)) return result;
            if (UnreadableCountdowns.Contains(native) || native.samples <= 0 || native.channels <= 0) return native;
            try
            {
                float[] original = new float[native.samples * native.channels];
                if (!native.GetData(original, 0)) { UnreadableCountdowns.Add(native); return native; }
                float[] samples = new float[native.samples];
                int block = Math.Max(1, native.frequency / 200);
                uint random = 173u;
                float previous = 0f, age = 1f;
                double totalEnergy = 0;
                for (int start = 0; start < native.samples; start += block)
                {
                    int end = Math.Min(native.samples, start + block);
                    double energy = 0;
                    for (int i = start * native.channels; i < end * native.channels; i++)
                        energy += original[i] * original[i];
                    totalEnergy += energy;
                    float rms = (float)Math.Sqrt(energy / ((end - start) * native.channels));
                    if (rms > Math.Max(.008f, previous * 1.8f)) age = 0f;
                    for (int i = start; i < end; i++)
                    {
                        age += 1f / native.frequency;
                        random = random * 1664525u + 1013904223u;
                        float noise = (random >> 8) / 8388608f - 1f;
                        float wood = (float)(Math.Sin(age * 2 * Math.PI * 740)
                            + .35 * Math.Sin(age * 2 * Math.PI * 1190));
                        float grain = .25f + .75f * (float)Math.Exp(-age * 24f);
                        samples[i] = Math.Max(-.8f, Math.Min(.8f, rms * (wood + .2f * noise) * grain));
                    }
                    previous = rms;
                }
                // Some streamed/compressed providers return a zero-filled buffer before loading.
                if (totalEnergy <= 1e-10) { UnreadableCountdowns.Add(native); return native; }
                result = AudioClip.Create("jinsong_poc.countdown.wood", native.samples, 1, native.frequency, false);
                if (!result.SetData(samples, 0)) { UnityEngine.Object.Destroy(result); UnreadableCountdowns.Add(native); return native; }
                CountdownClips.Add(native, result);
                return result;
            }
            catch (Exception)
            {
                UnreadableCountdowns.Add(native);
                return native;
            }
        }

        private static AudioClip Lookup(AudioDirectory directory, string id)
        {
            AudioClip native = directory.GetAudioClip(id);
            if (!Mod.Active || Mod.Assets == null || native == null || id == null) return native;
            int scene = SceneManager.GetActiveScene().buildIndex;
            if (scene != 1 && !(scene == 0 && (id == "click_vintage" || id == "interface_click"))) return native;
            if (ModLoader.Mods != null)
                foreach (LoadedMod mod in ModLoader.Mods)
                    if (mod != Mod.Assets && mod.Sounds != null && mod.Sounds.ContainsValue(native)) return native;
            if (id == "countdown") return Countdown(native);
            string key;
            AudioClip replacement;
            if (!Clips.TryGetValue(id, out key) || !Mod.Assets.Sounds.TryGetValue(key, out replacement) || replacement == null) return native;
            return replacement;
        }

        internal static void Restore()
        {
            foreach (var pair in Sources)
                if (pair.Key != null && pair.Key.clip == pair.Value.Applied) pair.Key.clip = pair.Value.Native;
            Sources.Clear();
            foreach (AudioClip clip in CountdownClips.Values) if (clip != null) UnityEngine.Object.Destroy(clip);
            CountdownClips.Clear();
            UnreadableCountdowns.Clear();
        }

        [HarmonyPatch]
        private static class DirectPlayers
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                foreach (string name in new[] { "Play", "Play2", "Play3" })
                    yield return AccessTools.Method(typeof(AudioManager), name, new[] { typeof(string) });
            }
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo native = AccessTools.Method(typeof(AudioDirectory), "GetAudioClip", new[] { typeof(string) });
                MethodInfo themed = AccessTools.Method(typeof(DirectShopAudio), "Lookup");
                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.Calls(native)) { instruction.opcode = OpCodes.Call; instruction.operand = themed; }
                    yield return instruction;
                }
            }
            private static void Postfix(AudioManager __instance, string __0, bool __result,
                AudioDirectory ___audioDirectory, MethodBase __originalMethod)
            {
                if (!Mod.Active || !__result || __0 == null || Mod.Assets == null) return;
                string key;
                AudioClip applied;
                if (__0 == "countdown")
                {
                    AudioClip native = ___audioDirectory.GetAudioClip(__0);
                    if (native == null || !CountdownClips.TryGetValue(native, out applied)) return;
                }
                else if (!Clips.TryGetValue(__0, out key) || !Mod.Assets.Sounds.TryGetValue(key, out applied)) return;
                string suffix = __originalMethod.Name == "Play" ? "" : __originalMethod.Name.Substring(4);
                FieldInfo field = AccessTools.Field(typeof(AudioManager), "audioSource" + suffix);
                AudioSource source = field == null ? null : field.GetValue(__instance) as AudioSource;
                if (source == null || source.clip != applied) return;
                Sources[source] = new State { Native = ___audioDirectory.GetAudioClip(__0), Applied = applied };
            }
        }
    }
}
