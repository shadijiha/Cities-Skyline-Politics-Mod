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


    // ========================================================================
    //  TRANSIENT CHIRP - ephemeral chirper entry.
    //
    //  Unlike PoliticsChirpMessage above (which is a MessageBase subclass and
    //  therefore gets serialized into the vanilla save by MessageManager),
    //  this class implements only ICities.IChirperMessage and is meant to be
    //  handed to ChirpPanel.AddMessage. ChirpPanel renders it in the bird
    //  feed UI for the session, but it is NEVER persisted into the save.
    //
    //  This is how we avoid poisoning the user's savegame with references
    //  to PoliticsMod types - if no MessageBase subclass of ours is ever
    //  queued, no serialized chirp can fail to resolve after uninstall.
    // ========================================================================
    public class PoliticsTransientChirp : ICities.IChirperMessage
    {
        public string senderName { get; private set; }
        public string text       { get; private set; }
        public uint   senderID   { get; private set; }

        public PoliticsTransientChirp(string sender, string message, uint id)
        {
            senderName = sender ?? "";
            text       = message ?? "";
            senderID   = id;
        }
    }
}
