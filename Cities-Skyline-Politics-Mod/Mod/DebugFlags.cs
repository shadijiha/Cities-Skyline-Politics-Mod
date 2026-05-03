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


    public static class DebugFlags
    {
        public static bool Verbose = true;
        // When true, only "essential" chirps go out (campaign start, election
        // winner, bill passage). Party slogans and crisis quips are suppressed.
        public static bool MinimalChirps = false;
    }
}
