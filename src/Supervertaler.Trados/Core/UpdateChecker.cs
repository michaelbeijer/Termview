using System;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Supervertaler.Trados.Settings;

namespace Supervertaler.Trados.Core
{
    /// <summary>
    /// Checks GitHub Releases for a newer version of the plugin.
    /// Call <see cref="CheckForUpdateAsync"/> from a background task — returns
    /// the new version info if an update is available, or null if up to date.
    /// </summary>
    public sealed class UpdateChecker
    {
        private static readonly HttpClient _http = new HttpClient();
        private const string ReleasesUrl = "https://api.github.com/repos/Supervertaler/Supervertaler-for-Trados/releases";

        static UpdateChecker()
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Supervertaler-Trados-UpdateCheck/1.0");
        }

        /// <summary>
        /// Checks GitHub for a newer release. Returns (newVersion, releaseUrl, pluginDownloadUrl)
        /// if an update is available, or null if the user is up to date (or if
        /// the check fails for any reason).
        /// </summary>
        public static async Task<(string version, string url, string pluginUrl)?> CheckForUpdateAsync()
        {
            var settings = TermLensSettings.Load();

            // Get the latest release from GitHub (first item is newest)
            var json = await _http.GetStringAsync(ReleasesUrl + "?per_page=1");

            // Parse the JSON array
            var releases = ParseReleases(json);
            if (releases == null || releases.Length == 0) return null;

            var latest = releases[0];
            var latestTag = (latest.TagName ?? "").TrimStart('v');
            var releaseUrl = latest.HtmlUrl ?? "";

            if (string.IsNullOrEmpty(latestTag)) return null;

            // Get current version
            var currentVersion = GetCurrentVersion();
            if (string.IsNullOrEmpty(currentVersion)) return null;

            // Compare
            if (CompareVersions(latestTag, currentVersion) <= 0) return null; // up to date

            // Check if user skipped this version
            if (string.Equals(settings.SkippedUpdateVersion, latestTag, StringComparison.OrdinalIgnoreCase))
                return null;

            // Find the .sdlplugin download URL from release assets
            string pluginUrl = null;
            if (latest.Assets != null)
            {
                foreach (var asset in latest.Assets)
                {
                    if (asset.Name != null && asset.Name.EndsWith(".sdlplugin", StringComparison.OrdinalIgnoreCase))
                    {
                        pluginUrl = asset.BrowserDownloadUrl;
                        break;
                    }
                }
            }

            return (latestTag, releaseUrl, pluginUrl);
        }

        /// <summary>
        /// Gets the current InformationalVersion from the assembly.
        /// </summary>
        internal static string GetCurrentVersion()
        {
            var asm = Assembly.GetExecutingAssembly();
            var attrs = asm.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
            if (attrs is AssemblyInformationalVersionAttribute[] infoAttrs && infoAttrs.Length > 0)
                return infoAttrs[0].InformationalVersion;

            var v = asm.GetName().Version;
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }

        /// <summary>
        /// Compares two semantic version strings (e.g. "4.1.0-beta" vs "4.0.2-beta").
        /// Returns positive if a > b, negative if a &lt; b, zero if equal.
        /// Pre-release (beta) sorts lower than release: 4.1.0-beta &lt; 4.1.0.
        /// </summary>
        internal static int CompareVersions(string a, string b)
        {
            ParseVersion(a, out int aMajor, out int aMinor, out int aPatch, out string aPre);
            ParseVersion(b, out int bMajor, out int bMinor, out int bPatch, out string bPre);

            var c = aMajor.CompareTo(bMajor);
            if (c != 0) return c;

            c = aMinor.CompareTo(bMinor);
            if (c != 0) return c;

            c = aPatch.CompareTo(bPatch);
            if (c != 0) return c;

            // Both have same numeric version — compare pre-release
            // No pre-release > any pre-release (4.1.0 > 4.1.0-beta)
            bool aHasPre = !string.IsNullOrEmpty(aPre);
            bool bHasPre = !string.IsNullOrEmpty(bPre);

            if (!aHasPre && !bHasPre) return 0;
            if (!aHasPre && bHasPre) return 1;   // a is release, b is pre-release
            if (aHasPre && !bHasPre) return -1;  // a is pre-release, b is release

            // Both have pre-release — compare lexically (beta.1 < beta.2)
            return string.Compare(aPre, bPre, StringComparison.OrdinalIgnoreCase);
        }

        private static void ParseVersion(string version, out int major, out int minor, out int patch, out string preRelease)
        {
            major = 0;
            minor = 0;
            patch = 0;
            preRelease = "";

            if (string.IsNullOrEmpty(version)) return;

            version = version.TrimStart('v');

            // Split off pre-release suffix
            var hyphen = version.IndexOf('-');
            string numPart;
            if (hyphen >= 0)
            {
                numPart = version.Substring(0, hyphen);
                preRelease = version.Substring(hyphen + 1);
            }
            else
            {
                numPart = version;
            }

            var parts = numPart.Split('.');
            if (parts.Length >= 1) int.TryParse(parts[0], out major);
            if (parts.Length >= 2) int.TryParse(parts[1], out minor);
            if (parts.Length >= 3) int.TryParse(parts[2], out patch);
        }

        /// <summary>
        /// Downloads a file from a URL to a local path.
        /// Used by the one-click update to download the .sdlplugin directly.
        /// </summary>
        internal static async Task DownloadFileAsync(string url, string destinationPath)
        {
            using (var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                using (var httpStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await httpStream.CopyToAsync(fileStream);
                }
            }
        }

        // --- Minimal JSON parsing for GitHub releases API ---

        [DataContract]
        private class GitHubRelease
        {
            [DataMember(Name = "tag_name")]
            public string TagName { get; set; }

            [DataMember(Name = "html_url")]
            public string HtmlUrl { get; set; }

            [DataMember(Name = "assets")]
            public GitHubAsset[] Assets { get; set; }
        }

        [DataContract]
        private class GitHubAsset
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "browser_download_url")]
            public string BrowserDownloadUrl { get; set; }
        }

        private static GitHubRelease[] ParseReleases(string json)
        {
            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    var serializer = new DataContractJsonSerializer(typeof(GitHubRelease[]));
                    return (GitHubRelease[])serializer.ReadObject(stream);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
