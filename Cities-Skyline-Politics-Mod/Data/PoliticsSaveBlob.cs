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
        public float[] VoterBiases; // 14 values in fixed order - see ApplyToTraits / CaptureTraits
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
            // actually loaded values, i.e. v2+ saves - otherwise leave defaults).
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
}
