using System.Collections.Generic;

namespace PoliticsMod.Localization.Languages
{
    // Russian (ru)
    public static class Ru
    {
        public static Language Build()
        {
            var s = new Dictionary<string, string>();

            // Mod metadata
            s[L10nKeys.Mod_Name]        = "Мод «Политика и выборы»";
            s[L10nKeys.Mod_Description] = "Жители выбирают парламент, коалиции задают городскую политику. Нажмите Ctrl+P, чтобы открыть панель.";

            // Settings
            s[L10nKeys.Settings_Group_Main]         = "Политика и выборы";
            s[L10nKeys.Settings_EnableDebugLogging] = "Включить отладочные логи";
            s[L10nKeys.Settings_Group_Hotkey]       = "Горячая клавиша панели";
            s[L10nKeys.Settings_Hotkey]             = "Клавиша";
            s[L10nKeys.Settings_RequireCtrl]        = "Требовать удержание Ctrl";
            s[L10nKeys.Settings_Group_Utilities]    = "Утилиты";
            s[L10nKeys.Settings_OpenElectionsPanel] = "Открыть панель выборов";
            s[L10nKeys.Settings_Language]           = "Язык";
            s[L10nKeys.Settings_Language_Auto]      = "Авто (язык игры)";

            // Common
            s[L10nKeys.Common_CloseX] = "X";

            // Main panel
            s[L10nKeys.Panel_Title]                  = "Политика и выборы";
            s[L10nKeys.Panel_Hemi_Tooltip]           = "Размер парламента растёт с населением:\n1 место на {0} жителей (минимум {1}, максимум {2}).";
            s[L10nKeys.Panel_ActivePolicies]         = "Действующие политики:";
            s[L10nKeys.Panel_ActivePolicies_None]    = "(нет)";
            s[L10nKeys.Panel_Overlay_Prefix]         = "Слой: {0}";
            s[L10nKeys.Panel_Button_CallSnapElection]= "Досрочные выборы";
            s[L10nKeys.Panel_Button_ManageParties]   = "Управление партиями";
            s[L10nKeys.Panel_Button_VoterTraits]     = "Характеристики избирателей";
            s[L10nKeys.Panel_Button_ElectionStats]   = "Статистика выборов";
            s[L10nKeys.Panel_Button_OpinionPolling]  = "Опросы общественного мнения";
            s[L10nKeys.Panel_MinimizeChirps]         = "Скрыть лишние чирпы";
            s[L10nKeys.Panel_MinimizeChirps_Tooltip] = "Публиковать только ключевые чирпы: начало кампании, результаты выборов и принятие законов.";
            s[L10nKeys.Panel_ElectionTimings]        = "Сроки выборов (редактируемые)";
            s[L10nKeys.Panel_Slider_TermLength]      = "Срок полномочий";
            s[L10nKeys.Panel_Slider_CampaignLength]  = "Длительность кампании";
            s[L10nKeys.Panel_Slider_ReElectionCooldown] = "Пауза перед перевыборами";
            s[L10nKeys.Panel_Slider_ReElectionCooldown_Tooltip] =
                "Игровых дней, на которые парламент замирает после ПРОВАЛА\n" +
                "коалиционных переговоров, прежде чем автоматически\n" +
                "назначить досрочные выборы.\n" +
                "\n" +
                "Срабатывает, только если никакая комбинация партий не\n" +
                "набирает большинство в пределах лимита партнёров. 0 —\n" +
                "повтор немедленно, большее значение — более долгий\n" +
                "политический тупик. На обычные выборы с успешной\n" +
                "коалицией не влияет.";
            s[L10nKeys.Panel_Days]                   = "{0} дн.";
            s[L10nKeys.Panel_Phase_Campaign]         = "Фаза: {0} | День {1}/{2} кампании";
            s[L10nKeys.Panel_Phase_Term]             = "Фаза: {0} | День {1}/{2} срока";
            s[L10nKeys.Phase_Idle]                   = "Ожидание";
            s[L10nKeys.Phase_Campaign]               = "Кампания";
            s[L10nKeys.Phase_Voting]                 = "Голосование";
            s[L10nKeys.Phase_Forming]                = "Формирование коалиции";
            s[L10nKeys.Phase_Governing]              = "У власти";
            s[L10nKeys.Phase_Failed]                 = "Коалиция не собрана";
            s[L10nKeys.Panel_Coalition_Header]       = "Коалиция: {0}  ({1}/{2})";
            s[L10nKeys.Panel_Coalition_None]         = "Коалиция: (нет)";
            s[L10nKeys.Panel_Policies_More]          = "+{0}";

            // Overlay
            s[L10nKeys.Overlay_Off]          = "Выкл";
            s[L10nKeys.Overlay_Party]        = "Партия";
            s[L10nKeys.Overlay_Turnout]      = "Явка";
            s[L10nKeys.Overlay_Satisfaction] = "Удовлетворённость";
            s[L10nKeys.InfoButton_Prefix]    = "Политика: {0}";

            // Party editor
            s[L10nKeys.PartyEditor_Title]               = "Управление партиями";
            s[L10nKeys.PartyEditor_Add]                 = "+ Добавить партию";
            s[L10nKeys.PartyEditor_Remove]              = "– Удалить";
            s[L10nKeys.PartyEditor_ShortName]           = "Сокращение";
            s[L10nKeys.PartyEditor_FullName]            = "Полное название";
            s[L10nKeys.PartyEditor_Color]               = "Цвет";
            s[L10nKeys.PartyEditor_IdeologyHeader]      = "Идеология (-1 … +1)";
            s[L10nKeys.PartyEditor_Ideology_Economic]   = "Экономика (лев.↔прав.)";
            s[L10nKeys.PartyEditor_Ideology_Social]     = "Общество (прогр.↔трад.)";
            s[L10nKeys.PartyEditor_Ideology_Governance] = "Власть (либер.↔автор.)";
            s[L10nKeys.PartyEditor_PoliciesHeader]      = "Политики (клик переключает: нейтр. → за → против)";
            s[L10nKeys.PartyEditor_Stance_Support]      = "{0}\nЗа: будет введена при приходе к власти";
            s[L10nKeys.PartyEditor_Stance_Oppose]       = "{0}\nПротив: будет отменена при приходе к власти";
            s[L10nKeys.PartyEditor_Stance_Neutral]      = "{0}\nНейтрально: при приходе к власти не трогают";
            s[L10nKeys.PartyEditor_TaxHeader]           = "Изменение налогов (п.п., -10 … +10)";
            s[L10nKeys.PartyEditor_Tax_Res]             = "Жилая";
            s[L10nKeys.PartyEditor_Tax_Com]             = "Коммерческая";
            s[L10nKeys.PartyEditor_Tax_Ind]             = "Промышленная";
            s[L10nKeys.PartyEditor_Tax_Off]             = "Офисы";
            s[L10nKeys.PartyEditor_BudgetHeader]        = "Изменение бюджета (п.п., -30 … +30)";
            s[L10nKeys.PartyEditor_Budget_Electricity]    = "Электричество";
            s[L10nKeys.PartyEditor_Budget_Water]          = "Вода";
            s[L10nKeys.PartyEditor_Budget_Garbage]        = "Мусор";
            s[L10nKeys.PartyEditor_Budget_Healthcare]     = "Медицина";
            s[L10nKeys.PartyEditor_Budget_Fire]           = "Пожарные";
            s[L10nKeys.PartyEditor_Budget_Police]         = "Полиция";
            s[L10nKeys.PartyEditor_Budget_Education]      = "Образование";
            s[L10nKeys.PartyEditor_Budget_Transport]      = "Транспорт";
            s[L10nKeys.PartyEditor_Budget_Beautification] = "Благоустройство";
            s[L10nKeys.PartyEditor_Budget_Roads]          = "Дороги";
            s[L10nKeys.PartyEditor_Budget_Industry]       = "Индустрия";
            s[L10nKeys.PartyEditor_NewPartyShortName]     = "НОВ{0}";
            s[L10nKeys.PartyEditor_NewPartyFullName]      = "Новая партия {0}";
            s[L10nKeys.PartyEditor_ResetAll]              = "Сбросить";

            // Voter Traits
            s[L10nKeys.VoterTraits_Title]                 = "Характеристики избирателей — сдвиг по экономической оси (-1 лев. … +1 прав.)";
            s[L10nKeys.VoterTraits_Note]                  =
                "Смещает избирателей по экономической оси. Отрицательные\n" +
                "значения = левее (выше налоги, сильнее соцуслуги).\n" +
                "Положительные = правее (ниже налоги, про-бизнес).\n" +
                "Голосуют только молодые, взрослые и пожилые жители.";
            s[L10nKeys.VoterTraits_Section_Education]     = "Образование";
            s[L10nKeys.VoterTraits_Edu_Uneducated]        = "Без образования";
            s[L10nKeys.VoterTraits_Edu_Educated]          = "С образованием";
            s[L10nKeys.VoterTraits_Edu_WellEducated]      = "Высокообразованные";
            s[L10nKeys.VoterTraits_Edu_HighlyEducated]    = "Элитное образование";
            s[L10nKeys.VoterTraits_Section_Wealth]        = "Доход";
            s[L10nKeys.VoterTraits_Wealth_Low]            = "Низкий доход";
            s[L10nKeys.VoterTraits_Wealth_Medium]         = "Средний доход";
            s[L10nKeys.VoterTraits_Wealth_High]           = "Высокий доход";
            s[L10nKeys.VoterTraits_Section_Employment]    = "Занятость";
            s[L10nKeys.VoterTraits_Employment_Employed]   = "Работают";
            s[L10nKeys.VoterTraits_Employment_Unemployed] = "Безработные";
            s[L10nKeys.VoterTraits_Section_Age]           = "Возраст";
            s[L10nKeys.VoterTraits_Age_Young]             = "Молодые";
            s[L10nKeys.VoterTraits_Age_Adult]             = "Взрослые";
            s[L10nKeys.VoterTraits_Age_Senior]            = "Пожилые";
            s[L10nKeys.VoterTraits_Section_Life]          = "Условия жизни";
            s[L10nKeys.VoterTraits_Life_Sick]             = "Болеют";
            s[L10nKeys.VoterTraits_Life_Pollution]        = "Живут в загрязнении";
            s[L10nKeys.VoterTraits_Section_Deficit]       = "Чувствительность к дефициту";
            s[L10nKeys.VoterTraits_Deficit_Label]         = "Множитель давления дефицита";
            s[L10nKeys.VoterTraits_Deficit_Tooltip]       =
                "Насколько сильно затяжной бюджетный дефицит толкает\n" +
                "избирателей к экономическому правому краю.\n" +
                "  0 = отключено; дефицит не влияет на голоса.\n" +
                "  1 = стандартная кривая: сдвиг вправо до +0,35 после\n" +
                "      примерно 6 недель подряд с дефицитом.\n" +
                "  2 = вдвое чувствительнее. 3 = максимум.\n" +
                "Накладывается поверх индивидуальных смещений.";
            s[L10nKeys.VoterTraits_Section_Incumbency]    = "Преимущество действующей власти";
            s[L10nKeys.VoterTraits_Incumbency_Label]      = "Бонус действующей власти";
            s[L10nKeys.VoterTraits_Incumbency_Tooltip]    =
                "Вероятность того, что ДОВОЛЬНЫЙ избиратель отдаст голос\n" +
                "действующей коалиции вместо того, чтобы голосовать\n" +
                "по идеологии или жалобам. Работает только для\n" +
                "избирателей со счастьем 60 и выше.\n" +
                "\n" +
                "  0,00 = нет эффекта; голосуют строго по совпадению.\n" +
                "  0,10 = по умолчанию. 1 из 10 довольных переходит к власти.\n" +
                "  0,25 = четверть довольных автоматически продлевает мандат.\n" +
                "  0,50 = половина довольных продлевает мандат;\n" +
                "         сильное преимущество при процветающем городе.\n" +
                "\n" +
                "Совет: поставьте низко (≤0,05), если хотите, чтобы\n" +
                "каждые выборы были открытыми, или высоко (≥0,25),\n" +
                "чтобы смоделировать непотопляемых инкумбентов.";
            s[L10nKeys.VoterTraits_Reset]                 = "Сбросить";

            // Election Stats
            s[L10nKeys.Stats_Title]             = "Статистика выборов";
            s[L10nKeys.Stats_NoData_Subtitle]   = "Данных о выборах ещё нет.";
            s[L10nKeys.Stats_NoData_Body]       = "Статистики выборов не найдено.";
            s[L10nKeys.Stats_Subtitle]          = "Выборы {0}-{1} — выборка {2} голосов  •  Явка {3}%";
            s[L10nKeys.Stats_PartyColors]       = "Цвета партий";
            s[L10nKeys.Stats_WhyPeopleVoted]    = "Почему голосовали";
            s[L10nKeys.Stats_Grievance_Ideology]       = "Чистая идеология";
            s[L10nKeys.Stats_Grievance_HighTaxes]      = "Высокие налоги";
            s[L10nKeys.Stats_Grievance_PoorHealth]     = "Плохое здоровье";
            s[L10nKeys.Stats_Grievance_HighCrime]      = "Высокая преступность";
            s[L10nKeys.Stats_Grievance_PoorEducation]  = "Низкое образование";
            s[L10nKeys.Stats_Grievance_Unemployment]   = "Безработица";
            s[L10nKeys.Stats_Grievance_Pollution]      = "Загрязнение";
            s[L10nKeys.Stats_Grievance_LowLandValue]   = "Низкая стоимость земли";
            s[L10nKeys.Stats_Grievance_NoiseTrash]     = "Шум / мусор";
            s[L10nKeys.Stats_Chart_ByAge]              = "Голоса по возрасту";
            s[L10nKeys.Stats_Chart_ByEducation]        = "Голоса по образованию";
            s[L10nKeys.Stats_Chart_ByWealth]           = "Голоса по доходу";
            s[L10nKeys.Stats_Chart_NoData]             = "(нет данных — старый формат сохранения)";
            s[L10nKeys.Stats_Votes_Suffix]             = "{0} гол.";
            s[L10nKeys.Stats_PctCountFormat]           = "{0:P1}  ({1})";
            s[L10nKeys.Bucket_Age_Young]               = "Молодые";
            s[L10nKeys.Bucket_Age_Adult]               = "Взрослые";
            s[L10nKeys.Bucket_Age_Senior]              = "Пожилые";
            s[L10nKeys.Bucket_Edu_Uneducated]          = "Без образования";
            s[L10nKeys.Bucket_Edu_Educated]            = "С образованием";
            s[L10nKeys.Bucket_Edu_WellEducated]        = "Высокообразованные";
            s[L10nKeys.Bucket_Edu_HighlyEducated]      = "Элитное образование";
            s[L10nKeys.Bucket_Wealth_Low]              = "Низкий доход";
            s[L10nKeys.Bucket_Wealth_Medium]           = "Средний доход";
            s[L10nKeys.Bucket_Wealth_High]             = "Высокий доход";

            // Opinion Polling
            s[L10nKeys.Polling_Title]         = "Опросы общественного мнения";
            s[L10nKeys.Polling_NoHistory]     = "Данных опросов ещё нет — замеры делаются каждый игровой день.";
            s[L10nKeys.Polling_Subtitle]      = "Ежедневный опрос — размер выборки {0} — показаны последние {1} дн.";
            s[L10nKeys.Polling_Axis_Today]    = "сегодня";
            s[L10nKeys.Polling_Axis_DaysAgo]  = "-{0}д";

            // Info-view button
            s[L10nKeys.InfoButton_Tooltip]     = "Политический слой: переключать Партия / Явка / Удовлетворённость";
            s[L10nKeys.InfoButton_DragTooltip] = "Перетащите, чтобы переместить";

            return new Language("ru", "Русский", s);
        }
    }
}
