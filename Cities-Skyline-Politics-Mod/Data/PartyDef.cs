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
    //  DATA MODELS
    // ========================================================================

    public class PartyDef
    {
        public int Id;
        public string ShortName;
        public string FullName;
        public Color32 Color;
        public Vector3 Ideology;                // -1..+1 per axis
        public DistrictPolicies.Policies[] VanillaPolicies;
        public PolicyModifiers Modifiers;
    }
}
