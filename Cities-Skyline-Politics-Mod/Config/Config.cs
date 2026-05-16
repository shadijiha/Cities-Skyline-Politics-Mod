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
    //  CONFIG - edit these to tune the mod without touching logic.
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

        /// <summary>Half-plus-one - minimum seats to form a ruling coalition.</summary>
        public static int MajorityThreshold
        {
            get { return (ParliamentSeats / 2) + 1; }
        }

        public const int MinPopulationForElections  = 0;    // 0 = no gate; parliament still floors at MinParliamentSeats
        // Default values for the runtime-editable fields below. The *actual*
        // values used by the simulation live in RuntimeConfig and are editable
        // via the in-game panel and persisted in the savegame.
        public const float DefaultTermLengthDays         = 365f; // in-game days between elections (1 year)
        public const float DefaultCampaignLengthDays     = 7f;   // campaign duration before voting day
        public const float DefaultReElectionCooldownDays = 14f;  // pause after failed coalition
        public const int   MaxCoalitionPartners          = 4;    // coalition cannot exceed this many parties

        // -- Voter model ----------------------------------------------------
        public const float VoterNoise               = 0.25f; // 0..1, randomness in voter decisions
        public const float TurnoutBase              = 0.55f; // base turnout if happy
        public const float TurnoutHappinessBoost    = 0.35f; // + up to this much from happiness
        public const int   VoterSampleSize          = 5000;  // citizens sampled per election (perf)

        // -- UI --------------------------------------------------------------
        public const KeyCode DefaultTogglePanelKey = KeyCode.P;  // default: Ctrl+P opens panel
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
        /// <summary>
        /// Returns a fresh copy of the built-in default party list.
        /// Used both to initialise <see cref="Parties"/> on startup and to
        /// restore it when the player clicks "Reset all parties".
        /// </summary>
        public static PartyDef[] DefaultParties()
        {
            return new PartyDef[]
            {
            new PartyDef
            {
                Id = 0, ShortName = "GRN", FullName = "Green Progressive",
                Color = new Color32( 76, 175,  80, 255),
                Ideology = new Vector3(-0.5f, -0.8f, -0.1f),
                VanillaPolicies = new[]
                {
                    DistrictPolicies.Policies.Recycling,
                    DistrictPolicies.Policies.FreeTransport,
                    DistrictPolicies.Policies.EncourageBiking
                },
                OpposedPolicies = new[]
                {
                    DistrictPolicies.Policies.BigBusiness,
                    DistrictPolicies.Policies.HighriseBan
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
                OpposedPolicies = new[]
                {
                    DistrictPolicies.Policies.BigBusiness
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
                    DistrictPolicies.Policies.SmallBusiness
                },
                OpposedPolicies = new[]
                {
                    DistrictPolicies.Policies.HeavyTrafficBan,
                    DistrictPolicies.Policies.HighriseBan
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
                    DistrictPolicies.Policies.BigBusiness,
                },
                OpposedPolicies = new[]
                {
                    DistrictPolicies.Policies.FreeTransport,
                    DistrictPolicies.Policies.EducationBoost
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
                    DistrictPolicies.Policies.SmallBusiness,
                },
                OpposedPolicies = new[]
                {
                    DistrictPolicies.Policies.BigBusiness,
                    DistrictPolicies.Policies.HighTechHousing
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
                OpposedPolicies = new[]
                {
                    DistrictPolicies.Policies.BigBusiness
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
        }

        public static PartyDef[] Parties = DefaultParties();

        public static int PartyCount()
        {
            return Parties.Length;
        }
    }
}
