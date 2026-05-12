using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.Math;
using ColossalFramework.UI;
using HarmonyLib;
using ICities;
using PoliticsMod.Localization;
using UnityEngine;

namespace PoliticsMod
{


    // ========================================================================
    //  IUserMod ENTRY POINT
    // ========================================================================
    public class PoliticsUserMod : ICities.IUserMod
    {
        public string Name        { get { return L10n.T(L10nKeys.Mod_Name); } }
        public string Description { get { return L10n.T(L10nKeys.Mod_Description); } }

        public void OnEnabled()
        {
            Log("OnEnabled");
            ModSettings.Load();
            L10n.Init();
            HarmonyPatcher.PatchAll();
        }

        public void OnDisabled()
        {
            Log("OnDisabled");
            HarmonyPatcher.UnpatchAll();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            var group = helper.AddGroup(L10n.T(L10nKeys.Settings_Group_Main));
            group.AddCheckbox(L10n.T(L10nKeys.Settings_EnableDebugLogging), DebugFlags.Verbose, v =>
            {
                DebugFlags.Verbose = v;
                ModSettings.Save();
            });

            // --- Panel toggle hotkey ------------------------------------
            var keyGroup = helper.AddGroup(L10n.T(L10nKeys.Settings_Group_Hotkey));

            // Pre-curated list of sensible hotkey candidates (single letters,
            // F-keys, and a couple of punctuation keys). Most people won't
            // want to bind something weird.
            var choices = new List<KeyCode>();
            for (KeyCode k = KeyCode.A; k <= KeyCode.Z; k++) choices.Add(k);
            for (KeyCode k = KeyCode.F1; k <= KeyCode.F12; k++) choices.Add(k);
            choices.Add(KeyCode.BackQuote);
            choices.Add(KeyCode.Tab);

            string[] labels = new string[choices.Count];
            int selected = 0;
            for (int i = 0; i < choices.Count; i++)
            {
                labels[i] = choices[i].ToString();
                if (choices[i] == RuntimeConfig.TogglePanelKey) selected = i;
            }

            keyGroup.AddDropdown(L10n.T(L10nKeys.Settings_Hotkey), labels, selected, v =>
            {
                if (v >= 0 && v < choices.Count)
                {
                    RuntimeConfig.TogglePanelKey = choices[v];
                    ModSettings.Save();
                }
            });

            keyGroup.AddCheckbox(L10n.T(L10nKeys.Settings_RequireCtrl),
                RuntimeConfig.TogglePanelRequireCtrl,
                v =>
                {
                    RuntimeConfig.TogglePanelRequireCtrl = v;
                    ModSettings.Save();
                });

            // --- Utility buttons ----------------------------------------
            var utilGroup = helper.AddGroup(L10n.T(L10nKeys.Settings_Group_Utilities));
            utilGroup.AddButton(L10n.T(L10nKeys.Settings_OpenElectionsPanel), () => {
                if (PoliticsPanel.Instance != null)
                {
                    PoliticsPanel.Show();
                }
                else
                {
                    // Panel not created yet: happens when the user opens the
                    // mod settings from the main menu (no save loaded).
                    Debug.Log(Config.LogPrefix + "Open Elections panel: no active city. Load a save first.");
                }
            });
        }

        public static void Log(string msg)
        {
            if (DebugFlags.Verbose) Debug.Log(Config.LogPrefix + msg);
        }
    }
}
