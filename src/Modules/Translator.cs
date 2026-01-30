using BetterAmongUs.Helpers;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BetterAmongUs.Modules;

/// <summary>
/// Provides translation services for BetterAmongUs, supporting multiple languages and fallback mechanisms.
/// </summary>
internal static class Translator
{
    internal static Dictionary<string, int> TranslateIdLookup = [];
    internal static Dictionary<string, Dictionary<int, string>> TranslateMaps = [];
    private const string ResourcePath = "BetterAmongUs.Resources.Lang";

    /// <summary>
    /// Initializes the translator by loading all language files from embedded resources.
    /// </summary>
    internal static void Initialize()
    {
        Logger_.Log("Loading language files...", "Translator");
        LoadLanguages();
        Logger_.Log("Language files loaded successfully", "Translator");
    }

    /// <summary>
    /// Loads all language JSON files from the assembly's embedded resources.
    /// </summary>
    private static void LoadLanguages()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var jsonFileNames = GetJsonResourceNames(assembly);

            TranslateMaps = [];

            if (jsonFileNames.Length == 0)
            {
                Logger_.Error("JSON translation files do not exist.", "Translator");
                return;
            }

            foreach (var jsonFileName in jsonFileNames)
            {
                LoadLanguageFile(assembly, jsonFileName);
            }

            TranslateIdLookup = TranslateIdLookup.OrderBy(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        catch (Exception ex)
        {
            Logger_.Error($"Error loading languages: {ex}", "Translator");
        }
    }

    /// <summary>
    /// Gets the names of all JSON translation files in the assembly resources.
    /// </summary>
    /// <param name="assembly">The assembly to search for resources.</param>
    /// <returns>Array of JSON resource file names.</returns>
    private static string[] GetJsonResourceNames(System.Reflection.Assembly assembly)
    {
        return assembly.GetManifestResourceNames()
            .Where(resourceName => resourceName.StartsWith(ResourcePath) && resourceName.EndsWith(".json"))
            .ToArray();
    }

    /// <summary>
    /// Loads a single language file from embedded resources.
    /// </summary>
    /// <param name="assembly">The assembly containing the resource.</param>
    /// <param name="resourceName">The name of the resource file.</param>
    private static void LoadLanguageFile(System.Reflection.Assembly assembly, string resourceName)
    {
        try
        {
            using var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null) return;

            using var reader = new StreamReader(resourceStream);
            var jsonContent = reader.ReadToEnd();
            var jsonDictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);

            if (jsonDictionary == null)
            {
                Logger_.Error($"Failed to deserialize JSON from {resourceName}", "Translator");
                return;
            }

            if (jsonDictionary.TryGetValue("LanguageID", out var languageIdStr) &&
                int.TryParse(languageIdStr, out var languageId))
            {
                jsonDictionary.Remove("LanguageID");
                var name = resourceName[(ResourcePath.Length + 1)..^5]; // remove path from name
                TranslateIdLookup[name] = languageId;
                MergeTranslations(TranslateMaps, languageId, jsonDictionary);
            }
            else
            {
                Logger_.Error($"Invalid JSON format in {resourceName}: Missing or invalid 'LanguageID' field.", "Translator");
            }
        }
        catch (Exception ex)
        {
            Logger_.Error($"Error loading language file {resourceName}: {ex}", "Translator");
        }
    }

    /// <summary>
    /// Merges translations from a language file into the main translation maps.
    /// </summary>
    /// <param name="translationMaps">The main translation dictionary to merge into.</param>
    /// <param name="languageId">The language ID for the translations.</param>
    /// <param name="translations">The translations to merge.</param>
    private static void MergeTranslations(
        Dictionary<string, Dictionary<int, string>> translationMaps,
        int languageId,
        Dictionary<string, string> translations)
    {
        foreach (var (key, value) in translations)
        {
            if (!translationMaps.ContainsKey(key))
            {
                translationMaps[key] = [];
            }

            // Replace escape sequences with actual characters
            var processedValue = value.Replace("\\n", "\n").Replace("\\r", "\r");
            translationMaps[key][languageId] = processedValue;
        }
    }

    /// <summary>
    /// Gets the language ID by its name.
    /// </summary>
    /// <param name="name">The name of the language.</param>
    /// <returns>The language ID, or -1 if not found.</returns>
    internal static int GetLanguageIdByName(string name)
    {
        if (TranslateIdLookup.TryGetValue(name, out var id))
        {
            return id;
        }

        return -1;
    }

    /// <summary>
    /// Gets a translated string with optional replacements.
    /// </summary>
    /// <param name="key">The translation key.</param>
    /// <param name="replacements">Dictionary of string replacements to apply.</param>
    /// <param name="useConsoleLanguage">Whether to force English for console output.</param>
    /// <param name="showInvalid">Whether to show invalid key indicators.</param>
    /// <param name="useVanilla">Whether to use vanilla Among Us translations.</param>
    /// <returns>The translated string.</returns>
    internal static string GetString(string key, Dictionary<string, string>? replacements = null, bool useConsoleLanguage = false, bool showInvalid = true, bool useVanilla = false)
    {
        if (useVanilla)
        {
            return GetVanillaString(key, showInvalid);
        }

        var langId = GetTargetLanguageId(useConsoleLanguage);
        var result = GetString(key, langId, showInvalid);

        return ApplyReplacements(result, replacements);
    }

    /// <summary>
    /// Gets a translated string for a specific language.
    /// </summary>
    /// <param name="key">The translation key.</param>
    /// <param name="languageId">The language to use.</param>
    /// <param name="showInvalid">Whether to show invalid key indicators.</param>
    /// <returns>The translated string.</returns>
    internal static string GetString(string key, SupportedLangs languageId, bool showInvalid = true)
    {
        var fallbackText = showInvalid ? $"<INVALID:{key}>" : key;

        try
        {
            // Try to get from custom translations
            if (TranslateMaps.TryGetValue(key, out var languageMap))
            {
                var result = GetTranslationFromMap(key, languageId, languageMap, showInvalid);
                if (result != null) return result;
            }

            // Fallback to vanilla string names
            return GetVanillaStringFallback(key, fallbackText);
        }
        catch (Exception ex)
        {
            Logger_.Error($"Error retrieving string [{key}]: {ex}", "Translator");
            return fallbackText;
        }
    }

    /// <summary>
    /// Gets a translation from the language map with Chinese character detection.
    /// </summary>
    private static string GetTranslationFromMap(string key, SupportedLangs languageId, Dictionary<int, string> languageMap, bool showInvalid)
    {
        if (languageMap.TryGetValue((int)languageId, out var translation) &&
            !string.IsNullOrEmpty(translation))
        {
            // Check for Chinese characters in non-Chinese languages
            if (!IsChineseLanguage(languageId) && ContainsChineseCharacters(translation))
            {
                var chineseTranslation = GetString(key, SupportedLangs.SChinese, showInvalid);
                if (translation == chineseTranslation)
                {
                    return GetEnglishFallback(key);
                }
            }
            return translation;
        }

        // Fallback to English if translation not found
        return languageId == SupportedLangs.English ? $"*{key}" : GetString(key, SupportedLangs.English, showInvalid);
    }

    /// <summary>
    /// Gets a vanilla Among Us translated string.
    /// </summary>
    private static string GetVanillaString(string key, bool showInvalid)
    {
        if (Enum.TryParse<StringNames>(key, out var stringName))
        {
            return TranslationController.Instance.GetString(stringName);
        }

        return showInvalid ? $"<INVALID:{key}> (vanillaStr)" : key;
    }

    /// <summary>
    /// Fallback method to get vanilla string names.
    /// </summary>
    private static string GetVanillaStringFallback(string key, string fallbackText)
    {
        var matchingStringNames = EnumHelper.GetAllValues<StringNames>()
            .Where(x => x.ToString() == key)
            .ToArray();

        return matchingStringNames.Length > 0 ? GetString(matchingStringNames[0]) : fallbackText;
    }

    /// <summary>
    /// Gets a vanilla Among Us string by StringNames enum.
    /// </summary>
    /// <param name="stringName">The StringNames enum value.</param>
    /// <returns>The translated string.</returns>
    internal static string GetString(StringNames stringName) =>
        TranslationController.Instance.GetString(stringName, new Il2CppReferenceArray<Il2CppSystem.Object>(0));

    /// <summary>
    /// Gets the target language ID based on settings and system configuration.
    /// </summary>
    /// <param name="useConsoleLanguage">Whether to force English for console.</param>
    /// <returns>The target language ID.</returns>
    internal static SupportedLangs GetTargetLanguageId(bool useConsoleLanguage = false)
    {
        if (useConsoleLanguage) return SupportedLangs.English;
        if (BAUPlugin.ForceOwnLanguage.Value) return GetUserSystemLanguage();

        return TranslationController.InstanceExists ?
            TranslationController.Instance.currentLanguage.languageID :
            SupportedLangs.English;
    }

    /// <summary>
    /// Gets the user's system language as a SupportedLangs enum.
    /// </summary>
    /// <returns>The system language ID.</returns>
    internal static SupportedLangs GetUserSystemLanguage()
    {
        try
        {
            var cultureName = CultureInfo.CurrentUICulture.Name;

            return cultureName switch
            {
                string name when name.StartsWith("zh_CHT") => SupportedLangs.TChinese,
                string name when name.StartsWith("zh") => SupportedLangs.SChinese,
                string name when name.StartsWith("ru") => SupportedLangs.Russian,
                string name when name.StartsWith("en") => SupportedLangs.English,
                _ => TranslationController.Instance.currentLanguage.languageID
            };
        }
        catch
        {
            return SupportedLangs.English;
        }
    }

    /// <summary>
    /// Applies string replacements to a translated text.
    /// </summary>
    private static string ApplyReplacements(string text, Dictionary<string, string> replacements)
    {
        if (replacements == null) return text;

        foreach (var replacement in replacements)
        {
            text = text.Replace(replacement.Key, replacement.Value);
        }
        return text;
    }

    /// <summary>
    /// Gets an English fallback translation.
    /// </summary>
    private static string GetEnglishFallback(string key) =>
        GetString(key, SupportedLangs.English);

    /// <summary>
    /// Checks if a language ID represents a Chinese language.
    /// </summary>
    private static bool IsChineseLanguage(SupportedLangs languageId) =>
        languageId is SupportedLangs.SChinese or SupportedLangs.TChinese;

    /// <summary>
    /// Checks if a string contains Chinese characters.
    /// </summary>
    private static bool ContainsChineseCharacters(string text) =>
        Regex.IsMatch(text, @"[\u4e00-\u9fa5]");
}