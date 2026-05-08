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


    public static class RuntimeConfig
    {
        public static float TermLengthDays         = Config.DefaultTermLengthDays;
        public static float CampaignLengthDays     = Config.DefaultCampaignLengthDays;
        public static float ReElectionCooldownDays = Config.DefaultReElectionCooldownDays;

        // How strongly a sustained budget deficit nudges voters toward the
        // economic right. 1.0 = vanilla curve; 0 = feature disabled;
        // 2.0 = twice as sensitive. See ElectionEngine.DeficitPressure.
        public static float DeficitPressureMultiplier = 1.0f;
        public const float MinDeficitMult = 0f;
        public const float MaxDeficitMult = 3f;

        // Hotkey that toggles the main Politics panel.
        // RequireCtrl = true means the user must hold Ctrl + key.
        public static KeyCode TogglePanelKey = Config.DefaultTogglePanelKey;
        public static bool    TogglePanelRequireCtrl = true;

        // Info-view overlay legend position (top-left corner, screen pixels).
        // The legend is draggable by its right-edge dotted handle; the final
        // position is persisted to ModSettings XML so it survives restarts.
        public static float OverlayLegendX = 15f;
        public static float OverlayLegendY = 115f;

        // "Politics: <mode>" info-view button position (top-left, screen px).
        // -1f means "never been placed" - the button falls back to its auto
        // anchor (top-right) on first launch, then this value tracks the
        // last user-dragged position.
        public static float InfoButtonX = -1f;
        public static float InfoButtonY = -1f;

        // Hard bounds so sliders stay sane. Change only if you need longer/shorter.
        public const float MinTerm     = 7f;    public const float MaxTerm     = 1825f; // 1 week .. 5 years
        public const float MinCampaign = 1f;    public const float MaxCampaign = 180f;  // 1 day .. 6 months
        public const float MinCooldown = 0f;    public const float MaxCooldown = 90f;   // 0 .. 3 months

        public static void ClampAll()
        {
            TermLengthDays         = Mathf.Clamp(TermLengthDays,         MinTerm,     MaxTerm);
            CampaignLengthDays     = Mathf.Clamp(CampaignLengthDays,     MinCampaign, MaxCampaign);
            ReElectionCooldownDays = Mathf.Clamp(ReElectionCooldownDays, MinCooldown, MaxCooldown);
            DeficitPressureMultiplier = Mathf.Clamp(DeficitPressureMultiplier, MinDeficitMult, MaxDeficitMult);
            // Campaign can't exceed term length
            if (CampaignLengthDays > TermLengthDays * 0.9f)
                CampaignLengthDays = TermLengthDays * 0.9f;
        }
    }
}
