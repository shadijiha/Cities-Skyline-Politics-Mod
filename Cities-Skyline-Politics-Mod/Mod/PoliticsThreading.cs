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
    //  THREADING EXTENSION - drives the simulation (timer + campaign drift).
    // ========================================================================
    public class PoliticsThreading : ThreadingExtensionBase
    {
        // We derive "days elapsed" from SimulationManager.m_currentGameTime
        // (a DateTime that matches exactly what the in-game day-night HUD
        // displays). This keeps our term counter in sync with the visible
        // day bar regardless of CS1's internal frames-per-day constant.
        //
        // When paused, m_currentGameTime stops advancing, so we re-baseline
        // on resume to avoid a huge jump.
        private double _lastGameDays = -1.0;
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

            // Current in-game time expressed as a fractional day count.
            double nowDays;
            try
            {
                nowDays = simMgr.m_currentGameTime.Ticks / (double)TimeSpan.TicksPerDay;
            }
            catch
            {
                return;
            }

            if (!_haveBaseline)
            {
                _lastGameDays = nowDays;
                _haveBaseline = true;
                return;
            }

            double delta = nowDays - _lastGameDays;
            _lastGameDays = nowDays;

            // Guard against negatives (e.g., scenario reset) and huge spikes
            // (e.g., save loading mid-frame).
            if (delta <= 0 || delta > 30) return;

            float dayDelta = (float)delta;

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
                catch { /* ignore - early frame before managers are ready */ }
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
}
