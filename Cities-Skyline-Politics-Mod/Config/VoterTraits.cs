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
    //  VOTER TRAITS - how citizen demographics bias voting on the economic
    //  axis (-1 left .. +1 right). Editable in-game and persisted in save.
    //
    //  Defaults are chosen to roughly match the previous hardcoded DecideVote
    //  behavior (high wealth pushes right, education pushes left, etc.).
    // ========================================================================
    public static class VoterTraits
    {
        // Education
        public static float BiasEduUneducated      = +0.30f; // less-educated → right
        public static float BiasEduEducated        = +0.05f;
        public static float BiasEduWellEducated    = -0.10f;
        public static float BiasEduHighlyEducated  = -0.30f; // highly-educated → left

        // Wealth
        public static float BiasWealthLow     = -0.30f;
        public static float BiasWealthMedium  =  0.00f;
        public static float BiasWealthHigh    = +0.40f;

        // Employment
        public static float BiasEmployed   = +0.05f;
        public static float BiasUnemployed = -0.25f;

        // Age (only young/adult/senior vote; children/teens are skipped)
        public static float BiasYoung  = -0.15f;
        public static float BiasAdult  =  0.00f;
        public static float BiasSenior = +0.20f;

        // Life conditions
        public static float BiasSick           = -0.20f;  // sick → wants healthcare (left)
        public static float BiasHighPollution  = -0.30f;  // pollution exposure → left (green/labour)

        public const float Min = -1f, Max = +1f;

        public static void ResetToDefaults()
        {
            BiasEduUneducated      = +0.30f;
            BiasEduEducated        = +0.05f;
            BiasEduWellEducated    = -0.10f;
            BiasEduHighlyEducated  = -0.30f;
            BiasWealthLow          = -0.30f;
            BiasWealthMedium       =  0.00f;
            BiasWealthHigh         = +0.40f;
            BiasEmployed           = +0.05f;
            BiasUnemployed         = -0.25f;
            BiasYoung              = -0.15f;
            BiasAdult              =  0.00f;
            BiasSenior             = +0.20f;
            BiasSick               = -0.20f;
            BiasHighPollution      = -0.30f;
        }
    }
}
