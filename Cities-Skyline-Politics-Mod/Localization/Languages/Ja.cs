using System.Collections.Generic;

namespace PoliticsMod.Localization.Languages
{
    // Japanese (ja)
    public static class Ja
    {
        public static Language Build()
        {
            var s = new Dictionary<string, string>();

            // Mod metadata
            s[L10nKeys.Mod_Name]        = "政治・選挙MOD";
            s[L10nKeys.Mod_Description] = "市民が議会を選び、連立政権が都市政策を決定します。Ctrl+P でパネルを開きます。";

            // Settings
            s[L10nKeys.Settings_Group_Main]         = "政治・選挙";
            s[L10nKeys.Settings_EnableDebugLogging] = "デバッグログを有効にする";
            s[L10nKeys.Settings_Group_Hotkey]       = "パネル切替ホットキー";
            s[L10nKeys.Settings_Hotkey]             = "ホットキー";
            s[L10nKeys.Settings_RequireCtrl]        = "Ctrl キーを併用する";
            s[L10nKeys.Settings_Group_Utilities]    = "ユーティリティ";
            s[L10nKeys.Settings_OpenElectionsPanel] = "選挙パネルを開く";
            s[L10nKeys.Settings_Language]           = "言語";
            s[L10nKeys.Settings_Language_Auto]      = "自動(ゲーム言語に合わせる)";

            // Common
            s[L10nKeys.Common_CloseX] = "X";

            // Main panel
            s[L10nKeys.Panel_Title]                  = "政治・選挙";
            s[L10nKeys.Panel_Hemi_Tooltip]           = "議席数は人口に応じて増減します:\n市民 {0} 人あたり 1 議席(最小 {1}、最大 {2})。";
            s[L10nKeys.Panel_ActivePolicies]         = "施行中の政策:";
            s[L10nKeys.Panel_ActivePolicies_None]    = "(なし)";
            s[L10nKeys.Panel_Overlay_Prefix]         = "オーバーレイ: {0}";
            s[L10nKeys.Panel_Button_CallSnapElection]= "解散・総選挙";
            s[L10nKeys.Panel_Button_ManageParties]   = "政党管理";
            s[L10nKeys.Panel_Button_VoterTraits]     = "有権者特性";
            s[L10nKeys.Panel_Button_ElectionStats]   = "選挙統計";
            s[L10nKeys.Panel_Button_OpinionPolling]  = "世論調査";
            s[L10nKeys.Panel_MinimizeChirps]         = "つぶやきを最小化";
            s[L10nKeys.Panel_MinimizeChirps_Tooltip] = "重要なつぶやきのみ投稿します:選挙戦開始・選挙結果・法案可決。";
            s[L10nKeys.Panel_ElectionTimings]        = "選挙スケジュール(変更可)";
            s[L10nKeys.Panel_Slider_TermLength]      = "任期";
            s[L10nKeys.Panel_Slider_CampaignLength]  = "選挙戦期間";
            s[L10nKeys.Panel_Slider_ReElectionCooldown] = "再選挙までの待機日数";
            s[L10nKeys.Panel_Slider_ReElectionCooldown_Tooltip] =
                "連立交渉が失敗した後、自動的に再選挙が行われるまで\n" +
                "議会が停滞する日数(ゲーム内日数)です。\n" +
                "\n" +
                "連立可能パートナー数の上限内でどの組み合わせも過半数を\n" +
                "確保できない場合のみ発動します。0 で即時再挑戦、\n" +
                "大きくすると政治的膠着を長く演出できます。\n" +
                "連立が成立した通常選挙には影響しません。";
            s[L10nKeys.Panel_Days]                   = "{0} 日";
            s[L10nKeys.Panel_Phase_Campaign]         = "フェーズ: {0} | 選挙戦 {1}/{2} 日目";
            s[L10nKeys.Panel_Phase_Term]             = "フェーズ: {0} | 任期 {1}/{2} 日目";
            s[L10nKeys.Phase_Idle]                   = "待機中";
            s[L10nKeys.Phase_Campaign]               = "選挙戦中";
            s[L10nKeys.Phase_Voting]                 = "投票中";
            s[L10nKeys.Phase_Forming]                = "連立交渉中";
            s[L10nKeys.Phase_Governing]              = "政権運営中";
            s[L10nKeys.Phase_Failed]                 = "連立失敗";
            s[L10nKeys.Panel_Coalition_Header]       = "連立: {0}  ({1}/{2})";
            s[L10nKeys.Panel_Coalition_None]         = "連立: (なし)";
            s[L10nKeys.Panel_Policies_More]          = "+{0}";

            // Overlay
            s[L10nKeys.Overlay_Off]          = "オフ";
            s[L10nKeys.Overlay_Party]        = "政党";
            s[L10nKeys.Overlay_Turnout]      = "投票率";
            s[L10nKeys.Overlay_Satisfaction] = "満足度";
            s[L10nKeys.InfoButton_Prefix]    = "政治: {0}";

            // Party editor
            s[L10nKeys.PartyEditor_Title]               = "政党管理";
            s[L10nKeys.PartyEditor_Add]                 = "+ 政党を追加";
            s[L10nKeys.PartyEditor_Remove]              = "– 削除";
            s[L10nKeys.PartyEditor_ShortName]           = "略称";
            s[L10nKeys.PartyEditor_FullName]            = "正式名称";
            s[L10nKeys.PartyEditor_Color]               = "色";
            s[L10nKeys.PartyEditor_IdeologyHeader]      = "イデオロギー (-1 … +1)";
            s[L10nKeys.PartyEditor_Ideology_Economic]   = "経済(左↔右)";
            s[L10nKeys.PartyEditor_Ideology_Social]     = "社会(進歩↔伝統)";
            s[L10nKeys.PartyEditor_Ideology_Governance] = "統治(自由↔権威)";
            s[L10nKeys.PartyEditor_PoliciesHeader]      = "政策(クリックで切替:中立 → 支持 → 反対)";
            s[L10nKeys.PartyEditor_Stance_Support]      = "{0}\n支持: 当選時に施行";
            s[L10nKeys.PartyEditor_Stance_Oppose]       = "{0}\n反対: 当選時に撤廃";
            s[L10nKeys.PartyEditor_Stance_Neutral]      = "{0}\n中立: 当選しても変更しない";
            s[L10nKeys.PartyEditor_TaxHeader]           = "税率変更(pt、-10 … +10)";
            s[L10nKeys.PartyEditor_Tax_Res]             = "住宅";
            s[L10nKeys.PartyEditor_Tax_Com]             = "商業";
            s[L10nKeys.PartyEditor_Tax_Ind]             = "工業";
            s[L10nKeys.PartyEditor_Tax_Off]             = "オフィス";
            s[L10nKeys.PartyEditor_BudgetHeader]        = "予算変更(pt、-30 … +30)";
            s[L10nKeys.PartyEditor_Budget_Electricity]    = "電力";
            s[L10nKeys.PartyEditor_Budget_Water]          = "水道";
            s[L10nKeys.PartyEditor_Budget_Garbage]        = "ゴミ処理";
            s[L10nKeys.PartyEditor_Budget_Healthcare]     = "医療";
            s[L10nKeys.PartyEditor_Budget_Fire]           = "消防";
            s[L10nKeys.PartyEditor_Budget_Police]         = "警察";
            s[L10nKeys.PartyEditor_Budget_Education]      = "教育";
            s[L10nKeys.PartyEditor_Budget_Transport]      = "公共交通";
            s[L10nKeys.PartyEditor_Budget_Beautification] = "景観";
            s[L10nKeys.PartyEditor_Budget_Roads]          = "道路";
            s[L10nKeys.PartyEditor_Budget_Industry]       = "産業";
            s[L10nKeys.PartyEditor_NewPartyShortName]     = "新{0}";
            s[L10nKeys.PartyEditor_NewPartyFullName]      = "新政党 {0}";
            s[L10nKeys.PartyEditor_ResetAll]              = "既定値に戻す";

            // Voter Traits
            s[L10nKeys.VoterTraits_Title]                 = "有権者特性 — 経済軸バイアス(-1 左 … +1 右)";
            s[L10nKeys.VoterTraits_Note]                  =
                "有権者の経済軸上の傾向を微調整します。負の値 = 左寄り\n" +
                "(増税・公共サービス重視)。正の値 = 右寄り(減税・企業寄り)。\n" +
                "投票するのは青年・成人・高齢の市民のみです。";
            s[L10nKeys.VoterTraits_Section_Education]     = "学歴";
            s[L10nKeys.VoterTraits_Edu_Uneducated]        = "未就学";
            s[L10nKeys.VoterTraits_Edu_Educated]          = "学歴あり";
            s[L10nKeys.VoterTraits_Edu_WellEducated]      = "高学歴";
            s[L10nKeys.VoterTraits_Edu_HighlyEducated]    = "最高学歴";
            s[L10nKeys.VoterTraits_Section_Wealth]        = "所得";
            s[L10nKeys.VoterTraits_Wealth_Low]            = "低所得";
            s[L10nKeys.VoterTraits_Wealth_Medium]         = "中所得";
            s[L10nKeys.VoterTraits_Wealth_High]           = "高所得";
            s[L10nKeys.VoterTraits_Section_Employment]    = "雇用";
            s[L10nKeys.VoterTraits_Employment_Employed]   = "就業中";
            s[L10nKeys.VoterTraits_Employment_Unemployed] = "失業中";
            s[L10nKeys.VoterTraits_Section_Age]           = "年齢層";
            s[L10nKeys.VoterTraits_Age_Young]             = "青年";
            s[L10nKeys.VoterTraits_Age_Adult]             = "成人";
            s[L10nKeys.VoterTraits_Age_Senior]            = "高齢";
            s[L10nKeys.VoterTraits_Section_Life]          = "生活状況";
            s[L10nKeys.VoterTraits_Life_Sick]             = "病気";
            s[L10nKeys.VoterTraits_Life_Pollution]        = "汚染地域に居住";
            s[L10nKeys.VoterTraits_Section_Deficit]       = "財政赤字への敏感度";
            s[L10nKeys.VoterTraits_Deficit_Label]         = "赤字プレッシャー係数";
            s[L10nKeys.VoterTraits_Deficit_Tooltip]       =
                "財政赤字が続くことで有権者を経済右派へ押す強さ。\n" +
                "  0 = 機能オフ。赤字は投票に影響しません。\n" +
                "  1 = デフォルト。約 6 週連続の赤字で最大 +0.35 の\n" +
                "      右寄り補正が発生します。\n" +
                "  2 = 感度2倍。3 = 最大。\n" +
                "各有権者の特性バイアスに上乗せされます。";
            s[L10nKeys.VoterTraits_Section_Incumbency]    = "現職優位";
            s[L10nKeys.VoterTraits_Incumbency_Label]      = "現職ボーナス";
            s[L10nKeys.VoterTraits_Incumbency_Tooltip]    =
                "幸福な有権者が、イデオロギーや不満ではなく現連立に\n" +
                "投票する確率。幸福度 60 以上の有権者のみに適用されます。\n" +
                "\n" +
                "  0.00 = 現職の優位なし。常に適合度で判断。\n" +
                "  0.10 = デフォルト。幸福層の10人に1人が与党寄りに。\n" +
                "  0.25 = 幸福層の1/4が自動で与党に投票。\n" +
                "  0.50 = 幸福層の半数が自動で与党に投票。都市が\n" +
                "         繁栄している間、現職が強い優位に立ちます。\n" +
                "\n" +
                "ヒント: 毎回競った選挙にしたいなら低め(≤0.05)、\n" +
                "打倒しにくい現職を演出したいなら高め(≥0.25)に。";
            s[L10nKeys.VoterTraits_Reset]                 = "既定値に戻す";

            // Election Stats
            s[L10nKeys.Stats_Title]             = "選挙統計";
            s[L10nKeys.Stats_NoData_Subtitle]   = "選挙データがまだありません。";
            s[L10nKeys.Stats_NoData_Body]       = "選挙統計データが見つかりませんでした。";
            s[L10nKeys.Stats_Subtitle]          = "{0}年{1}月の選挙 — サンプル {2} 票  •  投票率 {3}%";
            s[L10nKeys.Stats_PartyColors]       = "政党カラー";
            s[L10nKeys.Stats_WhyPeopleVoted]    = "投票理由";
            s[L10nKeys.Stats_Grievance_Ideology]       = "純粋にイデオロギー";
            s[L10nKeys.Stats_Grievance_HighTaxes]      = "重税";
            s[L10nKeys.Stats_Grievance_PoorHealth]     = "健康問題";
            s[L10nKeys.Stats_Grievance_HighCrime]      = "治安悪化";
            s[L10nKeys.Stats_Grievance_PoorEducation]  = "教育不足";
            s[L10nKeys.Stats_Grievance_Unemployment]   = "失業";
            s[L10nKeys.Stats_Grievance_Pollution]      = "汚染";
            s[L10nKeys.Stats_Grievance_LowLandValue]   = "地価低迷";
            s[L10nKeys.Stats_Grievance_NoiseTrash]     = "騒音 / ゴミ";
            s[L10nKeys.Stats_Chart_ByAge]              = "年齢層別の投票";
            s[L10nKeys.Stats_Chart_ByEducation]        = "学歴別の投票";
            s[L10nKeys.Stats_Chart_ByWealth]           = "所得別の投票";
            s[L10nKeys.Stats_Chart_NoData]             = "(データなし — 旧セーブ形式)";
            s[L10nKeys.Stats_Votes_Suffix]             = "{0} 票";
            s[L10nKeys.Stats_PctCountFormat]           = "{0:P1}  ({1})";
            s[L10nKeys.Bucket_Age_Young]               = "青年";
            s[L10nKeys.Bucket_Age_Adult]               = "成人";
            s[L10nKeys.Bucket_Age_Senior]              = "高齢";
            s[L10nKeys.Bucket_Edu_Uneducated]          = "未就学";
            s[L10nKeys.Bucket_Edu_Educated]            = "学歴あり";
            s[L10nKeys.Bucket_Edu_WellEducated]        = "高学歴";
            s[L10nKeys.Bucket_Edu_HighlyEducated]      = "最高学歴";
            s[L10nKeys.Bucket_Wealth_Low]              = "低所得";
            s[L10nKeys.Bucket_Wealth_Medium]           = "中所得";
            s[L10nKeys.Bucket_Wealth_High]             = "高所得";

            // Opinion Polling
            s[L10nKeys.Polling_Title]         = "世論調査";
            s[L10nKeys.Polling_NoHistory]     = "世論調査データがまだありません — ゲーム内の毎日サンプルします。";
            s[L10nKeys.Polling_Subtitle]      = "毎日の世論調査 — サンプル数 {0} — 直近 {1} 日間を表示";
            s[L10nKeys.Polling_Axis_Today]    = "今日";
            s[L10nKeys.Polling_Axis_DaysAgo]  = "-{0}日";

            // Info-view button
            s[L10nKeys.InfoButton_Tooltip]     = "政治インフォビュー: 政党 / 投票率 / 満足度 を切替";
            s[L10nKeys.InfoButton_DragTooltip] = "ドラッグで移動";

            return new Language("ja", "日本語", s);
        }
    }
}
