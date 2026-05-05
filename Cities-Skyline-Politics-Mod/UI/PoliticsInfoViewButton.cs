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
    //  INFO-VIEW BUTTON - standalone UI button sitting on screen, styled like
    //  the vanilla Info View tab buttons. Clicking it cycles:
    //      Off → Party → Turnout → Satisfaction → Off
    //  Each cycle forces a building color refresh so the tint changes
    //  instantly.
    // ========================================================================
    public class PoliticsInfoViewButton : UIPanel
    {
        private UIButton _btn;
        private UISprite _colorDot;
        private UIDragHandle _dragHandle;

        // Thin strip on the right reserved for the drag handle.
        private const float DragHandleWidth = 16f;

        public override void Start()
        {
            base.Start();

            // The Info Views toggle button anchors at the bottom-left of the screen.
            // We sit just above it so we're visually associated with the info views.
            size = new Vector2(170f, 38f);
            name = "PoliticsInfoViewButton";

            // Position: if the user has previously dragged the button we
            // restore that spot, otherwise fall back to the computed anchor.
            Vector2 pos;
            if (RuntimeConfig.InfoButtonX >= 0f && RuntimeConfig.InfoButtonY >= 0f)
            {
                pos = new Vector2(RuntimeConfig.InfoButtonX, RuntimeConfig.InfoButtonY);
            }
            else
            {
                pos = FindInfoViewAnchor();
                // Record the default so it persists from first launch.
                RuntimeConfig.InfoButtonX = pos.x;
                RuntimeConfig.InfoButtonY = pos.y;
            }
            relativePosition = pos;

            _btn = AddUIComponent<UIButton>();
            _btn.size = new Vector2(170f, 38f);
            _btn.relativePosition = Vector3.zero;
            _btn.normalBgSprite   = "ButtonMenu";
            _btn.hoveredBgSprite  = "ButtonMenuHovered";
            _btn.pressedBgSprite  = "ButtonMenuPressed";
            _btn.focusedBgSprite  = "ButtonMenuFocused";
            _btn.textColor        = new Color32(255, 255, 255, 255);
            _btn.textHorizontalAlignment = UIHorizontalAlignment.Center;
            _btn.textVerticalAlignment   = UIVerticalAlignment.Middle;
            _btn.textScale = 0.85f;
            // Mirror the right padding by the drag-handle width so the label
            // visually centers in the still-clickable portion of the button.
            _btn.textPadding = new RectOffset(24, 6 + (int)DragHandleWidth, 6, 6);
            _btn.tooltip = "Politics info view: cycle Party / Turnout / Satisfaction";
            _btn.eventClick += OnClick;

            // Small colored swatch on the left side of the button
            _colorDot = _btn.AddUIComponent<UISprite>();
            _colorDot.spriteName = "EmptySprite";
            _colorDot.size = new Vector2(14f, 14f);
            _colorDot.relativePosition = new Vector3(8f, 12f);
            _colorDot.color = new Color32(160, 160, 160, 255);

            BuildDragHandle();

            UpdateLabel();
        }

        /// <summary>
        /// Build the 16-pixel-wide drag-handle strip on the right edge of
        /// the button. The handle is a <see cref="UIDragHandle"/> targeting
        /// this panel, so CS1's UI framework moves the whole button for us.
        /// A 2 x 3 grid of small dots renders on top of the strip as a
        /// visual grip indicator, matching the legend overlay.
        /// </summary>
        private void BuildDragHandle()
        {
            _dragHandle = AddUIComponent<UIDragHandle>();
            _dragHandle.target = this;
            _dragHandle.size = new Vector2(DragHandleWidth, 38f);
            _dragHandle.relativePosition = new Vector3(170f - DragHandleWidth, 0f);
            _dragHandle.name = "PoliticsInfoViewButton.DragHandle";
            _dragHandle.tooltip = "Drag to move";

            // Subtle darker backdrop so the strip reads as a separate control.
            var handleBg = _dragHandle.AddUIComponent<UISprite>();
            handleBg.spriteName = "GenericPanel";
            handleBg.size = _dragHandle.size;
            handleBg.relativePosition = Vector3.zero;
            handleBg.color = new Color32(30, 30, 40, 180);
            handleBg.isInteractive = false; // don't steal mouse events

            // 2 columns x 3 rows of dots, centered vertically in the strip.
            const int cols = 2;
            const int rows = 3;
            const float dotSize = 3f;
            const float dotSpacingX = 5f;
            const float dotSpacingY = 6f;
            float totalGridW = (cols - 1) * dotSpacingX + dotSize;
            float totalGridH = (rows - 1) * dotSpacingY + dotSize;
            float gridOriginX = (_dragHandle.width  - totalGridW) / 2f;
            float gridOriginY = (_dragHandle.height - totalGridH) / 2f;
            for (int col = 0; col < cols; col++)
            {
                for (int row = 0; row < rows; row++)
                {
                    var dot = _dragHandle.AddUIComponent<UISprite>();
                    dot.spriteName = "EmptySprite";
                    dot.size = new Vector2(dotSize, dotSize);
                    dot.color = new Color32(220, 220, 220, 220);
                    dot.relativePosition = new Vector3(
                        gridOriginX + col * dotSpacingX,
                        gridOriginY + row * dotSpacingY);
                    dot.isInteractive = false;
                }
            }

            // Persist the final position on mouse-up so the XML isn't
            // written on every pointer tick during the drag.
            _dragHandle.eventMouseUp += (c, p) =>
            {
                RuntimeConfig.InfoButtonX = relativePosition.x;
                RuntimeConfig.InfoButtonY = relativePosition.y;
                try { ModSettings.Save(); }
                catch (Exception ex) { PoliticsUserMod.Log("Save info button position failed: " + ex.Message); }
            };
        }

        private Vector2 FindInfoViewAnchor()
        {
            // Place the button in the TOP-RIGHT corner of the screen, below
            // the top toolbar area but clear of the vanilla info views panel
            // (which opens from the top-left).
            // Using absolute screen coordinates via UIView.fixedWidth so the
            // position is stable across resolutions.
            try
            {
                var view = UIView.GetAView();
                if (view != null)
                {
                    return new Vector2(view.fixedWidth - 190f, 70f);
                }
            }
            catch { }
            return new Vector2(Screen.width - 190f, 70f);
        }

        private void OnClick(UIComponent c, UIMouseEventParameter p)
        {
            // Remember overlay state BEFORE cycling so we can detect the
            // Off -> Party transition and open the main panel with it.
            OverlayMode before = PoliticsState.Instance != null
                ? PoliticsState.Instance.Overlay
                : OverlayMode.Off;

            HarmonyPatcher.CycleOverlayAndSync();

            // On the specific transition Off -> Party, also open the main
            // Politics panel. Further cycles (Party -> Turnout -> Satisfaction
            // -> Off) leave the panel alone.
            if (before == OverlayMode.Off && PoliticsPanel.Instance != null)
            {
                PoliticsPanel.Show();
            }

            UpdateLabel();
        }

        public override void Update()
        {
            base.Update();
            // Keep label/swatch in sync if state changed elsewhere (panel button, etc.)
            UpdateLabel();
        }

        private OverlayMode _lastSeen = (OverlayMode)(-1);
        private void UpdateLabel()
        {
            var st = PoliticsState.Instance;
            if (st == null || _btn == null) return;
            if (st.Overlay == _lastSeen) return;
            _lastSeen = st.Overlay;

            string label;
            Color32 swatch;
            switch (st.Overlay)
            {
                case OverlayMode.Party:
                    label  = "Politics: Party";
                    swatch = new Color32(200, 200, 255, 255);
                    break;
                case OverlayMode.Turnout:
                    label  = "Politics: Turnout";
                    swatch = new Color32(120, 200, 120, 255);
                    break;
                case OverlayMode.Satisfaction:
                    label  = "Politics: Satisfaction";
                    swatch = new Color32(120, 180, 220, 255);
                    break;
                default:
                    label  = "Politics: Off";
                    swatch = new Color32(130, 130, 130, 255);
                    break;
            }
            _btn.text = label;
            if (_colorDot != null) _colorDot.color = swatch;
        }
    }
}
