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
    //  STATE - central singleton holding the live simulation state.
    // ========================================================================
    public enum ElectionPhase
    {
        Idle,           // waiting for next term
        Campaign,       // campaign running, voters drifting toward parties
        Voting,         // voting day (instant tally)
        Forming,        // coalition negotiations
        Governing,      // coalition in place, governing
        Failed          // no coalition possible - cooldown before re-election
    }



    public enum OverlayMode
    {
        Off = 0,
        Party = 1,
        Turnout = 2,
        Satisfaction = 3
    }



    // ========================================================================
    //  GRIEVANCES - issues a citizen cares about, derived from real game state.
    //  A citizen's grievance set steers their vote toward parties that offer
    //  remedies (tax cuts, budget boosts, etc.).
    // ========================================================================
    public enum Grievance
    {
        None = 0,
        HighTaxes,
        PoorHealth,
        HighCrime,
        PoorEducation,
        Unemployed,
        Pollution,
        LowLandValue,
        NoiseOrTrash,
    }

    // How a party feels about a given vanilla policy.
    //   Neutral = don't touch when elected
    //   Support = enact when elected
    //   Oppose  = repeal when elected, regardless of who enacted it
    public enum PolicyStance
    {
        Neutral = 0,
        Support = 1,
        Oppose  = 2,
    }
}
