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

        // v6: demographic cross-tabs - votes by (bucket, party).
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
            // v5 tail (optional - old saves don't have it)
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

            // v6 tail - demographic cross-tabs
            if (s.version >= 6)
            {
                VotesByAgeParty    = ReadMatrix(s);
                VotesByEduParty    = ReadMatrix(s);
                VotesByWealthParty = ReadMatrix(s);
            }
        }

        public void AfterDeserialize(DataSerializer s) { }
    }
}
