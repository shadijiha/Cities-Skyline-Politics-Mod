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
    //  UI HELPERS - reusable bits (draggable windows, etc.)
    // ========================================================================
    public static class UIHelpers
    {
        /// <summary>
        /// Make a UIPanel draggable by its top bar. Adds an invisible
        /// UIDragHandle spanning the full width of the top `headerHeight`
        /// pixels so the user can grab and move the window.
        /// </summary>
        public static UIDragHandle MakeDraggable(UIPanel panel, float headerHeight = 38f)
        {
            if (panel == null) return null;
            var drag = panel.AddUIComponent<UIDragHandle>();
            drag.target = panel;
            drag.relativePosition = Vector3.zero;
            drag.size = new Vector2(panel.width, headerHeight);
            // Keep the drag handle behind the close/title children visually.
            drag.SendToBack();
            return drag;
        }
    }
}
