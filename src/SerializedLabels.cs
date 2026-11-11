using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace JinSongPoc
{
    // Native Unity localization still resolves the original tables, arguments and listeners.
    internal static class SerializedLabels
    {
        private sealed class Label
        {
            internal LocalizedString Reference;
            internal TableReference Table;
            internal TableEntryReference Entry;
            internal UnityAction<string> Listener;
            internal TMP_Text[] Targets;
            internal string Native, Desired, Applied;

            internal bool StillMatches(LocalizeStringEvent source)
            {
                return source != null && source.StringReference == Reference
                    && Reference.TableReference.Equals(Table) && Reference.TableEntryReference.Equals(Entry);
            }
        }

        private static readonly string[] ShopTables = { "UI", "Interactable", "Item", "Mechanic" };
        private static readonly List<StringTable> Tables = new List<StringTable>();
        private static readonly Dictionary<LocalizeStringEvent, Label> Labels = new Dictionary<LocalizeStringEvent, Label>();
        private static Locale locale;
        private static int scene = -1;
        private static int generation;
        private static float nextDiscovery;

        internal static void Refresh()
        {
            if (!Mod.Active || Mod.Assets == null) return;
            Locale selected = LocalizationSettings.SelectedLocale;
            int currentScene = SceneManager.GetActiveScene().buildIndex;
            if (selected != locale || currentScene != scene)
            {
                Reset(selected == locale);
                locale = selected;
                scene = currentScene;
                if (selected == null || !Mod.Assets.Localization.ContainsKey(selected.Identifier.Code)) return;
                if (scene == 1 && selected.Identifier.Code == "zh")
                    foreach (string name in ShopTables) Request(name, selected, generation);
                else Request("UI", selected, generation);
            }
            if (Tables.Count == 0) return;
            if (Time.unscaledTime >= nextDiscovery)
            {
                nextDiscovery = Time.unscaledTime + 0.75f;
                Discover();
            }
            foreach (var entry in Labels) Apply(entry.Key, entry.Value);
        }

        private static void Request(string name, Locale selected, int requestGeneration)
        {
            AsyncOperationHandle<StringTable> request = LocalizationSettings.StringDatabase.GetTableAsync(name, selected);
            if (request.IsDone) Register(request, requestGeneration);
            else request.Completed += result => Register(result, requestGeneration);
        }

        private static void Register(AsyncOperationHandle<StringTable> result, int requestGeneration)
        {
            if (!Mod.Active || requestGeneration != generation || result.Status != AsyncOperationStatus.Succeeded
                || result.Result == null || result.Result.LocaleIdentifier.Code != locale.Identifier.Code) return;
            Tables.Add(result.Result);
            nextDiscovery = 0;
        }

        private static StringTable FindTable(TableReference reference)
        {
            foreach (StringTable table in Tables)
                if (reference.ReferenceType == TableReference.Type.Guid
                    ? reference.TableCollectionNameGuid == table.SharedData.TableCollectionNameGuid
                    : reference.TableCollectionName == table.TableCollectionName) return table;
            return null;
        }

        private static bool Remainder(string key)
        {
            return key == "ui_newspaper_title" || key == "ui_newspaper_subtitle"
                || key == "book_water_s2_body3" || key == "book_water_s3_body1";
        }

        private static void Discover()
        {
            var stale = new List<LocalizeStringEvent>();
            foreach (var entry in Labels)
                if (!entry.Value.StillMatches(entry.Key)) stale.Add(entry.Key);
            foreach (LocalizeStringEvent source in stale)
            {
                Detach(source, Labels[source], false);
                Labels.Remove(source);
            }
            foreach (LocalizeStringEvent source in UnityEngine.Object.FindObjectsOfType<LocalizeStringEvent>(true))
            {
                if (Labels.ContainsKey(source) || source.StringReference == null) continue;
                LocalizedString reference = source.StringReference;
                StringTable table = FindTable(reference.TableReference);
                if (table == null) continue;
                string key = reference.TableEntryReference.ResolveKeyName(table.SharedData);
                if (String.IsNullOrEmpty(key) || table.GetEntry(key) == null) continue;
                bool guide = table.TableCollectionName == "UI" && OrganizationText.IsGuide(key);
                bool remainder = table.TableCollectionName == "UI" && Remainder(key);
                if (!guide && !remainder && FixtureText.LocalizedValue(table.TableCollectionName, key) == null) continue;
                Bind(source, table.TableCollectionName, key, guide, remainder);
            }
        }

        private static void Bind(LocalizeStringEvent source, string table, string key, bool guide, bool remainder)
        {
            var targets = new HashSet<TMP_Text>();
            for (int i = 0; i < source.OnUpdateString.GetPersistentEventCount(); i++)
            {
                TMP_Text target = source.OnUpdateString.GetPersistentTarget(i) as TMP_Text;
                if (target != null) targets.Add(target);
            }
            if (targets.Count == 0)
            {
                TMP_Text own = source.GetComponent<TMP_Text>();
                if (own != null) targets.Add(own);
            }
            if (targets.Count == 0) return;

            LocalizedString reference = source.StringReference;
            var label = new Label { Reference = reference, Table = reference.TableReference, Entry = reference.TableEntryReference,
                Targets = new List<TMP_Text>(targets).ToArray() };
            label.Listener = native => {
                if (!Mod.Active || LocalizationSettings.SelectedLocale != locale || !label.StillMatches(source)) return;
                var arguments = reference.Arguments;
                object[] args = arguments == null ? null : new List<object>(arguments).ToArray();
                string result = FixtureText.LocalizedValue(table, key, args) ?? native;
                if (remainder) result = Mod.CurrentText("remainder_UI_" + key, args) ?? result;
                if (guide) result = OrganizationText.Display(result);
                label.Native = native;
                label.Desired = result;
            };
            Labels.Add(source, label);
            source.OnUpdateString.AddListener(label.Listener);
            if (source.isActiveAndEnabled) source.RefreshString();
        }

        private static void Apply(LocalizeStringEvent source, Label label)
        {
            if (!label.StillMatches(source) || label.Desired == null) return;
            // Some native components attach their text setter only when a panel opens.
            // Apply after the native event and leave a later writer's distinct value alone.
            foreach (TMP_Text target in label.Targets)
                if (target != null && target.text != label.Desired && (target.text == label.Native || target.text == label.Applied))
                    target.text = label.Desired;
            label.Applied = label.Desired;
        }

        private static void Detach(LocalizeStringEvent source, Label label, bool revert)
        {
            if (source != null) source.OnUpdateString.RemoveListener(label.Listener);
            if (!revert || label.Applied == null) return;
            foreach (TMP_Text target in label.Targets)
                if (target != null && target.text == label.Applied) target.text = label.Native;
        }

        private static void Reset(bool revert)
        {
            generation++;
            foreach (var entry in Labels) Detach(entry.Key, entry.Value, revert && entry.Value.StillMatches(entry.Key));
            Labels.Clear();
            Tables.Clear();
            nextDiscovery = 0;
        }

        internal static void Restore()
        {
            Reset(LocalizationSettings.SelectedLocale == locale);
            locale = null;
            scene = -1;
        }
    }
}
