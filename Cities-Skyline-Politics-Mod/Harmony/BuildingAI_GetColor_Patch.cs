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


    /// <summary>
    /// Harmony prefix on BuildingAI.GetColor. When the overlay is active and
    /// the building is residential, returns our custom color and skips the
    /// original method.
    /// </summary>
    [HarmonyPatch(typeof(BuildingAI), "GetColor")]
    public static class BuildingAI_GetColor_Patch
    {
        public static bool Prefix(BuildingAI __instance, ushort buildingID, ref Building data,
                                  InfoManager.InfoMode infoMode, ref Color __result)
        {
            var st = PoliticsState.Instance;
            if (st == null || !st.Initialized) return true;
            if (st.Overlay == OverlayMode.Off)  return true;

            // We piggyback on the vanilla "Density" (Population) info view.
            // When our overlay is active, we've switched InfoManager to Density
            // and hijack its coloring. Any other info mode: leave alone.
            if (infoMode != InfoManager.InfoMode.Density) return true;

            // Only color residential buildings (matches our data coverage).
            var info = data.Info;
            if (info == null) return true;
            if (info.GetService() != ItemClass.Service.Residential) return true;

            if (st.DominantPartyByBuilding == null) return true;
            if (buildingID >= st.DominantPartyByBuilding.Length) return true;

            // Neutral "no data" color for residential buildings that have no
            // voter data yet (built after last election, not sampled, etc.).
            // This keeps the info-view consistent - no vanilla population
            // colors leaking through.
            Color noData = new Color(0.35f, 0.35f, 0.4f, 1f);

            Color c;
            bool show;
            switch (st.Overlay)
            {
                case OverlayMode.Party:
                {
                    byte pid = st.DominantPartyByBuilding[buildingID];
                    if (pid >= PartyCountRef.Value)
                    {
                        __result = noData;
                        return false; // no data - show neutral
                    }
                    c = (Color)Config.Parties[pid].Color;
                    show = true;
                    break;
                }
                case OverlayMode.Turnout:
                {
                    byte t = st.TurnoutByBuilding[buildingID];
                    if (t == 0) { __result = noData; return false; }
                    c = Color.Lerp(new Color(0.7f, 0.1f, 0.1f), new Color(0.1f, 0.8f, 0.1f), t / 100f);
                    show = true;
                    break;
                }
                case OverlayMode.Satisfaction:
                {
                    byte sa = st.SatisfactionByBuilding[buildingID];
                    if (sa == 0) { __result = noData; return false; }
                    c = Color.Lerp(new Color(0.7f, 0.1f, 0.1f), new Color(0.2f, 0.7f, 0.9f), sa / 100f);
                    show = true;
                    break;
                }
                default:
                    return true;
            }

            if (!show) return true;

            // Boost saturation/brightness so it reads well on residential geometry.
            c.a = 1f;
            __result = c;
            return false; // skip original
        }
    }
}
