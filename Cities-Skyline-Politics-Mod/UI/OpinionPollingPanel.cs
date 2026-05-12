using System;
using System.Collections.Generic;
using ColossalFramework.UI;
using PoliticsMod.Localization;
using UnityEngine;

namespace PoliticsMod
{
    /// <summary>
    /// Opinion polling dashboard. Renders a daily-sample scatter plot per
    /// party with a rolling-average trend line overlay. All data comes from
    /// <see cref="OpinionPolling.History"/> (in-memory only, last 30 days).
    ///
    /// The heavy chart contents (gridlines, dots, trend lines) are drawn via
    /// <see cref="OnGUI"/> using one 1x1 white texture. That avoids the
    /// UIComponent churn (3000+ dots/segments per rebuild) that previously
    /// froze the game when the panel was open.
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
                if (_instance.isVisible) _instance.RefreshLegendAndSubtitle();
            }
        }

        private UILabel _title;
        private UILabel _subtitle;
        private UIPanel _chart;        // backdrop; chart contents live in OnGUI
        private UIPanel _legendRow;

        // Rolling-average window. 7 feels right at 30 days of data - enough
        // to smooth daily noise without lagging behind real shifts.
        private const int AverageWindow = 7;

        // Tracks the party count we last built the legend for, so we can
        // rebuild if the user adds/removes a party while the panel is open.
        private int _legendPartyCount = -1;
        private int _legendHistoryLen = -1;

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
            _title.text = L10n.T(L10nKeys.Polling_Title);
            _title.textScale = 1.1f;
            _title.relativePosition = new Vector3(15, 10);

            UIHelpers.MakeDraggable(this);

            var close = AddUIComponent<UIButton>();
            close.text = L10n.T(L10nKeys.Common_CloseX);
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

            RefreshLegendAndSubtitle();
        }

        public override void Update()
        {
            base.Update();
            if (!isVisible) return;
            // Cheap check: if the party count or recorded-day count changed,
            // rebuild the legend + subtitle. The chart itself lives in OnGUI
            // so it always reflects the latest data without any rebuild.
            if (Config.Parties.Length != _legendPartyCount ||
                OpinionPolling.History.Count != _legendHistoryLen)
            {
                RefreshLegendAndSubtitle();
            }
        }

        /// <summary>
        /// Rebuild the legend row and update the subtitle. Cheap - N party
        /// entries only. The heavy chart rendering happens in OnGUI with
        /// zero allocation.
        /// </summary>
        public void RefreshLegendAndSubtitle()
        {
            if (_legendRow == null || _subtitle == null) return;

            var killLegend = new List<GameObject>();
            foreach (Transform t in _legendRow.transform) killLegend.Add(t.gameObject);
            foreach (var g in killLegend) UnityEngine.Object.Destroy(g);

            BuildLegend();

            var history = OpinionPolling.History;
            if (history.Count == 0)
            {
                _subtitle.text = L10n.T(L10nKeys.Polling_NoHistory);
            }
            else
            {
                _subtitle.text = L10n.T(L10nKeys.Polling_Subtitle,
                    OpinionPolling.SampleSize, history.Count);
            }
            _legendPartyCount = Config.Parties.Length;
            _legendHistoryLen = history.Count;
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
                lbl.text = p.ShortName;
                lbl.tooltip = p.FullName;
                lbl.relativePosition = new Vector3(x + 18, 3);
                lbl.size = new Vector2(60f, 18f);

                x += 18f + Mathf.Max(40f, p.ShortName.Length * 8f);
            }
        }

        // ---- Chart rendering (OnGUI) ---------------------------------------

        private static Texture2D _pxTex;
        private static void EnsurePxTex()
        {
            if (_pxTex != null) return;
            _pxTex = new Texture2D(1, 1);
            _pxTex.SetPixel(0, 0, Color.white);
            _pxTex.Apply();
        }

        private void OnGUI()
        {
            if (!isVisible) return;
            if (_chart == null) return;
            var history = OpinionPolling.History;
            if (history.Count == 0) return;
            int n = Config.Parties.Length;
            if (n <= 0) return;

            EnsurePxTex();

            // Convert component-local coords to screen pixels, the same way
            // HemicycleView does. _chart.absolutePosition gives us the
            // top-left of the chart area in UI units.
            var view = GetUIView();
            if (view == null) return;
            float sx = Screen.width  / (float)view.fixedWidth;
            float sy = Screen.height / (float)view.fixedHeight;

            Vector3 absChart = _chart.absolutePosition;
            float chartX = absChart.x * sx;
            float chartY = absChart.y * sy;
            float chartW = _chart.width  * sx;
            float chartH = _chart.height * sy;

            // Plot area with padding for axis labels.
            float leftPad   = 38f * sx;
            float rightPad  = 10f * sx;
            float topPad    = 10f * sy;
            float bottomPad = 22f * sy;

            float plotX = chartX + leftPad;
            float plotY = chartY + topPad;
            float plotW = chartW - leftPad - rightPad;
            float plotH = chartH - topPad  - bottomPad;
            if (plotW <= 10f || plotH <= 10f) return;

            Color oldCol = GUI.color;

            // --- Horizontal gridlines at 0/25/50/75/100% + y-axis labels ---
            int[] gridPct = new int[] { 0, 25, 50, 75, 100 };
            var gridStyle = new GUIStyle();
            gridStyle.normal.textColor = new Color32(170, 170, 180, 255);
            gridStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(10f * Mathf.Min(sx, sy)));
            foreach (var g in gridPct)
            {
                float y = plotY + plotH * (1f - g / 100f);
                GUI.color = new Color32(70, 70, 80, 200);
                GUI.DrawTexture(new Rect(plotX, y, plotW, 1f), _pxTex);
                GUI.color = Color.white;
                GUI.Label(new Rect(chartX + 4f * sx, y - 7f * sy, 30f * sx, 14f * sy),
                          g + "%", gridStyle);
            }

            // --- X-axis tick labels ---
            int dayNewest = history[history.Count - 1].DayIndex;
            int dayOldest = history[0].DayIndex;
            int span = Math.Max(1, dayNewest - dayOldest);
            GUI.Label(new Rect(plotX, plotY + plotH + 4f * sy, 40f * sx, 14f * sy),
                      L10n.T(L10nKeys.Polling_Axis_DaysAgo, dayNewest - dayOldest), gridStyle);
            GUI.Label(new Rect(plotX + plotW - 30f * sx, plotY + plotH + 4f * sy,
                               40f * sx, 14f * sy),
                      L10n.T(L10nKeys.Polling_Axis_Today), gridStyle);

            // --- Rolling-average trend lines (drawn under dots) ---
            for (int p = 0; p < n; p++)
            {
                var avg = ComputeRollingAverage(history, p, AverageWindow);
                DrawTrendLine(history, avg, p, dayOldest, span,
                              plotX, plotY, plotW, plotH, sx, sy);
            }

            // --- Daily dots ---
            float dotHalf = Mathf.Max(1f, 2f * Mathf.Min(sx, sy));
            for (int s = 0; s < history.Count; s++)
            {
                var sample = history[s];
                var arr = sample.ShareByParty;
                if (arr == null) continue;
                int lim = Math.Min(n, arr.Length);
                float t = (sample.DayIndex - dayOldest) / (float)span;
                float x = plotX + plotW * t;
                for (int p = 0; p < lim; p++)
                {
                    float y = plotY + plotH * (1f - Mathf.Clamp01(arr[p]));
                    GUI.color = Config.Parties[p].Color;
                    GUI.DrawTexture(
                        new Rect(x - dotHalf, y - dotHalf, dotHalf * 2f, dotHalf * 2f),
                        _pxTex);
                }
            }

            GUI.color = oldCol;
        }

        /// <summary>
        /// Draw a trend line for one party as a series of rotated thin
        /// rectangles, one per (history[i], history[i+1]) segment. 29
        /// segments per party * 6 parties = ~174 DrawTexture calls per
        /// frame, cheap and allocation-free.
        /// </summary>
        private void DrawTrendLine(List<OpinionPolling.PollSample> history, float[] values,
                                   int partyIdx, int dayOldest, int span,
                                   float plotX, float plotY, float plotW, float plotH,
                                   float sx, float sy)
        {
            if (history.Count < 2) return;
            Color32 baseCol = Config.Parties[partyIdx].Color;
            Color lineCol = new Color(baseCol.r / 255f, baseCol.g / 255f,
                                      baseCol.b / 255f, 140f / 255f);
            float thickness = Mathf.Max(1f, 2f * Mathf.Min(sx, sy));

            float prevX = 0f, prevY = 0f;
            bool havePrev = false;
            for (int i = 0; i < history.Count; i++)
            {
                float t = (history[i].DayIndex - dayOldest) / (float)span;
                float x = plotX + plotW * t;
                float y = plotY + plotH * (1f - Mathf.Clamp01(values[i]));
                if (havePrev) DrawLineSegment(prevX, prevY, x, y, thickness, lineCol);
                prevX = x; prevY = y; havePrev = true;
            }
        }

        /// <summary>
        /// One rotated thin rectangle per segment. Uses GUIUtility rotation
        /// so we don't have to manually approximate the line with dots.
        /// </summary>
        private static void DrawLineSegment(float x1, float y1, float x2, float y2,
                                            float thickness, Color col)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len < 0.5f) return;

            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            Matrix4x4 prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, new Vector2(x1, y1));
            GUI.color = col;
            GUI.DrawTexture(new Rect(x1, y1 - thickness / 2f, len, thickness), _pxTex);
            GUI.matrix = prev;
        }

        /// <summary>
        /// Symmetric rolling average across a history window. Clamps at the
        /// ends so we have a value for every sample.
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
    }
}
