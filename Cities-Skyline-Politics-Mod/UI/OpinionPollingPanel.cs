using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;

namespace PoliticsMod
{
    /// <summary>
    /// Opinion polling dashboard. Renders a daily-sample scatter plot per
    /// party with a rolling-average trend line overlay. All data comes from
    /// <see cref="OpinionPolling.History"/> (in-memory only, last 30 days).
    /// </summary>
    public class OpinionPollingPanel : UIPanel
    {
        private static OpinionPollingPanel _instance;

        public static void Toggle()
        {
            var view = UIView.GetAView();
            if (view == null) return;
            bool justCreated = false;
            if (_instance == null)
            {
                _instance = view.AddUIComponent(typeof(OpinionPollingPanel)) as OpinionPollingPanel;
                justCreated = true;
            }
            if (_instance != null)
            {
                _instance.isVisible = justCreated ? true : !_instance.isVisible;
                if (_instance.isVisible) _instance.Refresh();
            }
        }

        private UILabel   _title;
        private UILabel   _subtitle;
        private UIPanel   _chart;
        private UIPanel   _legendRow;

        // Rolling-average window. 7 feels right at 30 days of data - enough
        // to smooth daily noise without lagging behind real shifts.
        private const int AverageWindow = 7;

        public override void Start()
        {
            base.Start();
            width  = 760;
            height = 540;
            backgroundSprite = "MenuPanel2";
            canFocus      = true;
            isInteractive = true;
            relativePosition = new Vector3(140, 60);

            _title = AddUIComponent<UILabel>();
            _title.text = "Opinion Polling";
            _title.textScale = 1.1f;
            _title.relativePosition = new Vector3(15, 10);

            UIHelpers.MakeDraggable(this);

            var close = AddUIComponent<UIButton>();
            close.text = "X";
            close.size = new Vector2(28, 24);
            close.relativePosition = new Vector3(width - 35, 8);
            close.normalBgSprite  = "ButtonMenu";
            close.hoveredBgSprite = "ButtonMenuHovered";
            close.pressedBgSprite = "ButtonMenuPressed";
            close.eventClick += (c, p) => { isVisible = false; };

            _subtitle = AddUIComponent<UILabel>();
            _subtitle.textScale = 0.85f;
            _subtitle.relativePosition = new Vector3(15, 38);
            _subtitle.textColor = new Color32(200, 200, 210, 255);

            _legendRow = AddUIComponent<UIPanel>();
            _legendRow.relativePosition = new Vector3(15, 62);
            _legendRow.size = new Vector2(width - 30, 24);
            _legendRow.autoLayout = false;

            _chart = AddUIComponent<UIPanel>();
            _chart.relativePosition = new Vector3(15, 94);
            _chart.size = new Vector2(width - 30, height - 110);
            _chart.backgroundSprite = "GenericPanel";
            _chart.color = new Color32(30, 30, 35, 230);

            Refresh();
        }

        public override void Update()
        {
            base.Update();
            if (!isVisible) return;
            // Rebuild once per real second so new poll samples appear without
            // the user having to reopen the panel.
            _repaintTimer += Time.unscaledDeltaTime;
            if (_repaintTimer >= 1.0f)
            {
                _repaintTimer = 0f;
                Refresh();
            }
        }
        private float _repaintTimer;

        public void Refresh()
        {
            if (_chart == null || _legendRow == null) return;

            // Tear down old chart children
            var killChart = new List<GameObject>();
            foreach (Transform t in _chart.transform) killChart.Add(t.gameObject);
            foreach (var g in killChart) UnityEngine.Object.Destroy(g);
            var killLegend = new List<GameObject>();
            foreach (Transform t in _legendRow.transform) killLegend.Add(t.gameObject);
            foreach (var g in killLegend) UnityEngine.Object.Destroy(g);

            BuildLegend();

            var history = OpinionPolling.History;
            if (history.Count == 0)
            {
                _subtitle.text = "No polling data yet - samples are collected each in-game day.";
                var msg = _chart.AddUIComponent<UILabel>();
                msg.text = "Waiting for the first daily poll...";
                msg.textScale = 0.9f;
                msg.relativePosition = new Vector3(20, 20);
                msg.textColor = new Color32(220, 220, 225, 255);
                return;
            }

            int dayNewest = history[history.Count - 1].DayIndex;
            int dayOldest = history[0].DayIndex;
            int span = Math.Max(1, dayNewest - dayOldest);
            _subtitle.text = string.Format(
                "Daily opinion poll - sample size {0} - showing last {1} day(s)",
                OpinionPolling.SampleSize, history.Count);

            DrawChart(history, dayOldest, dayNewest, span);
        }

        private void BuildLegend()
        {
            float x = 0f;
            int n = Config.Parties.Length;
            for (int i = 0; i < n; i++)
            {
                var p = Config.Parties[i];

                var swatch = _legendRow.AddUIComponent<UIPanel>();
                swatch.backgroundSprite = "GenericPanel";
                swatch.color = p.Color;
                swatch.size = new Vector2(14, 14);
                swatch.relativePosition = new Vector3(x, 4);

                var lbl = _legendRow.AddUIComponent<UILabel>();
                lbl.textScale = 0.78f;
                lbl.text = p.ShortName + " - " + p.FullName;
                lbl.relativePosition = new Vector3(x + 18, 3);
                lbl.size = new Vector2(200f, 18f);

                x += 18f + Mathf.Max(80f, (p.ShortName.Length + p.FullName.Length) * 5.2f);
            }
        }

        private void DrawChart(List<OpinionPolling.PollSample> history,
                               int dayOldest, int dayNewest, int span)
        {
            // Plot area inside _chart, with room for axis labels.
            const float leftPad   = 38f;
            const float rightPad  = 10f;
            const float topPad    = 10f;
            const float bottomPad = 22f;

            float plotW = _chart.width  - leftPad - rightPad;
            float plotH = _chart.height - topPad  - bottomPad;
            if (plotW <= 10f || plotH <= 10f) return;

            // Horizontal gridlines at 0/25/50/75/100 %.
            int[] gridPct = new int[] { 0, 25, 50, 75, 100 };
            foreach (var g in gridPct)
            {
                float y = topPad + plotH * (1f - g / 100f);
                var line = _chart.AddUIComponent<UIPanel>();
                line.relativePosition = new Vector3(leftPad, y);
                line.size = new Vector2(plotW, 1f);
                line.backgroundSprite = "GenericPanel";
                line.color = new Color32(70, 70, 80, 200);

                var lbl = _chart.AddUIComponent<UILabel>();
                lbl.textScale = 0.7f;
                lbl.text = g + "%";
                lbl.relativePosition = new Vector3(4f, y - 7f);
                lbl.textColor = new Color32(170, 170, 180, 255);
            }

            // X-axis tick labels (oldest / newest day).
            var xOldLbl = _chart.AddUIComponent<UILabel>();
            xOldLbl.textScale = 0.7f;
            xOldLbl.text = "-" + (dayNewest - dayOldest) + "d";
            xOldLbl.relativePosition = new Vector3(leftPad, topPad + plotH + 4);
            xOldLbl.textColor = new Color32(170, 170, 180, 255);

            var xNewLbl = _chart.AddUIComponent<UILabel>();
            xNewLbl.textScale = 0.7f;
            xNewLbl.text = "today";
            xNewLbl.relativePosition = new Vector3(leftPad + plotW - 30, topPad + plotH + 4);
            xNewLbl.textColor = new Color32(170, 170, 180, 255);

            int n = Config.Parties.Length;
            if (n <= 0) return;

            // --- Rolling-average trend lines (drawn under dots) ---
            for (int p = 0; p < n; p++)
            {
                var line = ComputeRollingAverage(history, p, AverageWindow);
                DrawLine(history, line, p, dayOldest, span,
                         leftPad, topPad, plotW, plotH);
            }

            // --- Daily dots ---
            for (int s = 0; s < history.Count; s++)
            {
                var sample = history[s];
                int sn = sample.ShareByParty != null ? sample.ShareByParty.Length : 0;
                int lim = Math.Min(n, sn);
                float t  = (sample.DayIndex - dayOldest) / (float)span;
                float x  = leftPad + plotW * t;
                for (int p = 0; p < lim; p++)
                {
                    float share = sample.ShareByParty[p];
                    float y = topPad + plotH * (1f - Mathf.Clamp01(share));
                    var dot = _chart.AddUIComponent<UIPanel>();
                    dot.relativePosition = new Vector3(x - 2f, y - 2f);
                    dot.size = new Vector2(4f, 4f);
                    dot.backgroundSprite = "GenericPanel";
                    dot.color = Config.Parties[p].Color;
                }
            }
        }

        /// <summary>
        /// Compute a centered rolling average of <paramref name="partyIdx"/>'s
        /// vote share across the history. Uses a symmetric window of
        /// <paramref name="window"/> samples; clamps at the ends so the line
        /// still has a value for every recorded day.
        /// </summary>
        private static float[] ComputeRollingAverage(List<OpinionPolling.PollSample> history,
                                                     int partyIdx, int window)
        {
            int count = history.Count;
            var smoothed = new float[count];
            int half = Math.Max(1, window / 2);
            for (int i = 0; i < count; i++)
            {
                int lo = Math.Max(0, i - half);
                int hi = Math.Min(count - 1, i + half);
                float sum = 0f;
                int c = 0;
                for (int k = lo; k <= hi; k++)
                {
                    var arr = history[k].ShareByParty;
                    if (arr != null && partyIdx < arr.Length)
                    {
                        sum += arr[partyIdx];
                        c++;
                    }
                }
                smoothed[i] = c > 0 ? sum / c : 0f;
            }
            return smoothed;
        }

        /// <summary>
        /// Draw a broken line by stacking thin rectangles between consecutive
        /// points. No line primitive in Colossal UI, so this is the honest
        /// cheapest way; over 30 days it stays cheap.
        /// </summary>
        private void DrawLine(List<OpinionPolling.PollSample> history, float[] values,
                              int partyIdx, int dayOldest, int span,
                              float leftPad, float topPad, float plotW, float plotH)
        {
            if (history.Count < 2) return;
            Color32 baseCol = Config.Parties[partyIdx].Color;
            // Slightly transparent so dots read on top.
            Color32 lineCol = new Color32(baseCol.r, baseCol.g, baseCol.b, 140);

            float prevX = 0, prevY = 0;
            bool havePrev = false;
            for (int i = 0; i < history.Count; i++)
            {
                float t = (history[i].DayIndex - dayOldest) / (float)span;
                float x = leftPad + plotW * t;
                float y = topPad + plotH * (1f - Mathf.Clamp01(values[i]));
                if (havePrev)
                {
                    DrawSegment(prevX, prevY, x, y, lineCol);
                }
                prevX = x; prevY = y; havePrev = true;
            }
        }

        /// <summary>
        /// Draw a straight line segment from (x1,y1) to (x2,y2) as a rotated
        /// thin sprite. We skip rotation and approximate by drawing a series
        /// of overlapping 2x2 dots - good enough for a 30-day trend line and
        /// avoids needing a rotation transform on UIPanel.
        /// </summary>
        private void DrawSegment(float x1, float y1, float x2, float y2, Color32 col)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 0.5f) return;
            int steps = Mathf.CeilToInt(len);
            for (int i = 0; i <= steps; i++)
            {
                float f = i / (float)steps;
                float x = x1 + dx * f;
                float y = y1 + dy * f;
                var seg = _chart.AddUIComponent<UIPanel>();
                seg.relativePosition = new Vector3(x - 1f, y - 1f);
                seg.size = new Vector2(2f, 2f);
                seg.backgroundSprite = "GenericPanel";
                seg.color = col;
            }
        }
    }
}
