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
    //  VOTER TRAITS PANEL - edit per-trait economic-axis biases with sliders.
    // ========================================================================
    public class VoterTraitsPanel : UIPanel
    {
        private static VoterTraitsPanel _instance;

        public static void Toggle()
        {
            var view = UIView.GetAView();
            if (view == null) return;
            bool justCreated = false;
            if (_instance == null)
            {
                _instance = view.AddUIComponent(typeof(VoterTraitsPanel)) as VoterTraitsPanel;
                justCreated = true;
            }
            if (_instance != null)
            {
                _instance.isVisible = justCreated ? true : !_instance.isVisible;
                if (_instance.isVisible) _instance.RefreshAll();
            }
        }

        private struct Row
        {
            public UILabel ValueLabel;
            public UISlider Slider;
            public Func<float> Get;
            public Action<float> Set;
            public string Fmt;
        }
        private List<Row> _rows = new List<Row>();

        public override void Start()
        {
            base.Start();
            width = 560;
            height = 720;
            backgroundSprite = "MenuPanel2";
            canFocus = true;
            isInteractive = true;
            relativePosition = new Vector3(180, 80);
            BuildUI();
            // Visibility controlled by Toggle()
            L10n.LanguageChanged += OnLanguageChanged;
        }

        public override void OnDestroy()
        {
            L10n.LanguageChanged -= OnLanguageChanged;
            base.OnDestroy();
        }

        private void OnLanguageChanged()
        {
            var kids = new List<GameObject>();
            foreach (Transform t in transform) kids.Add(t.gameObject);
            foreach (var g in kids) UnityEngine.Object.Destroy(g);
            _rows.Clear();
            BuildUI();
        }

        private void BuildUI()
        {
            var title = AddUIComponent<UILabel>();
            title.text = L10n.T(L10nKeys.VoterTraits_Title);
            title.textScale = 1.0f;
            title.relativePosition = new Vector3(15, 10);

            UIHelpers.MakeDraggable(this);

            var close = AddUIComponent<UIButton>();
            close.text = L10n.T(L10nKeys.Common_CloseX);
            close.size = new Vector2(28, 24);
            close.relativePosition = new Vector3(width - 35, 8);
            close.normalBgSprite = "ButtonMenu";
            close.hoveredBgSprite = "ButtonMenuHovered";
            close.pressedBgSprite = "ButtonMenuPressed";
            close.eventClick += (c, p) => { isVisible = false; };

            var note = AddUIComponent<UILabel>();
            note.text = L10n.T(L10nKeys.VoterTraits_Note);
            note.textScale = 0.72f;
            note.textColor = new Color32(190, 190, 195, 255);
            note.relativePosition = new Vector3(15, 34);
            note.autoSize = false;
            note.wordWrap = true;
            note.size = new Vector2(width - 30, 60);

            float y = 100f;
            y = AddSection(L10n.T(L10nKeys.VoterTraits_Section_Education), y);
            y = AddRow(y, L10n.T(L10nKeys.VoterTraits_Edu_Uneducated),        () => VoterTraits.BiasEduUneducated,     v => VoterTraits.BiasEduUneducated     = v);
            y = AddRow(y, L10n.T(L10nKeys.VoterTraits_Edu_Educated),          () => VoterTraits.BiasEduEducated,       v => VoterTraits.BiasEduEducated       = v);
            y = AddRow(y, L10n.T(L10nKeys.VoterTraits_Edu_WellEducated),      () => VoterTraits.BiasEduWellEducated,   v => VoterTraits.BiasEduWellEducated   = v);
            y = AddRow(y, L10n.T(L10nKeys.VoterTraits_Edu_HighlyEducated),    () => VoterTraits.BiasEduHighlyEducated, v => VoterTraits.BiasEduHighlyEducated = v);
            y += 6f;
            y = AddSection(L10n.T(L10nKeys.VoterTraits_Section_Wealth), y);
            y = AddRow(y, L10n.T(L10nKeys.VoterTraits_Wealth_Low),            () => VoterTraits.BiasWealthLow,    v => VoterTraits.BiasWealthLow    = v);
            y = AddRow(y, L10n.T(L10nKeys.VoterTraits_Wealth_Medium),         () => VoterTraits.BiasWealthMedium, v => VoterTraits.BiasWealthMedium = v);
            y = AddRow(y, L10n.T(L10nKeys.VoterTraits_Wealth_High),           () => VoterTraits.BiasWealthHigh,   v => VoterTraits.BiasWealthHigh   = v);
            y += 6f;
            y = AddSection(L10n.T(L10nKeys.VoterTraits_Section_Employment), y);
            y = AddRow(y, L10n.T(L10nKeys.VoterTraits_Employment_Employed),   () => VoterTraits.BiasEmployed,   v => VoterTraits.BiasEmployed   = v);
            y = AddRow(y, L10n.T(L10nKeys.VoterTraits_Employment_Unemployed), () => VoterTraits.BiasUnemployed, v => VoterTraits.BiasUnemployed = v);
            y += 6f;
            y = AddSection(L10n.T(L10nKeys.VoterTraits_Section_Age), y);
            y = AddRow(y, L10n.T(L10nKeys.VoterTraits_Age_Young),             () => VoterTraits.BiasYoung,  v => VoterTraits.BiasYoung  = v);
            y = AddRow(y, L10n.T(L10nKeys.VoterTraits_Age_Adult),             () => VoterTraits.BiasAdult,  v => VoterTraits.BiasAdult  = v);
            y = AddRow(y, L10n.T(L10nKeys.VoterTraits_Age_Senior),            () => VoterTraits.BiasSenior, v => VoterTraits.BiasSenior = v);
            y += 6f;
            y = AddSection(L10n.T(L10nKeys.VoterTraits_Section_Life), y);
            y = AddRow(y, L10n.T(L10nKeys.VoterTraits_Life_Sick),             () => VoterTraits.BiasSick,          v => VoterTraits.BiasSick          = v);
            y = AddRow(y, L10n.T(L10nKeys.VoterTraits_Life_Pollution),        () => VoterTraits.BiasHighPollution, v => VoterTraits.BiasHighPollution = v);

            y += 6f;
            y = AddSection(L10n.T(L10nKeys.VoterTraits_Section_Deficit), y);
            y = AddCustomRow(y, L10n.T(L10nKeys.VoterTraits_Deficit_Label),
                RuntimeConfig.MinDeficitMult, RuntimeConfig.MaxDeficitMult, 0.1f,
                () => RuntimeConfig.DeficitPressureMultiplier,
                v => RuntimeConfig.DeficitPressureMultiplier = v,
                "0.00",
                L10n.T(L10nKeys.VoterTraits_Deficit_Tooltip));

            y += 6f;
            y = AddSection(L10n.T(L10nKeys.VoterTraits_Section_Incumbency), y);
            y = AddCustomRow(y, L10n.T(L10nKeys.VoterTraits_Incumbency_Label),
                RuntimeConfig.MinIncumbency, RuntimeConfig.MaxIncumbency, 0.01f,
                () => RuntimeConfig.IncumbencyBonus,
                v => RuntimeConfig.IncumbencyBonus = v,
                "0.00",
                L10n.T(L10nKeys.VoterTraits_Incumbency_Tooltip));

            // Reset button
            var reset = AddUIComponent<UIButton>();
            reset.text = L10n.T(L10nKeys.VoterTraits_Reset);
            reset.size = new Vector2(180, 28);
            reset.relativePosition = new Vector3(width - 200, height - 40);
            reset.normalBgSprite = "ButtonMenu";
            reset.hoveredBgSprite = "ButtonMenuHovered";
            reset.pressedBgSprite = "ButtonMenuPressed";
            reset.textColor = Color.white;
            reset.eventClick += (c, p) => { VoterTraits.ResetToDefaults(); RefreshAll(); };
        }

        private float AddSection(string text, float y)
        {
            var hdr = AddUIComponent<UILabel>();
            hdr.text = text;
            hdr.textScale = 0.9f;
            hdr.textColor = new Color32(220, 220, 230, 255);
            hdr.relativePosition = new Vector3(15, y);
            return y + 22f;
        }

        private float AddRow(float y, string label, Func<float> get, Action<float> set)
        {
            var nameLbl = AddUIComponent<UILabel>();
            nameLbl.text = label;
            nameLbl.textScale = 0.8f;
            nameLbl.relativePosition = new Vector3(25, y);

            var valLbl = AddUIComponent<UILabel>();
            valLbl.textScale = 0.8f;
            valLbl.relativePosition = new Vector3(width - 65, y);
            valLbl.text = get().ToString("+0.00;-0.00;0.00");

            var slider = AddUIComponent<UISlider>();
            slider.relativePosition = new Vector3(180, y + 4);
            slider.size = new Vector2(width - 260, 12);
            slider.minValue = VoterTraits.Min;
            slider.maxValue = VoterTraits.Max;
            slider.stepSize = 0.05f;
            slider.value = Mathf.Clamp(get(), VoterTraits.Min, VoterTraits.Max);

            var track = slider.AddUIComponent<UISlicedSprite>();
            track.relativePosition = new Vector3(0, 5);
            track.size = new Vector2(slider.width, 3);
            track.spriteName = "BudgetSlider";
            var thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.size = new Vector2(10, 12);
            thumb.spriteName = "SliderBudget";
            slider.thumbObject = thumb;

            slider.eventValueChanged += (c, v) =>
            {
                set(v);
                valLbl.text = v.ToString("+0.00;-0.00;0.00");
            };

            _rows.Add(new Row { ValueLabel = valLbl, Slider = slider, Get = get, Set = set, Fmt = "+0.00;-0.00;0.00" });
            return y + 22f;
        }

        /// <summary>
        /// Slider row with an explicit range + value-format string. Used by
        /// knobs that don't live on the -1..+1 voter-bias scale (e.g.
        /// deficit sensitivity).
        /// </summary>
        private float AddCustomRow(float y, string label,
                                   float min, float max, float step,
                                   Func<float> get, Action<float> set,
                                   string fmt, string tooltip = null)
        {
            var nameLbl = AddUIComponent<UILabel>();
            nameLbl.text = label;
            nameLbl.textScale = 0.8f;
            nameLbl.relativePosition = new Vector3(25, y);

            var valLbl = AddUIComponent<UILabel>();
            valLbl.textScale = 0.8f;
            valLbl.relativePosition = new Vector3(width - 65, y);
            valLbl.text = get().ToString(fmt);

            var slider = AddUIComponent<UISlider>();
            slider.relativePosition = new Vector3(180, y + 4);
            slider.size = new Vector2(width - 260, 12);
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = step;
            slider.value = Mathf.Clamp(get(), min, max);

            var track = slider.AddUIComponent<UISlicedSprite>();
            track.relativePosition = new Vector3(0, 5);
            track.size = new Vector2(slider.width, 3);
            track.spriteName = "BudgetSlider";
            var thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.size = new Vector2(10, 12);
            thumb.spriteName = "SliderBudget";
            slider.thumbObject = thumb;

            slider.eventValueChanged += (c, v) =>
            {
                set(v);
                valLbl.text = v.ToString(fmt);
            };

            if (!string.IsNullOrEmpty(tooltip))
            {
                nameLbl.tooltip = tooltip;
                valLbl.tooltip  = tooltip;
                slider.tooltip  = tooltip;
            }

            _rows.Add(new Row { ValueLabel = valLbl, Slider = slider, Get = get, Set = set, Fmt = fmt });
            return y + 22f;
        }

        public void RefreshAll()
        {
            foreach (var r in _rows)
            {
                float v = r.Get();
                r.Slider.value = v;
                r.ValueLabel.text = v.ToString(r.Fmt ?? "+0.00;-0.00;0.00");
            }
        }
    }
}
