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
    //  CHIRPER MESSAGE - custom MessageBase subclass so our announcements
    //  show up in the in-game Chirper feed (blue bird). See
    //  MessageManager.QueueMessage.
    // ========================================================================
    public class PoliticsChirpMessage : MessageBase
    {
        private string _sender;
        private string _message;
        private uint   _id;

        // Parameterless constructor required for deserialization.
        public PoliticsChirpMessage() { }

        public PoliticsChirpMessage(string sender, string message, uint id)
        {
            _sender = sender;
            _message = message;
            _id = id;
        }

        public override uint GetSenderID() { return _id; }
        public override string GetSenderName() { return _sender; }
        public override string GetText() { return _message; }

        public override bool IsSimilarMessage(MessageBase other)
        {
            var m = other as PoliticsChirpMessage;
            if (m == null) return false;
            // Same sender (by id) posting the same message → duplicate.
            // Don't dedupe across different senders - two citizens can have
            // the same take, and we want each to be visible as its own chirp.
            return m._id == _id && (m._message ?? "") == (_message ?? "");
        }

        public override void Serialize(ColossalFramework.IO.DataSerializer s)
        {
            s.WriteSharedString(_sender);
            s.WriteUInt32(_id);
            s.WriteSharedString(_message);
        }

        public override void Deserialize(ColossalFramework.IO.DataSerializer s)
        {
            _sender  = s.ReadSharedString();
            _id      = s.ReadUInt32();
            _message = s.ReadSharedString();
        }

        public override void AfterDeserialize(ColossalFramework.IO.DataSerializer s) { }
    }
}
