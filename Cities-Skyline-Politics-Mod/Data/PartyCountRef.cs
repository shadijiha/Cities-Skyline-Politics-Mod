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


    public static class ConfigEx
    {
        public static int PartyCount() { return Config.Parties.Length; }
    }



    // Hack: C# 3.5 doesn't allow calling Config.Parties.Length in a const context
    // for ElectionResult's array init, so we route through this helper.
    public static class PartyCountRef
    {
        public static int Value { get { return Config.Parties.Length; } }
    }
}
