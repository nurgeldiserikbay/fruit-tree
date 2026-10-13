using System;

namespace FruitTree
{
    /// <summary>
    /// Paste the public Google Docs "Publish to web" URL here after legal review.
    /// Do not use a private edit link, a PDF or an invented placeholder URL.
    /// </summary>
    public static class FruitTreePrivacyConfig
    {
        public const string PublishedPolicyUrl = "";

        public static bool IsPublishedUrlValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                !string.Equals(uri.Host, "docs.google.com", StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrEmpty(uri.UserInfo) || uri.Port != 443)
                return false;

            // "Publish to web" document URLs have one nonempty published ID
            // and end in /pub. Merely containing "/pub" also accepts /publish,
            // /public-draft, and arbitrary subpaths, which are not public policies.
            const string prefix = "/document/d/e/";
            const string suffix = "/pub";
            string path = uri.AbsolutePath;
            if (!path.StartsWith(prefix, StringComparison.Ordinal) ||
                !path.EndsWith(suffix, StringComparison.Ordinal))
                return false;
            int idLength = path.Length - prefix.Length - suffix.Length;
            if (idLength < 1) return false;
            for (int i = prefix.Length; i < prefix.Length + idLength; i++)
            {
                char ch = path[i];
                if (!char.IsLetterOrDigit(ch) && ch != '-' && ch != '_')
                    return false;
            }
            return true;
        }
    }
}
