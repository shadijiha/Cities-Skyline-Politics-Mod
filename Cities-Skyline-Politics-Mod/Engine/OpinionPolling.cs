using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace PoliticsMod
{
    /// <summary>
    /// Daily opinion poll. A rolling in-memory history (not persisted) of
    /// per-day per-party vote shares, sampled from a random cross-section
    /// of the population. Drives the OpinionPollingPanel graph.
    /// </summary>
    public static class OpinionPolling
    {
        /// <summary>One poll result: the day it was taken and the per-party
        /// share (values 0..1 summing to ~1).</summary>
        public struct PollSample
        {
            public int    DayIndex;         // monotonically increasing in-game day counter
            public float[] ShareByParty;    // length = Config.Parties.Length at sample time
        }

        public const int MaxHistoryDays = 30;
        public const int SampleSize     = 1000;

        // Rolling history: newest at the end. Capped at MaxHistoryDays.
        public static readonly List<PollSample> History = new List<PollSample>();

        // Day counter we pin polls against. Bumps when a full in-game day has
        // elapsed in PoliticsThreading.
        public static int LastPolledDay = -1;

        /// <summary>
        /// Take a single opinion poll sample and append it to History,
        /// trimming old entries past MaxHistoryDays. Returns the sample
        /// taken, or null if no eligible citizens were found.
        /// </summary>
        public static PollSample? RunDailyPoll(int dayIndex)
        {
            int n = PartyCountRef.Value;
            if (n <= 0) return null;

            var tally = new float[n];
            int sampled = SampleRandomCitizens(SampleSize, tally);
            if (sampled == 0) return null;

            float total = 0f;
            for (int i = 0; i < n; i++) total += tally[i];
            if (total <= 0f) return null;

            var share = new float[n];
            for (int i = 0; i < n; i++) share[i] = tally[i] / total;

            var sample = new PollSample { DayIndex = dayIndex, ShareByParty = share };
            History.Add(sample);
            LastPolledDay = dayIndex;
            // Trim: drop any entry older than MaxHistoryDays relative to now.
            int cutoff = dayIndex - MaxHistoryDays + 1;
            while (History.Count > 0 && History[0].DayIndex < cutoff) History.RemoveAt(0);

            return sample;
        }

        /// <summary>
        /// Sample up to <paramref name="maxSamples"/> random citizens from the
        /// CitizenManager buffer, running each through ElectionEngine's vote
        /// decision. Bumps <paramref name="tally"/>[partyId] for every valid
        /// vote. Returns how many citizens actually voted (≤ maxSamples).
        /// Mirrors ElectionEngine.SampleCitizenPreferences but we keep a
        /// separate copy here so polling is independent of the campaign path.
        /// </summary>
        private static int SampleRandomCitizens(int maxSamples, float[] tally)
        {
            var cm = Singleton<CitizenManager>.instance;
            var bm = Singleton<BuildingManager>.instance;
            if (cm == null || bm == null) return 0;
            uint bufSize = cm.m_citizens.m_size;
            if (bufSize <= 1) return 0;

            int sampled = 0;
            int tries   = 0;
            int limit   = maxSamples * 8; // more generous than the campaign
                                          // sampler since we don't run this
                                          // every frame.
            var rng = ElectionEngine.Rng;
            while (sampled < maxSamples && tries < limit)
            {
                tries++;
                uint idx = (uint)rng.Next(1, (int)bufSize);
                var c = cm.m_citizens.m_buffer[idx];
                if ((c.m_flags & Citizen.Flags.Created) == 0) continue;
                if ((c.m_flags & Citizen.Flags.DummyTraffic) != 0) continue;
                Grievance _unused;
                int party = ElectionEngine.DecideVote(ref c, bm, out _unused);
                if (party < 0) continue;
                if (party >= tally.Length) continue; // safety if parties changed mid-poll
                tally[party] += 1f;
                sampled++;
            }
            return sampled;
        }

        /// <summary>Call on new-game load or when party count changes to
        /// avoid showing stale graphs.</summary>
        public static void Reset()
        {
            History.Clear();
            LastPolledDay = -1;
        }
    }
}
