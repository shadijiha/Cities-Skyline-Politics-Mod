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


    public class PoliticsState
    {
        public static PoliticsState Instance;

        // Timing (in-game "days"). We drive time by sampling SimulationManager.
        public float DaysSinceLastElection;
        public float DaysSinceCampaignStart;
        public ElectionPhase Phase = ElectionPhase.Idle;

        // Current parliament / coalition
        public int[] CurrentSeats = new int[PartyCountRef.Value];
        public float[] CurrentSupport = new float[PartyCountRef.Value]; // drifts during campaign
        public List<int> CoalitionPartyIds = new List<int>();
        public int[] ApprovalByParty = new int[PartyCountRef.Value]; // 0..100

        // Per-building cached dominant party (for overlay).
        // Rebuilt on election day; updated incrementally during Campaign.
        public byte[] DominantPartyByBuilding;   // length = BuildingManager.m_buildings.m_size
        public byte[] TurnoutByBuilding;         // 0..100
        public byte[] SatisfactionByBuilding;    // 0..100

        // History
        public List<ElectionResult> History = new List<ElectionResult>();

        // Policies currently applied by the coalition - so we can revert on coalition change.
        public List<DistrictPolicies.Policies> AppliedVanillaPolicies = new List<DistrictPolicies.Policies>();
        public bool PoliciesApplied;

        // Overlay
        public OverlayMode Overlay = OverlayMode.Off;

        // Last election snapshot (for UI)
        public ElectionResult LastResult;

        // Whether we've initialized from save yet
        public bool Initialized;

        // Rolling bill counter, resets on new term (for cosmetic "C-N" bill numbers)
        public int NextBillNumber = 1;

        // Cooldown timer (days) for Failed phase
        public float FailedCooldownRemaining;

        public PartyDef MajorCoalitionParty
        {
            get
            {
                if (CoalitionPartyIds == null || CoalitionPartyIds.Count == 0) return null;
                int best = CoalitionPartyIds[0];
                int bestSeats = CurrentSeats[best];
                for (int i = 1; i < CoalitionPartyIds.Count; i++)
                {
                    int p = CoalitionPartyIds[i];
                    if (CurrentSeats[p] > bestSeats) { best = p; bestSeats = CurrentSeats[p]; }
                }
                return Config.Parties[best];
            }
        }
    }
}
