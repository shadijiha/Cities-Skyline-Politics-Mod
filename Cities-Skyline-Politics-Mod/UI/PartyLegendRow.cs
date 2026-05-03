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
    //  PARTY LEGEND ROW - small compact horizontal stack of party swatches
    //  with seat counts, drawn underneath the hemicycle.
    // ========================================================================
    public class PartyLegendRow : UIPanel
    {
        private UILabel[] _items;

        public void Build(int itemsCount)
        {
            _items = new UILabel[itemsCount];
            float x = 0f;
            float cellW = (width - 4f) / itemsCount;
            for (int i = 0; i < itemsCount; i++)
            {
                var lbl = AddUIComponent<UILabel>();
                lbl.textScale = 0.72f;
                lbl.processMarkup = true;
                lbl.relativePosition = new Vector3(x, 2);
                lbl.size = new Vector2(cellW, 22);
                _items[i] = lbl;
                x += cellW;
            }
        }

        public void Refresh(int[] seats, HashSet<int> coalition, int total)
        {
            if (_items == null) return;
            for (int i = 0; i < _items.Length; i++)
            {
                var p = Config.Parties[i];
                int s = i < seats.Length ? seats[i] : 0;
                bool inCoal = coalition != null && coalition.Contains(i);
                // Coalition parties get a *; non-coalition get a small -.
                string marker = inCoal ? "*" : "-";
                string text = string.Format(
                    "<color #{0:X2}{1:X2}{2:X2}>{3}</color> {4} {5}",
                    p.Color.r, p.Color.g, p.Color.b, marker, p.ShortName, s);
                _items[i].text = text;
            }
        }
    }
}
