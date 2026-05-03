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
    //  INFO-VIEW OVERLAY - draws party/turnout/satisfaction dots over buildings.
    // ========================================================================
    public class PoliticsOverlay : MonoBehaviour
    {
        private Camera _cachedCam;
        private float  _camCacheTime;

        /// <summary>
        /// Cities: Skylines does not tag its main camera as "MainCamera", so
        /// <c>Camera.main</c> returns null. Instead, find the camera that owns
        /// <c>CameraController</c> (the gameplay camera).
        /// </summary>
        private Camera GetGameCamera()
        {
            if (_cachedCam != null && Time.realtimeSinceStartup - _camCacheTime < 2f)
                return _cachedCam;
            // CameraController owns the main gameplay camera in CS1.
            var cc = UnityEngine.Object.FindObjectOfType<CameraController>();
            if (cc != null)
            {
                var cam = cc.GetComponent<Camera>();
                if (cam == null)
                {
                    var cams = cc.GetComponentsInChildren<Camera>(true);
                    if (cams != null && cams.Length > 0) cam = cams[0];
                }
                if (cam != null) { _cachedCam = cam; _camCacheTime = Time.realtimeSinceStartup; return cam; }
            }
            // Fallbacks
            if (Camera.main != null) return Camera.main;
            var all = Camera.allCameras;
            if (all != null && all.Length > 0) { _cachedCam = all[0]; _camCacheTime = Time.realtimeSinceStartup; return _cachedCam; }
            return null;
        }

        private void OnGUI()
        {
            var st = PoliticsState.Instance;
            if (st == null || !st.Initialized) return;
            if (st.Overlay == OverlayMode.Off) return;
            if (st.DominantPartyByBuilding == null) return;

            // Harmony now handles per-building tinting via BuildingAI.GetColor patch.
            // This OnGUI just renders the legend and a "no data" hint when appropriate.

            // Legend + "no data" hint
            DrawLegend(st);
            bool anyData = false;
            if (st.DominantPartyByBuilding != null)
            {
                for (int i = 0; i < st.DominantPartyByBuilding.Length; i++)
                {
                    if (st.DominantPartyByBuilding[i] < PartyCountRef.Value) { anyData = true; break; }
                }
            }
            if (!anyData)
            {
                var s = new GUIStyle(GUI.skin.box);
                s.normal.textColor = Color.yellow;
                GUI.Box(new Rect(20f, 90f, 260f, 26f),
                    "No election yet - call a snap election!", s);
            }
        }

        private static Texture2D _dotTex;
        private static void EnsureDotTex()
        {
            if (_dotTex != null) return;
            _dotTex = new Texture2D(1, 1);
            _dotTex.SetPixel(0, 0, Color.white);
            _dotTex.Apply();
        }

        private static void DrawDot(float x, float y, float size, Color c)
        {
            EnsureDotTex();
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(x - size / 2f, y - size / 2f, size, size), _dotTex);
            GUI.color = old;
        }

        private static void DrawLegend(PoliticsState st)
        {
            EnsureDotTex();
            var old = GUI.color;
            float baseY = 120f;
            float baseX = 20f;
            var s = new GUIStyle(GUI.skin.box);
            s.alignment = TextAnchor.UpperLeft;
            s.normal.textColor = Color.white;
            GUI.Box(new Rect(baseX - 5, baseY - 5, 230, 22 * (PartyCountRef.Value + 1) + 10), "Overlay: " + st.Overlay, s);
            baseY += 22;

            if (st.Overlay == OverlayMode.Party)
            {
                for (int i = 0; i < PartyCountRef.Value; i++)
                {
                    var p = Config.Parties[i];
                    GUI.color = p.Color;
                    GUI.DrawTexture(new Rect(baseX, baseY + i * 22, 12, 12), _dotTex);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(baseX + 20, baseY + i * 22 - 4, 200, 20), p.FullName);
                }
            }
            else if (st.Overlay == OverlayMode.Turnout)
            {
                GUI.color = Color.red;   GUI.DrawTexture(new Rect(baseX, baseY, 12, 12), _dotTex);
                GUI.color = Color.white; GUI.Label(new Rect(baseX + 20, baseY - 4, 200, 20), "Low turnout");
                GUI.color = Color.green; GUI.DrawTexture(new Rect(baseX, baseY + 22, 12, 12), _dotTex);
                GUI.color = Color.white; GUI.Label(new Rect(baseX + 20, baseY + 18, 200, 20), "High turnout");
            }
            else if (st.Overlay == OverlayMode.Satisfaction)
            {
                GUI.color = Color.red;  GUI.DrawTexture(new Rect(baseX, baseY, 12, 12), _dotTex);
                GUI.color = Color.white;GUI.Label(new Rect(baseX + 20, baseY - 4, 200, 20), "Unhappy");
                GUI.color = Color.cyan; GUI.DrawTexture(new Rect(baseX, baseY + 22, 12, 12), _dotTex);
                GUI.color = Color.white;GUI.Label(new Rect(baseX + 20, baseY + 18, 200, 20), "Happy");
            }
            GUI.color = old;
        }
    }
}
