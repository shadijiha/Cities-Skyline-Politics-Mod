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


    // Utility: population via CitizenManager
    public static class CitizenManagerUtil
    {
        public static int GetPopulation()
        {
            try
            {
                var dm = Singleton<DistrictManager>.instance;
                return (int)dm.m_districts.m_buffer[0].m_populationData.m_finalCount;
            }
            catch { return 0; }
        }
    }
}
