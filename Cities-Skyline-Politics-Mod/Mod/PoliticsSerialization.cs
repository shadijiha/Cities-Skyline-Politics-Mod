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
    //  SERIALIZABLE DATA EXTENSION - persists political state in savegames.
    // ========================================================================
    public class PoliticsSerialization : SerializableDataExtensionBase
    {
        public const string DataId = "PoliticsMod.v1";
        public const uint   DataVersion = 6;

        public override void OnLoadData()
        {
            base.OnLoadData();
            if (PoliticsState.Instance == null) PoliticsState.Instance = new PoliticsState();

            byte[] bytes = serializableDataManager.LoadData(DataId);
            if (bytes == null || bytes.Length == 0)
            {
                PoliticsUserMod.Log("No saved politics data; starting fresh.");
                return;
            }
            try
            {
                using (var stream = new MemoryStream(bytes))
                {
                    var s = DataSerializer.Deserialize<PoliticsSaveBlob>(stream, DataSerializer.Mode.Memory);
                    s.Apply(PoliticsState.Instance);
                    PoliticsUserMod.Log("Loaded politics state (" + bytes.Length + " bytes) | " +
                        "Day=" + (int)PoliticsState.Instance.DaysSinceLastElection +
                        "/" + (int)RuntimeConfig.TermLengthDays +
                        ", Phase=" + PoliticsState.Instance.Phase +
                        ", Elections=" + PoliticsState.Instance.History.Count);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(Config.LogPrefix + "Failed to load save data: " + e);
            }
        }

        public override void OnSaveData()
        {
            base.OnSaveData();
            if (PoliticsState.Instance == null) return;

            try
            {
                var blob = PoliticsSaveBlob.From(PoliticsState.Instance);
                byte[] bytes;
                using (var stream = new MemoryStream())
                {
                    DataSerializer.Serialize(stream, DataSerializer.Mode.Memory, DataVersion, blob);
                    bytes = stream.ToArray();
                }
                serializableDataManager.SaveData(DataId, bytes);
                PoliticsUserMod.Log("Saved politics state (" + bytes.Length + " bytes) | " +
                    "Day=" + (int)PoliticsState.Instance.DaysSinceLastElection +
                    "/" + (int)RuntimeConfig.TermLengthDays +
                    ", Phase=" + PoliticsState.Instance.Phase +
                    ", Elections=" + PoliticsState.Instance.History.Count);
            }
            catch (Exception e)
            {
                Debug.LogError(Config.LogPrefix + "Failed to save data: " + e);
            }
        }
    }
}
