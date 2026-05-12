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
using PoliticsMod.Localization;
using UnityEngine;

namespace PoliticsMod
{


    // ========================================================================
    //  ELECTION STATS PANEL - bar chart of "why people voted" for the most
    //  recent election, driven by ElectionResult.VotesByGrievance.
    // ========================================================================
    public class ElectionStatsPanel : UIPanel
    {
        private static ElectionStatsPanel _instance;

        public static void Toggle()
        {
            var view = UIView.GetAView();
            if (view == null) return;
            bool justCreated = false;
            if (_instance == null)
            {
                _instance = view.AddUIComponent(typeof(ElectionStatsPanel)) as ElectionStatsPanel;
                justCreated = true;
            }

            if (_instance != null)
            {
                _instance.isVisible = justCreated ? true : !_instance.isVisible;
                if (_instance.isVisible) _instance.Refresh();
            }
        }

        private UILabel _title;
        private UILabel _subtitle;
        private UIScrollablePanel _chartPanel;
        private UIButton _closeBtn;

        public override void Start()
        {
            base.Start();
            width = 800;
            height = 780;
            backgroundSprite = "MenuPanel2";
            canFocus = true;
            isInteractive = true;
            relativePosition = new Vector3(120, 40);
            BuildUI();
            // Visibility controlled by Toggle().
            // Populate the chart immediately so the first-open flow works
            // even if Toggle's Refresh() call ran before Start built _chartPanel.
            Refresh();
            L10n.LanguageChanged += OnLanguageChanged;
        }

        public override void OnDestroy()
        {
            L10n.LanguageChanged -= OnLanguageChanged;
            base.OnDestroy();
        }

        private void OnLanguageChanged()
        {
            if (_title    != null) _title.text    = L10n.T(L10nKeys.Stats_Title);
            if (_closeBtn != null) _closeBtn.text = L10n.T(L10nKeys.Common_CloseX);
            // Refresh rebuilds the subtitle and every chart label from the
            // current catalog, so one call covers the rest of the panel.
            Refresh();
        }

        private UIScrollablePanel _scrollBody;
        private UIScrollbar _scrollBar;

        private void BuildUI()
        {
            _title = AddUIComponent<UILabel>();
            _title.text = L10n.T(L10nKeys.Stats_Title);
            _title.textScale = 1.1f;
            _title.relativePosition = new Vector3(15, 10);

            UIHelpers.MakeDraggable(this);

            var close = AddUIComponent<UIButton>();
            close.text = L10n.T(L10nKeys.Common_CloseX);
            close.size = new Vector2(28, 24);
            close.relativePosition = new Vector3(width - 35, 8);
            close.normalBgSprite = "ButtonMenu";
            close.hoveredBgSprite = "ButtonMenuHovered";
            close.pressedBgSprite = "ButtonMenuPressed";
            close.eventClick += (c, p) => { isVisible = false; };
            _closeBtn = close;

            _subtitle = AddUIComponent<UILabel>();
            _subtitle.textScale = 0.85f;
            _subtitle.relativePosition = new Vector3(15, 38);
            _subtitle.textColor = new Color32(200, 200, 210, 255);

            _scrollBody = AddUIComponent<UIScrollablePanel>();
            _scrollBody.relativePosition = new Vector3(15, 65);
            _scrollBody.size = new Vector2(width - 45, height - 80);
            _scrollBody.autoLayout = false;
            _scrollBody.clipChildren = true;
            _scrollBody.scrollWheelDirection = UIOrientation.Vertical;
            _scrollBody.builtinKeyNavigation = true;

            _scrollBar = AddUIComponent<UIScrollbar>();
            _scrollBar.relativePosition = new Vector3(width - 27, 65);
            _scrollBar.size = new Vector2(12, height - 80);
            _scrollBar.orientation = UIOrientation.Vertical;
            _scrollBar.stepSize = 20f;
            _scrollBar.incrementAmount = 40f;
            var sbTrack = _scrollBar.AddUIComponent<UISlicedSprite>();
            sbTrack.relativePosition = Vector3.zero;
            sbTrack.size = _scrollBar.size;
            sbTrack.spriteName = "ScrollbarTrack";
            _scrollBar.trackObject = sbTrack;
            var sbThumb = sbTrack.AddUIComponent<UISlicedSprite>();
            sbThumb.relativePosition = Vector3.zero;
            sbThumb.spriteName = "ScrollbarThumb";
            sbThumb.size = new Vector2(12, 40);
            _scrollBar.thumbObject = sbThumb;
            _scrollBody.verticalScrollbar = _scrollBar;
            _scrollBody.eventMouseWheel += (c, e) =>
            {
                _scrollBody.scrollPosition = new Vector2(
                    _scrollBody.scrollPosition.x,
                    Mathf.Max(0f, _scrollBody.scrollPosition.y - e.wheelDelta * 40f));
            };

            // The _chartPanel alias continues to refer to the drawable area
            // so existing code that builds bars inside it still works.
            _chartPanel = _scrollBody;
        }

        public void Refresh()
        {
            if (_chartPanel == null) return;
            // Tear down all children of the scrollable body.
            var kids = new List<GameObject>();
            foreach (Transform t in _chartPanel.transform) kids.Add(t.gameObject);
            foreach (var g in kids) UnityEngine.Object.Destroy(g);

            var st = PoliticsState.Instance;
            PoliticsUserMod.Log("Stats.Refresh: st=" + (st == null ? "null" : "ok") +
                                " History=" + (st == null ? -1 : st.History.Count) +
                                " LastResult=" + (st == null || st.LastResult == null ? "null" : "set"));
            // If we have history but LastResult wasn't set (e.g. loaded from a
            // save and the restore code path missed it), recover by promoting
            // the tail of History.
            if (st != null && st.LastResult == null && st.History != null && st.History.Count > 0)
            {
                st.LastResult = st.History[st.History.Count - 1];
            }
            if (st == null || st.LastResult == null)
            {
                _subtitle.text = L10n.T(L10nKeys.Stats_NoData_Subtitle);
                var msg = _chartPanel.AddUIComponent<UILabel>();
                msg.text = L10n.T(L10nKeys.Stats_NoData_Body);
                msg.textScale = 0.9f;
                msg.autoSize = false;
                msg.size = new Vector2(_chartPanel.width - 20f, 180f);
                msg.relativePosition = new Vector3(10f, 40f);
                msg.wordWrap = true;
                msg.textColor = new Color32(220, 220, 225, 255);
                return;
            }

            var r = st.LastResult;

            int total = 0;
            int[] tally = r.VotesByGrievance ?? new int[9];
            for (int i = 0; i < tally.Length; i++) total += tally[i];
            _subtitle.text = L10n.T(L10nKeys.Stats_Subtitle,
                r.Year, r.Month, total, (int)(r.Turnout * 100));

            float y = 0f;
            // --- Shared party legend (used by all demographic charts) ---
            y = DrawPartyLegend(y);

            // --- Grievance chart ---
            y = DrawGrievanceChart(y, r, tally, total);

            // --- Demographic stacked bars ---
            y = DrawStackedChart(y, L10n.T(L10nKeys.Stats_Chart_ByAge),
                r.VotesByAgeParty,
                new[] {
                    L10n.T(L10nKeys.Bucket_Age_Young),
                    L10n.T(L10nKeys.Bucket_Age_Adult),
                    L10n.T(L10nKeys.Bucket_Age_Senior)
                });
            y = DrawStackedChart(y, L10n.T(L10nKeys.Stats_Chart_ByEducation),
                r.VotesByEduParty,
                new[] {
                    L10n.T(L10nKeys.Bucket_Edu_Uneducated),
                    L10n.T(L10nKeys.Bucket_Edu_Educated),
                    L10n.T(L10nKeys.Bucket_Edu_WellEducated),
                    L10n.T(L10nKeys.Bucket_Edu_HighlyEducated)
                });
            y = DrawStackedChart(y, L10n.T(L10nKeys.Stats_Chart_ByWealth),
                r.VotesByWealthParty,
                new[] {
                    L10n.T(L10nKeys.Bucket_Wealth_Low),
                    L10n.T(L10nKeys.Bucket_Wealth_Medium),
                    L10n.T(L10nKeys.Bucket_Wealth_High)
                });
        }

        // ---- Chart helpers -------------------------------------------------

        /// <summary>One colored swatch + party name per party, across a row.</summary>
        private float DrawPartyLegend(float y)
        {
            var hdr = _chartPanel.AddUIComponent<UILabel>();
            hdr.text = L10n.T(L10nKeys.Stats_PartyColors);
            hdr.textScale = 0.85f;
            hdr.relativePosition = new Vector3(0, y);
            y += 20f;

            float x = 0f;
            foreach (var p in Config.Parties)
            {
                var swatch = _chartPanel.AddUIComponent<UIPanel>();
                swatch.backgroundSprite = "GenericPanel";
                swatch.color = p.Color;
                swatch.size = new Vector2(14, 14);
                swatch.relativePosition = new Vector3(x, y + 2);

                var lbl = _chartPanel.AddUIComponent<UILabel>();
                lbl.textScale = 0.75f;
                lbl.text = p.ShortName;
                lbl.relativePosition = new Vector3(x + 18, y + 2);
                x += 18 + Mathf.Max(40f, p.ShortName.Length * 9f);
            }

            return y + 28f;
        }

        private float DrawGrievanceChart(float y, ElectionResult r, int[] tally, int total)
        {
            var hdr = _chartPanel.AddUIComponent<UILabel>();
            hdr.text = L10n.T(L10nKeys.Stats_WhyPeopleVoted);
            hdr.textScale = 0.9f;
            hdr.relativePosition = new Vector3(0, y);
            y += 22f;

            string[] labels = new[]
            {
                L10n.T(L10nKeys.Stats_Grievance_Ideology),
                L10n.T(L10nKeys.Stats_Grievance_HighTaxes),
                L10n.T(L10nKeys.Stats_Grievance_PoorHealth),
                L10n.T(L10nKeys.Stats_Grievance_HighCrime),
                L10n.T(L10nKeys.Stats_Grievance_PoorEducation),
                L10n.T(L10nKeys.Stats_Grievance_Unemployment),
                L10n.T(L10nKeys.Stats_Grievance_Pollution),
                L10n.T(L10nKeys.Stats_Grievance_LowLandValue),
                L10n.T(L10nKeys.Stats_Grievance_NoiseTrash)
            };
            var colors = new Color32[]
            {
                new Color32(180, 180, 190, 255),
                new Color32(255, 152, 0, 255),
                new Color32(244, 67, 54, 255),
                new Color32(103, 58, 183, 255),
                new Color32(63, 181, 235, 255),
                new Color32(233, 30, 99, 255),
                new Color32(76, 175, 80, 255),
                new Color32(255, 235, 59, 255),
                new Color32(156, 204, 101, 255),
            };
            int rows = Math.Min(labels.Length, tally.Length);
            float rowH = 22f;
            float chartW = _chartPanel.width - 10f;
            for (int i = 0; i < rows; i++)
            {
                int v = tally[i];
                float frac = total > 0 ? v / (float)total : 0f;

                var rowLabel = _chartPanel.AddUIComponent<UILabel>();
                rowLabel.text = labels[i];
                rowLabel.textScale = 0.75f;
                rowLabel.relativePosition = new Vector3(0, y + 2);
                rowLabel.size = new Vector2(120, rowH);

                var bg = _chartPanel.AddUIComponent<UIPanel>();
                bg.relativePosition = new Vector3(130, y + 2);
                bg.size = new Vector2(chartW - 220, rowH - 6);
                bg.backgroundSprite = "GenericPanel";
                bg.color = new Color32(40, 40, 45, 180);

                var fg = _chartPanel.AddUIComponent<UIPanel>();
                fg.relativePosition = new Vector3(130, y + 2);
                fg.size = new Vector2(Mathf.Max(2f, (chartW - 220) * frac), rowH - 6);
                fg.backgroundSprite = "GenericPanel";
                fg.color = colors[i];

                var pctLbl = _chartPanel.AddUIComponent<UILabel>();
                pctLbl.textScale = 0.75f;
                pctLbl.text = L10n.T(L10nKeys.Stats_PctCountFormat, frac, v);
                pctLbl.relativePosition = new Vector3(chartW - 85, y + 2);
                pctLbl.size = new Vector2(85, rowH);
                y += rowH;
            }

            return y + 10f;
        }

        /// <summary>
        /// Draw one stacked bar per bucket showing the party split.
        /// </summary>
        private float DrawStackedChart(float y, string title, int[,] data, string[] bucketLabels)
        {
            var hdr = _chartPanel.AddUIComponent<UILabel>();
            hdr.text = title;
            hdr.textScale = 0.9f;
            hdr.relativePosition = new Vector3(0, y);
            y += 22f;

            if (data == null)
            {
                var msg = _chartPanel.AddUIComponent<UILabel>();
                msg.text = L10n.T(L10nKeys.Stats_Chart_NoData);
                msg.textScale = 0.75f;
                msg.textColor = new Color32(170, 170, 170, 255);
                msg.relativePosition = new Vector3(10, y);
                return y + 22f;
            }

            int buckets = data.GetLength(0);
            int parties = data.GetLength(1);
            float chartW = _chartPanel.width - 10f;
            float rowH = 22f;
            float barStart = 130f;
            float barW = chartW - 220f;

            for (int b = 0; b < buckets && b < bucketLabels.Length; b++)
            {
                int total = 0;
                for (int p = 0; p < parties; p++) total += data[b, p];

                var lbl = _chartPanel.AddUIComponent<UILabel>();
                lbl.text = bucketLabels[b];
                lbl.textScale = 0.75f;
                lbl.relativePosition = new Vector3(0, y + 2);
                lbl.size = new Vector2(120, rowH);

                var bg = _chartPanel.AddUIComponent<UIPanel>();
                bg.relativePosition = new Vector3(barStart, y + 2);
                bg.size = new Vector2(barW, rowH - 6);
                bg.backgroundSprite = "GenericPanel";
                bg.color = new Color32(40, 40, 45, 180);

                float xCursor = 0f;
                for (int p = 0; p < parties; p++)
                {
                    if (p >= Config.Parties.Length) break;
                    if (total <= 0) break;
                    float frac = data[b, p] / (float)total;
                    if (frac <= 0f) continue;
                    var seg = _chartPanel.AddUIComponent<UIPanel>();
                    seg.relativePosition = new Vector3(barStart + xCursor, y + 2);
                    seg.size = new Vector2(Mathf.Max(1f, barW * frac), rowH - 6);
                    seg.backgroundSprite = "GenericPanel";
                    seg.color = Config.Parties[p].Color;
                    xCursor += barW * frac;
                }

                var totalLbl = _chartPanel.AddUIComponent<UILabel>();
                totalLbl.textScale = 0.75f;
                totalLbl.text = L10n.T(L10nKeys.Stats_Votes_Suffix, total);
                totalLbl.relativePosition = new Vector3(chartW - 85, y + 2);
                totalLbl.size = new Vector2(85, rowH);
                y += rowH;
            }

            return y + 10f;
        }
    }
}
