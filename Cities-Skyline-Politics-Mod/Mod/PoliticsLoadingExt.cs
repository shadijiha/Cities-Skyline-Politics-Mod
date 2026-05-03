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
    //  LOADING EXTENSION - wires everything up when a city loads.
    // ========================================================================
    public class PoliticsLoadingExt : LoadingExtensionBase
    {
        private PoliticsPanel _panel;
        private PoliticsOverlay _overlay;
        private PoliticsInfoViewButton _infoBtn;

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            if (mode != LoadMode.LoadGame && mode != LoadMode.NewGame &&
                mode != LoadMode.NewGameFromScenario) return;

            if (PoliticsState.Instance == null)
                PoliticsState.Instance = new PoliticsState();

            // Initialize per-building arrays to the current buildings buffer size.
            uint bSize = BuildingManager.instance.m_buildings.m_size;
            var st = PoliticsState.Instance;
            if (st.DominantPartyByBuilding == null || st.DominantPartyByBuilding.Length != bSize)
            {
                st.DominantPartyByBuilding = new byte[bSize];
                st.TurnoutByBuilding       = new byte[bSize];
                st.SatisfactionByBuilding  = new byte[bSize];
                // Sentinel: 255 = "no data yet", so the overlay skips buildings before
                // the first election instead of drawing them all as party 0.
                for (int i = 0; i < bSize; i++) st.DominantPartyByBuilding[i] = 255;
            }

            st.Initialized = true;

            // Spawn UI.
            try
            {
                var uiView = UIView.GetAView();
                _panel   = uiView.AddUIComponent(typeof(PoliticsPanel)) as PoliticsPanel;
                if (_panel != null) _panel.isVisible = false;

                // Overlay is a plain MonoBehaviour on its own GameObject so OnGUI
                // is always called regardless of UIView's visibility state.
                var overlayGo = new GameObject("PoliticsOverlay");
                _overlay = overlayGo.AddComponent<PoliticsOverlay>();
                UnityEngine.Object.DontDestroyOnLoad(overlayGo);

                // Standalone Info-View-style button placed near the info views strip
                _infoBtn = uiView.AddUIComponent(typeof(PoliticsInfoViewButton)) as PoliticsInfoViewButton;

                PoliticsUserMod.Log("UI created");
            }
            catch (Exception e)
            {
                Debug.LogError(Config.LogPrefix + "UI creation failed: " + e);
            }
        }

        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            if (_panel != null)   { UnityEngine.Object.Destroy(_panel.gameObject);   _panel = null; }
            if (_overlay != null) { UnityEngine.Object.Destroy(_overlay.gameObject); _overlay = null; }
            if (_infoBtn != null) { UnityEngine.Object.Destroy(_infoBtn.gameObject); _infoBtn = null; }
            if (PoliticsState.Instance != null) PoliticsState.Instance.Initialized = false;
        }
    }
}
