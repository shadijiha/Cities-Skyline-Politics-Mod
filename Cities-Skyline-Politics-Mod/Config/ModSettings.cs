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
using UnityEngine;

namespace PoliticsMod
{


    // ========================================================================
    //  RUNTIME CONFIG - editable in-game via the politics panel, persisted in
    //  savegames. Use these instead of the Default* constants when reading
    //  election timings at simulation time.
    // ========================================================================
    // ========================================================================
    //  MOD SETTINGS - persisted across saves in an XML file in
    //  %LOCALAPPDATA%\Colossal Order\Cities_Skylines\ModSettings\PoliticsMod.xml.
    //  Things here are user preferences (hotkey, debug toggle) as opposed
    //  to per-city state (which lives in the savegame).
    // ========================================================================
    public static class ModSettings
    {
        private static string SettingsPath
        {
            get
            {
                string baseDir = ColossalFramework.IO.DataLocation.localApplicationData;
                return System.IO.Path.Combine(
                    System.IO.Path.Combine(baseDir, "ModSettings"),
                    "PoliticsMod.xml");
            }
        }

        public static void Load()
        {
            try
            {
                string path = SettingsPath;
                if (!System.IO.File.Exists(path)) return;
                var doc = new System.Xml.XmlDocument();
                doc.Load(path);
                var root = doc.DocumentElement;
                if (root == null) return;
                foreach (System.Xml.XmlNode n in root.ChildNodes)
                {
                    if (n.NodeType != System.Xml.XmlNodeType.Element) continue;
                    string k = n.Name;
                    string v = n.InnerText ?? "";
                    switch (k)
                    {
                        case "TogglePanelKey":
                            {
                                try { RuntimeConfig.TogglePanelKey = (KeyCode)Enum.Parse(typeof(KeyCode), v); }
                                catch { }
                                break;
                            }
                        case "TogglePanelRequireCtrl":
                            RuntimeConfig.TogglePanelRequireCtrl = (v == "true" || v == "True");
                            break;
                        case "VerboseLogging":
                            DebugFlags.Verbose = (v == "true" || v == "True");
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(Config.LogPrefix + "Failed to load mod settings: " + e.Message);
            }
        }

        public static void Save()
        {
            try
            {
                string path = SettingsPath;
                string dir = System.IO.Path.GetDirectoryName(path);
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                var doc = new System.Xml.XmlDocument();
                var root = doc.CreateElement("PoliticsModSettings");
                doc.AppendChild(root);
                AppendNode(doc, root, "TogglePanelKey",        RuntimeConfig.TogglePanelKey.ToString());
                AppendNode(doc, root, "TogglePanelRequireCtrl",RuntimeConfig.TogglePanelRequireCtrl ? "true" : "false");
                AppendNode(doc, root, "VerboseLogging",        DebugFlags.Verbose ? "true" : "false");
                doc.Save(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning(Config.LogPrefix + "Failed to save mod settings: " + e.Message);
            }
        }

        private static void AppendNode(System.Xml.XmlDocument doc, System.Xml.XmlElement parent, string name, string value)
        {
            var el = doc.CreateElement(name);
            el.InnerText = value;
            parent.AppendChild(el);
        }
    }
}
