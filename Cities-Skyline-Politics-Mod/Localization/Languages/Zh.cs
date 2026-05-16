using System.Collections.Generic;

namespace PoliticsMod.Localization.Languages
{
    // Simplified Chinese (zh)
    public static class Zh
    {
        public static Language Build()
        {
            var s = new Dictionary<string, string>();

            // Mod metadata
            s[L10nKeys.Mod_Name]        = "政治与选举模组";
            s[L10nKeys.Mod_Description] = "市民选出议会,执政联盟决定城市政策。按 Ctrl+P 打开面板。";

            // Settings
            s[L10nKeys.Settings_Group_Main]         = "政治与选举";
            s[L10nKeys.Settings_EnableDebugLogging] = "启用调试日志";
            s[L10nKeys.Settings_Group_Hotkey]       = "面板切换快捷键";
            s[L10nKeys.Settings_Hotkey]             = "快捷键";
            s[L10nKeys.Settings_RequireCtrl]        = "需要按住 Ctrl";
            s[L10nKeys.Settings_Group_Utilities]    = "实用功能";
            s[L10nKeys.Settings_OpenElectionsPanel] = "打开选举面板";
            s[L10nKeys.Settings_Language]           = "语言";
            s[L10nKeys.Settings_Language_Auto]      = "自动(跟随游戏语言)";

            // Common
            s[L10nKeys.Common_CloseX] = "X";

            // Main panel
            s[L10nKeys.Panel_Title]                  = "政治与选举";
            s[L10nKeys.Panel_Hemi_Tooltip]           = "议会席位数随人口增长:\n每 {0} 名市民 1 席(最少 {1} 席,最多 {2} 席)。";
            s[L10nKeys.Panel_ActivePolicies]         = "生效中的政策:";
            s[L10nKeys.Panel_ActivePolicies_None]    = "(无)";
            s[L10nKeys.Panel_Overlay_Prefix]         = "图层:{0}";
            s[L10nKeys.Panel_Button_CallSnapElection]= "提前举行选举";
            s[L10nKeys.Panel_Button_ManageParties]   = "管理政党";
            s[L10nKeys.Panel_Button_VoterTraits]     = "选民特征";
            s[L10nKeys.Panel_Button_ElectionStats]   = "选举统计";
            s[L10nKeys.Panel_Button_OpinionPolling]  = "民意调查";
            s[L10nKeys.Panel_MinimizeChirps]         = "精简推文";
            s[L10nKeys.Panel_MinimizeChirps_Tooltip] = "只发布关键推文:竞选开始、选举结果和法案通过。";
            s[L10nKeys.Panel_ElectionTimings]        = "选举节奏(可调整)";
            s[L10nKeys.Panel_Slider_TermLength]      = "任期长度";
            s[L10nKeys.Panel_Slider_CampaignLength]  = "竞选期长度";
            s[L10nKeys.Panel_Slider_ReElectionCooldown] = "重新选举冷却期";
            s[L10nKeys.Panel_Slider_ReElectionCooldown_Tooltip] =
                "当组阁失败时,议会在此冷却期(游戏内天数)后自动\n" +
                "召开临时选举。\n" +
                "\n" +
                "仅当在允许的联盟伙伴上限内没有任何政党组合能达到\n" +
                "多数席位时触发。设为 0 表示立即重选,设大一些则模拟\n" +
                "更长的政治僵局。对正常组阁成功的选举没有影响。";
            s[L10nKeys.Panel_Days]                   = "{0} 天";
            s[L10nKeys.Panel_Phase_Campaign]         = "阶段:{0} | 竞选期第 {1}/{2} 天";
            s[L10nKeys.Panel_Phase_Term]             = "阶段:{0} | 任期第 {1}/{2} 天";
            s[L10nKeys.Phase_Idle]                   = "空闲";
            s[L10nKeys.Phase_Campaign]               = "竞选中";
            s[L10nKeys.Phase_Voting]                 = "投票中";
            s[L10nKeys.Phase_Forming]                = "组阁中";
            s[L10nKeys.Phase_Governing]              = "执政中";
            s[L10nKeys.Phase_Failed]                 = "组阁失败";
            s[L10nKeys.Panel_Coalition_Header]       = "执政联盟:{0}  ({1}/{2})";
            s[L10nKeys.Panel_Coalition_None]         = "执政联盟:(无)";
            s[L10nKeys.Panel_Policies_More]          = "+{0}";

            // Overlay
            s[L10nKeys.Overlay_Off]          = "关闭";
            s[L10nKeys.Overlay_Party]        = "政党";
            s[L10nKeys.Overlay_Turnout]      = "投票率";
            s[L10nKeys.Overlay_Satisfaction] = "满意度";
            s[L10nKeys.InfoButton_Prefix]    = "政治:{0}";

            // Party editor
            s[L10nKeys.PartyEditor_Title]               = "管理政党";
            s[L10nKeys.PartyEditor_Add]                 = "+ 新增政党";
            s[L10nKeys.PartyEditor_Remove]              = "– 删除";
            s[L10nKeys.PartyEditor_ShortName]           = "简称";
            s[L10nKeys.PartyEditor_FullName]            = "全称";
            s[L10nKeys.PartyEditor_Color]               = "颜色";
            s[L10nKeys.PartyEditor_IdeologyHeader]      = "意识形态(-1 … +1)";
            s[L10nKeys.PartyEditor_Ideology_Economic]   = "经济(左↔右)";
            s[L10nKeys.PartyEditor_Ideology_Social]     = "社会(进步↔传统)";
            s[L10nKeys.PartyEditor_Ideology_Governance] = "治理(自由↔权威)";
            s[L10nKeys.PartyEditor_PoliciesHeader]      = "政策(点击切换:中立 → 支持 → 反对)";
            s[L10nKeys.PartyEditor_Stance_Support]      = "{0}\n支持:当选后将推行";
            s[L10nKeys.PartyEditor_Stance_Oppose]       = "{0}\n反对:当选后将废除";
            s[L10nKeys.PartyEditor_Stance_Neutral]      = "{0}\n中立:当选后不作改动";
            s[L10nKeys.PartyEditor_TaxHeader]           = "税率调整(百分点,-10 … +10)";
            s[L10nKeys.PartyEditor_Tax_Res]             = "住宅";
            s[L10nKeys.PartyEditor_Tax_Com]             = "商业";
            s[L10nKeys.PartyEditor_Tax_Ind]             = "工业";
            s[L10nKeys.PartyEditor_Tax_Off]             = "办公";
            s[L10nKeys.PartyEditor_BudgetHeader]        = "预算调整(百分点,-30 … +30)";
            s[L10nKeys.PartyEditor_Budget_Electricity]    = "电力";
            s[L10nKeys.PartyEditor_Budget_Water]          = "供水";
            s[L10nKeys.PartyEditor_Budget_Garbage]        = "垃圾处理";
            s[L10nKeys.PartyEditor_Budget_Healthcare]     = "医疗";
            s[L10nKeys.PartyEditor_Budget_Fire]           = "消防";
            s[L10nKeys.PartyEditor_Budget_Police]         = "警察";
            s[L10nKeys.PartyEditor_Budget_Education]      = "教育";
            s[L10nKeys.PartyEditor_Budget_Transport]      = "公共交通";
            s[L10nKeys.PartyEditor_Budget_Beautification] = "美化";
            s[L10nKeys.PartyEditor_Budget_Roads]          = "道路";
            s[L10nKeys.PartyEditor_Budget_Industry]       = "工业区";
            s[L10nKeys.PartyEditor_NewPartyShortName]     = "新{0}";
            s[L10nKeys.PartyEditor_NewPartyFullName]      = "新政党 {0}";
            s[L10nKeys.PartyEditor_ResetAll]              = "恢复默认";

            // Voter Traits
            s[L10nKeys.VoterTraits_Title]                 = "选民特征 — 经济轴倾向(-1 左 … +1 右)";
            s[L10nKeys.VoterTraits_Note]                  =
                "在经济轴上微调选民倾向。负值 = 偏左(高税、更强公共服务)。\n" +
                "正值 = 偏右(低税、亲商)。只有青年、成年与老年市民会投票。";
            s[L10nKeys.VoterTraits_Section_Education]     = "学历";
            s[L10nKeys.VoterTraits_Edu_Uneducated]        = "未受教育";
            s[L10nKeys.VoterTraits_Edu_Educated]          = "已受教育";
            s[L10nKeys.VoterTraits_Edu_WellEducated]      = "高学历";
            s[L10nKeys.VoterTraits_Edu_HighlyEducated]    = "顶尖学历";
            s[L10nKeys.VoterTraits_Section_Wealth]        = "财富";
            s[L10nKeys.VoterTraits_Wealth_Low]            = "低收入";
            s[L10nKeys.VoterTraits_Wealth_Medium]         = "中等收入";
            s[L10nKeys.VoterTraits_Wealth_High]           = "高收入";
            s[L10nKeys.VoterTraits_Section_Employment]    = "就业";
            s[L10nKeys.VoterTraits_Employment_Employed]   = "就业";
            s[L10nKeys.VoterTraits_Employment_Unemployed] = "失业";
            s[L10nKeys.VoterTraits_Section_Age]           = "年龄";
            s[L10nKeys.VoterTraits_Age_Young]             = "青年";
            s[L10nKeys.VoterTraits_Age_Adult]             = "成年";
            s[L10nKeys.VoterTraits_Age_Senior]            = "老年";
            s[L10nKeys.VoterTraits_Section_Life]          = "生活状况";
            s[L10nKeys.VoterTraits_Life_Sick]             = "患病";
            s[L10nKeys.VoterTraits_Life_Pollution]        = "居住在污染区";
            s[L10nKeys.VoterTraits_Section_Deficit]       = "财政赤字敏感度";
            s[L10nKeys.VoterTraits_Deficit_Label]         = "赤字压力系数";
            s[L10nKeys.VoterTraits_Deficit_Tooltip]       =
                "持续的财政赤字把选民推向经济右翼的强度。\n" +
                "  0 = 关闭此功能,赤字不影响投票。\n" +
                "  1 = 默认曲线:连续约 6 个赤字周后,最多产生\n" +
                "      +0.35 的右倾偏移。\n" +
                "  2 = 敏感度翻倍。3 = 最高。\n" +
                "效果叠加在每位选民自身的特征偏向之上。";
            s[L10nKeys.VoterTraits_Section_Incumbency]    = "现任优势";
            s[L10nKeys.VoterTraits_Incumbency_Label]      = "现任加分";
            s[L10nKeys.VoterTraits_Incumbency_Tooltip]    =
                "快乐选民直接投给现任执政联盟(而非按意识形态/诉求\n" +
                "投票)的概率。仅适用于幸福度 ≥ 60 的选民。\n" +
                "\n" +
                "  0.00 = 现任无加成,选民只按契合度投票。\n" +
                "  0.10 = 默认值,每 10 名快乐选民有 1 人倒向政府。\n" +
                "  0.25 = 四分之一快乐选民自动支持现任政府。\n" +
                "  0.50 = 一半快乐选民自动支持现任;城市繁荣时\n" +
                "         现任将具有强大的连任优势。\n" +
                "\n" +
                "提示:若想让每次选举都是开放竞争,调低(≤0.05);\n" +
                "若想模拟难以撼动的在任者,调高(≥0.25)。";
            s[L10nKeys.VoterTraits_Reset]                 = "恢复默认";

            // Election Stats
            s[L10nKeys.Stats_Title]             = "选举统计";
            s[L10nKeys.Stats_NoData_Subtitle]   = "暂无选举数据。";
            s[L10nKeys.Stats_NoData_Body]       = "未找到选举统计数据。";
            s[L10nKeys.Stats_Subtitle]          = "{0} 年 {1} 月选举 — 抽样 {2} 票  •  投票率 {3}%";
            s[L10nKeys.Stats_PartyColors]       = "政党颜色";
            s[L10nKeys.Stats_WhyPeopleVoted]    = "投票动机";
            s[L10nKeys.Stats_Grievance_Ideology]       = "纯粹意识形态";
            s[L10nKeys.Stats_Grievance_HighTaxes]      = "税负过重";
            s[L10nKeys.Stats_Grievance_PoorHealth]     = "健康不佳";
            s[L10nKeys.Stats_Grievance_HighCrime]      = "治安差";
            s[L10nKeys.Stats_Grievance_PoorEducation]  = "教育不足";
            s[L10nKeys.Stats_Grievance_Unemployment]   = "失业";
            s[L10nKeys.Stats_Grievance_Pollution]      = "污染";
            s[L10nKeys.Stats_Grievance_LowLandValue]   = "地价低迷";
            s[L10nKeys.Stats_Grievance_NoiseTrash]     = "噪音 / 垃圾";
            s[L10nKeys.Stats_Chart_ByAge]              = "按年龄段的投票分布";
            s[L10nKeys.Stats_Chart_ByEducation]        = "按学历的投票分布";
            s[L10nKeys.Stats_Chart_ByWealth]           = "按收入的投票分布";
            s[L10nKeys.Stats_Chart_NoData]             = "(无数据 — 旧存档格式)";
            s[L10nKeys.Stats_Votes_Suffix]             = "{0} 票";
            s[L10nKeys.Stats_PctCountFormat]           = "{0:P1}  ({1})";
            s[L10nKeys.Bucket_Age_Young]               = "青年";
            s[L10nKeys.Bucket_Age_Adult]               = "成年";
            s[L10nKeys.Bucket_Age_Senior]              = "老年";
            s[L10nKeys.Bucket_Edu_Uneducated]          = "未受教育";
            s[L10nKeys.Bucket_Edu_Educated]            = "已受教育";
            s[L10nKeys.Bucket_Edu_WellEducated]        = "高学历";
            s[L10nKeys.Bucket_Edu_HighlyEducated]      = "顶尖学历";
            s[L10nKeys.Bucket_Wealth_Low]              = "低收入";
            s[L10nKeys.Bucket_Wealth_Medium]           = "中等收入";
            s[L10nKeys.Bucket_Wealth_High]             = "高收入";

            // Opinion Polling
            s[L10nKeys.Polling_Title]         = "民意调查";
            s[L10nKeys.Polling_NoHistory]     = "暂无民调数据 — 每个游戏日采样一次。";
            s[L10nKeys.Polling_Subtitle]      = "每日民意调查 — 样本量 {0} — 显示最近 {1} 天";
            s[L10nKeys.Polling_Axis_Today]    = "今天";
            s[L10nKeys.Polling_Axis_DaysAgo]  = "-{0}天";

            // Info-view button
            s[L10nKeys.InfoButton_Tooltip]     = "政治信息视图:切换 政党 / 投票率 / 满意度";
            s[L10nKeys.InfoButton_DragTooltip] = "拖动以移动";

            return new Language("zh", "简体中文", s);
        }
    }
}
