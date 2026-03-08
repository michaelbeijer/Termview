using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Supervertaler.Trados.Core
{
    /// <summary>
    /// Utility methods for language name display.
    /// Converts full language display names (from Trados or culture codes)
    /// into shortened forms suitable for UI labels.
    /// </summary>
    public static class LanguageUtils
    {
        private static readonly Regex ParenthesizedRegion = new Regex(
            @"^(.+?)\s*\((.+?)\)$", RegexOptions.Compiled);

        /// <summary>
        /// Shortens a language display name by abbreviating the country/region part
        /// to its ISO 3166-1 alpha-2 code.
        /// <para>Examples:</para>
        /// <list type="bullet">
        /// <item>"Dutch (Belgium)" → "Dutch (BE)"</item>
        /// <item>"English (United States)" → "English (US)"</item>
        /// <item>"nl-BE" → "Dutch (BE)"</item>
        /// <item>"en" → "English" (neutral culture, no region)</item>
        /// <item>"Dutch" → "Dutch" (unchanged)</item>
        /// </list>
        /// </summary>
        public static string ShortenLanguageName(string langName)
        {
            if (string.IsNullOrWhiteSpace(langName))
                return langName;

            langName = langName.Trim();

            // 1) Try to parse as a culture code (e.g., "en-US", "nl-BE")
            try
            {
                var culture = new CultureInfo(langName);
                if (!culture.IsNeutralCulture && culture.Name.Contains("-"))
                {
                    var region = new RegionInfo(culture.Name);
                    var langPart = culture.Parent.EnglishName;
                    return $"{langPart} ({region.TwoLetterISORegionName})";
                }
                if (culture.IsNeutralCulture)
                    return culture.EnglishName;
            }
            catch
            {
                // Not a valid culture code — fall through to display name parsing
            }

            // 2) Try to parse "Language (Country)" format and shorten the country
            var match = ParenthesizedRegion.Match(langName);
            if (match.Success)
            {
                var language = match.Groups[1].Value;
                var country = match.Groups[2].Value;

                // Already short (2–3 chars)? Return as-is.
                if (country.Length <= 3)
                    return langName;

                var isoCode = FindCountryIsoCode(country);
                if (isoCode != null)
                    return $"{language} ({isoCode})";
            }

            // 3) Fall back unchanged
            return langName;
        }

        /// <summary>
        /// Finds the 2-letter ISO 3166-1 country code for a country name.
        /// Searches all specific cultures' RegionInfo for a match.
        /// </summary>
        private static string FindCountryIsoCode(string countryName)
        {
            foreach (var ci in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                try
                {
                    var region = new RegionInfo(ci.Name);
                    if (string.Equals(region.EnglishName, countryName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(region.DisplayName, countryName, StringComparison.OrdinalIgnoreCase))
                    {
                        return region.TwoLetterISORegionName;
                    }
                }
                catch
                {
                    // Some cultures may throw — skip them
                }
            }
            return null;
        }
    }
}
