using System.Collections.Generic;

namespace PoliticsMod.Localization.Languages
{
    // Master English catalog. All other languages fall back to this.
    public static class En
    {
        public static Language Build()
        {
            var s = new Dictionary<string, string>();

            // Mod metadata
            s[L10nKeys.Mod_Name]        = "Politics & Elections Mod";
            s[L10nKeys.Mod_Description] = "Citizens elect a parliament; coalitions shape city policies. Press Ctrl+P to open panel.";

            // Settings
            s[L10nKeys.Settings_Group_Main]         = "Politics & Elections";
            s[L10nKeys.Settings_EnableDebugLogging] = "Enable debug logging";
            s[L10nKeys.Settings_Group_Hotkey]       = "Panel toggle hotkey";
            s[L10nKeys.Settings_Hotkey]             = "Hotkey";
            s[L10nKeys.Settings_RequireCtrl]        = "Require Ctrl modifier";
            s[L10nKeys.Settings_Group_Utilities]    = "Utilities";
            s[L10nKeys.Settings_OpenElectionsPanel] = "Open Elections panel";

            // Common
            s[L10nKeys.Common_CloseX] = "X";

            // Main panel
            s[L10nKeys.Panel_Title]                  = "Politics & Elections";
            s[L10nKeys.Panel_Hemi_Tooltip]           = "Parliament size scales with your population:\n1 seat per {0} citizens (min {1}, max {2}).";
            s[L10nKeys.Panel_ActivePolicies]         = "Active policies:";
            s[L10nKeys.Panel_ActivePolicies_None]    = "(none)";
            s[L10nKeys.Panel_Overlay_Prefix]         = "Overlay: {0}";
            s[L10nKeys.Panel_Button_CallSnapElection]= "Call snap election";
            s[L10nKeys.Panel_Button_ManageParties]   = "Manage Parties";
            s[L10nKeys.Panel_Button_VoterTraits]     = "Voter Traits";
            s[L10nKeys.Panel_Button_ElectionStats]   = "Election Stats";
            s[L10nKeys.Panel_Button_OpinionPolling]  = "Opinion Polling";
            s[L10nKeys.Panel_MinimizeChirps]         = "Minimize chirps";
            s[L10nKeys.Panel_MinimizeChirps_Tooltip] = "Only post essential chirps: campaign start, election results, and bill passages.";
            s[L10nKeys.Panel_ElectionTimings]        = "Election timings (editable)";
            s[L10nKeys.Panel_Slider_TermLength]      = "Term length";
            s[L10nKeys.Panel_Slider_CampaignLength]  = "Campaign length";
            s[L10nKeys.Panel_Slider_ReElectionCooldown] = "Re-election cooldown";
            s[L10nKeys.Panel_Slider_ReElectionCooldown_Tooltip] =
                "In-game days parliament waits in limbo after a FAILED coalition\n" +
                "before auto-calling a snap re-election.\n" +
                "\n" +
                "Only triggers when no combination of parties can form a majority\n" +
                "within the coalition-partner cap. Set to 0 for immediate retries,\n" +
                "higher for a longer political deadlock. Has no effect on normal\n" +
                "elections where a coalition succeeds.";
            s[L10nKeys.Panel_Days]                   = "{0} days";
            s[L10nKeys.Panel_Phase_Campaign]         = "Phase: {0} | Day {1}/{2} of campaign";
            s[L10nKeys.Panel_Phase_Term]             = "Phase: {0} | Day {1}/{2} of term";
            s[L10nKeys.Panel_Coalition_Header]       = "Coalition: {0}  ({1}/{2})";
            s[L10nKeys.Panel_Coalition_None]         = "Coalition: (none)";
            s[L10nKeys.Panel_Policies_More]          = "+{0}";

            // Overlay
            s[L10nKeys.Overlay_Off]          = "Off";
            s[L10nKeys.Overlay_Party]        = "Party";
            s[L10nKeys.Overlay_Turnout]      = "Turnout";
            s[L10nKeys.Overlay_Satisfaction] = "Satisfaction";
            s[L10nKeys.InfoButton_Prefix]    = "Politics: {0}";

            // Party editor
            s[L10nKeys.PartyEditor_Title]               = "Manage Parties";
            s[L10nKeys.PartyEditor_Add]                 = "+ Add party";
            s[L10nKeys.PartyEditor_Remove]              = "– Remove";
            s[L10nKeys.PartyEditor_ShortName]           = "Short name";
            s[L10nKeys.PartyEditor_FullName]            = "Full name";
            s[L10nKeys.PartyEditor_Color]               = "Color";
            s[L10nKeys.PartyEditor_IdeologyHeader]      = "Ideology (-1 ... +1)";
            s[L10nKeys.PartyEditor_Ideology_Economic]   = "Economic (left↔right)";
            s[L10nKeys.PartyEditor_Ideology_Social]     = "Social (prog↔trad)";
            s[L10nKeys.PartyEditor_Ideology_Governance] = "Governance (lib↔auth)";
            s[L10nKeys.PartyEditor_PoliciesHeader]      = "Policies  (click to cycle: neutral -> support -> oppose)";
            s[L10nKeys.PartyEditor_Stance_Support]      = "{0}\nSupport: will be enacted when elected";
            s[L10nKeys.PartyEditor_Stance_Oppose]       = "{0}\nOppose: will be repealed when elected";
            s[L10nKeys.PartyEditor_Stance_Neutral]      = "{0}\nNeutral: left alone when elected";
            s[L10nKeys.PartyEditor_TaxHeader]           = "Tax deltas (pct points, -10 .. +10)";
            s[L10nKeys.PartyEditor_Tax_Res]             = "Residential";
            s[L10nKeys.PartyEditor_Tax_Com]             = "Commercial";
            s[L10nKeys.PartyEditor_Tax_Ind]             = "Industrial";
            s[L10nKeys.PartyEditor_Tax_Off]             = "Office";
            s[L10nKeys.PartyEditor_BudgetHeader]        = "Budget deltas (pct points, -30 .. +30)";
            s[L10nKeys.PartyEditor_Budget_Electricity]    = "Electricity";
            s[L10nKeys.PartyEditor_Budget_Water]          = "Water";
            s[L10nKeys.PartyEditor_Budget_Garbage]        = "Garbage";
            s[L10nKeys.PartyEditor_Budget_Healthcare]     = "Healthcare";
            s[L10nKeys.PartyEditor_Budget_Fire]           = "Fire";
            s[L10nKeys.PartyEditor_Budget_Police]         = "Police";
            s[L10nKeys.PartyEditor_Budget_Education]      = "Education";
            s[L10nKeys.PartyEditor_Budget_Transport]      = "Transport";
            s[L10nKeys.PartyEditor_Budget_Beautification] = "Beautification";
            s[L10nKeys.PartyEditor_Budget_Roads]          = "Roads";
            s[L10nKeys.PartyEditor_Budget_Industry]       = "Industry";
            s[L10nKeys.PartyEditor_NewPartyShortName]     = "NEW{0}";
            s[L10nKeys.PartyEditor_NewPartyFullName]      = "New Party {0}";

            // Voter Traits
            s[L10nKeys.VoterTraits_Title]                 = "Voter Traits - Economic Axis Bias (-1 left ... +1 right)";
            s[L10nKeys.VoterTraits_Note]                  =
                "Nudges voters on the economic axis. Negative = left-leaning (higher\n" +
                "taxes, stronger services). Positive = right-leaning (lower taxes,\n" +
                "business-friendly). Only Young, Adult, and Senior citizens vote.";
            s[L10nKeys.VoterTraits_Section_Education]     = "Education";
            s[L10nKeys.VoterTraits_Edu_Uneducated]        = "Uneducated";
            s[L10nKeys.VoterTraits_Edu_Educated]          = "Educated";
            s[L10nKeys.VoterTraits_Edu_WellEducated]      = "Well-educated";
            s[L10nKeys.VoterTraits_Edu_HighlyEducated]    = "Highly-educated";
            s[L10nKeys.VoterTraits_Section_Wealth]        = "Wealth";
            s[L10nKeys.VoterTraits_Wealth_Low]            = "Low wealth";
            s[L10nKeys.VoterTraits_Wealth_Medium]         = "Medium wealth";
            s[L10nKeys.VoterTraits_Wealth_High]           = "High wealth";
            s[L10nKeys.VoterTraits_Section_Employment]    = "Employment";
            s[L10nKeys.VoterTraits_Employment_Employed]   = "Employed";
            s[L10nKeys.VoterTraits_Employment_Unemployed] = "Unemployed";
            s[L10nKeys.VoterTraits_Section_Age]           = "Age";
            s[L10nKeys.VoterTraits_Age_Young]             = "Young";
            s[L10nKeys.VoterTraits_Age_Adult]             = "Adult";
            s[L10nKeys.VoterTraits_Age_Senior]            = "Senior";
            s[L10nKeys.VoterTraits_Section_Life]          = "Life conditions";
            s[L10nKeys.VoterTraits_Life_Sick]             = "Sick";
            s[L10nKeys.VoterTraits_Life_Pollution]        = "Lives in pollution";
            s[L10nKeys.VoterTraits_Section_Deficit]       = "Deficit sensitivity";
            s[L10nKeys.VoterTraits_Deficit_Label]         = "Deficit multiplier";
            s[L10nKeys.VoterTraits_Deficit_Tooltip]       =
                "How strongly a sustained budget deficit pushes voters toward\n" +
                "the economic right.\n" +
                "  0 = feature off, deficits don't move voters.\n" +
                "  1 = default curve: up to +0.35 right-wing nudge after\n" +
                "      about 6 consecutive deficit weeks.\n" +
                "  2 = twice as sensitive. 3 = maximum.\n" +
                "Applies on top of per-voter trait biases.";
            s[L10nKeys.VoterTraits_Section_Incumbency]    = "Incumbency";
            s[L10nKeys.VoterTraits_Incumbency_Label]      = "Incumbency bonus";
            s[L10nKeys.VoterTraits_Incumbency_Tooltip]    =
                "Probability a HAPPY voter rewards the sitting coalition with\n" +
                "their vote instead of following ideology / grievances.\n" +
                "Only applies to voters whose happiness is 60 or higher.\n" +
                "\n" +
                "  0.00 = incumbency has no effect; voters always pick on fit.\n" +
                "  0.10 = default. 1 in 10 happy voters flip to the gov.\n" +
                "  0.25 = quarter of happy voters auto-renew the gov.\n" +
                "  0.50 = half of happy voters auto-renew the gov; strong\n" +
                "         advantage for whoever's in power while the city is\n" +
                "         thriving.\n" +
                "\n" +
                "Tip: set low (<=0.05) if you want every election to feel like\n" +
                "an open contest, or high (>=0.25) to simulate hard-to-beat\n" +
                "incumbents.";
            s[L10nKeys.VoterTraits_Reset]                 = "Reset to defaults";

            // Election Stats
            s[L10nKeys.Stats_Title]             = "Election Stats";
            s[L10nKeys.Stats_NoData_Subtitle]   = "No election data yet.";
            s[L10nKeys.Stats_NoData_Body]       = "No election stat data found.";
            s[L10nKeys.Stats_Subtitle]          = "Election {0}-{1} - {2} votes sampled  •  Turnout {3}%";
            s[L10nKeys.Stats_PartyColors]       = "Party colors";
            s[L10nKeys.Stats_WhyPeopleVoted]    = "Why people voted";
            s[L10nKeys.Stats_Grievance_Ideology]       = "Pure ideology";
            s[L10nKeys.Stats_Grievance_HighTaxes]      = "High taxes";
            s[L10nKeys.Stats_Grievance_PoorHealth]     = "Poor health";
            s[L10nKeys.Stats_Grievance_HighCrime]      = "High crime";
            s[L10nKeys.Stats_Grievance_PoorEducation]  = "Poor education";
            s[L10nKeys.Stats_Grievance_Unemployment]   = "Unemployment";
            s[L10nKeys.Stats_Grievance_Pollution]      = "Pollution";
            s[L10nKeys.Stats_Grievance_LowLandValue]   = "Low land value";
            s[L10nKeys.Stats_Grievance_NoiseTrash]     = "Noise / trash";
            s[L10nKeys.Stats_Chart_ByAge]              = "Vote by age group";
            s[L10nKeys.Stats_Chart_ByEducation]        = "Vote by education";
            s[L10nKeys.Stats_Chart_ByWealth]           = "Vote by wealth";
            s[L10nKeys.Stats_Chart_NoData]             = "(no data - older save format)";
            s[L10nKeys.Stats_Votes_Suffix]             = "{0} votes";
            s[L10nKeys.Stats_PctCountFormat]           = "{0:P1}  ({1})";
            s[L10nKeys.Bucket_Age_Young]               = "Young";
            s[L10nKeys.Bucket_Age_Adult]               = "Adult";
            s[L10nKeys.Bucket_Age_Senior]              = "Senior";
            s[L10nKeys.Bucket_Edu_Uneducated]          = "Uneducated";
            s[L10nKeys.Bucket_Edu_Educated]            = "Educated";
            s[L10nKeys.Bucket_Edu_WellEducated]        = "Well-educated";
            s[L10nKeys.Bucket_Edu_HighlyEducated]      = "Highly-educated";
            s[L10nKeys.Bucket_Wealth_Low]              = "Low wealth";
            s[L10nKeys.Bucket_Wealth_Medium]           = "Medium wealth";
            s[L10nKeys.Bucket_Wealth_High]             = "High wealth";

            // Opinion Polling
            s[L10nKeys.Polling_Title]         = "Opinion Polling";
            s[L10nKeys.Polling_NoHistory]     = "No polling data yet - samples are collected each in-game day.";
            s[L10nKeys.Polling_Subtitle]      = "Daily opinion poll - sample size {0} - showing last {1} day(s)";
            s[L10nKeys.Polling_Axis_Today]    = "today";
            s[L10nKeys.Polling_Axis_DaysAgo]  = "-{0}d";

            // Info-view button
            s[L10nKeys.InfoButton_Tooltip]     = "Politics info view: cycle Party / Turnout / Satisfaction";
            s[L10nKeys.InfoButton_DragTooltip] = "Drag to move";

            return new Language("en", "English", s);
        }
    }
}
