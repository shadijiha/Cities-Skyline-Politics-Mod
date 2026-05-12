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
    //  PARTY EDITOR PANEL - in-game editor to add/remove parties and edit
    //  name, color, ideology, policies, and modifiers. Opened via the
    //  "Manage Parties" button on the main politics panel.
    // ========================================================================
    public class PartyEditorPanel : UIPanel
    {
        public const int MinParties = 1;
        public const int MaxParties = 12;

        private static PartyEditorPanel _instance;

        public static void Toggle()
        {
            var view = UIView.GetAView();
            if (view == null) return;
            bool justCreated = false;
            if (_instance == null)
            {
                _instance = view.AddUIComponent(typeof(PartyEditorPanel)) as PartyEditorPanel;
                justCreated = true;
            }
            if (_instance == null) return;
            // When first creating the panel, force it visible. Otherwise the
            // panel's own Start() sets isVisible=false right after we toggle
            // it on, swallowing the click.
            _instance.isVisible = justCreated ? true : !_instance.isVisible;
        }

        // Currently selected party index in Config.Parties
        private int _selectedIdx = 0;

        // Left-side list container + per-row buttons (rebuilt whenever the
        // party list mutates).
        private UIPanel _listPanel;
        private UIButton[] _listButtons;

        // Right-side detail form - a scrollable panel so tall forms fit.
        private UIScrollablePanel _formPanel;
        private UIScrollbar _formScroll;

        // Add/Remove buttons at the bottom of the list.
        private UIButton _addBtn, _removeBtn;

        public override void Start()
        {
            base.Start();
            width = 1100;
            height = 760;
            backgroundSprite = "MenuPanel2";
            canFocus = true;
            isInteractive = true;
            relativePosition = new Vector3(120, 30);
            BuildUI();

            // Reset the form scroll every time the panel goes from hidden
            // to visible. Without this, closing while scrolled and then
            // reopening leaves UIScrollablePanel in a stale layout state
            // (visible gap at the top, bottom content clipped).
            eventVisibilityChanged += (c, visible) =>
            {
                if (!visible || _formPanel == null) return;
                _formPanel.scrollPosition = Vector2.zero;
            };

            // Do NOT set isVisible here — the caller (Toggle) controls it.
            // Setting isVisible = false would fight with the Toggle that
            // triggered this Start() in the first place.
        }

        private void BuildUI()
        {
            var title = AddUIComponent<UILabel>();
            title.text = L10n.T(L10nKeys.PartyEditor_Title);
            title.textScale = 1.2f;
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

            // ---- Left: party list ----
            _listPanel = AddUIComponent<UIPanel>();
            _listPanel.relativePosition = new Vector3(15, 50);
            _listPanel.size = new Vector2(220, height - 110);
            _listPanel.backgroundSprite = "GenericPanel";
            _listPanel.color = new Color32(40, 40, 45, 220);            _addBtn = AddUIComponent<UIButton>();
            _addBtn.text = L10n.T(L10nKeys.PartyEditor_Add);
            _addBtn.size = new Vector2(105, 28);
            _addBtn.relativePosition = new Vector3(15, height - 50);
            _addBtn.normalBgSprite = "ButtonMenu";
            _addBtn.hoveredBgSprite = "ButtonMenuHovered";
            _addBtn.pressedBgSprite = "ButtonMenuPressed";
            _addBtn.textColor = Color.white;
            _addBtn.eventClick += (c, p) => { OnAddClicked(); };

            _removeBtn = AddUIComponent<UIButton>();
            _removeBtn.text = L10n.T(L10nKeys.PartyEditor_Remove);
            _removeBtn.size = new Vector2(105, 28);
            _removeBtn.relativePosition = new Vector3(130, height - 50);
            _removeBtn.normalBgSprite = "ButtonMenu";
            _removeBtn.hoveredBgSprite = "ButtonMenuHovered";
            _removeBtn.pressedBgSprite = "ButtonMenuPressed";
            _removeBtn.textColor = Color.white;
            _removeBtn.eventClick += (c, p) => { OnRemoveClicked(); };

            // ---- Right: detail form (scrollable) ----
            // Container just to hold the scrollbar next to the scrollable panel.
            var formContainer = AddUIComponent<UIPanel>();
            formContainer.relativePosition = new Vector3(250, 50);
            formContainer.size = new Vector2(width - 265, height - 65);
            formContainer.backgroundSprite = "GenericPanel";
            formContainer.color = new Color32(55, 55, 60, 220);

            _formPanel = formContainer.AddUIComponent<UIScrollablePanel>();
            _formPanel.relativePosition = new Vector3(0, 0);
            _formPanel.size = new Vector2(formContainer.width - 18, formContainer.height);
            _formPanel.autoLayout = false;
            _formPanel.clipChildren = true;
            _formPanel.scrollWheelDirection = UIOrientation.Vertical;
            _formPanel.builtinKeyNavigation = true;

            _formScroll = formContainer.AddUIComponent<UIScrollbar>();
            _formScroll.relativePosition = new Vector3(formContainer.width - 16, 2);
            _formScroll.size = new Vector2(14, formContainer.height - 4);
            _formScroll.orientation = UIOrientation.Vertical;
            _formScroll.stepSize = 24f;
            _formScroll.incrementAmount = 48f;

            var track = _formScroll.AddUIComponent<UISlicedSprite>();
            track.relativePosition = Vector3.zero;
            track.size = _formScroll.size;
            track.spriteName = "ScrollbarTrack";
            _formScroll.trackObject = track;

            var thumb = track.AddUIComponent<UISlicedSprite>();
            thumb.relativePosition = Vector3.zero;
            thumb.spriteName = "ScrollbarThumb";
            thumb.size = new Vector2(14, 40);
            _formScroll.thumbObject = thumb;

            _formPanel.verticalScrollbar = _formScroll;
            _formPanel.eventMouseWheel += (c, e) =>
            {
                _formPanel.scrollPosition = new Vector2(
                    _formPanel.scrollPosition.x,
                    Mathf.Max(0f, _formPanel.scrollPosition.y - e.wheelDelta * 48f));
            };

            RebuildList();
        }

        // ---- List management -------------------------------------------

        private void RebuildList()
        {
            if (_listPanel == null) return;
            // Remove prior buttons
            if (_listButtons != null)
            {
                foreach (var b in _listButtons)
                {
                    if (b != null) UnityEngine.Object.Destroy(b.gameObject);
                }
            }
            int n = Config.Parties.Length;
            _listButtons = new UIButton[n];
            for (int i = 0; i < n; i++)
            {
                int idx = i; // capture
                var b = _listPanel.AddUIComponent<UIButton>();
                b.size = new Vector2(_listPanel.width - 10, 34);
                b.relativePosition = new Vector3(5, 5 + i * 38);
                b.normalBgSprite  = (idx == _selectedIdx) ? "ButtonMenuFocused" : "ButtonMenu";
                b.hoveredBgSprite = "ButtonMenuHovered";
                b.pressedBgSprite = "ButtonMenuPressed";
                b.textColor = Color.white;
                b.textScale = 0.85f;
                b.textPadding = new RectOffset(28, 4, 8, 6);
                b.textHorizontalAlignment = UIHorizontalAlignment.Left;
                var p = Config.Parties[idx];
                b.text = p.ShortName + " - " + p.FullName;
                b.tooltip = p.FullName;

                // Color swatch
                var swatch = b.AddUIComponent<UISprite>();
                swatch.spriteName = "EmptySprite";
                swatch.size = new Vector2(16f, 16f);
                swatch.relativePosition = new Vector3(6f, 9f);
                swatch.color = p.Color;

                b.eventClick += (c, e) => { SelectParty(idx); };
                _listButtons[i] = b;
            }
            UpdateAddRemoveEnabled();
        }

        private void UpdateAddRemoveEnabled()
        {
            int n = Config.Parties.Length;
            if (_addBtn != null)    _addBtn.isEnabled    = n < MaxParties;
            if (_removeBtn != null) _removeBtn.isEnabled = n > MinParties;
        }

        private void SelectParty(int idx)
        {
            _selectedIdx = Mathf.Clamp(idx, 0, Config.Parties.Length - 1);
            RebuildList();
            // Form rebuild hook - implemented in the next step.
            RebuildForm();
        }

        // Form controls (rebuilt each time selection changes)
        private UITextField _shortNameField, _fullNameField;
        private UISprite _colorSwatch;
        private UISlider _econSlider, _socSlider, _govSlider;
        private UILabel  _econLbl, _socLbl, _govLbl;
        // Checkboxes for vanilla policies (built lazily from enum values)
        private UICheckBox[] _policyChecks;
        private DistrictPolicies.Policies[] _policyEnumCache;
        // Modifier sliders (tax + budgets)
        private UISlider[] _modSliders;     // index = ModifierField order
        private UILabel[]  _modLabels;

        /// <summary>Clear and rebuild the right-side form for the selected party.</summary>
        private void RebuildForm()
        {
            if (_formPanel == null) return;
            // Tear down all children
            var children = new List<GameObject>();
            foreach (Transform child in _formPanel.transform) children.Add(child.gameObject);
            foreach (var g in children) UnityEngine.Object.Destroy(g);

            // Reset scroll to the top — otherwise switching parties keeps the
            // old scroll offset and hides the top of the newly-built form.
            _formPanel.scrollPosition = Vector2.zero;

            if (_selectedIdx < 0 || _selectedIdx >= Config.Parties.Length) return;
            var party = Config.Parties[_selectedIdx];

            float y = 10f;
            const float labelW = 130f;
            const float fieldW = 250f;

            // --- Short name ---
            AddFormLabel(L10n.T(L10nKeys.PartyEditor_ShortName), 10, y);
            _shortNameField = AddTextField(labelW, y, 100f, party.ShortName, (v) => {
                party.ShortName = (v ?? "").Trim();
                RebuildList();
            });
            y += 34f;

            // --- Full name ---
            AddFormLabel(L10n.T(L10nKeys.PartyEditor_FullName), 10, y);
            _fullNameField = AddTextField(labelW, y, fieldW, party.FullName, (v) => {
                party.FullName = (v ?? "").Trim();
                RebuildList();
            });
            y += 34f;

            // --- Color picker: a clickable swatch that opens RGB sliders ---
            AddFormLabel(L10n.T(L10nKeys.PartyEditor_Color), 10, y);
            _colorSwatch = BuildColorPickerRow(labelW, y, party);
            y += 88f;   // swatch + R/G/B slider rows

            // --- Ideology sliders ---
            var header = _formPanel.AddUIComponent<UILabel>();
            header.text = L10n.T(L10nKeys.PartyEditor_IdeologyHeader);
            header.textScale = 0.95f;
            header.relativePosition = new Vector3(10, y);
            y += 24f;

            _econLbl = AddModSliderRow(y, L10n.T(L10nKeys.PartyEditor_Ideology_Economic), party.Ideology.x, -1f, +1f,
                v => { party.Ideology = new Vector3(v, party.Ideology.y, party.Ideology.z); UpdateIdeologyLabels(party); },
                out _econSlider);
            y += 44f;

            _socLbl = AddModSliderRow(y, L10n.T(L10nKeys.PartyEditor_Ideology_Social), party.Ideology.y, -1f, +1f,
                v => { party.Ideology = new Vector3(party.Ideology.x, v, party.Ideology.z); UpdateIdeologyLabels(party); },
                out _socSlider);
            y += 44f;

            _govLbl = AddModSliderRow(y, L10n.T(L10nKeys.PartyEditor_Ideology_Governance), party.Ideology.z, -1f, +1f,
                v => { party.Ideology = new Vector3(party.Ideology.x, party.Ideology.y, v); UpdateIdeologyLabels(party); },
                out _govSlider);
            y += 50f;

            UpdateIdeologyLabels(party);

            // --- Vanilla policy tiles ---
            var polHdr = _formPanel.AddUIComponent<UILabel>();
            polHdr.text = L10n.T(L10nKeys.PartyEditor_PoliciesHeader);
            polHdr.textScale = 0.95f;
            polHdr.relativePosition = new Vector3(10, y);
            y += 22f;

            _policyEnumCache = GetInterestingPolicies();
            _policyChecks = new UICheckBox[_policyEnumCache.Length]; // kept for compat (unused for tiles)

            // Scrollable sub-panel for policy tiles so the main form stays compact.
            const float policyBoxHeight = 220f;
            var policyScroll = _formPanel.AddUIComponent<UIScrollablePanel>();
            policyScroll.relativePosition = new Vector3(10, y);
            policyScroll.size = new Vector2(_formPanel.width - 30f, policyBoxHeight);
            policyScroll.backgroundSprite = "GenericPanel";
            policyScroll.color = new Color32(30, 30, 35, 200);
            policyScroll.autoLayout = false;
            policyScroll.clipChildren = true;
            policyScroll.scrollWheelDirection = UIOrientation.Vertical;
            policyScroll.builtinKeyNavigation = true;

            var policyScrollBar = _formPanel.AddUIComponent<UIScrollbar>();
            policyScrollBar.relativePosition = new Vector3(_formPanel.width - 20f, y);
            policyScrollBar.size = new Vector2(12, policyBoxHeight);
            policyScrollBar.orientation = UIOrientation.Vertical;
            policyScrollBar.stepSize = 20f;
            policyScrollBar.incrementAmount = 40f;
            var pbTrack = policyScrollBar.AddUIComponent<UISlicedSprite>();
            pbTrack.relativePosition = Vector3.zero;
            pbTrack.size = policyScrollBar.size;
            pbTrack.spriteName = "ScrollbarTrack";
            policyScrollBar.trackObject = pbTrack;
            var pbThumb = pbTrack.AddUIComponent<UISlicedSprite>();
            pbThumb.relativePosition = Vector3.zero;
            pbThumb.spriteName = "ScrollbarThumb";
            pbThumb.size = new Vector2(12, 30);
            policyScrollBar.thumbObject = pbThumb;
            policyScroll.verticalScrollbar = policyScrollBar;
            policyScroll.eventMouseWheel += (c, e) =>
            {
                policyScroll.scrollPosition = new Vector2(
                    policyScroll.scrollPosition.x,
                    Mathf.Max(0f, policyScroll.scrollPosition.y - e.wheelDelta * 40f));
            };

            // Build tiles inside the scrollable panel (relative to it).
            const int cols = 6;
            float margin = 6f;
            float tileSize = Mathf.Min(56f, (policyScroll.width - 12f - (cols - 1) * margin) / cols);
            float colW = tileSize + margin;
            float rowH = tileSize + 18f;
            for (int i = 0; i < _policyEnumCache.Length; i++)
            {
                int col = i % cols;
                int row = i / cols;
                var policy = _policyEnumCache[i];
                BuildPolicyTile(policyScroll,
                    2 + col * colW, 2 + row * rowH,
                    tileSize, policy, GetPartyPolicyStance(party, policy), party);
            }
            y += policyBoxHeight + 10f;

            // --- Tax modifiers ---
            var taxHdr = _formPanel.AddUIComponent<UILabel>();
            taxHdr.text = L10n.T(L10nKeys.PartyEditor_TaxHeader);
            taxHdr.textScale = 0.9f;
            taxHdr.relativePosition = new Vector3(10, y);
            y += 22f;

            y = AddModifierIntRow(y, L10n.T(L10nKeys.PartyEditor_Tax_Res), party.Modifiers.TaxDeltaRes,
                v => party.Modifiers.TaxDeltaRes = v);
            y = AddModifierIntRow(y, L10n.T(L10nKeys.PartyEditor_Tax_Com),  party.Modifiers.TaxDeltaCom,
                v => party.Modifiers.TaxDeltaCom = v);
            y = AddModifierIntRow(y, L10n.T(L10nKeys.PartyEditor_Tax_Ind),  party.Modifiers.TaxDeltaInd,
                v => party.Modifiers.TaxDeltaInd = v);
            y = AddModifierIntRow(y, L10n.T(L10nKeys.PartyEditor_Tax_Off),      party.Modifiers.TaxDeltaOff,
                v => party.Modifiers.TaxDeltaOff = v);

            y += 4f;
            // --- Budget modifiers ---
            var budHdr = _formPanel.AddUIComponent<UILabel>();
            budHdr.text = L10n.T(L10nKeys.PartyEditor_BudgetHeader);
            budHdr.textScale = 0.9f;
            budHdr.relativePosition = new Vector3(10, y);
            y += 22f;

            y = AddBudgetRow(y, L10n.T(L10nKeys.PartyEditor_Budget_Electricity),    party.Modifiers.BudgetDeltaElectricity,
                v => party.Modifiers.BudgetDeltaElectricity = v);
            y = AddBudgetRow(y, L10n.T(L10nKeys.PartyEditor_Budget_Water),          party.Modifiers.BudgetDeltaWater,
                v => party.Modifiers.BudgetDeltaWater = v);
            y = AddBudgetRow(y, L10n.T(L10nKeys.PartyEditor_Budget_Garbage),        party.Modifiers.BudgetDeltaGarbage,
                v => party.Modifiers.BudgetDeltaGarbage = v);
            y = AddBudgetRow(y, L10n.T(L10nKeys.PartyEditor_Budget_Healthcare),     party.Modifiers.BudgetDeltaHealth,
                v => party.Modifiers.BudgetDeltaHealth = v);
            y = AddBudgetRow(y, L10n.T(L10nKeys.PartyEditor_Budget_Fire),           party.Modifiers.BudgetDeltaFire,
                v => party.Modifiers.BudgetDeltaFire = v);
            y = AddBudgetRow(y, L10n.T(L10nKeys.PartyEditor_Budget_Police),         party.Modifiers.BudgetDeltaPolice,
                v => party.Modifiers.BudgetDeltaPolice = v);
            y = AddBudgetRow(y, L10n.T(L10nKeys.PartyEditor_Budget_Education),      party.Modifiers.BudgetDeltaEducation,
                v => party.Modifiers.BudgetDeltaEducation = v);
            y = AddBudgetRow(y, L10n.T(L10nKeys.PartyEditor_Budget_Transport),      party.Modifiers.BudgetDeltaTransport,
                v => party.Modifiers.BudgetDeltaTransport = v);
            y = AddBudgetRow(y, L10n.T(L10nKeys.PartyEditor_Budget_Beautification), party.Modifiers.BudgetDeltaBeautification,
                v => party.Modifiers.BudgetDeltaBeautification = v);
            y = AddBudgetRow(y, L10n.T(L10nKeys.PartyEditor_Budget_Roads),          party.Modifiers.BudgetDeltaRoads,
                v => party.Modifiers.BudgetDeltaRoads = v);
            y = AddBudgetRow(y, L10n.T(L10nKeys.PartyEditor_Budget_Industry),       party.Modifiers.BudgetDeltaIndustry,
                v => party.Modifiers.BudgetDeltaIndustry = v);

            // Add an invisible spacer child at the bottom of the form. The
            // scrollable panel measures its content extent by its children's
            // bounds, so placing a child past the last row forces the scroll
            // range to extend that far.
            if (_formPanel != null)
            {
                var spacer = _formPanel.AddUIComponent<UIPanel>();
                spacer.name = "FormBottomSpacer";
                spacer.backgroundSprite = null;
                spacer.color = new Color32(0, 0, 0, 0);
                spacer.size = new Vector2(4f, 40f);
                spacer.relativePosition = new Vector3(0f, y + 30f);
            }
        }

        /// <summary>The vanilla policies that make sense as city-wide platform items.</summary>
        /// <summary>
        /// Enumerate every DistrictPolicies.Policies enum value the game currently
        /// knows about, sorted alphabetically. Excludes "None". DLC-gated enum
        /// values are included if the enum value exists on this CS1 build.
        /// </summary>
        private static DistrictPolicies.Policies[] GetInterestingPolicies()
        {
            var values = Enum.GetValues(typeof(DistrictPolicies.Policies));
            var list = new List<DistrictPolicies.Policies>(values.Length);
            foreach (DistrictPolicies.Policies v in values)
            {
                // Skip the implicit 0/"None" entry
                if ((int)v == 0) continue;
                // Skip combined flag masks (if any - names typically include "Mask" or "All")
                var n = v.ToString();
                if (n.IndexOf("Mask", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (n.Equals("All",  StringComparison.OrdinalIgnoreCase)) continue;
                // Skip synthetic combos (those containing ", ")
                if (n.Contains(", ")) continue;
                list.Add(v);
            }
            list.Sort((a, b) => string.Compare(a.ToString(), b.ToString(),
                StringComparison.OrdinalIgnoreCase));
            return list.ToArray();
        }

        private static PolicyStance GetPartyPolicyStance(PartyDef party, DistrictPolicies.Policies policy)
        {
            if (party.VanillaPolicies != null)
                foreach (var p in party.VanillaPolicies)
                    if (p == policy) return PolicyStance.Support;
            if (party.OpposedPolicies != null)
                foreach (var p in party.OpposedPolicies)
                    if (p == policy) return PolicyStance.Oppose;
            return PolicyStance.Neutral;
        }

        private static void SetPartyPolicyStance(PartyDef party, DistrictPolicies.Policies policy, PolicyStance stance)
        {
            // Keep the two lists mutually exclusive: a policy can be in at
            // most one of Vanilla (Support) / Opposed (Oppose), never both.
            var sup = new HashSet<DistrictPolicies.Policies>(party.VanillaPolicies ?? new DistrictPolicies.Policies[0]);
            var opp = new HashSet<DistrictPolicies.Policies>(party.OpposedPolicies ?? new DistrictPolicies.Policies[0]);
            sup.Remove(policy);
            opp.Remove(policy);
            if (stance == PolicyStance.Support) sup.Add(policy);
            else if (stance == PolicyStance.Oppose) opp.Add(policy);

            var supArr = new DistrictPolicies.Policies[sup.Count]; int i = 0;
            foreach (var p in sup) supArr[i++] = p;
            var oppArr = new DistrictPolicies.Policies[opp.Count]; i = 0;
            foreach (var p in opp) oppArr[i++] = p;
            party.VanillaPolicies = supArr;
            party.OpposedPolicies = oppArr;
        }

        private static void TogglePolicyOnParty(PartyDef party, DistrictPolicies.Policies policy, bool on)
        {
            // Legacy helper retained for any older callers; routes to the new
            // stance setter. on=true -> Support, on=false -> Neutral.
            SetPartyPolicyStance(party, policy, on ? PolicyStance.Support : PolicyStance.Neutral);
        }

        /// <summary>
        /// Build a single clickable policy "tile" - an icon + label styled like
        /// the vanilla policy buttons. Click cycles the party's stance on this
        /// policy: Neutral -> Support -> Oppose -> Neutral.
        ///   Neutral = grey border, don't touch when elected
        ///   Support = green-tinted selected border, enact when elected
        ///   Oppose  = red-tinted selected border, repeal when elected
        /// </summary>
        private UIButton BuildPolicyTile(UIComponent parent, float x, float y, float size,
                                         DistrictPolicies.Policies policy,
                                         PolicyStance initialStance,
                                         PartyDef party)
        {
            var btn = parent.AddUIComponent<UIButton>();
            btn.relativePosition = new Vector3(x, y);
            btn.size = new Vector2(size, size);
            btn.hoveredBgSprite  = "ButtonMenuHovered";
            btn.pressedBgSprite  = "ButtonMenuPressed";
            btn.text = "";

            // Icon - vanilla "IconPolicy<Name>" sprite convention.
            var icon = btn.AddUIComponent<UISprite>();
            icon.size = new Vector2(size * 0.8f, size * 0.8f);
            icon.relativePosition = new Vector3((size - icon.width) / 2f,
                                                (size - icon.height) / 2f);
            string spriteName = "IconPolicy" + policy.ToString();
            icon.atlas = parent.GetUIView().defaultAtlas;
            icon.spriteName = spriteName;

            ApplyStanceToTile(btn, policy, initialStance);

            var capturedPolicy = policy;
            var capturedParty = party;
            btn.eventClick += (c, p) =>
            {
                var cur = GetPartyPolicyStance(capturedParty, capturedPolicy);
                var next = cur == PolicyStance.Neutral ? PolicyStance.Support
                         : cur == PolicyStance.Support ? PolicyStance.Oppose
                         : PolicyStance.Neutral;
                SetPartyPolicyStance(capturedParty, capturedPolicy, next);
                ApplyStanceToTile(btn, capturedPolicy, next);
                btn.Unfocus();
            };
            return btn;
        }

        // Apply the visual state + tooltip for a given stance. Colouring is
        // done via UIButton.color (tints the background sprite).
        private static void ApplyStanceToTile(UIButton btn, DistrictPolicies.Policies policy, PolicyStance stance)
        {
            // For Support / Oppose we swap to EmptySprite (plain colour fill)
            // so the tint renders at full brightness. ButtonMenuFocused is a
            // dark panel sprite - any tint applied to it gets multiplied
            // down into a muddy / grey-looking colour, which is why the
            // previous "red" came out looking desaturated.
            string bg;
            string hoverBg;
            string pressBg;
            Color32 col;
            string tip;
            switch (stance)
            {
                case PolicyStance.Support:
                    bg      = "EmptySprite";
                    hoverBg = "EmptySprite";
                    pressBg = "EmptySprite";
                    col = new Color32( 60, 200,  80, 255); // clean green fill
                    tip = L10n.T(L10nKeys.PartyEditor_Stance_Support, FormatPolicyName(policy));
                    break;
                case PolicyStance.Oppose:
                    bg      = "EmptySprite";
                    hoverBg = "EmptySprite";
                    pressBg = "EmptySprite";
                    col = new Color32(220,  40,  40, 255); // clean red fill
                    tip = L10n.T(L10nKeys.PartyEditor_Stance_Oppose, FormatPolicyName(policy));
                    break;
                default:
                    bg      = "ButtonMenu";
                    hoverBg = "ButtonMenuHovered";
                    pressBg = "ButtonMenuPressed";
                    col = new Color32(255, 255, 255, 255);
                    tip = L10n.T(L10nKeys.PartyEditor_Stance_Neutral, FormatPolicyName(policy));
                    break;
            }
            btn.normalBgSprite   = bg;
            btn.focusedBgSprite  = bg; // keep visual stable through focus
            btn.disabledBgSprite = bg;
            btn.hoveredBgSprite  = hoverBg;
            btn.pressedBgSprite  = pressBg;
            // Tint every state so hovering / pressing an opposed or supported
            // tile keeps the colour instead of reverting to the default grey
            // tint of ButtonMenuHovered / ButtonMenuPressed.
            btn.color            = col;
            btn.focusedColor     = col;
            btn.disabledColor    = col;
            btn.hoveredColor     = col;
            btn.pressedColor     = col;
            btn.tooltip          = tip;
        }

        /// <summary>Split a CamelCase enum name into spaced words.</summary>
        private static string FormatPolicyName(DistrictPolicies.Policies p)
        {
            string raw = p.ToString();
            var sb = new StringBuilder();
            for (int i = 0; i < raw.Length; i++)
            {
                if (i > 0 && char.IsUpper(raw[i]) && !char.IsUpper(raw[i - 1])) sb.Append(' ');
                sb.Append(raw[i]);
            }
            return sb.ToString();
        }

        private UICheckBox MakeCheckbox(float x, float y, string text, bool initial)
        {
            var cb = _formPanel.AddUIComponent<UICheckBox>();
            cb.relativePosition = new Vector3(x, y);
            cb.size = new Vector2(18f, 18f);
            // Small "check" sprite hack
            var sprite = cb.AddUIComponent<UISprite>();
            sprite.spriteName = "AchievementCheckedFalse";
            sprite.size = new Vector2(16f, 16f);
            sprite.relativePosition = Vector3.zero;
            var checkedSprite = cb.AddUIComponent<UISprite>();
            checkedSprite.spriteName = "AchievementCheckedTrue";
            checkedSprite.size = new Vector2(16f, 16f);
            checkedSprite.relativePosition = Vector3.zero;
            cb.checkedBoxObject = checkedSprite;

            var lbl = cb.AddUIComponent<UILabel>();
            lbl.text = text;
            lbl.textScale = 0.75f;
            lbl.relativePosition = new Vector3(22f, 0f);
            lbl.size = new Vector2(200f, 18f);

            cb.isChecked = initial;
            return cb;
        }

        private float AddModifierIntRow(float y, string label, int value, Action<int> onChanged)
        {
            return AddIntSliderRow(y, label, value, -10, 10, onChanged);
        }

        private float AddBudgetRow(float y, string label, int value, Action<int> onChanged)
        {
            return AddIntSliderRow(y, label, value, -30, 30, onChanged);
        }

        private float AddIntSliderRow(float y, string label, int value, int min, int max, Action<int> onChanged)
        {
            var nameLbl = _formPanel.AddUIComponent<UILabel>();
            nameLbl.text = label;
            nameLbl.textScale = 0.78f;
            nameLbl.relativePosition = new Vector3(10, y);

            var valLbl = _formPanel.AddUIComponent<UILabel>();
            valLbl.textScale = 0.78f;
            valLbl.text = value.ToString();
            valLbl.relativePosition = new Vector3(_formPanel.width - 60, y);

            var slider = _formPanel.AddUIComponent<UISlider>();
            slider.relativePosition = new Vector3(130, y + 3);
            slider.size = new Vector2(_formPanel.width - 200, 12);
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = 1f;
            slider.value = Mathf.Clamp(value, min, max);

            var track = slider.AddUIComponent<UISlicedSprite>();
            track.relativePosition = new Vector3(0, 5);
            track.size = new Vector2(slider.width, 3);
            track.spriteName = "BudgetSlider";
            var thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.size = new Vector2(10, 12);
            thumb.spriteName = "SliderBudget";
            slider.thumbObject = thumb;

            slider.eventValueChanged += (c, v) => {
                int iv = Mathf.RoundToInt(v);
                valLbl.text = iv.ToString();
                onChanged(iv);
            };
            return y + 18f;
        }

        // ---- Form helpers ---------------------------------------------

        private UILabel AddFormLabel(string text, float x, float y)
        {
            var l = _formPanel.AddUIComponent<UILabel>();
            l.text = text;
            l.textScale = 0.85f;
            l.relativePosition = new Vector3(x, y + 4);
            return l;
        }

        private UITextField AddTextField(float x, float y, float w, string initial, Action<string> onChanged)
        {
            var t = _formPanel.AddUIComponent<UITextField>();
            t.size = new Vector2(w, 28f);
            t.relativePosition = new Vector3(x, y);
            t.builtinKeyNavigation = true;
            t.readOnly = false;
            t.selectOnFocus = true;
            t.canFocus = true;
            t.horizontalAlignment = UIHorizontalAlignment.Left;
            t.normalBgSprite = "TextFieldPanel";
            // Foreground colour + visible blinking caret. Without cursorWidth/
            // cursorBlinkTime + a selectionSprite, Colossal's UITextField
            // draws the text but no caret, so users can type but have no
            // visual indication of where they are.
            t.color       = Color.white;
            t.textColor   = Color.white;
            t.cursorWidth = 1;
            t.cursorBlinkTime = 0.45f;
            t.selectionSprite = "EmptySprite";
            t.selectionBackgroundColor = new Color32(0, 120, 200, 255);
            t.padding = new RectOffset(6, 6, 6, 6);
            t.text = initial ?? "";
            t.eventTextSubmitted += (c, v) => { onChanged(v); };
            t.eventLostFocus     += (c, e) => { onChanged(t.text); };
            return t;
        }

        /// <summary>
        /// Build a simple RGB color picker: a colored swatch on the left plus
        /// three short R/G/B sliders stacked next to it. Updates party.Color
        /// live and refreshes the left-side list.
        /// </summary>
        private UISprite BuildColorPickerRow(float x, float y, PartyDef party)
        {
            var swatch = _formPanel.AddUIComponent<UISprite>();
            swatch.size = new Vector2(48f, 48f);
            swatch.relativePosition = new Vector3(x, y);
            swatch.spriteName = "EmptySprite";
            swatch.color = party.Color;

            float sliderX = x + 60f;
            float sliderY = y;
            float sliderW = _formPanel.width - sliderX - 20f;

            // R / G / B rows
            Action<char, int> addSlider = null;
            addSlider = (channel, initial) =>
            {
                var lbl = _formPanel.AddUIComponent<UILabel>();
                lbl.text = channel.ToString();
                lbl.textScale = 0.8f;
                lbl.relativePosition = new Vector3(sliderX, sliderY);

                var valLbl = _formPanel.AddUIComponent<UILabel>();
                valLbl.textScale = 0.75f;
                valLbl.text = initial.ToString();
                valLbl.relativePosition = new Vector3(_formPanel.width - 45, sliderY);

                var sl = _formPanel.AddUIComponent<UISlider>();
                sl.relativePosition = new Vector3(sliderX + 16f, sliderY + 2);
                sl.size = new Vector2(sliderW - 60f, 12);
                sl.minValue = 0f;
                sl.maxValue = 255f;
                sl.stepSize = 1f;
                sl.value = initial;

                var track = sl.AddUIComponent<UISlicedSprite>();
                track.relativePosition = new Vector3(0, 5);
                track.size = new Vector2(sl.width, 3);
                track.spriteName = "BudgetSlider";
                var thumb = sl.AddUIComponent<UISlicedSprite>();
                thumb.size = new Vector2(10, 12);
                thumb.spriteName = "SliderBudget";
                sl.thumbObject = thumb;

                char capC = channel;
                sl.eventValueChanged += (c, v) =>
                {
                    byte b = (byte)Mathf.Clamp((int)v, 0, 255);
                    valLbl.text = b.ToString();
                    var col = party.Color;
                    if (capC == 'R') col.r = b;
                    else if (capC == 'G') col.g = b;
                    else col.b = b;
                    party.Color = col;
                    swatch.color = col;
                    RebuildList();
                };

                sliderY += 18f;
            };
            addSlider('R', party.Color.r);
            addSlider('G', party.Color.g);
            addSlider('B', party.Color.b);
            return swatch;
        }

        private UILabel AddModSliderRow(float y, string label, float value, float min, float max,
                                        Action<float> onChanged, out UISlider slider)
        {
            var nameLbl = _formPanel.AddUIComponent<UILabel>();
            nameLbl.text = label;
            nameLbl.textScale = 0.8f;
            nameLbl.relativePosition = new Vector3(10, y);

            var valLbl = _formPanel.AddUIComponent<UILabel>();
            valLbl.textScale = 0.8f;
            valLbl.relativePosition = new Vector3(_formPanel.width - 80, y);

            slider = _formPanel.AddUIComponent<UISlider>();
            slider.relativePosition = new Vector3(10, y + 18);
            slider.size = new Vector2(_formPanel.width - 20, 16);
            slider.minValue = min;
            slider.maxValue = max;
            slider.stepSize = 0.05f;
            slider.value = Mathf.Clamp(value, min, max);

            var track = slider.AddUIComponent<UISlicedSprite>();
            track.relativePosition = new Vector3(0, 7);
            track.size = new Vector2(slider.width, 3);
            track.spriteName = "BudgetSlider";

            var thumb = slider.AddUIComponent<UISlicedSprite>();
            thumb.size = new Vector2(12, 16);
            thumb.spriteName = "SliderBudget";
            slider.thumbObject = thumb;

            slider.eventValueChanged += (c, v) => { onChanged(v); };
            return valLbl;
        }

        private void UpdateIdeologyLabels(PartyDef p)
        {
            if (_econLbl != null) _econLbl.text = p.Ideology.x.ToString("0.00");
            if (_socLbl  != null) _socLbl.text  = p.Ideology.y.ToString("0.00");
            if (_govLbl  != null) _govLbl.text  = p.Ideology.z.ToString("0.00");
        }

        private void OnAddClicked()
        {
            if (Config.Parties.Length >= MaxParties) return;
            // Create a new party with sensible defaults.
            var colorPool = new Color32[]
            {
                new Color32(120, 144, 156, 255), // slate
                new Color32(255, 152,   0, 255), // orange
                new Color32(0,   150, 136, 255), // teal
                new Color32(233,  30,  99, 255), // pink
                new Color32(205, 220,  57, 255), // lime
            };
            var used = new HashSet<uint>();
            foreach (var p in Config.Parties)
            {
                used.Add(((uint)p.Color.r << 16) | ((uint)p.Color.g << 8) | p.Color.b);
            }
            Color32 pick = colorPool[0];
            foreach (var c in colorPool)
            {
                uint key = ((uint)c.r << 16) | ((uint)c.g << 8) | c.b;
                if (!used.Contains(key)) { pick = c; break; }
            }
            int newIdx = Config.Parties.Length;
            string shortName = L10n.T(L10nKeys.PartyEditor_NewPartyShortName, newIdx + 1);
            var newParty = new PartyDef
            {
                Id = newIdx,
                ShortName = shortName,
                FullName = L10n.T(L10nKeys.PartyEditor_NewPartyFullName, newIdx + 1),
                Color = pick,
                Ideology = Vector3.zero,
                VanillaPolicies = new DistrictPolicies.Policies[0],
                OpposedPolicies = new DistrictPolicies.Policies[0],
                Modifiers = new PolicyModifiers { PollutionMultiplier = 1.0f }
            };
            var newList = new PartyDef[Config.Parties.Length + 1];
            Array.Copy(Config.Parties, newList, Config.Parties.Length);
            newList[newIdx] = newParty;
            Config.Parties = newList;
            ResizePerPartyArrays();
            // Poll history is indexed by party id at sample time; invalidate
            // it so the graph doesn't mix old samples with new ones.
            OpinionPolling.Reset();
            _selectedIdx = newIdx;
            RebuildList();
            RebuildForm();
            PoliticsUserMod.Log("Added party: " + shortName);
        }

        private void OnRemoveClicked()
        {
            if (Config.Parties.Length <= MinParties) return;
            if (_selectedIdx < 0 || _selectedIdx >= Config.Parties.Length) return;
            int removedIdx = _selectedIdx;
            string removed = Config.Parties[removedIdx].ShortName;

            var newList = new PartyDef[Config.Parties.Length - 1];
            int j = 0;
            for (int i = 0; i < Config.Parties.Length; i++)
            {
                if (i == removedIdx) continue;
                newList[j++] = Config.Parties[i];
            }
            // Reassign ids so they stay 0..N-1 contiguous.
            for (int i = 0; i < newList.Length; i++) newList[i].Id = i;
            Config.Parties = newList;

            // Shift (not truncate) per-party state so the values stay aligned
            // with the party they belong to, and remap every id stored in the
            // state through the old->new mapping.
            RemapStateAfterRemoval(removedIdx);
            // Poll history is indexed by party id at sample time; invalidate
            // it so the graph doesn't mix old samples with new ones.
            OpinionPolling.Reset();

            _selectedIdx = Mathf.Clamp(_selectedIdx, 0, Config.Parties.Length - 1);
            RebuildList();
            RebuildForm();
            PoliticsUserMod.Log("Removed party: " + removed + " (idx=" + removedIdx + ")");
        }

        /// <summary>
        /// Rewrite every piece of per-party state in PoliticsState so that
        /// indices and stored party ids reflect Config.Parties having lost
        /// slot <paramref name="removedIdx"/>. Values at indices greater than
        /// removedIdx shift down by one. Stored ids equal to removedIdx are
        /// dropped; ids greater than removedIdx decrement by one.
        /// </summary>
        private static void RemapStateAfterRemoval(int removedIdx)
        {
            var st = PoliticsState.Instance;
            if (st == null) return;
            int n = Config.Parties.Length;

            st.CurrentSeats    = ShiftIntArray(st.CurrentSeats, removedIdx, n);
            st.CurrentSupport  = ShiftFloatArray(st.CurrentSupport, removedIdx, n);
            st.ApprovalByParty = ShiftIntArray(st.ApprovalByParty, removedIdx, n);

            // Coalition ids: drop the removed one, shift higher ones down.
            if (st.CoalitionPartyIds != null)
            {
                var remapped = new List<int>(st.CoalitionPartyIds.Count);
                foreach (var id in st.CoalitionPartyIds)
                {
                    if (id == removedIdx) continue;
                    remapped.Add(id > removedIdx ? id - 1 : id);
                }
                st.CoalitionPartyIds = remapped;
            }

            // Per-building dominant party: removed -> 255 (no data),
            // higher ids shift down so the overlay still colours correctly.
            var bm = st.DominantPartyByBuilding;
            if (bm != null)
            {
                for (int i = 0; i < bm.Length; i++)
                {
                    byte pid = bm[i];
                    if (pid == 255) continue;
                    if (pid == removedIdx) bm[i] = 255;
                    else if (pid > removedIdx) bm[i] = (byte)(pid - 1);
                }
            }

            // History entries have per-party arrays sized at election time.
            // Shift them so old stats still display correctly side-by-side.
            if (st.History != null)
            {
                foreach (var r in st.History) RemapElectionResult(r, removedIdx);
            }
            if (st.LastResult != null && (st.History == null || st.History.Count == 0 || st.History[st.History.Count - 1] != st.LastResult))
            {
                RemapElectionResult(st.LastResult, removedIdx);
            }
            // Re-point LastResult at the tail of History if possible, so any
            // UI that reads either sees consistent data.
            if (st.History != null && st.History.Count > 0)
            {
                st.LastResult = st.History[st.History.Count - 1];
            }
        }

        private static void RemapElectionResult(ElectionResult r, int removedIdx)
        {
            if (r == null) return;
            if (r.SeatsByParty     != null) r.SeatsByParty     = ShiftIntArray(r.SeatsByParty,     removedIdx, r.SeatsByParty.Length     - 1);
            if (r.VoteShareByParty != null) r.VoteShareByParty = ShiftFloatArray(r.VoteShareByParty, removedIdx, r.VoteShareByParty.Length - 1);
            if (r.ApprovalByParty  != null) r.ApprovalByParty  = ShiftIntArray(r.ApprovalByParty,  removedIdx, r.ApprovalByParty.Length  - 1);

            if (r.CoalitionPartyIds != null)
            {
                var remapped = new List<int>(r.CoalitionPartyIds.Count);
                foreach (var id in r.CoalitionPartyIds)
                {
                    if (id == removedIdx) continue;
                    remapped.Add(id > removedIdx ? id - 1 : id);
                }
                r.CoalitionPartyIds = remapped;
            }

            r.VotesByAgeParty    = ShiftMatrixParty(r.VotesByAgeParty,    removedIdx);
            r.VotesByEduParty    = ShiftMatrixParty(r.VotesByEduParty,    removedIdx);
            r.VotesByWealthParty = ShiftMatrixParty(r.VotesByWealthParty, removedIdx);
        }

        // Copy src into a new array of length newLen, skipping index removedIdx.
        // If the resulting array is shorter than newLen, trailing slots stay 0.
        private static int[] ShiftIntArray(int[] src, int removedIdx, int newLen)
        {
            var dst = new int[newLen];
            if (src == null) return dst;
            int j = 0;
            for (int i = 0; i < src.Length && j < newLen; i++)
            {
                if (i == removedIdx) continue;
                dst[j++] = src[i];
            }
            return dst;
        }

        private static float[] ShiftFloatArray(float[] src, int removedIdx, int newLen)
        {
            var dst = new float[newLen];
            if (src == null) return dst;
            int j = 0;
            for (int i = 0; i < src.Length && j < newLen; i++)
            {
                if (i == removedIdx) continue;
                dst[j++] = src[i];
            }
            return dst;
        }

        // Shrink a [buckets, parties] matrix by dropping column removedIdx.
        private static int[,] ShiftMatrixParty(int[,] src, int removedIdx)
        {
            if (src == null) return null;
            int rows = src.GetLength(0);
            int cols = src.GetLength(1);
            if (removedIdx < 0 || removedIdx >= cols) return src;
            var dst = new int[rows, cols - 1];
            for (int r = 0; r < rows; r++)
            {
                int dc = 0;
                for (int c = 0; c < cols; c++)
                {
                    if (c == removedIdx) continue;
                    dst[r, dc++] = src[r, c];
                }
            }
            return dst;
        }

        /// <summary>
        /// Resize PoliticsState per-party arrays to match Config.Parties.Length.
        /// Also clear seats for removed parties so stale data doesn't show up
        /// in the hemicycle.
        /// </summary>
        private static void ResizePerPartyArrays()
        {
            var st = PoliticsState.Instance;
            if (st == null) return;
            int n = Config.Parties.Length;
            st.CurrentSeats    = ResizeIntArray(st.CurrentSeats, n);
            st.CurrentSupport  = ResizeFloatArray(st.CurrentSupport, n);
            st.ApprovalByParty = ResizeIntArray(st.ApprovalByParty, n);
            // Drop any coalition ids that no longer exist
            if (st.CoalitionPartyIds != null)
            {
                st.CoalitionPartyIds.RemoveAll(id => id < 0 || id >= n);
            }
        }

        private static int[] ResizeIntArray(int[] src, int len)
        {
            var dst = new int[len];
            if (src != null)
            {
                int copy = Math.Min(src.Length, len);
                for (int i = 0; i < copy; i++) dst[i] = src[i];
            }
            return dst;
        }

        private static float[] ResizeFloatArray(float[] src, int len)
        {
            var dst = new float[len];
            if (src != null)
            {
                int copy = Math.Min(src.Length, len);
                for (int i = 0; i < copy; i++) dst[i] = src[i];
            }
            return dst;
        }
    }
}
