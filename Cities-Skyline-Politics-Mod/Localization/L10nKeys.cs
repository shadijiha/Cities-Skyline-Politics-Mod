namespace PoliticsMod.Localization
{
    // String keys. Call sites use L10n.T(L10nKeys.Xxx).
    // Every key here must have a value in Languages/En.cs.
    public static class L10nKeys
    {
        // Mod metadata
        public const string Mod_Name        = "Mod_Name";
        public const string Mod_Description = "Mod_Description";

        // Settings UI
        public const string Settings_Group_Main            = "Settings_Group_Main";
        public const string Settings_EnableDebugLogging    = "Settings_EnableDebugLogging";
        public const string Settings_Group_Hotkey          = "Settings_Group_Hotkey";
        public const string Settings_Hotkey                = "Settings_Hotkey";
        public const string Settings_RequireCtrl           = "Settings_RequireCtrl";
        public const string Settings_Group_Utilities       = "Settings_Group_Utilities";
        public const string Settings_OpenElectionsPanel    = "Settings_OpenElectionsPanel";
        public const string Settings_Language              = "Settings_Language";
        public const string Settings_Language_Auto         = "Settings_Language_Auto";

        // Common
        public const string Common_CloseX   = "Common_CloseX";

        // Main Politics panel
        public const string Panel_Title                       = "Panel_Title";
        public const string Panel_Hemi_Tooltip                = "Panel_Hemi_Tooltip";
        public const string Panel_ActivePolicies              = "Panel_ActivePolicies";
        public const string Panel_ActivePolicies_None         = "Panel_ActivePolicies_None";
        public const string Panel_Overlay_Prefix              = "Panel_Overlay_Prefix";
        public const string Panel_Button_CallSnapElection     = "Panel_Button_CallSnapElection";
        public const string Panel_Button_ManageParties        = "Panel_Button_ManageParties";
        public const string Panel_Button_VoterTraits          = "Panel_Button_VoterTraits";
        public const string Panel_Button_ElectionStats        = "Panel_Button_ElectionStats";
        public const string Panel_Button_OpinionPolling       = "Panel_Button_OpinionPolling";
        public const string Panel_MinimizeChirps              = "Panel_MinimizeChirps";
        public const string Panel_MinimizeChirps_Tooltip      = "Panel_MinimizeChirps_Tooltip";
        public const string Panel_ElectionTimings             = "Panel_ElectionTimings";
        public const string Panel_Slider_TermLength           = "Panel_Slider_TermLength";
        public const string Panel_Slider_CampaignLength       = "Panel_Slider_CampaignLength";
        public const string Panel_Slider_ReElectionCooldown   = "Panel_Slider_ReElectionCooldown";
        public const string Panel_Slider_ReElectionCooldown_Tooltip = "Panel_Slider_ReElectionCooldown_Tooltip";
        public const string Panel_Days                        = "Panel_Days";
        public const string Panel_Phase_Campaign              = "Panel_Phase_Campaign";
        public const string Panel_Phase_Term                  = "Panel_Phase_Term";
        public const string Panel_Coalition_Header            = "Panel_Coalition_Header";
        public const string Panel_Coalition_None              = "Panel_Coalition_None";
        public const string Panel_Policies_More               = "Panel_Policies_More";

        // Overlay labels (also used by PoliticsInfoViewButton)
        public const string Overlay_Off                       = "Overlay_Off";
        public const string Overlay_Party                     = "Overlay_Party";
        public const string Overlay_Turnout                   = "Overlay_Turnout";
        public const string Overlay_Satisfaction              = "Overlay_Satisfaction";

        // Info-view button prefix ("Politics: X") - distinct from the main
        // panel's "Overlay: X" prefix.
        public const string InfoButton_Prefix                 = "InfoButton_Prefix";

        // Party editor
        public const string PartyEditor_Title                 = "PartyEditor_Title";
        public const string PartyEditor_Add                   = "PartyEditor_Add";
        public const string PartyEditor_Remove                = "PartyEditor_Remove";
        public const string PartyEditor_ShortName             = "PartyEditor_ShortName";
        public const string PartyEditor_FullName              = "PartyEditor_FullName";
        public const string PartyEditor_Color                 = "PartyEditor_Color";
        public const string PartyEditor_IdeologyHeader        = "PartyEditor_IdeologyHeader";
        public const string PartyEditor_Ideology_Economic     = "PartyEditor_Ideology_Economic";
        public const string PartyEditor_Ideology_Social       = "PartyEditor_Ideology_Social";
        public const string PartyEditor_Ideology_Governance   = "PartyEditor_Ideology_Governance";
        public const string PartyEditor_PoliciesHeader        = "PartyEditor_PoliciesHeader";
        public const string PartyEditor_Stance_Support        = "PartyEditor_Stance_Support";
        public const string PartyEditor_Stance_Oppose         = "PartyEditor_Stance_Oppose";
        public const string PartyEditor_Stance_Neutral        = "PartyEditor_Stance_Neutral";
        public const string PartyEditor_TaxHeader             = "PartyEditor_TaxHeader";
        public const string PartyEditor_Tax_Res               = "PartyEditor_Tax_Res";
        public const string PartyEditor_Tax_Com               = "PartyEditor_Tax_Com";
        public const string PartyEditor_Tax_Ind               = "PartyEditor_Tax_Ind";
        public const string PartyEditor_Tax_Off               = "PartyEditor_Tax_Off";
        public const string PartyEditor_BudgetHeader          = "PartyEditor_BudgetHeader";
        public const string PartyEditor_Budget_Electricity    = "PartyEditor_Budget_Electricity";
        public const string PartyEditor_Budget_Water          = "PartyEditor_Budget_Water";
        public const string PartyEditor_Budget_Garbage        = "PartyEditor_Budget_Garbage";
        public const string PartyEditor_Budget_Healthcare     = "PartyEditor_Budget_Healthcare";
        public const string PartyEditor_Budget_Fire           = "PartyEditor_Budget_Fire";
        public const string PartyEditor_Budget_Police         = "PartyEditor_Budget_Police";
        public const string PartyEditor_Budget_Education      = "PartyEditor_Budget_Education";
        public const string PartyEditor_Budget_Transport      = "PartyEditor_Budget_Transport";
        public const string PartyEditor_Budget_Beautification = "PartyEditor_Budget_Beautification";
        public const string PartyEditor_Budget_Roads          = "PartyEditor_Budget_Roads";
        public const string PartyEditor_Budget_Industry       = "PartyEditor_Budget_Industry";
        public const string PartyEditor_NewPartyShortName     = "PartyEditor_NewPartyShortName";
        public const string PartyEditor_NewPartyFullName      = "PartyEditor_NewPartyFullName";

        // Voter Traits panel
        public const string VoterTraits_Title                 = "VoterTraits_Title";
        public const string VoterTraits_Note                  = "VoterTraits_Note";
        public const string VoterTraits_Section_Education     = "VoterTraits_Section_Education";
        public const string VoterTraits_Edu_Uneducated        = "VoterTraits_Edu_Uneducated";
        public const string VoterTraits_Edu_Educated          = "VoterTraits_Edu_Educated";
        public const string VoterTraits_Edu_WellEducated      = "VoterTraits_Edu_WellEducated";
        public const string VoterTraits_Edu_HighlyEducated    = "VoterTraits_Edu_HighlyEducated";
        public const string VoterTraits_Section_Wealth        = "VoterTraits_Section_Wealth";
        public const string VoterTraits_Wealth_Low            = "VoterTraits_Wealth_Low";
        public const string VoterTraits_Wealth_Medium         = "VoterTraits_Wealth_Medium";
        public const string VoterTraits_Wealth_High           = "VoterTraits_Wealth_High";
        public const string VoterTraits_Section_Employment    = "VoterTraits_Section_Employment";
        public const string VoterTraits_Employment_Employed   = "VoterTraits_Employment_Employed";
        public const string VoterTraits_Employment_Unemployed = "VoterTraits_Employment_Unemployed";
        public const string VoterTraits_Section_Age           = "VoterTraits_Section_Age";
        public const string VoterTraits_Age_Young             = "VoterTraits_Age_Young";
        public const string VoterTraits_Age_Adult             = "VoterTraits_Age_Adult";
        public const string VoterTraits_Age_Senior            = "VoterTraits_Age_Senior";
        public const string VoterTraits_Section_Life          = "VoterTraits_Section_Life";
        public const string VoterTraits_Life_Sick             = "VoterTraits_Life_Sick";
        public const string VoterTraits_Life_Pollution        = "VoterTraits_Life_Pollution";
        public const string VoterTraits_Section_Deficit       = "VoterTraits_Section_Deficit";
        public const string VoterTraits_Deficit_Label         = "VoterTraits_Deficit_Label";
        public const string VoterTraits_Deficit_Tooltip       = "VoterTraits_Deficit_Tooltip";
        public const string VoterTraits_Section_Incumbency    = "VoterTraits_Section_Incumbency";
        public const string VoterTraits_Incumbency_Label      = "VoterTraits_Incumbency_Label";
        public const string VoterTraits_Incumbency_Tooltip    = "VoterTraits_Incumbency_Tooltip";
        public const string VoterTraits_Reset                 = "VoterTraits_Reset";

        // Election Stats panel
        public const string Stats_Title                       = "Stats_Title";
        public const string Stats_NoData_Subtitle             = "Stats_NoData_Subtitle";
        public const string Stats_NoData_Body                 = "Stats_NoData_Body";
        public const string Stats_Subtitle                    = "Stats_Subtitle";
        public const string Stats_PartyColors                 = "Stats_PartyColors";
        public const string Stats_WhyPeopleVoted              = "Stats_WhyPeopleVoted";
        public const string Stats_Grievance_Ideology          = "Stats_Grievance_Ideology";
        public const string Stats_Grievance_HighTaxes         = "Stats_Grievance_HighTaxes";
        public const string Stats_Grievance_PoorHealth        = "Stats_Grievance_PoorHealth";
        public const string Stats_Grievance_HighCrime         = "Stats_Grievance_HighCrime";
        public const string Stats_Grievance_PoorEducation     = "Stats_Grievance_PoorEducation";
        public const string Stats_Grievance_Unemployment      = "Stats_Grievance_Unemployment";
        public const string Stats_Grievance_Pollution         = "Stats_Grievance_Pollution";
        public const string Stats_Grievance_LowLandValue      = "Stats_Grievance_LowLandValue";
        public const string Stats_Grievance_NoiseTrash        = "Stats_Grievance_NoiseTrash";
        public const string Stats_Chart_ByAge                 = "Stats_Chart_ByAge";
        public const string Stats_Chart_ByEducation           = "Stats_Chart_ByEducation";
        public const string Stats_Chart_ByWealth              = "Stats_Chart_ByWealth";
        public const string Stats_Chart_NoData                = "Stats_Chart_NoData";
        public const string Stats_Votes_Suffix                = "Stats_Votes_Suffix";
        public const string Stats_PctCountFormat              = "Stats_PctCountFormat";
        // Demographic bucket labels (shared with Stats chart)
        public const string Bucket_Age_Young                  = "Bucket_Age_Young";
        public const string Bucket_Age_Adult                  = "Bucket_Age_Adult";
        public const string Bucket_Age_Senior                 = "Bucket_Age_Senior";
        public const string Bucket_Edu_Uneducated             = "Bucket_Edu_Uneducated";
        public const string Bucket_Edu_Educated               = "Bucket_Edu_Educated";
        public const string Bucket_Edu_WellEducated           = "Bucket_Edu_WellEducated";
        public const string Bucket_Edu_HighlyEducated         = "Bucket_Edu_HighlyEducated";
        public const string Bucket_Wealth_Low                 = "Bucket_Wealth_Low";
        public const string Bucket_Wealth_Medium              = "Bucket_Wealth_Medium";
        public const string Bucket_Wealth_High                = "Bucket_Wealth_High";

        // Opinion Polling panel
        public const string Polling_Title                     = "Polling_Title";
        public const string Polling_NoHistory                 = "Polling_NoHistory";
        public const string Polling_Subtitle                  = "Polling_Subtitle";
        public const string Polling_Axis_Today                = "Polling_Axis_Today";
        public const string Polling_Axis_DaysAgo              = "Polling_Axis_DaysAgo";

        // Info-view button
        public const string InfoButton_Tooltip                = "InfoButton_Tooltip";
        public const string InfoButton_DragTooltip            = "InfoButton_DragTooltip";
    }
}
