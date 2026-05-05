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
        private UIPanel _policiesIconRow;
        // Hash of the currently-rendered policy list, so the icon row is
        // only rebuilt when the list actually changes (Update() runs every
        // frame and we don't want to destroy/recreate UI that often).
        private int _lastPoliciesHash = -1;
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
            float legendH = PartyLegendRow.HeightFor(PartyCountRef.Value);
            _legend.size = new Vector2(width - 30, legendH);
            _legend.Build(PartyCountRef.Value);

            // Coalition/policies labels used to live at hardcoded y=270 and
            // y=293, which assumed a 22px single-row legend. When the legend
            // wraps to a second row (7+ parties) we shift everything below
            // down by the extra height so the labels aren't overlapped.
            float extraLegend = Mathf.Max(0f, legendH - 22f);

            _coalitionLabel = AddUIComponent<UILabel>();
            _coalitionLabel.textScale = 0.85f;
            _coalitionLabel.relativePosition = new Vector3(15, 270f + extraLegend);

            _policiesLabel = AddUIComponent<UILabel>();
            _policiesLabel.textScale = 0.8f;
            _policiesLabel.relativePosition = new Vector3(15, 293f + extraLegend);
            _policiesLabel.autoSize = false;
            _policiesLabel.size = new Vector2(105, 18);
            _policiesLabel.clipChildren = true;
            _policiesLabel.text = "Active policies:";

            // Icon row sits immediately to the right of the label and holds
            // one UISprite per active policy. Populated by RebuildPoliciesIcons
            // whenever the policy list changes.
            _policiesIconRow = AddUIComponent<UIPanel>();
            _policiesIconRow.relativePosition = new Vector3(120, 293f + extraLegend);
            _policiesIconRow.size = new Vector2(width - 135, 18);
            _policiesIconRow.clipChildren = true;
            _policiesIconRow.autoLayout = false;

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
            // Shift down by the same extraLegend offset so the "Election
            // timings" header doesn't overlap the Active policies label
            // when the legend wraps to a second row.
            float sliderY = 310f + extraLegend;
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

            // Policies - horizontal icon row. The row is rebuilt only when
            // the list of applied policies actually changes (hash compare),
            // so we're not churning UI components every frame.
            var policies = st.AppliedVanillaPolicies;
            int hash = 17;
            if (policies != null)
            {
                for (int i = 0; i < policies.Count; i++)
                    hash = unchecked(hash * 31 + (int)policies[i]);
            }
            if (hash != _lastPoliciesHash)
            {
                _lastPoliciesHash = hash;
                RebuildPoliciesIcons(policies);
            }

            _overlayBtn.text = "Overlay: " + st.Overlay;
        }

        /// <summary>
        /// Destroy the current policy icons and rebuild the row from
        /// <paramref name="policies"/>. Uses the vanilla "IconPolicy{Name}"
        /// sprites from the default UI atlas, same convention the party
        /// editor uses.  Each icon gets a tooltip with the spaced-out
        /// policy name. If the icons won't all fit in the row width, the
        /// trailing ones collapse into a "+N" label whose tooltip lists
        /// them.
        /// </summary>
        private void RebuildPoliciesIcons(List<DistrictPolicies.Policies> policies)
        {
            if (_policiesIconRow == null) return;

            // Tear down whatever is currently in the row.
            var toKill = new List<GameObject>();
            foreach (Transform t in _policiesIconRow.transform) toKill.Add(t.gameObject);
            foreach (var g in toKill) UnityEngine.Object.Destroy(g);

            if (policies == null || policies.Count == 0)
            {
                var none = _policiesIconRow.AddUIComponent<UILabel>();
                none.text = "(none)";
                none.textScale = 0.8f;
                none.relativePosition = new Vector3(0f, 0f);
                return;
            }

            var view = GetUIView();
            var atlas = view != null ? view.defaultAtlas : null;

            const float iconSize = 16f;
            const float gap      = 3f;
            float slotW = iconSize + gap;

            // How many icons physically fit into the row.
            int maxFit = Mathf.Max(1, Mathf.FloorToInt((_policiesIconRow.width + gap) / slotW));
            bool overflow = policies.Count > maxFit;
            // Reserve the last slot for a "+N more" marker when overflowing.
            int shown = overflow ? Math.Max(0, maxFit - 1) : policies.Count;

            float x = 0f;
            for (int i = 0; i < shown; i++)
            {
                var policy = policies[i];
                var icon = _policiesIconRow.AddUIComponent<UISprite>();
                icon.atlas = atlas;
                icon.spriteName = "IconPolicy" + policy.ToString();
                icon.size = new Vector2(iconSize, iconSize);
                icon.relativePosition = new Vector3(x, 1f);
                icon.tooltip = FormatPolicyDisplayName(policy);
                x += slotW;
            }

            if (overflow)
            {
                var more = _policiesIconRow.AddUIComponent<UILabel>();
                more.text = "+" + (policies.Count - shown);
                more.textScale = 0.75f;
                more.relativePosition = new Vector3(x, 3f);
                // Tooltip lists every policy that was hidden.
                var hidden = new StringBuilder();
                for (int i = shown; i < policies.Count; i++)
                {
                    if (hidden.Length > 0) hidden.Append(", ");
                    hidden.Append(FormatPolicyDisplayName(policies[i]));
                }
                more.tooltip = hidden.ToString();
            }
        }

        /// <summary>Split PascalCase enum name into a spaced human label.</summary>
        private static string FormatPolicyDisplayName(DistrictPolicies.Policies p)
        {
            string raw = p.ToString();
            var sb = new StringBuilder(raw.Length + 4);
            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && char.IsUpper(raw[i]) && !char.IsUpper(raw[i - 1])) sb.Append(' ');
                sb.Append(raw[i]);
            }
            return sb.ToString();
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
                // Reserve a bit of top-right padding inside the box for the
                // close button so text doesn't run under it.
                s.padding = new RectOffset(8, 36, 8, 8);
                Rect boxRect = new Rect(Screen.width - 460f, 100f, 440f, 260f);
                GUI.Box(boxRect, ResultsPopupText, s);

                // Close button in the top-right corner of the popup.
                var closeStyle = new GUIStyle(GUI.skin.button);
                closeStyle.fontSize = 14;
                closeStyle.alignment = TextAnchor.MiddleCenter;
                Rect closeRect = new Rect(boxRect.xMax - 28f, boxRect.y + 6f, 22f, 22f);
                if (GUI.Button(closeRect, "x", closeStyle))
                {
                    // Force-hide immediately. The guard above re-evaluates
                    // next frame and skips rendering.
                    ResultsPopupShownUntil = 0f;
                }
            }
        }
    }
}
