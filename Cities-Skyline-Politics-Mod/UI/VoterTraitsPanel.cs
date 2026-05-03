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
        }
        private List<Row> _rows = new List<Row>();

        public override void Start()
        {
            base.Start();
            width = 560;
            height = 660;
            backgroundSprite = "MenuPanel2";
            canFocus = true;
            isInteractive = true;
            relativePosition = new Vector3(180, 80);
            BuildUI();
            // Visibility controlled by Toggle()
        }

        private void BuildUI()
        {
            var title = AddUIComponent<UILabel>();
            title.text = "Voter Traits - Economic Axis Bias (-1 left ... +1 right)";
            title.textScale = 1.0f;
            title.relativePosition = new Vector3(15, 10);

            UIHelpers.MakeDraggable(this);

            var close = AddUIComponent<UIButton>();
            close.text = "X";
            close.size = new Vector2(28, 24);
            close.relativePosition = new Vector3(width - 35, 8);
            close.normalBgSprite = "ButtonMenu";
            close.hoveredBgSprite = "ButtonMenuHovered";
            close.pressedBgSprite = "ButtonMenuPressed";
            close.eventClick += (c, p) => { isVisible = false; };

            var note = AddUIComponent<UILabel>();
            note.text =
                "Nudges voters on the economic axis. Negative = left-leaning (higher\n" +
                "taxes, stronger services). Positive = right-leaning (lower taxes,\n" +
                "business-friendly). Only Young, Adult, and Senior citizens vote.";
            note.textScale = 0.72f;
            note.textColor = new Color32(190, 190, 195, 255);
            note.relativePosition = new Vector3(15, 34);
            note.autoSize = false;
            note.wordWrap = true;
            note.size = new Vector2(width - 30, 60);

            float y = 100f;
            y = AddSection("Education", y);
            y = AddRow(y, "Uneducated",        () => VoterTraits.BiasEduUneducated,     v => VoterTraits.BiasEduUneducated     = v);
            y = AddRow(y, "Educated",          () => VoterTraits.BiasEduEducated,       v => VoterTraits.BiasEduEducated       = v);
            y = AddRow(y, "Well-educated",     () => VoterTraits.BiasEduWellEducated,   v => VoterTraits.BiasEduWellEducated   = v);
            y = AddRow(y, "Highly-educated",   () => VoterTraits.BiasEduHighlyEducated, v => VoterTraits.BiasEduHighlyEducated = v);
            y += 6f;
            y = AddSection("Wealth", y);
            y = AddRow(y, "Low wealth",        () => VoterTraits.BiasWealthLow,    v => VoterTraits.BiasWealthLow    = v);
            y = AddRow(y, "Medium wealth",     () => VoterTraits.BiasWealthMedium, v => VoterTraits.BiasWealthMedium = v);
            y = AddRow(y, "High wealth",       () => VoterTraits.BiasWealthHigh,   v => VoterTraits.BiasWealthHigh   = v);
            y += 6f;
            y = AddSection("Employment", y);
            y = AddRow(y, "Employed",          () => VoterTraits.BiasEmployed,   v => VoterTraits.BiasEmployed   = v);
            y = AddRow(y, "Unemployed",        () => VoterTraits.BiasUnemployed, v => VoterTraits.BiasUnemployed = v);
            y += 6f;
            y = AddSection("Age", y);
            y = AddRow(y, "Young",             () => VoterTraits.BiasYoung,  v => VoterTraits.BiasYoung  = v);
            y = AddRow(y, "Adult",             () => VoterTraits.BiasAdult,  v => VoterTraits.BiasAdult  = v);
            y = AddRow(y, "Senior",            () => VoterTraits.BiasSenior, v => VoterTraits.BiasSenior = v);
            y += 6f;
            y = AddSection("Life conditions", y);
            y = AddRow(y, "Sick",              () => VoterTraits.BiasSick,          v => VoterTraits.BiasSick          = v);
            y = AddRow(y, "Lives in pollution",() => VoterTraits.BiasHighPollution, v => VoterTraits.BiasHighPollution = v);

            // Reset button
            var reset = AddUIComponent<UIButton>();
            reset.text = "Reset to defaults";
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

            _rows.Add(new Row { ValueLabel = valLbl, Slider = slider, Get = get, Set = set });
            return y + 22f;
        }

        public void RefreshAll()
        {
            foreach (var r in _rows)
            {
                float v = r.Get();
                r.Slider.value = v;
                r.ValueLabel.text = v.ToString("+0.00;-0.00;0.00");
            }
        }
    }
}
