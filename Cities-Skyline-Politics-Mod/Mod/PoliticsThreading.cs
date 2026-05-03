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
