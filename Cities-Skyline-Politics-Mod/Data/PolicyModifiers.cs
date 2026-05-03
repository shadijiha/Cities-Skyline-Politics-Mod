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
}
