using System.Collections.Generic;

namespace PoliticsMod.Localization.Languages
{
    // Portuguese (pt) - Brazilian variant
    public static class Pt
    {
        public static Language Build()
        {
            var s = new Dictionary<string, string>();

            // Mod metadata
            s[L10nKeys.Mod_Name]        = "Mod de Política e Eleições";
            s[L10nKeys.Mod_Description] = "Os cidadãos elegem um parlamento; coalizões moldam as políticas da cidade. Pressione Ctrl+P para abrir o painel.";

            // Settings
            s[L10nKeys.Settings_Group_Main]         = "Política e Eleições";
            s[L10nKeys.Settings_EnableDebugLogging] = "Ativar logs de depuração";
            s[L10nKeys.Settings_Group_Hotkey]       = "Tecla de atalho do painel";
            s[L10nKeys.Settings_Hotkey]             = "Tecla de atalho";
            s[L10nKeys.Settings_RequireCtrl]        = "Exigir Ctrl pressionado";
            s[L10nKeys.Settings_Group_Utilities]    = "Utilitários";
            s[L10nKeys.Settings_OpenElectionsPanel] = "Abrir painel de eleições";
            s[L10nKeys.Settings_Language]           = "Idioma";
            s[L10nKeys.Settings_Language_Auto]      = "Automático (idioma do jogo)";

            // Common
            s[L10nKeys.Common_CloseX] = "X";

            // Main panel
            s[L10nKeys.Panel_Title]                  = "Política e Eleições";
            s[L10nKeys.Panel_Hemi_Tooltip]           = "O tamanho do parlamento cresce com a população:\n1 cadeira a cada {0} cidadãos (mín. {1}, máx. {2}).";
            s[L10nKeys.Panel_ActivePolicies]         = "Políticas ativas:";
            s[L10nKeys.Panel_ActivePolicies_None]    = "(nenhuma)";
            s[L10nKeys.Panel_Overlay_Prefix]         = "Camada: {0}";
            s[L10nKeys.Panel_Button_CallSnapElection]= "Convocar eleição antecipada";
            s[L10nKeys.Panel_Button_ManageParties]   = "Gerenciar Partidos";
            s[L10nKeys.Panel_Button_VoterTraits]     = "Perfil dos Eleitores";
            s[L10nKeys.Panel_Button_ElectionStats]   = "Estatísticas da Eleição";
            s[L10nKeys.Panel_Button_OpinionPolling]  = "Pesquisa de Opinião";
            s[L10nKeys.Panel_MinimizeChirps]         = "Minimizar chirps";
            s[L10nKeys.Panel_MinimizeChirps_Tooltip] = "Postar apenas chirps essenciais: início de campanha, resultados e aprovação de leis.";
            s[L10nKeys.Panel_ElectionTimings]        = "Tempos eleitorais (editáveis)";
            s[L10nKeys.Panel_Slider_TermLength]      = "Duração do mandato";
            s[L10nKeys.Panel_Slider_CampaignLength]  = "Duração da campanha";
            s[L10nKeys.Panel_Slider_ReElectionCooldown] = "Cooldown de nova eleição";
            s[L10nKeys.Panel_Slider_ReElectionCooldown_Tooltip] =
                "Dias no jogo que o parlamento fica paralisado após uma\n" +
                "coalizão FRACASSADA antes de uma eleição antecipada\n" +
                "automática.\n" +
                "\n" +
                "Só dispara quando nenhuma combinação de partidos consegue\n" +
                "maioria dentro do limite de parceiros. Use 0 para tentar de\n" +
                "novo imediatamente, ou valores maiores para simular um\n" +
                "impasse político prolongado. Não afeta eleições normais\n" +
                "em que a coalizão é formada com sucesso.";
            s[L10nKeys.Panel_Days]                   = "{0} dias";
            s[L10nKeys.Panel_Phase_Campaign]         = "Fase: {0} | Dia {1}/{2} da campanha";
            s[L10nKeys.Panel_Phase_Term]             = "Fase: {0} | Dia {1}/{2} do mandato";
            s[L10nKeys.Phase_Idle]                   = "Ocioso";
            s[L10nKeys.Phase_Campaign]               = "Campanha";
            s[L10nKeys.Phase_Voting]                 = "Votação";
            s[L10nKeys.Phase_Forming]                = "Formando coalizão";
            s[L10nKeys.Phase_Governing]              = "Governando";
            s[L10nKeys.Phase_Failed]                 = "Coalizão fracassada";
            s[L10nKeys.Panel_Coalition_Header]       = "Coalizão: {0}  ({1}/{2})";
            s[L10nKeys.Panel_Coalition_None]         = "Coalizão: (nenhuma)";
            s[L10nKeys.Panel_Policies_More]          = "+{0}";

            // Overlay
            s[L10nKeys.Overlay_Off]          = "Desligado";
            s[L10nKeys.Overlay_Party]        = "Partido";
            s[L10nKeys.Overlay_Turnout]      = "Comparecimento";
            s[L10nKeys.Overlay_Satisfaction] = "Satisfação";
            s[L10nKeys.InfoButton_Prefix]    = "Política: {0}";

            // Party editor
            s[L10nKeys.PartyEditor_Title]               = "Gerenciar Partidos";
            s[L10nKeys.PartyEditor_Add]                 = "+ Adicionar partido";
            s[L10nKeys.PartyEditor_Remove]              = "– Remover";
            s[L10nKeys.PartyEditor_ShortName]           = "Sigla";
            s[L10nKeys.PartyEditor_FullName]            = "Nome completo";
            s[L10nKeys.PartyEditor_Color]               = "Cor";
            s[L10nKeys.PartyEditor_IdeologyHeader]      = "Ideologia (-1 ... +1)";
            s[L10nKeys.PartyEditor_Ideology_Economic]   = "Econômico (esq.↔dir.)";
            s[L10nKeys.PartyEditor_Ideology_Social]     = "Social (progr.↔trad.)";
            s[L10nKeys.PartyEditor_Ideology_Governance] = "Governança (liberal↔autorit.)";
            s[L10nKeys.PartyEditor_PoliciesHeader]      = "Políticas (clique para alternar: neutro → apoia → opõe)";
            s[L10nKeys.PartyEditor_Stance_Support]      = "{0}\nApoia: será implementada ao assumir";
            s[L10nKeys.PartyEditor_Stance_Oppose]       = "{0}\nOpõe-se: será revogada ao assumir";
            s[L10nKeys.PartyEditor_Stance_Neutral]      = "{0}\nNeutro: não é alterada ao assumir";
            s[L10nKeys.PartyEditor_TaxHeader]           = "Variação de impostos (pts., -10 .. +10)";
            s[L10nKeys.PartyEditor_Tax_Res]             = "Residencial";
            s[L10nKeys.PartyEditor_Tax_Com]             = "Comercial";
            s[L10nKeys.PartyEditor_Tax_Ind]             = "Industrial";
            s[L10nKeys.PartyEditor_Tax_Off]             = "Escritórios";
            s[L10nKeys.PartyEditor_BudgetHeader]        = "Variação de orçamento (pts., -30 .. +30)";
            s[L10nKeys.PartyEditor_Budget_Electricity]    = "Eletricidade";
            s[L10nKeys.PartyEditor_Budget_Water]          = "Água";
            s[L10nKeys.PartyEditor_Budget_Garbage]        = "Lixo";
            s[L10nKeys.PartyEditor_Budget_Healthcare]     = "Saúde";
            s[L10nKeys.PartyEditor_Budget_Fire]           = "Bombeiros";
            s[L10nKeys.PartyEditor_Budget_Police]         = "Polícia";
            s[L10nKeys.PartyEditor_Budget_Education]      = "Educação";
            s[L10nKeys.PartyEditor_Budget_Transport]      = "Transporte";
            s[L10nKeys.PartyEditor_Budget_Beautification] = "Paisagismo";
            s[L10nKeys.PartyEditor_Budget_Roads]          = "Estradas";
            s[L10nKeys.PartyEditor_Budget_Industry]       = "Indústria";
            s[L10nKeys.PartyEditor_NewPartyShortName]     = "NOVO{0}";
            s[L10nKeys.PartyEditor_NewPartyFullName]      = "Novo Partido {0}";
            s[L10nKeys.PartyEditor_ResetAll]              = "Restaurar padrões";

            // Voter Traits
            s[L10nKeys.VoterTraits_Title]                 = "Perfil dos Eleitores — Viés no eixo econômico (-1 esq. ... +1 dir.)";
            s[L10nKeys.VoterTraits_Note]                  =
                "Ajusta os eleitores no eixo econômico. Negativo = esquerda\n" +
                "(impostos maiores, serviços fortes). Positivo = direita\n" +
                "(impostos baixos, pró-empresa). Só votam cidadãos Jovens,\n" +
                "Adultos e Idosos.";
            s[L10nKeys.VoterTraits_Section_Education]     = "Educação";
            s[L10nKeys.VoterTraits_Edu_Uneducated]        = "Sem instrução";
            s[L10nKeys.VoterTraits_Edu_Educated]          = "Com instrução";
            s[L10nKeys.VoterTraits_Edu_WellEducated]      = "Bem instruído";
            s[L10nKeys.VoterTraits_Edu_HighlyEducated]    = "Altamente instruído";
            s[L10nKeys.VoterTraits_Section_Wealth]        = "Renda";
            s[L10nKeys.VoterTraits_Wealth_Low]            = "Baixa renda";
            s[L10nKeys.VoterTraits_Wealth_Medium]         = "Renda média";
            s[L10nKeys.VoterTraits_Wealth_High]           = "Alta renda";
            s[L10nKeys.VoterTraits_Section_Employment]    = "Emprego";
            s[L10nKeys.VoterTraits_Employment_Employed]   = "Empregado";
            s[L10nKeys.VoterTraits_Employment_Unemployed] = "Desempregado";
            s[L10nKeys.VoterTraits_Section_Age]           = "Idade";
            s[L10nKeys.VoterTraits_Age_Young]             = "Jovem";
            s[L10nKeys.VoterTraits_Age_Adult]             = "Adulto";
            s[L10nKeys.VoterTraits_Age_Senior]            = "Idoso";
            s[L10nKeys.VoterTraits_Section_Life]          = "Condições de vida";
            s[L10nKeys.VoterTraits_Life_Sick]             = "Doente";
            s[L10nKeys.VoterTraits_Life_Pollution]        = "Mora em zona poluída";
            s[L10nKeys.VoterTraits_Section_Deficit]       = "Sensibilidade ao déficit";
            s[L10nKeys.VoterTraits_Deficit_Label]         = "Multiplicador do déficit";
            s[L10nKeys.VoterTraits_Deficit_Tooltip]       =
                "Quão fortemente um déficit orçamentário prolongado empurra\n" +
                "os eleitores para a direita econômica.\n" +
                "  0 = desligado; déficits não afetam o voto.\n" +
                "  1 = curva padrão: até +0,35 de viés à direita após\n" +
                "      cerca de 6 semanas seguidas de déficit.\n" +
                "  2 = sensibilidade dobrada. 3 = máximo.\n" +
                "Somado ao viés individual de cada eleitor.";
            s[L10nKeys.VoterTraits_Section_Incumbency]    = "Vantagem do incumbente";
            s[L10nKeys.VoterTraits_Incumbency_Label]      = "Bônus de incumbência";
            s[L10nKeys.VoterTraits_Incumbency_Tooltip]    =
                "Probabilidade de um eleitor FELIZ premiar a coalizão\n" +
                "no poder com seu voto, em vez de seguir ideologia ou\n" +
                "queixas. Só se aplica a eleitores com felicidade 60 ou\n" +
                "mais.\n" +
                "\n" +
                "  0,00 = sem efeito; voto sempre pela afinidade.\n" +
                "  0,10 = padrão. 1 em cada 10 eleitores felizes migra\n" +
                "         para o governo.\n" +
                "  0,25 = 1/4 dos eleitores felizes renova o governo.\n" +
                "  0,50 = metade dos eleitores felizes renova o governo;\n" +
                "         forte vantagem enquanto a cidade prospera.\n" +
                "\n" +
                "Dica: deixe baixo (≤0,05) se quiser eleições sempre\n" +
                "abertas, ou alto (≥0,25) para simular incumbentes\n" +
                "difíceis de derrotar.";
            s[L10nKeys.VoterTraits_Reset]                 = "Restaurar padrões";

            // Election Stats
            s[L10nKeys.Stats_Title]             = "Estatísticas da Eleição";
            s[L10nKeys.Stats_NoData_Subtitle]   = "Ainda não há dados eleitorais.";
            s[L10nKeys.Stats_NoData_Body]       = "Não foram encontrados dados estatísticos de eleição.";
            s[L10nKeys.Stats_Subtitle]          = "Eleição {0}-{1} — {2} votos amostrados  •  Comparecimento {3}%";
            s[L10nKeys.Stats_PartyColors]       = "Cores dos partidos";
            s[L10nKeys.Stats_WhyPeopleVoted]    = "Por que as pessoas votaram";
            s[L10nKeys.Stats_Grievance_Ideology]       = "Ideologia pura";
            s[L10nKeys.Stats_Grievance_HighTaxes]      = "Impostos altos";
            s[L10nKeys.Stats_Grievance_PoorHealth]     = "Saúde ruim";
            s[L10nKeys.Stats_Grievance_HighCrime]      = "Criminalidade alta";
            s[L10nKeys.Stats_Grievance_PoorEducation]  = "Educação deficiente";
            s[L10nKeys.Stats_Grievance_Unemployment]   = "Desemprego";
            s[L10nKeys.Stats_Grievance_Pollution]      = "Poluição";
            s[L10nKeys.Stats_Grievance_LowLandValue]   = "Baixo valor do solo";
            s[L10nKeys.Stats_Grievance_NoiseTrash]     = "Ruído / lixo";
            s[L10nKeys.Stats_Chart_ByAge]              = "Voto por faixa etária";
            s[L10nKeys.Stats_Chart_ByEducation]        = "Voto por escolaridade";
            s[L10nKeys.Stats_Chart_ByWealth]           = "Voto por renda";
            s[L10nKeys.Stats_Chart_NoData]             = "(sem dados — formato de save antigo)";
            s[L10nKeys.Stats_Votes_Suffix]             = "{0} votos";
            s[L10nKeys.Stats_PctCountFormat]           = "{0:P1}  ({1})";
            s[L10nKeys.Bucket_Age_Young]               = "Jovem";
            s[L10nKeys.Bucket_Age_Adult]               = "Adulto";
            s[L10nKeys.Bucket_Age_Senior]              = "Idoso";
            s[L10nKeys.Bucket_Edu_Uneducated]          = "Sem instrução";
            s[L10nKeys.Bucket_Edu_Educated]            = "Com instrução";
            s[L10nKeys.Bucket_Edu_WellEducated]        = "Bem instruído";
            s[L10nKeys.Bucket_Edu_HighlyEducated]      = "Altamente instruído";
            s[L10nKeys.Bucket_Wealth_Low]              = "Baixa renda";
            s[L10nKeys.Bucket_Wealth_Medium]           = "Renda média";
            s[L10nKeys.Bucket_Wealth_High]             = "Alta renda";

            // Opinion Polling
            s[L10nKeys.Polling_Title]         = "Pesquisa de Opinião";
            s[L10nKeys.Polling_NoHistory]     = "Ainda não há dados — as amostras são coletadas a cada dia no jogo.";
            s[L10nKeys.Polling_Subtitle]      = "Pesquisa diária — amostra {0} — mostrando últimos {1} dia(s)";
            s[L10nKeys.Polling_Axis_Today]    = "hoje";
            s[L10nKeys.Polling_Axis_DaysAgo]  = "-{0}d";

            // Info-view button
            s[L10nKeys.InfoButton_Tooltip]     = "Camada política: alternar Partido / Comparecimento / Satisfação";
            s[L10nKeys.InfoButton_DragTooltip] = "Arraste para mover";

            return new Language("pt", "Português (Brasil)", s);
        }
    }
}
