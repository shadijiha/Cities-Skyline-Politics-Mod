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

        // Layout tuning. Past MaxPerRow parties we wrap to a second row,
        // doubling the panel's height. Anything up to this count still
        // renders as a single row so the compact look stays for small
        // party counts.
        private const int   MaxPerRow = 6;
        private const float RowHeight = 22f;

        /// <summary>How many rows Build() will use for the given item count.</summary>
        public static int RowsFor(int itemsCount)
        {
            return Mathf.Max(1, Mathf.CeilToInt(itemsCount / (float)MaxPerRow));
        }

        /// <summary>Total pixel height the legend will occupy for itemsCount.</summary>
        public static float HeightFor(int itemsCount)
        {
            return RowsFor(itemsCount) * RowHeight;
        }

        public void Build(int itemsCount)
        {
            _items = new UILabel[itemsCount];
            int rows   = RowsFor(itemsCount);
            // Distribute items as evenly as possible across rows.
            int perRow = Mathf.Max(1, Mathf.CeilToInt(itemsCount / (float)rows));
            float cellW = (width - 4f) / perRow;

            for (int i = 0; i < itemsCount; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                var lbl = AddUIComponent<UILabel>();
                lbl.textScale = 0.72f;
                lbl.processMarkup = true;
                lbl.relativePosition = new Vector3(col * cellW, 2 + row * RowHeight);
                lbl.size = new Vector2(cellW, RowHeight);
                _items[i] = lbl;
            }

            // Grow the panel to fit all the rows; the parent panel queries
            // HeightFor(...) before placement so there's room below.
            height = rows * RowHeight;
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
