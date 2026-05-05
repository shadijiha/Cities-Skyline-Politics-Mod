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

        // Hotkey state -------------------------------------------------------
        // Detecting the panel-toggle hotkey from a plain MonoBehaviour rather
        // than from PoliticsPanel.Update() works around the Colossal UI
        // framework occasionally not ticking Update() on UIComponents that
        // have been invisible since creation. This GameObject is created with
        // DontDestroyOnLoad so its Update fires every frame regardless of
        // what the rest of the UI is doing.

        /// <summary>
        /// Runs every frame. Only purpose is polling the panel-toggle hotkey
        /// (configurable via mod settings). Pops the Politics panel when
        /// fired. Guards against common "doesn't work" causes: an unfocused
        /// panel, a text-field-focused UI, and a missing Ctrl modifier when
        /// one is required.
        /// </summary>
        private void Update()
        {
            try
            {
                var key = RuntimeConfig.TogglePanelKey;
                if (key == KeyCode.None) return;
                if (!Input.GetKeyDown(key)) return;

                // Don't toggle while the user is typing in a text field,
                // otherwise renaming a party (or a district, etc.) would
                // pop the Politics panel on every key press.
                if (IsTextInputFocused())
                {
                    PoliticsUserMod.Log("Hotkey " + key + " ignored - text field has focus.");
                    return;
                }

                bool ctrlRequired = RuntimeConfig.TogglePanelRequireCtrl;
                bool ctrlHeld = Input.GetKey(KeyCode.LeftControl)
                             || Input.GetKey(KeyCode.RightControl);
                if (ctrlRequired && !ctrlHeld)
                {
                    // The most common "hotkey doesn't work" cause: the user
                    // picked a new key but left "Require Ctrl" checked.
                    PoliticsUserMod.Log("Hotkey " + key
                        + " pressed but 'Require Ctrl modifier' is enabled. "
                        + "Hold Ctrl, or uncheck 'Require Ctrl' in mod settings.");
                    return;
                }

                if (PoliticsPanel.Instance == null)
                {
                    PoliticsUserMod.Log("Hotkey fired but PoliticsPanel.Instance is null - UI not ready yet.");
                    return;
                }

                PoliticsUserMod.Log("Hotkey fired: " + (ctrlHeld ? "Ctrl+" : "") + key
                    + " -> toggling Politics panel.");
                PoliticsPanel.Toggle();
            }
            catch (Exception e)
            {
                PoliticsUserMod.Log("Overlay hotkey handler threw: " + e.Message);
            }
        }

        /// <summary>
        /// True when the currently focused UI component is something that
        /// is expected to capture raw keyboard input (text fields and
        /// multi-line text components). We avoid toggling the panel in that
        /// case so typing a party name doesn't open/close the panel.
        /// </summary>
        private static bool IsTextInputFocused()
        {
            try
            {
                var view = UIView.GetAView();
                if (view == null) return false;
                var active = view.activeComponent;
                if (active == null) return false;
                if (active is UITextField)  return true;
#if UNITY_2018_OR_NEWER
                // UITextComponent doesn't exist on every CS1 build so only
                // guard against it conditionally.
                if (active is UITextComponent) return true;
#endif
                return false;
            }
            catch
            {
                return false;
            }
        }

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

        // ------------------------------------------------------------------
        // Drag state for the legend panel. The legend is moved by the user
        // pressing the dotted handle on its right edge and dragging. We keep
        // the in-progress position in RuntimeConfig and write to ModSettings
        // XML on mouse-up so the final position survives across sessions.
        // ------------------------------------------------------------------
        private bool    _dragging;
        private Vector2 _dragStartMouse;
        private Vector2 _dragStartPanel;

        private const float LegendPanelWidth = 230f;
        private const float DragHandleWidth  = 16f;

        private void OnGUI()
        {
            var st = PoliticsState.Instance;
            if (st == null || !st.Initialized) return;
            if (st.Overlay == OverlayMode.Off) return;
            if (st.DominantPartyByBuilding == null) return;

            // Harmony now handles per-building tinting via BuildingAI.GetColor patch.
            // This OnGUI renders the legend (draggable) and a "no data" hint.

            float panelW = LegendPanelWidth;
            float panelH = 22f * (PartyCountRef.Value + 1) + 10f;

            // Clamp in case the window was resized since the last save.
            float px = Mathf.Clamp(RuntimeConfig.OverlayLegendX,
                0f, Mathf.Max(0f, Screen.width  - panelW));
            float py = Mathf.Clamp(RuntimeConfig.OverlayLegendY,
                0f, Mathf.Max(0f, Screen.height - panelH));
            RuntimeConfig.OverlayLegendX = px;
            RuntimeConfig.OverlayLegendY = py;

            DrawLegend(st, px, py, panelW, panelH);
            DrawDragHandle(px, py, panelW, panelH);
            HandleLegendDrag(px, py, panelW, panelH);

            // "No data" hint floats just above the legend so it moves with it.
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
                GUI.Box(new Rect(px, py - 32f, 260f, 26f),
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

        /// <summary>
        /// Draw the legend box at (px, py). px/py is the top-left of the box
        /// in screen pixels. panelW/panelH are its outer size.
        /// </summary>
        private static void DrawLegend(PoliticsState st,
            float px, float py, float panelW, float panelH)
        {
            EnsureDotTex();
            var old = GUI.color;
            var s = new GUIStyle(GUI.skin.box);
            s.alignment = TextAnchor.UpperLeft;
            s.normal.textColor = Color.white;
            GUI.Box(new Rect(px, py, panelW, panelH), "Overlay: " + st.Overlay, s);

            // Content origin: just inside the box, below the header row.
            float cx = px + 5f;
            float cy = py + 22f;

            if (st.Overlay == OverlayMode.Party)
            {
                for (int i = 0; i < PartyCountRef.Value; i++)
                {
                    var p = Config.Parties[i];
                    GUI.color = p.Color;
                    GUI.DrawTexture(new Rect(cx, cy + i * 22, 12, 12), _dotTex);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(cx + 20, cy + i * 22 - 4, 200, 20), p.FullName);
                }
            }
            else if (st.Overlay == OverlayMode.Turnout)
            {
                GUI.color = Color.red;   GUI.DrawTexture(new Rect(cx, cy, 12, 12), _dotTex);
                GUI.color = Color.white; GUI.Label(new Rect(cx + 20, cy - 4, 200, 20), "Low turnout");
                GUI.color = Color.green; GUI.DrawTexture(new Rect(cx, cy + 22, 12, 12), _dotTex);
                GUI.color = Color.white; GUI.Label(new Rect(cx + 20, cy + 18, 200, 20), "High turnout");
            }
            else if (st.Overlay == OverlayMode.Satisfaction)
            {
                GUI.color = Color.red;  GUI.DrawTexture(new Rect(cx, cy, 12, 12), _dotTex);
                GUI.color = Color.white;GUI.Label(new Rect(cx + 20, cy - 4, 200, 20), "Unhappy");
                GUI.color = Color.cyan; GUI.DrawTexture(new Rect(cx, cy + 22, 12, 12), _dotTex);
                GUI.color = Color.white;GUI.Label(new Rect(cx + 20, cy + 18, 200, 20), "Happy");
            }
            GUI.color = old;
        }

        /// <summary>
        /// Render a 2 x 3 grid of small dots on the right edge of the legend
        /// to signal "grab here to move". Dots brighten while dragging for
        /// feedback.
        /// </summary>
        private void DrawDragHandle(float px, float py, float panelW, float panelH)
        {
            EnsureDotTex();
            var old = GUI.color;
            GUI.color = _dragging
                ? new Color(1f, 0.85f, 0.2f, 1.0f)        // yellow while grabbed
                : new Color(0.85f, 0.85f, 0.85f, 0.8f);   // subtle grey otherwise

            float rightCol = px + panelW - 6f;
            float leftCol  = px + panelW - 11f;
            float centerY  = py + panelH / 2f;
            float dotSize  = 3f;
            // 3 rows, spaced 6px apart, vertically centered.
            for (int row = 0; row < 3; row++)
            {
                float y = centerY - 7f + row * 6f;
                GUI.DrawTexture(new Rect(leftCol,  y, dotSize, dotSize), _dotTex);
                GUI.DrawTexture(new Rect(rightCol, y, dotSize, dotSize), _dotTex);
            }
            GUI.color = old;
        }

        /// <summary>
        /// Consume mouse events on the drag-handle hit rect. Updates
        /// RuntimeConfig.OverlayLegendX/Y live while dragging, persists to
        /// ModSettings XML on mouse-up. Clamped to the visible screen.
        /// </summary>
        private void HandleLegendDrag(float px, float py, float panelW, float panelH)
        {
            var e = Event.current;
            if (e == null) return;

            // Hit rect: the rightmost strip of the legend box.
            Rect handleRect = new Rect(
                px + panelW - DragHandleWidth,
                py,
                DragHandleWidth,
                panelH);

            if (e.type == EventType.MouseDown && e.button == 0
                && handleRect.Contains(e.mousePosition))
            {
                _dragging = true;
                _dragStartMouse = e.mousePosition;
                _dragStartPanel = new Vector2(px, py);
                e.Use();
            }
            else if (_dragging && e.type == EventType.MouseDrag)
            {
                Vector2 delta = e.mousePosition - _dragStartMouse;
                float newX = Mathf.Clamp(_dragStartPanel.x + delta.x,
                    0f, Mathf.Max(0f, Screen.width  - panelW));
                float newY = Mathf.Clamp(_dragStartPanel.y + delta.y,
                    0f, Mathf.Max(0f, Screen.height - panelH));
                RuntimeConfig.OverlayLegendX = newX;
                RuntimeConfig.OverlayLegendY = newY;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _dragging)
            {
                _dragging = false;
                try { ModSettings.Save(); }
                catch (Exception ex) { PoliticsUserMod.Log("Save overlay position failed: " + ex.Message); }
                e.Use();
            }
        }

        private static void DrawLegend(PoliticsState st)
        {
            // Back-compat shim - no longer used, but kept for any external
            // callers / reflective access. Delegates to the positioned
            // overload at the current RuntimeConfig location.
            float panelW = LegendPanelWidth;
            float panelH = 22f * (PartyCountRef.Value + 1) + 10f;
            DrawLegend(st,
                RuntimeConfig.OverlayLegendX,
                RuntimeConfig.OverlayLegendY,
                panelW, panelH);
        }
    }
}
