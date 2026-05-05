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


    public class PoliticsPanel : UIPanel
    {
        public static string LatestToast;
        public static float  LatestToastTime;
        public static string ResultsPopupText;
        public static float  ResultsPopupShownUntil;

        // Singleton pointer set when the loading extension creates the panel.
        // Used by the mod settings panel to open the main window.
        public static PoliticsPanel Instance;

        public static void Show()
        {
            if (Instance != null) Instance.isVisible = true;
        }

        public static void Toggle()
        {
            if (Instance != null) Instance.isVisible = !Instance.isVisible;
        }

        private UILabel _title;
        private UILabel _phaseLabel;
        private UILabel _coalitionLabel;
        private UILabel _policiesLabel;
        private HemicycleView _hemi;
        private PartyLegendRow _legend;
        private UIButton _overlayBtn;
        private UIButton _forceBtn;

        // Runtime config sliders
        private UISlider _termSlider, _campSlider, _coolSlider;
        private UILabel  _termLbl,    _campLbl,    _coolLbl;

        public override void Start()
        {
            base.Start();
            width = 520;
            height = 640;
            backgroundSprite = "MenuPanel2";
            canFocus = true;
            isInteractive = true;
            relativePosition = new Vector3(120, 80);
            Instance = this;
            BuildUI();
        }

        public override void OnDestroy()
        {
            if (Instance == this) Instance = null;
            base.OnDestroy();
        }

        private void BuildUI()
        {
            _title = AddUIComponent<UILabel>();
            _title.text = "Politics & Elections";
            _title.textScale = 1.2f;
            _title.relativePosition = new Vector3(15, 10);

            UIHelpers.MakeDraggable(this);

            var close = AddUIComponent<UIButton>();
            close.text = "X";
            close.size = new Vector2(28, 24);
            close.relativePosition = new Vector3(width - 35, 8);
            close.normalBgSprite = "ButtonMenu";
            close.hoveredBgSprite = "ButtonMenuHovered";
            close.pressedBgSprite = "ButtonMenuPressed";
            close.eventClick += (c, p) => { isVisible = false; };

            _phaseLabel = AddUIComponent<UILabel>();
            _phaseLabel.textScale = 0.9f;
            _phaseLabel.relativePosition = new Vector3(15, 40);

            // -------- Parliament hemicycle + legend --------
            _hemi = AddUIComponent<HemicycleView>();
            _hemi.relativePosition = new Vector3(15, 70);
            _hemi.size = new Vector2(width - 30, 170);

            _legend = AddUIComponent<PartyLegendRow>();
            _legend.relativePosition = new Vector3(15, 70 + 170 + 2);
            _legend.size = new Vector2(width - 30, 22);
            _legend.Build(PartyCountRef.Value);

            _coalitionLabel = AddUIComponent<UILabel>();
            _coalitionLabel.textScale = 0.85f;
            _coalitionLabel.relativePosition = new Vector3(15, 270);

            _policiesLabel = AddUIComponent<UILabel>();
            _policiesLabel.textScale = 0.8f;
            _policiesLabel.relativePosition = new Vector3(15, 293);
            // Single-line label - the list is truncated to avoid overflow.
            _policiesLabel.autoSize = false;
            _policiesLabel.size = new Vector2(width - 30, 18);
            _policiesLabel.clipChildren = true;

            _overlayBtn = AddUIComponent<UIButton>();
            _overlayBtn.text = "Overlay: Off";
            _overlayBtn.size = new Vector2(200, 32);
            _overlayBtn.relativePosition = new Vector3(15, height - 90);
            _overlayBtn.normalBgSprite = "ButtonMenu";
            _overlayBtn.hoveredBgSprite = "ButtonMenuHovered";
            _overlayBtn.pressedBgSprite = "ButtonMenuPressed";
            _overlayBtn.textColor = Color.white;
            _overlayBtn.eventClick += (c, p) =>
            {
                HarmonyPatcher.CycleOverlayAndSync();
            };

            _forceBtn = AddUIComponent<UIButton>();
            _forceBtn.text = "Call snap election";
            _forceBtn.size = new Vector2(200, 32);
            _forceBtn.relativePosition = new Vector3(230, height - 90);
            _forceBtn.normalBgSprite = "ButtonMenu";
            _forceBtn.hoveredBgSprite = "ButtonMenuHovered";
            _forceBtn.pressedBgSprite = "ButtonMenuPressed";
            _forceBtn.textColor = Color.white;
            _forceBtn.eventClick += (c, p) =>
            {
                if (PoliticsState.Instance != null)
                    ElectionEngine.TriggerCampaign(force: true);
            };

            // -------- Manage Parties button --------
            var manageBtn = AddUIComponent<UIButton>();
            manageBtn.text = "Manage Parties";
            manageBtn.size = new Vector2(200, 32);
            manageBtn.relativePosition = new Vector3(15, height - 50);
            manageBtn.normalBgSprite = "ButtonMenu";
            manageBtn.hoveredBgSprite = "ButtonMenuHovered";
            manageBtn.pressedBgSprite = "ButtonMenuPressed";
            manageBtn.textColor = Color.white;
            manageBtn.eventClick += (c, p) =>
            {
                PartyEditorPanel.Toggle();
            };

            // -------- Voter Traits button --------
            var traitsBtn = AddUIComponent<UIButton>();
            traitsBtn.text = "Voter Traits";
            traitsBtn.size = new Vector2(200, 32);
            traitsBtn.relativePosition = new Vector3(230, height - 50);
            traitsBtn.normalBgSprite = "ButtonMenu";
            traitsBtn.hoveredBgSprite = "ButtonMenuHovered";
            traitsBtn.pressedBgSprite = "ButtonMenuPressed";
            traitsBtn.textColor = Color.white;
            traitsBtn.eventClick += (c, p) =>
            {
                VoterTraitsPanel.Toggle();
            };

            // -------- Election Stats button --------
            var statsBtn = AddUIComponent<UIButton>();
            statsBtn.text = "Election Stats";
            statsBtn.size = new Vector2(200, 32);
            statsBtn.relativePosition = new Vector3(230, height - 130);
            statsBtn.normalBgSprite = "ButtonMenu";
            statsBtn.hoveredBgSprite = "ButtonMenuHovered";
            statsBtn.pressedBgSprite = "ButtonMenuPressed";
            statsBtn.textColor = Color.white;
            statsBtn.eventClick += (c, p) =>
            {
                ElectionStatsPanel.Toggle();
            };

            // -------- Minimize Chirps checkbox --------
            var minChirpsCB = AddUIComponent<UICheckBox>();
            minChirpsCB.relativePosition = new Vector3(15, height - 127);
            minChirpsCB.size = new Vector2(200f, 22f);

            var cbUnchecked = minChirpsCB.AddUIComponent<UISprite>();
            cbUnchecked.spriteName = "AchievementCheckedFalse";
            cbUnchecked.size = new Vector2(16f, 16f);
            cbUnchecked.relativePosition = new Vector3(0f, 3f);
            var cbChecked = minChirpsCB.AddUIComponent<UISprite>();
            cbChecked.spriteName = "AchievementCheckedTrue";
            cbChecked.size = new Vector2(16f, 16f);
            cbChecked.relativePosition = new Vector3(0f, 3f);
            minChirpsCB.checkedBoxObject = cbChecked;

            var cbLbl = minChirpsCB.AddUIComponent<UILabel>();
            cbLbl.text = "Minimize chirps";
            cbLbl.textScale = 0.8f;
            cbLbl.relativePosition = new Vector3(22f, 3f);
            cbLbl.tooltip = "Only post essential chirps: campaign start, election results, and bill passages.";

            minChirpsCB.isChecked = DebugFlags.MinimalChirps;
            minChirpsCB.eventCheckChanged += (c, v) => { DebugFlags.MinimalChirps = v; };

            // -------- Runtime config sliders --------
            float sliderY = 310f;
            var header = AddUIComponent<UILabel>();
            header.text = "Election timings (editable)";
            header.textScale = 0.95f;
            header.relativePosition = new Vector3(15, sliderY);
            sliderY += 26f;

            _termLbl = BuildSliderRow(sliderY, "Term length", out _termSlider,
                RuntimeConfig.MinTerm, RuntimeConfig.MaxTerm, RuntimeConfig.TermLengthDays,
                v => { RuntimeConfig.TermLengthDays = v; RuntimeConfig.ClampAll(); UpdateSliderLabels(); });
            sliderY += 48f;

            _campLbl = BuildSliderRow(sliderY, "Campaign length", out _campSlider,
                RuntimeConfig.MinCampaign, RuntimeConfig.MaxCampaign, RuntimeConfig.CampaignLengthDays,
                v => { RuntimeConfig.CampaignLengthDays = v; RuntimeConfig.ClampAll(); UpdateSliderLabels(); });
            sliderY += 48f;

            _coolLbl = BuildSliderRow(sliderY, "Re-election cooldown", out _coolSlider,
                RuntimeConfig.MinCooldown, RuntimeConfig.MaxCooldown, RuntimeConfig.ReElectionCooldownDays,
                v => { RuntimeConfig.ReElectionCooldownDays = v; RuntimeConfig.ClampAll(); UpdateSliderLabels(); });

            UpdateSliderLabels();
        }

        private UILabel BuildSliderRow(float y, string name, out UISlider slider,
                                       float min, float max, float value,
                                       Action<float> onChanged)
        {
            var nameLbl = AddUIComponent<UILabel>();
            nameLbl.text = name;
            nameLbl.textScale = 0.8f;
            nameLbl.relativePosition = new Vector3(15, y);

            var valueLbl = AddUIComponent<UILabel>();
            valueLbl.textScale = 0.8f;
            valueLbl.relativePosition = new Vector3(width - 115, y);

            slider = AddUIComponent<UISlider>();
            slider.relativePosition = new Vector3(15, y + 18);
            slider.size = new Vector2(width - 30, 16);
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = 1f;
            slider.value    = Mathf.Clamp(value, min, max);

            // Slider visual bits
            var track = slider.AddUIComponent<UISlicedSprite>();
            track.relativePosition = new Vector3(0, 7);
            track.size = new Vector2(slider.width, 3);
            track.spriteName = "BudgetSlider";

            var thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.size = new Vector2(12, 16);
            thumb.spriteName = "SliderBudget";
            slider.thumbObject = thumb;

            slider.eventValueChanged += (c, v) => { onChanged(v); };

            // Return value label so we can update it from UpdateSliderLabels
            return valueLbl;
        }

        private void UpdateSliderLabels()
        {
            if (_termLbl != null) _termLbl.text = ((int)RuntimeConfig.TermLengthDays) + " days";
            if (_campLbl != null) _campLbl.text = ((int)RuntimeConfig.CampaignLengthDays) + " days";
            if (_coolLbl != null) _coolLbl.text = ((int)RuntimeConfig.ReElectionCooldownDays) + " days";
            // Keep sliders in sync if ClampAll modified values
            if (_termSlider != null && Mathf.Abs(_termSlider.value - RuntimeConfig.TermLengthDays) > 0.5f)
                _termSlider.value = RuntimeConfig.TermLengthDays;
            if (_campSlider != null && Mathf.Abs(_campSlider.value - RuntimeConfig.CampaignLengthDays) > 0.5f)
                _campSlider.value = RuntimeConfig.CampaignLengthDays;
            if (_coolSlider != null && Mathf.Abs(_coolSlider.value - RuntimeConfig.ReElectionCooldownDays) > 0.5f)
                _coolSlider.value = RuntimeConfig.ReElectionCooldownDays;
        }

        public override void Update()
        {
            base.Update();

            // NOTE: hotkey detection lives in PoliticsOverlay.Update() (a
            // plain MonoBehaviour on a DontDestroyOnLoad GameObject). Hosting
            // it here was unreliable because the Colossal UI framework stops
            // ticking UIComponent.Update() on components that were never
            // visible, leaving brand-new sessions with no way to open the
            // panel.

            if (!isVisible) return;

            var st = PoliticsState.Instance;
            if (st == null) return;

            _phaseLabel.text = string.Format("Phase: {0} | Day {1}/{2} of term",
                st.Phase, (int)st.DaysSinceLastElection, (int)RuntimeConfig.TermLengthDays);

            // Parliament hemicycle + legend
            var coalSet = new HashSet<int>(st.CoalitionPartyIds ?? new List<int>());
            if (_hemi != null) _hemi.SetData(st.CurrentSeats, coalSet, Config.ParliamentSeats);
            if (_legend != null) _legend.Refresh(st.CurrentSeats, coalSet, Config.ParliamentSeats);

            // Coalition
            if (st.CoalitionPartyIds != null && st.CoalitionPartyIds.Count > 0)
            {
                var sb = new StringBuilder("Coalition: ");
                for (int i = 0; i < st.CoalitionPartyIds.Count; i++)
                {
                    if (i > 0) sb.Append(" + ");
                    sb.Append(Config.Parties[st.CoalitionPartyIds[i]].ShortName);
                }
                int totalSeats = 0;
                foreach (var id in st.CoalitionPartyIds) totalSeats += st.CurrentSeats[id];
                sb.Append("  (" + totalSeats + "/" + Config.ParliamentSeats + ")");
                _coalitionLabel.text = sb.ToString();
            }
            else
            {
                _coalitionLabel.text = "Coalition: (none)";
            }

            // Policies - single-line summary with truncation.
            if (st.AppliedVanillaPolicies != null && st.AppliedVanillaPolicies.Count > 0)
            {
                const int maxShown = 4;
                var sb = new StringBuilder("Active policies: ");
                int shown = Math.Min(maxShown, st.AppliedVanillaPolicies.Count);
                for (int i = 0; i < shown; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(st.AppliedVanillaPolicies[i]);
                }
                if (st.AppliedVanillaPolicies.Count > shown)
                    sb.Append(" (+").Append(st.AppliedVanillaPolicies.Count - shown).Append(" more)");
                _policiesLabel.text = sb.ToString();
                _policiesLabel.tooltip = string.Join(", ",
                    st.AppliedVanillaPolicies.ConvertAll(p => p.ToString()).ToArray());
            }
            else
            {
                _policiesLabel.text = "Active policies: (none)";
                _policiesLabel.tooltip = "";
            }

            _overlayBtn.text = "Overlay: " + st.Overlay;
        }

        private void OnGUI()
        {
            // Toast
            if (!string.IsNullOrEmpty(LatestToast) &&
                Time.realtimeSinceStartup - LatestToastTime < Config.NotificationDurationS)
            {
                var s = new GUIStyle(GUI.skin.box);
                s.fontSize = 16;
                s.normal.textColor = Color.white;
                GUI.Box(new Rect(Screen.width / 2f - 250f, 40f, 500f, 40f), LatestToast, s);
            }
            // Results popup
            if (!string.IsNullOrEmpty(ResultsPopupText) &&
                Time.realtimeSinceStartup < ResultsPopupShownUntil)
            {
                var s = new GUIStyle(GUI.skin.box);
                s.fontSize = 14;
                s.normal.textColor = Color.white;
                s.alignment = TextAnchor.UpperLeft;
                GUI.Box(new Rect(Screen.width - 460f, 100f, 440f, 260f), ResultsPopupText, s);
            }
        }
    }
}
