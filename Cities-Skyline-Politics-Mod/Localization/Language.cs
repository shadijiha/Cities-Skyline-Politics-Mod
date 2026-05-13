using System.Collections.Generic;

namespace PoliticsMod.Localization
{
    // One language catalog: short code (matches LocaleManager, e.g. "en")
    // plus a key -> string dictionary.
    public class Language
    {
        public string Code;
        public string DisplayName;
        public Dictionary<string, string> Strings;

        public Language(string code, string displayName, Dictionary<string, string> strings)
        {
            Code = code;
            DisplayName = displayName;
            Strings = strings != null ? strings : new Dictionary<string, string>();
        }
    }
}
