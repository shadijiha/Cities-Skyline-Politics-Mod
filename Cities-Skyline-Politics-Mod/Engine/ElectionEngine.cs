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
    //  ELECTION ENGINE - voter simulation, coalition formation, policy apply.
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
            int days = (int)RuntimeConfig.CampaignLengthDays;
            string toastMsg = force
                ? "Snap election called! Campaign runs for " + days + " days."
                : "Campaign begins - elections in " + days + " days";
            ShowToast(toastMsg);
            // Chirper announcement from a generic "City News" account
            string newsMsg = force
                ? "Snap election called! Campaign runs for " + days + " days. #Vote"
                : "Campaign season begins! Elections in " + days + " days. #Vote";
            PostChirp("City News", newsMsg, 1u);
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

        /// <summary>
        /// Pick a slogan body for a party's chirp, bucketed by the party's
        /// ideology (economic x-axis, social y-axis) and context
        /// ("campaign" / "victory" / "defeat"). The party's ShortName is
        /// appended as a hashtag at the very end, so user-renamed parties
        /// automatically get the right tag without editing slogan strings.
        ///
        /// Parties that are near-neutral on BOTH the economic and social
        /// axes don't fit any flavor pool and just post a bare hashtag.
        /// </summary>
        private static string PickSloganForParty(int partyId, string context)
        {
            var p = Config.Parties[partyId];
            string[] pool = SelectSloganPool(p.Ideology, context);

            // No ideological fit (or empty pool) -> just the hashtag.
            if (pool == null || pool.Length == 0)
                return "#" + p.ShortName + " #Vote";

            string body = pool[UnityEngine.Random.Range(0, pool.Length)];
            return body + " #" + p.ShortName;
        }

        /// <summary>
        /// Bucket the party's ideology into one of six cells (3 economic
        /// x 2 social) and return the matching slogan pool for the context.
        /// Returns null when the party is too centrist on BOTH axes to pick
        /// a flavor pool, so the caller can fall back to a bare hashtag.
        /// </summary>
        private static string[] SelectSloganPool(Vector3 ideology, string context)
        {
            float econ = ideology.x;
            float social = ideology.y;

            // "Truly neutral" fallback: inside the dead zone on both axes we
            // have nothing interesting to say, so don't pretend to.
            if (Mathf.Abs(econ) < 0.15f && Mathf.Abs(social) < 0.15f) return null;

            // econ: 0 = left (<-0.3), 1 = center, 2 = right (>+0.3)
            int econBucket = econ < -0.3f ? 0 : (econ > +0.3f ? 2 : 1);
            // social: 0 = progressive (y<0), 1 = traditional (y>=0)
            int socialBucket = social < 0f ? 0 : 1;

            string[][][] matrix;
            if (context == "campaign")     matrix = CampaignPools;
            else if (context == "victory") matrix = VictoryPools;
            else                           matrix = DefeatPools;

            return matrix[econBucket][socialBucket];
        }

        // --------------------------------------------------------------
        // Slogan matrix: [econBucket][socialBucket][variantIndex].
        //   econBucket:   0 = left,         1 = center,       2 = right
        //   socialBucket: 0 = progressive,  1 = traditional
        //
        // Each cell has 2 variants, picked randomly. The party's ShortName
        // is appended by PickSloganForParty() so these strings never embed
        // a hashtag.
        // --------------------------------------------------------------
        private static readonly string[][][] CampaignPools = new string[][][]
        {
            // --- LEFT ------------------------------------------------------
            new string[][]
            {
                // left + progressive
                new string[]
                {
                    "A greener, fairer city for all.",
                    "Healthcare and housing are human rights.",
                },
                // left + traditional
                new string[]
                {
                    "Good jobs. Strong unions. Proud neighborhoods.",
                    "The workers built this city. Time to take it back.",
                },
            },
            // --- CENTER ----------------------------------------------------
            new string[][]
            {
                // center + progressive
                new string[]
                {
                    "Smart, pragmatic progress.",
                    "Evidence-based policy for a modern city.",
                },
                // center + traditional
                new string[]
                {
                    "Steady hands. Sound judgment.",
                    "Protect what works. Fix what doesn't.",
                },
            },
            // --- RIGHT -----------------------------------------------------
            new string[][]
            {
                // right + progressive
                new string[]
                {
                    "Lower taxes. Open markets. Open minds.",
                    "Free citizens build the best cities.",
                },
                // right + traditional
                new string[]
                {
                    "Law, order, and lower taxes.",
                    "Back to basics. Back to greatness.",
                },
            },
        };

        private static readonly string[][][] VictoryPools = new string[][][]
        {
            // --- LEFT ------------------------------------------------------
            new string[][]
            {
                // left + progressive
                new string[]
                {
                    "A mandate for justice and the planet.",
                    "Together we build the city we deserve.",
                },
                // left + traditional
                new string[]
                {
                    "A win for every family that works for a living.",
                    "Hard work and solidarity carried the day.",
                },
            },
            // --- CENTER ----------------------------------------------------
            new string[][]
            {
                // center + progressive
                new string[]
                {
                    "A mandate for competent, compassionate government.",
                    "Reason and reform - starting today.",
                },
                // center + traditional
                new string[]
                {
                    "A vote for stability and common sense.",
                    "We will govern for every citizen.",
                },
            },
            // --- RIGHT -----------------------------------------------------
            new string[][]
            {
                // right + progressive
                new string[]
                {
                    "A victory for liberty and prosperity.",
                    "The future belongs to the free.",
                },
                // right + traditional
                new string[]
                {
                    "A clear mandate to restore our values.",
                    "The silent majority has spoken.",
                },
            },
        };

        private static readonly string[][][] DefeatPools = new string[][][]
        {
            // --- LEFT ------------------------------------------------------
            new string[][]
            {
                // left + progressive
                new string[]
                {
                    "The movement doesn't stop at an election.",
                    "We keep organizing. We keep fighting.",
                },
                // left + traditional
                new string[]
                {
                    "The workers' fight never ends.",
                    "We regroup. We organize. We come back.",
                },
            },
            // --- CENTER ----------------------------------------------------
            new string[][]
            {
                // center + progressive
                new string[]
                {
                    "We accept the result. The work continues.",
                    "Good ideas don't lose. They wait.",
                },
                // center + traditional
                new string[]
                {
                    "We thank our voters and stand as loyal opposition.",
                    "Democracy spoke. We listen.",
                },
            },
            // --- RIGHT -----------------------------------------------------
            new string[][]
            {
                // right + progressive
                new string[]
                {
                    "Freedom is a long game. We're patient.",
                    "Markets correct. So will politics.",
                },
                // right + traditional
                new string[]
                {
                    "We fight on for the heart of our city.",
                    "We accept the result. The struggle for our values continues.",
                },
            },
        };

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
                PoliticsUserMod.Log("No voters sampled - aborting election.");
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
                    "" + Config.Parties[chosen[0]].ShortName + " wins. Coalition formed with " +
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
                    "Coalition talks collapsed. Snap re-election in " +
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

            // Per-building tallies (compact - residential buildings only)
            var perBuildingTally  = new int[bBuf, PartyCountRef.Value];
            var perBuildingVoters = new int[bBuf];
            var perBuildingHappy  = new int[bBuf];
            var overallTally      = new float[PartyCountRef.Value];
            int totalSampled = 0;
            // Grievance tally for the current election - indexed by (int)Grievance.
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
            // and takes <100ms - acceptable.
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
            // Approximate - BuildingAI.CalculateHomeCount would be exact but varies by type.
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
        ///   * Ideology (from VoterTraits "nudges" + party.Ideology) - the
        ///     baseline leaning.
        ///   * Grievances (real-game-state complaints) - strong pull toward
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
            // Deficit pressure - the city is losing money → voters drift right
            // (lower taxes, business-friendly). Strength 0..0.35 based on how
            // many consecutive weeks the budget has been negative.
            econ += DeficitPressure;

            econ = Mathf.Clamp(econ, -1f, 1f);

            float ageF = Mathf.Clamp01(c.m_age / 240f);
            float soc  = Mathf.Clamp(ageF * 1.2f - 0.4f - (education * 0.15f), -1f, 1f);
            float gov  = Mathf.Clamp((_rng.Next(0, 100) - 50) / 100f, -1f, 1f);

            // Small random jitter (half the old VoterNoise magnitude - rest of
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
                // [-1,1]^3 cube is sqrt(12) ≈ 3.46 - normalize by that.
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
                        // ever touched it - first-time enactment from our POV).
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
                "Parliament votes {0}-{1} to REPEAL bill C-{2}: An Act to end {3}.",
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
                // No platform supporter - use coalition center
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
            // (rare - opposition has a big majority), we override the chirp text.
            if (yes <= no)
            {
                // Force the tally to squeak through - this is a post-hoc
                // narrative fit. Keep the enacted fact, adjust numbers.
                int flip = (no - yes) + 1;
                no      = Math.Max(0, no - flip);
                yes     = yes + flip;
                passedOrFailed = "passes";
            }

            string chirp = string.Format(
                "Parliament votes {0}-{1} to {2} bill C-{3}: An Act to {4}.",
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
                "Parliament votes {0}-{1} to pass bill C-{2} (Budget & Tax): An Act to {3}.",
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
        ///
        /// Uses <c>ChirpPanel.AddMessage(IChirperMessage)</c> - the ephemeral
        /// mod-facing path - NOT <c>MessageManager.QueueMessage</c>. The
        /// distinction matters: MessageManager serializes every queued
        /// MessageBase into the vanilla save, which used to poison the save
        /// with references to PoliticsMod types. Those references then caused
        /// "Simulation error: Unknown type: PoliticsMod.PoliticsChirpMessage"
        /// when the save was later loaded without our mod installed.
        ///
        /// ChirpPanel.AddMessage with an IChirperMessage renders the chirp in
        /// the bird feed UI for the session but never writes to the save,
        /// which is exactly what we want.
        /// </summary>
        public static void PostChirp(string sender, string text, uint senderSeed = 0)
        {
            try
            {
                if (senderSeed == 0u)
                {
                    // Use a hash of the sender name so repeated chirps from
                    // the same party cluster in the chirper.
                    senderSeed = (uint)(Math.Abs(sender.GetHashCode()) | 1);
                }

                var panel = ChirpPanel.instance;
                if (panel == null) return; // menu / loading - silently skip

                panel.AddMessage(new PoliticsTransientChirp(sender, text, senderSeed));
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
                "Running a city is like running a business - cut the waste.",
                "Another deficit? I'm done funding this. " + tag,
                "Big spenders are bankrupting us. Switching my vote.",
                "If my household ran like this, we'd be homeless.",
                "Austerity now - or chaos later. " + tag,
            };

            uint cid;
            string name = PickRandomCitizenName(out cid);
            if (cid == 0u) return; // no real citizen found - skip; clicking a fake ID does nothing
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
            PoliticsPanel.ResultsPopupShownUntil = Time.realtimeSinceStartup + 15f;
            PoliticsUserMod.Log(sb.ToString());
        }
    }
}
