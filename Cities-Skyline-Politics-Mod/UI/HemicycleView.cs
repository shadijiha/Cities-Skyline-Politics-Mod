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
    //  UI PANEL - bar chart, coalition, policies, term countdown.
    // ========================================================================
    // ========================================================================
    //  HEMICYCLE VIEW - semi-circular parliament seat visualization.
    //  Arranges N seats in concentric arcs and colors each by party.
    //  Coalition seats are drawn with a subtle outer glow (lighter variant).
    // ========================================================================
    public class HemicycleView : UIComponent
    {
        private int[] _seats = new int[0];
        private HashSet<int> _coalition = new HashSet<int>();
        private int _totalSeats;

        // Cached seat dots (world-space relative to this component)
        private struct SeatDot { public Vector2 Pos; public int PartyId; public float Radius; }
        private SeatDot[] _dots;

        public void SetData(int[] seatsByParty, IEnumerable<int> coalitionPartyIds, int total)
        {
            _seats = seatsByParty;
            _totalSeats = Math.Max(1, total);
            _coalition.Clear();
            if (coalitionPartyIds != null)
                foreach (var id in coalitionPartyIds) _coalition.Add(id);
            Recompute();
            Invalidate();
        }

        private static Texture2D _dotTex;
        private static void EnsureTex()
        {
            if (_dotTex != null) return;
            // Soft round dot rendered at a generous resolution so the seat
            // markers stay crisp at low seat counts, where each dot draws
            // ~40-80 px on screen. 64 px source + bilinear filter produces
            // a cleanly anti-aliased circle at any realistic draw size.
            const int sz = 64;
            _dotTex = new Texture2D(sz, sz, TextureFormat.ARGB32, false);
            _dotTex.filterMode = FilterMode.Bilinear;
            _dotTex.wrapMode   = TextureWrapMode.Clamp;
            // Anti-aliased edge band: 1 px wide in texel space gives a
            // smooth circumference that scales gracefully.
            float center  = (sz - 1) / 2f;
            float outerR  = center;               // in texels
            float edgeW   = 1.0f;                 // AA band width in texels
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float d  = Mathf.Sqrt(dx * dx + dy * dy);
                // Inside (d <= outerR - edgeW) -> fully opaque.
                // Edge band -> linear falloff to 0 at outerR.
                float a = d <= outerR - edgeW
                    ? 1f
                    : Mathf.Clamp01(1f - (d - (outerR - edgeW)) / edgeW);
                _dotTex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            _dotTex.Apply();
        }

        /// <summary>
        /// Allocate seat positions along concentric arcs spanning a half-circle.
        /// Uses a simple algorithm: pick number of rows to make seat density
        /// roughly uniform, distribute seats proportionally by arc length, then
        /// fill arcs left-to-right with parties in ideology order.
        /// </summary>
        private void Recompute()
        {
            if (_seats == null || _seats.Length == 0) { _dots = new SeatDot[0]; return; }

            // Layout params
            float padding = 10f;
            float w = width  - 2 * padding;
            float h = height - 2 * padding;
            // The hemicycle uses a half-circle in the TOP half of the component
            // (baseline along the bottom). center at bottom-middle.
            Vector2 center = new Vector2(padding + w / 2f, padding + h);
            float outerR = Mathf.Min(w / 2f, h);
            // Choose number of rows based on total seats (visually pleasant).
            int rows = _totalSeats <= 50  ? 3 :
                       _totalSeats <= 100 ? 5 :
                       _totalSeats <= 200 ? 6 :
                       _totalSeats <= 400 ? 8 : 10;
            float innerR = outerR * 0.45f;

            // Compute per-row arc radius and how many seats on each row.
            // Proportional to arc length (radius), so density is uniform.
            float[] radii = new float[rows];
            float totalArcWeight = 0f;
            for (int r = 0; r < rows; r++)
            {
                float t = rows == 1 ? 0f : r / (float)(rows - 1);
                radii[r] = Mathf.Lerp(innerR, outerR, t);
                totalArcWeight += radii[r];
            }
            int[] rowCounts = new int[rows];
            int assigned = 0;
            for (int r = 0; r < rows; r++)
            {
                rowCounts[r] = Mathf.Max(1, Mathf.RoundToInt(_totalSeats * (radii[r] / totalArcWeight)));
                assigned += rowCounts[r];
            }
            // Adjust remainder so sum equals _totalSeats
            int diff = _totalSeats - assigned;
            for (int r = rows - 1; r >= 0 && diff != 0; r--)
            {
                if (diff > 0) { rowCounts[r]++; diff--; }
                else if (rowCounts[r] > 1) { rowCounts[r]--; diff++; }
            }

            // Party ordering: left-to-right by political ideology economic axis
            // (left parties on the left of the hemicycle).
            int[] partyOrder = new int[_seats.Length];
            for (int i = 0; i < partyOrder.Length; i++) partyOrder[i] = i;
            Array.Sort(partyOrder, (a, b) =>
                Config.Parties[a].Ideology.x.CompareTo(Config.Parties[b].Ideology.x));

            // Flatten seats into an ordered sequence so we know which
            // sequential seat index belongs to which party.
            int[] seatOwner = new int[_totalSeats];
            int k = 0;
            foreach (var pid in partyOrder)
            {
                int n = (pid < _seats.Length) ? _seats[pid] : 0;
                for (int j = 0; j < n && k < seatOwner.Length; j++) seatOwner[k++] = pid;
            }
            // Unassigned (should not happen unless seat totals mismatch)
            while (k < seatOwner.Length) seatOwner[k++] = -1;

            // Compute dot radius so dots don't overlap on the tightest row.
            int widestRow = 0;
            for (int r = 0; r < rows; r++) if (rowCounts[r] > rowCounts[widestRow]) widestRow = r;
            float avgArc = Mathf.PI * radii[widestRow] / rowCounts[widestRow];
            float dotR = Mathf.Max(2f, avgArc * 0.42f);

            _dots = new SeatDot[_totalSeats];
            // Build the list of all seat positions across all rows, with their
            // (row, angle). Then sort by angle DESC (π → 0) so we place seats
            // strictly left-to-right regardless of row. This way ideology
            // ordering flows left → right across the whole hemicycle instead
            // of inner-to-outer.
            var positions = new List<KeyValuePair<float, Vector2>>(_totalSeats);
            for (int r = 0; r < rows; r++)
            {
                int n = rowCounts[r];
                for (int j = 0; j < n; j++)
                {
                    float t = n == 1 ? 0.5f : j / (float)(n - 1);
                    float angle = Mathf.PI - Mathf.PI * t;
                    float x = center.x + radii[r] * Mathf.Cos(angle);
                    float y = center.y - radii[r] * Mathf.Sin(angle);
                    positions.Add(new KeyValuePair<float, Vector2>(angle, new Vector2(x, y)));
                }
            }
            // Sort left-to-right (large angle = left, angle 0 = right)
            positions.Sort((a, b) => b.Key.CompareTo(a.Key));

            for (int i = 0; i < positions.Count && i < _totalSeats; i++)
            {
                _dots[i] = new SeatDot
                {
                    Pos = positions[i].Value,
                    PartyId = seatOwner[i],
                    Radius = dotR
                };
            }
        }

        protected override void OnSizeChanged()
        {
            base.OnSizeChanged();
            Recompute();
            Invalidate();
        }

        // Render seat dots via OnGUI. (UIComponent OnGUI is called by Unity.)
        private void OnGUI()
        {
            if (_dots == null || _dots.Length == 0) return;
            if (!isVisible) return;
            if (parent == null || !((UIComponent)parent).isVisible) return;

            EnsureTex();
            var oldColor = GUI.color;

            // Translate from component-local coords to screen coords.
            Vector3 absPos = absolutePosition;
            UIView view = GetUIView();
            float ratio = view != null ? view.PixelsToUnits() : 1f;
            // UIComponent.absolutePosition is in UI units; convert to screen pixels.
            // The GUI.DrawTexture uses screen pixels with origin top-left.
            float screenScaleX = Screen.width  / (float)view.fixedWidth;
            float screenScaleY = Screen.height / (float)view.fixedHeight;

            for (int i = 0; i < _dots.Length; i++)
            {
                var d = _dots[i];
                if (d.PartyId < 0) continue;
                var party = (d.PartyId < Config.Parties.Length) ? Config.Parties[d.PartyId] : null;
                if (party == null) continue;
                Color c = party.Color;
                if (_coalition.Contains(d.PartyId))
                {
                    // brighten slightly for coalition seats
                    c = Color.Lerp((Color)party.Color, Color.white, 0.25f);
                }

                float sx = (absPos.x + d.Pos.x) * screenScaleX;
                float sy = (absPos.y + d.Pos.y) * screenScaleY;
                float sr = d.Radius * Mathf.Min(screenScaleX, screenScaleY);

                // Outer outline for coalition seats
                if (_coalition.Contains(d.PartyId))
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.9f);
                    GUI.DrawTexture(new Rect(sx - sr - 1, sy - sr - 1, sr * 2 + 2, sr * 2 + 2), _dotTex);
                }
                GUI.color = c;
                GUI.DrawTexture(new Rect(sx - sr, sy - sr, sr * 2, sr * 2), _dotTex);
            }

            GUI.color = oldColor;
        }
    }
}
