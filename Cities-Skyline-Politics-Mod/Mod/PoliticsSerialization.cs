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
        public const uint   DataVersion = 9;

        public override void OnLoadData()
        {
            base.OnLoadData();
            if (PoliticsState.Instance == null) PoliticsState.Instance = new PoliticsState();

            // Always reset to defaults first so that stale static state from a
            // previously loaded save never bleeds into this one. The saved party
            // list (v3+) will overwrite this below if present.
            Config.Parties = Config.DefaultParties();

            byte[] bytes = null;
            try
            {
                bytes = serializableDataManager.LoadData(DataId);
            }
            catch (Exception e)
            {
                Debug.LogError(Config.LogPrefix + "LoadData(" + DataId + ") threw: " + e);
            }
            if (bytes == null || bytes.Length == 0)
            {
                PoliticsUserMod.Log("No saved politics data; starting fresh.");
                return;
            }

            // Deserialize into a temporary state first so that if anything
            // fails we don't leave PoliticsState.Instance half-mutated.
            var tempState = new PoliticsState();
            try
            {
                using (var stream = new MemoryStream(bytes))
                {
                    var s = DataSerializer.Deserialize<PoliticsSaveBlob>(stream, DataSerializer.Mode.Memory);
                    if (s == null) throw new Exception("Deserialize returned null blob");
                    s.Apply(tempState);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(Config.LogPrefix +
                    "Failed to load save data (" + bytes.Length + " bytes). " +
                    "Keeping fresh state; user can resume without politics history. Error: " + e);
                // Keep PoliticsState.Instance as a fresh instance; don't touch it.
                return;
            }

            // Promote the fully-deserialized state.
            PoliticsState.Instance = tempState;
            try
            {
                PoliticsUserMod.Log("Loaded politics state (" + bytes.Length + " bytes) | " +
                    "Day=" + (int)PoliticsState.Instance.DaysSinceLastElection +
                    "/" + (int)RuntimeConfig.TermLengthDays +
                    ", Phase=" + PoliticsState.Instance.Phase +
                    ", Elections=" + PoliticsState.Instance.History.Count);
            }
            catch { /* logging is best-effort only */ }
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
