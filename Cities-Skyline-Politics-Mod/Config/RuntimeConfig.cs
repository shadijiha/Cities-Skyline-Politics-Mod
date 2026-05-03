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

        // Hotkey that toggles the main Politics panel.
        // RequireCtrl = true means the user must hold Ctrl + key.
        public static KeyCode TogglePanelKey = Config.DefaultTogglePanelKey;
        public static bool    TogglePanelRequireCtrl = true;

        // Hard bounds so sliders stay sane. Change only if you need longer/shorter.
        public const float MinTerm     = 7f;    public const float MaxTerm     = 1825f; // 1 week .. 5 years
        public const float MinCampaign = 1f;    public const float MaxCampaign = 180f;  // 1 day .. 6 months
        public const float MinCooldown = 0f;    public const float MaxCooldown = 90f;   // 0 .. 3 months

        public static void ClampAll()
        {
            TermLengthDays         = Mathf.Clamp(TermLengthDays,         MinTerm,     MaxTerm);
            CampaignLengthDays     = Mathf.Clamp(CampaignLengthDays,     MinCampaign, MaxCampaign);
            ReElectionCooldownDays = Mathf.Clamp(ReElectionCooldownDays, MinCooldown, MaxCooldown);
            // Campaign can't exceed term length
            if (CampaignLengthDays > TermLengthDays * 0.9f)
                CampaignLengthDays = TermLengthDays * 0.9f;
        }
    }
}
