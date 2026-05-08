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


    /// <summary>Serializable shape for a PartyDef - persisted in savegame (v3+).</summary>
    public class PartyBlob : IDataContainer
    {
        public int Id;
        public string ShortName;
        public string FullName;
        public byte R, G, B;
        public float IdX, IdY, IdZ;
        public int[] VanillaPolicies;
        // v7+: policies this party actively opposes (repealed on election).
        public int[] OpposedPolicies;
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
            var opp = p.OpposedPolicies ?? new DistrictPolicies.Policies[0];
            b.OpposedPolicies = new int[opp.Length];
            for (int i = 0; i < opp.Length; i++) b.OpposedPolicies[i] = (int)opp[i];
            return b;
        }

        public PartyDef ToParty()
        {
            var vpol = new DistrictPolicies.Policies[VanillaPolicies != null ? VanillaPolicies.Length : 0];
            for (int i = 0; i < vpol.Length; i++) vpol[i] = (DistrictPolicies.Policies)VanillaPolicies[i];
            var opol = new DistrictPolicies.Policies[OpposedPolicies != null ? OpposedPolicies.Length : 0];
            for (int i = 0; i < opol.Length; i++) opol[i] = (DistrictPolicies.Policies)OpposedPolicies[i];
            return new PartyDef
            {
                Id = Id,
                ShortName = ShortName ?? ("P" + Id),
                FullName  = FullName  ?? ShortName,
                Color = new Color32(R, G, B, 255),
                Ideology = new Vector3(IdX, IdY, IdZ),
                VanillaPolicies = vpol,
                OpposedPolicies = opol,
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

            // v7: opposed policies (per-party). Older saves just read back as empty.
            if (s.version >= 7)
            {
                int on = OpposedPolicies != null ? OpposedPolicies.Length : 0;
                s.WriteInt32(on);
                if (OpposedPolicies != null) foreach (var v in OpposedPolicies) s.WriteInt32(v);
            }
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

            if (s.version >= 7)
            {
                int on = s.ReadInt32();
                OpposedPolicies = new int[on];
                for (int i = 0; i < on; i++) OpposedPolicies[i] = s.ReadInt32();
            }
            else
            {
                OpposedPolicies = new int[0];
            }
        }

        public void AfterDeserialize(DataSerializer s) { }
    }
}
