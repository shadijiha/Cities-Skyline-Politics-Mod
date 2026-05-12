using System;
using System.Collections.Generic;
using ColossalFramework.Globalization;
using UnityEngine;

namespace PoliticsMod.Localization
{
    // Static localization facade.
    //
    //   L10n.Init()           - call once from OnEnabled.
    //   L10n.T(key)           - lookup. Missing key -> English -> key itself.
    //   L10n.T(key, args)     - string.Format variant.
    //   L10n.LanguageChanged  - event; UI can re-render on it.
    //
    // Picks a catalog matching the game language, else English.
    // Re-picks automatically when the player changes language in options.
    public static class L10n
    {
        private static readonly Dictionary<string, Language> _catalogs =
            new Dictionary<string, Language>();

        private static Language _current;
        private static Language _fallback;
        private static bool _subscribedLocale;

        public static event Action LanguageChanged;

        public static bool IsInitialized { get { return _current != null; } }

        public static string CurrentCode
        {
            get { return _current != null ? _current.Code : "en"; }
        }

        public static void Init()
        {
            RegisterAll();

            // English is required - it's the fallback for everything.
            if (!_catalogs.TryGetValue("en", out _fallback))
            {
                Debug.LogError(Config.LogPrefix + "L10n: English catalog missing.");
                return;
            }

            SelectForGameLanguage();
            TrySubscribeLocaleChanged();

            PoliticsUserMod.Log("L10n: using '" + CurrentCode +
                "' (" + _catalogs.Count + " catalogs)");
        }

        public static string T(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (_current == null) return key; // Init() not run yet.

            string value;
            if (_current.Strings != null && _current.Strings.TryGetValue(key, out value))
                return value;

            if (_fallback != null && _fallback != _current &&
                _fallback.Strings.TryGetValue(key, out value))
                return value;

            return key;
        }

        public static string T(string key, params object[] args)
        {
            string template = T(key);
            if (args == null || args.Length == 0) return template;
            try { return string.Format(template, args); }
            catch (FormatException) { return template; }
        }

        public static void Register(Language lang)
        {
            if (lang == null || string.IsNullOrEmpty(lang.Code)) return;
            _catalogs[lang.Code] = lang;
        }

        public static string[] SupportedCodes()
        {
            string[] codes = new string[_catalogs.Count];
            int i = 0;
            foreach (var k in _catalogs.Keys) codes[i++] = k;
            return codes;
        }

        // Add new languages here.
        private static void RegisterAll()
        {
            Register(Languages.En.Build());
            // Register(Languages.De.Build());
            // Register(Languages.Fr.Build());
        }

        private static void SelectForGameLanguage()
        {
            string gameCode = TryGetGameLanguage();
            if (!string.IsNullOrEmpty(gameCode) && _catalogs.ContainsKey(gameCode))
            {
                _current = _catalogs[gameCode];
            }
            else
            {
                _current = _fallback;
                if (!string.IsNullOrEmpty(gameCode))
                    PoliticsUserMod.Log("L10n: '" + gameCode + "' unsupported, using English.");
            }
        }

        // Returns null if LocaleManager isn't ready (main menu, pre-load).
        private static string TryGetGameLanguage()
        {
            try
            {
                if (LocaleManager.exists && LocaleManager.instance != null)
                    return LocaleManager.instance.language;
            }
            catch (Exception e)
            {
                Debug.LogWarning(Config.LogPrefix + "L10n: LocaleManager read failed: " + e.Message);
            }
            return null;
        }

        private static void TrySubscribeLocaleChanged()
        {
            if (_subscribedLocale) return;
            try
            {
                LocaleManager.eventLocaleChanged += OnGameLocaleChanged;
                _subscribedLocale = true;
            }
            catch (Exception e)
            {
                Debug.LogWarning(Config.LogPrefix + "L10n: eventLocaleChanged subscribe failed: " + e.Message);
            }
        }

        private static void OnGameLocaleChanged()
        {
            string previous = CurrentCode;
            SelectForGameLanguage();
            if (CurrentCode == previous) return;

            PoliticsUserMod.Log("L10n: language -> '" + CurrentCode + "'");
            var h = LanguageChanged;
            if (h == null) return;
            try { h(); }
            catch (Exception e)
            {
                Debug.LogError(Config.LogPrefix + "L10n: handler threw: " + e);
            }
        }
    }
}
