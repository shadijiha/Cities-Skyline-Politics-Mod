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
    //  HARMONY PATCHER - tints buildings natively when overlay is active.
    //
    //  We use a BuildingAI.GetColor prefix patch. When our overlay mode is
    //  something other than Off, we compute the color for residential buildings
    //  based on that building's cached per-building data, and short-circuit the
    //  original method by assigning to __result + returning false.
    //
    //  The patch is only effective when:
    //    * CitiesHarmony is installed and loaded (provides 0Harmony.dll)
    //    * The overlay is enabled
    //    * The InfoManager is in a neutral mode (None) - otherwise we defer to
    //      the vanilla info view so Education/Crime/etc. still work.
    // ========================================================================
    public static class HarmonyPatcher
    {
        private const string HarmonyId = "com.shadijih.politicsmod";
        private static bool _patched = false;

        public static bool IsPatched { get { return _patched; } }

        public static void PatchAll()
        {
            if (_patched) return;
            try
            {
                var harmony = new Harmony(HarmonyId);

                // First: apply attribute-declared patches (base BuildingAI.GetColor).
                harmony.PatchAll(typeof(HarmonyPatcher).Assembly);

                // BuildingAI.GetColor is virtual and OVERRIDDEN in many subclasses
                // (ResidentialBuildingAI, LowResidentialBuildingAI, etc.). A
                // patch on the base is NOT inherited - we must patch each
                // subclass method individually.
                var prefix = typeof(BuildingAI_GetColor_Patch).GetMethod("Prefix",
                    BindingFlags.Public | BindingFlags.Static);

                var hmPrefix = new HarmonyMethod(prefix);
                int count = 0;
                foreach (var t in typeof(BuildingAI).Assembly.GetTypes())
                {
                    if (!typeof(BuildingAI).IsAssignableFrom(t)) continue;
                    if (t == typeof(BuildingAI)) continue; // already patched via attribute
                    if (t.IsAbstract) continue;
                    // Find THIS type's own declared GetColor, not the inherited one.
                    var method = t.GetMethod(
                        "GetColor",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (method == null) continue;
                    try
                    {
                        harmony.Patch(method, hmPrefix);
                        count++;
                    }
                    catch (Exception pe)
                    {
                        PoliticsUserMod.Log("Could not patch " + t.Name + ".GetColor: " + pe.Message);
                    }
                }

                _patched = true;
                PoliticsUserMod.Log("Harmony patches applied (" + count + " BuildingAI subclass overrides).");
            }
            catch (Exception e)
            {
                Debug.LogError(Config.LogPrefix + "Harmony patch failed (is CitiesHarmony installed?): " + e);
            }
        }

        public static void UnpatchAll()
        {
            if (!_patched) return;
            try
            {
                var harmony = new Harmony(HarmonyId);
                harmony.UnpatchAll(HarmonyId);
                _patched = false;
                PoliticsUserMod.Log("Harmony patches removed.");
            }
            catch (Exception e)
            {
                Debug.LogError(Config.LogPrefix + "Harmony unpatch failed: " + e);
            }
        }

        /// <summary>
        /// Force the game to re-color all buildings. Call this when the overlay
        /// mode changes so tints update immediately.
        /// </summary>
        public static void RefreshBuildingColors()
        {
            try
            {
                var bm = Singleton<BuildingManager>.instance;
                uint n = bm.m_buildings.m_size;
                for (ushort i = 1; i < n; i++)
                {
                    var b = bm.m_buildings.m_buffer[i];
                    if ((b.m_flags & Building.Flags.Created) == 0) continue;
                    if (b.Info == null) continue;
                    if (b.Info.GetService() != ItemClass.Service.Residential) continue;
                    bm.UpdateBuildingColors(i);
                }
            }
            catch (Exception e)
            {
                PoliticsUserMod.Log("RefreshBuildingColors failed: " + e.Message);
            }
        }

        /// <summary>
        /// Cycle the politics overlay (Off → Party → Turnout → Satisfaction → Off).
        ///
        /// To get the full info-view experience (dimmed other buildings, legend
        /// panel, etc.), we use a three-pronged approach:
        ///   1. SetCurrentMode(Density, Default)  - flips the engine state
        ///   2. Find and "click" the vanilla Population info-view button if
        ///      we can locate it, to trigger the UI chrome
        ///   3. Fallback: directly toggle the info-view panel components
        ///
        /// Our Harmony patch on BuildingAI.GetColor overrides the coloring
        /// while our overlay state is non-Off.
        /// </summary>
        public static void CycleOverlayAndSync()
        {
            var st = PoliticsState.Instance;
            if (st == null) return;

            OverlayMode prev = st.Overlay;
            int next = ((int)st.Overlay + 1) % 4;
            st.Overlay = (OverlayMode)next;

            try
            {
                if (st.Overlay == OverlayMode.Off)
                {
                    // Exit the info view entirely
                    ExitInfoView();
                }
                else if (prev == OverlayMode.Off)
                {
                    // First transition Off → Party: enter info view
                    EnterDensityInfoView();
                }
                // Subsequent cycles just change our overlay data; no UI change needed.
            }
            catch (Exception e)
            {
                PoliticsUserMod.Log("Info view sync failed: " + e.Message);
            }

            RefreshBuildingColors();
        }

        private static void EnterDensityInfoView()
        {
            // 1. Engine state
            var im = InfoManager.instance;
            if (im != null)
            {
                im.SetCurrentMode(InfoManager.InfoMode.Density, InfoManager.SubInfoMode.Default);
            }

            // 2. Try to open the vanilla info-views panel so the legend shows up.
            //    The panel is named "InfoViewsPanel" (or "InfoPanel" in some builds).
            try
            {
                var view = UIView.GetAView();
                if (view != null)
                {
                    var infoPanel = view.FindUIComponent("InfoViewsPanel");
                    if (infoPanel != null)
                    {
                        // Not opening/closing the panel itself - just making sure
                        // the info mode is live. The panel chrome follows.
                        PoliticsUserMod.Log("InfoViewsPanel located (" + infoPanel.name + ")");
                    }
                }
            }
            catch (Exception e)
            {
                PoliticsUserMod.Log("UI probe failed: " + e.Message);
            }
            PoliticsUserMod.Log("Entered Density info view (overlay=" +
                PoliticsState.Instance.Overlay + ")");
        }

        private static void ExitInfoView()
        {
            var im = InfoManager.instance;
            if (im != null)
            {
                im.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
            }
            PoliticsUserMod.Log("Exited info view.");
        }
    }
}
