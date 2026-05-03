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
            // currently disabled - my previous density-based proxies were too
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
}
