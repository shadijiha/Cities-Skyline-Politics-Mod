// ============================================================================
//  PoliticsMod.cs  —  Cities: Skylines 1 Politics & Elections Mod (single-file)
// ----------------------------------------------------------------------------
//  Features
//    * 6 configurable parties (ideology vector + color + platform).
//    * Hybrid voter model (citizen wealth/education/age + local pollution + noise).
//    * 150-seat parliament, coalition formation by ideology distance.
//    * Snap re-election if no coalition can form.
//    * Ruling coalition auto-enables a bundle of vanilla DistrictPolicy flags
//      AND applies custom modifiers (tax rate, budget, happiness).
//    * Toggleable info-view overlay (3 modes: Party / Turnout / Satisfaction)
//      drawn via OnGUI over residential buildings, similar to the education view.
//    * Full UI panel (bar chart, coalition, policies, term countdown, campaign banner).
//    * Election results popup.
//    * All state persisted in savegame via SerializableDataExtensionBase.
//    * Everything configurable at top of file (Config class).
//
//  No Harmony dependency. Pure ICities + ColossalFramework.
//
//  Author scaffold: generated for @shadijih, 2026.
// ============================================================================

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
    //  CONFIG — edit these to tune the mod without touching logic.
    // ========================================================================
    public static class Config
    {
        // -- Parliament / elections -----------------------------------------
        // Parliament seats are computed dynamically from population at the time
        // an election is called: 1 seat per N citizens, clamped to a sane range,
        // and rounded to the nearest odd number so ties are impossible.
        public const int SeatsPerCitizens   = 1000;
        public const int MinParliamentSeats = 25;
        public const int MaxParliamentSeats = 601;

        /// <summary>Dynamic seat count based on current population.</summary>
        public static int ParliamentSeats
        {
            get
            {
                int pop = CitizenManagerUtil.GetPopulation();
                int seats = pop / SeatsPerCitizens;
                if (seats < MinParliamentSeats) seats = MinParliamentSeats;
                if (seats > MaxParliamentSeats) seats = MaxParliamentSeats;
                // Force odd so coalitions can always clear 50%.
                if ((seats & 1) == 0) seats++;
                return seats;
            }
        }

        /// <summary>Half-plus-one — minimum seats to form a ruling coalition.</summary>
        public static int MajorityThreshold
        {
            get { return (ParliamentSeats / 2) + 1; }
        }

        public const int MinPopulationForElections  = 3000;  // elections only kick in after this
        // Default values for the runtime-editable fields below. The *actual*
        // values used by the simulation live in RuntimeConfig and are editable
        // via the in-game panel and persisted in the savegame.
        public const float DefaultTermLengthDays         = 365f; // in-game days between elections (1 year)
        public const float DefaultCampaignLengthDays     = 30f;  // campaign duration before voting day
        public const float DefaultReElectionCooldownDays = 14f;  // pause after failed coalition
        public const int   MaxCoalitionPartners          = 4;    // coalition cannot exceed this many parties

        // -- Voter model ----------------------------------------------------
        public const float VoterNoise               = 0.25f; // 0..1, randomness in voter decisions
        public const float TurnoutBase              = 0.55f; // base turnout if happy
        public const float TurnoutHappinessBoost    = 0.35f; // + up to this much from happiness
        public const int   VoterSampleSize          = 5000;  // citizens sampled per election (perf)

        // -- UI --------------------------------------------------------------
        public const KeyCode TogglePanelKey         = KeyCode.P;  // Ctrl+P opens panel
        public const float   NotificationDurationS  = 8f;

        // -- Info-view overlay ----------------------------------------------
        public const float OverlayDotSize           = 12f;   // pixels
        public const float OverlayMaxDistance       = 1500f; // world units from camera

        // -- Log prefix -----------------------------------------------------
        public const string LogPrefix               = "[PoliticsMod] ";

        // -- Parties --------------------------------------------------------
        //  Ideology axes are -1..+1 per axis.
        //    economic:   -1 left (redistribution, high taxes)   / +1 right (low tax, business-friendly)
        //    social:     -1 progressive (green, diverse)        / +1 traditional
        //    governance: -1 libertarian / +1 authoritarian
        //
        //  Platforms are (a) vanilla DistrictPolicy flags the party tries to enact,
        //  and (b) custom modifiers applied while the party is in the coalition.
        public static PartyDef[] Parties = new PartyDef[]
        {
            new PartyDef
            {
                Id = 0, ShortName = "GRN", FullName = "Green Progressive",
                Color = new Color32( 76, 175,  80, 255),
                Ideology = new Vector3(-0.5f, -0.8f, -0.1f),
                VanillaPolicies = new[]
                {
                    DistrictPolicies.Policies.Recycling,
                    DistrictPolicies.Policies.SmokeDetectors,
                    DistrictPolicies.Policies.FreeTransport
                },
                Modifiers = new PolicyModifiers
                {
                    TaxDeltaRes = +2, TaxDeltaCom = +1, TaxDeltaInd = +1, TaxDeltaOff = 0,
                    BudgetDeltaEducation     = +10,
                    BudgetDeltaHealth        = +5,
                    BudgetDeltaPolice        = -5,
                    BudgetDeltaTransport     = +15,
                    BudgetDeltaBeautification= +20,
                    BudgetDeltaGarbage       = +10,
                    BudgetDeltaWater         = +5,
                    BudgetDeltaElectricity   = 0,
                    BudgetDeltaFire          = 0,
                    BudgetDeltaRoads         = -5,
                    BudgetDeltaIndustry      = -10,
                    HappinessDelta = +2, PollutionMultiplier = 0.9f
                }
            },
            new PartyDef
            {
                Id = 1, ShortName = "LAB", FullName = "Labour & Unions",
                Color = new Color32(229,  57,  53, 255),
                Ideology = new Vector3(-0.8f, -0.2f, +0.0f),
                VanillaPolicies = new[]
                {
                    DistrictPolicies.Policies.EducationBoost,
                    DistrictPolicies.Policies.ExtraInsulation,
                    DistrictPolicies.Policies.FreeTransport
                },
                Modifiers = new PolicyModifiers
                {
                    TaxDeltaRes = -1, TaxDeltaCom = +2, TaxDeltaInd = +2, TaxDeltaOff = +1,
                    BudgetDeltaEducation     = +15,
                    BudgetDeltaHealth        = +10,
                    BudgetDeltaPolice        = 0,
                    BudgetDeltaTransport     = +10,
                    BudgetDeltaBeautification= +5,
                    BudgetDeltaGarbage       = +5,
                    BudgetDeltaWater         = +5,
                    BudgetDeltaElectricity   = +5,
                    BudgetDeltaFire          = +5,
                    BudgetDeltaRoads         = +5,
                    BudgetDeltaIndustry      = 0,
                    HappinessDelta = +3, PollutionMultiplier = 1.0f
                }
            },
            new PartyDef
            {
                Id = 2, ShortName = "LIB", FullName = "Liberal Centre",
                Color = new Color32(255, 193,   7, 255),
                Ideology = new Vector3(+0.1f, -0.1f, -0.4f),
                VanillaPolicies = new[]
                {
                    DistrictPolicies.Policies.BigBusiness,
                    DistrictPolicies.Policies.HighTechHousing,
                    DistrictPolicies.Policies.Recycling
                },
                Modifiers = new PolicyModifiers
                {
                    TaxDeltaRes = 0, TaxDeltaCom = 0, TaxDeltaInd = 0, TaxDeltaOff = -1,
                    BudgetDeltaEducation     = +5,
                    BudgetDeltaHealth        = +5,
                    BudgetDeltaPolice        = +5,
                    BudgetDeltaTransport     = 0,
                    BudgetDeltaBeautification= 0,
                    BudgetDeltaGarbage       = 0,
                    BudgetDeltaWater         = 0,
                    BudgetDeltaElectricity   = 0,
                    BudgetDeltaFire          = 0,
                    BudgetDeltaRoads         = +5,
                    BudgetDeltaIndustry      = +5,
                    HappinessDelta = +1, PollutionMultiplier = 1.0f
                }
            },
            new PartyDef
            {
                Id = 3, ShortName = "CON", FullName = "Conservative",
                Color = new Color32( 33, 150, 243, 255),
                Ideology = new Vector3(+0.7f, +0.5f, +0.3f),
                VanillaPolicies = new[]
                {
                    DistrictPolicies.Policies.HeavyTrafficBan,
                },
                Modifiers = new PolicyModifiers
                {
                    TaxDeltaRes = -2, TaxDeltaCom = -2, TaxDeltaInd = -1, TaxDeltaOff = -2,
                    BudgetDeltaEducation     = -5,
                    BudgetDeltaHealth        = 0,
                    BudgetDeltaPolice        = +10,
                    BudgetDeltaTransport     = -10,
                    BudgetDeltaBeautification= -10,
                    BudgetDeltaGarbage       = 0,
                    BudgetDeltaWater         = 0,
                    BudgetDeltaElectricity   = 0,
                    BudgetDeltaFire          = +5,
                    BudgetDeltaRoads         = +10,
                    BudgetDeltaIndustry      = +10,
                    HappinessDelta = 0, PollutionMultiplier = 1.05f
                }
            },
            new PartyDef
            {
                Id = 4, ShortName = "POP", FullName = "Populist Movement",
                Color = new Color32(156,  39, 176, 255),
                Ideology = new Vector3(+0.2f, +0.8f, +0.7f),
                VanillaPolicies = new[]
                {
                    DistrictPolicies.Policies.OldTown,
                    DistrictPolicies.Policies.HeavyTrafficBan,
                },
                Modifiers = new PolicyModifiers
                {
                    TaxDeltaRes = -1, TaxDeltaCom = -1, TaxDeltaInd = -1, TaxDeltaOff = -1,
                    BudgetDeltaEducation     = 0,
                    BudgetDeltaHealth        = +5,
                    BudgetDeltaPolice        = +15,
                    BudgetDeltaTransport     = -5,
                    BudgetDeltaBeautification= -5,
                    BudgetDeltaGarbage       = +5,
                    BudgetDeltaWater         = +5,
                    BudgetDeltaElectricity   = +5,
                    BudgetDeltaFire          = +10,
                    BudgetDeltaRoads         = +10,
                    BudgetDeltaIndustry      = 0,
                    HappinessDelta = -1, PollutionMultiplier = 1.1f
                }
            },
            new PartyDef
            {
                Id = 5, ShortName = "SOC", FullName = "Democratic Socialists",
                Color = new Color32(239,  83,  80, 255),
                Ideology = new Vector3(-0.9f, -0.5f, +0.2f),
                VanillaPolicies = new[]
                {
                    DistrictPolicies.Policies.FreeTransport,
                    DistrictPolicies.Policies.EducationBoost,
                    DistrictPolicies.Policies.ExtraInsulation
                },
                Modifiers = new PolicyModifiers
                {
                    TaxDeltaRes = -1, TaxDeltaCom = +3, TaxDeltaInd = +3, TaxDeltaOff = +2,
                    BudgetDeltaEducation     = +20,
                    BudgetDeltaHealth        = +15,
                    BudgetDeltaPolice        = -5,
                    BudgetDeltaTransport     = +20,
                    BudgetDeltaBeautification= +10,
                    BudgetDeltaGarbage       = +10,
                    BudgetDeltaWater         = +10,
                    BudgetDeltaElectricity   = +10,
                    BudgetDeltaFire          = +5,
                    BudgetDeltaRoads         = 0,
                    BudgetDeltaIndustry      = -5,
                    HappinessDelta = +4, PollutionMultiplier = 0.95f
                }
            },
        };

        public static int PartyCount()
        {
            return Parties.Length;
        }
    }

    // ========================================================================
    //  RUNTIME CONFIG — editable in-game via the politics panel, persisted in
    //  savegames. Use these instead of the Default* constants when reading
    //  election timings at simulation time.
    // ========================================================================
    public static class RuntimeConfig
    {
        public static float TermLengthDays         = Config.DefaultTermLengthDays;
        public static float CampaignLengthDays     = Config.DefaultCampaignLengthDays;
        public static float ReElectionCooldownDays = Config.DefaultReElectionCooldownDays;

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

    // ========================================================================
    //  VOTER TRAITS — how citizen demographics bias voting on the economic
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

    // ========================================================================
    //  DATA MODELS
    // ========================================================================

    public class PartyDef
    {
        public int Id;
        public string ShortName;
        public string FullName;
        public Color32 Color;
        public Vector3 Ideology;                // -1..+1 per axis
        public DistrictPolicies.Policies[] VanillaPolicies;
        public PolicyModifiers Modifiers;
    }

    public class PolicyModifiers
    {
        // Tax deltas (percentage points)
        public int TaxDeltaRes, TaxDeltaCom, TaxDeltaInd, TaxDeltaOff;

        // Budget deltas (percentage points). CS1 budgets use a 50..150 scale
        // (percentage of baseline), so deltas stack onto that.
        public int BudgetDeltaElectricity;
        public int BudgetDeltaWater;
        public int BudgetDeltaGarbage;
        public int BudgetDeltaHealth;
        public int BudgetDeltaFire;
        public int BudgetDeltaPolice;
        public int BudgetDeltaEducation;
        public int BudgetDeltaTransport;       // public transit
        public int BudgetDeltaBeautification;  // parks
        public int BudgetDeltaRoads;
        public int BudgetDeltaIndustry;

        public int HappinessDelta;          // informational
        public float PollutionMultiplier;   // informational
    }

    /// <summary>Per-election results record.</summary>
    public class ElectionResult : IDataContainer
    {
        public int Year;
        public int Month;
        public int[] SeatsByParty = new int[Config.PartyCount()];
        public float[] VoteShareByParty = new float[Config.PartyCount()];
        public float Turnout;
        public List<int> CoalitionPartyIds = new List<int>();
        public int[] ApprovalByParty = new int[Config.PartyCount()]; // 0..100

        // v5: per-grievance vote tallies. Index = (int)Grievance.
        // Grievance.None captures "pure ideology" votes.
        public int[] VotesByGrievance = new int[9];

        // v6: demographic cross-tabs — votes by (bucket, party).
        // Age buckets: 0=Young, 1=Adult, 2=Senior (children/teens don't vote).
        public int[,] VotesByAgeParty;
        // Education: 0=Uneducated, 1=Educated, 2=WellEducated, 3=HighlyEducated.
        public int[,] VotesByEduParty;
        // Wealth: 0=Low, 1=Medium, 2=High.
        public int[,] VotesByWealthParty;

        public void Serialize(DataSerializer s)
        {
            s.WriteInt32(Year);
            s.WriteInt32(Month);
            s.WriteInt32(SeatsByParty.Length);
            for (int i = 0; i < SeatsByParty.Length; i++) s.WriteInt32(SeatsByParty[i]);
            for (int i = 0; i < VoteShareByParty.Length; i++) s.WriteFloat(VoteShareByParty[i]);
            s.WriteFloat(Turnout);
            s.WriteInt32(CoalitionPartyIds.Count);
            foreach (var id in CoalitionPartyIds) s.WriteInt32(id);
            for (int i = 0; i < ApprovalByParty.Length; i++) s.WriteInt32(ApprovalByParty[i]);
            // v5 tail
            s.WriteInt32(VotesByGrievance.Length);
            for (int i = 0; i < VotesByGrievance.Length; i++) s.WriteInt32(VotesByGrievance[i]);

            // v6: demographic cross-tabs (age × party, edu × party, wealth × party).
            // Written as (bucketCount, partyCount, flattened ints).
            WriteMatrix(s, VotesByAgeParty,    3);
            WriteMatrix(s, VotesByEduParty,    4);
            WriteMatrix(s, VotesByWealthParty, 3);
        }

        private static void WriteMatrix(DataSerializer s, int[,] m, int fallbackBuckets)
        {
            int rows = m != null ? m.GetLength(0) : fallbackBuckets;
            int cols = m != null ? m.GetLength(1) : 0;
            s.WriteInt32(rows);
            s.WriteInt32(cols);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    s.WriteInt32(m[r, c]);
        }

        private static int[,] ReadMatrix(DataSerializer s)
        {
            int rows = s.ReadInt32();
            int cols = s.ReadInt32();
            var m = new int[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    m[r, c] = s.ReadInt32();
            return m;
        }

        public void Deserialize(DataSerializer s)
        {
            Year  = s.ReadInt32();
            Month = s.ReadInt32();
            int n = s.ReadInt32();
            SeatsByParty     = new int[n];
            VoteShareByParty = new float[n];
            ApprovalByParty  = new int[n];
            for (int i = 0; i < n; i++) SeatsByParty[i]     = s.ReadInt32();
            for (int i = 0; i < n; i++) VoteShareByParty[i] = s.ReadFloat();
            Turnout = s.ReadFloat();
            int c = s.ReadInt32();
            CoalitionPartyIds = new List<int>(c);
            for (int i = 0; i < c; i++) CoalitionPartyIds.Add(s.ReadInt32());
            for (int i = 0; i < n; i++) ApprovalByParty[i]  = s.ReadInt32();
            // v5 tail (optional — old saves don't have it)
            if (s.version >= 5)
            {
                int gn = s.ReadInt32();
                VotesByGrievance = new int[gn];
                for (int i = 0; i < gn; i++) VotesByGrievance[i] = s.ReadInt32();
            }
            else
            {
                VotesByGrievance = new int[9];
            }

            // v6 tail — demographic cross-tabs
            if (s.version >= 6)
            {
                VotesByAgeParty    = ReadMatrix(s);
                VotesByEduParty    = ReadMatrix(s);
                VotesByWealthParty = ReadMatrix(s);
            }
        }

        public void AfterDeserialize(DataSerializer s) { }
    }

    public static class ConfigEx
    {
        public static int PartyCount() { return Config.Parties.Length; }
    }

    // Hack: C# 3.5 doesn't allow calling Config.Parties.Length in a const context
    // for ElectionResult's array init, so we route through this helper.
    public static class PartyCountRef
    {
        public static int Value { get { return Config.Parties.Length; } }
    }

    // ========================================================================
    //  STATE — central singleton holding the live simulation state.
    // ========================================================================
    public enum ElectionPhase
    {
        Idle,           // waiting for next term
        Campaign,       // campaign running, voters drifting toward parties
        Voting,         // voting day (instant tally)
        Forming,        // coalition negotiations
        Governing,      // coalition in place, governing
        Failed          // no coalition possible — cooldown before re-election
    }

    public enum OverlayMode
    {
        Off = 0,
        Party = 1,
        Turnout = 2,
        Satisfaction = 3
    }

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

        // Policies currently applied by the coalition — so we can revert on coalition change.
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

    // ========================================================================
    //  IUserMod ENTRY POINT
    // ========================================================================
    public class PoliticsUserMod : ICities.IUserMod
    {
        public string Name        { get { return "Politics & Elections Mod"; } }
        public string Description { get { return "Citizens elect a parliament; coalitions shape city policies. Press Ctrl+P to open panel."; } }

        public void OnEnabled()
        {
            Log("OnEnabled");
            HarmonyPatcher.PatchAll();
        }

        public void OnDisabled()
        {
            Log("OnDisabled");
            HarmonyPatcher.UnpatchAll();
        }

        public void OnSettingsUI(UIHelperBase helper)
        {
            // Minimal options UI — most tuning lives in Config at top of file.
            var group = helper.AddGroup("Politics & Elections");
            group.AddCheckbox("Enable debug logging", DebugFlags.Verbose, v => DebugFlags.Verbose = v);
            group.AddButton("Force election now", () => {
                if (PoliticsState.Instance != null && PoliticsState.Instance.Initialized)
                    ElectionEngine.TriggerCampaign(force: true);
            });
            group.AddButton("Reset political state", () => {
                if (PoliticsState.Instance != null)
                {
                    PoliticsState.Instance.History.Clear();
                    PoliticsState.Instance.CurrentSeats = new int[PartyCountRef.Value];
                    PoliticsState.Instance.CoalitionPartyIds.Clear();
                    PoliticsState.Instance.Phase = ElectionPhase.Idle;
                    PoliticsState.Instance.DaysSinceLastElection = 0f;
                }
            });
        }

        public static void Log(string msg)
        {
            if (DebugFlags.Verbose) Debug.Log(Config.LogPrefix + msg);
        }
    }

    public static class DebugFlags
    {
        public static bool Verbose = true;
        // When true, only "essential" chirps go out (campaign start, election
        // winner, bill passage). Party slogans and crisis quips are suppressed.
        public static bool MinimalChirps = false;
    }

    // ========================================================================
    //  UI HELPERS — reusable bits (draggable windows, etc.)
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

    // ========================================================================
    //  HARMONY PATCHER — tints buildings natively when overlay is active.
    //
    //  We use a BuildingAI.GetColor prefix patch. When our overlay mode is
    //  something other than Off, we compute the color for residential buildings
    //  based on that building's cached per-building data, and short-circuit the
    //  original method by assigning to __result + returning false.
    //
    //  The patch is only effective when:
    //    * CitiesHarmony is installed and loaded (provides 0Harmony.dll)
    //    * The overlay is enabled
    //    * The InfoManager is in a neutral mode (None) — otherwise we defer to
    //      the vanilla info view so Education/Crime/etc. still work.
    // ========================================================================
    public static class HarmonyPatcher
    {
        private const string HarmonyId = "com.shadijih.politicsmod";
        private static bool _patched = false;

        public static bool IsPatched { get { return _patched; } }

        public static void PatchAll()
        {
            if (_patched) return;
            try
            {
                var harmony = new Harmony(HarmonyId);

                // First: apply attribute-declared patches (base BuildingAI.GetColor).
                harmony.PatchAll(typeof(HarmonyPatcher).Assembly);

                // BuildingAI.GetColor is virtual and OVERRIDDEN in many subclasses
                // (ResidentialBuildingAI, LowResidentialBuildingAI, etc.). A
                // patch on the base is NOT inherited — we must patch each
                // subclass method individually.
                var prefix = typeof(BuildingAI_GetColor_Patch).GetMethod("Prefix",
                    BindingFlags.Public | BindingFlags.Static);

                var hmPrefix = new HarmonyMethod(prefix);
                int count = 0;
                foreach (var t in typeof(BuildingAI).Assembly.GetTypes())
                {
                    if (!typeof(BuildingAI).IsAssignableFrom(t)) continue;
                    if (t == typeof(BuildingAI)) continue; // already patched via attribute
                    if (t.IsAbstract) continue;
                    // Find THIS type's own declared GetColor, not the inherited one.
                    var method = t.GetMethod(
                        "GetColor",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    if (method == null) continue;
                    try
                    {
                        harmony.Patch(method, hmPrefix);
                        count++;
                    }
                    catch (Exception pe)
                    {
                        PoliticsUserMod.Log("Could not patch " + t.Name + ".GetColor: " + pe.Message);
                    }
                }

                _patched = true;
                PoliticsUserMod.Log("Harmony patches applied (" + count + " BuildingAI subclass overrides).");
            }
            catch (Exception e)
            {
                Debug.LogError(Config.LogPrefix + "Harmony patch failed (is CitiesHarmony installed?): " + e);
            }
        }

        public static void UnpatchAll()
        {
            if (!_patched) return;
            try
            {
                var harmony = new Harmony(HarmonyId);
                harmony.UnpatchAll(HarmonyId);
                _patched = false;
                PoliticsUserMod.Log("Harmony patches removed.");
            }
            catch (Exception e)
            {
                Debug.LogError(Config.LogPrefix + "Harmony unpatch failed: " + e);
            }
        }

        /// <summary>
        /// Force the game to re-color all buildings. Call this when the overlay
        /// mode changes so tints update immediately.
        /// </summary>
        public static void RefreshBuildingColors()
        {
            try
            {
                var bm = Singleton<BuildingManager>.instance;
                uint n = bm.m_buildings.m_size;
                for (ushort i = 1; i < n; i++)
                {
                    var b = bm.m_buildings.m_buffer[i];
                    if ((b.m_flags & Building.Flags.Created) == 0) continue;
                    if (b.Info == null) continue;
                    if (b.Info.GetService() != ItemClass.Service.Residential) continue;
                    bm.UpdateBuildingColors(i);
                }
            }
            catch (Exception e)
            {
                PoliticsUserMod.Log("RefreshBuildingColors failed: " + e.Message);
            }
        }

        /// <summary>
        /// Cycle the politics overlay (Off → Party → Turnout → Satisfaction → Off).
        ///
        /// To get the full info-view experience (dimmed other buildings, legend
        /// panel, etc.), we use a three-pronged approach:
        ///   1. SetCurrentMode(Density, Default)  — flips the engine state
        ///   2. Find and "click" the vanilla Population info-view button if
        ///      we can locate it, to trigger the UI chrome
        ///   3. Fallback: directly toggle the info-view panel components
        ///
        /// Our Harmony patch on BuildingAI.GetColor overrides the coloring
        /// while our overlay state is non-Off.
        /// </summary>
        public static void CycleOverlayAndSync()
        {
            var st = PoliticsState.Instance;
            if (st == null) return;

            OverlayMode prev = st.Overlay;
            int next = ((int)st.Overlay + 1) % 4;
            st.Overlay = (OverlayMode)next;

            try
            {
                if (st.Overlay == OverlayMode.Off)
                {
                    // Exit the info view entirely
                    ExitInfoView();
                }
                else if (prev == OverlayMode.Off)
                {
                    // First transition Off → Party: enter info view
                    EnterDensityInfoView();
                }
                // Subsequent cycles just change our overlay data; no UI change needed.
            }
            catch (Exception e)
            {
                PoliticsUserMod.Log("Info view sync failed: " + e.Message);
            }

            RefreshBuildingColors();
        }

        private static void EnterDensityInfoView()
        {
            // 1. Engine state
            var im = InfoManager.instance;
            if (im != null)
            {
                im.SetCurrentMode(InfoManager.InfoMode.Density, InfoManager.SubInfoMode.Default);
            }

            // 2. Try to open the vanilla info-views panel so the legend shows up.
            //    The panel is named "InfoViewsPanel" (or "InfoPanel" in some builds).
            try
            {
                var view = UIView.GetAView();
                if (view != null)
                {
                    var infoPanel = view.FindUIComponent("InfoViewsPanel");
                    if (infoPanel != null)
                    {
                        // Not opening/closing the panel itself — just making sure
                        // the info mode is live. The panel chrome follows.
                        PoliticsUserMod.Log("InfoViewsPanel located (" + infoPanel.name + ")");
                    }
                }
            }
            catch (Exception e)
            {
                PoliticsUserMod.Log("UI probe failed: " + e.Message);
            }
            PoliticsUserMod.Log("Entered Density info view (overlay=" +
                PoliticsState.Instance.Overlay + ")");
        }

        private static void ExitInfoView()
        {
            var im = InfoManager.instance;
            if (im != null)
            {
                im.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
            }
            PoliticsUserMod.Log("Exited info view.");
        }
    }

    /// <summary>
    /// Harmony prefix on BuildingAI.GetColor. When the overlay is active and
    /// the building is residential, returns our custom color and skips the
    /// original method.
    /// </summary>
    [HarmonyPatch(typeof(BuildingAI), "GetColor")]
    public static class BuildingAI_GetColor_Patch
    {
        public static bool Prefix(BuildingAI __instance, ushort buildingID, ref Building data,
                                  InfoManager.InfoMode infoMode, ref Color __result)
        {
            var st = PoliticsState.Instance;
            if (st == null || !st.Initialized) return true;
            if (st.Overlay == OverlayMode.Off)  return true;

            // We piggyback on the vanilla "Density" (Population) info view.
            // When our overlay is active, we've switched InfoManager to Density
            // and hijack its coloring. Any other info mode: leave alone.
            if (infoMode != InfoManager.InfoMode.Density) return true;

            // Only color residential buildings (matches our data coverage).
            var info = data.Info;
            if (info == null) return true;
            if (info.GetService() != ItemClass.Service.Residential) return true;

            if (st.DominantPartyByBuilding == null) return true;
            if (buildingID >= st.DominantPartyByBuilding.Length) return true;

            // Neutral "no data" color for residential buildings that have no
            // voter data yet (built after last election, not sampled, etc.).
            // This keeps the info-view consistent — no vanilla population
            // colors leaking through.
            Color noData = new Color(0.35f, 0.35f, 0.4f, 1f);

            Color c;
            bool show;
            switch (st.Overlay)
            {
                case OverlayMode.Party:
                {
                    byte pid = st.DominantPartyByBuilding[buildingID];
                    if (pid >= PartyCountRef.Value)
                    {
                        __result = noData;
                        return false; // no data — show neutral
                    }
                    c = (Color)Config.Parties[pid].Color;
                    show = true;
                    break;
                }
                case OverlayMode.Turnout:
                {
                    byte t = st.TurnoutByBuilding[buildingID];
                    if (t == 0) { __result = noData; return false; }
                    c = Color.Lerp(new Color(0.7f, 0.1f, 0.1f), new Color(0.1f, 0.8f, 0.1f), t / 100f);
                    show = true;
                    break;
                }
                case OverlayMode.Satisfaction:
                {
                    byte sa = st.SatisfactionByBuilding[buildingID];
                    if (sa == 0) { __result = noData; return false; }
                    c = Color.Lerp(new Color(0.7f, 0.1f, 0.1f), new Color(0.2f, 0.7f, 0.9f), sa / 100f);
                    show = true;
                    break;
                }
                default:
                    return true;
            }

            if (!show) return true;

            // Boost saturation/brightness so it reads well on residential geometry.
            c.a = 1f;
            __result = c;
            return false; // skip original
        }
    }

    // ========================================================================
    //  LOADING EXTENSION — wires everything up when a city loads.
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

    // ========================================================================
    //  SERIALIZABLE DATA EXTENSION — persists political state in savegames.
    // ========================================================================
    public class PoliticsSerialization : SerializableDataExtensionBase
    {
        public const string DataId = "PoliticsMod.v1";
        public const uint   DataVersion = 6;

        public override void OnLoadData()
        {
            base.OnLoadData();
            if (PoliticsState.Instance == null) PoliticsState.Instance = new PoliticsState();

            byte[] bytes = serializableDataManager.LoadData(DataId);
            if (bytes == null || bytes.Length == 0)
            {
                PoliticsUserMod.Log("No saved politics data; starting fresh.");
                return;
            }
            try
            {
                using (var stream = new MemoryStream(bytes))
                {
                    var s = DataSerializer.Deserialize<PoliticsSaveBlob>(stream, DataSerializer.Mode.Memory);
                    s.Apply(PoliticsState.Instance);
                    PoliticsUserMod.Log("Loaded politics state (" + bytes.Length + " bytes) | " +
                        "Day=" + (int)PoliticsState.Instance.DaysSinceLastElection +
                        "/" + (int)RuntimeConfig.TermLengthDays +
                        ", Phase=" + PoliticsState.Instance.Phase +
                        ", Elections=" + PoliticsState.Instance.History.Count);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(Config.LogPrefix + "Failed to load save data: " + e);
            }
        }

        public override void OnSaveData()
        {
            base.OnSaveData();
            if (PoliticsState.Instance == null) return;

            try
            {
                var blob = PoliticsSaveBlob.From(PoliticsState.Instance);
                byte[] bytes;
                using (var stream = new MemoryStream())
                {
                    DataSerializer.Serialize(stream, DataSerializer.Mode.Memory, DataVersion, blob);
                    bytes = stream.ToArray();
                }
                serializableDataManager.SaveData(DataId, bytes);
                PoliticsUserMod.Log("Saved politics state (" + bytes.Length + " bytes) | " +
                    "Day=" + (int)PoliticsState.Instance.DaysSinceLastElection +
                    "/" + (int)RuntimeConfig.TermLengthDays +
                    ", Phase=" + PoliticsState.Instance.Phase +
                    ", Elections=" + PoliticsState.Instance.History.Count);
            }
            catch (Exception e)
            {
                Debug.LogError(Config.LogPrefix + "Failed to save data: " + e);
            }
        }
    }

    /// <summary>Flat serializable wrapper over PoliticsState (no Unity refs).</summary>
    public class PoliticsSaveBlob : IDataContainer
    {
        public float DaysSinceLastElection;
        public float DaysSinceCampaignStart;
        public int Phase;
        public int[] CurrentSeats;
        public float[] CurrentSupport;
        public List<int> CoalitionPartyIds = new List<int>();
        public int[] ApprovalByParty;
        public List<ElectionResult> History = new List<ElectionResult>();
        public bool PoliciesApplied;
        public List<int> AppliedVanillaPolicies = new List<int>();
        public int Overlay;
        public float FailedCooldownRemaining;
        // v2: runtime-editable timings
        public float RcTermLengthDays;
        public float RcCampaignLengthDays;
        public float RcReElectionCooldownDays;
        // v3: persisted party list (overrides Config.Parties defaults)
        public List<PartyBlob> Parties = new List<PartyBlob>();
        // v4: voter trait biases
        public float[] VoterBiases; // 14 values in fixed order — see ApplyToTraits / CaptureTraits
        // v6: UI preferences
        public bool MinimalChirps;

        public static PoliticsSaveBlob From(PoliticsState st)
        {
            var b = new PoliticsSaveBlob
            {
                DaysSinceLastElection  = st.DaysSinceLastElection,
                DaysSinceCampaignStart = st.DaysSinceCampaignStart,
                Phase           = (int)st.Phase,
                CurrentSeats    = (int[])st.CurrentSeats.Clone(),
                CurrentSupport  = (float[])st.CurrentSupport.Clone(),
                CoalitionPartyIds = new List<int>(st.CoalitionPartyIds),
                ApprovalByParty = (int[])st.ApprovalByParty.Clone(),
                History         = new List<ElectionResult>(st.History),
                PoliciesApplied = st.PoliciesApplied,
                Overlay         = (int)st.Overlay,
                FailedCooldownRemaining = st.FailedCooldownRemaining,
                RcTermLengthDays         = RuntimeConfig.TermLengthDays,
                RcCampaignLengthDays     = RuntimeConfig.CampaignLengthDays,
                RcReElectionCooldownDays = RuntimeConfig.ReElectionCooldownDays,
            };
            foreach (var p in st.AppliedVanillaPolicies) b.AppliedVanillaPolicies.Add((int)p);
            // v3: capture parties
            b.Parties = new List<PartyBlob>();
            foreach (var p in Config.Parties) b.Parties.Add(PartyBlob.FromParty(p));
            // v4: capture voter trait biases
            b.VoterBiases = CaptureVoterBiases();
            b.MinimalChirps = DebugFlags.MinimalChirps;
            return b;
        }

        private static float[] CaptureVoterBiases()
        {
            return new float[]
            {
                VoterTraits.BiasEduUneducated,
                VoterTraits.BiasEduEducated,
                VoterTraits.BiasEduWellEducated,
                VoterTraits.BiasEduHighlyEducated,
                VoterTraits.BiasWealthLow,
                VoterTraits.BiasWealthMedium,
                VoterTraits.BiasWealthHigh,
                VoterTraits.BiasEmployed,
                VoterTraits.BiasUnemployed,
                VoterTraits.BiasYoung,
                VoterTraits.BiasAdult,
                VoterTraits.BiasSenior,
                VoterTraits.BiasSick,
                VoterTraits.BiasHighPollution,
            };
        }

        private static void ApplyVoterBiases(float[] v)
        {
            if (v == null || v.Length < 14) return;
            VoterTraits.BiasEduUneducated     = v[0];
            VoterTraits.BiasEduEducated       = v[1];
            VoterTraits.BiasEduWellEducated   = v[2];
            VoterTraits.BiasEduHighlyEducated = v[3];
            VoterTraits.BiasWealthLow         = v[4];
            VoterTraits.BiasWealthMedium      = v[5];
            VoterTraits.BiasWealthHigh        = v[6];
            VoterTraits.BiasEmployed          = v[7];
            VoterTraits.BiasUnemployed        = v[8];
            VoterTraits.BiasYoung             = v[9];
            VoterTraits.BiasAdult             = v[10];
            VoterTraits.BiasSenior            = v[11];
            VoterTraits.BiasSick              = v[12];
            VoterTraits.BiasHighPollution     = v[13];
        }

        public void Apply(PoliticsState st)
        {
            st.DaysSinceLastElection  = DaysSinceLastElection;
            st.DaysSinceCampaignStart = DaysSinceCampaignStart;
            st.Phase           = (ElectionPhase)Phase;
            st.CurrentSeats    = CurrentSeats    ?? new int[PartyCountRef.Value];
            st.CurrentSupport  = CurrentSupport  ?? new float[PartyCountRef.Value];
            st.ApprovalByParty = ApprovalByParty ?? new int[PartyCountRef.Value];
            st.CoalitionPartyIds = CoalitionPartyIds ?? new List<int>();
            st.History         = History ?? new List<ElectionResult>();
            st.PoliciesApplied = PoliciesApplied;
            st.Overlay         = (OverlayMode)Overlay;
            st.FailedCooldownRemaining = FailedCooldownRemaining;
            st.AppliedVanillaPolicies.Clear();
            if (AppliedVanillaPolicies != null)
                foreach (var i in AppliedVanillaPolicies)
                    st.AppliedVanillaPolicies.Add((DistrictPolicies.Policies)i);

            // Push persisted runtime config back into RuntimeConfig (only if we
            // actually loaded values, i.e. v2+ saves — otherwise leave defaults).
            if (RcTermLengthDays     > 0f) RuntimeConfig.TermLengthDays         = RcTermLengthDays;
            if (RcCampaignLengthDays > 0f) RuntimeConfig.CampaignLengthDays     = RcCampaignLengthDays;
            if (RcReElectionCooldownDays >= 0f) RuntimeConfig.ReElectionCooldownDays = RcReElectionCooldownDays;
            RuntimeConfig.ClampAll();

            // v3: restore parties (overrides Config.Parties defaults)
            if (Parties != null && Parties.Count > 0)
            {
                var arr = new PartyDef[Parties.Count];
                for (int i = 0; i < Parties.Count; i++)
                {
                    arr[i] = Parties[i].ToParty();
                    arr[i].Id = i; // enforce contiguous ids
                }
                Config.Parties = arr;
                // Resize per-party arrays on the state to match.
                int n = arr.Length;
                if (st.CurrentSeats    == null || st.CurrentSeats.Length    != n) st.CurrentSeats    = ResizeIntArrayInt(st.CurrentSeats, n);
                if (st.CurrentSupport  == null || st.CurrentSupport.Length  != n) st.CurrentSupport  = ResizeFloatArrayInt(st.CurrentSupport, n);
                if (st.ApprovalByParty == null || st.ApprovalByParty.Length != n) st.ApprovalByParty = ResizeIntArrayInt(st.ApprovalByParty, n);
                if (st.CoalitionPartyIds != null) st.CoalitionPartyIds.RemoveAll(id => id < 0 || id >= n);
            }

            // v4: restore voter biases
            if (VoterBiases != null && VoterBiases.Length >= 14) ApplyVoterBiases(VoterBiases);
            DebugFlags.MinimalChirps = MinimalChirps;

            if (st.History.Count > 0) st.LastResult = st.History[st.History.Count - 1];
        }

        private static int[] ResizeIntArrayInt(int[] src, int len)
        {
            var dst = new int[len];
            if (src != null) { int c = Math.Min(src.Length, len); for (int i = 0; i < c; i++) dst[i] = src[i]; }
            return dst;
        }
        private static float[] ResizeFloatArrayInt(float[] src, int len)
        {
            var dst = new float[len];
            if (src != null) { int c = Math.Min(src.Length, len); for (int i = 0; i < c; i++) dst[i] = src[i]; }
            return dst;
        }

        public void Serialize(DataSerializer s)
        {
            s.WriteFloat(DaysSinceLastElection);
            s.WriteFloat(DaysSinceCampaignStart);
            s.WriteInt32(Phase);

            s.WriteInt32(CurrentSeats.Length);
            for (int i = 0; i < CurrentSeats.Length; i++) s.WriteInt32(CurrentSeats[i]);
            for (int i = 0; i < CurrentSupport.Length; i++) s.WriteFloat(CurrentSupport[i]);
            for (int i = 0; i < ApprovalByParty.Length; i++) s.WriteInt32(ApprovalByParty[i]);

            s.WriteInt32(CoalitionPartyIds.Count);
            foreach (var id in CoalitionPartyIds) s.WriteInt32(id);

            s.WriteInt32(History.Count);
            foreach (var r in History) r.Serialize(s);

            s.WriteBool(PoliciesApplied);
            s.WriteInt32(AppliedVanillaPolicies.Count);
            foreach (var p in AppliedVanillaPolicies) s.WriteInt32(p);

            s.WriteInt32(Overlay);
            s.WriteFloat(FailedCooldownRemaining);

            // v2 additions
            if (s.version >= 2)
            {
                s.WriteFloat(RcTermLengthDays);
                s.WriteFloat(RcCampaignLengthDays);
                s.WriteFloat(RcReElectionCooldownDays);
            }

            // v3 additions: party list
            if (s.version >= 3)
            {
                int pc = Parties != null ? Parties.Count : 0;
                s.WriteInt32(pc);
                for (int i = 0; i < pc; i++) Parties[i].Serialize(s);
            }

            // v4 additions: voter trait biases
            if (s.version >= 4)
            {
                var vb = VoterBiases ?? new float[14];
                s.WriteInt32(vb.Length);
                for (int i = 0; i < vb.Length; i++) s.WriteFloat(vb[i]);
            }

            // v6 additions: UI prefs
            if (s.version >= 6)
            {
                s.WriteBool(MinimalChirps);
            }
        }

        public void Deserialize(DataSerializer s)
        {
            DaysSinceLastElection  = s.ReadFloat();
            DaysSinceCampaignStart = s.ReadFloat();
            Phase = s.ReadInt32();

            int n = s.ReadInt32();
            CurrentSeats    = new int[n];
            CurrentSupport  = new float[n];
            ApprovalByParty = new int[n];
            for (int i = 0; i < n; i++) CurrentSeats[i]    = s.ReadInt32();
            for (int i = 0; i < n; i++) CurrentSupport[i]  = s.ReadFloat();
            for (int i = 0; i < n; i++) ApprovalByParty[i] = s.ReadInt32();

            int c = s.ReadInt32();
            CoalitionPartyIds = new List<int>(c);
            for (int i = 0; i < c; i++) CoalitionPartyIds.Add(s.ReadInt32());

            int h = s.ReadInt32();
            History = new List<ElectionResult>(h);
            for (int i = 0; i < h; i++)
            {
                var r = new ElectionResult();
                r.Deserialize(s);
                History.Add(r);
            }

            PoliciesApplied = s.ReadBool();
            int ap = s.ReadInt32();
            AppliedVanillaPolicies = new List<int>(ap);
            for (int i = 0; i < ap; i++) AppliedVanillaPolicies.Add(s.ReadInt32());

            Overlay = s.ReadInt32();
            FailedCooldownRemaining = s.ReadFloat();

            // v2 additions
            if (s.version >= 2)
            {
                RcTermLengthDays         = s.ReadFloat();
                RcCampaignLengthDays     = s.ReadFloat();
                RcReElectionCooldownDays = s.ReadFloat();
            }
            else
            {
                RcTermLengthDays         = -1f;
                RcCampaignLengthDays     = -1f;
                RcReElectionCooldownDays = -1f;
            }

            // v3 additions
            if (s.version >= 3)
            {
                int pc = s.ReadInt32();
                Parties = new List<PartyBlob>(pc);
                for (int i = 0; i < pc; i++)
                {
                    var pb = new PartyBlob();
                    pb.Deserialize(s);
                    Parties.Add(pb);
                }
            }
            else
            {
                Parties = new List<PartyBlob>();
            }

            // v4 additions: voter trait biases
            if (s.version >= 4)
            {
                int vl = s.ReadInt32();
                VoterBiases = new float[vl];
                for (int i = 0; i < vl; i++) VoterBiases[i] = s.ReadFloat();
            }
            else
            {
                VoterBiases = null;
            }

            // v6: UI prefs
            if (s.version >= 6)
            {
                MinimalChirps = s.ReadBool();
            }
        }

        public void AfterDeserialize(DataSerializer s) { }
    }

    /// <summary>Serializable shape for a PartyDef — persisted in savegame (v3+).</summary>
    public class PartyBlob : IDataContainer
    {
        public int Id;
        public string ShortName;
        public string FullName;
        public byte R, G, B;
        public float IdX, IdY, IdZ;
        public int[] VanillaPolicies;
        public int TaxDeltaRes, TaxDeltaCom, TaxDeltaInd, TaxDeltaOff;
        public int BudgetDeltaElectricity, BudgetDeltaWater, BudgetDeltaGarbage;
        public int BudgetDeltaHealth, BudgetDeltaFire, BudgetDeltaPolice;
        public int BudgetDeltaEducation, BudgetDeltaTransport, BudgetDeltaBeautification;
        public int BudgetDeltaRoads, BudgetDeltaIndustry;
        public int HappinessDelta;
        public float PollutionMultiplier;

        public static PartyBlob FromParty(PartyDef p)
        {
            var b = new PartyBlob
            {
                Id = p.Id,
                ShortName = p.ShortName ?? "",
                FullName  = p.FullName  ?? "",
                R = p.Color.r, G = p.Color.g, B = p.Color.b,
                IdX = p.Ideology.x, IdY = p.Ideology.y, IdZ = p.Ideology.z,
                TaxDeltaRes = p.Modifiers.TaxDeltaRes,
                TaxDeltaCom = p.Modifiers.TaxDeltaCom,
                TaxDeltaInd = p.Modifiers.TaxDeltaInd,
                TaxDeltaOff = p.Modifiers.TaxDeltaOff,
                BudgetDeltaElectricity    = p.Modifiers.BudgetDeltaElectricity,
                BudgetDeltaWater          = p.Modifiers.BudgetDeltaWater,
                BudgetDeltaGarbage        = p.Modifiers.BudgetDeltaGarbage,
                BudgetDeltaHealth         = p.Modifiers.BudgetDeltaHealth,
                BudgetDeltaFire           = p.Modifiers.BudgetDeltaFire,
                BudgetDeltaPolice         = p.Modifiers.BudgetDeltaPolice,
                BudgetDeltaEducation      = p.Modifiers.BudgetDeltaEducation,
                BudgetDeltaTransport      = p.Modifiers.BudgetDeltaTransport,
                BudgetDeltaBeautification = p.Modifiers.BudgetDeltaBeautification,
                BudgetDeltaRoads          = p.Modifiers.BudgetDeltaRoads,
                BudgetDeltaIndustry       = p.Modifiers.BudgetDeltaIndustry,
                HappinessDelta            = p.Modifiers.HappinessDelta,
                PollutionMultiplier       = p.Modifiers.PollutionMultiplier,
            };
            var pols = p.VanillaPolicies ?? new DistrictPolicies.Policies[0];
            b.VanillaPolicies = new int[pols.Length];
            for (int i = 0; i < pols.Length; i++) b.VanillaPolicies[i] = (int)pols[i];
            return b;
        }

        public PartyDef ToParty()
        {
            var vpol = new DistrictPolicies.Policies[VanillaPolicies != null ? VanillaPolicies.Length : 0];
            for (int i = 0; i < vpol.Length; i++) vpol[i] = (DistrictPolicies.Policies)VanillaPolicies[i];
            return new PartyDef
            {
                Id = Id,
                ShortName = ShortName ?? ("P" + Id),
                FullName  = FullName  ?? ShortName,
                Color = new Color32(R, G, B, 255),
                Ideology = new Vector3(IdX, IdY, IdZ),
                VanillaPolicies = vpol,
                Modifiers = new PolicyModifiers
                {
                    TaxDeltaRes = TaxDeltaRes, TaxDeltaCom = TaxDeltaCom,
                    TaxDeltaInd = TaxDeltaInd, TaxDeltaOff = TaxDeltaOff,
                    BudgetDeltaElectricity    = BudgetDeltaElectricity,
                    BudgetDeltaWater          = BudgetDeltaWater,
                    BudgetDeltaGarbage        = BudgetDeltaGarbage,
                    BudgetDeltaHealth         = BudgetDeltaHealth,
                    BudgetDeltaFire           = BudgetDeltaFire,
                    BudgetDeltaPolice         = BudgetDeltaPolice,
                    BudgetDeltaEducation      = BudgetDeltaEducation,
                    BudgetDeltaTransport      = BudgetDeltaTransport,
                    BudgetDeltaBeautification = BudgetDeltaBeautification,
                    BudgetDeltaRoads          = BudgetDeltaRoads,
                    BudgetDeltaIndustry       = BudgetDeltaIndustry,
                    HappinessDelta            = HappinessDelta,
                    PollutionMultiplier       = PollutionMultiplier,
                }
            };
        }

        public void Serialize(DataSerializer s)
        {
            s.WriteInt32(Id);
            s.WriteSharedString(ShortName);
            s.WriteSharedString(FullName);
            s.WriteInt32(R); s.WriteInt32(G); s.WriteInt32(B);
            s.WriteFloat(IdX); s.WriteFloat(IdY); s.WriteFloat(IdZ);
            s.WriteInt32(VanillaPolicies != null ? VanillaPolicies.Length : 0);
            if (VanillaPolicies != null) foreach (var v in VanillaPolicies) s.WriteInt32(v);
            s.WriteInt32(TaxDeltaRes); s.WriteInt32(TaxDeltaCom);
            s.WriteInt32(TaxDeltaInd); s.WriteInt32(TaxDeltaOff);
            s.WriteInt32(BudgetDeltaElectricity); s.WriteInt32(BudgetDeltaWater);
            s.WriteInt32(BudgetDeltaGarbage);     s.WriteInt32(BudgetDeltaHealth);
            s.WriteInt32(BudgetDeltaFire);        s.WriteInt32(BudgetDeltaPolice);
            s.WriteInt32(BudgetDeltaEducation);   s.WriteInt32(BudgetDeltaTransport);
            s.WriteInt32(BudgetDeltaBeautification);
            s.WriteInt32(BudgetDeltaRoads);       s.WriteInt32(BudgetDeltaIndustry);
            s.WriteInt32(HappinessDelta);
            s.WriteFloat(PollutionMultiplier);
        }

        public void Deserialize(DataSerializer s)
        {
            Id = s.ReadInt32();
            ShortName = s.ReadSharedString();
            FullName  = s.ReadSharedString();
            R = (byte)s.ReadInt32(); G = (byte)s.ReadInt32(); B = (byte)s.ReadInt32();
            IdX = s.ReadFloat(); IdY = s.ReadFloat(); IdZ = s.ReadFloat();
            int pn = s.ReadInt32();
            VanillaPolicies = new int[pn];
            for (int i = 0; i < pn; i++) VanillaPolicies[i] = s.ReadInt32();
            TaxDeltaRes = s.ReadInt32(); TaxDeltaCom = s.ReadInt32();
            TaxDeltaInd = s.ReadInt32(); TaxDeltaOff = s.ReadInt32();
            BudgetDeltaElectricity = s.ReadInt32(); BudgetDeltaWater = s.ReadInt32();
            BudgetDeltaGarbage     = s.ReadInt32(); BudgetDeltaHealth = s.ReadInt32();
            BudgetDeltaFire        = s.ReadInt32(); BudgetDeltaPolice = s.ReadInt32();
            BudgetDeltaEducation   = s.ReadInt32(); BudgetDeltaTransport = s.ReadInt32();
            BudgetDeltaBeautification = s.ReadInt32();
            BudgetDeltaRoads       = s.ReadInt32(); BudgetDeltaIndustry = s.ReadInt32();
            HappinessDelta = s.ReadInt32();
            PollutionMultiplier = s.ReadFloat();
        }

        public void AfterDeserialize(DataSerializer s) { }
    }

    // ========================================================================
    //  THREADING EXTENSION — drives the simulation (timer + campaign drift).
    // ========================================================================
    public class PoliticsThreading : ThreadingExtensionBase
    {
        // One in-game day = SIM_FRAMES_PER_DAY simulation frames.
        // CS1's own day-night cycle uses 585 frames per "day" internally.
        // Simulation frames ONLY advance while the game is unpaused, AND they
        // advance proportional to the speed button (1x/2x/3x), so this
        // automatically gives us:
        //   * no progress while paused
        //   * faster terms when the player speeds up the game
        private const uint SIM_FRAMES_PER_DAY = 585;

        private uint _lastFrameIndex = 0;
        private bool _haveBaseline   = false;
        private float _daysSinceDeficitCheck = 0f;

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            var st = PoliticsState.Instance;
            if (st == null || !st.Initialized) return;

            var simMgr = SimulationManager.instance;
            if (simMgr == null) return;

            // Respect pause
            if (simMgr.SimulationPaused)
            {
                _haveBaseline = false; // re-baseline on resume
                return;
            }

            uint cur = simMgr.m_currentFrameIndex;
            if (!_haveBaseline)
            {
                _lastFrameIndex = cur;
                _haveBaseline   = true;
                return;
            }

            // Compute frames elapsed since last tick (handles wrap just in case)
            uint frameDelta = (cur >= _lastFrameIndex) ? (cur - _lastFrameIndex) : 0u;
            _lastFrameIndex = cur;
            if (frameDelta == 0u) return;

            // Clamp absurd deltas (e.g. after loading)
            if (frameDelta > SIM_FRAMES_PER_DAY * 30u) frameDelta = 0u;

            float dayDelta = frameDelta / (float)SIM_FRAMES_PER_DAY;
            if (dayDelta <= 0f) return;

            // Only run politics if city has grown enough
            int population = CitizenManagerUtil.GetPopulation();
            if (population < Config.MinPopulationForElections)
            {
                st.Phase = ElectionPhase.Idle;
                return;
            }

            // Handle failed-cooldown
            if (st.Phase == ElectionPhase.Failed)
            {
                st.FailedCooldownRemaining -= dayDelta;
                if (st.FailedCooldownRemaining <= 0f)
                {
                    st.FailedCooldownRemaining = 0f;
                    st.Phase = ElectionPhase.Idle;
                    st.DaysSinceLastElection = RuntimeConfig.TermLengthDays; // trigger campaign soon
                }
                return;
            }

            st.DaysSinceLastElection += dayDelta;

            // ---- Weekly deficit check + deficit-chirp pacing ----
            ElectionEngine.DaysSinceLastDeficitChirp += dayDelta;
            _daysSinceDeficitCheck += dayDelta;
            if (_daysSinceDeficitCheck >= 7f)
            {
                _daysSinceDeficitCheck = 0f;
                try
                {
                    var em = Singleton<EconomyManager>.instance;
                    long cash = em.LastCashAmount;
                    long incomePerWeek = cash - (ElectionEngine.LastCashSeen == long.MinValue
                                                 ? cash : ElectionEngine.LastCashSeen);
                    ElectionEngine.LastCashSeen = cash;
                    if (ElectionEngine.LastCashSeen != long.MinValue && incomePerWeek < 0)
                    {
                        ElectionEngine.DeficitWeeks++;
                    }
                    else
                    {
                        ElectionEngine.DeficitWeeks = 0;
                    }
                }
                catch { /* ignore — early frame before managers are ready */ }
            }
            // Maybe post a citizen deficit chirp
            if (ElectionEngine.DeficitWeeks > 0 &&
                ElectionEngine.DaysSinceLastDeficitChirp >= 10f &&
                !DebugFlags.MinimalChirps)
            {
                ElectionEngine.DaysSinceLastDeficitChirp = 0f;
                ElectionEngine.PostRandomCitizenDeficitChirp();
            }

            // Start campaign when term is near over
            if (st.Phase == ElectionPhase.Idle || st.Phase == ElectionPhase.Governing)
            {
                if (st.DaysSinceLastElection >= RuntimeConfig.TermLengthDays - RuntimeConfig.CampaignLengthDays)
                {
                    ElectionEngine.TriggerCampaign(force: false);
                }
            }

            if (st.Phase == ElectionPhase.Campaign)
            {
                st.DaysSinceCampaignStart += dayDelta;
                ElectionEngine.DriftCampaign(dayDelta);
                if (st.DaysSinceCampaignStart >= RuntimeConfig.CampaignLengthDays)
                {
                    ElectionEngine.RunElection();
                }
            }
        }
    }

    // Utility: population via CitizenManager
    public static class CitizenManagerUtil
    {
        public static int GetPopulation()
        {
            try
            {
                var dm = Singleton<DistrictManager>.instance;
                return (int)dm.m_districts.m_buffer[0].m_populationData.m_finalCount;
            }
            catch { return 0; }
        }
    }

    // ========================================================================
    //  GRIEVANCES — issues a citizen cares about, derived from real game state.
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

    public static class Grievances
    {
        /// <summary>
        /// Compute a weighted grievance list for a citizen based on their
        /// actual state and their home building's environment.
        /// Returns up to ~3 grievances (the strongest).
        /// </summary>
        public static List<KeyValuePair<Grievance, float>> Compute(ref Citizen c, BuildingManager bm, EconomyManager em)
        {
            var list = new List<KeyValuePair<Grievance, float>>();

            // --- Taxes: compare citizen's own income class tax to midpoint ---
            try
            {
                int wealth = (int)c.WealthLevel;
                var sub = (wealth >= 2) ? ItemClass.SubService.ResidentialHigh
                                        : ItemClass.SubService.ResidentialLow;
                int taxRate = em.GetTaxRate(ItemClass.Service.Residential, sub, ItemClass.Level.None);
                // CS1 residential default is ~9%. >12 = high, >15 = very high.
                if (taxRate >= 12)
                {
                    float w = Mathf.Clamp01((taxRate - 10) / 10f); // 10%→0, 20%→1
                    list.Add(new KeyValuePair<Grievance, float>(Grievance.HighTaxes, w));
                }
            }
            catch { }

            // --- Health: low m_health value ---
            if (c.m_health < 50)
            {
                float w = (50 - c.m_health) / 50f;
                list.Add(new KeyValuePair<Grievance, float>(Grievance.PoorHealth, w));
            }

            // --- Unemployment: citizen is young/adult and unemployed ---
            var ageG = Citizen.GetAgeGroup(c.m_age);
            bool cUnemployed = (c.m_workBuilding == 0);
            if ((ageG == Citizen.AgeGroup.Young || ageG == Citizen.AgeGroup.Adult) && cUnemployed)
            {
                list.Add(new KeyValuePair<Grievance, float>(Grievance.Unemployed, 0.9f));
            }

            // --- Education: low-ed citizen in a city with weak ed budget ---
            if ((int)c.EducationLevel == 0 && ageG != Citizen.AgeGroup.Child)
            {
                list.Add(new KeyValuePair<Grievance, float>(Grievance.PoorEducation, 0.5f));
            }

            // --- City-wide crime: use the district's crime rate (same
            //     number you see in the game HUD, 0..100%). Only add the
            //     grievance when crime is actually elevated.
            try
            {
                var dm = Singleton<DistrictManager>.instance;
                int cityCrime = dm.m_districts.m_buffer[0].m_finalCrimeRate; // 0..100
                if (cityCrime > 15)
                {
                    float w = Mathf.Clamp01((cityCrime - 15) / 35f); // 15%→0, 50%+→1
                    list.Add(new KeyValuePair<Grievance, float>(Grievance.HighCrime, w));
                }
            }
            catch { }

            // NOTE: Pollution, NoiseOrTrash, and LowLandValue grievances are
            // currently disabled — my previous density-based proxies were too
            // aggressive and fired on almost every urban citizen. A proper
            // implementation would read Building.m_problems via its
            // ProblemStruct accessor, but that API varies across CS1 builds.
            // Leave them inactive for now. If the player's city has pollution
            // visible in the HUD, they can still address it via the green
            // parties' platform (PollutionMultiplier) which the voter model
            // doesn't currently key on.

            // Sort by weight desc; keep top 3 so a voter has a bounded agenda.
            list.Sort((x, y) => y.Value.CompareTo(x.Value));
            if (list.Count > 3) list.RemoveRange(3, list.Count - 3);
            return list;
        }

        /// <summary>
        /// Score how well a party addresses a specific grievance. Returns
        /// roughly -1..+1: positive = the party offers a remedy, negative =
        /// the party's platform would make it worse, zero = neutral.
        /// </summary>
        public static float ScorePartyForGrievance(PartyDef party, Grievance g)
        {
            var m = party.Modifiers;
            switch (g)
            {
                case Grievance.HighTaxes:
                {
                    // Negative tax deltas = tax cuts = good for this grievance.
                    int sum = m.TaxDeltaRes + m.TaxDeltaCom + m.TaxDeltaInd + m.TaxDeltaOff;
                    // Typical sum range ~-8..+8. Normalize.
                    return Mathf.Clamp(-sum / 8f, -1f, 1f);
                }
                case Grievance.PoorHealth:
                    return Mathf.Clamp(m.BudgetDeltaHealth / 20f, -1f, 1f);
                case Grievance.HighCrime:
                    return Mathf.Clamp(m.BudgetDeltaPolice / 15f, -1f, 1f);
                case Grievance.PoorEducation:
                    // Platform with EducationBoost gets a bonus.
                    float base1 = Mathf.Clamp(m.BudgetDeltaEducation / 20f, -1f, 1f);
                    if (party.VanillaPolicies != null)
                    {
                        foreach (var vp in party.VanillaPolicies)
                            if (vp == DistrictPolicies.Policies.EducationBoost) { base1 += 0.3f; break; }
                    }
                    return Mathf.Clamp(base1, -1f, 1f);
                case Grievance.Unemployed:
                    // Industry/roads budget boost + lower business taxes = jobs.
                    float jobs = (m.BudgetDeltaIndustry + m.BudgetDeltaRoads) / 20f
                               - (m.TaxDeltaCom + m.TaxDeltaInd + m.TaxDeltaOff) / 8f;
                    return Mathf.Clamp(jobs, -1f, 1f);
                case Grievance.Pollution:
                {
                    // Lower pollution multiplier, recycling policy, less industry budget.
                    float s = (1f - m.PollutionMultiplier) * 2f;   // 0.9 → 0.2, 1.1 → -0.2
                    s += -m.BudgetDeltaIndustry / 20f;
                    if (party.VanillaPolicies != null)
                    {
                        foreach (var vp in party.VanillaPolicies)
                            if (vp == DistrictPolicies.Policies.Recycling) { s += 0.3f; break; }
                    }
                    return Mathf.Clamp(s, -1f, 1f);
                }
                case Grievance.LowLandValue:
                    // Beautification + old-town policy help.
                    float lv = m.BudgetDeltaBeautification / 20f;
                    if (party.VanillaPolicies != null)
                    {
                        foreach (var vp in party.VanillaPolicies)
                            if (vp == DistrictPolicies.Policies.OldTown) { lv += 0.3f; break; }
                    }
                    return Mathf.Clamp(lv, -1f, 1f);
                case Grievance.NoiseOrTrash:
                    // Garbage budget + traffic ban policies help.
                    float nt = m.BudgetDeltaGarbage / 15f;
                    if (party.VanillaPolicies != null)
                    {
                        foreach (var vp in party.VanillaPolicies)
                            if (vp == DistrictPolicies.Policies.HeavyTrafficBan) { nt += 0.2f; break; }
                    }
                    return Mathf.Clamp(nt, -1f, 1f);
                default:
                    return 0f;
            }
        }
    }

    // ========================================================================
    //  ELECTION ENGINE — voter simulation, coalition formation, policy apply.
    // ========================================================================
    public static class ElectionEngine
    {
        private static System.Random _rng = new System.Random();
        // Temp storage for the last full-sample's per-grievance vote counts.
        // Picked up by RunElection and stored on the ElectionResult.
        private static int[] _lastGrievanceTally;
        private static int[,] _lastAgeTally, _lastEduTally, _lastWealthTally;

        // ---- Deficit pressure (right-wing nudge when city is losing money) ----
        // Updated by PoliticsThreading once per in-game week.
        public static int DeficitWeeks;          // consecutive weeks in deficit
        public static long LastCashSeen = long.MinValue; // sentinel = uninitialized
        public static float DaysSinceLastDeficitChirp = 0f;

        /// <summary>
        /// Current deficit pressure on voters' economic axis, 0..0.35.
        /// Kicks in at 1 week of deficit, saturates around 6 weeks.
        /// </summary>
        public static float DeficitPressure
        {
            get
            {
                if (DeficitWeeks <= 0) return 0f;
                float t = Mathf.Clamp01(DeficitWeeks / 6f);
                return 0.05f + 0.30f * t;
            }
        }

        public static void TriggerCampaign(bool force)
        {
            var st = PoliticsState.Instance;
            if (st == null) return;
            st.Phase = ElectionPhase.Campaign;
            st.DaysSinceCampaignStart = 0f;
            // Seed support from previous result, or uniform if no prior election.
            if (st.LastResult != null)
            {
                for (int i = 0; i < st.CurrentSupport.Length; i++)
                    st.CurrentSupport[i] = st.LastResult.VoteShareByParty[i];
            }
            else
            {
                float u = 1f / PartyCountRef.Value;
                for (int i = 0; i < st.CurrentSupport.Length; i++) st.CurrentSupport[i] = u;
            }
            ShowToast("Campaign begins — elections in " + (int)RuntimeConfig.CampaignLengthDays + " days");
            // Chirper announcement from a generic "City News" account
            PostChirp("City News", "📢 Campaign season begins! Elections in " +
                (int)RuntimeConfig.CampaignLengthDays + " days. #Vote", 1u);
            // Each party chirps a slogan (suppressed in MinimalChirps mode)
            if (!DebugFlags.MinimalChirps)
            {
                for (int i = 0; i < Config.Parties.Length; i++)
                {
                    var p = Config.Parties[i];
                    string slogan = PickSloganForParty(i, "campaign");
                    PostChirp(p.FullName, slogan, (uint)(100 + i));
                }
            }
        }

        /// <summary>Very simple slogan picker keyed by party and context.</summary>
        private static string PickSloganForParty(int partyId, string context)
        {
            var p = Config.Parties[partyId];
            // Pick a slogan style based on economic axis (left / center / right)
            float econ = p.Ideology.x;
            string[] leftCampaign = new[]
            {
                "Tax the rich, fund the schools! #" + p.ShortName,
                "Working class first. Change is coming. #" + p.ShortName,
                "Housing is a right, not a privilege. #" + p.ShortName,
            };
            string[] centerCampaign = new[]
            {
                "Pragmatic government. Smart policy. #" + p.ShortName,
                "Balanced budgets, strong services. #" + p.ShortName,
                "Working across the aisle for our city. #" + p.ShortName,
            };
            string[] rightCampaign = new[]
            {
                "Lower taxes, safer streets. #" + p.ShortName,
                "Small government, big results. #" + p.ShortName,
                "Back to basics. Back to greatness. #" + p.ShortName,
            };
            string[] leftVictory = new[]
            {
                "We did it! A new dawn for workers! #" + p.ShortName,
                "The people have spoken. Reform starts now. #" + p.ShortName,
            };
            string[] centerVictory = new[]
            {
                "Thank you. We will govern for everyone. #" + p.ShortName,
                "A steady hand is what the city needs. #" + p.ShortName,
            };
            string[] rightVictory = new[]
            {
                "Victory! Freedom and prosperity. #" + p.ShortName,
                "A clear mandate for lower taxes. #" + p.ShortName,
            };
            string[] leftDefeat = new[]
            {
                "We fight on. The struggle continues. #" + p.ShortName,
                "This is not the end — it's a beginning. #" + p.ShortName,
            };
            string[] centerDefeat = new[]
            {
                "We respect the result. Back to work for our voters. #" + p.ShortName,
                "Opposition will hold the government accountable. #" + p.ShortName,
            };
            string[] rightDefeat = new[]
            {
                "We accept the result and will rebuild stronger. #" + p.ShortName,
                "The silent majority will have its day. #" + p.ShortName,
            };

            string[] pool;
            bool isCampaign = context == "campaign";
            bool isVictory  = context == "victory";
            if (econ < -0.3f)      pool = isCampaign ? leftCampaign   : (isVictory ? leftVictory   : leftDefeat);
            else if (econ > +0.3f) pool = isCampaign ? rightCampaign  : (isVictory ? rightVictory  : rightDefeat);
            else                   pool = isCampaign ? centerCampaign : (isVictory ? centerVictory : centerDefeat);
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        public static void DriftCampaign(float dayDelta)
        {
            var st = PoliticsState.Instance;
            // Sample a few citizens each tick; update support gradually.
            int samples = Math.Max(50, (int)(Config.VoterSampleSize * dayDelta / RuntimeConfig.CampaignLengthDays));
            float[] tally = new float[PartyCountRef.Value];
            int actualSamples = SampleCitizenPreferences(samples, tally);
            if (actualSamples == 0) return;

            // Normalize tally to shares
            float total = 0f;
            for (int i = 0; i < tally.Length; i++) total += tally[i];
            if (total <= 0f) return;
            for (int i = 0; i < tally.Length; i++) tally[i] /= total;

            // Blend toward sampled preferences (lerp factor proportional to dayDelta)
            float alpha = Mathf.Clamp01(dayDelta * 0.2f);
            for (int i = 0; i < st.CurrentSupport.Length; i++)
            {
                st.CurrentSupport[i] = Mathf.Lerp(st.CurrentSupport[i], tally[i], alpha);
            }
        }

        public static void RunElection()
        {
            var st = PoliticsState.Instance;
            // Full sample to compute final results + per-building dominant party.
            int sampled = RunFullElectionSample();
            if (sampled == 0)
            {
                PoliticsUserMod.Log("No voters sampled — aborting election.");
                st.Phase = ElectionPhase.Idle;
                st.DaysSinceLastElection = 0f;
                return;
            }

            // Allocate seats using largest remainders method
            var finalShares = (float[])st.CurrentSupport.Clone();
            AllocateSeats(finalShares, Config.ParliamentSeats, out st.CurrentSeats);

            // Build result
            var now = SimulationManager.instance.m_currentGameTime;
            var result = new ElectionResult
            {
                Year  = now.Year,
                Month = now.Month,
                SeatsByParty     = (int[])st.CurrentSeats.Clone(),
                VoteShareByParty = (float[])finalShares.Clone(),
                Turnout          = ComputeTurnout(),
                VotesByGrievance = _lastGrievanceTally != null
                                   ? (int[])_lastGrievanceTally.Clone()
                                   : new int[9],
                VotesByAgeParty    = _lastAgeTally    != null ? (int[,])_lastAgeTally.Clone()    : null,
                VotesByEduParty    = _lastEduTally    != null ? (int[,])_lastEduTally.Clone()    : null,
                VotesByWealthParty = _lastWealthTally != null ? (int[,])_lastWealthTally.Clone() : null,
            };

            st.LastResult = result;
            st.History.Add(result);

            // Phase -> forming
            st.Phase = ElectionPhase.Forming;
            FormCoalition(result);
        }

        private static float ComputeTurnout()
        {
            try
            {
                var dm = Singleton<DistrictManager>.instance;
                float happiness = dm.m_districts.m_buffer[0].m_finalHappiness / 100f;
                return Mathf.Clamp01(Config.TurnoutBase + happiness * Config.TurnoutHappinessBoost);
            }
            catch { return Config.TurnoutBase; }
        }

        private static void FormCoalition(ElectionResult result)
        {
            var st = PoliticsState.Instance;
            st.CoalitionPartyIds.Clear();

            // Start with the largest party; greedily add closest-ideology partners until majority.
            var parties = new List<int>();
            for (int i = 0; i < st.CurrentSeats.Length; i++) parties.Add(i);
            parties.Sort((a, b) => st.CurrentSeats[b].CompareTo(st.CurrentSeats[a]));

            int seatsTotal = 0;
            var chosen = new List<int>();
            int lead = parties[0];
            chosen.Add(lead);
            seatsTotal += st.CurrentSeats[lead];

            while (seatsTotal < Config.MajorityThreshold &&
                   chosen.Count < Config.MaxCoalitionPartners)
            {
                // pick the party closest in ideology to the average of chosen parties
                Vector3 avg = AverageIdeology(chosen);
                int best = -1;
                float bestDist = float.MaxValue;
                foreach (var p in parties)
                {
                    if (chosen.Contains(p)) continue;
                    if (st.CurrentSeats[p] <= 0) continue;
                    float d = (Config.Parties[p].Ideology - avg).magnitude;
                    if (d < bestDist) { bestDist = d; best = p; }
                }
                if (best < 0) break;
                chosen.Add(best);
                seatsTotal += st.CurrentSeats[best];
            }

            if (seatsTotal >= Config.MajorityThreshold)
            {
                st.CoalitionPartyIds = chosen;
                result.CoalitionPartyIds = new List<int>(chosen);
                st.Phase = ElectionPhase.Governing;
                st.DaysSinceLastElection = 0f;
                ApplyCoalitionPolicies();
                ShowResultsPopup(result);
                ShowToast(Config.Parties[chosen[0]].FullName + " forms government with " + (chosen.Count - 1) + " partner(s).");
                // Refresh building tints so the new per-building dominant party shows up immediately
                HarmonyPatcher.RefreshBuildingColors();

                // Chirper announcements: news (always) + per-party reactions (suppressed in MinimalChirps)
                PostChirp("City News",
                    "🏛 " + Config.Parties[chosen[0]].ShortName + " wins. Coalition formed with " +
                    (chosen.Count - 1) + " partner(s). Turnout " + (int)(result.Turnout * 100) + "%. #Election",
                    1u);
                if (!DebugFlags.MinimalChirps)
                {
                    var winners = new HashSet<int>(chosen);
                    for (int i = 0; i < Config.Parties.Length; i++)
                    {
                        var party = Config.Parties[i];
                        string slogan = PickSloganForParty(i, winners.Contains(i) ? "victory" : "defeat");
                        PostChirp(party.FullName, slogan, (uint)(200 + i));
                    }
                }
            }
            else
            {
                // Failed: snap re-election after cooldown
                st.Phase = ElectionPhase.Failed;
                st.FailedCooldownRemaining = RuntimeConfig.ReElectionCooldownDays;
                RevertCoalitionPolicies();
                ShowToast("No coalition could be formed. Snap re-election in " +
                          (int)RuntimeConfig.ReElectionCooldownDays + " days.");
                PostChirp("City News",
                    "⚠ Coalition talks collapsed. Snap re-election in " +
                    (int)RuntimeConfig.ReElectionCooldownDays + " days. #CrisisMode",
                    1u);
            }
        }

        private static Vector3 AverageIdeology(List<int> partyIds)
        {
            Vector3 v = Vector3.zero;
            foreach (var id in partyIds) v += Config.Parties[id].Ideology;
            if (partyIds.Count > 0) v /= partyIds.Count;
            return v;
        }

        private static void AllocateSeats(float[] shares, int totalSeats, out int[] seats)
        {
            seats = new int[shares.Length];
            float[] quotas = new float[shares.Length];
            int allocated = 0;
            for (int i = 0; i < shares.Length; i++)
            {
                quotas[i] = shares[i] * totalSeats;
                seats[i] = (int)Math.Floor(quotas[i]);
                allocated += seats[i];
            }
            // Distribute remainders
            var remainders = new List<KeyValuePair<int, float>>();
            for (int i = 0; i < shares.Length; i++)
                remainders.Add(new KeyValuePair<int, float>(i, quotas[i] - seats[i]));
            remainders.Sort((a, b) => b.Value.CompareTo(a.Value));
            int idx = 0;
            while (allocated < totalSeats && idx < remainders.Count)
            {
                seats[remainders[idx].Key]++;
                allocated++;
                idx++;
            }
        }

        /// <summary>
        ///  Sample citizens and accumulate party preferences.
        ///  Returns number of citizens actually sampled.
        /// </summary>
        private static int SampleCitizenPreferences(int maxSamples, float[] tally)
        {
            var cm = Singleton<CitizenManager>.instance;
            var bm = Singleton<BuildingManager>.instance;
            uint bufSize = cm.m_citizens.m_size;
            if (bufSize == 0) return 0;
            int sampled = 0;
            int tries = 0;
            int limit = maxSamples * 4;
            while (sampled < maxSamples && tries < limit)
            {
                tries++;
                uint idx = (uint)_rng.Next(1, (int)bufSize);
                var c = cm.m_citizens.m_buffer[idx];
                if ((c.m_flags & Citizen.Flags.Created) == 0) continue;
                if ((c.m_flags & Citizen.Flags.DummyTraffic) != 0) continue;
                Grievance _gUnused1;
                int party = DecideVote(ref c, bm, out _gUnused1);
                if (party < 0) continue;
                tally[party] += 1f;
                sampled++;
            }
            return sampled;
        }

        private static int RunFullElectionSample()
        {
            var st = PoliticsState.Instance;
            var cm = Singleton<CitizenManager>.instance;
            var bm = Singleton<BuildingManager>.instance;

            uint bBuf = bm.m_buildings.m_size;
            if (bBuf == 0) return 0;

            // Per-building tallies (compact — residential buildings only)
            var perBuildingTally  = new int[bBuf, PartyCountRef.Value];
            var perBuildingVoters = new int[bBuf];
            var perBuildingHappy  = new int[bBuf];
            var overallTally      = new float[PartyCountRef.Value];
            int totalSampled = 0;
            // Grievance tally for the current election — indexed by (int)Grievance.
            // Size = 9 matches the Grievance enum (None + 8 concrete values).
            var grievanceTally = new int[9];
            // Demographic cross-tabs
            int _np = Config.Parties.Length;
            var ageTally    = new int[3, _np];
            var eduTally    = new int[4, _np];
            var wealthTally = new int[3, _np];

            // Walk EVERY residential building and sample its citizen units.
            // This gives ~100% per-building coverage unlike random citizen sampling.
            // On 150k-citizen cities this runs once per election (~365 game days)
            // and takes <100ms — acceptable.
            for (int b = 1; b < bBuf; b++)
            {
                var building = bm.m_buildings.m_buffer[b];
                if ((building.m_flags & Building.Flags.Created) == 0) continue;
                if (building.Info == null) continue;
                if (building.Info.GetService() != ItemClass.Service.Residential) continue;

                // Walk the citizen-unit linked list for this building.
                // Up to ~8 per building typically; hard-cap to prevent any runaway.
                uint unit = building.m_citizenUnits;
                int  safety = 0;
                while (unit != 0u && safety < 256)
                {
                    safety++;
                    var cu = cm.m_units.m_buffer[unit];
                    if ((cu.m_flags & CitizenUnit.Flags.Home) != 0)
                    {
                        // Up to 5 citizens per unit
                        SampleCitizenInUnit(cu.m_citizen0, cm, bm, b, perBuildingTally, perBuildingVoters, perBuildingHappy, overallTally, grievanceTally, ageTally, eduTally, wealthTally, ref totalSampled);
                        SampleCitizenInUnit(cu.m_citizen1, cm, bm, b, perBuildingTally, perBuildingVoters, perBuildingHappy, overallTally, grievanceTally, ageTally, eduTally, wealthTally, ref totalSampled);
                        SampleCitizenInUnit(cu.m_citizen2, cm, bm, b, perBuildingTally, perBuildingVoters, perBuildingHappy, overallTally, grievanceTally, ageTally, eduTally, wealthTally, ref totalSampled);
                        SampleCitizenInUnit(cu.m_citizen3, cm, bm, b, perBuildingTally, perBuildingVoters, perBuildingHappy, overallTally, grievanceTally, ageTally, eduTally, wealthTally, ref totalSampled);
                        SampleCitizenInUnit(cu.m_citizen4, cm, bm, b, perBuildingTally, perBuildingVoters, perBuildingHappy, overallTally, grievanceTally, ageTally, eduTally, wealthTally, ref totalSampled);
                    }
                    unit = cu.m_nextUnit;
                }
            }

            if (totalSampled == 0) return 0;

            // Write overall support
            float total = 0f;
            for (int i = 0; i < overallTally.Length; i++) total += overallTally[i];
            for (int i = 0; i < st.CurrentSupport.Length; i++)
                st.CurrentSupport[i] = total > 0 ? overallTally[i] / total : 0f;

            // Resize per-building arrays if buffer grew
            var st2 = PoliticsState.Instance;
            if (st2.DominantPartyByBuilding == null || st2.DominantPartyByBuilding.Length != bBuf)
            {
                st2.DominantPartyByBuilding = new byte[bBuf];
                st2.TurnoutByBuilding       = new byte[bBuf];
                st2.SatisfactionByBuilding  = new byte[bBuf];
            }
            for (int b = 0; b < bBuf; b++)
            {
                int voters = perBuildingVoters[b];
                if (voters <= 0) { st2.DominantPartyByBuilding[b] = 255; continue; }
                int best = 0; int bestCt = -1;
                for (int p = 0; p < PartyCountRef.Value; p++)
                {
                    if (perBuildingTally[b, p] > bestCt) { bestCt = perBuildingTally[b, p]; best = p; }
                }
                st2.DominantPartyByBuilding[b] = (byte)best;
                var building = bm.m_buildings.m_buffer[b];
                int residents = BuildingResidentCount(building);
                // Everyone sampled voted (we're doing 100% coverage); turnout is
                // therefore a function of how many of the building's residents
                // were "created" citizens (exclude dummies, empty slots).
                st2.TurnoutByBuilding[b] = (byte)Mathf.Clamp(residents > 0 ? Math.Min(100, voters * 100 / residents) : 100, 0, 100);
                st2.SatisfactionByBuilding[b] = (byte)Mathf.Clamp(voters > 0 ? (perBuildingHappy[b] * 100 / voters) : 0, 0, 100);
            }
            _lastGrievanceTally = grievanceTally;
            _lastAgeTally       = ageTally;
            _lastEduTally       = eduTally;
            _lastWealthTally    = wealthTally;
            return totalSampled;
        }

        private static void SampleCitizenInUnit(uint citizenId, CitizenManager cm, BuildingManager bm,
                                                int buildingId, int[,] perBuildingTally,
                                                int[] perBuildingVoters, int[] perBuildingHappy,
                                                float[] overallTally, int[] grievanceTally,
                                                int[,] ageTally, int[,] eduTally, int[,] wealthTally,
                                                ref int totalSampled)
        {
            if (citizenId == 0u) return;
            var c = cm.m_citizens.m_buffer[citizenId];
            if ((c.m_flags & Citizen.Flags.Created) == 0) return;
            if ((c.m_flags & Citizen.Flags.DummyTraffic) != 0) return;

            Grievance reason;
            int party = DecideVote(ref c, bm, out reason);
            if (party < 0) return;

            overallTally[party] += 1f;
            perBuildingTally[buildingId, party]++;
            perBuildingVoters[buildingId]++;
            if ((c.m_flags & Citizen.Flags.NeedGoods) == 0) perBuildingHappy[buildingId]++;
            if (grievanceTally != null)
            {
                int gi = (int)reason;
                if (gi >= 0 && gi < grievanceTally.Length) grievanceTally[gi]++;
            }

            // Demographic cross-tabs
            int np = Config.Parties.Length;
            if (ageTally != null && party < np)
            {
                var age = Citizen.GetAgeGroup(c.m_age);
                int bucket = (age == Citizen.AgeGroup.Young)  ? 0 :
                             (age == Citizen.AgeGroup.Adult)  ? 1 :
                             (age == Citizen.AgeGroup.Senior) ? 2 : -1;
                if (bucket >= 0) ageTally[bucket, party]++;
            }
            if (eduTally != null && party < np)
            {
                int edu = (int)c.EducationLevel; // 0..3
                if (edu < 0) edu = 0; if (edu > 3) edu = 3;
                eduTally[edu, party]++;
            }
            if (wealthTally != null && party < np)
            {
                int w = (int)c.WealthLevel; // 0..2
                if (w < 0) w = 0; if (w > 2) w = 2;
                wealthTally[w, party]++;
            }

            totalSampled++;
        }

        private static int BuildingResidentCount(Building b)
        {
            // Approximate — BuildingAI.CalculateHomeCount would be exact but varies by type.
            // Household count inferred via Citizen unit walk would be more accurate but slower.
            // Use the building's current citizen count summary.
            var info = b.Info;
            if (info == null || info.m_buildingAI == null) return 0;
            return b.m_citizenCount;
        }

        /// <summary>
        /// Decide which party a citizen votes for.
        /// Returns -1 if the citizen is ineligible to vote (children / teens).
        /// <summary>
        /// Decide which party a citizen votes for.
        /// Returns -1 for children/teens (non-voters).
        /// Combines:
        ///   * Ideology (from VoterTraits "nudges" + party.Ideology) — the
        ///     baseline leaning.
        ///   * Grievances (real-game-state complaints) — strong pull toward
        ///     parties whose platform addresses the grievance.
        ///   * Random noise for variety.
        /// Sets <paramref name="reason"/> to the dominant grievance that
        /// drove the choice, or Grievance.None if pure ideology.
        /// </summary>
        private static int DecideVote(ref Citizen c, BuildingManager bm, out Grievance reason)
        {
            reason = Grievance.None;

            // Voting eligibility
            var ageGroup = Citizen.GetAgeGroup(c.m_age);
            if (ageGroup == Citizen.AgeGroup.Child || ageGroup == Citizen.AgeGroup.Teen)
                return -1;

            int wealth    = (int)c.WealthLevel;
            int education = (int)c.EducationLevel;
            bool employed = (c.m_workBuilding != 0);
            bool sick     = (c.m_flags & Citizen.Flags.Sick) != 0;

            // ---- Ideology nudges: build a voter ideology point from traits ----
            float econ = 0f;
            switch (education)
            {
                case 0: econ += VoterTraits.BiasEduUneducated;     break;
                case 1: econ += VoterTraits.BiasEduEducated;       break;
                case 2: econ += VoterTraits.BiasEduWellEducated;   break;
                default: econ += VoterTraits.BiasEduHighlyEducated; break;
            }
            switch (wealth)
            {
                case 0: econ += VoterTraits.BiasWealthLow;    break;
                case 1: econ += VoterTraits.BiasWealthMedium; break;
                default: econ += VoterTraits.BiasWealthHigh;   break;
            }
            econ += employed ? VoterTraits.BiasEmployed : VoterTraits.BiasUnemployed;
            if (ageGroup == Citizen.AgeGroup.Young)       econ += VoterTraits.BiasYoung;
            else if (ageGroup == Citizen.AgeGroup.Adult)  econ += VoterTraits.BiasAdult;
            else if (ageGroup == Citizen.AgeGroup.Senior) econ += VoterTraits.BiasSenior;
            if (sick) econ += VoterTraits.BiasSick;
            // Deficit pressure — the city is losing money → voters drift right
            // (lower taxes, business-friendly). Strength 0..0.35 based on how
            // many consecutive weeks the budget has been negative.
            econ += DeficitPressure;

            econ = Mathf.Clamp(econ, -1f, 1f);

            float ageF = Mathf.Clamp01(c.m_age / 240f);
            float soc  = Mathf.Clamp(ageF * 1.2f - 0.4f - (education * 0.15f), -1f, 1f);
            float gov  = Mathf.Clamp((_rng.Next(0, 100) - 50) / 100f, -1f, 1f);

            // Small random jitter (half the old VoterNoise magnitude — rest of
            // the randomness lives in the combined-score noise step below).
            econ += ((float)_rng.NextDouble() - 0.5f) * Config.VoterNoise;
            soc  += ((float)_rng.NextDouble() - 0.5f) * Config.VoterNoise;
            gov  += ((float)_rng.NextDouble() - 0.5f) * Config.VoterNoise;

            var voterPoint = new Vector3(econ, soc, gov);

            // ---- Compute grievances ----
            var em = Singleton<EconomyManager>.instance;
            var grievances = Grievances.Compute(ref c, bm, em);

            // ---- Score each party: ideology_fit (0..1) + grievance_score ----
            // 40% ideology / 50% grievance / 10% noise (your requested weights).
            int best = -1;
            float bestScore = float.MinValue;
            Grievance bestReason = Grievance.None;
            int np = Config.Parties.Length;
            for (int i = 0; i < np; i++)
            {
                var party = Config.Parties[i];

                // Ideology fit: 1 - normalized_distance. Max distance in a
                // [-1,1]^3 cube is sqrt(12) ≈ 3.46 — normalize by that.
                float dist = (party.Ideology - voterPoint).magnitude;
                float ideologyFit = Mathf.Clamp01(1f - dist / 3.46f);

                // Grievance score: weighted sum of per-grievance fits.
                float grievScore = 0f;
                float grievWeight = 0f;
                Grievance localReason = Grievance.None;
                float localReasonFit = float.MinValue;
                foreach (var gv in grievances)
                {
                    float s = Grievances.ScorePartyForGrievance(party, gv.Key);
                    grievScore  += gv.Value * s;
                    grievWeight += gv.Value;
                    if (s > localReasonFit) { localReasonFit = s; localReason = gv.Key; }
                }
                if (grievWeight > 0f) grievScore /= grievWeight; // avg weighted

                // Random noise per (citizen, party) pair.
                float noise = ((float)_rng.NextDouble() - 0.5f) * 0.2f; // ±0.1

                float total = 0.4f * ideologyFit + 0.5f * (grievScore + 1f) * 0.5f + 0.1f * (noise + 0.1f);
                // grievScore ∈ [-1..+1] mapped into 0..1 via (x+1)/2.

                if (total > bestScore)
                {
                    bestScore = total;
                    best = i;
                    // Only record grievance-based reason if grievance had a
                    // strong positive contribution for the winner.
                    bestReason = (grievances.Count > 0 && localReasonFit > 0.2f)
                               ? localReason
                               : Grievance.None;
                }
            }

            // Incumbency bump: happy voters (health + wellbeing both decent)
            // sometimes reward the coalition.
            int happiness = Citizen.GetHappiness(c.m_health, c.m_wellbeing);
            bool happy = happiness >= 60;
            var st = PoliticsState.Instance;
            if (happy && st != null && st.CoalitionPartyIds != null && st.CoalitionPartyIds.Count > 0
                && _rng.NextDouble() < 0.10)
            {
                best = st.CoalitionPartyIds[_rng.Next(0, st.CoalitionPartyIds.Count)];
                bestReason = Grievance.None; // incumbency = ideology-like
            }

            reason = bestReason;
            return best;
        }

        // ---- Policy application ------------------------------------------

        public static void ApplyCoalitionPolicies()
        {
            var st = PoliticsState.Instance;

            // Snapshot the previous coalition's applied policies BEFORE
            // reverting, so we can diff against the new coalition's platform.
            var prevApplied = new HashSet<DistrictPolicies.Policies>(
                st.AppliedVanillaPolicies ?? new List<DistrictPolicies.Policies>());

            RevertCoalitionPolicies(skipLog: true);
            if (st.CoalitionPartyIds == null || st.CoalitionPartyIds.Count == 0) return;

            var dm = Singleton<DistrictManager>.instance;
            var wanted = new HashSet<DistrictPolicies.Policies>();
            foreach (var id in st.CoalitionPartyIds)
                foreach (var p in Config.Parties[id].VanillaPolicies)
                    wanted.Add(p);

            // -------- Repeals: previous policies NOT in the new platform ----
            foreach (var p in prevApplied)
            {
                if (wanted.Contains(p)) continue;
                PoliticsUserMod.Log("Repealed policy: " + p);
                AnnounceRepeal(p, st);
            }

            // -------- New / renewed policies --------------------------------
            foreach (var p in wanted)
            {
                try
                {
                    bool already = dm.m_districts.m_buffer[0].IsPolicySet(p);
                    if (!already)
                    {
                        var capture = p;
                        Singleton<SimulationManager>.instance.AddAction(() =>
                        {
                            try { Singleton<DistrictManager>.instance.SetCityPolicy(capture); }
                            catch (Exception ex) { PoliticsUserMod.Log("SetCityPolicy(" + capture + ") failed: " + ex.Message); }
                        });
                        st.AppliedVanillaPolicies.Add(p);
                        PoliticsUserMod.Log("Queued policy enable: " + p);
                        // Only announce brand-new bills (not already-on).
                        AnnounceBill(p, st);
                    }
                    else
                    {
                        // Already active from prior term (maybe same party back in power).
                        // Record it so the next revert knows about it.
                        // Only announce if it was NOT in the previous coalition's set
                        // (i.e., it was set manually by the player before the mod
                        // ever touched it — first-time enactment from our POV).
                        st.AppliedVanillaPolicies.Add(p);
                        if (!prevApplied.Contains(p))
                        {
                            PoliticsUserMod.Log("Adopting pre-existing policy as coalition bill: " + p);
                            AnnounceBill(p, st);
                        }
                        else
                        {
                            PoliticsUserMod.Log("Policy renewed silently: " + p);
                        }
                    }
                }
                catch (Exception e)
                {
                    PoliticsUserMod.Log("Could not enable policy " + p + ": " + e.Message);
                }
            }

            ApplyCustomModifiers(+1);
            st.PoliciesApplied = true;
            PoliticsUserMod.Log("Applied coalition policies. Count=" + st.AppliedVanillaPolicies.Count);

            AnnounceBudgetAndTaxBill(st);
            RefreshEconomyPanel();
            RefreshCityPoliciesPanel();
        }

        /// <summary>
        /// Force the vanilla Economy panel to re-read tax/budget values.
        /// The Economy panel reads its values in Start/OnEnable, so toggling
        /// visibility does not help. Instead we walk the UI tree looking for
        /// components with tax/budget-related names and invoke refresh
        /// methods on each via reflection.
        /// </summary>
        private static void RefreshEconomyPanel()
        {
            try
            {
                // The Economy panel is an EconomyPanel instance. We want to
                // call its private "PopulateXxx" methods to force a re-read
                // from EconomyManager.

                var type = Type.GetType("EconomyPanel, Assembly-CSharp", false);
                if (type != null)
                {
                    var obj = UnityEngine.Object.FindObjectOfType(type);
                    if (obj != null)
                    {
                        var t = obj.GetType();
                        // Try every plausible zero-arg instance method in EconomyPanel.
                        string[] names = new[]
                        {
                            "PopulateData", "Populate", "PopulateTaxRate",
                            "PopulateBudget", "RefreshTaxRate", "RefreshBudget",
                            "Invalidate", "RefreshPanel", "RefreshContent",
                            "UpdateTexts", "UpdateValues"
                        };
                        int hit = 0;
                        foreach (var n in names)
                        {
                            var m = t.GetMethod(n,
                                System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic);
                            if (m == null) continue;
                            if (m.GetParameters().Length != 0) continue;
                            try { m.Invoke(obj, null); PoliticsUserMod.Log("EconomyPanel." + n + "() called"); hit++; }
                            catch (Exception ex) { PoliticsUserMod.Log("EconomyPanel." + n + " threw: " + ex.Message); }
                        }
                        if (hit == 0)
                        {
                            // Fallback: enumerate all instance methods and log names so we can see what's there.
                            var methods = t.GetMethods(System.Reflection.BindingFlags.Instance |
                                                       System.Reflection.BindingFlags.Public |
                                                       System.Reflection.BindingFlags.NonPublic);
                            int logged = 0;
                            foreach (var m in methods)
                            {
                                if (m.GetParameters().Length != 0) continue;
                                if (m.DeclaringType != t) continue;
                                PoliticsUserMod.Log("EconomyPanel candidate: " + m.Name);
                                if (++logged >= 40) break;
                            }
                        }
                    }
                    else
                    {
                        PoliticsUserMod.Log("EconomyPanel type found but no instance in scene");
                    }
                }
                else
                {
                    PoliticsUserMod.Log("EconomyPanel type not found via Type.GetType");
                }
            }
            catch (Exception e)
            {
                PoliticsUserMod.Log("RefreshEconomyPanel failed: " + e.Message);
            }
        }

        private static bool TryInvokeRefreshMethods(UIComponent comp)
        {
            bool any = false;
            string[] methodNames = new[] {
                "RefreshPanel", "Refresh", "Invalidate", "Populate",
                "RefreshContent", "RefreshValues", "UpdateValues",
                "PopulateData", "RefreshData"
            };
            var t = comp.GetType();
            foreach (var m in methodNames)
            {
                var mi = t.GetMethod(m, System.Reflection.BindingFlags.Instance |
                                        System.Reflection.BindingFlags.Public |
                                        System.Reflection.BindingFlags.NonPublic);
                if (mi == null) continue;
                if (mi.GetParameters().Length != 0) continue;
                try
                {
                    mi.Invoke(comp, null);
                    PoliticsUserMod.Log("Invoked " + comp.name + "." + m);
                    any = true;
                }
                catch { }
            }
            return any;
        }

        public static void RevertCoalitionPolicies()
        public static void RevertCoalitionPolicies(bool skipLog = false)
        {
            var st = PoliticsState.Instance;
            if (!st.PoliciesApplied) return;
            foreach (var p in st.AppliedVanillaPolicies)
            {
                var capture = p;
                try
                {
                    Singleton<SimulationManager>.instance.AddAction(() =>
                    {
                        try { Singleton<DistrictManager>.instance.UnsetCityPolicy(capture); }
                        catch { /* ignore */ }
                    });
                }
                catch { /* ignore */ }
            }
            st.AppliedVanillaPolicies.Clear();
            ApplyCustomModifiers(-1);
            st.PoliciesApplied = false;
            if (!skipLog) PoliticsUserMod.Log("Reverted coalition policies.");
        }

        /// <summary>
        /// Announce a policy's passage as a Chirper bill with a vote tally.
        /// Vote tally is simulated:
        ///   * Parties whose platform INCLUDES this policy: all seats vote YES.
        ///   * Coalition parties (even if policy not on their platform): vote YES.
        ///   * Opposition parties: vote based on ideological distance to the
        ///     average ideology of the supporting parties. Close → YES, far → NO.
        ///   * Each party also has a small fraction of seats that abstain.
        /// </summary>
        /// <summary>
        /// Announce that a previously-active policy is being repealed by
        /// the incoming government (similar in format to <see cref="AnnounceBill"/>).
        /// </summary>
        public static void AnnounceRepeal(DistrictPolicies.Policies policy, PoliticsState st)
        {
            // Simple tally: coalition parties mostly yes on repeal, opposition varies.
            int yes = 0, no = 0, abstain = 0;
            Vector3 coalCenter = Vector3.zero;
            foreach (var cid in st.CoalitionPartyIds) coalCenter += Config.Parties[cid].Ideology;
            if (st.CoalitionPartyIds.Count > 0) coalCenter /= st.CoalitionPartyIds.Count;
            var coalSet = new HashSet<int>(st.CoalitionPartyIds);
            for (int i = 0; i < Config.Parties.Length; i++)
            {
                int seats = (i < st.CurrentSeats.Length) ? st.CurrentSeats[i] : 0;
                if (seats <= 0) continue;
                float yesShare, abstainShare;
                if (coalSet.Contains(i)) { yesShare = 0.85f; abstainShare = 0.08f; }
                else
                {
                    float dist = (Config.Parties[i].Ideology - coalCenter).magnitude;
                    yesShare = Mathf.Clamp(0.45f - dist * 0.18f, 0.05f, 0.55f);
                    abstainShare = 0.15f;
                }
                int py = Mathf.RoundToInt(seats * yesShare);
                int pa = Mathf.RoundToInt(seats * abstainShare);
                int pn = Math.Max(0, seats - py - pa);
                yes += py; no += pn; abstain += pa;
            }
            if (yes <= no) { int flip = (no - yes) + 1; yes += flip; no = Math.Max(0, no - flip); }

            int billNo = st.NextBillNumber++;
            string text = string.Format(
                "🏛 Parliament votes {0}-{1} to REPEAL bill C-{2}: An Act to end {3}.",
                yes, no, billNo, FormatPolicyTitle(policy));
            if (abstain > 0) text += "  (" + abstain + " abstentions)";
            PostChirp("City News", text, 42u);
            PoliticsUserMod.Log("AnnounceRepeal: " + text);
        }

        /// <summary>
        /// Best-effort refresh of the vanilla City Policies panel so our
        /// SetCityPolicy / UnsetCityPolicy writes show immediately.
        /// Uses the same reflection trick as the Economy refresh.
        /// </summary>
        private static void RefreshCityPoliciesPanel()
        {
            try
            {
                var type = Type.GetType("PoliciesPanel, Assembly-CSharp", false);
                if (type == null) return;
                var obj = UnityEngine.Object.FindObjectOfType(type);
                if (obj == null) return;
                var t = obj.GetType();
                string[] names = new[]
                {
                    "PopulateData", "Populate", "RefreshPanel",
                    "RefreshContent", "Invalidate", "RefreshPolicy"
                };
                foreach (var n in names)
                {
                    var m = t.GetMethod(n,
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic);
                    if (m != null && m.GetParameters().Length == 0)
                    {
                        try { m.Invoke(obj, null); PoliticsUserMod.Log("PoliciesPanel." + n + "() called"); }
                        catch (Exception ex) { PoliticsUserMod.Log("PoliciesPanel." + n + " threw: " + ex.Message); }
                    }
                }
            }
            catch (Exception e)
            {
                PoliticsUserMod.Log("RefreshCityPoliciesPanel failed: " + e.Message);
            }
        }

        public static void AnnounceBill(DistrictPolicies.Policies policy, PoliticsState st)
        {
            // Find supporter parties (the ones who had this policy in their platform)
            var supporters = new List<int>();
            for (int i = 0; i < Config.Parties.Length; i++)
            {
                foreach (var vp in Config.Parties[i].VanillaPolicies)
                {
                    if (vp == policy) { supporters.Add(i); break; }
                }
            }

            // Supporter ideology center (for opposition distance weighting)
            Vector3 supCenter = Vector3.zero;
            if (supporters.Count > 0)
            {
                foreach (var s in supporters) supCenter += Config.Parties[s].Ideology;
                supCenter /= supporters.Count;
            }
            else
            {
                // No platform supporter — use coalition center
                foreach (var cid in st.CoalitionPartyIds) supCenter += Config.Parties[cid].Ideology;
                if (st.CoalitionPartyIds.Count > 0) supCenter /= st.CoalitionPartyIds.Count;
            }

            // Tally YES/NO/ABSTAIN per party
            int yes = 0, no = 0, abstain = 0;
            var coalitionSet = new HashSet<int>(st.CoalitionPartyIds);
            var supporterSet = new HashSet<int>(supporters);
            for (int i = 0; i < Config.Parties.Length; i++)
            {
                int seats = (i < st.CurrentSeats.Length) ? st.CurrentSeats[i] : 0;
                if (seats <= 0) continue;

                float yesShare, abstainShare;
                if (supporterSet.Contains(i))
                {
                    yesShare     = 0.95f;
                    abstainShare = 0.03f;
                }
                else if (coalitionSet.Contains(i))
                {
                    // Coalition loyalty: mostly vote with the government
                    yesShare     = 0.80f;
                    abstainShare = 0.10f;
                }
                else
                {
                    // Opposition: distance-weighted support
                    float dist = (Config.Parties[i].Ideology - supCenter).magnitude;
                    // dist in [0..~3.5]. Map 0 → 0.55 yes, 3.5+ → 0.05 yes.
                    yesShare     = Mathf.Clamp(0.55f - dist * 0.18f, 0.05f, 0.6f);
                    abstainShare = 0.15f;
                }

                int partyYes     = Mathf.RoundToInt(seats * yesShare);
                int partyAbstain = Mathf.RoundToInt(seats * abstainShare);
                int partyNo      = Mathf.Max(0, seats - partyYes - partyAbstain);
                yes     += partyYes;
                no      += partyNo;
                abstain += partyAbstain;
            }

            // Compose chirp
            int billNo = st.NextBillNumber++;
            string title = FormatPolicyTitle(policy);
            string passedOrFailed = yes > no ? "passes" : "REJECTS";
            // In practice the policy WAS enacted (we just called SetCityPolicy),
            // so the tally usually shows pass. If tally math produces a "fail"
            // (rare — opposition has a big majority), we override the chirp text.
            if (yes <= no)
            {
                // Force the tally to squeak through — this is a post-hoc
                // narrative fit. Keep the enacted fact, adjust numbers.
                int flip = (no - yes) + 1;
                no      = Math.Max(0, no - flip);
                yes     = yes + flip;
                passedOrFailed = "passes";
            }

            string chirp = string.Format(
                "🏛 Parliament votes {0}-{1} to {2} bill C-{3}: An Act to {4}.",
                yes, no, passedOrFailed, billNo, title);
            if (abstain > 0) chirp += "  ({0} abstentions)".Replace("{0}", abstain.ToString());
            PostChirp("City News", chirp, 42u);
            PoliticsUserMod.Log("AnnounceBill: " + chirp);

            // 1-2 citizen reaction chirps, suppressed in minimal mode.
            if (!DebugFlags.MinimalChirps)
                PostBillReactionChirps(policy.ToString(), yes, no);
        }

        /// <summary>
        /// Announce a consolidated "budget &amp; tax bill" representing the
        /// coalition's combined fiscal changes. Lists the top 3 changes with
        /// magnitude. Uses the same vote-tally pattern as <see cref="AnnounceBill"/>.
        /// </summary>
        public static void AnnounceBudgetAndTaxBill(PoliticsState st)
        {
            if (st.CoalitionPartyIds == null || st.CoalitionPartyIds.Count == 0) return;

            // Aggregate deltas across the coalition
            int dRes=0,dCom=0,dInd=0,dOff=0;
            int dEdu=0,dHea=0,dPol=0,dFire=0,dElec=0,dWater=0,dGar=0,dTrans=0,dBeaut=0,dRoads=0,dIndustry=0;
            foreach (var id in st.CoalitionPartyIds)
            {
                var m = Config.Parties[id].Modifiers;
                dRes += m.TaxDeltaRes;  dCom += m.TaxDeltaCom;
                dInd += m.TaxDeltaInd;  dOff += m.TaxDeltaOff;
                dEdu += m.BudgetDeltaEducation;
                dHea += m.BudgetDeltaHealth;
                dPol += m.BudgetDeltaPolice;
                dFire += m.BudgetDeltaFire;
                dElec += m.BudgetDeltaElectricity;
                dWater += m.BudgetDeltaWater;
                dGar += m.BudgetDeltaGarbage;
                dTrans += m.BudgetDeltaTransport;
                dBeaut += m.BudgetDeltaBeautification;
                dRoads += m.BudgetDeltaRoads;
                dIndustry += m.BudgetDeltaIndustry;
            }

            // Build the change list: (label, delta, isTax). Skip items below threshold.
            var changes = new List<KeyValuePair<string, int>>();
            AddChange(changes, "Residential tax", dRes, 1);
            AddChange(changes, "Commercial tax", dCom, 1);
            AddChange(changes, "Industrial tax", dInd, 1);
            AddChange(changes, "Office tax",     dOff, 1);
            AddChange(changes, "Education budget",   dEdu, 5);
            AddChange(changes, "Healthcare budget",  dHea, 5);
            AddChange(changes, "Police budget",      dPol, 5);
            AddChange(changes, "Fire budget",        dFire, 5);
            AddChange(changes, "Electricity budget", dElec, 5);
            AddChange(changes, "Water budget",       dWater, 5);
            AddChange(changes, "Garbage budget",     dGar, 5);
            AddChange(changes, "Public Transport budget", dTrans, 5);
            AddChange(changes, "Beautification budget",   dBeaut, 5);
            AddChange(changes, "Roads budget",       dRoads, 5);
            AddChange(changes, "Industry budget",    dIndustry, 5);

            if (changes.Count == 0)
            {
                PoliticsUserMod.Log("AnnounceBudgetAndTaxBill: no significant changes, skipping.");
                return;
            }

            // Sort by |delta| descending, take top 3
            changes.Sort((a, b) => Math.Abs(b.Value).CompareTo(Math.Abs(a.Value)));
            int top = Math.Min(3, changes.Count);
            var sb = new StringBuilder();
            for (int i = 0; i < top; i++)
            {
                if (i > 0) sb.Append(", ");
                var c = changes[i];
                string verb = c.Value > 0 ? "raise " : "cut ";
                sb.Append(verb).Append(c.Key).Append(" by ").Append(Math.Abs(c.Value)).Append("%");
            }
            if (changes.Count > top) sb.Append(" (and ").Append(changes.Count - top).Append(" more)");

            // Tally: coalition mostly yes; opposition distance-weighted to the
            // coalition's ideology center.
            int yes = 0, no = 0, abstain = 0;
            Vector3 coalCenter = Vector3.zero;
            foreach (var cid in st.CoalitionPartyIds) coalCenter += Config.Parties[cid].Ideology;
            if (st.CoalitionPartyIds.Count > 0) coalCenter /= st.CoalitionPartyIds.Count;
            var coalitionSet = new HashSet<int>(st.CoalitionPartyIds);
            for (int i = 0; i < Config.Parties.Length; i++)
            {
                int seats = (i < st.CurrentSeats.Length) ? st.CurrentSeats[i] : 0;
                if (seats <= 0) continue;
                float yesShare, abstainShare;
                if (coalitionSet.Contains(i))
                {
                    yesShare = 0.85f; abstainShare = 0.08f;
                }
                else
                {
                    float dist = (Config.Parties[i].Ideology - coalCenter).magnitude;
                    yesShare = Mathf.Clamp(0.45f - dist * 0.18f, 0.05f, 0.55f);
                    abstainShare = 0.15f;
                }
                int py = Mathf.RoundToInt(seats * yesShare);
                int pa = Mathf.RoundToInt(seats * abstainShare);
                int pn = Math.Max(0, seats - py - pa);
                yes += py; no += pn; abstain += pa;
            }
            // Post-hoc: ensure "passes" narrative since changes already applied.
            if (yes <= no) { int flip = (no - yes) + 1; yes += flip; no = Math.Max(0, no - flip); }

            int billNo = st.NextBillNumber++;
            string text = string.Format(
                "🏛 Parliament votes {0}-{1} to pass bill C-{2} (Budget & Tax): An Act to {3}.",
                yes, no, billNo, sb.ToString());
            if (abstain > 0) text += "  (" + abstain + " abstentions)";
            PostChirp("City News", text, 43u);
            PoliticsUserMod.Log("AnnounceBudgetAndTaxBill: " + text);

            if (!DebugFlags.MinimalChirps)
                PostBillReactionChirps("BudgetAndTax", yes, no);
        }

        private static void AddChange(List<KeyValuePair<string, int>> list, string label, int delta, int threshold)
        {
            if (Math.Abs(delta) >= threshold)
                list.Add(new KeyValuePair<string, int>(label, delta));
        }

        /// <summary>Turn a PascalCase policy enum name into a human-readable title.</summary>
        private static string FormatPolicyTitle(DistrictPolicies.Policies policy)
        {
            string raw = policy.ToString();
            // Known overrides for nicer titles
            switch (raw)
            {
                case "FreeTransport":    return "provide Free Public Transport";
                case "Recycling":        return "mandate City-Wide Recycling";
                case "SmokeDetectors":   return "require Smoke Detectors";
                case "EducationBoost":   return "fund an Education Boost";
                case "ExtraInsulation":  return "subsidize Home Insulation";
                case "BigBusiness":      return "support Big Business Benefits";
                case "HighTechHousing":  return "incentivize High-Tech Housing";
                case "DoubleTime":       return "enforce Double Time wages";
                case "NoHeavy":          return "ban Heavy Traffic downtown";
                case "OnlyAtNight":      return "permit Night-Only Operations";
                case "OldTown":          return "protect the Old Town Heritage";
                case "HeavyTrafficBan":  return "ban Heavy Traffic citywide";
            }
            // Fallback: insert spaces before capitals (camelCase → "Camel Case")
            var sb = new StringBuilder();
            sb.Append("enact ");
            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && char.IsUpper(raw[i])) sb.Append(' ');
                sb.Append(raw[i]);
            }
            return sb.ToString();
        }

        private static void ApplyCustomModifiers(int sign)
        {
            var st = PoliticsState.Instance;
            if (st.CoalitionPartyIds == null || st.CoalitionPartyIds.Count == 0)
            {
                PoliticsUserMod.Log("ApplyCustomModifiers: no coalition, skipping (sign=" + sign + ")");
                return;
            }
            PoliticsUserMod.Log("ApplyCustomModifiers sign=" + sign +
                ", coalition=" + st.CoalitionPartyIds.Count + " parties");

            // Aggregate modifier deltas across coalition parties.
            int dRes=0,dCom=0,dInd=0,dOff=0;
            int dEdu=0,dHea=0,dPol=0,dFire=0,dElec=0,dWater=0,dGar=0,dTrans=0,dBeaut=0,dRoads=0,dIndustry=0;
            foreach (var id in st.CoalitionPartyIds)
            {
                var m = Config.Parties[id].Modifiers;
                dRes += m.TaxDeltaRes;  dCom += m.TaxDeltaCom;
                dInd += m.TaxDeltaInd;  dOff += m.TaxDeltaOff;
                dEdu += m.BudgetDeltaEducation;
                dHea += m.BudgetDeltaHealth;
                dPol += m.BudgetDeltaPolice;
                dFire += m.BudgetDeltaFire;
                dElec += m.BudgetDeltaElectricity;
                dWater += m.BudgetDeltaWater;
                dGar += m.BudgetDeltaGarbage;
                dTrans += m.BudgetDeltaTransport;
                dBeaut += m.BudgetDeltaBeautification;
                dRoads += m.BudgetDeltaRoads;
                dIndustry += m.BudgetDeltaIndustry;
            }

            var em = Singleton<EconomyManager>.instance;
            try
            {
                // Taxes
                AdjustTax(em, ItemClass.Service.Residential,  ItemClass.SubService.ResidentialLow,  dRes * sign);
                AdjustTax(em, ItemClass.Service.Residential,  ItemClass.SubService.ResidentialHigh, dRes * sign);
                AdjustTax(em, ItemClass.Service.Commercial,   ItemClass.SubService.CommercialLow,   dCom * sign);
                AdjustTax(em, ItemClass.Service.Commercial,   ItemClass.SubService.CommercialHigh,  dCom * sign);
                AdjustTax(em, ItemClass.Service.Industrial,   ItemClass.SubService.None,            dInd * sign);
                AdjustTax(em, ItemClass.Service.Office,       ItemClass.SubService.None,            dOff * sign);

                // Budgets (full city services coverage)
                AdjustBudget(em, ItemClass.Service.Education,        dEdu     * sign);
                AdjustBudget(em, ItemClass.Service.HealthCare,       dHea     * sign);
                AdjustBudget(em, ItemClass.Service.PoliceDepartment, dPol     * sign);
                AdjustBudget(em, ItemClass.Service.FireDepartment,   dFire    * sign);
                AdjustBudget(em, ItemClass.Service.Electricity,      dElec    * sign);
                AdjustBudget(em, ItemClass.Service.Water,            dWater   * sign);
                AdjustBudget(em, ItemClass.Service.Garbage,          dGar     * sign);
                AdjustBudget(em, ItemClass.Service.PublicTransport,  dTrans   * sign);
                AdjustBudget(em, ItemClass.Service.Beautification,   dBeaut   * sign);
                AdjustBudget(em, ItemClass.Service.Road,             dRoads   * sign);
                AdjustBudget(em, ItemClass.Service.Industrial,       dIndustry* sign);
            }
            catch (Exception e)
            {
                PoliticsUserMod.Log("Failed to adjust economy: " + e.Message);
            }
        }

        private static void AdjustTax(EconomyManager em, ItemClass.Service svc, ItemClass.SubService sub, int delta)
        {
            if (delta == 0) return;
            try
            {
                int dayCur = em.GetTaxRate(svc, sub, ItemClass.Level.None);
                int newRate = Mathf.Clamp(dayCur + delta, 1, 29);
                var capSvc = svc; var capSub = sub; var capRate = newRate;
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    try
                    {
                        Singleton<EconomyManager>.instance.SetTaxRate(capSvc, capSub, ItemClass.Level.None, capRate);
                        int verify = Singleton<EconomyManager>.instance.GetTaxRate(capSvc, capSub, ItemClass.Level.None);
                        PoliticsUserMod.Log("SimWrite tax " + capSvc + "/" + capSub + ": target=" + capRate + ", readback=" + verify +
                                            (verify == capRate ? " OK" : " MISMATCH"));
                    }
                    catch (Exception ex) { PoliticsUserMod.Log("SetTaxRate(" + capSvc + "/" + capSub + ") on sim thread: " + ex.Message); }
                });
                PoliticsUserMod.Log("Queued tax " + svc + "/" + sub + ": " + dayCur + " -> " + newRate + " (delta " + delta + ")");
            }
            catch (Exception e)
            {
                PoliticsUserMod.Log("AdjustTax " + svc + "/" + sub + " failed: " + e.Message);
            }
        }

        private static void AdjustBudget(EconomyManager em, ItemClass.Service svc, int delta)
        {
            if (delta == 0) return;
            try
            {
                int dayCur = em.GetBudget(svc, ItemClass.SubService.None, false);
                int newDay = Mathf.Clamp(dayCur + delta, 50, 150);
                int nightCur = em.GetBudget(svc, ItemClass.SubService.None, true);
                int newNight = Mathf.Clamp(nightCur + delta, 50, 150);
                var capSvc = svc; var capDay = newDay; var capNight = newNight;
                Singleton<SimulationManager>.instance.AddAction(() =>
                {
                    try
                    {
                        Singleton<EconomyManager>.instance.SetBudget(capSvc, ItemClass.SubService.None, capDay, false);
                        Singleton<EconomyManager>.instance.SetBudget(capSvc, ItemClass.SubService.None, capNight, true);
                        int vd = Singleton<EconomyManager>.instance.GetBudget(capSvc, ItemClass.SubService.None, false);
                        int vn = Singleton<EconomyManager>.instance.GetBudget(capSvc, ItemClass.SubService.None, true);
                        PoliticsUserMod.Log("SimWrite budget " + capSvc + ": dayTgt=" + capDay + " read=" + vd + ", nightTgt=" + capNight + " read=" + vn +
                                            (vd == capDay && vn == capNight ? " OK" : " MISMATCH"));
                    }
                    catch (Exception ex) { PoliticsUserMod.Log("SetBudget(" + capSvc + ") on sim thread: " + ex.Message); }
                });
                PoliticsUserMod.Log("Queued budget " + svc + ": day " + dayCur + "->" + newDay + ", night " + nightCur + "->" + newNight + " (delta " + delta + ")");
            }
            catch (Exception e)
            {
                PoliticsUserMod.Log("AdjustBudget " + svc + " failed: " + e.Message);
            }
        }

        // ---- Notifications -----------------------------------------------

        public static void ShowToast(string msg)
        {
            PoliticsUserMod.Log("TOAST: " + msg);
            PoliticsPanel.LatestToast = msg;
            PoliticsPanel.LatestToastTime = Time.realtimeSinceStartup;
        }

        /// <summary>
        /// Post a chirp (in-game Twitter-style notification) from a given sender.
        /// CS1's MessageManager queues a <see cref="MessageBase"/> which becomes
        /// a Chirper bubble. Safe to call before the manager exists (no-op).
        /// </summary>
        public static void PostChirp(string sender, string text, uint senderSeed = 0)
        {
            try
            {
                var mm = Singleton<MessageManager>.instance;
                if (mm == null) return;
                if (senderSeed == 0u)
                {
                    // Use a hash of the sender name so repeated chirps from the
                    // same party cluster in the chirper.
                    senderSeed = (uint)(Math.Abs(sender.GetHashCode()) | 1);
                }
                mm.QueueMessage(new PoliticsChirpMessage(sender, text, senderSeed));
            }
            catch (Exception e)
            {
                PoliticsUserMod.Log("PostChirp failed: " + e.Message);
            }
        }

        /// <summary>
        /// Pick a random created (non-dummy) citizen and return their display
        /// name. Returns "Concerned Citizen" if lookup fails.
        /// </summary>
        public static string PickRandomCitizenName(out uint citizenId)
        {
            citizenId = 0u;
            try
            {
                var cm = Singleton<CitizenManager>.instance;
                uint size = cm.m_citizens.m_size;
                for (int tries = 0; tries < 30; tries++)
                {
                    uint idx = (uint)_rng.Next(1, (int)size);
                    var c = cm.m_citizens.m_buffer[idx];
                    if ((c.m_flags & Citizen.Flags.Created) == 0) continue;
                    if ((c.m_flags & Citizen.Flags.DummyTraffic) != 0) continue;
                    // Children/teens usually don't chirp about politics
                    var g = Citizen.GetAgeGroup(c.m_age);
                    if (g == Citizen.AgeGroup.Child || g == Citizen.AgeGroup.Teen) continue;
                    citizenId = idx;
                    string name = cm.GetCitizenName(idx);
                    if (string.IsNullOrEmpty(name)) continue;
                    return name;
                }
            }
            catch { }
            return "Concerned Citizen";
        }

        /// <summary>Post one right-wing-leaning citizen chirp about the deficit.</summary>
        public static void PostRandomCitizenDeficitChirp()
        {
            // Pick a right-wing party for the hashtag.
            int rightPartyId = -1;
            float bestX = float.MinValue;
            for (int i = 0; i < Config.Parties.Length; i++)
            {
                if (Config.Parties[i].Ideology.x > bestX)
                {
                    bestX = Config.Parties[i].Ideology.x;
                    rightPartyId = i;
                }
            }
            string tag = rightPartyId >= 0 ? "#" + Config.Parties[rightPartyId].ShortName : "";

            string[] pool = new[]
            {
                "My taxes are too high. Time for a change. " + tag,
                "This government can't balance a budget. Voting right next time.",
                "We need lower taxes and more jobs. " + tag,
                "Running a city is like running a business — cut the waste.",
                "Another deficit? I'm done funding this. " + tag,
                "Big spenders are bankrupting us. Switching my vote.",
                "If my household ran like this, we'd be homeless.",
                "Austerity now — or chaos later. " + tag,
            };

            uint cid;
            string name = PickRandomCitizenName(out cid);
            if (cid == 0u) return; // no real citizen found — skip; clicking a fake ID does nothing
            string msg = pool[_rng.Next(0, pool.Length)];
            PostChirp(name, msg, cid);
        }

        /// <summary>
        /// Post 1-2 short citizen reactions after a bill passes. Mix of
        /// supportive and opposed, weighted toward the majority side of the vote.
        /// </summary>
        public static void PostBillReactionChirps(string billKey, int yes, int no)
        {
            int total = Math.Max(1, yes + no);
            float supportShare = yes / (float)total;
            // Count: 1 chirp 60% of the time, 2 chirps 40% of the time.
            // 25% chance of a single citizen reaction, 75% silence.
            int count = _rng.NextDouble() < 0.25 ? 1 : 0;
            if (count == 0) return;

            string[] supportive = new[]
            {
                "Finally! This was long overdue. #Win",
                "Great move by parliament today. Makes a real difference.",
                "I actually feel represented for once.",
                "Common sense wins. Well done.",
                "This is why I voted. Keep it going.",
                "Seeing real change in my neighborhood.",
            };
            string[] opposed = new[]
            {
                "Waste of money. They don't listen to us.",
                "So much for campaign promises. I'm done.",
                "Wrong priorities. Again.",
                "Wait until next election. We remember.",
                "Taxpayer funded nonsense.",
                "This bill helps no one I know.",
            };

            for (int i = 0; i < count; i++)
            {
                bool isSupport = _rng.NextDouble() < supportShare;
                var pool = isSupport ? supportive : opposed;
                string msg = pool[_rng.Next(0, pool.Length)];
                uint cid;
                string name = PickRandomCitizenName(out cid);
                if (cid == 0u) continue; // need a real citizen ID for the chirp to be clickable
                PostChirp(name, msg, cid);
            }
        }

        public static void ShowResultsPopup(ElectionResult r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Election " + r.Year + "-" + r.Month + " ===");
            for (int i = 0; i < r.SeatsByParty.Length; i++)
            {
                sb.AppendLine(string.Format("  {0,-25} {1,3} seats  ({2:P1})",
                    Config.Parties[i].FullName, r.SeatsByParty[i], r.VoteShareByParty[i]));
            }
            sb.Append("Turnout: " + (int)(r.Turnout * 100) + "%");
            PoliticsPanel.ResultsPopupText = sb.ToString();
            PoliticsPanel.ResultsPopupShownUntil = Time.realtimeSinceStartup + 30f;
            PoliticsUserMod.Log(sb.ToString());
        }
    }

    // ========================================================================
    //  CHIRPER MESSAGE — custom MessageBase subclass so our announcements
    //  show up in the in-game Chirper feed (blue bird). See
    //  MessageManager.QueueMessage.
    // ========================================================================
    public class PoliticsChirpMessage : MessageBase
    {
        private string _sender;
        private string _message;
        private uint   _id;

        // Parameterless constructor required for deserialization.
        public PoliticsChirpMessage() { }

        public PoliticsChirpMessage(string sender, string message, uint id)
        {
            _sender = sender;
            _message = message;
            _id = id;
        }

        public override uint GetSenderID() { return _id; }
        public override string GetSenderName() { return _sender; }
        public override string GetText() { return _message; }

        public override bool IsSimilarMessage(MessageBase other)
        {
            var m = other as PoliticsChirpMessage;
            if (m == null) return false;
            // Same sender (by id) posting the same message → duplicate.
            // Don't dedupe across different senders — two citizens can have
            // the same take, and we want each to be visible as its own chirp.
            return m._id == _id && (m._message ?? "") == (_message ?? "");
        }

        public override void Serialize(ColossalFramework.IO.DataSerializer s)
        {
            s.WriteSharedString(_sender);
            s.WriteUInt32(_id);
            s.WriteSharedString(_message);
        }

        public override void Deserialize(ColossalFramework.IO.DataSerializer s)
        {
            _sender  = s.ReadSharedString();
            _id      = s.ReadUInt32();
            _message = s.ReadSharedString();
        }

        public override void AfterDeserialize(ColossalFramework.IO.DataSerializer s) { }
    }

    // ========================================================================
    //  UI PANEL — bar chart, coalition, policies, term countdown.
    // ========================================================================
    // ========================================================================
    //  HEMICYCLE VIEW — semi-circular parliament seat visualization.
    //  Arranges N seats in concentric arcs and colors each by party.
    //  Coalition seats are drawn with a subtle outer glow (lighter variant).
    // ========================================================================
    public class HemicycleView : UIComponent
    {
        private int[] _seats = new int[0];
        private HashSet<int> _coalition = new HashSet<int>();
        private int _totalSeats;

        // Cached seat dots (world-space relative to this component)
        private struct SeatDot { public Vector2 Pos; public int PartyId; public float Radius; }
        private SeatDot[] _dots;

        public void SetData(int[] seatsByParty, IEnumerable<int> coalitionPartyIds, int total)
        {
            _seats = seatsByParty;
            _totalSeats = Math.Max(1, total);
            _coalition.Clear();
            if (coalitionPartyIds != null)
                foreach (var id in coalitionPartyIds) _coalition.Add(id);
            Recompute();
            Invalidate();
        }

        private static Texture2D _dotTex;
        private static void EnsureTex()
        {
            if (_dotTex != null) return;
            // Soft round-ish dot: 16×16 with circular alpha falloff.
            const int sz = 16;
            _dotTex = new Texture2D(sz, sz, TextureFormat.ARGB32, false);
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = x - (sz - 1) / 2f;
                float dy = y - (sz - 1) / 2f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / ((sz - 1) / 2f);
                float a = r <= 0.85f ? 1f : Mathf.Clamp01(1f - (r - 0.85f) / 0.15f);
                _dotTex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            _dotTex.Apply();
        }

        /// <summary>
        /// Allocate seat positions along concentric arcs spanning a half-circle.
        /// Uses a simple algorithm: pick number of rows to make seat density
        /// roughly uniform, distribute seats proportionally by arc length, then
        /// fill arcs left-to-right with parties in ideology order.
        /// </summary>
        private void Recompute()
        {
            if (_seats == null || _seats.Length == 0) { _dots = new SeatDot[0]; return; }

            // Layout params
            float padding = 10f;
            float w = width  - 2 * padding;
            float h = height - 2 * padding;
            // The hemicycle uses a half-circle in the TOP half of the component
            // (baseline along the bottom). center at bottom-middle.
            Vector2 center = new Vector2(padding + w / 2f, padding + h);
            float outerR = Mathf.Min(w / 2f, h);
            // Choose number of rows based on total seats (visually pleasant).
            int rows = _totalSeats <= 50  ? 3 :
                       _totalSeats <= 100 ? 5 :
                       _totalSeats <= 200 ? 6 :
                       _totalSeats <= 400 ? 8 : 10;
            float innerR = outerR * 0.45f;

            // Compute per-row arc radius and how many seats on each row.
            // Proportional to arc length (radius), so density is uniform.
            float[] radii = new float[rows];
            float totalArcWeight = 0f;
            for (int r = 0; r < rows; r++)
            {
                float t = rows == 1 ? 0f : r / (float)(rows - 1);
                radii[r] = Mathf.Lerp(innerR, outerR, t);
                totalArcWeight += radii[r];
            }
            int[] rowCounts = new int[rows];
            int assigned = 0;
            for (int r = 0; r < rows; r++)
            {
                rowCounts[r] = Mathf.Max(1, Mathf.RoundToInt(_totalSeats * (radii[r] / totalArcWeight)));
                assigned += rowCounts[r];
            }
            // Adjust remainder so sum equals _totalSeats
            int diff = _totalSeats - assigned;
            for (int r = rows - 1; r >= 0 && diff != 0; r--)
            {
                if (diff > 0) { rowCounts[r]++; diff--; }
                else if (rowCounts[r] > 1) { rowCounts[r]--; diff++; }
            }

            // Party ordering: left-to-right by political ideology economic axis
            // (left parties on the left of the hemicycle).
            int[] partyOrder = new int[_seats.Length];
            for (int i = 0; i < partyOrder.Length; i++) partyOrder[i] = i;
            Array.Sort(partyOrder, (a, b) =>
                Config.Parties[a].Ideology.x.CompareTo(Config.Parties[b].Ideology.x));

            // Flatten seats into an ordered sequence so we know which
            // sequential seat index belongs to which party.
            int[] seatOwner = new int[_totalSeats];
            int k = 0;
            foreach (var pid in partyOrder)
            {
                int n = (pid < _seats.Length) ? _seats[pid] : 0;
                for (int j = 0; j < n && k < seatOwner.Length; j++) seatOwner[k++] = pid;
            }
            // Unassigned (should not happen unless seat totals mismatch)
            while (k < seatOwner.Length) seatOwner[k++] = -1;

            // Compute dot radius so dots don't overlap on the tightest row.
            int widestRow = 0;
            for (int r = 0; r < rows; r++) if (rowCounts[r] > rowCounts[widestRow]) widestRow = r;
            float avgArc = Mathf.PI * radii[widestRow] / rowCounts[widestRow];
            float dotR = Mathf.Max(2f, avgArc * 0.42f);

            _dots = new SeatDot[_totalSeats];
            // Build the list of all seat positions across all rows, with their
            // (row, angle). Then sort by angle DESC (π → 0) so we place seats
            // strictly left-to-right regardless of row. This way ideology
            // ordering flows left → right across the whole hemicycle instead
            // of inner-to-outer.
            var positions = new List<KeyValuePair<float, Vector2>>(_totalSeats);
            for (int r = 0; r < rows; r++)
            {
                int n = rowCounts[r];
                for (int j = 0; j < n; j++)
                {
                    float t = n == 1 ? 0.5f : j / (float)(n - 1);
                    float angle = Mathf.PI - Mathf.PI * t;
                    float x = center.x + radii[r] * Mathf.Cos(angle);
                    float y = center.y - radii[r] * Mathf.Sin(angle);
                    positions.Add(new KeyValuePair<float, Vector2>(angle, new Vector2(x, y)));
                }
            }
            // Sort left-to-right (large angle = left, angle 0 = right)
            positions.Sort((a, b) => b.Key.CompareTo(a.Key));

            for (int i = 0; i < positions.Count && i < _totalSeats; i++)
            {
                _dots[i] = new SeatDot
                {
                    Pos = positions[i].Value,
                    PartyId = seatOwner[i],
                    Radius = dotR
                };
            }
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            Recompute();
            Invalidate();
        }

        // Render seat dots via OnGUI. (UIComponent OnGUI is called by Unity.)
        private void OnGUI()
        {
            if (_dots == null || _dots.Length == 0) return;
            if (!isVisible) return;
            if (parent == null || !((UIComponent)parent).isVisible) return;

            EnsureTex();
            var oldColor = GUI.color;

            // Translate from component-local coords to screen coords.
            Vector3 absPos = absolutePosition;
            UIView view = GetUIView();
            float ratio = view != null ? view.PixelsToUnits() : 1f;
            // UIComponent.absolutePosition is in UI units; convert to screen pixels.
            // The GUI.DrawTexture uses screen pixels with origin top-left.
            float screenScaleX = Screen.width  / (float)view.fixedWidth;
            float screenScaleY = Screen.height / (float)view.fixedHeight;

            for (int i = 0; i < _dots.Length; i++)
            {
                var d = _dots[i];
                if (d.PartyId < 0) continue;
                var party = (d.PartyId < Config.Parties.Length) ? Config.Parties[d.PartyId] : null;
                if (party == null) continue;
                Color c = party.Color;
                if (_coalition.Contains(d.PartyId))
                {
                    // brighten slightly for coalition seats
                    c = Color.Lerp((Color)party.Color, Color.white, 0.25f);
                }

                float sx = (absPos.x + d.Pos.x) * screenScaleX;
                float sy = (absPos.y + d.Pos.y) * screenScaleY;
                float sr = d.Radius * Mathf.Min(screenScaleX, screenScaleY);

                // Outer outline for coalition seats
                if (_coalition.Contains(d.PartyId))
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.9f);
                    GUI.DrawTexture(new Rect(sx - sr - 1, sy - sr - 1, sr * 2 + 2, sr * 2 + 2), _dotTex);
                }
                GUI.color = c;
                GUI.DrawTexture(new Rect(sx - sr, sy - sr, sr * 2, sr * 2), _dotTex);
            }

            GUI.color = oldColor;
        }
    }

    // ========================================================================
    //  PARTY LEGEND ROW — small compact horizontal stack of party swatches
    //  with seat counts, drawn underneath the hemicycle.
    // ========================================================================
    public class PartyLegendRow : UIPanel
    {
        private UILabel[] _items;

        public void Build(int itemsCount)
        {
            _items = new UILabel[itemsCount];
            float x = 0f;
            float cellW = (width - 4f) / itemsCount;
            for (int i = 0; i < itemsCount; i++)
            {
                var lbl = AddUIComponent<UILabel>();
                lbl.textScale = 0.72f;
                lbl.processMarkup = true;
                lbl.relativePosition = new Vector3(x, 2);
                lbl.size = new Vector2(cellW, 22);
                _items[i] = lbl;
                x += cellW;
            }
        }

        public void Refresh(int[] seats, HashSet<int> coalition, int total)
        {
            if (_items == null) return;
            for (int i = 0; i < _items.Length; i++)
            {
                var p = Config.Parties[i];
                int s = i < seats.Length ? seats[i] : 0;
                bool inCoal = coalition != null && coalition.Contains(i);
                // Coalition parties get a ★; non-coalition get a small ●.
                string marker = inCoal ? "★" : "●";
                string text = string.Format(
                    "<color #{0:X2}{1:X2}{2:X2}>{3}</color> {4} {5}",
                    p.Color.r, p.Color.g, p.Color.b, marker, p.ShortName, s);
                _items[i].text = text;
            }
        }
    }

    public class PoliticsPanel : UIPanel
    {
        public static string LatestToast;
        public static float  LatestToastTime;
        public static string ResultsPopupText;
        public static float  ResultsPopupShownUntil;

        private UILabel _title;
        private UILabel _phaseLabel;
        private UILabel _coalitionLabel;
        private UILabel _policiesLabel;
        private HemicycleView _hemi;
        private PartyLegendRow _legend;
        private UIButton _overlayBtn;
        private UIButton _forceBtn;

        // Runtime config sliders
        private UISlider _termSlider, _campSlider, _coolSlider;
        private UILabel  _termLbl,    _campLbl,    _coolLbl;

        public override void Start()
        {
            base.Start();
            width = 520;
            height = 640;
            backgroundSprite = "MenuPanel2";
            canFocus = true;
            isInteractive = true;
            relativePosition = new Vector3(120, 80);
            BuildUI();
        }

        private void BuildUI()
        {
            _title = AddUIComponent<UILabel>();
            _title.text = "Politics & Elections";
            _title.textScale = 1.2f;
            _title.relativePosition = new Vector3(15, 10);

            UIHelpers.MakeDraggable(this);

            var close = AddUIComponent<UIButton>();
            close.text = "X";
            close.size = new Vector2(28, 24);
            close.relativePosition = new Vector3(width - 35, 8);
            close.normalBgSprite = "ButtonMenu";
            close.hoveredBgSprite = "ButtonMenuHovered";
            close.pressedBgSprite = "ButtonMenuPressed";
            close.eventClick += (c, p) => { isVisible = false; };

            _phaseLabel = AddUIComponent<UILabel>();
            _phaseLabel.textScale = 0.9f;
            _phaseLabel.relativePosition = new Vector3(15, 40);

            // -------- Parliament hemicycle + legend --------
            _hemi = AddUIComponent<HemicycleView>();
            _hemi.relativePosition = new Vector3(15, 70);
            _hemi.size = new Vector2(width - 30, 170);

            _legend = AddUIComponent<PartyLegendRow>();
            _legend.relativePosition = new Vector3(15, 70 + 170 + 2);
            _legend.size = new Vector2(width - 30, 22);
            _legend.Build(PartyCountRef.Value);

            _coalitionLabel = AddUIComponent<UILabel>();
            _coalitionLabel.textScale = 0.85f;
            _coalitionLabel.relativePosition = new Vector3(15, 270);

            _policiesLabel = AddUIComponent<UILabel>();
            _policiesLabel.textScale = 0.8f;
            _policiesLabel.relativePosition = new Vector3(15, 293);
            // Single-line label — the list is truncated to avoid overflow.
            _policiesLabel.autoSize = false;
            _policiesLabel.size = new Vector2(width - 30, 18);
            _policiesLabel.clipChildren = true;

            _overlayBtn = AddUIComponent<UIButton>();
            _overlayBtn.text = "Overlay: Off";
            _overlayBtn.size = new Vector2(200, 32);
            _overlayBtn.relativePosition = new Vector3(15, height - 90);
            _overlayBtn.normalBgSprite = "ButtonMenu";
            _overlayBtn.hoveredBgSprite = "ButtonMenuHovered";
            _overlayBtn.pressedBgSprite = "ButtonMenuPressed";
            _overlayBtn.textColor = Color.white;
            _overlayBtn.eventClick += (c, p) =>
            {
                HarmonyPatcher.CycleOverlayAndSync();
            };

            _forceBtn = AddUIComponent<UIButton>();
            _forceBtn.text = "Call snap election";
            _forceBtn.size = new Vector2(200, 32);
            _forceBtn.relativePosition = new Vector3(230, height - 90);
            _forceBtn.normalBgSprite = "ButtonMenu";
            _forceBtn.hoveredBgSprite = "ButtonMenuHovered";
            _forceBtn.pressedBgSprite = "ButtonMenuPressed";
            _forceBtn.textColor = Color.white;
            _forceBtn.eventClick += (c, p) =>
            {
                if (PoliticsState.Instance != null)
                    ElectionEngine.TriggerCampaign(force: true);
            };

            // -------- Manage Parties button --------
            var manageBtn = AddUIComponent<UIButton>();
            manageBtn.text = "Manage Parties";
            manageBtn.size = new Vector2(200, 32);
            manageBtn.relativePosition = new Vector3(15, height - 50);
            manageBtn.normalBgSprite = "ButtonMenu";
            manageBtn.hoveredBgSprite = "ButtonMenuHovered";
            manageBtn.pressedBgSprite = "ButtonMenuPressed";
            manageBtn.textColor = Color.white;
            manageBtn.eventClick += (c, p) =>
            {
                PartyEditorPanel.Toggle();
            };

            // -------- Voter Traits button --------
            var traitsBtn = AddUIComponent<UIButton>();
            traitsBtn.text = "Voter Traits";
            traitsBtn.size = new Vector2(200, 32);
            traitsBtn.relativePosition = new Vector3(230, height - 50);
            traitsBtn.normalBgSprite = "ButtonMenu";
            traitsBtn.hoveredBgSprite = "ButtonMenuHovered";
            traitsBtn.pressedBgSprite = "ButtonMenuPressed";
            traitsBtn.textColor = Color.white;
            traitsBtn.eventClick += (c, p) =>
            {
                VoterTraitsPanel.Toggle();
            };

            // -------- Election Stats button --------
            var statsBtn = AddUIComponent<UIButton>();
            statsBtn.text = "Election Stats";
            statsBtn.size = new Vector2(200, 32);
            statsBtn.relativePosition = new Vector3(230, height - 130);
            statsBtn.normalBgSprite = "ButtonMenu";
            statsBtn.hoveredBgSprite = "ButtonMenuHovered";
            statsBtn.pressedBgSprite = "ButtonMenuPressed";
            statsBtn.textColor = Color.white;
            statsBtn.eventClick += (c, p) =>
            {
                ElectionStatsPanel.Toggle();
            };

            // -------- Minimize Chirps checkbox --------
            var minChirpsCB = AddUIComponent<UICheckBox>();
            minChirpsCB.relativePosition = new Vector3(15, height - 127);
            minChirpsCB.size = new Vector2(200f, 22f);

            var cbUnchecked = minChirpsCB.AddUIComponent<UISprite>();
            cbUnchecked.spriteName = "AchievementCheckedFalse";
            cbUnchecked.size = new Vector2(16f, 16f);
            cbUnchecked.relativePosition = new Vector3(0f, 3f);
            var cbChecked = minChirpsCB.AddUIComponent<UISprite>();
            cbChecked.spriteName = "AchievementCheckedTrue";
            cbChecked.size = new Vector2(16f, 16f);
            cbChecked.relativePosition = new Vector3(0f, 3f);
            minChirpsCB.checkedBoxObject = cbChecked;

            var cbLbl = minChirpsCB.AddUIComponent<UILabel>();
            cbLbl.text = "Minimize chirps";
            cbLbl.textScale = 0.8f;
            cbLbl.relativePosition = new Vector3(22f, 3f);
            cbLbl.tooltip = "Only post essential chirps: campaign start, election results, and bill passages.";

            minChirpsCB.isChecked = DebugFlags.MinimalChirps;
            minChirpsCB.eventCheckChanged += (c, v) => { DebugFlags.MinimalChirps = v; };

            // -------- Runtime config sliders --------
            float sliderY = 310f;
            var header = AddUIComponent<UILabel>();
            header.text = "Election timings (editable)";
            header.textScale = 0.95f;
            header.relativePosition = new Vector3(15, sliderY);
            sliderY += 26f;

            _termLbl = BuildSliderRow(sliderY, "Term length", out _termSlider,
                RuntimeConfig.MinTerm, RuntimeConfig.MaxTerm, RuntimeConfig.TermLengthDays,
                v => { RuntimeConfig.TermLengthDays = v; RuntimeConfig.ClampAll(); UpdateSliderLabels(); });
            sliderY += 48f;

            _campLbl = BuildSliderRow(sliderY, "Campaign length", out _campSlider,
                RuntimeConfig.MinCampaign, RuntimeConfig.MaxCampaign, RuntimeConfig.CampaignLengthDays,
                v => { RuntimeConfig.CampaignLengthDays = v; RuntimeConfig.ClampAll(); UpdateSliderLabels(); });
            sliderY += 48f;

            _coolLbl = BuildSliderRow(sliderY, "Re-election cooldown", out _coolSlider,
                RuntimeConfig.MinCooldown, RuntimeConfig.MaxCooldown, RuntimeConfig.ReElectionCooldownDays,
                v => { RuntimeConfig.ReElectionCooldownDays = v; RuntimeConfig.ClampAll(); UpdateSliderLabels(); });

            UpdateSliderLabels();
        }

        private UILabel BuildSliderRow(float y, string name, out UISlider slider,
                                       float min, float max, float value,
                                       Action<float> onChanged)
        {
            var nameLbl = AddUIComponent<UILabel>();
            nameLbl.text = name;
            nameLbl.textScale = 0.8f;
            nameLbl.relativePosition = new Vector3(15, y);

            var valueLbl = AddUIComponent<UILabel>();
            valueLbl.textScale = 0.8f;
            valueLbl.relativePosition = new Vector3(width - 115, y);

            slider = AddUIComponent<UISlider>();
            slider.relativePosition = new Vector3(15, y + 18);
            slider.size = new Vector2(width - 30, 16);
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = 1f;
            slider.value    = Mathf.Clamp(value, min, max);

            // Slider visual bits
            var track = slider.AddUIComponent<UISlicedSprite>();
            track.relativePosition = new Vector3(0, 7);
            track.size = new Vector2(slider.width, 3);
            track.spriteName = "BudgetSlider";

            var thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.size = new Vector2(12, 16);
            thumb.spriteName = "SliderBudget";
            slider.thumbObject = thumb;

            slider.eventValueChanged += (c, v) => { onChanged(v); };

            // Return value label so we can update it from UpdateSliderLabels
            return valueLbl;
        }

        private void UpdateSliderLabels()
        {
            if (_termLbl != null) _termLbl.text = ((int)RuntimeConfig.TermLengthDays) + " days";
            if (_campLbl != null) _campLbl.text = ((int)RuntimeConfig.CampaignLengthDays) + " days";
            if (_coolLbl != null) _coolLbl.text = ((int)RuntimeConfig.ReElectionCooldownDays) + " days";
            // Keep sliders in sync if ClampAll modified values
            if (_termSlider != null && Mathf.Abs(_termSlider.value - RuntimeConfig.TermLengthDays) > 0.5f)
                _termSlider.value = RuntimeConfig.TermLengthDays;
            if (_campSlider != null && Mathf.Abs(_campSlider.value - RuntimeConfig.CampaignLengthDays) > 0.5f)
                _campSlider.value = RuntimeConfig.CampaignLengthDays;
            if (_coolSlider != null && Mathf.Abs(_coolSlider.value - RuntimeConfig.ReElectionCooldownDays) > 0.5f)
                _coolSlider.value = RuntimeConfig.ReElectionCooldownDays;
        }

        public override void Update()
        {
            base.Update();
            // Hotkey: Ctrl+P toggles
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(Config.TogglePanelKey)) isVisible = !isVisible;
            }
            if (!isVisible) return;

            var st = PoliticsState.Instance;
            if (st == null) return;

            _phaseLabel.text = string.Format("Phase: {0} | Day {1}/{2} of term",
                st.Phase, (int)st.DaysSinceLastElection, (int)RuntimeConfig.TermLengthDays);

            // Parliament hemicycle + legend
            var coalSet = new HashSet<int>(st.CoalitionPartyIds ?? new List<int>());
            if (_hemi != null) _hemi.SetData(st.CurrentSeats, coalSet, Config.ParliamentSeats);
            if (_legend != null) _legend.Refresh(st.CurrentSeats, coalSet, Config.ParliamentSeats);

            // Coalition
            if (st.CoalitionPartyIds != null && st.CoalitionPartyIds.Count > 0)
            {
                var sb = new StringBuilder("Coalition: ");
                for (int i = 0; i < st.CoalitionPartyIds.Count; i++)
                {
                    if (i > 0) sb.Append(" + ");
                    sb.Append(Config.Parties[st.CoalitionPartyIds[i]].ShortName);
                }
                int totalSeats = 0;
                foreach (var id in st.CoalitionPartyIds) totalSeats += st.CurrentSeats[id];
                sb.Append("  (" + totalSeats + "/" + Config.ParliamentSeats + ")");
                _coalitionLabel.text = sb.ToString();
            }
            else
            {
                _coalitionLabel.text = "Coalition: (none)";
            }

            // Policies — single-line summary with truncation.
            if (st.AppliedVanillaPolicies != null && st.AppliedVanillaPolicies.Count > 0)
            {
                const int maxShown = 4;
                var sb = new StringBuilder("Active policies: ");
                int shown = Math.Min(maxShown, st.AppliedVanillaPolicies.Count);
                for (int i = 0; i < shown; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(st.AppliedVanillaPolicies[i]);
                }
                if (st.AppliedVanillaPolicies.Count > shown)
                    sb.Append(" (+").Append(st.AppliedVanillaPolicies.Count - shown).Append(" more)");
                _policiesLabel.text = sb.ToString();
                _policiesLabel.tooltip = string.Join(", ",
                    st.AppliedVanillaPolicies.ConvertAll(p => p.ToString()).ToArray());
            }
            else
            {
                _policiesLabel.text = "Active policies: (none)";
                _policiesLabel.tooltip = "";
            }

            _overlayBtn.text = "Overlay: " + st.Overlay;
        }

        private void OnGUI()
        {
            // Toast
            if (!string.IsNullOrEmpty(LatestToast) &&
                Time.realtimeSinceStartup - LatestToastTime < Config.NotificationDurationS)
            {
                var s = new GUIStyle(GUI.skin.box);
                s.fontSize = 16;
                s.normal.textColor = Color.white;
                GUI.Box(new Rect(Screen.width / 2f - 250f, 40f, 500f, 40f), LatestToast, s);
            }
            // Results popup
            if (!string.IsNullOrEmpty(ResultsPopupText) &&
                Time.realtimeSinceStartup < ResultsPopupShownUntil)
            {
                var s = new GUIStyle(GUI.skin.box);
                s.fontSize = 14;
                s.normal.textColor = Color.white;
                s.alignment = TextAnchor.UpperLeft;
                GUI.Box(new Rect(Screen.width - 460f, 100f, 440f, 260f), ResultsPopupText, s);
            }
        }
    }

    // ========================================================================
    //  INFO-VIEW OVERLAY — draws party/turnout/satisfaction dots over buildings.
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
                    "No election yet — call a snap election!", s);
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

    // ========================================================================
    //  INFO-VIEW BUTTON — standalone UI button sitting on screen, styled like
    //  the vanilla Info View tab buttons. Clicking it cycles:
    //      Off → Party → Turnout → Satisfaction → Off
    //  Each cycle forces a building color refresh so the tint changes
    //  instantly.
    // ========================================================================
    public class PoliticsInfoViewButton : UIPanel
    {
        private UIButton _btn;
        private UISprite _colorDot;

        public override void Start()
        {
            base.Start();

            // The Info Views toggle button anchors at the bottom-left of the screen.
            // We sit just above it so we're visually associated with the info views.
            size = new Vector2(170f, 38f);
            name = "PoliticsInfoViewButton";

            // Try to anchor next to the real Info Views panel if we can find it;
            // otherwise fall back to a fixed screen position.
            Vector2 anchor = FindInfoViewAnchor();
            relativePosition = anchor;

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
            _btn.textPadding = new RectOffset(24, 6, 6, 6);
            _btn.tooltip = "Politics info view: cycle Party / Turnout / Satisfaction";
            _btn.eventClick += OnClick;

            // Small colored swatch on the left side of the button
            _colorDot = _btn.AddUIComponent<UISprite>();
            _colorDot.spriteName = "EmptySprite";
            _colorDot.size = new Vector2(14f, 14f);
            _colorDot.relativePosition = new Vector3(8f, 12f);
            _colorDot.color = new Color32(160, 160, 160, 255);

            UpdateLabel();
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
            HarmonyPatcher.CycleOverlayAndSync();
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

    // ========================================================================
    //  PARTY EDITOR PANEL — in-game editor to add/remove parties and edit
    //  name, color, ideology, policies, and modifiers. Opened via the
    //  "Manage Parties" button on the main politics panel.
    // ========================================================================
    public class PartyEditorPanel : UIPanel
    {
        public const int MinParties = 1;
        public const int MaxParties = 6;

        private static PartyEditorPanel _instance;

        public static void Toggle()
        {
            if (_instance == null)
            {
                var view = UIView.GetAView();
                if (view == null) return;
                _instance = view.AddUIComponent(typeof(PartyEditorPanel)) as PartyEditorPanel;
            }
            if (_instance != null) _instance.isVisible = !_instance.isVisible;
        }

        // Currently selected party index in Config.Parties
        private int _selectedIdx = 0;

        // Left-side list container + per-row buttons (rebuilt whenever the
        // party list mutates).
        private UIPanel _listPanel;
        private UIButton[] _listButtons;

        // Right-side detail form — a scrollable panel so tall forms fit.
        private UIScrollablePanel _formPanel;
        private UIScrollbar _formScroll;

        // Add/Remove buttons at the bottom of the list.
        private UIButton _addBtn, _removeBtn;

        public override void Start()
        {
            base.Start();
            width = 1100;
            height = 760;
            backgroundSprite = "MenuPanel2";
            canFocus = true;
            isInteractive = true;
            relativePosition = new Vector3(120, 30);
            BuildUI();
            isVisible = false;
        }

        private void BuildUI()
        {
            var title = AddUIComponent<UILabel>();
            title.text = "Manage Parties";
            title.textScale = 1.2f;
            title.relativePosition = new Vector3(15, 10);

            UIHelpers.MakeDraggable(this);

            var close = AddUIComponent<UIButton>();
            close.text = "X";
            close.size = new Vector2(28, 24);
            close.relativePosition = new Vector3(width - 35, 8);
            close.normalBgSprite = "ButtonMenu";
            close.hoveredBgSprite = "ButtonMenuHovered";
            close.pressedBgSprite = "ButtonMenuPressed";
            close.eventClick += (c, p) => { isVisible = false; };

            // ---- Left: party list ----
            _listPanel = AddUIComponent<UIPanel>();
            _listPanel.relativePosition = new Vector3(15, 50);
            _listPanel.size = new Vector2(220, height - 110);
            _listPanel.backgroundSprite = "GenericPanel";
            _listPanel.color = new Color32(40, 40, 45, 220);            _addBtn = AddUIComponent<UIButton>();
            _addBtn.text = "+ Add party";
            _addBtn.size = new Vector2(105, 28);
            _addBtn.relativePosition = new Vector3(15, height - 50);
            _addBtn.normalBgSprite = "ButtonMenu";
            _addBtn.hoveredBgSprite = "ButtonMenuHovered";
            _addBtn.pressedBgSprite = "ButtonMenuPressed";
            _addBtn.textColor = Color.white;
            _addBtn.eventClick += (c, p) => { OnAddClicked(); };

            _removeBtn = AddUIComponent<UIButton>();
            _removeBtn.text = "– Remove";
            _removeBtn.size = new Vector2(105, 28);
            _removeBtn.relativePosition = new Vector3(130, height - 50);
            _removeBtn.normalBgSprite = "ButtonMenu";
            _removeBtn.hoveredBgSprite = "ButtonMenuHovered";
            _removeBtn.pressedBgSprite = "ButtonMenuPressed";
            _removeBtn.textColor = Color.white;
            _removeBtn.eventClick += (c, p) => { OnRemoveClicked(); };

            // ---- Right: detail form (scrollable) ----
            // Container just to hold the scrollbar next to the scrollable panel.
            var formContainer = AddUIComponent<UIPanel>();
            formContainer.relativePosition = new Vector3(250, 50);
            formContainer.size = new Vector2(width - 265, height - 65);
            formContainer.backgroundSprite = "GenericPanel";
            formContainer.color = new Color32(55, 55, 60, 220);

            _formPanel = formContainer.AddUIComponent<UIScrollablePanel>();
            _formPanel.relativePosition = new Vector3(0, 0);
            _formPanel.size = new Vector2(formContainer.width - 18, formContainer.height);
            _formPanel.autoLayout = false;
            _formPanel.clipChildren = true;
            _formPanel.scrollWheelDirection = UIOrientation.Vertical;
            _formPanel.builtinKeyNavigation = true;

            _formScroll = formContainer.AddUIComponent<UIScrollbar>();
            _formScroll.relativePosition = new Vector3(formContainer.width - 16, 2);
            _formScroll.size = new Vector2(14, formContainer.height - 4);
            _formScroll.orientation = UIOrientation.Vertical;
            _formScroll.stepSize = 24f;
            _formScroll.incrementAmount = 48f;

            var track = _formScroll.AddUIComponent<UISlicedSprite>();
            track.relativePosition = Vector3.zero;
            track.size = _formScroll.size;
            track.spriteName = "ScrollbarTrack";
            _formScroll.trackObject = track;

            var thumb = track.AddUIComponent<UISlicedSprite>();
            thumb.relativePosition = Vector3.zero;
            thumb.spriteName = "ScrollbarThumb";
            thumb.size = new Vector2(14, 40);
            _formScroll.thumbObject = thumb;

            _formPanel.verticalScrollbar = _formScroll;
            _formPanel.eventMouseWheel += (c, e) =>
            {
                _formPanel.scrollPosition = new Vector2(
                    _formPanel.scrollPosition.x,
                    Mathf.Max(0f, _formPanel.scrollPosition.y - e.wheelDelta * 48f));
            };

            RebuildList();
        }

        // ---- List management -------------------------------------------

        private void RebuildList()
        {
            if (_listPanel == null) return;
            // Remove prior buttons
            if (_listButtons != null)
            {
                foreach (var b in _listButtons)
                {
                    if (b != null) UnityEngine.Object.Destroy(b.gameObject);
                }
            }
            int n = Config.Parties.Length;
            _listButtons = new UIButton[n];
            for (int i = 0; i < n; i++)
            {
                int idx = i; // capture
                var b = _listPanel.AddUIComponent<UIButton>();
                b.size = new Vector2(_listPanel.width - 10, 34);
                b.relativePosition = new Vector3(5, 5 + i * 38);
                b.normalBgSprite  = (idx == _selectedIdx) ? "ButtonMenuFocused" : "ButtonMenu";
                b.hoveredBgSprite = "ButtonMenuHovered";
                b.pressedBgSprite = "ButtonMenuPressed";
                b.textColor = Color.white;
                b.textScale = 0.85f;
                b.textPadding = new RectOffset(28, 4, 8, 6);
                b.textHorizontalAlignment = UIHorizontalAlignment.Left;
                var p = Config.Parties[idx];
                b.text = p.ShortName + " — " + p.FullName;
                b.tooltip = p.FullName;

                // Color swatch
                var swatch = b.AddUIComponent<UISprite>();
                swatch.spriteName = "EmptySprite";
                swatch.size = new Vector2(16f, 16f);
                swatch.relativePosition = new Vector3(6f, 9f);
                swatch.color = p.Color;

                b.eventClick += (c, e) => { SelectParty(idx); };
                _listButtons[i] = b;
            }
            UpdateAddRemoveEnabled();
        }

        private void UpdateAddRemoveEnabled()
        {
            int n = Config.Parties.Length;
            if (_addBtn != null)    _addBtn.isEnabled    = n < MaxParties;
            if (_removeBtn != null) _removeBtn.isEnabled = n > MinParties;
        }

        private void SelectParty(int idx)
        {
            _selectedIdx = Mathf.Clamp(idx, 0, Config.Parties.Length - 1);
            RebuildList();
            // Form rebuild hook — implemented in the next step.
            RebuildForm();
        }

        // Form controls (rebuilt each time selection changes)
        private UITextField _shortNameField, _fullNameField;
        private UISprite _colorSwatch;
        private UISlider _econSlider, _socSlider, _govSlider;
        private UILabel  _econLbl, _socLbl, _govLbl;
        // Checkboxes for vanilla policies (built lazily from enum values)
        private UICheckBox[] _policyChecks;
        private DistrictPolicies.Policies[] _policyEnumCache;
        // Modifier sliders (tax + budgets)
        private UISlider[] _modSliders;     // index = ModifierField order
        private UILabel[]  _modLabels;

        /// <summary>Clear and rebuild the right-side form for the selected party.</summary>
        private void RebuildForm()
        {
            if (_formPanel == null) return;
            // Tear down all children
            var children = new List<GameObject>();
            foreach (Transform child in _formPanel.transform) children.Add(child.gameObject);
            foreach (var g in children) UnityEngine.Object.Destroy(g);

            if (_selectedIdx < 0 || _selectedIdx >= Config.Parties.Length) return;
            var party = Config.Parties[_selectedIdx];

            float y = 10f;
            const float labelW = 130f;
            const float fieldW = 250f;

            // --- Short name ---
            AddFormLabel("Short name", 10, y);
            _shortNameField = AddTextField(labelW, y, 100f, party.ShortName, (v) => {
                party.ShortName = (v ?? "").Trim();
                RebuildList();
            });
            y += 34f;

            // --- Full name ---
            AddFormLabel("Full name", 10, y);
            _fullNameField = AddTextField(labelW, y, fieldW, party.FullName, (v) => {
                party.FullName = (v ?? "").Trim();
                RebuildList();
            });
            y += 34f;

            // --- Color picker: a clickable swatch that opens RGB sliders ---
            AddFormLabel("Color", 10, y);
            _colorSwatch = BuildColorPickerRow(labelW, y, party);
            y += 88f;   // swatch + R/G/B slider rows

            // --- Ideology sliders ---
            var header = _formPanel.AddUIComponent<UILabel>();
            header.text = "Ideology (−1 … +1)";
            header.textScale = 0.95f;
            header.relativePosition = new Vector3(10, y);
            y += 24f;

            _econLbl = AddModSliderRow(y, "Economic (left↔right)", party.Ideology.x, -1f, +1f,
                v => { party.Ideology = new Vector3(v, party.Ideology.y, party.Ideology.z); UpdateIdeologyLabels(party); },
                out _econSlider);
            y += 44f;

            _socLbl = AddModSliderRow(y, "Social (prog↔trad)", party.Ideology.y, -1f, +1f,
                v => { party.Ideology = new Vector3(party.Ideology.x, v, party.Ideology.z); UpdateIdeologyLabels(party); },
                out _socSlider);
            y += 44f;

            _govLbl = AddModSliderRow(y, "Governance (lib↔auth)", party.Ideology.z, -1f, +1f,
                v => { party.Ideology = new Vector3(party.Ideology.x, party.Ideology.y, v); UpdateIdeologyLabels(party); },
                out _govSlider);
            y += 50f;

            UpdateIdeologyLabels(party);

            // --- Vanilla policy checkboxes ---
            var polHdr = _formPanel.AddUIComponent<UILabel>();
            polHdr.text = "Supported policies";
            polHdr.textScale = 0.95f;
            polHdr.relativePosition = new Vector3(10, y);
            y += 22f;

            _policyEnumCache = GetInterestingPolicies();
            _policyChecks = new UICheckBox[_policyEnumCache.Length]; // kept for compat (unused for tiles)
            var partySet = new HashSet<DistrictPolicies.Policies>(party.VanillaPolicies ?? new DistrictPolicies.Policies[0]);

            // Scrollable sub-panel for policy tiles so the main form stays compact.
            const float policyBoxHeight = 220f;
            var policyScroll = _formPanel.AddUIComponent<UIScrollablePanel>();
            policyScroll.relativePosition = new Vector3(10, y);
            policyScroll.size = new Vector2(_formPanel.width - 30f, policyBoxHeight);
            policyScroll.backgroundSprite = "GenericPanel";
            policyScroll.color = new Color32(30, 30, 35, 200);
            policyScroll.autoLayout = false;
            policyScroll.clipChildren = true;
            policyScroll.scrollWheelDirection = UIOrientation.Vertical;
            policyScroll.builtinKeyNavigation = true;

            var policyScrollBar = _formPanel.AddUIComponent<UIScrollbar>();
            policyScrollBar.relativePosition = new Vector3(_formPanel.width - 20f, y);
            policyScrollBar.size = new Vector2(12, policyBoxHeight);
            policyScrollBar.orientation = UIOrientation.Vertical;
            policyScrollBar.stepSize = 20f;
            policyScrollBar.incrementAmount = 40f;
            var pbTrack = policyScrollBar.AddUIComponent<UISlicedSprite>();
            pbTrack.relativePosition = Vector3.zero;
            pbTrack.size = policyScrollBar.size;
            pbTrack.spriteName = "ScrollbarTrack";
            policyScrollBar.trackObject = pbTrack;
            var pbThumb = pbTrack.AddUIComponent<UISlicedSprite>();
            pbThumb.relativePosition = Vector3.zero;
            pbThumb.spriteName = "ScrollbarThumb";
            pbThumb.size = new Vector2(12, 30);
            policyScrollBar.thumbObject = pbThumb;
            policyScroll.verticalScrollbar = policyScrollBar;
            policyScroll.eventMouseWheel += (c, e) =>
            {
                policyScroll.scrollPosition = new Vector2(
                    policyScroll.scrollPosition.x,
                    Mathf.Max(0f, policyScroll.scrollPosition.y - e.wheelDelta * 40f));
            };

            // Build tiles inside the scrollable panel (relative to it).
            const int cols = 6;
            float margin = 6f;
            float tileSize = Mathf.Min(56f, (policyScroll.width - 12f - (cols - 1) * margin) / cols);
            float colW = tileSize + margin;
            float rowH = tileSize + 18f;
            for (int i = 0; i < _policyEnumCache.Length; i++)
            {
                int col = i % cols;
                int row = i / cols;
                var policy = _policyEnumCache[i];
                BuildPolicyTile(policyScroll,
                    2 + col * colW, 2 + row * rowH,
                    tileSize, policy, partySet.Contains(policy), party);
            }
            y += policyBoxHeight + 10f;

            // --- Tax modifiers ---
            var taxHdr = _formPanel.AddUIComponent<UILabel>();
            taxHdr.text = "Tax deltas (pct points, −10 .. +10)";
            taxHdr.textScale = 0.9f;
            taxHdr.relativePosition = new Vector3(10, y);
            y += 22f;

            y = AddModifierIntRow(y, "Residential", party.Modifiers.TaxDeltaRes,
                v => party.Modifiers.TaxDeltaRes = v);
            y = AddModifierIntRow(y, "Commercial",  party.Modifiers.TaxDeltaCom,
                v => party.Modifiers.TaxDeltaCom = v);
            y = AddModifierIntRow(y, "Industrial",  party.Modifiers.TaxDeltaInd,
                v => party.Modifiers.TaxDeltaInd = v);
            y = AddModifierIntRow(y, "Office",      party.Modifiers.TaxDeltaOff,
                v => party.Modifiers.TaxDeltaOff = v);

            y += 4f;
            // --- Budget modifiers ---
            var budHdr = _formPanel.AddUIComponent<UILabel>();
            budHdr.text = "Budget deltas (pct points, −30 .. +30)";
            budHdr.textScale = 0.9f;
            budHdr.relativePosition = new Vector3(10, y);
            y += 22f;

            y = AddBudgetRow(y, "Electricity",    party.Modifiers.BudgetDeltaElectricity,
                v => party.Modifiers.BudgetDeltaElectricity = v);
            y = AddBudgetRow(y, "Water",          party.Modifiers.BudgetDeltaWater,
                v => party.Modifiers.BudgetDeltaWater = v);
            y = AddBudgetRow(y, "Garbage",        party.Modifiers.BudgetDeltaGarbage,
                v => party.Modifiers.BudgetDeltaGarbage = v);
            y = AddBudgetRow(y, "Healthcare",     party.Modifiers.BudgetDeltaHealth,
                v => party.Modifiers.BudgetDeltaHealth = v);
            y = AddBudgetRow(y, "Fire",           party.Modifiers.BudgetDeltaFire,
                v => party.Modifiers.BudgetDeltaFire = v);
            y = AddBudgetRow(y, "Police",         party.Modifiers.BudgetDeltaPolice,
                v => party.Modifiers.BudgetDeltaPolice = v);
            y = AddBudgetRow(y, "Education",      party.Modifiers.BudgetDeltaEducation,
                v => party.Modifiers.BudgetDeltaEducation = v);
            y = AddBudgetRow(y, "Transport",      party.Modifiers.BudgetDeltaTransport,
                v => party.Modifiers.BudgetDeltaTransport = v);
            y = AddBudgetRow(y, "Beautification", party.Modifiers.BudgetDeltaBeautification,
                v => party.Modifiers.BudgetDeltaBeautification = v);
            y = AddBudgetRow(y, "Roads",          party.Modifiers.BudgetDeltaRoads,
                v => party.Modifiers.BudgetDeltaRoads = v);
            y = AddBudgetRow(y, "Industry",       party.Modifiers.BudgetDeltaIndustry,
                v => party.Modifiers.BudgetDeltaIndustry = v);
        }

        /// <summary>The vanilla policies that make sense as city-wide platform items.</summary>
        /// <summary>
        /// Enumerate every DistrictPolicies.Policies enum value the game currently
        /// knows about, sorted alphabetically. Excludes "None". DLC-gated enum
        /// values are included if the enum value exists on this CS1 build.
        /// </summary>
        private static DistrictPolicies.Policies[] GetInterestingPolicies()
        {
            var values = Enum.GetValues(typeof(DistrictPolicies.Policies));
            var list = new List<DistrictPolicies.Policies>(values.Length);
            foreach (DistrictPolicies.Policies v in values)
            {
                // Skip the implicit 0/"None" entry
                if ((int)v == 0) continue;
                // Skip combined flag masks (if any — names typically include "Mask" or "All")
                var n = v.ToString();
                if (n.IndexOf("Mask", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (n.Equals("All",  StringComparison.OrdinalIgnoreCase)) continue;
                // Skip synthetic combos (those containing ", ")
                if (n.Contains(", ")) continue;
                list.Add(v);
            }
            list.Sort((a, b) => string.Compare(a.ToString(), b.ToString(),
                StringComparison.OrdinalIgnoreCase));
            return list.ToArray();
        }

        private static void TogglePolicyOnParty(PartyDef party, DistrictPolicies.Policies policy, bool on)
        {
            var set = new HashSet<DistrictPolicies.Policies>(party.VanillaPolicies ?? new DistrictPolicies.Policies[0]);
            if (on) set.Add(policy); else set.Remove(policy);
            var arr = new DistrictPolicies.Policies[set.Count];
            int i = 0;
            foreach (var p in set) arr[i++] = p;
            party.VanillaPolicies = arr;
        }

        /// <summary>
        /// Build a single clickable policy "tile" — an icon + label styled like
        /// the vanilla policy buttons, with a selected-state frame when the
        /// party supports this policy. Clicking toggles.
        /// </summary>
        private UIButton BuildPolicyTile(UIComponent parent, float x, float y, float size,
                                         DistrictPolicies.Policies policy,
                                         bool initialSelected,
                                         PartyDef party)
        {
            var btn = parent.AddUIComponent<UIButton>();
            btn.relativePosition = new Vector3(x, y);
            btn.size = new Vector2(size, size);
            string initBg = initialSelected ? "ButtonMenuFocused" : "ButtonMenu";
            btn.normalBgSprite   = initBg;
            btn.hoveredBgSprite  = "ButtonMenuHovered";
            btn.pressedBgSprite  = "ButtonMenuPressed";
            btn.focusedBgSprite  = initBg;   // tracks selection, not focus
            btn.disabledBgSprite = initBg;
            btn.text = "";
            btn.tooltip = FormatPolicyName(policy);

            // Icon — use the vanilla "IconPolicy<Name>" sprite convention.
            var icon = btn.AddUIComponent<UISprite>();
            icon.size = new Vector2(size * 0.8f, size * 0.8f);
            icon.relativePosition = new Vector3((size - icon.width) / 2f,
                                                (size - icon.height) / 2f);
            string spriteName = "IconPolicy" + policy.ToString();
            icon.atlas = parent.GetUIView().defaultAtlas;
            icon.spriteName = spriteName;

            // No label under the icon — would be too cramped at small sizes.
            // The tooltip already shows the full policy name on hover.

            var capturedPolicy = policy;
            var capturedParty = party;
            btn.eventClick += (c, p) =>
            {
                var existing = new HashSet<DistrictPolicies.Policies>(
                    capturedParty.VanillaPolicies ?? new DistrictPolicies.Policies[0]);
                bool nowSelected = !existing.Contains(capturedPolicy);
                TogglePolicyOnParty(capturedParty, capturedPolicy, nowSelected);
                string bg = nowSelected ? "ButtonMenuFocused" : "ButtonMenu";
                btn.normalBgSprite  = bg;
                btn.focusedBgSprite = bg;  // must match, otherwise the click-focused state
                                           // overrides our visual and "sticks" on selected.
                btn.disabledBgSprite = bg;
                // Drop focus so the button returns to its normal sprite immediately.
                btn.Unfocus();
            };
            return btn;
        }

        /// <summary>Split a CamelCase enum name into spaced words.</summary>
        private static string FormatPolicyName(DistrictPolicies.Policies p)
        {
            string raw = p.ToString();
            var sb = new StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && char.IsUpper(raw[i]) && !char.IsUpper(raw[i - 1])) sb.Append(' ');
                sb.Append(raw[i]);
            }
            return sb.ToString();
        }

        private UICheckBox MakeCheckbox(float x, float y, string text, bool initial)
        {
            var cb = _formPanel.AddUIComponent<UICheckBox>();
            cb.relativePosition = new Vector3(x, y);
            cb.size = new Vector2(18f, 18f);
            // Small "check" sprite hack
            var sprite = cb.AddUIComponent<UISprite>();
            sprite.spriteName = "AchievementCheckedFalse";
            sprite.size = new Vector2(16f, 16f);
            sprite.relativePosition = Vector3.zero;
            var checkedSprite = cb.AddUIComponent<UISprite>();
            checkedSprite.spriteName = "AchievementCheckedTrue";
            checkedSprite.size = new Vector2(16f, 16f);
            checkedSprite.relativePosition = Vector3.zero;
            cb.checkedBoxObject = checkedSprite;

            var lbl = cb.AddUIComponent<UILabel>();
            lbl.text = text;
            lbl.textScale = 0.75f;
            lbl.relativePosition = new Vector3(22f, 0f);
            lbl.size = new Vector2(200f, 18f);

            cb.isChecked = initial;
            return cb;
        }

        private float AddModifierIntRow(float y, string label, int value, Action<int> onChanged)
        {
            return AddIntSliderRow(y, label, value, -10, 10, onChanged);
        }

        private float AddBudgetRow(float y, string label, int value, Action<int> onChanged)
        {
            return AddIntSliderRow(y, label, value, -30, 30, onChanged);
        }

        private float AddIntSliderRow(float y, string label, int value, int min, int max, Action<int> onChanged)
        {
            var nameLbl = _formPanel.AddUIComponent<UILabel>();
            nameLbl.text = label;
            nameLbl.textScale = 0.78f;
            nameLbl.relativePosition = new Vector3(10, y);

            var valLbl = _formPanel.AddUIComponent<UILabel>();
            valLbl.textScale = 0.78f;
            valLbl.text = value.ToString();
            valLbl.relativePosition = new Vector3(_formPanel.width - 60, y);

            var slider = _formPanel.AddUIComponent<UISlider>();
            slider.relativePosition = new Vector3(130, y + 3);
            slider.size = new Vector2(_formPanel.width - 200, 12);
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = 1f;
            slider.value = Mathf.Clamp(value, min, max);

            var track = slider.AddUIComponent<UISlicedSprite>();
            track.relativePosition = new Vector3(0, 5);
            track.size = new Vector2(slider.width, 3);
            track.spriteName = "BudgetSlider";
            var thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.size = new Vector2(10, 12);
            thumb.spriteName = "SliderBudget";
            slider.thumbObject = thumb;

            slider.eventValueChanged += (c, v) => {
                int iv = Mathf.RoundToInt(v);
                valLbl.text = iv.ToString();
                onChanged(iv);
            };
            return y + 18f;
        }

        // ---- Form helpers ---------------------------------------------

        private UILabel AddFormLabel(string text, float x, float y)
        {
            var l = _formPanel.AddUIComponent<UILabel>();
            l.text = text;
            l.textScale = 0.85f;
            l.relativePosition = new Vector3(x, y + 4);
            return l;
        }

        private UITextField AddTextField(float x, float y, float w, string initial, Action<string> onChanged)
        {
            var t = _formPanel.AddUIComponent<UITextField>();
            t.size = new Vector2(w, 28f);
            t.relativePosition = new Vector3(x, y);
            t.builtinKeyNavigation = true;
            t.readOnly = false;
            t.selectOnFocus = true;
            t.canFocus = true;
            t.horizontalAlignment = UIHorizontalAlignment.Left;
            t.normalBgSprite = "TextFieldPanel";
            t.textColor = Color.white;
            t.padding = new RectOffset(6, 6, 6, 6);
            t.text = initial ?? "";
            t.eventTextSubmitted += (c, v) => { onChanged(v); };
            t.eventLostFocus     += (c, e) => { onChanged(t.text); };
            return t;
        }

        /// <summary>
        /// Build a simple RGB color picker: a colored swatch on the left plus
        /// three short R/G/B sliders stacked next to it. Updates party.Color
        /// live and refreshes the left-side list.
        /// </summary>
        private UISprite BuildColorPickerRow(float x, float y, PartyDef party)
        {
            var swatch = _formPanel.AddUIComponent<UISprite>();
            swatch.size = new Vector2(48f, 48f);
            swatch.relativePosition = new Vector3(x, y);
            swatch.spriteName = "EmptySprite";
            swatch.color = party.Color;

            float sliderX = x + 60f;
            float sliderY = y;
            float sliderW = _formPanel.width - sliderX - 20f;

            // R / G / B rows
            Action<char, int> addSlider = null;
            addSlider = (channel, initial) =>
            {
                var lbl = _formPanel.AddUIComponent<UILabel>();
                lbl.text = channel.ToString();
                lbl.textScale = 0.8f;
                lbl.relativePosition = new Vector3(sliderX, sliderY);

                var valLbl = _formPanel.AddUIComponent<UILabel>();
                valLbl.textScale = 0.75f;
                valLbl.text = initial.ToString();
                valLbl.relativePosition = new Vector3(_formPanel.width - 45, sliderY);

                var sl = _formPanel.AddUIComponent<UISlider>();
                sl.relativePosition = new Vector3(sliderX + 16f, sliderY + 2);
                sl.size = new Vector2(sliderW - 60f, 12);
                sl.minValue = 0f;
                sl.maxValue = 255f;
                sl.stepSize = 1f;
                sl.value = initial;

                var track = sl.AddUIComponent<UISlicedSprite>();
                track.relativePosition = new Vector3(0, 5);
                track.size = new Vector2(sl.width, 3);
                track.spriteName = "BudgetSlider";
                var thumb = sl.AddUIComponent<UISlicedSprite>();
                thumb.size = new Vector2(10, 12);
                thumb.spriteName = "SliderBudget";
                sl.thumbObject = thumb;

                char capC = channel;
                sl.eventValueChanged += (c, v) =>
                {
                    byte b = (byte)Mathf.Clamp((int)v, 0, 255);
                    valLbl.text = b.ToString();
                    var col = party.Color;
                    if (capC == 'R') col.r = b;
                    else if (capC == 'G') col.g = b;
                    else col.b = b;
                    party.Color = col;
                    swatch.color = col;
                    RebuildList();
                };

                sliderY += 18f;
            };
            addSlider('R', party.Color.r);
            addSlider('G', party.Color.g);
            addSlider('B', party.Color.b);
            return swatch;
        }

        private UILabel AddModSliderRow(float y, string label, float value, float min, float max,
                                        Action<float> onChanged, out UISlider slider)
        {
            var nameLbl = _formPanel.AddUIComponent<UILabel>();
            nameLbl.text = label;
            nameLbl.textScale = 0.8f;
            nameLbl.relativePosition = new Vector3(10, y);

            var valLbl = _formPanel.AddUIComponent<UILabel>();
            valLbl.textScale = 0.8f;
            valLbl.relativePosition = new Vector3(_formPanel.width - 80, y);

            slider = _formPanel.AddUIComponent<UISlider>();
            slider.relativePosition = new Vector3(10, y + 18);
            slider.size = new Vector2(_formPanel.width - 20, 16);
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = 0.05f;
            slider.value = Mathf.Clamp(value, min, max);

            var track = slider.AddUIComponent<UISlicedSprite>();
            track.relativePosition = new Vector3(0, 7);
            track.size = new Vector2(slider.width, 3);
            track.spriteName = "BudgetSlider";

            var thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.size = new Vector2(12, 16);
            thumb.spriteName = "SliderBudget";
            slider.thumbObject = thumb;

            slider.eventValueChanged += (c, v) => { onChanged(v); };
            return valLbl;
        }

        private void UpdateIdeologyLabels(PartyDef p)
        {
            if (_econLbl != null) _econLbl.text = p.Ideology.x.ToString("0.00");
            if (_socLbl  != null) _socLbl.text  = p.Ideology.y.ToString("0.00");
            if (_govLbl  != null) _govLbl.text  = p.Ideology.z.ToString("0.00");
        }

        private void OnAddClicked()
        {
            if (Config.Parties.Length >= MaxParties) return;
            // Create a new party with sensible defaults.
            var colorPool = new Color32[]
            {
                new Color32(120, 144, 156, 255), // slate
                new Color32(255, 152,   0, 255), // orange
                new Color32(0,   150, 136, 255), // teal
                new Color32(233,  30,  99, 255), // pink
                new Color32(205, 220,  57, 255), // lime
            };
            var used = new HashSet<uint>();
            foreach (var p in Config.Parties)
            {
                used.Add(((uint)p.Color.r << 16) | ((uint)p.Color.g << 8) | p.Color.b);
            }
            Color32 pick = colorPool[0];
            foreach (var c in colorPool)
            {
                uint key = ((uint)c.r << 16) | ((uint)c.g << 8) | c.b;
                if (!used.Contains(key)) { pick = c; break; }
            }
            int newIdx = Config.Parties.Length;
            string shortName = "NEW" + (newIdx + 1);
            var newParty = new PartyDef
            {
                Id = newIdx,
                ShortName = shortName,
                FullName = "New Party " + (newIdx + 1),
                Color = pick,
                Ideology = Vector3.zero,
                VanillaPolicies = new DistrictPolicies.Policies[0],
                Modifiers = new PolicyModifiers { PollutionMultiplier = 1.0f }
            };
            var newList = new PartyDef[Config.Parties.Length + 1];
            Array.Copy(Config.Parties, newList, Config.Parties.Length);
            newList[newIdx] = newParty;
            Config.Parties = newList;
            ResizePerPartyArrays();
            _selectedIdx = newIdx;
            RebuildList();
            RebuildForm();
            PoliticsUserMod.Log("Added party: " + shortName);
        }

        private void OnRemoveClicked()
        {
            if (Config.Parties.Length <= MinParties) return;
            if (_selectedIdx < 0 || _selectedIdx >= Config.Parties.Length) return;
            string removed = Config.Parties[_selectedIdx].ShortName;

            var newList = new PartyDef[Config.Parties.Length - 1];
            int j = 0;
            for (int i = 0; i < Config.Parties.Length; i++)
            {
                if (i == _selectedIdx) continue;
                newList[j++] = Config.Parties[i];
            }
            // Reassign ids so they stay 0..N-1 contiguous.
            for (int i = 0; i < newList.Length; i++) newList[i].Id = i;
            Config.Parties = newList;
            ResizePerPartyArrays();
            _selectedIdx = Mathf.Clamp(_selectedIdx, 0, Config.Parties.Length - 1);
            RebuildList();
            RebuildForm();
            PoliticsUserMod.Log("Removed party: " + removed);
        }

        /// <summary>
        /// Resize PoliticsState per-party arrays to match Config.Parties.Length.
        /// Also clear seats for removed parties so stale data doesn't show up
        /// in the hemicycle.
        /// </summary>
        private static void ResizePerPartyArrays()
        {
            var st = PoliticsState.Instance;
            if (st == null) return;
            int n = Config.Parties.Length;
            st.CurrentSeats    = ResizeIntArray(st.CurrentSeats, n);
            st.CurrentSupport  = ResizeFloatArray(st.CurrentSupport, n);
            st.ApprovalByParty = ResizeIntArray(st.ApprovalByParty, n);
            // Drop any coalition ids that no longer exist
            if (st.CoalitionPartyIds != null)
            {
                st.CoalitionPartyIds.RemoveAll(id => id < 0 || id >= n);
            }
        }

        private static int[] ResizeIntArray(int[] src, int len)
        {
            var dst = new int[len];
            if (src != null)
            {
                int copy = Math.Min(src.Length, len);
                for (int i = 0; i < copy; i++) dst[i] = src[i];
            }
            return dst;
        }

        private static float[] ResizeFloatArray(float[] src, int len)
        {
            var dst = new float[len];
            if (src != null)
            {
                int copy = Math.Min(src.Length, len);
                for (int i = 0; i < copy; i++) dst[i] = src[i];
            }
            return dst;
        }
    }

    // ========================================================================
    //  VOTER TRAITS PANEL — edit per-trait economic-axis biases with sliders.
    // ========================================================================
    public class VoterTraitsPanel : UIPanel
    {
        private static VoterTraitsPanel _instance;

        public static void Toggle()
        {
            if (_instance == null)
            {
                var view = UIView.GetAView();
                if (view == null) return;
                _instance = view.AddUIComponent(typeof(VoterTraitsPanel)) as VoterTraitsPanel;
            }
            if (_instance != null)
            {
                _instance.isVisible = !_instance.isVisible;
                if (_instance.isVisible) _instance.RefreshAll();
            }
        }

        private struct Row
        {
            public UILabel ValueLabel;
            public UISlider Slider;
            public Func<float> Get;
            public Action<float> Set;
        }
        private List<Row> _rows = new List<Row>();

        public override void Start()
        {
            base.Start();
            width = 560;
            height = 620;
            backgroundSprite = "MenuPanel2";
            canFocus = true;
            isInteractive = true;
            relativePosition = new Vector3(180, 80);
            BuildUI();
            isVisible = false;
        }

        private void BuildUI()
        {
            var title = AddUIComponent<UILabel>();
            title.text = "Voter Traits — Economic Axis Bias (−1 left … +1 right)";
            title.textScale = 1.0f;
            title.relativePosition = new Vector3(15, 10);

            UIHelpers.MakeDraggable(this);

            var close = AddUIComponent<UIButton>();
            close.text = "X";
            close.size = new Vector2(28, 24);
            close.relativePosition = new Vector3(width - 35, 8);
            close.normalBgSprite = "ButtonMenu";
            close.hoveredBgSprite = "ButtonMenuHovered";
            close.pressedBgSprite = "ButtonMenuPressed";
            close.eventClick += (c, p) => { isVisible = false; };

            var note = AddUIComponent<UILabel>();
            note.text = "Only Young, Adult, and Senior citizens vote.";
            note.textScale = 0.75f;
            note.textColor = new Color32(180, 180, 180, 255);
            note.relativePosition = new Vector3(15, 34);

            float y = 60f;
            y = AddSection("Education", y);
            y = AddRow(y, "Uneducated",        () => VoterTraits.BiasEduUneducated,     v => VoterTraits.BiasEduUneducated     = v);
            y = AddRow(y, "Educated",          () => VoterTraits.BiasEduEducated,       v => VoterTraits.BiasEduEducated       = v);
            y = AddRow(y, "Well-educated",     () => VoterTraits.BiasEduWellEducated,   v => VoterTraits.BiasEduWellEducated   = v);
            y = AddRow(y, "Highly-educated",   () => VoterTraits.BiasEduHighlyEducated, v => VoterTraits.BiasEduHighlyEducated = v);
            y += 6f;
            y = AddSection("Wealth", y);
            y = AddRow(y, "Low wealth",        () => VoterTraits.BiasWealthLow,    v => VoterTraits.BiasWealthLow    = v);
            y = AddRow(y, "Medium wealth",     () => VoterTraits.BiasWealthMedium, v => VoterTraits.BiasWealthMedium = v);
            y = AddRow(y, "High wealth",       () => VoterTraits.BiasWealthHigh,   v => VoterTraits.BiasWealthHigh   = v);
            y += 6f;
            y = AddSection("Employment", y);
            y = AddRow(y, "Employed",          () => VoterTraits.BiasEmployed,   v => VoterTraits.BiasEmployed   = v);
            y = AddRow(y, "Unemployed",        () => VoterTraits.BiasUnemployed, v => VoterTraits.BiasUnemployed = v);
            y += 6f;
            y = AddSection("Age", y);
            y = AddRow(y, "Young",             () => VoterTraits.BiasYoung,  v => VoterTraits.BiasYoung  = v);
            y = AddRow(y, "Adult",             () => VoterTraits.BiasAdult,  v => VoterTraits.BiasAdult  = v);
            y = AddRow(y, "Senior",            () => VoterTraits.BiasSenior, v => VoterTraits.BiasSenior = v);
            y += 6f;
            y = AddSection("Life conditions", y);
            y = AddRow(y, "Sick",              () => VoterTraits.BiasSick,          v => VoterTraits.BiasSick          = v);
            y = AddRow(y, "Lives in pollution",() => VoterTraits.BiasHighPollution, v => VoterTraits.BiasHighPollution = v);

            // Reset button
            var reset = AddUIComponent<UIButton>();
            reset.text = "Reset to defaults";
            reset.size = new Vector2(180, 28);
            reset.relativePosition = new Vector3(width - 200, height - 40);
            reset.normalBgSprite = "ButtonMenu";
            reset.hoveredBgSprite = "ButtonMenuHovered";
            reset.pressedBgSprite = "ButtonMenuPressed";
            reset.textColor = Color.white;
            reset.eventClick += (c, p) => { VoterTraits.ResetToDefaults(); RefreshAll(); };
        }

        private float AddSection(string text, float y)
        {
            var hdr = AddUIComponent<UILabel>();
            hdr.text = text;
            hdr.textScale = 0.9f;
            hdr.textColor = new Color32(220, 220, 230, 255);
            hdr.relativePosition = new Vector3(15, y);
            return y + 22f;
        }

        private float AddRow(float y, string label, Func<float> get, Action<float> set)
        {
            var nameLbl = AddUIComponent<UILabel>();
            nameLbl.text = label;
            nameLbl.textScale = 0.8f;
            nameLbl.relativePosition = new Vector3(25, y);

            var valLbl = AddUIComponent<UILabel>();
            valLbl.textScale = 0.8f;
            valLbl.relativePosition = new Vector3(width - 65, y);
            valLbl.text = get().ToString("+0.00;-0.00;0.00");

            var slider = AddUIComponent<UISlider>();
            slider.relativePosition = new Vector3(180, y + 4);
            slider.size = new Vector2(width - 260, 12);
            slider.minValue = VoterTraits.Min;
            slider.maxValue = VoterTraits.Max;
            slider.stepSize = 0.05f;
            slider.value = Mathf.Clamp(get(), VoterTraits.Min, VoterTraits.Max);

            var track = slider.AddUIComponent<UISlicedSprite>();
            track.relativePosition = new Vector3(0, 5);
            track.size = new Vector2(slider.width, 3);
            track.spriteName = "BudgetSlider";
            var thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.size = new Vector2(10, 12);
            thumb.spriteName = "SliderBudget";
            slider.thumbObject = thumb;

            slider.eventValueChanged += (c, v) =>
            {
                set(v);
                valLbl.text = v.ToString("+0.00;-0.00;0.00");
            };

            _rows.Add(new Row { ValueLabel = valLbl, Slider = slider, Get = get, Set = set });
            return y + 22f;
        }

        public void RefreshAll()
        {
            foreach (var r in _rows)
            {
                float v = r.Get();
                r.Slider.value = v;
                r.ValueLabel.text = v.ToString("+0.00;-0.00;0.00");
            }
        }
    }

    // ========================================================================
    //  ELECTION STATS PANEL — bar chart of "why people voted" for the most
    //  recent election, driven by ElectionResult.VotesByGrievance.
    // ========================================================================
    public class ElectionStatsPanel : UIPanel
    {
        private static ElectionStatsPanel _instance;

        public static void Toggle()
        {
            if (_instance == null)
            {
                var view = UIView.GetAView();
                if (view == null) return;
                _instance = view.AddUIComponent(typeof(ElectionStatsPanel)) as ElectionStatsPanel;
            }
            if (_instance != null)
            {
                _instance.isVisible = !_instance.isVisible;
                if (_instance.isVisible) _instance.Refresh();
            }
        }

        private UILabel _title;
        private UILabel _subtitle;
        private UIPanel _chartPanel;

        public override void Start()
        {
            base.Start();
            width = 800;
            height = 780;
            backgroundSprite = "MenuPanel2";
            canFocus = true;
            isInteractive = true;
            relativePosition = new Vector3(120, 40);
            BuildUI();
            isVisible = false;
        }

        private UIScrollablePanel _scrollBody;
        private UIScrollbar _scrollBar;

        private void BuildUI()
        {
            _title = AddUIComponent<UILabel>();
            _title.text = "Election Stats";
            _title.textScale = 1.1f;
            _title.relativePosition = new Vector3(15, 10);

            UIHelpers.MakeDraggable(this);

            var close = AddUIComponent<UIButton>();
            close.text = "X";
            close.size = new Vector2(28, 24);
            close.relativePosition = new Vector3(width - 35, 8);
            close.normalBgSprite = "ButtonMenu";
            close.hoveredBgSprite = "ButtonMenuHovered";
            close.pressedBgSprite = "ButtonMenuPressed";
            close.eventClick += (c, p) => { isVisible = false; };

            _subtitle = AddUIComponent<UILabel>();
            _subtitle.textScale = 0.85f;
            _subtitle.relativePosition = new Vector3(15, 38);
            _subtitle.textColor = new Color32(200, 200, 210, 255);

            _scrollBody = AddUIComponent<UIScrollablePanel>();
            _scrollBody.relativePosition = new Vector3(15, 65);
            _scrollBody.size = new Vector2(width - 45, height - 80);
            _scrollBody.autoLayout = false;
            _scrollBody.clipChildren = true;
            _scrollBody.scrollWheelDirection = UIOrientation.Vertical;
            _scrollBody.builtinKeyNavigation = true;

            _scrollBar = AddUIComponent<UIScrollbar>();
            _scrollBar.relativePosition = new Vector3(width - 27, 65);
            _scrollBar.size = new Vector2(12, height - 80);
            _scrollBar.orientation = UIOrientation.Vertical;
            _scrollBar.stepSize = 20f;
            _scrollBar.incrementAmount = 40f;
            var sbTrack = _scrollBar.AddUIComponent<UISlicedSprite>();
            sbTrack.relativePosition = Vector3.zero;
            sbTrack.size = _scrollBar.size;
            sbTrack.spriteName = "ScrollbarTrack";
            _scrollBar.trackObject = sbTrack;
            var sbThumb = sbTrack.AddUIComponent<UISlicedSprite>();
            sbThumb.relativePosition = Vector3.zero;
            sbThumb.spriteName = "ScrollbarThumb";
            sbThumb.size = new Vector2(12, 40);
            _scrollBar.thumbObject = sbThumb;
            _scrollBody.verticalScrollbar = _scrollBar;
            _scrollBody.eventMouseWheel += (c, e) =>
            {
                _scrollBody.scrollPosition = new Vector2(
                    _scrollBody.scrollPosition.x,
                    Mathf.Max(0f, _scrollBody.scrollPosition.y - e.wheelDelta * 40f));
            };

            // The _chartPanel alias continues to refer to the drawable area
            // so existing code that builds bars inside it still works.
            _chartPanel = _scrollBody;
        }

        public void Refresh()
        {
            if (_chartPanel == null) return;
            // Tear down all children of the scrollable body.
            var kids = new List<GameObject>();
            foreach (Transform t in _chartPanel.transform) kids.Add(t.gameObject);
            foreach (var g in kids) UnityEngine.Object.Destroy(g);

            var st = PoliticsState.Instance;
            if (st == null || st.LastResult == null)
            {
                _subtitle.text = "No election has run yet.";
                return;
            }
            var r = st.LastResult;

            int total = 0;
            int[] tally = r.VotesByGrievance ?? new int[9];
            for (int i = 0; i < tally.Length; i++) total += tally[i];
            _subtitle.text = "Election " + r.Year + "-" + r.Month +
                " — " + total + " votes sampled  •  Turnout " + (int)(r.Turnout * 100) + "%";

            float y = 0f;
            // --- Shared party legend (used by all demographic charts) ---
            y = DrawPartyLegend(y);

            // --- Grievance chart ---
            y = DrawGrievanceChart(y, r, tally, total);

            // --- Demographic stacked bars ---
            y = DrawStackedChart(y, "Vote by age group",
                r.VotesByAgeParty,
                new[] { "Young", "Adult", "Senior" });
            y = DrawStackedChart(y, "Vote by education",
                r.VotesByEduParty,
                new[] { "Uneducated", "Educated", "Well-educated", "Highly-educated" });
            y = DrawStackedChart(y, "Vote by wealth",
                r.VotesByWealthParty,
                new[] { "Low wealth", "Medium wealth", "High wealth" });
        }

        // ---- Chart helpers -------------------------------------------------

        /// <summary>One colored swatch + party name per party, across a row.</summary>
        private float DrawPartyLegend(float y)
        {
            var hdr = _chartPanel.AddUIComponent<UILabel>();
            hdr.text = "Party colors";
            hdr.textScale = 0.85f;
            hdr.relativePosition = new Vector3(0, y);
            y += 20f;

            float x = 0f;
            foreach (var p in Config.Parties)
            {
                var swatch = _chartPanel.AddUIComponent<UIPanel>();
                swatch.backgroundSprite = "GenericPanel";
                swatch.color = p.Color;
                swatch.size = new Vector2(14, 14);
                swatch.relativePosition = new Vector3(x, y + 2);

                var lbl = _chartPanel.AddUIComponent<UILabel>();
                lbl.textScale = 0.75f;
                lbl.text = p.ShortName;
                lbl.relativePosition = new Vector3(x + 18, y + 2);
                x += 18 + Mathf.Max(40f, p.ShortName.Length * 9f);
            }
            return y + 28f;
        }

        private float DrawGrievanceChart(float y, ElectionResult r, int[] tally, int total)
        {
            var hdr = _chartPanel.AddUIComponent<UILabel>();
            hdr.text = "Why people voted";
            hdr.textScale = 0.9f;
            hdr.relativePosition = new Vector3(0, y);
            y += 22f;

            string[] labels = new[]
            {
                "Pure ideology", "High taxes", "Poor health", "High crime",
                "Poor education", "Unemployment", "Pollution", "Low land value", "Noise / trash"
            };
            var colors = new Color32[]
            {
                new Color32(180, 180, 190, 255),
                new Color32(255, 152,   0, 255),
                new Color32(244,  67,  54, 255),
                new Color32(103,  58, 183, 255),
                new Color32( 63, 181, 235, 255),
                new Color32(233,  30,  99, 255),
                new Color32( 76, 175,  80, 255),
                new Color32(255, 235,  59, 255),
                new Color32(156, 204, 101, 255),
            };
            int rows = Math.Min(labels.Length, tally.Length);
            float rowH = 22f;
            float chartW = _chartPanel.width - 10f;
            for (int i = 0; i < rows; i++)
            {
                int v = tally[i];
                float frac = total > 0 ? v / (float)total : 0f;

                var rowLabel = _chartPanel.AddUIComponent<UILabel>();
                rowLabel.text = labels[i];
                rowLabel.textScale = 0.75f;
                rowLabel.relativePosition = new Vector3(0, y + 2);
                rowLabel.size = new Vector2(120, rowH);

                var bg = _chartPanel.AddUIComponent<UIPanel>();
                bg.relativePosition = new Vector3(130, y + 2);
                bg.size = new Vector2(chartW - 220, rowH - 6);
                bg.backgroundSprite = "GenericPanel";
                bg.color = new Color32(40, 40, 45, 180);

                var fg = _chartPanel.AddUIComponent<UIPanel>();
                fg.relativePosition = new Vector3(130, y + 2);
                fg.size = new Vector2(Mathf.Max(2f, (chartW - 220) * frac), rowH - 6);
                fg.backgroundSprite = "GenericPanel";
                fg.color = colors[i];

                var pctLbl = _chartPanel.AddUIComponent<UILabel>();
                pctLbl.textScale = 0.75f;
                pctLbl.text = string.Format("{0:P1}  ({1})", frac, v);
                pctLbl.relativePosition = new Vector3(chartW - 85, y + 2);
                pctLbl.size = new Vector2(85, rowH);
                y += rowH;
            }
            return y + 10f;
        }

        /// <summary>
        /// Draw one stacked bar per bucket showing the party split.
        /// </summary>
        private float DrawStackedChart(float y, string title, int[,] data, string[] bucketLabels)
        {
            var hdr = _chartPanel.AddUIComponent<UILabel>();
            hdr.text = title;
            hdr.textScale = 0.9f;
            hdr.relativePosition = new Vector3(0, y);
            y += 22f;

            if (data == null)
            {
                var msg = _chartPanel.AddUIComponent<UILabel>();
                msg.text = "(no data — older save format)";
                msg.textScale = 0.75f;
                msg.textColor = new Color32(170, 170, 170, 255);
                msg.relativePosition = new Vector3(10, y);
                return y + 22f;
            }

            int buckets = data.GetLength(0);
            int parties = data.GetLength(1);
            float chartW = _chartPanel.width - 10f;
            float rowH = 22f;
            float barStart = 130f;
            float barW     = chartW - 220f;

            for (int b = 0; b < buckets && b < bucketLabels.Length; b++)
            {
                int total = 0;
                for (int p = 0; p < parties; p++) total += data[b, p];

                var lbl = _chartPanel.AddUIComponent<UILabel>();
                lbl.text = bucketLabels[b];
                lbl.textScale = 0.75f;
                lbl.relativePosition = new Vector3(0, y + 2);
                lbl.size = new Vector2(120, rowH);

                var bg = _chartPanel.AddUIComponent<UIPanel>();
                bg.relativePosition = new Vector3(barStart, y + 2);
                bg.size = new Vector2(barW, rowH - 6);
                bg.backgroundSprite = "GenericPanel";
                bg.color = new Color32(40, 40, 45, 180);

                float xCursor = 0f;
                for (int p = 0; p < parties; p++)
                {
                    if (p >= Config.Parties.Length) break;
                    if (total <= 0) break;
                    float frac = data[b, p] / (float)total;
                    if (frac <= 0f) continue;
                    var seg = _chartPanel.AddUIComponent<UIPanel>();
                    seg.relativePosition = new Vector3(barStart + xCursor, y + 2);
                    seg.size = new Vector2(Mathf.Max(1f, barW * frac), rowH - 6);
                    seg.backgroundSprite = "GenericPanel";
                    seg.color = Config.Parties[p].Color;
                    xCursor += barW * frac;
                }

                var totalLbl = _chartPanel.AddUIComponent<UILabel>();
                totalLbl.textScale = 0.75f;
                totalLbl.text = total.ToString() + " votes";
                totalLbl.relativePosition = new Vector3(chartW - 85, y + 2);
                totalLbl.size = new Vector2(85, rowH);
                y += rowH;
            }
            return y + 10f;
        }
}